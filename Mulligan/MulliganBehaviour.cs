//      __  ___      _____                 
//     /  |/  /_  __/ / (_)___ _____ _____ 
//    / /|_/ / / / / / / / __ `/ __ `/ __ \
//   / /  / / /_/ / / / / /_/ / /_/ / / / /
//  /_/  /_/\__,_/_/_/_/\__, /\__,_/_/ /_/ 
//                     /____/
//                                 PPG Mod
//
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Mulligan
{
	public class MulliganBehaviour : MonoBehaviour
	{
		private static ObjectState[] SavedScene;
		private static readonly List<LayeringOrder> SortingLayersList = new List<LayeringOrder>();
		private static readonly List<Flipper> FlippingAholes          = new List<Flipper>();
		private static readonly List<int> NoFlip                      = new List<int>();
		private static int LastSceneNumber                   = 1;
		private static bool CheckDisabledCollisions          = false;
		private static int ApplyLayerOrdering                = 0;
		private static int CurrentPoseMode                   = 0;

		private static readonly List<DoubleFist> DoubleFisters        = new List<DoubleFist>();
		private static bool CheckDoubleFist                  = false;

		private static bool SceneLoaded = false;

		private static readonly List<DisabledCollision> DisabledCollisions = new List<DisabledCollision>();
		private static readonly List<CarBehaviour> CARS4KIDS               = new List<CarBehaviour>();

		public static KeyCode xModifierx;

		public struct LayeringOrder 
		{
			public string sortingLayerName;
			public int sortingOrder;
		}

		private struct Flipper
		{
			public int instanceID;
			public PersonBehaviour PB;
			public Vector2 moveA;
			public Vector2 moveB;
			public Vector3 flipScale;
			public int status;

			public override int GetHashCode() => base.GetHashCode();
			public override bool Equals(object obj) => (int)obj == instanceID;
		}

		public class CustomPose
		{
			public Dictionary<string, float> LimbAngles { get; set; }
			public bool ShouldStandUpright { get; set; }
			public float AnimationSpeedMultiplier { get; set; }

		}

		private struct DoubleFist
		{
			public PersonBehaviour PBO;
			public PhysicalBehaviour Item;
			public string HoldingPose;
			public Vector3 HoldingPosition;
		}

		private Dictionary<string, CustomPose> CustomPoses = new Dictionary<string, CustomPose>();
		private bool bInitCustomPoses = false;


		// - - - - - - - - - - - - - - - - - - -
		//  STRUCT: DISABLED COLLISION
		//  
		//  When auto-equipped, collisions are disabled
		//  When object is dropped, the colliders are mapped here so
		//  we can re-enable the collision when safe and not touching
		//
		private struct DisabledCollision
		{
			private readonly Transform obj1;
			private readonly PhysicalBehaviour obj2;
			public DisabledCollision(Transform _obj1, PhysicalBehaviour _obj2)
			{
				obj1 = _obj1;
				obj2 = _obj2;
			}
			public bool Check()
			{

				if (obj1 == null || obj2 == null) return true;

				if (obj2.colliders.Length > 0) {

					Collider2D[] OtherColliders = obj1.GetComponentsInChildren<Collider2D>();

					if (OtherColliders == null || OtherColliders.Length == 0) return true;

					for (int i = obj2.colliders.Length; --i >= 0;)
					{
						for (int i2 = OtherColliders.Length; --i2 >= 0;)
						{
							if ((bool)(UnityEngine.Object)obj2.colliders[i] && (bool)(UnityEngine.Object)OtherColliders[i] && obj2.colliders[i].bounds.Intersects(OtherColliders[i].bounds)) return false;
						}
					}

					for (int i = obj2.colliders.Length; --i >= 0;)
					{
						for (int i2 = OtherColliders.Length; --i2 >= 0;)
						{
							if ((bool)(UnityEngine.Object)obj2.colliders[i] && (bool)(UnityEngine.Object)OtherColliders[i])
								Physics2D.IgnoreCollision(obj2.colliders[i], OtherColliders[i], false);
						}
					}
				}
				return true;
			}
		}

		public void FixedUpdate()
		{
			if (FlipUtility.FlipState != FlipStates.ready)
			{
				FlipUtility.RunFlip();
			}

			if (SceneLoaded)
			{
				foreach (PhysicalBehaviour item in Global.main.PhysicalObjectsInWorld)
				{
					if (item.name == "Brain")
					{
						Transform head = item.transform.root.Find("Head");
						item.transform.position = head.position;
					}
				}

				SceneLoaded = false;
			}
		}

		public void Update()
		{
			if (Time.frameCount % 1000 == 0) { 
				if (CheckDoubleFist) HoldBothHands();
				if (CheckDisabledCollisions) FixDisabledCollisions();
			}

			if (ApplyLayerOrdering > 0)
			{
				--ApplyLayerOrdering;
				ReapplyLayerOrder();
			}

			//
			//  Check for Hotkey, Execute associated Function
			//
			if (Input.GetKey(KeyCode.None)) return;     // dont bother checking the others
			else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
					&& (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) ) 
			{
				if (Input.GetKeyDown(KeyCode.Keypad1)) ToggleMapConfig("floodlights");
				if (Input.GetKeyDown(KeyCode.Keypad2)) ToggleMapConfig("fog");
				if (Input.GetKeyDown(KeyCode.Keypad3)) ToggleMapConfig("rain");
				if (Input.GetKeyDown(KeyCode.Keypad4)) ToggleMapConfig("snow");
			}
			else if (Mulligan.KeyCheck("Mul-HealAll"     )) HealPeople(true);
			else if (Mulligan.KeyCheck("Mul-HealSelected")) HealPeople(false);
			else if (Mulligan.KeyCheck("Mul-Mend"        )) MendPeople();
			else if (Mulligan.KeyCheck("Mul-Strengthen"  )) Strengthen();
			else if (Mulligan.KeyCheck("Mul-LayerUp"     )) AdjustSortingOrder(10);
			else if (Mulligan.KeyCheck("Mul-LayerDown"   )) AdjustSortingOrder(-10);
			else if (Mulligan.KeyCheck("Mul-LayerBG"     )) LayerBG(true);
			else if (Mulligan.KeyCheck("Mul-LayerFG"     )) LayerBG(false);
			else if (Mulligan.KeyCheck("Mul-LayerTop"    )) AdjustSortingLayer("Top");
			else if (Mulligan.KeyCheck("Mul-LayerBottom" )) AdjustSortingLayer("Bottom");
			else if (Mulligan.KeyCheck("Mul-ShowInfo"    )) ShowInfo();
			else if (Mulligan.KeyCheck("Mul-CycleVerbose")) CycleVerbose();
			else if (Mulligan.KeyCheck("Mul-Follow"      )) FollowObject();
			else if (Mulligan.KeyCheck("Mul-FreezeItems" )) FreezeItems();
			else if (Mulligan.KeyCheck("Mul-CarSpeed"    )) SetCarSpeed();
			else if (Mulligan.KeyCheck("Mul-QuickSave"   )) QuickSave();
			else if (Mulligan.KeyCheck("Mul-QuickReset"  )) QuickReset(true);
			else if (Mulligan.KeyCheck("Mul-SaveScene"   )) SaveScene();
			else if (Mulligan.KeyCheck("Mul-LoadScene"   )) LoadScene();
			else if (Mulligan.KeyCheck("Mul-GrabItemF"   )) GrabClosestItem(true);
			else if (Mulligan.KeyCheck("Mul-GrabItemB"   )) GrabClosestItem(false);
			else if (Mulligan.KeyCheck("Mul-ActivateHeld")) ActivateHeldItems();
			else if (Mulligan.KeyCheck("Mul-TglCollision")) ToggleCollisions();
			else if (Mulligan.KeyCheck("Mul-PeepSit"     )) ChangePose((int)PoseState.Sitting);
			else if (Mulligan.KeyCheck("Mul-PeepWalk"    )) ChangePose((int)PoseState.Walking);
			else if (Mulligan.KeyCheck("Mul-PeepFlat"    )) ChangePose((int)PoseState.Flat);
			else if (Mulligan.KeyCheck("Mul-PeepCower"   )) ChangePose((int)PoseState.Protective);
			else if (Mulligan.KeyCheck("Mul-PeepStumble" )) ChangePose((int)PoseState.Stumbling);
			else if (Mulligan.KeyCheck("Mul-PeepPain"    )) ChangePose((int)PoseState.WrithingInPain);
			else if (Mulligan.KeyCheck("Mul-PeepFlailing")) ChangePose((int)PoseState.Flailing);
			else if (Mulligan.KeyCheck("Mul-PeepSwimming")) ChangePose((int)PoseState.Swimming);
			else if (Mulligan.KeyCheck("Mul-DelItems"    )) DeleteItems();
			else if (Mulligan.KeyCheck("Mul-DelXFItems"  )) DeleteXFItems();
			else if (Mulligan.KeyCheck("Mul-SelectSame"  )) SelectSame();
			else if (Mulligan.KeyCheck("Mul-FlipSelected")) FlipUtility.FlipInit(GetSelected());
			//else if (Mulligan.KeyCheck("Mul-FlipSelected")) FlipSelectedItems(GetSelected());
			else if (Mulligan.KeyCheck("Mul-PoseMode"    )) CyclePoseMode();
		}



		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: GET SELECTED SHTUFF
		//
		public List<PhysicalBehaviour> GetSelected(bool bothTypes = false)
		{
			List<PhysicalBehaviour> PBL = new List<PhysicalBehaviour>();

			if (bothTypes || SelectionController.Main.SelectedObjects.Count == 0)
			{
				foreach (PhysicalBehaviour PB in Global.main.PhysicalObjectsInWorld)
				{
					if (!PB.Selectable || PB.colliders.Length == 0) continue;

					foreach (Collider2D collider in PB.colliders)
					{
						if (!collider) continue;

						if (collider.OverlapPoint((Vector2)Global.main.MousePosition))
						{
							if (PB.transform.root.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour component))
							{
								if (component.Selectable) PBL.Add(component);
							}
							else
							{
								PBL.Add(PB);
							}
						}
					}
				}
			}

			if (bothTypes || PBL.Count == 0) PBL.AddRange(SelectionController.Main.SelectedObjects);

			return PBL;

		}

		public void CyclePoseMode(int poseMode=-1)
		{
			if (++CurrentPoseMode >= 3) CurrentPoseMode = 0;

			if (poseMode >= 0) CurrentPoseMode = poseMode;

			if (Mulligan.verboseLevel >= 1) {

				string[] modeNames  = new string[3] {"DEFAULT", "CUSTOM", "CREATE"};
				string[] modeColors = new string[3] {"blue",    "green",  "red"};

				ModAPI.Notify("Pose Mode: <color=" + modeColors[CurrentPoseMode] + ">" + modeNames[CurrentPoseMode]+ "</color>");

			}
			if (!bInitCustomPoses) InitCustomPoses();

		}

		public void CreatePoseData(int customPoseId)
		{

			if( !bInitCustomPoses) InitCustomPoses();
			//  Make sure only 1 person is selected
			List<int> uniquePeeps = new List<int>();

			foreach (PhysicalBehaviour SelectedObject in GetSelected())
			{
				PersonBehaviour person = SelectedObject.gameObject.GetComponentInParent<PersonBehaviour>();

				if (person && !uniquePeeps.Contains(person.GetHashCode())) uniquePeeps.Add(person.GetHashCode());
			}

			if (uniquePeeps.Count > 1)
			{
				if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=red>Pose Failed:</color> Multiple peeps selected");
				return;
			}

			CustomPose customPose = new CustomPose()
			{
				ShouldStandUpright       = false,
				AnimationSpeedMultiplier = 1f,
				LimbAngles               = new Dictionary<string, float>()
			};


			//  Get limb positions
			foreach (PhysicalBehaviour PB in GetSelected())
			{
				if (PB.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb))
				{
					customPose.LimbAngles.Add(limb.name, limb.HasJoint ? limb.Joint.jointAngle : 0f);
				}
			}

			string poseName = "MulCustom_" + customPoseId;

			if (CustomPoses.ContainsKey(poseName) ) CustomPoses[poseName] = customPose;
			else CustomPoses.Add(poseName, customPose);

			PlayerPrefs.SetString("MulCustomPoses", JsonConvert.SerializeObject(CustomPoses, Formatting.None));

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=green>Pose Created:</color> Custom " + customPoseId);
			CyclePoseMode(1);

		}

		private void InitCustomPoses()
		{

			if (bInitCustomPoses) return;

			bInitCustomPoses = true;

			string preCustomPoses = @"{
				'HoldRifle':{ 'LimbAngles':
					{ 'LowerArmFront':-120,'UpperArmFront':20,'UpperArm':-75},'ShouldStandUpright':false,'AnimationSpeedMultiplier':100.0},
				'HoldHandgun':{ 'LimbAngles'
					{ 'LowerArmFront':-20,'UpperArmFront':-80,'LowerArm':-20,'UpperArm':-80},'ShouldStandUpright':false,'AnimationSpeedMultiplier':100.0}
			}";


			CustomPoses = JsonConvert.DeserializeObject<Dictionary<string, CustomPose>>(preCustomPoses.Replace("'", "\""));


			if (PlayerPrefs.HasKey("MulCustomPoses"))
			{

				Dictionary<string, CustomPose> UserPoses = JsonConvert.DeserializeObject<Dictionary<string, CustomPose>>(
					PlayerPrefs.GetString("MulCustomPoses"));

				foreach (KeyValuePair<string, CustomPose> pair in UserPoses) {

					if (!CustomPoses.ContainsKey(pair.Key)) CustomPoses.Add(pair.Key, pair.Value);

				}

			}

			if (Mulligan.verboseLevel >= 2) ModAPI.Notify("Initialized " + CustomPoses.Count + " Custom Poses");

		}



		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: GET CUSTOM POSE
		//  Checks if peep has a custom pose and creates it if necessary
		//
		public int GetCustomPose(PersonBehaviour PB, string PoseName)
		{
			if (!bInitCustomPoses) InitCustomPoses();

			if (!CustomPoses.ContainsKey(PoseName)) {
				if (Mulligan.verboseLevel >= 2) ModAPI.Notify("Missing Custom Pose: " + PoseName);
				return 0;
			}

			for (int pnum = PB.Poses.Count; --pnum >= 0;)
				if (PB.Poses[pnum].Name == PoseName) return pnum;

			RagdollPose customPose = new RagdollPose()
			{
				Name                     = PoseName,
				Angles                   = new List<RagdollPose.LimbPose>(),
				ShouldStandUpright       = CustomPoses[PoseName].ShouldStandUpright,
				AnimationSpeedMultiplier = CustomPoses[PoseName].AnimationSpeedMultiplier 
			};


			foreach (LimbBehaviour limb in PB.Limbs)
			{
				if (CustomPoses[PoseName].LimbAngles.TryGetValue(limb.name, out float limbAngle))
				{
					customPose.Angles.Add(new RagdollPose.LimbPose(limb, limbAngle)
					{
						Animated = true,
						StartAngle = limbAngle,
						EndAngle = limbAngle,
						AnimationDuration = 10.01f,
					});
				}
				else
				{
					customPose.Angles.Add(new RagdollPose.LimbPose(limb, 0f)
					{
						Animated = false,
						StartAngle = 0f,
						EndAngle = 0f,
					});
				}
			}

			customPose.ConstructDictionary();

			PB.Poses.Add(customPose);

			if (Mulligan.verboseLevel >= 2) ModAPI.Notify("Assigned Custom Pose: " + PoseName);

			return PB.Poses.Count - 1;
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: CHANGE POSE
		//
		public void ChangePose(int poseNum)
		{

			//  Check which pose mode were current using
			if (CurrentPoseMode == 2)
			{
				CreatePoseData(poseNum);
				return;
			}

			int triggeredPoseNum = poseNum;

			int peepsCount = 0;
			List<PersonBehaviour> PeopleAlreadyPosed = new List<PersonBehaviour>();

			//  Loop thru all peeps within a selection and change their PoseIndex
			//
			foreach (PhysicalBehaviour SelectedObject in GetSelected())
			{
				PersonBehaviour person = SelectedObject.gameObject.GetComponentInParent<PersonBehaviour>();

				if (person)
				{
					if (PeopleAlreadyPosed.Contains(person)) continue;

					++peepsCount;

					PeopleAlreadyPosed.Add(person);

					if (CurrentPoseMode == 1)
					{
						poseNum = GetCustomPose(person, "MulCustom_" + triggeredPoseNum);
					}

					//  If the person was already doing that pose, then have them stand still instead
					if (person.OverridePoseIndex == poseNum) person.OverridePoseIndex = -1;
					else person.OverridePoseIndex = poseNum;

				}
			}

			if (Mulligan.verboseLevel >= 2)
			{
				string poseName;
				if (CurrentPoseMode == 1) poseName = "CustomPose_" + triggeredPoseNum;
				else { PoseState tmp = (PoseState)poseNum; poseName = tmp.ToString(); }
				ModAPI.Notify("[<color=red>" + poseName + "</color>] toggled for " + peepsCount);
			}
		}

		public void ToggleMapConfig(string settingName)
		{
			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Setting: " + settingName);

			bool doForced = false;
			bool forceVal = true;

			foreach (MapConfig _mapConfig in UnityEngine.Object.FindObjectsOfType<MapConfig>())
			{
				EnvironmentalSettings esettings = _mapConfig.Settings.ShallowClone();

				switch (settingName.ToLower())
				{
					case "floodlights":
						esettings.Floodlights = doForced ? forceVal : !esettings.Floodlights;
						break;

					case "rain":
						esettings.Rain = doForced ? forceVal : !esettings.Rain;
						break;

					case "fog":
						esettings.Fog = doForced ? forceVal : !esettings.Fog;
						break;

					case "snow":
						esettings.Snow = doForced ? forceVal : !esettings.Snow;
						break;
				}

				//_mapConfig.Settings = esettings;
				_mapConfig.ApplySettings(esettings);
			}


		}


		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: HOLD ITEM WITH BOTH HANDS
		//  Shit attempt to have peeps hold an item with both hands
		//
		public void HoldBothHands()
		{
			for (int i = DoubleFisters.Count; --i >= 0;)
			{
				DoubleFist doubleFist = DoubleFisters[i];

				if (!doubleFist.PBO || !doubleFist.Item)
				{
					DoubleFisters.RemoveAt(i);
					continue;
				}
				if (doubleFist.PBO.ActivePose.Name != doubleFist.HoldingPose) { 

					//  Check if peep already has pose
					int pnum     = GetCustomPose(doubleFist.PBO, doubleFist.HoldingPose);

					doubleFist.PBO.OverridePoseIndex = pnum;

					continue;
				}

				Rigidbody2D handF = doubleFist.PBO.transform.Find("FrontArm").Find("LowerArmFront").GetComponent<Rigidbody2D>();
				Rigidbody2D handB = doubleFist.PBO.transform.Find("BackArm").Find("LowerArm").GetComponent<Rigidbody2D>();



				if (Math.Abs(handF.rotation) > 20 && (
					(Math.Abs(handF.angularVelocity) < 3 && Math.Abs(handB.angularVelocity) < 3 ) 
					) ) { 

					Vector2 NearestHoldingPos = new Vector2(0.0f, 0.0f);
					GripBehaviour GB          = handF.GetComponent<GripBehaviour>();

					doubleFist.Item.gameObject.layer = 9;
					//doubleFist.Item.MakeWeightful();

					DoubleFisters.RemoveAt(i);

					doubleFist.Item.transform.position += GB.transform.TransformPoint(GB.GripPosition) - doubleFist.Item.transform.TransformPoint((Vector3)NearestHoldingPos);

					if (!handB.GetComponent<GripBehaviour>().isHolding) handB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

					if (!GB.isHolding) {
						handF.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
					}
				}
			}

			if (DoubleFisters.Count == 0) CheckDoubleFist = false;
		}


		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: GRAB CLOSEST ITEM
		//
		public void GrabClosestItem(bool useFrontArm=true)
		{
			PersonBehaviour PBO; 
			Collider2D[]    noCollide;
			Rigidbody2D     LowerArm;
			Rigidbody2D     AltArm;
			List<int>       PeepsWithItems    = new List<int>();
			int             peepsEquipped     = 0;
			int             peepsDropped      = 0;
			int             verbose           = Mulligan.verboseLevel;
			bool            bothArms;
			List <PhysicalBehaviour> JustHeld = new List<PhysicalBehaviour>();

			if (useFrontArm) bothArms = InputSystem.Held("Mul-GrabItemB");
			else bothArms             = InputSystem.Held("Mul-GrabItemF");

			Mulligan.verboseLevel = 0;

			//  Look for peeps and find their arm
			foreach (PhysicalBehaviour Selected in GetSelected())
			{
				PBO = Selected.GetComponentInParent<PersonBehaviour>();

				//  Check if this is an item being held by person
				if (!PBO)
				{
					if (Selected.beingHeldByGripper && !JustHeld.Contains(Selected))
					{
						foreach (FixedJoint2D TmpJoint in FindObjectsOfType<FixedJoint2D>())
						{
							if (Selected.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb2d) 
								&& TmpJoint.connectedBody == rb2d)
							{
								PBO = TmpJoint.gameObject.GetComponentInParent<PersonBehaviour>();
								break;
							}
						}
					}
					if (!PBO) continue;
				}
				bool PersonFlipped = PBO.transform.localScale.x < 0.0f;

				LowerArm = useFrontArm
							? PBO.transform.Find("FrontArm").transform.Find("LowerArmFront").GetComponent<Rigidbody2D>()
							: PBO.transform.Find("BackArm").transform.Find("LowerArm").GetComponent<Rigidbody2D>();

				AltArm   = !useFrontArm
							? PBO.transform.Find("FrontArm").transform.Find("LowerArmFront").GetComponent<Rigidbody2D>()
							: PBO.transform.Find("BackArm").transform.Find("LowerArm").GetComponent<Rigidbody2D>();

				if (!LowerArm) {
					if(Mulligan.verboseLevel >= 2) ModAPI.Notify("Cant Find Lower Arm");
					continue;
				}

				if (PeepsWithItems.Contains(LowerArm.GetInstanceID())) continue;

				PeepsWithItems.Add(LowerArm.GetInstanceID());

				GripBehaviour GB  = LowerArm.GetComponent<GripBehaviour>();
				float ArmRotation = LowerArm.rotation;

				if (bothArms && !useFrontArm)
				{
					if (Mulligan.verboseLevel >= 2) ModAPI.Notify("Grabbing with both hands");
					GripBehaviour GB2 = AltArm.GetComponent<GripBehaviour>();

					//  Make sure only one hand as an item
					if (GB.isHolding != GB2.isHolding)
					{
						PhysicalBehaviour phys3     = GB.isHolding ? GB.CurrentlyHolding : GB2.CurrentlyHolding;
						Vector3 holdingPosition;

						holdingPosition = GB.GripPosition;
						phys3.rigidbody.mass = 0.06f;

						if (LowerArm.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb))
						{
							limb.BaseStrength += 100f;
						}

						phys3.gameObject.layer = 10;
						string holdingPose = phys3.HoldingPositions.Length > 1 ? "HoldRifle" : "HoldHandgun";

						Vector3 nearestP = phys3.GetNearestLocalHoldingPoint(holdingPosition, out _);

						DoubleFisters.Add( new DoubleFist() { 
							Item            = phys3, 
							PBO             = PBO, 
							HoldingPose     = holdingPose, 
							HoldingPosition = nearestP
						} );
						CheckDoubleFist = true;

						continue;
					}
				}

				if (GB.isHolding)
				{
					//  DROP ITEM 
					PhysicalBehaviour physo = GB.CurrentlyHolding;

					noCollide = GB.transform.root.GetComponentsInChildren<Collider2D>();

					LowerArm.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

					foreach (Collider2D col1 in physo.transform.root.GetComponentsInChildren<Collider2D>())
					{
						foreach (Collider2D col2 in noCollide)
						{
							if ((bool)(UnityEngine.Object)col2 && (bool)(UnityEngine.Object)col1)
								Physics2D.IgnoreCollision(col1, col2);
						}
					}

					//  Add objects to list for re-enabling colliders
					DisabledCollisions.Add(new DisabledCollision(PBO.transform, physo));

					CheckDisabledCollisions     = true;
					//FixedJoint2D ItemjointStuck = null;
					GB.isHolding                = false;

					physo.MakeWeightful();

					//  Need to loop through these incase PickUpNearestObject() was also triggered
					//  and would've created its own joint
					//while (GB.gameObject.TryGetComponent<FixedJoint2D>(out FixedJoint2D ItemJoint))
					//{
					//    if (ItemjointStuck == ItemJoint) break; // pervert endless loop
					//    DestroyImmediate(ItemJoint);
					//    ItemjointStuck = ItemJoint;
					//}

					++peepsDropped;

					if (CustomPoses.ContainsKey(PBO.ActivePose.Name)) PBO.OverridePoseIndex = -1;

					continue;
				}

				Vector2 worldPoint        = (Vector2)GB.transform.TransformPoint(GB.GripPosition);
				Vector2 NearestHoldingPos = new Vector2(0.0f, 0.0f);

				PhysicalBehaviour phys = null;

				float num2 = float.MaxValue;

				//  loop thru objects and determine which is closest to peep
				foreach (PhysicalBehaviour physicalBehaviour in Global.main.PhysicalObjectsInWorld)
				{
					// skip items already being held
					if (!CanHold(physicalBehaviour)) continue;

					Vector2 localHoldingPoint = physicalBehaviour.GetNearestLocalHoldingPoint(worldPoint, out float distance);
					if ((double)distance < (double)num2)
					{
						num2              = distance;
						NearestHoldingPos = localHoldingPoint;
						phys              = physicalBehaviour;
					}
				}

				if (!(bool)(UnityEngine.Object)phys) return;

				bool ItemFlipped   = phys.transform.localScale.x < 0.0f;
				if (PersonFlipped != ItemFlipped) FlipSelectedItems(new List<PhysicalBehaviour> { phys });

				JustHeld.Add(phys);

				//  Check if person has attachments
				//Attachment[] attachments = FlipUtility.GetAttachments(PBO);

					//  Adjust layers of the hand and the held object so object is over body but under hand and arms
				int SOrder = LowerArm.GetComponent<SpriteRenderer>().sortingOrder;
				if (useFrontArm && SOrder <= 1)
				{
					SOrder = 2;
					LowerArm.GetComponent<SpriteRenderer>().sortingOrder = SOrder;
					PBO.transform.Find("FrontArm").transform.Find("UpperArmFront").GetComponent<SpriteRenderer>().sortingOrder = SOrder;
				}

				phys.GetComponent<SpriteRenderer>().sortingLayerName = LowerArm.GetComponent<SpriteRenderer>().sortingLayerName;
				phys.GetComponent<SpriteRenderer>().sortingOrder     = SOrder + (useFrontArm ? -1 : 1);

				foreach(Attachment attachment in FlipUtility.GetAttachments(PBO)) {
					if (useFrontArm && attachment.Parent.name == "UpperLegFront")
					{
						phys.GetComponent<SpriteRenderer>().sortingLayerName = attachment.G.GetComponentInChildren<SpriteRenderer>().sortingLayerName;
						phys.GetComponent<SpriteRenderer>().sortingOrder     = attachment.G.GetComponentInChildren<SpriteRenderer>().sortingOrder + 1;
					} else
					if ( !useFrontArm ) { 
						if (attachment.Parent.name == "LowerArm" )
						{
							phys.GetComponent<SpriteRenderer>().sortingLayerName = attachment.G.GetComponent<SpriteRenderer>().sortingLayerName;
							phys.GetComponent<SpriteRenderer>().sortingOrder     = attachment.G.GetComponent<SpriteRenderer>().sortingOrder + 1;
						}
						if (attachment.Parent.name == "UpperLeg" || attachment.Parent.name == "LowerLeg")
						{
							attachment.G.GetComponent<SpriteRenderer>().sortingOrder = 12;
						}
					}
					ToggleCollisions( attachment.T, phys.transform );
				}


				phys.transform.rotation = Quaternion.Euler(0.0f, 0.0f, PersonFlipped ? ArmRotation + 95.0f : ArmRotation - 95.0f);



				//  The majority of items seem to look better using the grippos with the lower X val when not flipped
				foreach (Vector3 HPos in phys.HoldingPositions) if (HPos.x < NearestHoldingPos.x) NearestHoldingPos = HPos;

				phys.transform.position += GB.transform.TransformPoint(GB.GripPosition) - phys.transform.TransformPoint((Vector3)NearestHoldingPos);

				LowerArm.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

				//  Disables collisions between the object and person holding it (also alt hand item).
				noCollide = GB.transform.root.GetComponentsInChildren<Collider2D>();

				if (AltArm.TryGetComponent<GripBehaviour>(out GripBehaviour GBAlt))
				{
					if (GBAlt.isHolding)
					{
						PhysicalBehaviour PBAltItem = GBAlt.CurrentlyHolding;

						foreach (Collider2D col1 in PBAltItem.transform.root.GetComponentsInChildren<Collider2D>())
						{
							foreach (Collider2D col2 in phys.transform.root.GetComponentsInChildren<Collider2D>())
							{
								if ((bool)(UnityEngine.Object)col2 && (bool)(UnityEngine.Object)col1)
									Physics2D.IgnoreCollision(col1, col2);
							}
						}
					}
				}

				++peepsEquipped;
			}

			Mulligan.verboseLevel = verbose;

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=green>Equipped: " + peepsEquipped + "</color>  <color=red>Dropped: " + peepsDropped);

		}

		public bool CanHold(PhysicalBehaviour PB)
		{
			if (PB.beingHeldByGripper || PB.TryGetComponent<FreezeBehaviour>(out _)) return false;

			bool canHoldItem = false;

			SpawnableAsset SpawnName = CatalogBehaviour.Main.GetSpawnable(PB.name);

			if(SpawnName) {
				switch (SpawnName.Category.ToString().ToLower())
				{
					case "melee (category)":
					case "firearms (category)":
					case "explosives (category)":
					case "biohazard (category)":
						canHoldItem = true;
						break;

					default:
						break;
				}
			} 

			if (!canHoldItem)
			{
				if (SelectionController.Main.SelectedObjects.Contains(PB)) canHoldItem = true;
			}

			return canHoldItem;

		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: ACTIVATE HELD ITEMS
		//
		public void ActivateHeldItems()
		{
			List<int> BeenActivated = new List<int>();
			PersonBehaviour PBO;
			foreach (PhysicalBehaviour Selected in GetSelected())
			{
				if (PBO = Selected.GetComponentInParent<PersonBehaviour>())
				{
					if (BeenActivated.Contains(PBO.GetHashCode())) continue;
					BeenActivated.Add(PBO.GetHashCode());

					//  Check if this is an item being held by person
					if (PBO) ActivateHeldItems(PBO);
				}

			}
		}


		public void ActivateHeldItems(PersonBehaviour PB)
		{
			List<Rigidbody2D> Arms = new List<Rigidbody2D>();
			Rigidbody2D LowerArm;

			LowerArm = PB.transform.Find("FrontArm").transform.Find("LowerArmFront").GetComponent<Rigidbody2D>();
			if (LowerArm) Arms.Add(LowerArm);

			LowerArm = PB.transform.Find("BackArm").Find("LowerArm").GetComponent<Rigidbody2D>();
			if (LowerArm) Arms.Add(LowerArm);

			if( Arms.Count > 0 ) { 
				foreach (Rigidbody2D Arm in Arms)
				{
					GripBehaviour GB = Arm.GetComponent<GripBehaviour>();
					if (GB.isHolding)
					{
						GB.CurrentlyHolding.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
					}
				}
			}
		}

		private void FixDisabledCollisions()
		{
			bool stillActives = false;

			if(DisabledCollisions.Count > 0)
			{ 
				for( int i = DisabledCollisions.Count; --i >= 0; ) 
				{
					if (DisabledCollisions[i].Check()) DisabledCollisions.RemoveAt(i);
					else stillActives = true;
				}
			}

			if (!stillActives) CheckDisabledCollisions = false;
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: TOGGLE COLLISIONS
		//
		public void ToggleCollisions()
		{
			int  collisionCount = 0;
			bool disableCollision;

			List<PhysicalBehaviour> PBL = GetSelected();

			//  fek it, just do the opposite of majority
			foreach (PhysicalBehaviour PBO in PBL)
			{
				collisionCount += (PBO.gameObject.TryGetComponent<LayerSerialisationBehaviour>(out LayerSerialisationBehaviour component) 
					&& component.Layer == 10) ? -1 : 1;
			}

			disableCollision = (collisionCount >= 0);

			foreach (PhysicalBehaviour PBO in PBL)
			{
				if (disableCollision)
				{
					LayerSerialisationBehaviour LSB = PBO.gameObject.AddComponent<LayerSerialisationBehaviour>();
					LSB.Layer = 10;
				} else
				{
					if (PBO.gameObject.TryGetComponent<LayerSerialisationBehaviour>(out LayerSerialisationBehaviour component))
					{
						PBO.gameObject.layer = 9;
						UnityEngine.Object.Destroy((UnityEngine.Object)component);
					}
				}
			}

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify((disableCollision ? "Collisions Disabled: " : "Collisions Enabled: ") + PBL.Count);
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: SET MOTOR SPEED
		//
		private void SetCarSpeed()
		{
			CARS4KIDS.Clear();

			float lastMotorSpeed = 0f;

			List<PhysicalBehaviour> PBL = GetSelected();

			foreach (CarBehaviour sCar in FindObjectsOfType<CarBehaviour>())
			{
				if (PBL.Contains(sCar.GetComponentInChildren<PhysicalBehaviour>()))
				{
					CARS4KIDS.Add(sCar);
					lastMotorSpeed = Math.Abs(sCar.MotorSpeed);
				}
			}

			if (CARS4KIDS.Count < 1)
			{
				if (Mulligan.verboseLevel >= 1) ModAPI.Notify("No cars found in selection!");
				return;
			}

			DialogBox dialog = null;

			dialog = DialogBoxManager.TextEntry(
				@"Set motor speed for the selected vehicle(s)",
				lastMotorSpeed.ToString(),
				new DialogButton("Set Speed", true, new UnityAction[1] { (UnityAction)(() => ApplyMotorSpeed(dialog)) }),
				new DialogButton("Cancel", true, (UnityAction[])Array.Empty<UnityAction>()));

			void ApplyMotorSpeed(DialogBox d)
			{
				string motorSpeedRaw = d.EnteredText;

				if (float.TryParse(motorSpeedRaw, out float motorSpeed))
				{
					foreach (CarBehaviour somecar in CARS4KIDS) somecar.MotorSpeed = Math.Abs(motorSpeed) * Math.Sign(somecar.MotorSpeed);
					CARS4KIDS.Clear();
				}
				else
				{
					if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Invalid MotorSpeed Value #!");
				}
			}
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: FOLLOW OBJECT
		//
		public void FollowObject()
		{
			List<PhysicalBehaviour> PBL = GetSelected();

			if (PBL.Count > 0)
				FollowObject(PBL[0]);
			else
				Global.main.CameraControlBehaviour.CurrentlyFollowing.Clear();        
		}

		public void FollowObject(PhysicalBehaviour SelectedObj)
		{

			if (Global.main.CameraControlBehaviour.CurrentlyFollowing.Contains(SelectedObj))
			{
				Global.main.CameraControlBehaviour.CurrentlyFollowing.Remove(SelectedObj);
				if (Mulligan.verboseLevel >= 2) ModAPI.Notify("Stopped Following: <color=red>" + SelectedObj.name + "</color>");
			}
			else
			{
				Global.main.CameraControlBehaviour.CurrentlyFollowing.Add(SelectedObj);
				if (Mulligan.verboseLevel >= 2) ModAPI.Notify("Following: <color=green>" + SelectedObj.name + "</color>");
			}
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: FREEZE ITEMS
		//
		public void FreezeItems()
		{

			bool doFreeze = true;
			bool firstSet = false;
			int itemCount = 0;

			foreach(PhysicalBehaviour PBO in GetSelected())
			{
				FreezeBehaviour component = PBO.GetComponent<FreezeBehaviour>();
				if (!firstSet)
				{
					firstSet = true;
					doFreeze = !(bool)(UnityEngine.Object)component;
				}
				if ((bool)(UnityEngine.Object)component)
				{
					if (!doFreeze)
					{
						UnityEngine.Object.Destroy((UnityEngine.Object)component);
						++itemCount;
					}
				}
				else
				{
					if (doFreeze)
					{
						PBO.gameObject.AddComponent<FreezeBehaviour>();
						++itemCount;
					}
				}
			}

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify((doFreeze ? "Frozen: " : "Unfrozen: ") + itemCount);

		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: HEAL PEOPLE
		//
		public void HealPeople( bool everybody=false )
		{
			int healedCount = 0;

			if (!everybody)   {

				foreach (PhysicalBehaviour SelectedObject in GetSelected()) {
					if (SelectedObject.OnFire)
					{
						SelectedObject.Extinguish();
						SelectedObject.burnIntensity = 0.0f;
						SelectedObject.BurnProgress  = 0f;
					}

					PersonBehaviour person = SelectedObject.gameObject.GetComponentInParent<PersonBehaviour>();

					if (person) {
						if (SelectedObject.OnFire)
						{
							if (!SelectedObject.isActiveAndEnabled) continue;
							SelectedObject.Extinguish();
							SelectedObject.burnIntensity = 0.0f;
							SelectedObject.BurnProgress  = 0f;

							person.GetComponentInParent<PhysicalBehaviour>().Extinguish();
							person.GetComponentInParent<PhysicalBehaviour>().burnIntensity = 0.0f;
							person.GetComponentInParent<PhysicalBehaviour>().BurnProgress  = 0f;
						}
						healedCount++;

						if (SelectedObject.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb))
						{
							HealAll(limb);
						}

					}
				}
			} else {

				PersonBehaviour[] people = FindObjectsOfType<PersonBehaviour>();

				foreach (PersonBehaviour person in people) {

					healedCount++;

					HealAll(person.Limbs);

				}

			}

			if (Mulligan.verboseLevel >= 2) ModAPI.Notify("# Healed: <color=green>" + healedCount + "</color>");

		}

		public static void HealAll(LimbBehaviour limb)
		{
			if (!limb.isActiveAndEnabled) return;

			Liquid liquid = Liquid.GetLiquid("LIFE SERUM");
			if (limb.PhysicalBehaviour.OnFire) StopDropRoll();
			limb.HealBone();
			limb.Health   = limb.InitialHealth;
			limb.Numbness = 0.0f;

			limb.CirculationBehaviour.HealBleeding();
			limb.CirculationBehaviour.IsPump            = limb.CirculationBehaviour.WasInitiallyPumping;
			limb.CirculationBehaviour.GunshotWoundCount = 0;
			limb.CirculationBehaviour.StabWoundCount    = 0;
			limb.CirculationBehaviour.BloodFlow         = 1f;
			limb.CirculationBehaviour.ForceSetAllLiquid(0f);
			limb.CirculationBehaviour.AddLiquid(limb.GetOriginalBloodType(), 1f);
			limb.CirculationBehaviour.AddLiquid(liquid, 0.1f);

			limb.BruiseCount = 0;

			limb.PhysicalBehaviour.BurnProgress     = 0.0f;
			limb.SkinMaterialHandler.AcidProgress   = 0.0f;
			limb.SkinMaterialHandler.RottenProgress = 0.0f;

			//for (int index = 0; index < limb.SkinMaterialHandler.bulletHolePoints.Length; ++index)
			//	limb.SkinMaterialHandler.bulletHolePoints[index].z  = 100f;
			for (int index = 0; index < limb.SkinMaterialHandler.damagePoints.Length; ++index)
				limb.SkinMaterialHandler.damagePoints[index].z      = 100f;

			limb.SkinMaterialHandler.Sync();

			if (limb.HasBrain)
			{
				limb.Person.Consciousness   = 1f;
				limb.Person.ShockLevel      = 0.0f;
				limb.Person.PainLevel       = 0.0f;
				limb.Person.OxygenLevel     = 1f;
				limb.Person.AdrenalineLevel = 1f;
			}

		}

		public static void HealAll(LimbBehaviour[] LB)
		{
			foreach(LimbBehaviour limb in LB ) { HealAll( limb ); };
		}

		public static void StopDropRoll()
		{
			PhysicalBehaviour[] PhB = FindObjectsOfType<PhysicalBehaviour>();
			foreach (PhysicalBehaviour pb in PhB)
			{
				pb.Extinguish();
				pb.burnIntensity = 0f;
				pb.BurnProgress  = 0f;
			}
		}


		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: DELETE SELECTED ITEMS
		//
		public void DeleteItems()
		{
			int itemCount = 0;

			if (SelectionController.Main.SelectedObjects.Count == 0)
			{
				foreach (PhysicalBehaviour PB in Global.main.PhysicalObjectsInWorld)
				{
					if(!PB.Selectable || !PB.Deletable || PB.colliders.Length == 0) continue;

					foreach (Collider2D collider in PB.colliders)
					{
						if(!collider) continue;
						if (collider.OverlapPoint((Vector2)Global.main.MousePosition))
						{
							if (PB.transform.root.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour component))
							{
								if (component.Deletable)
								{
									PB.transform.root.gameObject.SendMessage("OnUserDelete", SendMessageOptions.DontRequireReceiver);
									UnityEngine.Object.Destroy((UnityEngine.Object)PB.transform.root.gameObject);
									++itemCount;
								}
							}
							else
							{
								PB.transform.root.gameObject.SendMessage("OnUserDelete", SendMessageOptions.DontRequireReceiver);
								UnityEngine.Object.Destroy((UnityEngine.Object)PB.transform.root.gameObject);
								++itemCount;
							}

						}
					}
				}
			}
			else 
			{ 

				foreach (PhysicalBehaviour PB in SelectionController.Main.SelectedObjects)
				{
					if (PB.transform.root.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour component))
					{
						if (component.Deletable)
						{
							PB.transform.root.gameObject.SendMessage("OnUserDelete", SendMessageOptions.DontRequireReceiver);
							UnityEngine.Object.Destroy((UnityEngine.Object)PB.transform.root.gameObject);
							++itemCount;
						}
					}
					else
					{
						PB.transform.root.gameObject.SendMessage("OnUserDelete", SendMessageOptions.DontRequireReceiver);
						UnityEngine.Object.Destroy((UnityEngine.Object)PB.transform.root.gameObject);
						++itemCount;
					}
				}
			}

			SelectionController.Main.ClearSelection();

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=red>Deleted Items: </color>" + itemCount);
		}



		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: DELETE SELECTED ITEMS
		//
		public void DeleteXFItems()
		{
			int itemCount = 0;

			if (SelectionController.Main.SelectedObjects.Count == 0)
			{
				foreach (PhysicalBehaviour PB in Global.main.PhysicalObjectsInWorld)
				{
					if(!PB.Selectable || !PB.Deletable || PB.colliders.Length == 0) continue;
					if (PB.TryGetComponent<FreezeBehaviour>( out _))   continue;


					PB.transform.root.gameObject.SendMessage("OnUserDelete", SendMessageOptions.DontRequireReceiver);
					UnityEngine.Object.Destroy((UnityEngine.Object)PB.transform.root.gameObject);
					++itemCount;

				}
			}
			else 
			{ 

				foreach (PhysicalBehaviour PB in SelectionController.Main.SelectedObjects)
				{
					if (PB.TryGetComponent<FreezeBehaviour>( out _))   continue;

					if (PB.transform.root.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour component))
					{
						if (component.Deletable)
						{
							PB.transform.root.gameObject.SendMessage("OnUserDelete", SendMessageOptions.DontRequireReceiver);
							UnityEngine.Object.Destroy((UnityEngine.Object)PB.transform.root.gameObject);
							++itemCount;
						}
					}
					else
					{
						PB.transform.root.gameObject.SendMessage("OnUserDelete", SendMessageOptions.DontRequireReceiver);
						UnityEngine.Object.Destroy((UnityEngine.Object)PB.transform.root.gameObject);
						++itemCount;
					}
				}
			}

			SelectionController.Main.ClearSelection();

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=red>Deleted Unfrozen Items: </color>" + itemCount);
		}


		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: SELECT SAME ITEMS
		//
		public void SelectSame()
		{
			int itemCount      = 0;
			List<string> Items = new List<string>();

			//
			//  Build list of shtuff to select
			//
			if (Mulligan.holdingShift || SelectionController.Main.SelectedObjects.Count == 0)
			{
				foreach (PhysicalBehaviour PB in Global.main.PhysicalObjectsInWorld)
				{
					if (!PB.Selectable || PB.colliders.Length == 0) continue;

					foreach (Collider2D collider in PB.colliders)
					{
						if (!collider) continue;

						if (collider.OverlapPoint((Vector2)Global.main.MousePosition))
						{
							if (PB.transform.root.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour component))
							{
								if (component.Selectable) Items.Add(component.name);
							}
							else
							{
								Items.Add(PB.name);
							}
						}
					}
				}
			}

			if (Items.Count == 0)
			{
				foreach (PhysicalBehaviour PB in SelectionController.Main.SelectedObjects)
				{
					if (PB.transform.root.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour component))
					{
						if (component.Selectable) Items.Add(component.name);
					}
					else
					{
						Items.Add(PB.name);
					}
				}
			}

			if (Items.Count == 0)
			{
				if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=red>No items found to select</color>");
				return;
			}

			if (!Mulligan.holdingShift) SelectionController.Main.ClearSelection();

			//
			//  Select all shtuff matching list
			//
			foreach (PhysicalBehaviour PB in Global.main.PhysicalObjectsInWorld)
			{
				if (Items.Contains(PB.name))
				{
					SelectionController.Main.Select(PB, true);
					++itemCount;
				}
			}

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=green>Selected Items: </color>" + itemCount);

		}

		public void MendPeople()
		{
			int limbCount = 0;

			foreach (PhysicalBehaviour PB in GetSelected())
			{
				if (PB.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb) && limb.isActiveAndEnabled)
				{
					limb.Health = limb.InitialHealth;
					limb.CirculationBehaviour.BleedingRate  *= 0.8f;
					limb.SkinMaterialHandler.RottenProgress *= 0.8f;
					limb.HealBone();
					++limbCount;
				}
			}

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=yellow>Mended </color>" + limbCount + " <color=yellow>limbs</color>");
		}


		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: STRENGTHEN LIMBS
		//
		public void Strengthen()
		{

			int itemCount = 0;

			foreach (PhysicalBehaviour PB in GetSelected())
			{
				if (PB.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb))
				{
					limb.Vitality             *= 0.1f;
					limb.RegenerationSpeed    += 5f;
					limb.ImpactPainMultiplier *= 0.1f;
					limb.InitialHealth        += 150f;
					limb.BreakingThreshold    += 10f;
					limb.ShotDamageMultiplier *= 0.1f;
					limb.Health               += limb.InitialHealth;
					limb.BaseStrength          = Mathf.Min(15f, limb.BaseStrength + 5f);
					limb.CirculationBehaviour.HealBleeding();
					limb.Wince(10f);

					++itemCount;
				}

			}

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=green>Strength Boosted </color>" + itemCount + " <color=green>limbs</color>");
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: FLIP PERSON/ITEMS
		//
		//  Following code was based off of what was in the "ActiveHumans" mod.
		//  That mod has some of the most clever shit I've seen so far
		//
		public void FlipSelectedItems(List<PhysicalBehaviour> ItemsToFlip)
		{

			FlippingAholes.Clear();

			int countedItems  = 0;
			int countedHumans = 0;
			int countedCars   = 0;

			NoFlip.Clear();

			PersonBehaviour           PBO;
			RandomCarTextureBehaviour CAR;

			if( ItemsToFlip == null || ItemsToFlip.Count == 0) return;

			//  First flip people and cars since they may have attached objects
			foreach (PhysicalBehaviour Selected in ItemsToFlip)
			{
				int ObjectID = Selected.GetHashCode();

				if (PBO = Selected.gameObject.GetComponentInParent<PersonBehaviour>())
				{
					NoFlip.Add(ObjectID);
					if (FlipPeep(PBO)) countedHumans++;
				} 

				else if (CAR = Selected.gameObject.GetComponentInParent<RandomCarTextureBehaviour>())
				{
					NoFlip.Add(ObjectID);
					if (FlipCar(CAR)) countedCars++;
				}
			}

			foreach (PhysicalBehaviour Selected in ItemsToFlip)
			{
				if (NoFlip.Contains(Selected.GetHashCode())) continue;

				countedItems++;

				Vector3 theScale              = Selected.transform.localScale;
				theScale.x *= -1;
				Selected.transform.localScale = theScale;
			}

			if (Mulligan.verboseLevel >= 2) ModAPI.Notify(
				"<color=blue><-></color> "
				+ "<color=red>Items: "    + countedItems   + "</color> "
				+ "<color=green>Cars: "   + countedCars    + "</color> "
				+ "<color=yellow>Peeps: " + countedHumans  + "</color> ");
		}


		public bool FlipPeep(PersonBehaviour PBO)
		{
			Flipper flipper;

			int instID = PBO.GetInstanceID();

			foreach (Flipper flipperTmp in FlippingAholes)
			{
				if (flipperTmp.instanceID == instID) return false;
			}

			Rigidbody2D       head;
			Vector2           moveDif;
			PhysicalBehaviour hObj = null;
			//FixedJoint2D      Itemjoint;

			flipper = new Flipper()
			{
				PB         = PBO,
				flipScale  = PBO.transform.localScale,
				instanceID = PBO.GetInstanceID(),
				status     = 1
			};

			flipper.flipScale.x *= -1;
			FlippingAholes.Add(flipper);

			foreach (LimbBehaviour limb in flipper.PB.Limbs)
			{
				if (limb == flipper.PB.Limbs[1]) flipper.moveB = limb.transform.position;
				if (limb.HasJoint)
				{
					limb.BreakingThreshold *= 8;

					if (limb.name    != "LowerBody"
						&& limb.name != "MiddleBody"
						&& limb.name != "UpperBody"
						&& limb.name != "UpperArm"
						&& limb.name != "UpperArmFront"
						&& limb.name != "Head")
					{
						JointAngleLimits2D t     = limb.Joint.limits;
						t.min *= -1f;
						t.max *= -1f;
						limb.Joint.limits        = t;
						limb.OriginalJointLimits = new Vector2(limb.OriginalJointLimits.x * -1f, limb.OriginalJointLimits.y * -1f);
					}
				}
			}

			Transform headT = flipper.PB.transform.Find("Head");

			if (headT)
{
				head = headT.GetComponent<Rigidbody2D>();

				flipper.PB.transform.localScale = flipper.flipScale;

				foreach (LimbBehaviour limb in flipper.PB.Limbs)
{
					if (limb == flipper.PB.Limbs[1]) flipper.moveA = head.transform.position;
				}
				moveDif = flipper.moveB - flipper.moveA;
				flipper.PB.AngleOffset *= -1f;
				flipper.PB.transform.position = new Vector2(flipper.PB.transform.position.x + moveDif.x, flipper.PB.transform.position.y);

				foreach (LimbBehaviour limb in flipper.PB.Limbs) if (limb.HasJoint) limb.Broken = false;
				GripBehaviour[] grips = flipper.PB.GetComponentsInChildren<GripBehaviour>();
				bool alreadyFlipped;

				foreach (GripBehaviour grip in grips)
				{
					if (grip.isHolding)
					{
						alreadyFlipped =  (hObj && hObj == grip.CurrentlyHolding);
						hObj = grip.CurrentlyHolding;

						grip.SendMessage("Use", new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

						// Break joint
						//while (Itemjoint = grip.gameObject.GetComponent<FixedJoint2D>())
						//{
						//    //DestroyImmediate( Itemjoint);
						//}

						FixedJoint2D joint;
						Vector2 GripPoint;

						if (!alreadyFlipped) { 
							//  Flip Item
							Vector3 theScale = hObj.transform.localScale;
							theScale.x *= -1.0f;
							hObj.transform.localScale = theScale;

							//  Set new item rotation
							hObj.transform.rotation = Quaternion.Euler(
								0.0f, 0.0f,
								grip.GetComponentInParent<Rigidbody2D>().rotation + 95.0f * (flipper.PB.transform.localScale.x < 0.0f ? 1.0f : -1.0f)
							);

							//  Move item to flipped position
							GripPoint = hObj.GetNearestLocalHoldingPoint(grip.transform.TransformPoint(grip.GripPosition), out float distance);
							hObj.transform.position += grip.transform.TransformPoint(grip.GripPosition) -
								hObj.transform.TransformPoint(GripPoint);

							//  Create new joint
							//joint                 = grip.gameObject.AddComponent<FixedJoint2D>();
							//joint.connectedBody   = hObj.rigidbody;
							//joint.anchor          = (Vector2)grip.GripPosition;
							//joint.connectedAnchor = GripPoint;
							//joint.enableCollision = false;
							grip.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

							NoFlip.Add(hObj.GetHashCode());
						} else
						{
							GripPoint = hObj.GetNearestLocalHoldingPoint(grip.transform.TransformPoint(grip.GripPosition), out _);
							hObj.transform.position += grip.transform.TransformPoint(grip.GripPosition) -
								hObj.transform.TransformPoint(GripPoint);
							//  Create new joint
							grip.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
							//joint                 = grip.gameObject.AddComponent<FixedJoint2D>();
							//joint.connectedBody   = hObj.rigidbody;
							//joint.anchor          = (Vector2)grip.GripPosition;
							//joint.connectedAnchor = GripPoint;
							//joint.enableCollision = false;

							NoFlip.Add(hObj.GetHashCode());
						}


					}
				}
			}
			return true;
		}

		public bool FlipCar(RandomCarTextureBehaviour CAR)
		{
			//
			//  Obviously theres a more efficient, clever way to do this
			//  but me aint figured that out yet
			//
			//  If the car is moving, or not on a perfectly level surface,
			//  then shit gets craycray
			//
			Vector3 body      = CAR.Body.transform.position;
			Vector3 frontDoor = CAR.FrontDoor.transform.position;
			Vector3 backDoor  = CAR.BackDoor.transform.position;
			Vector3 bonnet    = CAR.Bonnet.transform.position;
			Vector3 boot      = CAR.Boot.transform.position;
			Vector3 theScale  = CAR.transform.localScale;

			NoFlip.Add(CAR.Body.GetComponent<PhysicalBehaviour>().GetHashCode());
			NoFlip.Add(CAR.FrontDoor.GetComponent<PhysicalBehaviour>().GetHashCode());
			NoFlip.Add(CAR.BackDoor.GetComponent<PhysicalBehaviour>().GetHashCode());
			NoFlip.Add(CAR.Bonnet.GetComponent<PhysicalBehaviour>().GetHashCode());
			NoFlip.Add(CAR.Boot.GetComponent<PhysicalBehaviour>().GetHashCode());

			theScale.x *= -1.0f;

			float flipMod = (theScale.x < 0.0f) ? -1.0f : 1.0f;

			CAR.transform.localScale = theScale;

			Vector3 bodyFlipped = CAR.Body.transform.position;

			float distance = body.x - frontDoor.x;
			if (Math.Abs(distance) < 1.0f)
			{
				theScale                           = CAR.FrontDoor.transform.localScale;
				theScale.x *= -1;
				CAR.FrontDoor.transform.localScale = theScale;
				CAR.FrontDoor.transform.position   = new Vector3(bodyFlipped.x - (-0.6f * flipMod), bodyFlipped.y + 0.05f);
				CAR.FrontDoor.transform.rotation   = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			}

			distance = body.x - backDoor.x;
			if (Math.Abs(distance) < 1.5f)
			{
				theScale                          = CAR.BackDoor.transform.localScale;
				theScale.x *= -1;
				CAR.BackDoor.transform.localScale = theScale;
				CAR.BackDoor.transform.position   = new Vector3(bodyFlipped.x - (1.05f * flipMod), bodyFlipped.y + 0.05f);
				CAR.BackDoor.transform.rotation   = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			}

			distance = body.x - bonnet.x;
			if (Math.Abs(distance) < 3.0f)
			{
				theScale                        = CAR.Bonnet.transform.localScale;
				theScale.x *= -1;
				CAR.Bonnet.transform.localScale = theScale;
				CAR.Bonnet.transform.position   = new Vector3(bodyFlipped.x - (-2.4f * flipMod), bodyFlipped.y + 0.1f);
				CAR.Bonnet.transform.rotation   = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			}

			distance = body.x - boot.x;
			if (Math.Abs(distance) < 3.1f)
			{
				theScale                      = CAR.Boot.transform.localScale;
				theScale.x *= -1;
				CAR.Boot.transform.localScale = theScale;
				CAR.Boot.transform.position   = new Vector3(bodyFlipped.x - (3.0f * flipMod), bodyFlipped.y + 0.2f);
				CAR.Boot.transform.rotation   = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			}

			foreach (Joint2D TireJoint in CAR.GetComponents<Joint2D>())
			{
				GameObject Tire = TireJoint.connectedBody.gameObject;

				if (Tire.name == "Wheel1")
				{
					Tire.transform.position = new Vector3(bodyFlipped.x - (2.0f * flipMod), Tire.transform.position.y);
				}
				else if (Tire.name == "Wheel2")
				{
					Tire.transform.position = new Vector3(bodyFlipped.x - (-2.2f * flipMod), Tire.transform.position.y);
				}

			}

			return true;
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: SAVE SCENE
		//
		public void SaveScene()
		{
			DialogBox dialog = null;
			dialog = DialogBoxManager.TextEntry(
				"Save to which scene #? (0-9)",
				LastSceneNumber.ToString(),
				new DialogButton("Save", true, new UnityAction[1] { (UnityAction) (() => SaveSceneNum(dialog)) }), 
				new DialogButton("Cancel", true, (UnityAction[])Array.Empty<UnityAction>()));

			void SaveSceneNum(DialogBox d)
			{
				string intext = d.EnteredText;
				if (intext == "") intext = LastSceneNumber.ToString();

				if (int.TryParse(intext, out int sceneNumber)
					&& (sceneNumber >= 0 || sceneNumber <= 9)) SceneSaveAsContraption(sceneNumber);

				else ModAPI.Notify("Invalid Scene #! Try (0-9)");
			}
		}


		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: LOAD SCENE
		//
		public void LoadScene()
		{
			DialogBox dialog = (DialogBox)null;

			dialog = DialogBoxManager.TextEntry(
				"Load which scene #? (0-9)", LastSceneNumber.ToString(),
				new DialogButton("Load", true, new UnityAction[1] { (UnityAction)(() => LoadSceneNum(dialog)) }),
				new DialogButton("Cancel", true, (UnityAction[])Array.Empty<UnityAction>()));

			void LoadSceneNum(DialogBox d)
			{
				string intext = d.EnteredText;
				if (intext == "") intext = LastSceneNumber.ToString();

				if (int.TryParse(intext, out int sceneNumber) && (sceneNumber >= 0 || sceneNumber <= 9))
					SceneLoadAsContraption(sceneNumber);

				else ModAPI.Notify("Invalid Scene #! Try (0-9)");
			}
		}


		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: ADJUST SORTING LAYER
		//
		public void AdjustSortingLayer(string layerName)
		{
			int countedItems = 0;

			foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
			{
				SelectedObject.GetComponent<SpriteRenderer>().sortingLayerName = layerName;
				countedItems++;
			}

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("SortingLayer set for <b>" + countedItems + "</b> items to " + layerName);
		}


		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: ADJUST SORTING ORDER
		//
		public void AdjustSortingOrder(int delta)
		{
			int countedItems = 0;

			foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
			{
				SelectedObject.GetComponent<SpriteRenderer>().sortingOrder += delta;
				countedItems++;
			}

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("SortingOrder set for <b>" + countedItems + "</b> items (delta: "+delta+")");
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: SET TO BACKGROUND
		//
		public void LayerBG(bool isBackground)
		{
			string msg = "background";
			if (!isBackground) msg = "foreground";

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Set Items to " + msg);

			foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
			{
				SelectedObject.gameObject.SetLayer(isBackground ? 2 : 9);
			}
		}

		public void ToggleCollisions(Transform t1, Transform t2, bool enable=false, bool goRoot=true)
		{
			Collider2D[] colSet1;
			Collider2D[] colSet2;

			if (goRoot)
			{
				colSet1 = t1.root.GetComponentsInChildren<Collider2D>();
				colSet2 = t2.root.GetComponentsInChildren<Collider2D>();
			}
			else
			{
				colSet1 = t1.GetComponentsInChildren<Collider2D>();
				colSet2 = t2.GetComponentsInChildren<Collider2D>();
			}

			foreach (Collider2D col1 in colSet1)
			{
				if (!(bool)(UnityEngine.Object) col1) continue;

				foreach (Collider2D col2 in colSet2)
				{
					if ((bool)(UnityEngine.Object) col2) 
						Physics2D.IgnoreCollision(col1,col2,!enable);
				}
			}
		}


		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: QUICK SAVE
		//
		public void QuickSave()
		{
			SortingLayersList.Clear();
			List<int> HashCheck = new List<int>();

			List<GameObject> SelectedObjects = new List<GameObject>();

			foreach (PhysicalBehaviour selectedObject in Global.main.PhysicalObjectsInWorld)
			{
				//  Also get the sorting layers so we can re-apply it
				//if (selectedObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer SR))
				//{
				//    SortingLayersList.Add(new LayeringOrder()
				//    {
				//        sortingLayerName = SR.sortingLayerName,
				//        sortingOrder = SR.sortingOrder
				//    });
				//}
				MulliganSerializationBehaviour MSB = selectedObject.gameObject.GetOrAddComponent<MulliganSerializationBehaviour>();

				MSB.Setup();

				int objHash = selectedObject.GetHashCode();

				//if (objHash == null || objHash <1) return;

				if (!HashCheck.Contains(objHash))
				{
					HashCheck.Add(objHash);

					if (selectedObject == null || selectedObject.gameObject == null) continue;;


					SelectedObjects.Add(selectedObject.gameObject);
				}
			}

			MulliganBehaviour.SavedScene = ObjectStateConverter.Convert(SelectedObjects.ToArray(), new Vector3());

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<b>Saved scene containing " + SelectedObjects.Count + "</b> items");
		}


		public void SceneSaveAsContraption(int sceneNumber = 1)
		{
			LastSceneNumber                        = sceneNumber;
			List<LayeringOrder> SortingContraption = new List<LayeringOrder>();
			List<GameObject> SelectedObjects       = new List<GameObject>();
			string ContraptionName                 = "scene_" + sceneNumber;

			foreach (PhysicalBehaviour selectedObject in Global.main.PhysicalObjectsInWorld)
			{
				//  Also get the sorting layers so we can re-apply it
				//SpriteRenderer SR = selectedObject.GetComponent<SpriteRenderer>();

				//SortingContraption.Add(new LayeringOrder()
				//{
				//    sortingLayerName = SR.sortingLayerName,
				//    sortingOrder     = SR.sortingOrder
				//});

				MulliganSerializationBehaviour MSB = selectedObject.gameObject.GetOrAddComponent<MulliganSerializationBehaviour>();

				MSB.Setup();

				SelectedObjects.Add(selectedObject.gameObject);
			}

			ObjectState[] objectStates = ObjectStateConverter.Convert(SelectedObjects.ToArray(), new Vector3());

			ContraptionSerialiser.SaveThumbnail(objectStates, ContraptionName);
			ContraptionSerialiser.SaveContraption(ContraptionName, objectStates);

			PlayerPrefs.SetString(ContraptionName, JsonConvert.SerializeObject(SortingContraption, Formatting.Indented));

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Saved Scene: <b>"+ContraptionName+"</b>");
		}


		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: SCENE RESTORE
		//
		public void QuickReset( bool preWipe = true )
		{
			if (MulliganBehaviour.SavedScene == null || MulliganBehaviour.SavedScene.Length <= 0) return;

			if (preWipe)
			{
				ClearButtonBehaviour clearer = FindObjectOfType<ClearButtonBehaviour>();
				if (clearer) clearer.ClearEverything();
			}

			UndoControllerBehaviour.RegisterAction(
				(IUndoableAction)new PasteLoadAction(
					(IEnumerable<UnityEngine.Object>)ObjectStateConverter.Convert(MulliganBehaviour.SavedScene,
					new Vector3()), "Paste"));

			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Restored Scene");

			SceneLoaded = true;
			//ApplyLayerOrdering = 10;
		}

		public void SceneLoadAsContraption(int sceneNumber = 1)
		{
			string ContraptionName = "scene_" + sceneNumber;

			string ContraptionFile      = "Contraptions/" + ContraptionName + "/" + ContraptionName;

			ContraptionMetaData myScene = new ContraptionMetaData(ContraptionName)
			{
				PathToMetadata  = ContraptionFile + ".json",
				PathToDataFile  = System.IO.Path.Combine(ContraptionFile + ".jaap"),
				PathToThumbnail = System.IO.Path.Combine(ContraptionFile + ".png")
			};

			Contraption contraption     = ContraptionSerialiser.LoadContraption(myScene);

			if (contraption == null)
			{
				if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=red>Failed to load: </color>Scene " + sceneNumber);
				return;
			}



			ClearButtonBehaviour clearer = FindObjectOfType<ClearButtonBehaviour>();
			clearer.ClearEverything();

			UndoControllerBehaviour.RegisterAction(
				(IUndoableAction)new PasteLoadAction(
					(IEnumerable<UnityEngine.Object>)ObjectStateConverter.Convert(
						contraption.ObjectStates, new Vector3()), "Paste"));



			//SortingLayersList.Clear();
			//SortingLayersList = JsonConvert.DeserializeObject<List<LayeringOrder>>(PlayerPrefs.GetString(ContraptionName));
			SceneLoaded = true;
			if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Loaded Scene: <b>" + ContraptionName + "</b>");

			//ApplyLayerOrdering = 10;
		}



		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: CYCLE VERBOSE
		//
		public void CycleVerbose()
		{
			if(++Mulligan.verboseLevel > 2) Mulligan.verboseLevel = 0;

			string level = "";

			switch (Mulligan.verboseLevel)
			{
				case 0:
					level = "OFF";
					break;

				case 1:
					level = "Minimal";
					break;

				case 2:
					level = "Maximum";
					break;

			}

			ModAPI.Notify("Mulligan Verbose Level: <color=green>" + level + "</color>");
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: SHOW INFO
		//
		public void ShowInfo()
		{
			LimbStatusViewBehaviour.Main.Limbs = new List<LimbBehaviour>();
			foreach (Component selectedObject in GetSelected())
			{
				if (selectedObject.TryGetComponent<LimbBehaviour>(out LimbBehaviour component))
					LimbStatusViewBehaviour.Main.Limbs.Add(component);
			}
			LimbStatusViewBehaviour.Main.gameObject.SetActive(true);
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: REAPPLY LAYER ORDER
		//
		public void ReapplyLayerOrder()
		{
			//  Now reapply the previous layer settings
			//
			if (SortingLayersList.Count > 0)
			{
				int i = -1;

				LayeringOrder LO;
				LayeringOrder[] TempList = SortingLayersList.ToArray();

				foreach (PhysicalBehaviour selectedObject in Global.main.PhysicalObjectsInWorld)
				{
					//  Also get the sorting layers so we can re-apply it
					SpriteRenderer SR = selectedObject.GetComponent<SpriteRenderer>();

					LO = TempList[++i];

					SR.sortingLayerName = LO.sortingLayerName;
					SR.sortingOrder     = LO.sortingOrder;
				}
			}
		}

		public class MulliganSerializationBehaviour : MonoBehaviour
		{
			public string SortingName { get; set; } = "";
			public int SortingOrder   { get; set; } = -1;
			public int Layer          { get; set; } = -1;

			private bool _manual      = false;


			private void Start()
			{
				if (Layer > -1) gameObject.SetLayer(Layer);
				if (gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer SR))
				{
					if (SortingOrder > -1)	SR.sortingOrder		= SortingOrder;
					if (SortingName != "")	SR.sortingLayerName = SortingName;
				}

				if ( !_manual )
				{
					UnityEngine.Object.Destroy((UnityEngine.Object)this);
				}
			}

			public void Setup()
			{
				_manual = true;

				if (gameObject?.layer != null)
				{
					Layer = gameObject.layer;
				}

				if (gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer SR))
				{
					if (SR.sortingOrder > -1)
					{
						SortingOrder = SR.sortingOrder;
					}

					if (SR.sortingLayerName != "")
					{
						SortingName = SR.sortingLayerName;
					}
				}

			}
		}
	}





}

