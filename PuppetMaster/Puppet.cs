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
using System.Linq;
using UnityEngine;

namespace PuppetMaster
{
	[SkipSerialisation]
	public class Puppet : MonoBehaviour
	{
		public Dictionary<string, LimbPoseSnapshot> PoseSnapshotsx = new Dictionary<string, LimbPoseSnapshot>();

		public struct LimbPoseSnapshot
		{
			public Dictionary<string, RagdollPose.LimbPose> DLimbs;
		}

		private bool _isActive               = false;
		private bool _isWalking              = false;

		public bool IsWalking
		{
			get { return _isWalking; }
			set { 
				_isWalking = value; 
				if (!value) PBO.DesiredWalkingDirection = 0f;
			}
		}
		public bool IsSwinging               = false;
		public bool IsCrouching              = false;
		public bool IsFiring                 = false;
		public bool IsHero                   = false;
		public bool IsHeroAiming             = false;
		public bool JumpLocked               = false;
		public bool IsEmote                  = false;
		public bool FacingLeft               = false;
		public bool BlockMoves               = false;
		public bool checkFlipColliders       = false;
		public bool pauseAiming              = false;
		public bool resetFlipControl         = false;
		public bool RunUpdate                = false;
		public bool CanHeroAim               = false;
		public bool HeroMode                 = false;
		public bool hasFlipped               = false;
		public bool GetUp                    = false;
		public bool ChaseVehicle             = true;

		public List<RagdollPose> PoseSnapshots = new List<RagdollPose>();

		private bool blockFlip               = false;

		private bool _hasTouchedGround       = true;

		public bool HasTouchedGround { 
			get { return _hasTouchedGround; } 
			set { 
				_hasTouchedGround = value; 
				if (value) PuppetMaster.ChaseCam.FreezeY = false;
			}
		} 

		public Hero hero;

		private readonly Liquid LifeLiquid   = Liquid.GetLiquid("LIFE SERUM");

		public bool IsInvincible
		{
			get { return _isInvincible; }
			set { 
				_isInvincible = value; 
				RunUpdate     = value; 
				if (value && !LB["Head"].IsAndroid) RunLimbs(LimbStrenthen);
				else if (!value && !LB["Head"].IsAndroid) {
					RunLimbs(LimbCure);
					//RunLimbs(LimbReset);
					RunRigids(RigidReset);
				}
			}
		}

		private bool _isInvincible = false;

		public bool IsAiming
		{
			get { return _IsAiming; }
			set { 
				_IsAiming = value;
				if (value == false) CheckMouseClickStatus();
			}
		}

		private bool _IsAiming   = false;
		private bool _IsPointing = false;

		public bool IsPointing
		{
			get { return _IsPointing; }
			set { 
				_IsPointing = value; 
				if (value == false ) CheckMouseClickStatus();
			}
		}

		public bool IsActive
		{
			get {  return _isActive; }
			set
			{
				_isActive = value;

				if ( !_isActive )
				{
					RunLimbs(LimbReset);
					RunRigids(RigidReset);
					if (PG)
					{
						PG.AimMode = AimModes.Off;
						Actions.ClearActions();
						IsAiming = IsCrouching = IsFiring = IsHero = IsHeroAiming = IsInVehicle = IsInvincible = IsPointing = IsSwinging = IsWalking = false;
					}
				}
			}
		}


		public bool IsInVehicle
		{
			get { return _IsInVehicle; }
			set { 
				_IsInVehicle = value; 
				Inventory.AllowedTypes  = _IsInVehicle 
										? ItemTypes.VehicleSafe 
										: ItemTypes.All;

				PuppetMaster.ChaseCam.ChaseMode = value ? ChaseModes.Vehicle : ChaseModes.Puppet;

				}
		}
		private bool _IsInVehicle = false;


		public int EmoteId                   = -1;

		public float Facing                  = 1f;
		public float TotalWeight             = 0;
		public float DisableMoves            = 0;

		public PersonBehaviour PBO           = null;
		public Inventory Inventory;
		public Actions Actions               = null;
		public Collider2D[] Colliders        = null;

		public PuppetGrip PG;

		public KeyCode IgnoreKey             = KeyCode.None;
		public bool FireProof                = false;

		public Dictionary<string, int> PuppetPose     = new Dictionary<string, int>();
		public Dictionary<string, Rigidbody2D> RB2    = new Dictionary<string, Rigidbody2D>();
		public Dictionary<string, LimbBehaviour> LB   = new Dictionary<string, LimbBehaviour>();
		private readonly Dictionary<string, LimbSnapshot> LimbOriginals   = new Dictionary<string, LimbSnapshot>();
		private readonly Dictionary<string, RigidSnapshot> RigidOriginals = new Dictionary<string, RigidSnapshot>();

		public List<Collider2D> FlipColliders = new List<Collider2D>();
		public CustomParts customParts        = new CustomParts();


		public bool IsReady    => (bool)(PuppetMaster.Master.Activated 
													&& PBO.isActiveAndEnabled 
													&& PBO.IsAlive() 
													&& PBO.Consciousness >= 1 
													&& PBO.ShockLevel < 0.3f );

		public bool IsUpright  => (bool)(!LB["LowerBody"].IsOnFloor
													&& !LB["UpperBody"].IsOnFloor
													&& !LB["Head"].IsOnFloor
													&& LB["FootFront"].IsOnFloor
													&& LB["Foot"].IsOnFloor)
													&& !IsInVehicle;

		public bool CanWalk    => (bool)(IsWalking || (PBO.IsTouchingFloor 
													&& !IsCrouching
													&& !KB.Up 
													&& !IsInVehicle //&& Actions.attack.state == AttackState.ready
													&& Actions.prone.state == ProneState.ready));

		public bool CanSwing   => (bool) (IsSwinging || (!PBO.IsTouchingFloor 
												&& Mathf.Abs(RB2["Head"].velocity.y) < Mathf.Abs(RB2["Head"].velocity.x)) 
												&& !IsWalking 
												&& Actions.prone.state == ProneState.ready
												&& !IsCrouching
												&& Actions.jump.State == JumpState.ready 
												&& !IsInVehicle
												&& Mathf.Abs(Vector2.Distance(Util.FindFloor(LB["Foot"].transform.position ),LB["Foot"].transform.position)) > 5f);

		public bool CanAttack       => (bool)(!IsInVehicle && Actions.jump.State == JumpState.ready);
		public bool IsFlipped       => (bool)(PBO.transform.localScale.x < 0.0f);
		public bool DisabledMoves   => (bool)(IsInVehicle || BlockMoves || DisableMoves > Time.time);
		public bool SpecialMode     => (bool)(IsAiming || IsPointing || Actions.CombatMode );

		public Hands Hand           => KB.Control ? Hands.Back : Hands.Front;

		public Thing HandThing      => Hand == Hands.Front ? PG.FH.Thing : PG.BH.Thing;


		//
		// ─── INITIALIZE PUPPET ────────────────────────────────────────────────────────────────
		//
		public void Init(PersonBehaviour _pbo)
		{
			TotalWeight       = 0f;
			PBO               = _pbo;

			//  Cache Rigidbody Maps
			Rigidbody2D[] RBs = PBO.transform.GetComponentsInChildren<Rigidbody2D>();

			foreach (Rigidbody2D rb in RBs)
			{

				//  Take snapshots of the current values for drag, intertia & mass
				//  Since these are modified during actions, we can set it back proper
				//
				RB2.Add(rb.name, rb);

				RigidSnapshot RBOG = new RigidSnapshot()
				{
					drag    = rb.drag,
					inertia = rb.inertia,
					mass    = rb.mass,
				};

				RigidOriginals.Add(rb.name, RBOG);

				TotalWeight += rb.mass;
			}

			SetupLimbs(true);

			if (PBO.OverridePoseIndex != (int)PoseState.Rest || PBO.OverridePoseIndex != (int)PoseState.Sitting)
			{
				PBO.OverridePoseIndex = -1;
			}

			Actions        = new Actions(this);
			FacingLeft     = IsFlipped;
			Facing         = FacingLeft ? 1f : -1f;

			Colliders      = PBO.transform.root.GetComponentsInChildren<Collider2D>(); 

			Util.Notify("Puppet set: <color=yellow>" + PBO.name + "</color>", VerboseLevels.Minimal);

			if (!JTMain.Notified[0])
			{
				JTMain.Notified[0] = true;
				Util.Notify("Hold [<color=yellow>SHIFT</color>] and double click items to hold with <color=green>Front</color> hand", VerboseLevels.Minimal);
				Util.Notify("Hold [<color=yellow>CTRL</color>]  and double click items to hold with <color=green>Back</color>  hand", VerboseLevels.Minimal);
			}

			PuppetMaster.Puppet = this;
			PuppetMaster.StartChaseCam();


			//  Find any custom attached armor/clothes/shtuff connected to puppet
			CheckAttachments();

			if (Hero.HeroCheck()) {
				Util.Notify("<color=red>A <color=orange>hero<color=red> was found!</color>", VerboseLevels.Minimal);
				IsHero = true;
				if (hero == null) hero = new Hero();
				hero.LoadHero();
			}

			SavePoses();

			Inventory = PBO.gameObject.GetOrAddComponent<Inventory>() as Inventory;
			Inventory.Puppet = this;
		}

		public void Reset()
		{
			CheckAttachments();
			SetupLimbs();
			ResetPoses();
			PG.enabled = true;
		}

		public PuppetGrip ResetPG()
		{
			if ( PBO.TryGetComponent<PuppetGrip>( out PuppetGrip _pg ) )
			{
				UnityEngine.Object.DestroyImmediate( _pg );
				this.PG = PBO.gameObject.AddComponent<PuppetGrip>() as PuppetGrip;
				PG.Puppet = this;
			}

			return this.PG;
		}


		public void SetupLimbs(bool init=false)
		{
			//  Cache LimbBeheaviour Maps
			LimbBehaviour[] LBs = PBO.GetComponentsInChildren<LimbBehaviour>();

			LB.Clear();
			LimbOriginals.Clear();

			foreach (LimbBehaviour limb in LBs)
			{
				LB.Add(limb.name, limb);

				LimbSnapshot LBOG = new LimbSnapshot()
				{
					BaseStrength      = limb.BaseStrength,
					BreakingThreshold = limb.BreakingThreshold,
					Broken            = limb.Broken,
					BruiseCount       = limb.BruiseCount,
					FakeUprightForce  = limb.FakeUprightForce,
					Frozen            = limb.Frozen,
					Health            = limb.Health,
					Numbness          = limb.Numbness,
					RegenerationSpeed = limb.RegenerationSpeed,
					Vitality          = limb.Vitality,
					Properties        = limb.GetComponent<PhysicalBehaviour>().Properties.ShallowClone(),
				};

				if (limb.HasJoint)
				{
					LBOG.jLimitMax = limb.Joint.limits.max;
					LBOG.jLimitMin = limb.Joint.limits.min;
				}

				LimbOriginals.Add(limb.name, LBOG);

				if (init)
				{
					Hero.SetupLimbButton(limb);
				}

				if (limb.name.Contains("ArmFront"))
				{
					limb.PhysicalBehaviour.spriteRenderer.sortingOrder = Math.Max( limb.PhysicalBehaviour.spriteRenderer.sortingOrder, 5 );
				}

			}

			RunRigids(RigidReset);


			//  Add PuppetGrip
			PG = PBO.gameObject.GetOrAddComponent<PuppetGrip>();
			PG.Puppet = this;
		}


		//
		// ─── UNITY FIXED UPDATE ────────────────────────────────────────────────────────────────
		//
		private void FixedUpdate()
		{

			if (!IsActive) return;

			//if (!IsInvincible) IsInvincible = true;

			if (Time.frameCount % 100 == 0) 
			{
				if (PBO == null) 
				{ 
					IsActive = false;
					PuppetMaster.ActivePuppet = 0;
					return;
				}

				//if (resetFlipControl && !KB.Left && !KB.Right && (bool)!PG.IsLodged) {

				//    resetFlipControl = false;

				//}
			}

			Actions.RunActions();

			//if (FlipState != FlipStates.ready) RunFlip();

			if (IsPointing)
			{
				if (IsReady) PointWeapon(PG.FH);
			}

			if (IsWalking)
			{
				if (KB.Alt && KB.Left && !FacingLeft && (Time.time - KB.KeyTimes.Left > 0.5f) && (Time.time - KB.KeyTimes.Left < 1.0f) && Flip())        FacingLeft = true;
				else if (KB.Alt && KB.Right && FacingLeft && (Time.time - KB.KeyTimes.Right > 0.5f) && (Time.time - KB.KeyTimes.Right < 1.0f) && Flip())  FacingLeft = false;
				else if (!KB.Right && !KB.Left)
				{
					IsWalking = false;
					StopPerson();
				}
			}


			if (checkFlipColliders && Time.frameCount % 5 == 0) CheckClearedCollisions();

			if (GetUp) {
				GetUp = false;
				RunLimbs(LimbUp,1.5f);
				if (!GetUp)
				{
					PBO.LinkedPoses[PoseState.Rest].ShouldStumble = true;
				}
			}
		}


		//
		// ─── UNITY UPDATE ────────────────────────────────────────────────────────────────
		//
		private void Update()
		{
			if (!IsActive) return;

			if (!RunUpdate) return;

			RunUpdate = false;
			if (FireProof)
			{
				RunUpdate           = true;
				PBO.PainLevel       = 0.0f;
				PBO.ShockLevel      = 0.0f;
				PBO.AdrenalineLevel = 1.0f;
			}

			if (IsInvincible)
			{
				RunUpdate           = true;
				PBO.PainLevel       = 0.0f;
				PBO.ShockLevel      = 0.0f;
				PBO.AdrenalineLevel = 1.0f;

				RunLimbs(LimbGangster);
			}
		}

		private void LateUpdate()
		{
			if (IsHero && HeroMode) hero.HeroAimRun();
		}

		public void SavePoses()
		{
			PoseSnapshotsx.Clear();
			for ( int i = PBO.Poses.Count; --i >= 1; )
			{
				RagdollPose Pose = PBO.Poses[i];

				if ( Pose == null || Pose.Name == null || Pose.AngleDictionary == null || Pose.AngleDictionary.Count == 0) continue;

				//foreach ( KeyValuePair<LimbBehaviour, RagdollPose.LimbPose> pair in PBO.Poses[i].AngleDictionary )
				//{

				//}

				LimbPoseSnapshot LPS = new LimbPoseSnapshot();

				LPS.DLimbs = new Dictionary<string, RagdollPose.LimbPose>();

				foreach ( KeyValuePair<LimbBehaviour, RagdollPose.LimbPose> pair in Pose.AngleDictionary )
				{
					if ( pair.Key.name.Contains( "Arm" ) )
					{
						LPS.DLimbs.Add(pair.Key.name, pair.Value);
					}
				}

				PoseSnapshotsx.Add(Pose.Name, LPS);
			}

			//foreach ( KeyValuePair<PoseState, RagdollPose> pair in PBO.LinkedPoses )
			//{
			//    PBO.LinkedPoses[pair.Key].ShouldStumble = false;
			//}

		}

		public void ResetPoses()
		{
			ResetPoses(LB["LowerArm"], LB["UpperArm"], LB["LowerArmFront"], LB["UpperArmFront"]);

		}

		public void ResetPoses( params LimbBehaviour[] limbs )
		{
			string poseName;
			for ( int i = PBO.Poses.Count; --i >= 1; )
			{
				poseName = PBO.Poses[i].Name;
				if ( PoseSnapshotsx.ContainsKey( poseName ) )
				{
					foreach ( LimbBehaviour limb in limbs )
					{
						if ( PBO.Poses[i].AngleDictionary.ContainsKey( limb ) )
							PBO.Poses[i].AngleDictionary[limb] = PoseSnapshotsx[poseName].DLimbs[limb.name];
					}
				}
			}
		}




		public void CheckAttachments()
		{
			foreach (FixedJoint2D fJoint in UnityEngine.Object.FindObjectsOfType<FixedJoint2D>())
			{
				if (RB2.Values.Contains(fJoint.connectedBody))
				{
					if ( fJoint.gameObject.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour pb ) )
					{
						if (pb.beingHeldByGripper) continue;
					}

					if (customParts.Add(fJoint.gameObject, fJoint.connectedBody))
					{
						fJoint.attachedRigidbody.mass *= 0.01f;
						Util.Notify("Found attached: " + fJoint.gameObject.name, VerboseLevels.Full);
					}
				}
			}
		}

		public void SetHeroAttack(int attackId, string limbName)
		{
			if (hero == null) hero = new Hero();

			hero.HeroSetup(attackId, limbName);
		}


		//
		// ─── FLIP ────────────────────────────────────────────────────────────────
		//
		public bool Flip(bool forced=false)
		{
			if (!forced) { 
				if (
					blockFlip ||
					DisabledMoves || 
					!IsUpright || 
					(KB.Left && KB.Right) || 
					Actions.jump.State != JumpState.ready || 
					Actions.backflip.State != BackflipState.ready ||
					!PBO.isActiveAndEnabled || 
					!PBO.IsAlive() || 
					!PBO.IsTouchingFloor || 
					PBO.Consciousness < 1) return false;

				if ((PG.IsLodged) || resetFlipControl) {
					PBO.DesiredWalkingDirection = 0;
					resetFlipControl = true;
					return false;
				}
			}

			StartCoroutine("FlipPuppet");

			return true;
		}

		IEnumerator FlipPuppet()
		{
			RunRigids(RigidStop);
			PBO.DesiredWalkingDirection = 0;

			yield return new WaitForEndOfFrame();

			FlipUtility.FlipInit(LB["Head"].PhysicalBehaviour);

			while ( FlipUtility.FlipState != FlipStates.ready )
				yield return new WaitForEndOfFrame();

			FacingLeft = IsFlipped;
			Facing     = FacingLeft ? 1f : -1f;
			yield return null;
		}

		public bool CheckClearedCollisions()
		{
			Collider2D tmpCol;

			for (int i = FlipColliders.Count; --i >= 0;)
			{
				Collider2D coll = (Collider2D)FlipColliders[i];

				if (coll == null)
				{
					FlipColliders.RemoveAt(i);
					continue;
				}
				if (tmpCol = Physics2D.OverlapBox(coll.transform.position, (Vector2)coll.bounds.size, 0))
				{
					PersonBehaviour hitperson = tmpCol.attachedRigidbody.GetComponentInParent<PersonBehaviour>();
					if (hitperson == null || hitperson == PBO)
					{
						coll.enabled = true;
						FlipColliders.RemoveAt(i);
					}
				}
			}

			checkFlipColliders = FlipColliders.Count > 0;
			return checkFlipColliders;
		}

		//
		// ─── CHECK CONNTROLS ────────────────────────────────────────────────────────────────
		//
		public void CheckControls()
		{
			if (!IsActive) return;
			if (!KB.ActionHeld && !KB.MouseDown) IsFiring = false;

			if (KB.Inventory && !Inventory.InventoryWatch) Inventory.InventoryWatch = true;

			if (KB.Activate && PG.IsHolding)
			{
				//  ---
				//  Activate Items
				//  ---
				if (KB.Control && PG.BH.IsHolding) PG.BH.Thing.Activate(PG.BH.Thing.isAutomatic);
				else if (PG.FH.IsHolding) PG.FH.Thing.Activate(PG.FH.Thing.isAutomatic);
			}

			if(KB.Action2 || KB.Action || ( (PG.CanAttack || IsPointing) && (KB.MouseDown || KB.Mouse2Down)))
			{
				//  ---
				//  Action Keys
				//  ---
				PuppetHand attackHand = PG.GetAttackHand();

				if (attackHand != null) { 
					if (attackHand.Thing.isChainSaw)
					{
						attackHand.Thing.Activate(true);
					}
					if (attackHand.Thing.canStrike ) {

						if (attackHand.Thing.isLodged)
						{
							// Sword or knife is stuck in enemy
							if (KB.Left || KB.Right)
							{
								if (KB.Left == FacingLeft && KB.Right != FacingLeft) Actions.attack.Init(Attack.AttackIds.dislodgeKick, attackHand);
								else Actions.attack.Init(Attack.AttackIds.dislodgeBack, attackHand);
							} 
							else if (KB.Down)
							{
								if (HandThing.canSlice) Actions.attack.Init(Attack.AttackIds.dislodgeIchi, attackHand);
								else Actions.attack.Init(Attack.AttackIds.dislodgeKick, attackHand);
							}
						} else
						{
							if (KB.Action  || (Actions.CombatMode && KB.MouseDown))  Actions.attack.Init(Attack.AttackIds.club, attackHand);
							if (KB.Action2 || (Actions.CombatMode && KB.Mouse2Down)) Actions.attack.Init(Attack.AttackIds.thrust, attackHand);
						}
					}
				}
			}

			if (( KB.Left || KB.Right) && !DisabledMoves && IsReady)
			{
				//  ---
				//  Keys LEFT + RIGHT
				//  ---
				if ( CanWalk )
				{

					if (!KB.Alt && (KB.KeyCombos.DoubleRight || KB.KeyCombos.DoubleLeft))
					{
						if (FacingLeft == KB.Left)
						{
							Actions.dive.Init();
						} else
						{
							Actions.backflip.Init();
						}

						KB.KeyCombos.DoubleRight = KB.KeyCombos.DoubleLeft = false;


					} else { 

						if (!IsWalking) HasTouchedGround = true;

						if ( FacingLeft != KB.Left )
						{
							if (KB.Alt)
							{
								Flip();
								IsWalking = false;
							}
							else
							{
								if (PBO.DesiredWalkingDirection > -1f) PBO.DesiredWalkingDirection = -10f;
								IsWalking = true;
							}
						}
						else
						{
							if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 10f;
							IsWalking = true;
						}

						//if (!IsWalking)
						//{

						//    if (FacingLeft != KB.Left) {

						//        if (KB.Left  && (Time.time - KB.KeyTimes.Left > 0.5f) && (Time.time - KB.KeyTimes.Left < 1.0f))   IsWalking   = Flip();
						//        if (KB.Right && (Time.time - KB.KeyTimes.Right > 0.5f) && (Time.time - KB.KeyTimes.Right < 1.0f)) IsWalking   = Flip();

						//    }
						//    else {
						//        IsWalking        = true;
						//        HasTouchedGround = true;
						//    }

						//    if (IsWalking) PBO.OverridePoseIndex = (int)PoseState.Walking;

						//}

					}
				} 
				else  if (IsCrouching)
				{
					if (FacingLeft != KB.Left) Flip();
					//FacingLeft = IsFlipped;
				}
				else if ( (KB.Alt || !HasTouchedGround) && !IsSwinging && CanSwing  )
				{
					Actions.swing.Init();
				}
			} 

			else if (KB.Down && !IsCrouching && Actions.prone.state == ProneState.ready && PBO.IsTouchingFloor && IsReady)
			{
				//  ---
				//  Keys DOWN
				//  ---
				if (KB.KeyCombos.DoubleDown)
				{
					if (!DisabledMoves) Actions.prone.Init();
					KB.KeyCombos.DoubleDown = false;
				} 
				else if (!DisabledMoves && Actions.prone.state == ProneState.ready) { 

					if (KB.Alt) PBO.OverridePoseIndex = (int)PoseState.Sitting;
					else
					{
						//PBO.OverridePoseIndex  = PuppetPose["JT_Duck"];
						if (!IsCrouching) Actions.crouch.Init();
					}

				}

			}

			else if (KB.Up)
			{
				//  ---
				//  Keys UP
				//  ---
				if (!IsInVehicle && Actions.prone.state != ProneState.ready)
				{
					StopPerson();
					JumpLocked = true;
				} 
				else if (!IsReady)
				{
					Recover();
				}
				else if (!DisabledMoves && !IsInVehicle) Actions.jump.Init();

			}


			else if (KB.Emote)
			{

			}



			else if (KB.Throw && IsReady)
			{
				//  ---
				//  Keys Throw
				//  ---
				if (IsReady && Actions.throwItem.state == ThrowState.ready) {
					if (PG.FH.IsHolding)
					{
						if (!PG.BH.IsHolding || !KB.Control) Actions.throwItem.Init(PG.FH);
						else Actions.throwItem.Init(PG.BH);
					}
					else if ( PG.BH.IsHolding )
					{
						if (!PG.FH.IsHolding || !KB.Shift) Actions.throwItem.Init(PG.BH);
						else Actions.throwItem.Init(PG.FH);
					}

				}

			}


			else {

				if (JumpLocked) JumpLocked = false;

			}

			if (KB.Aim )
			{
				switch ( PG.AimMode )
				{
					case AimModes.Off:
						PG.AimMode = AimModes.Manual;
						break;

					default:
						PG.AimMode = AimModes.Off;
						break;

				}

				Util.Notify("Aim Mode: <color=green>" + PG.AimMode.ToString(), VerboseLevels.Minimal);

				if (PG.AimMode == AimModes.Manual) Util.Notify("<color=red>[<color=yellow>SHIFT<color=red>] Front Hand  -  [<color=yellow>CTRL<color=red>] Back Hand</color>", VerboseLevels.Minimal );

			}

			//if (KB.ActionHeld || (IsAiming && KB.MouseDown))
			//{
			//    //  ---
			//    //  Keys ACTION
			//    //  ---
			//    if (!IsFiring && IsReady)
			//    {
			//        // Check if held item is something aimable
			//        if (!LB["LowerArmFront"].isActiveAndEnabled || !LB["LowerArmFront"].GripBehaviour.isHolding || !HandThing.canShoot)
			//        {
			//            IsFiring = false;
			//            return;
			//        }

			//        IsFiring = true;

			//        FireWeapon();
			//    }
			//    else if (HandThing.isAutomatic) FireWeapon();
			//}



		}


		//
		// ─── DO EMOTE ────────────────────────────────────────────────────────────────
		//
		public void DoEmote()
		{
			if (!IsEmote)
			{
				EmoteId = -1;

				KB.DisableNumberKeys();

				IsEmote = true;
			}

			EmoteId = KB.CheckNumberKey();

			if (EmoteId == -1) return;

		}


		//
		// ─── FIRE WEAPON ────────────────────────────────────────────────────────────────
		//
		private void FireWeapon()
		{
			if (!PBO.IsAlive()) return;
			HandThing.Activate(HandThing.isAutomatic);
		}


		//
		// ─── STOP PERSON ────────────────────────────────────────────────────────────────
		//
		public void StopPerson()
		{
			if ( PG.IsAiming ) {
				PG.FH.uArmL.Broken = PG.BH.uArmL.Broken = false;
				RunRigids(RigidReset);
				PG.FH.Thing.Reset();
			}
			if (!IsWalking && !IsCrouching) { 

				if (PBO.OverridePoseIndex != (int)PoseState.Rest || PBO.OverridePoseIndex != (int)PoseState.Sitting)
				{
					PBO.OverridePoseIndex = -1;
				}
			}
		}



		//
		// ─── POINT WEAPON ────────────────────────────────────────────────────────────────
		//
		public void PointWeapon(PuppetHand hand)
		{
			if (IsInVehicle)
			{
				if ( hand.Thing == null)
				{
					IsPointing = false;
					CheckMouseClickStatus();
				}

				if (hand.Thing?.JointType == JointTypes.FixedJoint) hand.Thing?.ChangeJoint(JointTypes.HingeJoint);

				Vector3 mouseX = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				Vector2 TPos;
				float TAngle;

				if (mouseX.x * Facing > ((RB2["Head"].position.x + 1.5) * Facing))
				{
					TPos   = (RB2["UpperBody"].position + (Vector2.right * 3) - HandThing.R.position);
					TAngle = Vector2.SignedAngle(Vector2.right, TPos) - (hand.Thing.angleAim + (hand.Thing.angleOffset * Facing));

				} else
				{
					TPos   = (mouseX - hand.Thing.tr.position);
					TAngle = Vector2.SignedAngle(Vector2.right, TPos) - (hand.Thing.angleAim + (hand.Thing.angleOffset * Facing));
				}

				hand.Thing?.R.MoveRotation(TAngle);
				hand.Thing?.R.AddForce(TPos * TotalWeight * Time.deltaTime * 12f);

				if (KB.Throw)
				{
					Actions.throwItem.power = Garage.Bike.CurrentSpeed;
					Actions.throwItem.Init(hand);
				}

				return;
			}
			else
			if (hand.Thing == null || hand.Thing?.isChainSaw == false)
			{
				IsPointing                    =
				LB["UpperArm"].Broken      =
				LB["UpperArmFront"].Broken =
				LB["LowerArm"].Broken      =
				LB["LowerArmFront"].Broken = false;
				RunRigids(RigidReset);
				return;
			}

			Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			if (mouse.x * Facing > (RB2["Head"].position.x * Facing))
			{
				LB["UpperArm"].Broken       =
				LB["UpperArmFront"].Broken  =
				LB["LowerArm"].Broken       =
				LB["LowerArmFront"].Broken  = false;
				RunRigids(RigidReset);
				return;
			}

			LB["UpperArm"].Broken           =
			LB["UpperArmFront"].Broken      =
			LB["LowerArm"].Broken           =
			LB["LowerArmFront"].Broken      = (IsWalking || IsCrouching);

			hand.Thing.R.inertia                 = 0.01f;
			hand.Thing.R.mass                    = hand.Thing.P.InitialMass / 2;

			if (pauseAiming) return;

			Vector3 TargetPos = (mouse - hand.Thing.tr.position);

			float TargetAngle = Vector2.SignedAngle(Vector2.right, TargetPos) - (hand.Thing.angleAim + (hand.Thing.angleOffset * Facing));

			hand.Thing.R.AddForce(TargetPos * TotalWeight * 3.5f );

			RB2["UpperBody"].AddForce(TargetPos * TotalWeight * 3.5f * -1);
		}



		//
		// ─── INVINCIBLE ────────────────────────────────────────────────────────────────
		//
		public void Invincible(bool doit)
		{
			foreach (LimbBehaviour limb in LB.Values) { if (limb.isActiveAndEnabled) limb.ImmuneToDamage = doit; }
		}

		//
		// ─── RUN LIMBS ────────────────────────────────────────────────────────────────
		//
		public void RunLimbs(Action<LimbBehaviour> action)
		{
			foreach (LimbBehaviour limb in LB.Values) { if (limb.isActiveAndEnabled) action(limb); }
		}

		public void RunLimbs<t>(Action<LimbBehaviour, t> action, t option)
		{
			foreach (LimbBehaviour limb in LB.Values) { if (limb.isActiveAndEnabled) action(limb, option); }
		}

		public void LimbFireProof(LimbBehaviour limb, bool option) {  

			PhysicalProperties propys           = limb.GetComponent<PhysicalBehaviour>().Properties.ShallowClone();
			propys.Flammability                 = option ? 0f : LimbOriginals[limb.name].Properties.Flammability;
			propys.BurningTemperatureThreshold  = float.MaxValue;
			propys.Burnrate                     = 0.00001f;

			limb.PhysicalBehaviour.Extinguish();
			limb.PhysicalBehaviour.Properties          = propys;
			limb.PhysicalBehaviour.SimulateTemperature = !option;
			limb.PhysicalBehaviour.ChargeBurns         = false;
			limb.PhysicalBehaviour.BurnProgress        = 0f;
			limb.DiscomfortingHeatTemperature          = float.MaxValue;
		}

		public void LimbImmune(LimbBehaviour limb, bool option)     => limb.ImmuneToDamage = option;
		public void LimbHeal(LimbBehaviour limb) { limb.BruiseCount = 0; }
		public void LimbGhost (LimbBehaviour limb, bool option) { limb.gameObject.layer = LayerMask.NameToLayer(option ? "Debris" : "Objects"); }
		public void LimbUp(LimbBehaviour limb, float option)
		{
			if (Vector2.Dot( limb.transform.up, Vector2.down ) < 0.1f ) return;
			//Mathf.Abs(limb.transform.eulerAngles.z) 
			PBO.LinkedPoses[PoseState.Rest].ShouldStumble = false;
			GetUp = true;

			float num = Mathf.DeltaAngle(limb.transform.eulerAngles.z, 0f) * limb.FakeUprightForce * limb.MotorStrength;
			if ( !float.IsNaN( num ) ) 
			{
				if ( limb.IsAndroid ) num *= 9f;
				limb.PhysicalBehaviour.rigidbody.AddTorque( num * 1.2f * limb.Person.ActivePose.UprightForceMultiplier * option );
			}
		}
		public void LimbStrenthen (LimbBehaviour limb) { 
			if (limb.BaseStrength < 15f) { 
				limb.BaseStrength = Mathf.Min(15f, limb.BaseStrength + 5f); 
				limb.Wince(10f);
			}
		}

		public void LimbCure( LimbBehaviour limb )
		{
			limb.HealBone();
			limb.PhysicalBehaviour.BurnProgress         =
			limb.SkinMaterialHandler.AcidProgress       =
			limb.SkinMaterialHandler.RottenProgress     = 0.0f;
			limb.BruiseCount                            = 
			limb.CirculationBehaviour.StabWoundCount    =
			limb.CirculationBehaviour.GunshotWoundCount = 0;
		}

		public void LimbRecover(LimbBehaviour limb)
		{
			limb.HealBone();
			limb.CirculationBehaviour.HealBleeding();

			limb.Health                                 = limb.InitialHealth;

			limb.Numbness                               =
			limb.PhysicalBehaviour.BurnProgress         =
			limb.SkinMaterialHandler.AcidProgress       =
			limb.SkinMaterialHandler.RottenProgress     = 0.0f;

			limb.CirculationBehaviour.IsPump            = limb.CirculationBehaviour.WasInitiallyPumping;
			limb.CirculationBehaviour.BloodFlow         = 1f;

			limb.BruiseCount                            = 
			limb.CirculationBehaviour.StabWoundCount    =
			limb.CirculationBehaviour.GunshotWoundCount = 0;

			limb.CirculationBehaviour.ForceSetAllLiquid(0f);
			limb.CirculationBehaviour.AddLiquid(limb.GetOriginalBloodType(), 1f);
			limb.CirculationBehaviour.AddLiquid(LifeLiquid, 0.1f);
		}

		public void LimbGangster (LimbBehaviour limb)
		{

			limb.RegenerationSpeed = 
			limb.BreakingThreshold = float.MaxValue;

			limb.LungsPunctured = 
			limb.IsLethalToBreak = false;

			limb.CirculationBehaviour.InternalBleedingIntensity =
			limb.Vitality = 
			limb.ImpactPainMultiplier = 
			limb.ShotDamageMultiplier = 
			limb.Numbness *= 0f;

			limb.BruiseCount = 0;

			if (limb.Health < limb.InitialHealth) limb.Health = limb.InitialHealth;
		}

		public void LimbReset(LimbBehaviour limb)
		{
			LimbSnapshot OG = LimbOriginals[limb.name];

			limb.BaseStrength      = OG.BaseStrength;
			limb.Broken            = OG.Broken;
			limb.BreakingThreshold = OG.BreakingThreshold;
			limb.BruiseCount       = OG.BruiseCount;
			limb.Frozen            = OG.Frozen;
			limb.Health            = OG.Health;
			limb.Numbness          = OG.Numbness;
			limb.RegenerationSpeed = OG.RegenerationSpeed;
			limb.Vitality          = OG.Vitality;

			//BaseStrength = limb.BaseStrength,
			//        BreakingThreshold = limb.BreakingThreshold,
			//        Broken = limb.Broken,
			//        BruiseCount = limb.BruiseCount,
			//        Frozen = limb.Frozen,
			//        Health = limb.Health,
			//        Numbness = limb.Numbness,
			//        RegenerationSpeed = limb.RegenerationSpeed,
			//        Vitality = limb.Vitality,
		}




		//
		// ─── RUN RIGIDS ────────────────────────────────────────────────────────────────
		//
		public void RunRigids(Action<Rigidbody2D> action)
		{
			foreach (Rigidbody2D rigid in RB2.Values) { action(rigid); }
		}

		public void RunRigids<t>(Action<Rigidbody2D, t> action, t option)
		{
			foreach (Rigidbody2D rigid in RB2.Values) { action(rigid, option); }
		}

		public void RigidInertia(Rigidbody2D rb, float option) => rb.inertia = (option == -1) ? RigidOriginals[rb.name].inertia : option;
		public void RigidMass(Rigidbody2D rb, float option) => rb.mass = (option == -1) ? RigidOriginals[rb.name].mass : option;
		public void RigidAddMass(Rigidbody2D rb, float option) => rb.mass *= option;
		public void RigidDrag(Rigidbody2D rb, float option) => rb.drag = (option == -1) ? RigidOriginals[rb.name].drag : option;
		public void BodyInertiaFix(Rigidbody2D rb) => SetRigidOriginal(rb.name);
		public void RigidReset(Rigidbody2D rb)
		{
			rb.mass           = RigidOriginals[rb.name].mass;
			rb.drag           = RigidOriginals[rb.name].drag;
			rb.inertia        = RigidOriginals[rb.name].inertia;
			rb.angularDrag    = RigidOriginals[rb.name].angularDrag;
			rb.freezeRotation = false;
		}
		public void RigidStop(Rigidbody2D rb)
		{
			rb.velocity        = Vector2.zero;
			rb.angularVelocity = 0f;
		}




		//
		// ─── HOLD THING ────────────────────────────────────────────────────────────────
		//
		//public void HoldThing(Thing thing, HoldStyle holdStyle = HoldStyle.Front)
		//{
		//    if ( handThing != null )
		//    {
		//        DropThing();
		//        return;
		//    }

		//    handThing = thing;
		//    handThing.AttachPuppetHand( this, true );

		//    if ( Actions.CombatMode && handThing.canShoot ) Actions.CombatMode = false;
		//    if ( IsAiming && !handThing.canShoot ) AimingStop();

		//    if ( IsInVehicle ) Garage.NoCollisionsWithHeldItem( handThing );

		//    foreach ( Transform tr in customParts.Transforms )
		//    {
		//        if ( tr != null ) Util.ToggleCollisions( handThing.tr, tr, false, false );
		//    }

		//    PG.Hold( thing, holdStyle );

		//    PG.CheckGrips();

		//}



		//public void DropThing(HoldStyle holdStyle = HoldStyle.Front)
		//{
		//    if ( handThing != null )
		//    {
		//        if ( handThing.P == null )
		//        {
		//            handThing = (Thing)null;
		//            return;
		//        }

		//        if ( GripF.isHolding )
		//        {
		//            LB["LowerArmFront"].SendMessage( "Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver );
		//            if ( LB["LowerArm"].GripBehaviour.isHolding )
		//            {
		//                LB["LowerArm"].SendMessage( "Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver );
		//            }
		//            Util.DisableCollision( PBO.transform, handThing.P, true );

		//            PuppetMaster.CheckDisabledCollisions = true;

		//            handThing.Dropped();
		//            handThing = (Thing)null;
		//        }
		//    }
		//    PG.Drop( holdStyle );
		//    PG.CheckGrips();
		//}


		//
		// ─── RECOVER ────────────────────────────────────────────────────────────────
		//
		public void Recover()
		{
			PBO.Consciousness   = 1f;
			PBO.ShockLevel      = 0.0f;
			PBO.PainLevel       = 0.0f;
			PBO.OxygenLevel     = 1f;
			PBO.AdrenalineLevel = 1f;

			if (KB.Modifier) RunLimbs(LimbRecover);
		}


		//
		// ─── SET RIGID ORIGINAL ────────────────────────────────────────────────────────────────
		//
		public void SetRigidOriginal(string rigidName, string propName="")
		{
			List<string> props = new List<string>() { "mass", "drag", "inertia" };

			if (propName != "") {
				props.Clear();
				props.Add(propName);
			}

			foreach (string pname in props)
			{
				if (RigidOriginals.TryGetValue(rigidName, out RigidSnapshot rigidOG))
				{
					switch(pname)
					{
						case "mass":
							RB2[ rigidName ].mass = rigidOG.mass;
							break;

						case "drag":
							RB2[rigidName].drag = rigidOG.drag;
							break;

						case "inertia":
							RB2[rigidName].inertia = rigidOG.inertia;
							break;
					}

				}
			}
		}

		public void CheckMouseClickStatus()
		{
			//if (!PG.IsAiming && !Actions.CombatMode && !IsHeroAiming && !IsPointing) KB.EnableMouse();
			if (!PG.IsAiming) KB.EnableMouse();
		}



	}
}
