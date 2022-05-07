//                             ___           ___         ___     
//  Feel free to use and      /\  \         /\  \       /\__\    
//  modify any of this code   \:\  \       /::\  \     /:/  /    
//                             \:\  \     /:/\:\__\   /:/  /     
//   ________  ________    _____\:\  \   /:/ /:/  /  /:/  /  ___ 
//  |\   __  \|\   __  \  /::::::::\__\ /:/_/:/  /  /:/__/  /\__\
//  \ \  \|\  \ \  \|\  \ \:\~~\~~\/__/ \:\/:/  /   \:\  \ /:/  /
//   \ \   ____\ \   ____\ \:\  \        \::/__/     \:\  /:/  / 
//    \ \  \___|\ \  \___|  \:\  \        \:\  \      \:\/:/  /  
//     \ \__\    \ \__\      \:\__\        \:\__\      \::/  /   
//      \|__|     \|__|       \/__/         \/__/       \/__/     
//
//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PPnpc
{
	public class NpcHand : MonoBehaviour
	{
		private bool _isAiming = false;
		
		public NpcBehaviour NPC;
		public NpcTool Tool;
		public GripBehaviour GB;
		public LimbBehaviour LB;
		public Rigidbody2D RB;
		public PhysicalBehaviour PB;
		public Transform T;
		public bool FireAtWill = false;
		public PhysicalBehaviour uArm;
		public LimbBehaviour uArmL;
		

		public bool DualWield      = false;
		public bool IsFiring       = false;
		public bool IsHolding      = false;
		
		public bool[] Flags        = { false, false, false, false };
		public Vector3[] Positions = { Vector3.zero, Vector3.zero };
		public float[] MiscValues  = { 0f, 0f, 0f };
		public int[] MiscInts      = { 0,0,0 };
		
		public float BypassCheck   = 0f;
		
		public Vector2 V2Pos;
		public float LastFire;
		public float NextFireTime = 0f;
		public float FireRate = 0.5f;

		public  Vector3 AimTarget = Vector3.zero;
		private float Facing => NPC.Facing;

		public AimStyles AimStyle = AimStyles.Standard;


		public RagdollPose.LimbPose PoseL;
		public RagdollPose.LimbPose PoseU;

		
		public bool IsAiming
		{
			get { return _isAiming; }
			set { 
				_isAiming = value;
				if (value == false) {
					FireAtWill = false;
					ConfigHandForAiming(false);
				}
				else
				{
					ConfigHandForAiming(true);
				}
			}
		}

		private PhysicalBehaviour _aimAt;

		public PhysicalBehaviour AimAt
		{
			get { return _aimAt; }
			set { 
				_aimAt = value; 
				AimTarget = AimAt.transform.position;
				NPC.Mojo.Feel("Bored", -1f);
				NPC.PBO.AdrenalineLevel += 0.1f;
			}

		}
		private bool _aiming = false;
		public bool Aiming
		{
			get { return _aiming; }
			set { 
				if (_aiming != value)
				{
					NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[LB]    = PoseL;
					NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[uArmL] = PoseU;
					NPC.PBO.Poses[6].AngleDictionary[LB]    = PoseL;
					NPC.PBO.Poses[6].AngleDictionary[uArmL] = PoseU;
				}
				_aiming = value; 
			}

		}

		public void FixHandsPose( int poseId = 6 )
		{
			NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[LB]    = PoseL;
			NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[uArmL] = PoseU;
			NPC.PBO.Poses[poseId].AngleDictionary[LB]    = PoseL;
			NPC.PBO.Poses[poseId].AngleDictionary[uArmL] = PoseU;
		}



		public bool Target( PhysicalBehaviour pb )
		{
			Aiming = true;
			AimAt  = pb;

			if ( Time.time > NextFireTime )
			{
				NextFireTime = Time.time + FireRate; // Time.time + xxx.rr(FireRate, FireRate * 10);
				//Tool.Activate( Tool.isAutomatic);
				//FunFacts.Inc(NPC.NpcId, "Fired Weapon");
			}

			//uArmL.Broken = true;
			return true;
		}
		
		void FixedUpdate()
		{
			if (!NPC.Active) return;
			if ( _aiming )
			{
				V2Pos = NPC.Head.position * Facing;
				
				if ( AimTarget != null &&  ( AimTarget.x * Facing < V2Pos.x - 0.5f ) && ( Mathf.Abs( Vector2.Distance( AimTarget * Facing, V2Pos ) ) > 2.5f ) )
				{
					AimWeapon();
				}
			}
		}

		//public void Drop()     => PG.Drop(this);
		public bool CanAim       => (bool)(Tool != null && Tool.CanAim    && LB != null && LB.IsConsideredAlive); 
		public bool CanAttack    => (bool)(Tool != null && Tool.CanStrike && LB != null && LB.IsConsideredAlive); 
		public string HandId     => (this == NPC.FH) ? "Front" : "Back";
		public NpcHand AltHand   => (this == NPC.FH) ? NPC.BH : NPC.FH;

		

		
		public void Init()
		{
			string armID = name.Contains("Front") ? "ArmFront" : "Arm";
			NPC          = gameObject.transform.root.GetComponent<NpcBehaviour>();
			GB           = gameObject.GetComponent<GripBehaviour>();
			LB	         = NPC.LB["Lower" + armID];
			RB           = NPC.RB["Lower" + armID];
			PB           = LB.PhysicalBehaviour;
			uArmL        = NPC.LB["Upper" + armID];
			uArm         = uArmL.PhysicalBehaviour;
			T            = LB.transform;
			//PoseL        = NpcPose.FreeLimbPose(LB, true);
			//PoseU        = NpcPose.FreeLimbPose(uArmL, true);
			PoseL = NPC.PBO.Poses[0].AngleDictionary[LB];
			PoseU = NPC.PBO.Poses[0].AngleDictionary[uArmL];

			if ( GB.isHolding )
			{
				PhysicalBehaviour item = (PhysicalBehaviour)GB.CurrentlyHolding;
				Tool                  = item.gameObject.GetOrAddComponent<NpcTool>();
				IsHolding              = true;
				Tool.SetTool(item, this);
			}

			StartCoroutine(IValidate());
		}


		public void AimWeapon()
		{
			if (!NPC)
			{
				Object.Destroy(this);
				return;
			}

			if (Vector2.Dot( NPC.Head.up, Vector2.down ) > 0.1f ) return;

			//if (!NPC.IsUpright) return;

			//if (!NPC.IsUpright) { Aiming = false; return; }
			//V2Pos = NPC.LB["UpperBody"].transform.position;

			//if ((NPC.Facing < 0 && AimAt.transform.position.x > V2Pos.x  - 0.5f ) ||
			//	(NPC.Facing > 0 && AimAt.transform.position.x < V2Pos.x )) return;

			if (!LB.isActiveAndEnabled || !LB.IsConsideredAlive) {
				IsAiming = false;
				return;
			}

			if (!AimAt || !T || !RB || !Tool.T)
			{
				Aiming = IsAiming = false;
				return;
			}

			if (Time.frameCount % 10 == 0 || AimTarget == null) AimTarget  = AimAt.transform.position + (Vector3)Random.insideUnitCircle * NPC.Config.AimingSkill;
			switch ( Tool.props.CurrentAimStyle )
			{
				case AimStyles.Proned:
					//AimTarget.y += 0.01f;
					break;

				case AimStyles.Crouched:
					//AimTarget.y += 0.01f;
					break;

				case AimStyles.Rockets:
					AimTarget.y += xxx.rr(0.1f,0.5f);
					FireRate = 1.5f;
					break;

				case AimStyles.Spray:
					AimTarget.y += Mathf.Sin(Time.time) * 0.1f;
					break;
			}
	
			float fDist = DualWield ? -0.2f : (NPC.Facing < 0 ? 0.5f : -0.1f);
				
			Vector3 E3Pos    = ((AimTarget - T.position) * -NPC.Facing);
			
			E3Pos.y += fDist;

			float fAngle   = Mathf.Atan2(E3Pos.y, E3Pos.x) * Mathf.Rad2Deg;

			if (DualWield) RB.AddForce(E3Pos.normalized * -NPC.Facing * Time.fixedDeltaTime * 35f);
			
			if (!Tool.T) { Aiming = IsAiming = false; return; }

			float fNum     = Mathf.DeltaAngle(Tool.T.eulerAngles.z, fAngle)  * Time.fixedDeltaTime * 15f;
			
			if (Time.frameCount % 1000 == 0) fDist = float.MinValue;

			if ( !float.IsNaN( fNum ) ) Tool.R.AddTorque(Mathf.Clamp(fNum, -30f, 30f));

			if (FireAtWill && (Tool.props.isAutomatic || Time.time > NextFireTime)) {
				if (!Tool.props.isAutomatic) FunFacts.Inc(NPC.NpcId,"ShotsFired");
				NextFireTime = Time.time + xxx.rr(FireRate, FireRate * 10);
				if (GB.isHolding) Tool.Activate(Tool.props.isAutomatic);
			}


			//if (fNum < 0.05f &&
			//if (FireAtWill && Mathf.Abs(fNum) < 2f && CheckMyShot()) {
			if (FireAtWill && Mathf.Abs(fNum) < 2f ) {
				
				
				
				
				FireAtWill = xxx.rr(1,3) == 2;

				
				NPC.Mojo.Feel("Angry", -5f);
				NPC.Mojo.Feel("Bored", -5.5f);
				NPC.Mojo.Feel("Annoyed", -2.5f);
			  
				NPC.PBO.AdrenalineLevel += 1f;

				//if ( Time.time - NPC.LastLogTime > 1f)
				//{

				//	NPC.LastLog = new EventLog()
				//	{
				//		EventId    = EventIds.Gun,
				//		Importance = 3,
				//		NpcId      = NPC.NpcEnemy.NpcId,
				//		NpcType    = NPC.NpcEnemy.Config.NpcType,
				//		Timestamp  = Time.time
				//	};
					
				//	NPC.EventLog.Add( NPC.LastLog );

				//	NpcEvents.BroadcastRadius(NPC,300f,EventIds.Gun,3);

				//}
			}

		}
		public bool ValidateItem( PhysicalBehaviour item )
		{
			if (item.beingHeldByGripper)
			{
				GripBehaviour[] AllGrips = UnityEngine.Object.FindObjectsOfType<GripBehaviour>();

				foreach ( GripBehaviour grip in AllGrips )
				{
					if ( grip.isHolding && grip.CurrentlyHolding == item && (grip != GB || grip != AltHand.GB)) return false;
				}
			}

			return true;
		}

		public bool Validate()
		{
			if (!GB.isHolding)
			{
				Tool     = null;
				IsHolding = false;
				return false;
			} else if (Tool && Tool.Hand.NPC != NPC ) 
			{
				Drop();
				return false;
			}
			else 
			{
				if ( Tool == null || Tool.P == null || Tool.P != GB.CurrentlyHolding )
				{
					Tool = GB.CurrentlyHolding.gameObject.GetOrAddComponent<NpcTool>();
					Tool.SetTool(GB.CurrentlyHolding, this);
					//Tool.GetDetails();
					IsHolding = true;
				}
				return true;
			}
		}

		public IEnumerator IValidate()
		{
			for (; ; )
			{
				if (Tool && Tool.Hand.NPC != NPC ) Drop();
				Check();

				yield return new WaitForSeconds(1);
			}

		}

		
		public void Hold( Props prop )
		{
			if (!prop.P) return;
			
			Validate();
			if (!ValidateItem(prop.P)) return;
			if (!LB.IsCapable || LB.GripBehaviour.isHolding) return;

			if (prop.needsTwoHands && (AltHand.IsHolding || !AltHand.LB.IsCapable)) return;

			Tool = prop.gameObject.GetOrAddComponent<NpcTool>() as NpcTool;

			Tool.SetTool(prop.P, this);

			BypassCheck = Time.time + 2f;

			if (AltHand.IsHolding && AltHand.Tool) {
				xxx.ToggleCollisions(Tool.T, AltHand.Tool.T,false,false );
			}

			xxx.ToggleCollisions(NPC.PBO.transform, Tool.T, false);

			IsHolding = true;

			NPC.ScannedPropsIgnored.Add(prop);
			
			StartCoroutine(IResetPosition());

			if (AltHand.IsHolding) NPC.Goals.Scavenge = false;
			if (prop.canShoot || prop.canStab || prop.canStrike) NPC.Goals.Attack = true;
			
			if (prop.canShoot) NPC.HasGun         = true;
			if (prop.canStab) NPC.HasKnife        = true;
			if (prop.canStrike) NPC.HasClub       = true;
			if (prop.canExplode) NPC.HasExplosive = true;
			if (prop.canFightFire) NPC.HasFireF   = true;

		}



		public void Drop()
		{
			GB.DropObject();
			IsHolding = IsAiming = Aiming = false;
			
		}
		public void Check()
		{
			if ( Time.time < BypassCheck ) return;
			if ( GB.isHolding )
			{
				if (!IsHolding) { 
					IsHolding = true;

					if (!GB.CurrentlyHolding.TryGetComponent<NpcTool>(out NpcTool tool)) {
						Tool = tool;
						
					}

						Tool = GB.CurrentlyHolding.gameObject.AddComponent<NpcTool>();

				}

			}
			else
			{
				if (IsHolding) {
					string itemName = "unknown";
					
					if (Tool != null) {
						itemName = Tool.name;
						//Tool.Dropped();
					}
					
					NPC.Goals.Scavenge = true;


					IsHolding = false;
					
				}
			}
		}

		public void FixLayer()
		{
			if (NPC.PBO == null) {
				UnityEngine.Object.Destroy(NPC);
				UnityEngine.Object.Destroy(this);
				return;
			}
			if (!GB.isHolding) return;

			//Tool.G.SetLayer(10);
			
			SpriteRenderer SR;

			if ( this == NPC.FH )
			{
				//  Place Item behind front arm
				if ( PB.TryGetComponent<SpriteRenderer>( out SR ) )
				{
					SR.sortingOrder = 4;

					Tool.P.spriteRenderer.sortingLayerName = SR.sortingLayerName;
					Tool.P.spriteRenderer.sortingOrder     = SR.sortingOrder - 1;
				} else
				{
					
				}

				//  If Puppet is wearing Armor or attached cloTool
				//if (Puppet.customParts.Parts.Count > 0)
				//{
				//	foreach ( CustomPart part in Puppet.customParts.Parts )
				//	{
				//		if (part.Parent.name == "UpperLegFront")
				//		{
				//			//  Place Item in front of armor attached to front lower leg
				//			if ( part.G.TryGetComponent<SpriteRenderer>( out SR ) )
				//			{
				//				hand.Tool.P.spriteRenderer.sortingLayerName = SR.sortingLayerName;
				//				hand.Tool.P.spriteRenderer.sortingOrder     = SR.sortingOrder + 1;
				//			}
				//		}
				//	}
				//}
			}
			else
			{
				if ( PB.TryGetComponent<SpriteRenderer>( out SR ) )
				{
					SR.sortingOrder = -4;
					Tool.P.spriteRenderer.sortingLayerName = SR.sortingLayerName;
					Tool.P.spriteRenderer.sortingOrder     = SR.sortingOrder + 1;
				}

				//  If Puppet is wearing Armor or attached cloTool
				//if (Puppet.customParts.Parts.Count > 0)
				//{
				//	foreach ( CustomPart part in Puppet.customParts.Parts )
				//	{
				//		if (part.Parent.name == "LowerArm")
				//		{
				//			//  Place Item in front of armor attached to front lower leg
				//			if ( part.G.TryGetComponent<SpriteRenderer>( out SR ) )
				//			{
				//				Tool.P.spriteRenderer.sortingLayerName = SR.sortingLayerName;
				//				Tool.P.spriteRenderer.sortingOrder     = SR.sortingOrder + 1;
				//			}

				//		}
				//		if ( part.Parent.name == "UpperLeg" || part.Parent.name == "LowerLeg" )
				//		{
				//			if ( part.G.TryGetComponent<SpriteRenderer>( out SR ) )
				//			{
				//				SR.sortingOrder = 12;
				//			}
				//		}
				//	}
				//}
			}
		}

		public void ConfigHandForAiming( bool enableAiming )
		{
			if (enableAiming)
			{ 
				
				uArm.rigidbody.drag = 1f;
				RB.drag             = 1f;
				if (Tool && Tool.P) Tool.R.drag        = 1f;

				//if (NPC.PBO.OverridePoseIndex > 0)
				//{
				//	NPC.PBO.ActivePose.AngleDictionary[hand.LB]    = hand.PoseL;
				//	NPC.PBO.ActivePose.AngleDictionary[hand.uArmL] = hand.PoseU;
				//} 

				//NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[hand.LB]    = hand.PoseL;
				//NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[hand.uArmL] = hand.PoseU;

				//NPC.PBO.Poses[6].AngleDictionary[hand.LB]    = hand.PoseL;
				//NPC.PBO.Poses[6].AngleDictionary[hand.uArmL] = hand.PoseU;

				//hand.LB.Broken    = false;
				//hand.uArmL.Broken = false;
			}

			else
			{
				//Puppet.RigidReset(hand.RB); 
				//Puppet.RigidReset(hand.uArm.rigidbody);

				//Puppet.ResetPoses(hand.LB);
				//Puppet.ResetPoses(hand.uArmL);

				//if (!FH.Flags[0] && !BH.Flags[0]) Puppet.ResetPoses();
				//if (hand.Tool != null) hand.Tool.rigidSnapshot.Reset(hand.Tool.R);
			}

		}


		IEnumerator IResetPosition( bool noRotate=false, bool extraFlip=false )
		{
			if (!IsHolding || !Tool.P) yield break;

			PM3 qResp;
			float ieTimeout = Time.time + 5;
			do
			{
				qResp = NPC.IsHoldPaused(ieTimeout, false);
				if (qResp == PM3.Timeout) yield break;
				if (qResp == PM3.Error) yield break;
				if (qResp == PM3.Fail ) yield return new WaitForEndOfFrame();
			
			} while(qResp == PM3.Fail);
			
			NPC.PauseHold = true;

			//if ( false && this == NPC.BH && NPC.FH.IsHolding && (!NPC.BH.Tool || (NPC.FH.Tool && NPC.FH.Tool == NPC.BH.Tool)) )
			if (AltHand.IsHolding && AltHand.Tool == Tool)
			{
				NPC.PauseHold = false;
				StartCoroutine( IDualGrip() );
			}
			else
			{

				bool hideAlt = false;
				int altLayer = 9;
				
				//AltHand.Validate();
				
				if ( AltHand.IsHolding && AltHand.Tool != Tool )
				{
					//	Prevent weirdness when lined up Grips are activated
					altLayer = AltHand.Tool.G.layer;
					AltHand.Tool.gameObject.SetLayer( 2 );
					yield return new WaitForFixedUpdate();
				}

				bool ToolFlipped           = Tool.IsFlipped;

			
				if (extraFlip) ToolFlipped = !ToolFlipped;

				if (NPC.IsFlipped != ToolFlipped) Tool.Flip();
				//yield return new WaitForFixedUpdate();


				if (Tool.angleHold == 0f)
				{
					Tool.angleHold = (Tool.props.holdToSide && !noRotate) ? 5.0f : 95.0f;
					Tool.props.angleAim  = 95.0f;
				}

				float ToolRotation = IsAiming ? Tool.props.angleAim : Tool.angleHold;

				ToolRotation += Tool.props.angleOffset;

				Vector3 hpos = Tool.HoldingPosition;
				
				Tool.T.rotation = Quaternion.Euler(0.0f, 0.0f, Tool.IsFlipped ? RB.rotation + ToolRotation : RB.rotation - ToolRotation);

				Tool.T.position += GB.transform.TransformPoint(GB.GripPosition) - Tool.T.TransformPoint((Vector3)hpos);



				//hand.Tool.T.rotation = Quaternion.Euler( 0.0f, 0.0f, hand.Tool.IsFlipped ? hand.RB.rotation + hand.Tool.angleHold : hand.RB.rotation - hand.Tool.angleHold );

				//hand.Tool.T.position += hand.GB.transform.TransformPoint( hand.GB.GripPosition ) - hand.Tool.T.TransformPoint( (Vector3)hpos );
				
				GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

				yield return new WaitForFixedUpdate();

				Tool.T.rotation = Quaternion.Euler( 0.0f, 0.0f, Tool.IsFlipped ? RB.rotation + Tool.angleHold : RB.rotation - Tool.angleHold );
				
				if ( hideAlt )
				{
					yield return new WaitForFixedUpdate();

					AltHand.Tool.G.SetLayer( altLayer );

					yield return new WaitForFixedUpdate();
				}


				NPC.PauseHold = false;
			}
			
			FixLayer();


			if ( Tool.HoldingPosition != Tool.AltHoldingPosition )
			{
				AltHand.Tool     = Tool; 
				AltHand.IsHolding = true;
				StartCoroutine( AltHand.IDualGrip() );
			}


			//if (FH.IsHolding) ModAPI.Notify("FrontHand: Isholding:" + FH.IsHolding + " + Tool: " + (FH.Tool ? "yes" : "no") + " + Tool.P: " + (FH.Tool.P ? FH.Tool.P.name : "No"));
			//if (BH.IsHolding) ModAPI.Notify("BackHand: Isholding:" + BH.IsHolding + " + Tool: " + (BH.Tool ? "yes" : "no") + " + Tool.P: " + (BH.Tool.P ? BH.Tool.P.name : "No"));
		}

		IEnumerator IDualGrip()
		{

			PM3 qResp;
			float ieTimeout = Time.time + 5;
			do
			{
				qResp = NPC.IsHoldPaused(ieTimeout, true);
				if (qResp == PM3.Timeout) yield break;
				if (qResp == PM3.Error) yield break;
				yield return new WaitForEndOfFrame();
			
			} while(false && qResp == PM3.Fail);

			NPC.PauseHold          = true;
			
			//if (!AltHand.Validate() )
			//{
			//	PauseHold = false;
			//	yield break;
			//}
		
			IsHolding     = true;
			Tool         = AltHand.Tool;
	
			xxx.ToggleWallCollisions(AltHand.Tool.T);

			AltHand.Tool.P.MakeWeightless();


			//if ( AltHand.Tool.P.HoldingPositions.Length == 1 )
			//{
			//	Vector2 hPos = (Vector2)altHand.Tool.AltHoldingPosition;// + (Random.insideUnitCircle * 0.1f);


			//	//	Define the special holding position

			//}

			Vector3 dir;
			Vector3 dir2;
			
			float timeout;
			float dist          = 1;
			float lastDist      = 1;

		
			//foreach ( Vector3 hPos in new Vector3[] {AltHand.Tool.AltHoldingPosition, AltHand.Tool.HoldingPosition} )
			//{
				
				float minDist       = float.MaxValue;
				int attempts		= 0;

					NPC.LB["LowerArm"].Broken = NPC.LB["LowerArmFront"].Broken = NPC.LB["UpperArm"].Broken = NPC.LB["UpperArmFront"].Broken = true;
				
					//Puppet.RunRigids(Puppet.RigidReset);
				
					yield return new WaitForFixedUpdate();

					RB.drag          = 0.01f;
					RB.inertia       = 0.01f;
					Tool.R.drag      = 0.01f;
					Tool.R.inertia	 = 0.01f;
				
					//RB.drag        = 0.01f;
					//AltHand.uArm.rigidbody.drag    = 0.01f;
			
				timeout = Time.time + 2;

				do { 
					//if (!AltHand.Validate() )
					//{
					//	PauseHold = false;
					//	yield break;
					//}

					AltHand.Tool.R.velocity  = Vector3.zero;
					RB.velocity              = Vector3.zero;

					lastDist                 = dist;
					//if (H.Tool == null)
					//{
					//	PauseHold = false;
					//	yield break;
					//}
					
		
					dir = (NPC.RB["UpperBody"].position - AltHand.Tool.R.position) + (Vector2.right * 0.1f * -Facing);
					AltHand.Tool.R.AddForce( dir * Time.fixedDeltaTime * 1000f);

					dir2   = AltHand.Tool.T.TransformPoint(AltHand.Tool.AltHoldingPosition) - RB.transform.TransformPoint(GB.GripPosition);

					RB.AddForce(dir2 * Time.fixedDeltaTime * 1000f);
					
					dist   = Mathf.Abs(Vector2.Distance(GB.transform.TransformPoint(GB.GripPosition), AltHand.Tool.T.TransformPoint(AltHand.Tool.AltHoldingPosition)));
					
					if (dist < minDist) 
					{
						minDist  = dist;
						attempts = 0;

					} else if (dist <= minDist + 0.01f)
					{
						if (++attempts > 5) break; 
					}

					if (Time.time > timeout) break; 

					yield return new WaitForFixedUpdate();
					//if ( !AltHand.Validate() )
					//{
					//	PauseHold = false;
					//	yield break;
					//}
				
				} while (dist > 0.1f || dist < lastDist);
				
				
			//	++hPosId;

			//	if (!failedGrab) {
			//		break;
			//	}
			//}
		
			GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
			
			//AltHand.Tool.Reset();
			
			//AltHand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
			
			yield return new WaitForEndOfFrame();
			
			//AltHand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
			
			LB.Broken = AltHand.LB.Broken = uArmL.Broken = AltHand.uArmL.Broken = false;
			
			Tool                 = AltHand.Tool;
			NPC.PauseHold         = false;

			AltHand.FixLayer();

			DualWield = AltHand.DualWield = true;

			ConfigHandForAiming(true);
			AltHand.ConfigHandForAiming(true);
			Tool.T.rotation = Quaternion.Euler( 0.0f, 0.0f, Tool.IsFlipped ? RB.rotation + Tool.angleHold : RB.rotation - Tool.angleHold );
			yield return null;

		}
	}
}
