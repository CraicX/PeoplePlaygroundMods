//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
namespace PuppetMaster
{
	public enum CrouchState
	{
		ready,
		crouching,
		getup,
	};

	public class Crouch
	{
		public CrouchState state = CrouchState.ready;
		public Puppet Puppet     = null;

		private MoveSet ActionPose;


		//
		// ─── CROUCH INIT ────────────────────────────────────────────────────────────────
		//
		public void Init()
		{
			if (state == CrouchState.ready )
			{
				Puppet.GetUp = false;
				state      = CrouchState.crouching;
				ActionPose = new MoveSet("crouch", false);
				ActionPose.Ragdoll.ShouldStandUpright       = true;
				ActionPose.Ragdoll.Rigidity                 = 2.2f;
				ActionPose.Ragdoll.AnimationSpeedMultiplier = 10.5f;
				ActionPose.Import();
				ActionPose.RunMove();

				Puppet.PG.BlockArmPose(null, false);


			}
		}


		//
		// ─── PRONE GO ────────────────────────────────────────────────────────────────
		//
		public void Go()
		{
			if (state == CrouchState.crouching)
			{

				Puppet.IsCrouching = KB.Down;

				if (Puppet.IsCrouching)
				{
					if ( Puppet.PG.IsAiming && Puppet.PG.FH.HoldStyle == HoldStyle.Dual) {
						Puppet.PG.FH.uArmL.Broken                     = Puppet.PG.BH.uArmL.Broken = true;
						Puppet.PG.FH.uArm.rigidbody.angularVelocity   = Puppet.PG.BH.uArm.rigidbody.angularVelocity = Puppet.PG.FH.Thing.R.angularVelocity = 0.0f;
						Puppet.PG.FH.uArm.rigidbody.drag              = Puppet.PG.BH.uArm.rigidbody.drag = Puppet.PG.FH.Thing.R.drag = 0.1f; 
						Puppet.PG.FH.uArm.rigidbody.angularDrag       = Puppet.PG.BH.uArm.rigidbody.angularDrag = Puppet.PG.FH.Thing.R.angularDrag = 0.1f; 
					}

					if (KB.Left || KB.Right)
					{
						if (Puppet.FacingLeft != KB.Left && Puppet.IsReady) Puppet.Flip();
					}
				}
				else
				{
					if ( Puppet.PG.IsAiming )
					{
						Puppet.PG.FH.uArmL.Broken = Puppet.PG.BH.uArmL.Broken = false;
						Puppet.RunRigids( Puppet.RigidReset );
						Puppet.PG.FH.Thing.Reset();
					}
					state = CrouchState.ready;

					ActionPose.ClearMove();
					Puppet.PBO.OverridePoseIndex = 0;
				}
			}
		}
	}
}
