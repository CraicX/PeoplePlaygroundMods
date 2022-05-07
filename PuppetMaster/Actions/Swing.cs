//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using UnityEngine;
using System.Collections.Generic;

namespace PuppetMaster
{
	public enum SwingState
	{
		ready,
		swinging,
	};

	public class Swing
	{
		public int frame              = 0;
		public Puppet Puppet          = null;
		private float facing           = 1;
		private float timeNoSwing      = 0;
		private float force            = 0;

		public MoveSet SwingPose;
		public MoveSet SwingPoseF;
		public MoveSet SwingPoseB;


		private SwingState _state = SwingState.ready;
		public SwingState State {  
			get { return _state; }
			set { 
				_state = value; 
				if (value != SwingState.ready) PuppetMaster.ChaseCam.FreezeY = true;
			}
		}


		public void Init()
		{
			if (State == SwingState.ready)
			{
				Puppet.GetUp = false;

				foreach ( LimbBehaviour limb in Puppet.LB.Values )
				{
					if ((bool)limb?.IsOnFloor) { Puppet.HasTouchedGround=true; return ; }
				}

				State                        = SwingState.swinging;
				frame                        = 0;
				facing                       = Puppet.FacingLeft ? -1 : 1;

				Puppet.BlockMoves  = true;
				Puppet.IsSwinging  = true;

				Puppet.HasTouchedGround = false;

				SwingPose= new MoveSet("swing", false);
				SwingPose.Ragdoll.ShouldStandUpright       = false;
				SwingPose.Ragdoll.State                    = PoseState.Sitting;
				SwingPose.Ragdoll.Rigidity                 = 5.2f;
				SwingPose.Ragdoll.AnimationSpeedMultiplier = 12.5f;
				SwingPose.Import();

				SwingPoseF = new MoveSet("swing_f", false);
				SwingPoseF.Ragdoll.ShouldStandUpright       = false;
				SwingPoseF.Ragdoll.State                    = PoseState.Sitting;
				SwingPoseF.Ragdoll.Rigidity                 = 2.2f;
				SwingPoseF.Ragdoll.AnimationSpeedMultiplier = 4.5f;
				SwingPoseF.Import();

				SwingPoseB = new MoveSet("swing_b", false);
				SwingPoseB.Ragdoll.ShouldStandUpright       = false;
				SwingPoseB.Ragdoll.State                    = PoseState.Sitting;
				SwingPoseB.Ragdoll.Rigidity                 = 4.2f;
				SwingPoseB.Ragdoll.AnimationSpeedMultiplier = 4.5f;
				SwingPoseB.Import();

				Puppet.RunRigids(Puppet.RigidInertia, 0.1f);

			}
		}



		//
		// ─── SWING GO ────────────────────────────────────────────────────────────────
		//
		public void Go()
		{
			if ( ++frame % 20 == 0 )
			{
				foreach ( LimbBehaviour limb in Puppet.LB.Values )
				{
					if ((bool)limb?.IsOnFloor) {
						Puppet.HasTouchedGround = true;
						Stop();
						return;
					}
				}
			}



			if (KB.Left || KB.Right)
			{
				force = KB.Modifier ? 5f : 1.5f;
				timeNoSwing = 0;
				if ( (facing == 1 && KB.Right) || (facing == -1 && KB.Left) )
				{
					if (KB.Modifier) SwingPose.RunMove();
					else SwingPoseF.RunMove();
				}
				else SwingPoseB.RunMove();

				Vector2 direction = KB.Right ? Vector2.right : Vector2.left;

				if (KB.Up)          direction += Vector2.up;
				else if (KB.Down)   direction += Vector2.down;

				Puppet.RB2["MiddleBody"].AddForce(direction * Puppet.TotalWeight * force);

			} else
			{
				timeNoSwing += Time.fixedDeltaTime;

				if (timeNoSwing > 1f) Stop();
			}

		}

		public void Stop()
		{
			State             = SwingState.ready;
			Puppet.IsSwinging = false;
			Puppet.BlockMoves = false;
			SwingPose.ClearMove();
			Puppet.RunRigids(Puppet.RigidReset);
		}

	}
}
