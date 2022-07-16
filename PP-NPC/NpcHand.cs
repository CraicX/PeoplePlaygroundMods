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
using System;

namespace PPnpc
{
	[SkipSerialisation]
	public class NpcHand : MonoBehaviour
	{
		private bool _isAiming = false;
		public bool IsPrimaryGrip = true;
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
		public float PoseSkillOffset = 1f;
		public FixedJoint2D GrabJoint;
		public Coroutine GrabAct;
		public int checkGrip = 0;

		int myPointID = 0;

		public SwingState swingState = SwingState.idle;
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
				if (value && value.transform) { 
					_aimAt = value; 

					AimTarget = AimAt.transform.position;

					NPC.Mojo.Feel("Bored", -1f);

					if (NPC.PBO) NPC.PBO.AdrenalineLevel += 0.1f;
				}
			}
		}


		public void FixHandsPose( int poseId = 6 )
		{
			//NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[LB]    = PoseL;
			//NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[uArmL] = PoseU;
			//NPC.PBO.Poses[poseId].AngleDictionary[LB]    = PoseL;
			//NPC.PBO.Poses[poseId].AngleDictionary[uArmL] = PoseU;
		}



		public bool Target( PhysicalBehaviour pb )
		{
			IsAiming = true;
			AimAt    = pb;

			if ( Time.time > NextFireTime )
			{
				NextFireTime = Time.time + FireRate; // Time.time + xxx.rr(FireRate, FireRate * 10);
				//Tool.Activate( Tool.isAutomatic);
			}

			//uArmL.Broken = true;
			return true;
		}

		void FixedUpdate()
		{
			if (!NPC.Active) return;
			if ( _isAiming )
			{
				V2Pos = NPC.Head.position * Facing;

				if ( AimTarget != null &&  ( AimTarget.x * Facing < V2Pos.x - 0.5f ) && ( Mathf.Abs( Vector2.Distance( AimTarget * Facing, V2Pos ) ) > 1.5f ) )
				{
					AimWeapon();
				}
			}
		}

		//public void Drop()          => PG.Drop(this);
		public bool CanAim            => (bool)(Tool != null && Tool.CanAim    && LB != null && LB.IsConsideredAlive); 
		public bool CanAttack         => (bool)(Tool != null && Tool.CanStrike && LB != null && LB.IsConsideredAlive); 
		public string HandId          => (this == NPC.FH) ? "Front" : "Back";
		public string HandShortId     => (this == NPC.FH) ? "fh" : "bh";
		public NpcHand AltHand        => (this == NPC.FH) ? NPC.BH : NPC.FH;




		public void Init()
		{
			string armID   = name.Contains("Front") ? "ArmFront" : "Arm";
			NPC            = gameObject.transform.root.GetComponent<NpcBehaviour>();
			GB             = gameObject.GetComponent<GripBehaviour>();
			LB	           = NPC.LB["Lower" + armID];
			RB             = NPC.RB["Lower" + armID];
			PB             = LB.PhysicalBehaviour;
			uArmL          = NPC.LB["Upper" + armID];
			uArm           = uArmL.PhysicalBehaviour;
			T              = LB.transform;
			//PoseL        = NpcPose.FreeLimbPose(LB, true);
			//PoseU        = NpcPose.FreeLimbPose(uArmL, true);
			PoseL          = NPC.PBO.Poses[0].AngleDictionary[LB];
			PoseU          = NPC.PBO.Poses[0].AngleDictionary[uArmL];
			IsHolding      = false;
			Tool           = null;
			_isAiming	   = false;
			DualWield      = false;
			IsFiring       = false;
			IsAiming       = false;
			FireAtWill	   = false;

			if ( GB.isHolding )
			{
				PhysicalBehaviour item = (PhysicalBehaviour)GB.CurrentlyHolding;
				Tool                   = item.gameObject.GetOrAddComponent<NpcTool>();
				IsHolding              = true;
				Tool.SetTool(item, this);
			}

			StartCoroutine(IValidate());
		}


		public void AimWeapon()
		{
			if (!NPC)
			{
				if (Tool) UnityEngine.Object.Destroy(Tool);
				UnityEngine.Object.Destroy(this);
				return;
			}

			if (Vector2.Dot( NPC.Head.up, Vector2.down ) > 0.1f ) {
				return;
			}

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
				IsAiming = false;
				return;
			}

			if (Time.frameCount % 10 == 0 || AimTarget == null) AimTarget  = AimAt.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * (NPC.Config.AimingSkill * PoseSkillOffset);
			switch ( Tool.props.CurrentAimStyle )
			{
				case AimStyles.Proned:
					AimTarget.y += 0.1f;
					PoseSkillOffset = 0.3f;
					break;

				case AimStyles.Crouched:
					AimTarget.y += 0.01f;
					PoseSkillOffset = 0.6f;
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
			Vector3 E3Pos;

			if (AltHand.IsAiming && this.HandId == "Back")
			{
				Vector3 Offset = UnityEngine.Random.insideUnitCircle;
				E3Pos    = ((AimTarget - T.position) * -NPC.Facing) + Offset;

			}


			E3Pos    = ((AimTarget - T.position) * -NPC.Facing);

			E3Pos.y += fDist;

			float fAngle   = Mathf.Atan2(E3Pos.y, E3Pos.x) * Mathf.Rad2Deg;

			if (DualWield) RB.AddForce(E3Pos.normalized * NPC.TotalWeight * -NPC.Facing * Time.fixedDeltaTime * 35f);

			if (!Tool.T) { IsAiming = false; return; }

			float fNum     = Mathf.DeltaAngle(Tool.T.eulerAngles.z, fAngle)  * Time.fixedDeltaTime * 15f;

			if (Time.frameCount % 1000 == 0) fDist = float.MinValue;

			if ( !float.IsNaN( fNum ) ) Tool.R.AddTorque(Mathf.Clamp(fNum * NPC.TotalWeight, -30f, 30f));

			if (FireAtWill && (Tool.props.isAutomatic || Time.time > NextFireTime)) {
				NextFireTime = Time.time + xxx.rr(FireRate, FireRate * 2);
				if (GB.isHolding) {
					Tool.Activate(Tool.props.isAutomatic);
					NPC.Mojo.Feel("Bored",-5f);
					NPC.Mojo.Feel("Angry",-1f);
					NPC.Mojo.Feel("Fear",-5f);
					NPC.PBO.AdrenalineLevel = Mathf.Clamp(NPC.PBO.AdrenalineLevel + 1, 1, 5);
				}
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

				//	Check if its currently touching any other npc
				NpcBehaviour[] npcs = UnityEngine.Object.FindObjectsOfType<NpcBehaviour>();

				foreach ( NpcBehaviour otherNpc in npcs )
				{
					if (NPC == otherNpc) continue;

					if (xxx.IsColliding(otherNpc.Head, item.transform)) return false;

					if (otherNpc.FH.Tool && otherNpc.FH.Tool.P == item) return false;
					if (otherNpc.BH.Tool && otherNpc.BH.Tool.P == item) return false;
				}


			}

			return true;
		}

		public bool Validate()
		{
			if (!GB || !GB.isHolding)
			{
				Tool      = null;
				IsHolding = false;
				return false;
			} else if (Tool && Tool.Hand && Tool.Hand.NPC != NPC ) 
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
				if (Tool && Tool.Hand && Tool.Hand.NPC != NPC ) Drop();
				Check();
				ResetGlobals();

				if (IsHolding && ++checkGrip > 10) { CheckGrips(); checkGrip = 0;}

				yield return new WaitForSeconds(1);
			}

		}


		public void CheckGrips()
		{
			if (!Tool || !Tool.P || !IsPrimaryGrip) return;
			
			float tr         = Mathf.RoundToInt(RB.rotation + (95f * Tool.Facing));
			Vector3 hpos     = Tool.HoldingPosition;
			Vector3 alphaPos = GB.transform.TransformPoint(GB.GripPosition) - Tool.T.TransformPoint((Vector3)hpos);
			float xrotate    = Mathf.Abs(Tool.R.rotation - tr );

			float threshHold = AltHand.IsPrimaryGrip ? 0.1f : 0.3f;

			if (xrotate > 3f || alphaPos.x > threshHold || alphaPos.y > threshHold)
			{
				//	weapon position is off, should reposition grip
				Debug.Log("GRIPS rotate: " + xrotate + "   Gpos: " + alphaPos);
				if (AltHand.Tool == Tool) AltHand.Drop();
				GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
				xxx.ToggleCollisions(Tool.transform, NPC.Head.root, false, false);
				StartCoroutine(IResetPosition());
			}
			
			 

			
			
		}

		public void Hold( Props prop )
		{
			if (!prop.P || !xxx.CanHold(prop.P)) return;

			NPC.Actions.RecacheMoves = true;

			if ((!NPC.EnhancementFirearms && prop.canShoot) ||
				(!NPC.EnhancementMelee && prop.canStrike)){

				if (!NPC.ScannedPropsIgnored.Contains(prop)) NPC.ScannedPropsIgnored.Add(prop);
				return;
			}

			if (prop.canUpgrade)
			{
				NpcChip chip = prop.P.GetComponent<NpcChip>();
				bool need=  false;
				if (chip.ChipType == Chips.Memory && !NPC.EnhancementMemory) need = true;
				else if (chip.ChipType == Chips.Memory && !NPC.EnhancementMemory) need = true;
				else if (chip.ChipType == Chips.Troll && !NPC.EnhancementTroll) need = true;
				else if (chip.ChipType == Chips.Melee && !NPC.EnhancementMelee) need = true;
				else if (chip.ChipType == Chips.Hero && !NPC.EnhancementHero) need = true;
				else if (chip.ChipType == Chips.Karate && !NPC.EnhancementKarate) need = true;
				else if (chip.ChipType == Chips.Firearms && !NPC.EnhancementFirearms) need = true;

				if (!need) return;
			}

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
			if (prop.canShoot || prop.canStab || prop.canStrike) {
				NPC.Goals.Attack = true;
				NPC.Mojo.Feelings["Fear"] *= 0.1f;
			}

			if (prop.canRecruit) NPC.Goals.Recruit = true;
			if (prop.canUpgrade) NPC.Goals.Upgrade = true;
			if ( prop.Traits["sword"] ) NPC.HasSword = true;
			if (prop.canStab) NPC.HasKnife        = true;
			if ( prop.Traits["club"] ) {
				NPC.HasClub       = true;
			}
			if (prop.canExplode) NPC.HasExplosive = true;
			if (prop.canFightFire) NPC.HasFireF   = true;

		}



		public void Drop()
		{
			IsPrimaryGrip = AltHand.IsPrimaryGrip = true;

			if (Tool) Tool.Dropped();
			GB.DropObject();
			IsHolding = IsAiming = FireAtWill = false;

			ResetGlobals();
			NPC.Actions.RecacheMoves = true;
		}

		public void ResetGlobals()
		{
			NPC.Goals.Scavenge  = (!IsHolding || !AltHand.IsHolding) ;
			NPC.Goals.Attack    = (Tool && (Tool.props.canShoot || Tool.props.canStab || Tool.props.canStrike)) ||
			                      (AltHand.Tool && (AltHand.Tool.props.canShoot || AltHand.Tool.props.canStab || AltHand.Tool.props.canStrike)) ;

			NPC.Goals.Recruit   = (Tool && Tool.props.canRecruit)           || (AltHand.Tool && AltHand.Tool.props.canRecruit);
			NPC.Goals.Upgrade   = (Tool && Tool.props.canUpgrade)           || (AltHand.Tool && AltHand.Tool.props.canUpgrade); 
			NPC.HasKnife        = (Tool && Tool.props.Traits["knife"] ) || (AltHand.Tool && AltHand.Tool.props.Traits["knife"] );
			NPC.HasExplosive    = (Tool && Tool.props.canExplode)           || (AltHand.Tool && AltHand.Tool.props.canExplode);
			NPC.HasFireF        = (Tool && Tool.props.canFightFire)         || (AltHand.Tool && AltHand.Tool.props.canFightFire);
			NPC.HasClub         = (Tool && Tool.props.Traits["club"])   || (AltHand.Tool && AltHand.Tool.props.Traits["club"]);
			NPC.HasSword        = (Tool && Tool.props.Traits["sword"])  || (AltHand.Tool && AltHand.Tool.props.Traits["sword"]);
		}


		public void Stop()
		{
			IsAiming = IsFiring = FireAtWill = false;
			ConfigHandForAiming(false);
		}

		public void Check()
		{
			if ( Time.time < BypassCheck ) return;
			if ( GB.isHolding )
			{
				if (NPC.PBO.Consciousness < 0.5f)
				{
					Drop();
					GB.DropObject();
					return;
				}
				if (!IsHolding) { 
					IsHolding = true;
					if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + " " + HandId + " Check(): mismatch");
					if (!GB.CurrentlyHolding.gameObject.TryGetComponent<NpcTool>(out NpcTool tool)) {
						Tool = tool;

					} else { 

						Tool = GB.CurrentlyHolding.gameObject.AddComponent<NpcTool>();
					}

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
				uArm.rigidbody.drag = 1.01f;
				RB.drag             = 1.01f;
				if ( Tool && Tool.P ) Tool.R.drag = 1.01f;

				//uArm.rigidbody.drag = 1f;
				//RB.drag             = 1f;
				//if ( Tool && Tool.P ) Tool.R.drag = 1f;

				NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[LB]    = PoseL;
				NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[uArmL] = PoseU;
				NPC.PBO.Poses[6].AngleDictionary[LB]                          = PoseL;
				NPC.PBO.Poses[6].AngleDictionary[uArmL]                       = PoseU;

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
				NPC.ResetPoses(LB,uArmL);
				uArmL.Broken = LB.Broken = false;
				NPC.RigidReset(uArm.rigidbody);
				NPC.RigidReset(RB);
				//NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[LB]    = NpcBehaviour.WalkPose.AngleDictionary[LB];
				//NPC.PBO.LinkedPoses[PoseState.Walking].AngleDictionary[uArmL] = NpcBehaviour.WalkPose.AngleDictionary[uArmL];
				//NPC.PBO.Poses[6].AngleDictionary[LB]                          = NpcBehaviour.WalkPose.AngleDictionary[LB];
				//NPC.PBO.Poses[6].AngleDictionary[uArmL]                       = NpcBehaviour.WalkPose.AngleDictionary[uArmL];


				//Puppet.RigidReset(hand.RB); 
				//Puppet.RigidReset(hand.uArm.rigidbody);

				//Puppet.ResetPoses(hand.LB);
				//Puppet.ResetPoses(hand.uArmL);

				//if (!FH.Flags[0] && !BH.Flags[0]) Puppet.ResetPoses();
				//if (hand.Tool != null) hand.Tool.rigidSnapshot.Reset(hand.Tool.R);
			}

		}


		public Vector3[] PlotThrow(Vector2 pos, Vector2 velocity, int steps)
		{
			Vector3[] results    = new Vector3[steps];

			float timestep       = Time.fixedDeltaTime / Physics2D.velocityIterations;

			Vector2 gravityAccel = Physics2D.gravity * Tool.P.InitialGravityScale * timestep * timestep;

			//float drag          = 1f - timestep * initDrag;

			Vector2 moveStep     = velocity * timestep;

			RaycastHit2D hit;

			int i;

			for (i = 0; i < steps; i++)
			{
				moveStep += gravityAccel;

				pos += moveStep;
				if (i > 70) results[i - 71] = pos;
				if (i > 120)
				{
					hit = Physics2D.Raycast(pos, (pos + moveStep).normalized, 0.5f);

					if (hit.transform)
					{

						Array.Resize(ref results, i - 71);

						return results;
					}

				}



			}
			Array.Resize(ref results, i - 71);
			return results;
		}

		public IEnumerator IGrab( Rigidbody2D target, float delay=0 )
        {
			IsGrabbing = false;
			if (GrabJoint) UnityEngine.Object.DestroyImmediate((UnityEngine.Object) GrabJoint);

			ConfigHandForAiming(true);

			Vector2 dir;

			if (delay > 0) yield return new WaitForSeconds(delay);
			//LB.Broken = uArmL.Broken = true;
			do { 
				dir = Vector2.MoveTowards(T.position, target.position, 3f * Time.fixedDeltaTime);
				RB.MovePosition(dir);
				yield return new WaitForFixedUpdate();
			} while (-NPC.Facing * T.position.x < -NPC.Facing * target.position.x && Vector2.Distance(T.position, target.position) > 0.3f);
			if (!target) {
				LB.Broken = uArmL.Broken = false;
				yield break;

			}
			if (Vector2.Distance(T.position,target.position) > 0.3f) yield break;
		
			GrabJoint                 = RB.gameObject.AddComponent<FixedJoint2D>() as FixedJoint2D;
			GrabJoint.dampingRatio    = 1;
			GrabJoint.frequency       = 0;
			GrabJoint.connectedBody   = target.gameObject.GetComponent<Rigidbody2D>();
			GrabJoint.breakForce	  = 200f;
			GrabJoint.breakTorque     = 100f;
			GrabJoint.enabled         = true;

			IsGrabbing			      = true;
			LB.Broken                 = uArmL.Broken = false;
        }

		public bool IsGrabbing = false;

		

		public IEnumerator IPoint( Transform target, float offsetY=0f)
		{
			myPointID = xxx.rr(1,100);
			int myID = myPointID;

			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + " " + HandId + " IPoint()");
			if (!target) yield break;

			ConfigHandForAiming(true);

			if ( Tool && Tool.P )
			{
				Tool.BGhost.Add(target, Time.time);

				xxx.ToggleCollisions(Tool.T, target.root, true, false);

				//uArmL.Broken = LB.Broken =  true;

				Tool.T.rotation = Quaternion.Euler( 0.0f, 0.0f, Tool.IsFlipped ? RB.rotation + Tool.props.angleAim : RB.rotation - Tool.props.angleAim );

			}


			while(target && !NPC.CancelAction && myPointID == myID)
			{ 
				if (Time.frameCount % 10 == 0) ConfigHandForAiming(true);

				Vector3 E3Pos  = ((NPC.RB["MiddleBody"].transform.position - target.position) ).normalized;
				//E3Pos.y += offsetY;

				float fAngle   = Mathf.Atan2(E3Pos.y, E3Pos.x) * Mathf.Rad2Deg;

				if (Tool.T) { 

					float fNum     = Mathf.DeltaAngle(Tool.T.eulerAngles.z, fAngle)  * Time.fixedDeltaTime * 10f;

					if ( !float.IsNaN( fNum ) ) {
						Tool.R.AddTorque(Mathf.Clamp(fNum * NPC.TotalWeight, -15f, 15f));

						//V3Vel = target.position - 
						//uArm.rigidbody.MoveRotation(Quaternion.Euler(V3Vel));

						//RB.AddForce(V3Vel * NPC.TotalWeight * Time.fixedDeltaTime * 1000f);
					}

				} else
				{
					float fNum     = Mathf.DeltaAngle(T.eulerAngles.z, fAngle)  * Time.fixedDeltaTime * 10f;

					if ( !float.IsNaN( fNum ) ) RB.AddTorque(Mathf.Clamp(fNum * NPC.TotalWeight, -15f, 15f));
				}

				yield return new WaitForFixedUpdate();

			}

			if (Tool.T && target) { 

				Tool.T.rotation = Quaternion.Euler( 0.0f, 0.0f, Tool.IsFlipped ? RB.rotation + Tool.props.angleHold : RB.rotation - Tool.props.angleHold);

				NPC.NoGhost.Remove(target.root);

				ConfigHandForAiming(false);
			}
			else if (target)
			{
				ConfigHandForAiming(false);
			}
		}

		public IEnumerator IPointArm( Transform target, float seconds=10f )
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + " " + HandId + " IPoint()");
			if (!target) yield break;

			ConfigHandForAiming(true);

			Vector3 V3Pos;
			float fDist;
			Vector3 V3Vel;
			float timeout = Time.time + seconds;

			while(target && !NPC.CancelAction && Time.time < timeout)
			{ 
				if (Time.frameCount % 20 == 0) ConfigHandForAiming(true);

				V3Pos = (T.position - target.position);

				fDist = V3Pos.magnitude;

				V3Vel = new Vector3(0f, 0f, (Mathf.Atan2(V3Pos.y, V3Pos.x) * Mathf.Rad2Deg) - (85.5f + (16.5f * Facing)));

				uArm.rigidbody.MoveRotation(Quaternion.Euler(V3Vel));

				RB.AddForce(V3Vel * NPC.TotalWeight * Time.fixedDeltaTime * 1000f);

				yield return new WaitForFixedUpdate();
			}

			ConfigHandForAiming(false);
		}


		public IEnumerator IResetPosition( bool noRotate=false, bool extraFlip=false )
		{
			if (!NPC.PBO.IsAlive()) yield break;
			if (!IsHolding || Tool == null || !Tool.P) yield break;

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

			IsPrimaryGrip = true;

			bool hideAlt = false;
			int altLayer = 9;

			//AltHand.Validate();

			if ( AltHand.IsHolding && AltHand.Tool != Tool )
			{
				//	Prevent weirdness when lined up Grips are activated
				altLayer = AltHand.Tool.G.layer;
				AltHand.Tool.gameObject.SetLayer( 2 );
				yield return new WaitForFixedUpdate();
				hideAlt = true;
			}

			bool ToolFlipped           = Tool.IsFlipped;


			if (extraFlip) ToolFlipped = !ToolFlipped;

			if (NPC.IsFlipped != ToolFlipped) Tool.Flip();
			//yield return new WaitForFixedUpdate();

				


			if (Tool.props.angleHold == 0f)
			{
				Tool.props.angleHold = (Tool.props.holdToSide && !noRotate) ? 5.0f : 95.0f;
				Tool.props.angleAim  = 95.0f;
			}

			float ToolRotation = IsAiming ? Tool.props.angleAim : Tool.props.angleHold;

			ToolRotation += Tool.props.angleOffset;

			Vector3 hpos = Tool.HoldingPosition;

			Tool.T.rotation = Quaternion.Euler(0.0f, 0.0f, Tool.IsFlipped ? RB.rotation + ToolRotation : RB.rotation - ToolRotation);

			Tool.T.position += GB.transform.TransformPoint(GB.GripPosition) - Tool.T.TransformPoint((Vector3)hpos);

			Tool.P.MakeWeightless();

			//hand.Tool.T.rotation = Quaternion.Euler( 0.0f, 0.0f, hand.Tool.IsFlipped ? hand.RB.rotation + hand.Tool.angleHold : hand.RB.rotation - hand.Tool.angleHold );

			//hand.Tool.T.position += hand.GB.transform.TransformPoint( hand.GB.GripPosition ) - hand.Tool.T.TransformPoint( (Vector3)hpos );

			GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

			yield return new WaitForFixedUpdate();

			Tool.T.rotation = Quaternion.Euler( 0.0f, 0.0f, Tool.IsFlipped ? RB.rotation + Tool.props.angleHold : RB.rotation - Tool.props.angleHold );


			if ( Tool.HoldingPosition != Tool.AltHoldingPosition )
			{
				FixLayer();
				// DUAL GRIP POSE
				NpcPose gripPose = new NpcPose(NPC,"dualgrip_" + HandShortId, true);
				float timer = Time.time + 1.0f;
				AltHand.IsPrimaryGrip = false;
				ConfigHandForAiming(false);
				AltHand.ConfigHandForAiming(false);
				NPC.PBO.DesiredWalkingDirection = 0;
				NPC.PBO.OverridePoseIndex = -1;
				AltHand.ConfigHandForAiming(false);
				if ( AltHand.IsHolding )
				{
					AltHand.Drop();
					yield return new WaitForEndOfFrame();
				}
				
				while(Time.time < timer) { 
					gripPose.CombineMove();
					yield return new WaitForFixedUpdate();
				}

				AltHand.Tool      = Tool; 
				AltHand.IsHolding = true;
				AltHand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
				GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
				Tool.T.rotation = Quaternion.Euler(0.0f, 0.0f, Tool.IsFlipped ? RB.rotation + ToolRotation : RB.rotation - ToolRotation);
				Tool.T.position += GB.transform.TransformPoint(GB.GripPosition) - Tool.T.TransformPoint((Vector3)hpos);
				GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
			}


			if ( hideAlt )
			{
				yield return new WaitForFixedUpdate();

				AltHand.Tool.G.SetLayer( altLayer );

				yield return new WaitForFixedUpdate();
			}


			NPC.PauseHold = false;

			if ( Tool.HoldingPosition != Tool.AltHoldingPosition )
			{
					
				//StartCoroutine( AltHand.IDualGrip() );
			}

			FixLayer();





			//if (FH.IsHolding) ModAPI.Notify("FrontHand: Isholding:" + FH.IsHolding + " + Tool: " + (FH.Tool ? "yes" : "no") + " + Tool.P: " + (FH.Tool.P ? FH.Tool.P.name : "No"));
			//if (BH.IsHolding) ModAPI.Notify("BackHand: Isholding:" + BH.IsHolding + " + Tool: " + (BH.Tool ? "yes" : "no") + " + Tool.P: " + (BH.Tool.P ? BH.Tool.P.name : "No"));
		}

		public IEnumerator IDualGrip()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + " " + HandId + " IDualGrip()");
			PM3 qResp;
			float ieTimeout = Time.time + 5;
			do
			{
				qResp = NPC.IsHoldPaused(ieTimeout, true);
				if (qResp == PM3.Timeout) yield break;
				if (qResp == PM3.Error) yield break;
				yield return new WaitForEndOfFrame();

			} while(qResp == PM3.Fail);

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

			AltHand.IsPrimaryGrip = false;


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

			timeout = Time.time + 3;

			do { 
				//if (!AltHand.Validate() )
				//{
				//	PauseHold = false;
				//	yield break;
				//}

				AltHand.Tool.R.velocity  = Vector3.zero;
				AltHand.RB.velocity      = Vector3.zero;
				RB.velocity              = Vector3.zero;

				lastDist                 = dist;
				//if (H.Tool == null)
				//{
				//	PauseHold = false;
				//	yield break;
				//}


				dir = (NPC.RB["UpperBody"].position - AltHand.Tool.R.position) + (Vector2.right * 0.1f * -Facing);
				//AltHand.Tool.R.AddForce( dir * Time.fixedDeltaTime * 1000f);
				AltHand.Tool.R.AddForce( dir * NPC.TotalWeight * Time.fixedDeltaTime * 1000f);

				dir2   = AltHand.Tool.T.TransformPoint(AltHand.Tool.AltHoldingPosition) - RB.transform.TransformPoint(GB.GripPosition);

				//RB.AddForce(dir2 * Time.fixedDeltaTime * 1000f);
				RB.AddForce(dir2 * NPC.TotalWeight * Time.fixedDeltaTime * 1000f);

				dist   = Mathf.Abs(Vector2.Distance(GB.transform.TransformPoint(GB.GripPosition), AltHand.Tool.T.TransformPoint(AltHand.Tool.AltHoldingPosition)));

				if (dist < minDist) 
				{
					minDist  = dist;
					attempts = 0;

				} else if (dist <= minDist)  // #CHECKME
				//} else if (dist <= minDist + 0.01f)
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

			AltHand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
			GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

			//AltHand.Tool.Reset();





			Tool                 = AltHand.Tool;
			NPC.PauseHold         = false;

			AltHand.FixLayer();

			DualWield = AltHand.DualWield = true;

			ConfigHandForAiming(true);
			AltHand.ConfigHandForAiming(true);

			yield return new WaitForFixedUpdate();
			Tool.T.rotation = Quaternion.Euler( 0.0f, 0.0f, Tool.IsFlipped ? RB.rotation + Tool.props.angleHold : RB.rotation - Tool.props.angleHold );
			yield return new WaitForFixedUpdate();
			LB.Broken = AltHand.LB.Broken = uArmL.Broken = AltHand.uArmL.Broken = false;
			AltHand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
			IsHolding = true;

			yield return null;

		}
	}
}
