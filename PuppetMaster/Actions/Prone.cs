//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System.Collections.Generic;
using UnityEngine;

namespace PuppetMaster
{
	public enum ProneState
	{
		ready,
		start,
		drop,
		final,
	};

	public class Prone
	{
		public ProneState state       = ProneState.ready;
		public Puppet Puppet          = null;
		public float facing           = 1;
		public int frame              = 0;

		private MoveSet ActionPose;


		//
		// ─── PRONE INIT ────────────────────────────────────────────────────────────────
		//
		public void Init()
		{
			Puppet.GetUp = false;
			frame = 0;

			if (state == ProneState.ready)
			{
				frame  = 0;
				state  = ProneState.start;
				facing = Puppet.Facing;



				ActionPose = new MoveSet("prone", false);


				ActionPose.Ragdoll.ShouldStandUpright       = false;
				ActionPose.Ragdoll.State                    = PoseState.Sitting;
				ActionPose.Ragdoll.Rigidity                 = 2.2f;
				ActionPose.Ragdoll.AnimationSpeedMultiplier = 10.5f;

				ActionPose.Import();

				Puppet.PBO.OverridePoseIndex = 8;

				Puppet.RB2["UpperBody"].AddForce(Vector2.up * 5 * Puppet.TotalWeight);

				frame = 0;

				if (state == ProneState.ready && Puppet.PBO.OverridePoseIndex != ActionPose.poseID)
				{
					frame = 0;
					state = ProneState.start;
					facing = Puppet.FacingLeft ? -1 : 1;

					Puppet.RB2["UpperBody"].AddForce(Vector2.up * 5 * Puppet.TotalWeight);
				}
			}

		}


		//
		// ─── PRONE GO ────────────────────────────────────────────────────────────────
		//
		public void Go()
		{
			frame++;

			if (state == ProneState.start)
			{
				Puppet.RB2["UpperBody"].AddForce(Vector2.up * 25 * Puppet.TotalWeight);

				state = ProneState.drop;
				frame = 0;

				Puppet.IsCrouching = false;
				return;
			}

			if (state == ProneState.drop)
			{

				if (frame == 1) { 
					Puppet.RB2["UpperBody"].AddForce(Vector2.right * -facing * 50 * Puppet.TotalWeight);
					Puppet.RB2["LowerLeg"].AddForce(Vector2.right * facing * 5 * Puppet.TotalWeight);
					Puppet.RB2["LowerLegFront"].AddForce(Vector2.right * facing * 5 * Puppet.TotalWeight);
					Puppet.RB2["LowerBody"].AddForce(Vector2.up * 15 * Puppet.TotalWeight);
					Puppet.RB2["FootFront"].AddForce(Vector2.right * facing * 5 * Puppet.TotalWeight);
					Puppet.RB2["Foot"].AddForce(Vector2.right * facing * 5 * Puppet.TotalWeight);
				}

				if (frame >= 5)
				{
					ActionPose.RunMove();

					state = ProneState.final;
					frame = 0;
					return;
				}
			}

			if (state == ProneState.final)
			{
				if ( Puppet.PG.IsAiming && Puppet.PG.FH.HoldStyle == HoldStyle.Dual) {
					Puppet.PG.FH.uArmL.Broken                     = Puppet.PG.BH.uArmL.Broken = true;
					Puppet.PG.FH.uArm.rigidbody.angularVelocity   = Puppet.PG.BH.uArm.rigidbody.angularVelocity = Puppet.PG.FH.Thing.R.angularVelocity = 0.0f;
					Puppet.PG.FH.uArm.rigidbody.drag              = Puppet.PG.BH.uArm.rigidbody.drag = Puppet.PG.FH.Thing.R.drag = 0.1f; 
					Puppet.PG.FH.uArm.rigidbody.angularDrag       = Puppet.PG.BH.uArm.rigidbody.angularDrag = Puppet.PG.FH.Thing.R.angularDrag = 0.1f; 
				}
				if (Puppet.PBO.OverridePoseIndex != ActionPose.poseID)
				{
					state = ProneState.ready;

				}
			}
		}
	}
}
