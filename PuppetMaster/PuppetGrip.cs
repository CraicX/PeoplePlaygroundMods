//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetMaster
{
	[SkipSerialisation]
	public class PuppetGrip : MonoBehaviour
	{
		public PuppetHand FH;
		public PuppetHand BH;
		public PuppetHand[] BothHands;
		private Puppet _puppet;
		public PuppetAim PuppetAim;

		private bool Initialized     = false;

		private AimModes _aimMode    = AimModes.Off;
		private bool _dualWield      = false;
		private bool _isAiming       = false;

		private Coroutine _iCheckAiming;

		public bool PauseHold = false;

		public int HoldingPositionId = 0;

		private HoldPoses _HoldPose = HoldPoses.Default;

		public static Dictionary<HoldPoses, HPose> DualHoldPoses = new Dictionary<HoldPoses, HPose>();

		public HPose CurrentHoldPose;

		private bool _wasDualWielding = false;

		public ForcedHoldingPositions ForcedHolding = ForcedHoldingPositions.Default;






		// ─────────────────────────────────────────────────────────────────────────────────────────────
		//   :::::: PROPS
		// ─────────────────────────────────────────────────────────────────────────────────────────────


		public Puppet Puppet
		{
			get { return _puppet; }
			set { 
				if (!_puppet && _puppet == value) return;
				_puppet = value;
				foreach ( GripBehaviour grip in value.PBO.GetComponentsInChildren<GripBehaviour>() )
				{
					if ( grip == null ) continue;

					if (grip.name.Contains("Front")) FH = new PuppetHand(grip);
					else BH = new PuppetHand(grip);
				}

				BothHands = new PuppetHand[] { FH,BH };

				if (!Initialized) {
					StartCoroutine(ICheckGrips());
				}

				Initialized = true;
			}
		}

		public bool PauseDualWielding
		{
			set { 
				if (value)
				{
					if ( DualWield )
					{
						_wasDualWielding = true;
						Drop(BH, false);
					} else
					{
						_wasDualWielding = false;
					}
				}
				else if (_wasDualWielding)
				{
					StartCoroutine(IDualGrip(BH));
					_wasDualWielding = false;
				}
			}
		}

		public HoldPoses HoldPose
		{
			get { return _HoldPose; }
			set { 
				_HoldPose = value; 
				if ( value != HoldPoses.Default )
				{
					Util.Notify("[" + FH.Thing.name + "] Using Hold Pose: " + value, VerboseLevels.Full);
					if (DualHoldPoses.ContainsKey(value))  CurrentHoldPose = DualHoldPoses[value];
					else HoldPose = HoldPoses.Default;
				}
			}
		}

		public AimModes AimMode
		{
			get { return _aimMode; }
			set { 

				if (!FH.IsHolding && !BH.IsHolding) value = AimModes.Off;

				if ( value == AimModes.Off )
				{
					if ( PuppetAim != null ) Util.DestroyNow(PuppetAim);
					if ( gameObject.TryGetComponent<PuppetAim>( out PuppetAim puppetAim ) ) Util.DestroyNow(puppetAim);
					_aimMode = value;
					IsAiming = false;
					if (_iCheckAiming != null )
					{
						StopCoroutine(_iCheckAiming);
						_iCheckAiming = null;
					}
				}
				else
				{
					if ( _aimMode == AimModes.Off )
					{
						PuppetAim         = gameObject.AddComponent<PuppetAim>() as PuppetAim;
						PuppetAim.PG      = this;
						PuppetAim.Puppet  = Puppet;
						PuppetAim.enabled = true;

						_iCheckAiming = StartCoroutine(ICheckAiming());
					}

					_aimMode = value; 

				}
			}
		}

		public bool IsAiming
		{
			get { return _isAiming; }
			set { 

				if ( _isAiming != value )
				{
					if (!value && HoldPose != HoldPoses.Default) DoDualPose(true);

					if (value) KB.DisableMouse();
					else {
						_isAiming = value; 
						Puppet.CheckMouseClickStatus();
					}
				}
			_isAiming = value; 
			}

		}

		public bool DualWield
		{
			get { return _dualWield; }
			set { 
				if ( value == false && value != _dualWield )
				{
					Puppet.ResetPoses();
					FH.HoldStyle = HoldStyle.Front;
					BH.HoldStyle = HoldStyle.Back;
				}
				_dualWield = value; 
			}
		}

		public bool IsLodged
		{
			get
			{
				if (FH.Thing != null && FH.Thing.isLodged) return true;
				if (BH.Thing != null && BH.Thing.isLodged) return true;

				return false;
			}
		}




		// ────────────────────────────────────────────────────────────────────────────────────────────────
		//   :::::: FUNCS              
		// ────────────────────────────────────────────────────────────────────────────────────────────────


		public bool IsHolding => (bool)(FH.IsHolding || BH.IsHolding);
		public bool CanAim    => (bool)(FH.CanAim    || BH.CanAim);
		public bool CanAttack => (bool)(FH.CanAttack || BH.CanAttack);

		public PuppetHand GetAltHand(PuppetHand hand) => hand == FH ? BH : FH;


		private void Start()
		{
			BuildDualPoses();
			if ( Initialized )
			{
				if (_puppet == null) UnityEngine.Object.Destroy(this);
			} else
			{
			}

		}


		private void FixedUpdate()
		{
			if (!Puppet || !Puppet.IsActive) this.enabled = false;

			if ( IsHolding && !IsAiming )
			{
				if (HoldPose != HoldPoses.Default) DoDualPose();
			}
		}


		private void Update()
		{
			if (AimMode != AimModes.Off) { 

				if (!KB.ActionHeld  && !KB.MouseDown)  FH.IsFiring = false;
				if (!KB.Action2Held && !KB.Mouse2Down) BH.IsFiring = false;

				if (AimMode == AimModes.Manual) 
				{

					if (KB.ActionHeld || (FH.IsAiming && KB.MouseDown))
					{
						if (Puppet.IsReady && FH.IsHolding && FH.CanAim && (!FH.IsFiring || FH.Thing.isAutomatic ))
						{
							FH.IsFiring = !(!FH.LB.isActiveAndEnabled || !FH.GB.isHolding || !FH.Thing.canShoot);

							if (FH.IsFiring) ActivateItem(FH);
						}
					}

					if (KB.Action2Held || (BH.IsAiming && KB.Mouse2Down))
					{
						if (Puppet.IsReady && BH.IsHolding && BH.CanAim && (!BH.IsFiring || BH.Thing.isAutomatic ))
						{
							BH.IsFiring = !(!BH.LB.isActiveAndEnabled || !BH.GB.isHolding || !BH.Thing.canShoot);

							if (BH.IsFiring) ActivateItem(BH);
						}
					}
				}


			}
		}

		public void DoDualPose(bool init=false)
		{
			return;
			if ( init )
			{
				FH.LB.Broken    = BH.LB.Broken    = CurrentHoldPose.LowerArmBroken;
				FH.uArmL.Broken = BH.uArmL.Broken = CurrentHoldPose.UpperArmBroken;
			}
			else
			{ 
				FH.RB.AddForce(CurrentHoldPose.FrontLowerPos * -Puppet.Facing * Puppet.TotalWeight * CurrentHoldPose.FrontLowerForce * Time.fixedDeltaTime);
				BH.RB.AddForce(CurrentHoldPose.BackLowerPos  * -Puppet.Facing * Puppet.TotalWeight * CurrentHoldPose.BackLowerForce  * Time.fixedDeltaTime);
				FH.uArm.rigidbody.AddForce(CurrentHoldPose.FrontUpperPos * -Puppet.Facing * Puppet.TotalWeight * CurrentHoldPose.FrontUpperForce * Time.fixedDeltaTime);
				BH.uArm.rigidbody.AddForce(CurrentHoldPose.BackUpperPos  * -Puppet.Facing * Puppet.TotalWeight * CurrentHoldPose.BackUpperForce  * Time.fixedDeltaTime);
			}
		}



		public void BuildDualPoses()
		{
			DualHoldPoses.Clear();

			//DualHoldPoses = new Dictionary<HoldPoses, HPose>()
			//{
			//	{ HoldPoses.Handgun, new HPose(new Vector3(0.1f, -0.7f, 0.0f), new Vector3(0.4f, -1.0f, 0.0f))
			//		{ FrontLowerForce=40f, FrontUpperForce=40f, BackLowerForce=40f, BackUpperForce=40f, UpperArmBroken=false, LowerArmBroken=false } 
			//	},
			//	//{HoldPoses.Bat, new HPose(-96.47509f){ } },
			//	//{HoldPoses.Sword, new HPose(-96.47509f){ } },
			//	//{HoldPoses.Pipe, new HPose(-96.47509f){ } },
			//};
		}

		public void ConfigHandForAiming( PuppetHand hand, bool enableAiming )
		{
			IsAiming = (FH.IsAiming || BH.IsAiming);

			Util.Notify("ConfiguringAim for: " + hand.LB.name + " <color=yellow>" + enableAiming + "</color>");

			if (enableAiming)
			{ 

				// Modify aiming offset wee bit so they dont shoot exactly same spot when yielding 2 weapons
				if ( FH.CanAim && BH.CanAim ) hand.Positions[0] = Random.insideUnitCircle * 0.5f;
				else hand.Positions[0] = Vector2.zero;

				hand.uArm.rigidbody.drag = 1f;
				hand.RB.drag             = 1f;
				if (hand.Thing != null && hand.Thing.P != null) hand.Thing.R.drag        = 1f;

				if (Puppet.PBO.OverridePoseIndex > 0)
				{
					Puppet.PBO.ActivePose.AngleDictionary[hand.LB]    = hand.PoseL;
					Puppet.PBO.ActivePose.AngleDictionary[hand.uArmL] = hand.PoseU;
				} 

				Puppet.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[hand.LB]    = hand.PoseL;
				Puppet.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[hand.uArmL] = hand.PoseU;

				Puppet.PBO.Poses[6].AngleDictionary[hand.LB]    = hand.PoseL;
				Puppet.PBO.Poses[6].AngleDictionary[hand.uArmL] = hand.PoseU;

				hand.LB.Broken    = false;
				hand.uArmL.Broken = false;
			}

			else
			{
				//Puppet.RigidReset(hand.RB); 
				//Puppet.RigidReset(hand.uArm.rigidbody);

				//Puppet.ResetPoses(hand.LB);
				//Puppet.ResetPoses(hand.uArmL);

				//if (!FH.Flags[0] && !BH.Flags[0]) Puppet.ResetPoses();
				if (hand.Thing != null) hand.Thing.rigidSnapshot.Reset(hand.Thing.R);
			}

		}

		public bool ActivateItem(PuppetHand pHand)
		{
			pHand.Thing.Activate(pHand.Thing.isAutomatic);

			//ModAPI.Notify(@"Drag: " +
			//	pHand.uArm.rigidbody.drag + " - " +
			//	pHand.RB.drag             + " - " +
			//	pHand.Thing.R.drag        + " - " +
			//	Puppet.TotalWeight
			//);       

			return true;
		}

		public PuppetHand GetAttackHand()
		{
			if ( FH.IsHolding && FH.CanAttack)
			{
				if ((!BH.IsHolding || !BH.CanAttack) && !KB.Control) return FH;
				if (KB.Shift) return FH;
			}

			if ( BH.IsHolding && BH.CanAttack)
			{
				if ((!FH.IsHolding || !FH.CanAttack) && !KB.Shift) return BH;
				if (KB.Control) return BH;
				return FH;
			}

			return (PuppetHand)null;
		}

		public void Hold( Thing thing, PuppetHand hand = null )
		{
			if (thing == null) return;

			bool tryDual = false;

			if (KB.Control && KB.Shift) { hand = FH; tryDual = true; }
			if (ForcedHolding == ForcedHoldingPositions.Default && !BH.IsHolding && !FH.IsHolding && thing.AltPositions.Length > 1 ) tryDual = true;
			if (ForcedHolding == ForcedHoldingPositions.TwoHand) tryDual = true;

			PuppetMaster.GrabCooldown = Time.time + 1f;

			if (hand == null) hand = FH;

			hand.BypassCheck = Time.time + 2f;

			PuppetHand altHand = GetAltHand(hand);

			if (altHand.IsHolding && altHand.Thing != null) {
				Util.ToggleCollisions(thing.tr, altHand.Thing.tr,false,false );
			}

			//  Disable collisions between held item and Armor/Clothes
			if ( Puppet.customParts.Parts.Count > 0 )
			{
				foreach ( CustomPart part in Puppet.customParts.Parts )
				{
					Util.ToggleCollisions(thing.tr, part.T,false,false);
				}
			}

			Util.DisableCollision(Puppet.PBO.transform, thing.P, false);

			if (hand.IsHolding)
			{
				//ModAPI.Notify("Already Holding");
				if (hand.Thing == thing) return;
				Drop(hand);
				return;
			}

			thing.SetHand(this, hand);

			hand.IsHolding = true;

			StartCoroutine(IResetPosition(hand));
			//if (hand == FH || !tryDual) 
			if (tryDual) { StartCoroutine(IDualGrip(BH)); }

			hand.IsHolding = true;

		}


		public void Hold( Thing thing, HoldStyle holdStyle )
		{

			Hold (thing, holdStyle == HoldStyle.Back ? BH : FH);
		}


		public void Drop( HoldStyle holdStyle )
		{
			if ( holdStyle == HoldStyle.Front || holdStyle == HoldStyle.Dual ) Drop(FH);
			if ( holdStyle == HoldStyle.Back  || holdStyle == HoldStyle.Dual ) Drop(BH);
		}

		public void Drop( PuppetHand hand, bool permaDrop=true )
		{
			hand.IsHolding = false;
			if (hand.GB.isHolding) hand.GB.DropObject();

			if (permaDrop) {
				Util.DisableCollision(Puppet.PBO.transform, hand.Thing.P, false);
				hand.Thing.Dropped();
				//hand.Thing = null;
			}
		}


		public void Drop(Hands hand)
		{
			if ( hand == Hands.Front) Drop (FH);
			if ( hand == Hands.Back ) Drop (BH);
		}


		public void Drop( Thing thing )
		{
			if (FH.IsHolding && FH.Thing == thing ) Drop(FH);
			if (BH.IsHolding && BH.Thing == thing ) Drop(BH);
		}


		public void FlipHeldItems(bool doDrop=true)
		{
			if (doDrop)
			{
				if ( FH.IsHolding ) {

					if ( FH.Thing != null && FH.Thing.G.TryGetComponent<HingeJoint2D>( out HingeJoint2D joint ) )
					{
						joint.enabled = false;
					}
					FH.GB.DropObject();
					FH.IsHolding = false;

				}
				if ( BH.IsHolding ) {
					if ( BH.GB.gameObject.TryGetComponent<HingeJoint2D>( out HingeJoint2D joint ) )
					{
						joint.enabled = false;
					}
					BH.GB.DropObject();
					BH.IsHolding = false;
				}
				return;
			}
			if ( FH.IsHolding ) StartCoroutine(IResetPosition(FH, false, false));
			if ( BH.IsHolding ) StartCoroutine(IResetPosition(BH, false, false));
		}


		public void FixLayer(PuppetHand hand)
		{
			SpriteRenderer SR;

			if ( hand.HoldStyle == HoldStyle.Front || hand.HoldStyle == HoldStyle.Dual )
			{
				//  Place Item behind front arm
				if ( hand.PB.TryGetComponent<SpriteRenderer>( out SR ) )
				{
					FH.Thing.P.spriteRenderer.sortingLayerName = SR.sortingLayerName;
					FH.Thing.P.spriteRenderer.sortingOrder     = SR.sortingOrder - 1;
				}

				//  If Puppet is wearing Armor or attached clothing
				if (Puppet.customParts.Parts.Count > 0)
				{
					foreach ( CustomPart part in Puppet.customParts.Parts )
					{
						if (part.Parent.name == "UpperLegFront")
						{
							//  Place Item in front of armor attached to front lower leg
							if ( part.G.TryGetComponent<SpriteRenderer>( out SR ) )
							{
								hand.Thing.P.spriteRenderer.sortingLayerName = SR.sortingLayerName;
								hand.Thing.P.spriteRenderer.sortingOrder     = SR.sortingOrder + 1;
							}
						}
					}
				}
			}
			else
			{
				if ( hand.PB.TryGetComponent<SpriteRenderer>( out SR ) )
				{
					hand.Thing.P.spriteRenderer.sortingLayerName = SR.sortingLayerName;
					hand.Thing.P.spriteRenderer.sortingOrder     = SR.sortingOrder + 1;
				}

				//  If Puppet is wearing Armor or attached clothing
				if (Puppet.customParts.Parts.Count > 0)
				{
					foreach ( CustomPart part in Puppet.customParts.Parts )
					{
						if (part.Parent.name == "LowerArm")
						{
							//  Place Item in front of armor attached to front lower leg
							if ( part.G.TryGetComponent<SpriteRenderer>( out SR ) )
							{
								hand.Thing.P.spriteRenderer.sortingLayerName = SR.sortingLayerName;
								hand.Thing.P.spriteRenderer.sortingOrder     = SR.sortingOrder + 1;
							}

						}
						if ( part.Parent.name == "UpperLeg" || part.Parent.name == "LowerLeg" )
						{
							if ( part.G.TryGetComponent<SpriteRenderer>( out SR ) )
							{
								SR.sortingOrder = 12;
							}
						}
					}
				}
			}
		}


		public void FixPosition( PuppetHand hand, bool noRotate = false, bool extraFlip = false )
		{
			StartCoroutine(IResetPosition(hand, noRotate, extraFlip));
		}


		public void BlockArmPose( PuppetHand hand=null, bool includeWalking=true )
		{
			if ( hand == null )
			{
				BlockArmPose(FH, includeWalking); 
				BlockArmPose(BH, includeWalking);
			}
			else
			{
				if (includeWalking) { 
					Puppet.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[hand.LB]    = hand.PoseL;
					Puppet.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[hand.uArmL] = hand.PoseU;
				}
				if ( Puppet.PBO.OverridePoseIndex > 0 )
				{
					Puppet.PBO.ActivePose.AngleDictionary[hand.LB]    = hand.PoseL;
					Puppet.PBO.ActivePose.AngleDictionary[hand.uArmL] = hand.PoseU;
				}
			}
		}


		public PM3 IsHoldPaused(float IeTimeout, bool dual=false)
		{
			PM3 qResp = PM3.Success;

			if (PauseHold || Puppet.GetUp || !Puppet.IsReady || !Puppet.IsUpright || !new int[]{-1,0,7}.Contains(Puppet.PBO.OverridePoseIndex)) qResp = PM3.Fail;
			if (qResp == PM3.Success && dual)
			{
				if (FH.Thing == null || FH.Thing.P == null) return PM3.Error;
				if ((FH.Thing.R.position.x * Puppet.Facing) > (Puppet.RB2["Head"].position.x * Puppet.Facing) - (0.5f * Puppet.Facing)) qResp = PM3.Fail;
			}

			if (Time.time > IeTimeout) return PM3.Timeout;
			return qResp;
		}



		// ────────────────────────────────────────────────────────────────────────────────────────────────
		//   :::::: IENUMERATORs
		// ────────────────────────────────────────────────────────────────────────────────────────────────

		public IEnumerator IDualGrip( PuppetHand hand )
		{
			Util.Notify("Starting Dual Grip", VerboseLevels.Full);

			PM3 qResp;
			float ieTimeout = Time.time + 5;
			do
			{
				qResp = IsHoldPaused(ieTimeout, true);
				if (qResp == PM3.Timeout) yield break;
				if (qResp == PM3.Error) yield break;
				yield return new WaitForEndOfFrame();

			} while(qResp == PM3.Fail);

			PauseHold          = true;
			bool oneMove       = false;
			PuppetHand altHand = GetAltHand( hand );

			if (!altHand.Validate() )
			{
				//ModAPI.Notify("Could not validate");
				PauseHold = false;
				yield break;
			}

			hand.IsHolding     = true;
			hand.Thing         = altHand.Thing;
			hand.HoldStyle     = altHand.HoldStyle = HoldStyle.Dual;

			Util.ToggleWallCollisions(altHand.Thing.tr);

			altHand.Thing.P.MakeWeightless();


			if ( altHand.Thing.AltPositions.Length == 1 )
			{
				Vector2 hPos = (Vector2)altHand.Thing.HoldingPosition;// + (Random.insideUnitCircle * 0.1f);
				altHand.Thing.AltPositions = new Vector3[] { hPos };
				oneMove = true;

				//	Define the special holding position

				if ( altHand.Thing.canAim ) HoldPose = HoldPoses.Handgun;
				else if (altHand.Thing.canStrike)
				{
					if (altHand.Thing.canStab && altHand.Thing.P.ObjectArea > 1.5f) HoldPose = HoldPoses.Sword;
					else if (!altHand.Thing.P.Properties.Sharp && altHand.Thing.P.ObjectArea > 1.5f) HoldPose = HoldPoses.Bat;
					else HoldPose = HoldPoses.Pipe;
				}

			} else HoldPose = HoldPoses.Default;

			Vector3 dir;
			Vector3 dir2;

			float timeout;
			float dist          = 1;
			float lastDist      = 1;
			float thingRotation = altHand.Thing.angleAim + altHand.Thing.angleOffset;
			bool failedGrab     = false;

			int hPosId = 0;

			foreach ( Vector3 hPos in altHand.Thing.AltPositions )
			{
				float minDist       = float.MaxValue;
				int attempts		= 0;

				if ( true || !oneMove )
				{

					hand.LB.Broken = altHand.LB.Broken = hand.uArmL.Broken = altHand.uArmL.Broken = true;
					failedGrab = false;

					Puppet.RunRigids(Puppet.RigidReset);

					yield return new WaitForFixedUpdate();

					hand.RB.drag                     = 0.01f;
					hand.RB.inertia                  = 0.01f;

					altHand.Thing.R.drag             = 0.01f;
					altHand.Thing.R.inertia	         = 0.01f;

					//hand.RB.drag                   = 0.01f;
					//altHand.uArm.rigidbody.drag    = 0.01f;
				}

				timeout = Time.time + 2;

				do { 
					if (!altHand.Validate() )
					{
						PauseHold = false;
						yield break;
					}

					altHand.Thing.R.velocity = Vector3.zero;
					hand.RB.velocity      = Vector3.zero;

					lastDist              = dist;
					if (FH.Thing == null)
					{
						PauseHold = false;
						yield break;
					}


					if (!oneMove) {
						dir = (Puppet.RB2["UpperBody"].position - altHand.Thing.R.position) + (Vector2.right * 0.1f * -Puppet.Facing);
						altHand.Thing.R.AddForce( dir * Time.fixedDeltaTime * 1000f);
					}

					dir2   = altHand.Thing.tr.TransformPoint(hPos) - hand.RB.transform.TransformPoint(hand.GB.GripPosition);

					hand.RB.AddForce(dir2 * Time.fixedDeltaTime * Puppet.TotalWeight * 1000f);

					dist   = Mathf.Abs(Vector2.Distance(hand.GB.transform.TransformPoint(hand.GB.GripPosition), altHand.Thing.tr.TransformPoint(hPos)));
					if (dist < minDist) 
					{
						minDist  = dist;
						attempts = 0;
					} else if (dist <= minDist + 0.01f)
					{
						if (++attempts > 5) { failedGrab = false; break; }
					}

					if (Time.time > timeout) { failedGrab = true; break; }

					yield return new WaitForFixedUpdate();
					if ( !altHand.Validate() )
					{
						PauseHold = false;
						yield break;
					}

				} while (dist > 0.1f || dist < lastDist);


				++hPosId;

				Util.Notify("Tried " + hPos + ": " + (failedGrab ? "failed" : "success") + " (" + hPosId + "/" + altHand.Thing.AltPositions.Length + ")", VerboseLevels.Full);

				if (!failedGrab) {
					Puppet.RunRigids(Puppet.RigidStop);
					break;
				}
			}

			hand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

			Puppet.RunRigids(Puppet.RigidReset);
			altHand.Thing.Reset();

			//altHand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

			yield return new WaitForEndOfFrame();

			//altHand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

			altHand.LB.Broken = altHand.uArmL.Broken = hand.LB.Broken = hand.uArmL.Broken = false;
			hand.Thing        = altHand.Thing;
			PauseHold         = false;

			FixLayer(altHand);



			ConfigHandForAiming(FH,true);
			ConfigHandForAiming(BH,true);

			yield return null;

		}


		IEnumerator ICheckGrips()
		{
			yield return new WaitForSeconds(2);
			for (; ; )
			{
				if (FH != null) FH.Check();
				if (BH != null) BH.Check();

				if(FH.IsHolding && BH.IsHolding && FH.Thing == BH.Thing) DualWield = true;
				else DualWield = false;


				if (!FH.IsHolding && !BH.IsHolding) AimMode = AimModes.Off;
				yield return new WaitForSeconds(1);
			}
		}


		IEnumerator ICheckAiming()
		{
			yield return new WaitForSeconds(2);
			for (; ; )
			{
				if (AimMode == AimModes.Off) break;

				if (!FH.IsHolding && !BH.IsHolding) AimMode = AimModes.Off;

				yield return new WaitForSeconds(1f);
			}

			yield return null;
		}


		IEnumerator IResetPosition( PuppetHand hand, bool noRotate=false, bool extraFlip=false )
		{
			Util.Notify("Starting reset Position", VerboseLevels.Full);
			if (!hand.IsHolding) yield return null;

			PM3 qResp;
			float ieTimeout = Time.time + 5;
			do
			{
				qResp = IsHoldPaused(ieTimeout, false);
				if (qResp == PM3.Timeout) yield break;
				if (qResp == PM3.Error) yield break;
				if (qResp == PM3.Fail ) yield return new WaitForEndOfFrame();

			} while(qResp == PM3.Fail);

			PauseHold = true;

			PuppetHand altHand = GetAltHand(hand);

			if ( hand == BH && FH.IsHolding && FH.Thing == BH.Thing )
			{
				BH.HoldStyle = FH.HoldStyle = HoldStyle.Dual;
				PauseHold = false;
				StartCoroutine(IDualGrip(hand));
			}
			else
			{


				bool hideAlt = false;
				int altLayer = 9;
				altHand.Validate();
				if ( altHand.IsHolding && altHand.Thing != hand.Thing )
				{

					altLayer = altHand.Thing.G.layer;
					altHand.Thing.G.SetLayer(2);
					yield return new WaitForFixedUpdate();
				}

				bool thingFlipped           = hand.Thing.IsFlipped;

				if (extraFlip) thingFlipped = !thingFlipped;

				if (Puppet.IsFlipped != thingFlipped) hand.Thing.Flip();

				yield return new WaitForFixedUpdate();

				if (hand.Thing.angleHold == 0f)
				{
					hand.Thing.angleHold = (hand.Thing.holdToSide && !noRotate) ? 5.0f : 95.0f;
					hand.Thing.angleAim  = 95.0f;
				}

				if (hand.Thing.HoldingMove != "") hand.Thing.ActionPose.RunMove();

				Vector3 hpos;

				if (Puppet.IsInVehicle) hpos = hand.Thing.HoldingPositionJoust;
				else hpos                    = hand.Thing.HoldingPosition;

				//if (P.Properties.SharpAxes.Length == 1) angleHold = angleAim = P.Properties.SharpAxes[0].Axis.x;

				float thingRotation = hand.IsAiming ? hand.Thing.angleAim : hand.Thing.angleHold;

				thingRotation += hand.Thing.angleOffset;

				hand.Thing.tr.rotation = Quaternion.Euler(0.0f, 0.0f, hand.Thing.IsFlipped ? hand.RB.rotation + thingRotation : hand.RB.rotation - thingRotation);

				hand.Thing.tr.position += hand.GB.transform.TransformPoint(hand.GB.GripPosition) - hand.Thing.tr.TransformPoint((Vector3)hpos);

				//GB.Use(ActivationPropagation());
				hand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

				yield return new WaitForFixedUpdate();

				if (hand.Thing.angleAim != hand.Thing.angleHold)
				{
					hand.Thing.tr.rotation = Quaternion.Euler(0.0f, 0.0f, hand.Thing.IsFlipped ? hand.RB.rotation + thingRotation : hand.RB.rotation - thingRotation);
				}

				if ( hideAlt )
				{
					yield return new WaitForFixedUpdate();

					altHand.Thing.G.SetLayer(altLayer);

					yield return new WaitForFixedUpdate();
				}

				//ModAPI.Notify("Holding: " + hand.GB.CurrentlyHolding);
				FixLayer(hand);

				PauseHold = false;
			}

			yield return null;
		}


	}


}


