//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PuppetMaster
{
	[SkipSerialisation]
	public class Thing : MonoBehaviour
	{
		public GameObject G;
		public PhysicalBehaviour P;
		public Rigidbody2D R;
		public Transform tr;
		public Puppet Puppet;
		public PuppetGrip PG;
		public PuppetHand PH;
		public RigidSnapshot rigidSnapshot;

		public GripBehaviour PGB;

		public Rigidbody2D PuppetArm;
		public LimbBehaviour HitLimb;
		public HoldStyle HoldStyle;

		public string Name;
		public int Hash;
		public static int ThingCounter  = 0;
		public int MyId                 = 0;

		public bool canAim              = true;
		public bool canShoot            = false;
		public bool canStab             = false;
		public bool canStrike           = false;
		public bool canSlice            = false;
		public bool isAutomatic         = false;
		public bool holdToSide          = false;
		public bool hasDetails          = false;
		public bool isFlashlight        = false;
		public bool isEnergySword       = false;
		public bool isChainSaw          = false;
		public bool isSyringe           = false;
		public bool twoHands            = false;
		public bool isLodged            = false;
		public bool isShiv              = false;
		public bool isSpear             = false;
		public bool isPersistant        = false;
		public bool isThrown            = false;
		public bool doCollisionCheck    = false;
		public bool protectFromFire     = false;
		public bool canBeDamaged        = false;
		public bool doSecondHand        = false;
		public bool doNotDispose        = false;


		public AudioClip InvSFX;

		private float secondHandDelay   = 0f;

		public string HoldingMove       = "";

		private bool itemActiveOGSetting = false;


		public PhysicalBehaviour StabVictim;
		public LodgedIntoTypes LodgedIntoType;

		public float AttackDamage       = 0f;
		public float angleHold          = 0f;
		public float angleAim           = 0f;
		public float angleOffset        = 0f;
		public float size               = 0f;
		public float originalInertia    = 0f;
		public float itemLength         = 0f;

		public static Dictionary<int, Vector2> ManualPositions = new Dictionary<int, Vector2>();
		public List<int> AttackedList                          = new List<int>();


		public float ImpactForceThreshold;

		public Collider2D Collider;
		public UnityEvent Actions;
		public Collider2D[] ItemColliders;
		public Collider2D[] Collisions = new Collider2D[3];
		public int CollisionCount      = 0;

		public Vector3[] AltPositions;

		public Vector3 HoldingPosition;
		public Vector3 HoldingPositionJoust;
		public Vector3 HoldingOffset;

		public MoveSet ActionPose;

		public JointTypes JointType = JointTypes.FixedJoint;

		public bool IsFlipped => (bool)(tr.localScale.x < 0.0f);
		public bool doDestroyCheck = true;
		private string sortingLayerName;
		private int sortingOrder;

		private float facing                = 1f;
		private float throwTimeout          = 0f;
		public static float delayCollisions = 0f;


		public void Awake()
		{
			MyId = ++ThingCounter;
			G    = gameObject;
			P    = gameObject.GetComponent<PhysicalBehaviour>();
			R    = gameObject.GetComponent<Rigidbody2D>();
			tr   = P.transform;

			rigidSnapshot = new RigidSnapshot(R);

			isPersistant = false;

			Name = P.name;
			Hash = P.GetHashCode();

			originalInertia  = R.inertia;

			sortingLayerName = P.GetComponent<SpriteRenderer>().sortingLayerName;
			sortingOrder     = P.GetComponent<SpriteRenderer>().sortingOrder;

			Collider2D[] collider = new Collider2D[5];
			int colCount = R.GetAttachedColliders(collider);

			ItemColliders = new Collider2D[colCount];

			for (int i = 0; i < colCount; i++) ItemColliders[i] = collider[i];

			Array.Sort( P.HoldingPositions,(a,b) => a.x.CompareTo(b.x) );

			HoldingPosition = P.HoldingPositions[0];
			Array.Reverse(P.HoldingPositions);

			AltPositions    = P.HoldingPositions;

			GetDetails();
		}

		public void Update()
		{
			if (isPersistant) return;

			if (doCollisionCheck)
			{
				if (CheckCollisions() == LodgedIntoTypes.Nothing)
				{
					doCollisionCheck = false;
					if (G.layer == 10) G.SetLayer(9);
				}
			}
		}

		public void FixedUpdate()
		{
			if (isThrown) AnimateThrow();
		}

		public void Reset(bool nomass=true)
		{
			R.mass           = rigidSnapshot.mass;
			R.drag           = rigidSnapshot.drag;
			R.inertia        = rigidSnapshot.inertia;
			R.angularDrag    = rigidSnapshot.angularDrag;
			P.MakeWeightless();

			//R.mass          *= 0.01f;
		}

		public bool ChangeJoint(JointTypes _jointType)
		{
			if (JointType == _jointType) return true;

			Util.Notify("Changing Joint on <color=yellow>" 
							+ Name + "</color> To use: <color=yellow>" 
							+ (JointTypes)_jointType + "</color>", VerboseLevels.Full);

			if (_jointType == JointTypes.HingeJoint)
			{
				if (PGB.gameObject.TryGetComponent<FixedJoint2D>(out FixedJoint2D fj2d))
				{
					HingeJoint2D Joint    = G.AddComponent<HingeJoint2D>();
					Joint.connectedBody   = PGB.PhysicalBehaviour.rigidbody;
					Joint.anchor          = fj2d.connectedAnchor;
					Joint.connectedAnchor = Joint.anchor; //PGB.transform.TransformVector(fj2d.anchor);
					Joint.breakForce      = Utils.CalculateBreakForceForCable((AnchoredJoint2D)Joint, float.PositiveInfinity);
					Joint.enableCollision = false;
					Joint.enabled         = true;
					fj2d.enabled          = false;

					JointType             = JointTypes.HingeJoint;

					return true;
				}
			}
			else
			if (_jointType == JointTypes.FixedJoint)
			{ 
				if (G.TryGetComponent<HingeJoint2D>(out HingeJoint2D killJoint))
				{ 
					Util.Destroy((UnityEngine.Object)killJoint);
				}

				if (PH.GB.gameObject.TryGetComponent<FixedJoint2D>(out FixedJoint2D fj2d))
				{ 
					fj2d.enabled = true;
				}

				JointType = JointTypes.FixedJoint;

				if (Puppet.IsPointing) Puppet.IsPointing = false;

				return true;
			}

			return false;
		}

		public void JustThrown()
		{
			if (canStab || isSyringe)
			{
				isThrown        = true;
				facing          = IsFlipped ? 1f : -1f;
				throwTimeout    = Time.time + 30f;
				delayCollisions = Time.time + 2f;
			}
		}

		private void GrabSecondHand()
		{
			if (!twoHands)
			{
				doSecondHand = false;
				return;
			}

			Puppet.LB["UpperArm"].Broken      = false;
			Puppet.LB["UpperArmFront"].Broken = false;
			Puppet.LB["LowerArm"].Broken      = false;
			Puppet.LB["LowerArmFront"].Broken = false;

			Puppet.pauseAiming = true;

			if (HoldingMove != "") ActionPose.RunMove();

			if (Time.time < secondHandDelay) return;

			Collider2D[] colliders   = new Collider2D[5];
			ContactFilter2D filter2D = new ContactFilter2D();
			int colCount             = ItemColliders[0].OverlapCollider(filter2D, colliders);

			if (Time.time > secondHandDelay + 1f)
			{
				Puppet.pauseAiming = false;
				doSecondHand       = false;

				//Puppet.GripB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
				//Puppet.LB["LowerArm"].SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

				Puppet.PBO.OverridePoseIndex = -1;

				P.MakeWeightless();

				float thingRotation = Puppet.IsAiming ? angleAim : angleHold;

				thingRotation += angleOffset;
				tr.rotation    = Quaternion.Euler(0.0f, 0.0f, IsFlipped ? PuppetArm.rotation + thingRotation : PuppetArm.rotation - thingRotation);
				tr.position   += PGB.transform.TransformPoint(PGB.GripPosition) - tr.TransformPoint((Vector3)HoldingPosition);

				return;
			}

			for (int i = 0; i < colCount; i++)
			{
				if (colliders[i].attachedRigidbody.name == "LowerArm")
				{
					Puppet.pauseAiming = false;
					doSecondHand       = false;

					Puppet.LB["LowerArm"].SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
					Puppet.PBO.OverridePoseIndex = -1;
					P.MakeWeightless();

					float thingRotation = Puppet.IsAiming ? angleAim : angleHold;

					thingRotation += angleOffset;
					tr.rotation    = Quaternion.Euler(0.0f, 0.0f, IsFlipped ? PuppetArm.rotation + thingRotation : PuppetArm.rotation - thingRotation);
					tr.position   += PGB.transform.TransformPoint(PGB.GripPosition) - tr.TransformPoint((Vector3)HoldingPosition);

					return;
				}
			}
		}

		private void AnimateThrow()
		{
			Vector2 v    = R.velocity;
			float angle  = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

			R.MoveRotation(angle + ((angleAim - (angleOffset * facing)) * -1f));

			if (Time.time > throwTimeout) isThrown = false;
		}

		public void MakePersistant()
		{
			isPersistant = true;
			CancelInvoke();
		}

		public enum LodgedIntoTypes
		{
			Nothing,
			Person,
			Wall,
			Item,
		}

		public LodgedIntoTypes CheckCollisions()
		{
			CollisionCount             = 0;
			LodgedIntoTypes touchingType = LodgedIntoTypes.Nothing;

			List<Collider2D> col2dList = new List<Collider2D>();

			Rigidbody2D rb;

			foreach (Collider2D collider in tr.root.GetComponentsInChildren<Collider2D>())
			{
				if (!collider || collider == null) continue;

				ContactFilter2D contactFilter = new ContactFilter2D(); 
				contactFilter                 = contactFilter.NoFilter();
				CollisionCount                = (int)collider?.OverlapCollider(contactFilter, col2dList);

				if (CollisionCount > 0)
				{
					foreach (Collider2D colliderOther in col2dList)
					{
						if (!colliderOther || colliderOther == null) continue;

						rb = colliderOther.attachedRigidbody;

						if (Puppet.IsInVehicle && rb.transform.root.name == "Bicycle") continue;

						if (rb == R || rb.name == R.name) continue;
						if (Puppet.RB2.ContainsValue(rb)) continue;

						if (rb.name.Contains("Wall") || rb.name == "Root" )      touchingType = LodgedIntoTypes.Wall;
						else if (rb.GetComponentInParent<PersonBehaviour>() != null) touchingType = LodgedIntoTypes.Person;
						else touchingType = LodgedIntoTypes.Item;

						//Util.Notify("DoCollisionCheck: " + doCollisionCheck + " and Touching: " + touchingType.ToString() + " :" + rb.name);

						return touchingType;

					}
				}
			}

			doCollisionCheck = false;
			//Util.Notify("DoCollisionCheckX: " + doCollisionCheck);

			return touchingType;
		}

		public bool CheckLodged()
		{
			isLodged = false;
			bool notLodged = true;
			foreach (PhysicalBehaviour.Penetration penetration in P.penetrations)
			{
				if (penetration.Active) {
					isLodged   = true;
					StabVictim                  = penetration.Victim;
					notLodged                   = false;

					if (StabVictim.name.Contains("Wall") || StabVictim.name == "Root" ) LodgedIntoType = LodgedIntoTypes.Wall;
					else if (StabVictim.TryGetComponent<PersonBehaviour>(out _)) LodgedIntoType = LodgedIntoTypes.Person;
					else LodgedIntoType = LodgedIntoTypes.Item;
				}

			}

			if (notLodged) LodgedIntoType = LodgedIntoTypes.Nothing;

			return isLodged;
		}


		//
		// ─── COLLISIONS ────────────────────────────────────────────────────────────────
		//
		private void OnCollisionEnter2D(Collision2D coll=null)
		{
			if (isThrown) { 
				StartCoroutine(Recycler());
				isThrown = false; 
				return;
			}

			if (Puppet != null)
			{
				if (coll.gameObject.layer == 11) Util.ToggleCollisions(tr,coll.transform,false,false);
			}

			//  if AttackDamage is not set, then we're not attacking


			if (coll == null) return;

			Invoke("CheckLodged", 0.5f);

			if (AttackDamage <= 0f) return;

			LimbBehaviour theHitLimb  = (LimbBehaviour)null;
			PersonBehaviour hitperson = coll.rigidbody.GetComponentInParent<PersonBehaviour>();

			if (hitperson != null && hitperson != Puppet.PBO)
			{
				//if (AttackedList.Contains(hitperson.GetHashCode())) return;
				if (canStab)
				{
					if (coll.rigidbody.TryGetComponent<LimbBehaviour>(out LimbBehaviour limbStabbed))
					{


						//  Check if power was enough to do a finisher
						if (Puppet.Actions.attack.CurrentMove == Attack.MoveTypes.slash && canSlice)
						{


							if (AttackDamage > 30 && AttackDamage < 40)
							{
								//int health = Util.GetOverallHealth(hitperson);
								//if (health > 75) health = 75;
								if (UnityEngine.Random.Range(1, 100) > 90)
								{
									//Util.Detach(hitperson, limbStabbed);
									limbStabbed.Slice();
									limbStabbed.gameObject.layer = 11;

								}
							}
							if (!HitLimb.IsConsideredAlive) {
								HitLimb.Slice();
								HitLimb.gameObject.SetLayer(10);
								HitLimb.IsDismembered = true;
							}

						}

						else if (Puppet.Actions.attack.CurrentMove == Attack.MoveTypes.stab)
						{

							if (AttackDamage > 30 && AttackDamage < 40)
							{
								int health = Util.GetOverallHealth(hitperson);
								if (health > 75) health = 75;
								if (UnityEngine.Random.Range(1, 100) > health)
								{
									if (coll.rigidbody.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour PB))
									{
										Stabbing stabbing = new Stabbing(P, PB, (Vector2)(tr.position - PB.transform.position).normalized, coll.collider.ClosestPoint(tr.position));

										limbStabbed.Stabbed(stabbing);
										Util.Notify("Critical", VerboseLevels.Minimal);
									}
								}
							}
							HitLimb = limbStabbed;
							HitLimb.CirculationBehaviour.Cut((Vector2) coll.GetContact(0).point, UnityEngine.Random.insideUnitCircle);
							HitLimb.CirculationBehaviour.BleedingRate *= (AttackDamage * 0.02f);
							HitLimb.CirculationBehaviour.BloodLossRateMultiplier *= 0.1f;
							if (!HitLimb.IsConsideredAlive) {
								HitLimb.Slice();
								HitLimb.gameObject.SetLayer(10);
								HitLimb.IsDismembered = true;
							}


						}
					}
				}
				else
				{
					if (coll.rigidbody.TryGetComponent<LimbBehaviour>(out LimbBehaviour limbStabbed))
					{
						theHitLimb = limbStabbed;
						//  Check if power was enough to do a finisher
						if (Puppet.Actions.attack.CurrentMove == Attack.MoveTypes.slash)
						{
							if (AttackDamage > 30 && AttackDamage < 40)
							{
								int health = Util.GetOverallHealth(hitperson);
								if (health > 75) health = 75;
								if (UnityEngine.Random.Range(1, 100) > health)
								{
									//Util.Detach(hitperson, limbStabbed);
									limbStabbed.Crush();
									Util.Notify("Critical", VerboseLevels.Minimal);
								}
							}
						}

						else if (Puppet.Actions.attack.CurrentMove == Attack.MoveTypes.stab)
						{
							if (AttackDamage > 30 && AttackDamage < 40)
							{
								int health = Util.GetOverallHealth(hitperson);
								if (health > 75) health = 75;
								if (UnityEngine.Random.Range(1, 100) > health)
								{
									if (coll.rigidbody.TryGetComponent<PhysicalBehaviour>(out _))
									{
										limbStabbed.BreakBone();
										Util.Notify("Critical", VerboseLevels.Minimal);
									}
								}
							}

						}
						if (!HitLimb.IsConsideredAlive) HitLimb.Crush();
					}
				}

				AttackedList.Add(hitperson.GetHashCode());
				hitperson.Consciousness   = 1f;
				hitperson.ShockLevel      = 0.0f;
				hitperson.PainLevel       = 0.0f;
				hitperson.OxygenLevel     = 1f;
				hitperson.AdrenalineLevel = 1f;

				if (theHitLimb != null)
				{
					if (theHitLimb.Health  < theHitLimb.InitialHealth)  theHitLimb.Health += (theHitLimb.InitialHealth - theHitLimb.Health) / 2f;
					if (theHitLimb.BruiseCount > 2) theHitLimb.BruiseCount--;
				}
			}
		}







		//
		// ─── DETACH ────────────────────────────────────────────────────────────────
		//
		public void Dropped()
		{
			if (Puppet != null)
			{
				Puppet     = null;
				PGB = null;

				LayerFix(false);
			}
			Util.ToggleWallCollisions(tr,true,true);

			P.MakeWeightful();
			//if (!isPersistant && !doNotDispose) Dispose(true);

		}

		//
		// ─── ATTACH PUPPET HAND ────────────────────────────────────────────────────────────────
		//
		public bool SetHand(PuppetGrip pg, PuppetHand hand)
		{
			if (!hasDetails) GetDetails();

			Util.ToggleWallCollisions(tr, false);

			P.MakeWeightless();

			Puppet          = pg.Puppet;
			PG              = pg;
			PH              = hand;
			hand.Thing      = this;






			if (protectFromFire != Puppet.FireProof) { 
				Puppet.RunLimbs(Puppet.LimbFireProof, protectFromFire);
				Puppet.RunUpdate = true;
				Puppet.FireProof = protectFromFire;
			}

			if ( hand.Thing != PG.GetAltHand( hand ).Thing )
			{
				if (InvSFX != null) { 
					AudioSource AudioSFX1;
					AudioSFX1 = G.GetOrAddComponent<AudioSource>();
					AudioSFX1.volume = 1.0f;
					AudioSFX1.transform.SetParent(tr);
					AudioSFX1.transform.position = tr.position;
					AudioSFX1.PlayOneShot(InvSFX);
				}
			}

			return true;

		}



		public void LayerFix(bool inHand)
		{
			if (P == null) return;
			P.GetComponent<SpriteRenderer>().sortingLayerName = sortingLayerName;
			P.GetComponent<SpriteRenderer>().sortingOrder     = sortingOrder;
		}



		//
		// ─── FLIP ────────────────────────────────────────────────────────────────
		//
		public void Flip()
		{
			Vector3 scale = tr.localScale;
			scale.x      *= -1;
			tr.localScale = scale;
		}


		//
		// ─── ACTIVATE ────────────────────────────────────────────────────────────────
		//
		public void Activate(bool continuous=false)
		{
			P.SendMessage(continuous ? "UseContinuous" : "Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
		}






		public void TurnItemOff()
		{
			if (isEnergySword && P.TryGetComponent<EnergySwordBehaviour>(out EnergySwordBehaviour esb))
			{
				itemActiveOGSetting = esb.Activated;
				if (esb.Activated) Activate();
			}
		}

		public void TurnItemOn(bool ifWasOn = false)
		{
			if (ifWasOn && !itemActiveOGSetting) return;

			if (isEnergySword && P.TryGetComponent<EnergySwordBehaviour>(out EnergySwordBehaviour esb))
			{

				if (!esb.Activated) Activate();
			}
		}

		public void DisableSelfDamage(bool option)
		{
			if (canBeDamaged && P.TryGetComponent<DamagableMachineryBehaviour>(out DamagableMachineryBehaviour dmb))
			{
				dmb.enabled = option;
			}
		}

		//
		// ─── GET DETAILS ────────────────────────────────────────────────────────────────
		//
		public void GetDetails()
		{
			const float SmallShivLength = 1.25f;

			string itemName = P.name.ToLower();


			hasDetails = true;

			//  Loop through items components and check for behaviour classes that identify if its auto or manual

			string AutoFire = @"ACCELERATORGUNBEHAVIOUR,FLAMETHROWERBEHAVIOUR,PHYSICSGUNBEHAVIOUR,MINIGUNBEHAVIOUR,TEMPERATURERAYGUNBEHAVIOUR";

			string ManualFire   = @"ARCHELIXCASTERBEHAVIOUR,BEAMFORMERBEHAVIOUR,ROCKETLAUNCHERBEHAVIOUR,GENERICSCIFIWEAPON40BEHAVIOUR,
									LIGHTNINGGUNBEHAVIOUR,PULSEDRUMBEHAVIOUR,HEALTHGUNBEHAVIOUR,FREEZEGUNBEHAVIOUR,SINGLEFLOODLIGHTBEHAVIOUR";

			string FireProtection = @"ENERGYSWORDBEHAVIOUR,FLAMETHROWERBEHAVIOUR";

			string Damageable = @"ENERGYSWORDBEHAVIOUR";
			string EnergySword = @"ENERGYSWORDBEHAVIOUR";

			MonoBehaviour[] Components = P.GetComponents<MonoBehaviour>();

			if (Components.Length > 0)
			{
				for (int i = Components.Length; --i >= 0;)
				{
					string compo = Components[i].GetType().ToString().ToUpper();

					if (Damageable.Contains(compo)) canBeDamaged        = true;
					if (EnergySword.Contains(compo)) isEnergySword      = true;
					if (FireProtection.Contains(compo)) protectFromFire = true;

					if (AutoFire.Contains(compo))
					{
						canShoot    = true;
						isAutomatic = true;
					}

					if (ManualFire.Contains(compo))
					{
						canShoot    = true;
						isAutomatic = false;
					}
				}
			}

			CanShoot[] ShootComponents = P.GetComponents<CanShoot>();

			if ( ShootComponents.Length > 0 )
			{
				canShoot    = true;
			}

			//  @Todo: need to return modified values to their OG after they're dropped

			//  Determine if we can do auto firing

			if (P.TryGetComponent(out FirearmBehaviour FBH))
			{
				canShoot             = true;
				isAutomatic          = FBH.Automatic;
				FBH.Cartridge.Recoil = 0.1f;

				InvSFX = isAutomatic ? JTMain.InvRifle : JTMain.InvGun;

				if (P.name.ToLower().Contains("shot")) InvSFX = JTMain.InvShotgun;
			}
			else if (P.TryGetComponent(out ProjectileLauncherBehaviour PLB))
			{
				canShoot             = true;
				isAutomatic          = PLB.IsAutomatic;
				PLB.recoilMultiplier = 0.1f;
			}
			else if (P.TryGetComponent(out BlasterBehaviour BB))
			{
				canShoot             = true;
				isAutomatic          = BB.Automatic;
				BB.Recoil            = 0.1f;
			}
			else if (P.TryGetComponent(out BeamformerBehaviour BB2))
			{
				BB2.RecoilForce      = 0.1f;
			}
			else if (P.TryGetComponent(out GenericScifiWeapon40Behaviour GWB))
			{
				GWB.RecoilForce = 0.1f;
			}
			//else if (P.TryGetComponent(out AcceleratorGunBehaviour AB))
			//{
			//    AB.RecoilIntensity = 0.1f;
			//    canShoot = true;
			//    isAutomatic = true;
			//}
			//else if (P.TryGetComponent<ArchelixCasterBehaviour>(out _))
			//{
			//    canShoot    = true;
			//    isAutomatic = false;
			//}
			else if (P.TryGetComponent<SingleFloodlightBehaviour>(out _))
			{
				isFlashlight = true;
				angleHold    = 95f;
				angleAim     = 180f;
				canAim       = true;
			} 

			//  Flag Larger Items since they interfere with walking
			holdToSide = isFlashlight;// || (canShoot && P.ObjectArea > LargeItemLength);
			size       = Mathf.Max(tr.lossyScale.x, tr.lossyScale.y);

			//  Check for clubs/bats
			if (!canShoot)
			{
				canStab     = (P.Properties.Sharp && P.StabCausesWound) || isSyringe;
				canStrike   = true;
				//twoHands    = size > 1.1f;
				canSlice    = P.TryGetComponent<SharpOnAllSidesBehaviour>(out _);

				if (canStab) {
					if (P.ObjectArea < SmallShivLength) isShiv = true;
				}

				if (canSlice) InvSFX = JTMain.InvSword;
				if (isShiv)   InvSFX = JTMain.InvKnife;

			} else
			{
				canAim      = true;
			}

			if (itemName.Contains("syringe"))
			{
				angleOffset = -95f;
				isSyringe   = true;
				isShiv      = true;

				InvSFX = (AudioClip)null;
			}
			else if (itemName.Contains("crystal"))
			{
				angleOffset = -95f;
				InvSFX = (AudioClip)null;
			}
			else if (itemName.Contains("bulb"))
			{
				isShiv      = true;
				angleOffset = 180f;
				InvSFX = (AudioClip)null;
			}
			else if (itemName.Contains("stick"))
			{
				canStrike    = true;
				angleOffset  = 180f;
				InvSFX = (AudioClip)null;
			}
			else if (itemName.Contains("rod"))
			{
				canStrike   = true;
				angleOffset = 180f;
				InvSFX = (AudioClip)null;
			}
			else if (itemName.Contains("bolt"))
			{
				isShiv      = true;
				angleOffset = -90f;
				InvSFX = (AudioClip)null;
			}

			if (P.TryGetComponent<ChainsawBehaviour>(out _))
			{
				isChainSaw  = true;
				twoHands    = true;
				HoldingMove = "chainsaw";
				canStab     = false;
				canSlice    = false;
				canStrike   = false;
				InvSFX = (AudioClip)null;
			}

			if (HoldingMove != "")
			{
				ActionPose                                  = new MoveSet(HoldingMove, false);
				ActionPose.Ragdoll.Rigidity                 = 2.3f;
				ActionPose.Ragdoll.AnimationSpeedMultiplier = 2.5f;
				ActionPose.Ragdoll.UprightForceMultiplier   = 1f;
				ActionPose.Import();
			}

		}




		protected virtual void Dispose(bool disposing)
		{
			Destroy(false);
		}

		public void Destroy(bool deleteThing=true)
		{
			if (Puppet != null) Puppet?.PG.Drop(this);

			if (deleteThing && (bool)P?.Deletable)
			{
				G.SendMessage("OnUserDelete", SendMessageOptions.DontRequireReceiver);

				UnityEngine.Object.Destroy((UnityEngine.Object)G);
			}

			Util.Destroy(this);

		}

		IEnumerator Recycler()
		{
			int counter = 0;
			for (; ; )
			{
				if (++counter > 15)	{
					Util.Notify("Recycling Dropped Item: <color=orange>" + P.name + "</color>", VerboseLevels.Full);
					Destroy(true);
				}
				yield return new WaitForSeconds(2);
			}
		}




	}
}
