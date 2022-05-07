//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using UnityEngine;
namespace PuppetMaster
{
	public enum DiveState
	{
		ready,
		start,
		diving,
		landed,
	};

	public class Dive
	{
		public Puppet Puppet          = null;
		public bool DoMaxPayne        = false;
		public int frame              = 0;
		public float force            = 0;
		public float facing           = 1;
		private bool handPlant        = false;
		private bool keepFlipping     = true;

		private bool isAndroid = false;

		private MoveSet ActionPose;

		private XTimer xtimer;

		private DiveState _state = DiveState.ready;

		public DiveState State
		{
			get { return _state; }
			set
			{
				_state = value;
				//Puppet.IsInvincible = _state != DiveState.ready;
				if (_state != DiveState.ready) Puppet.IsInvincible = true;
				else {
					if (!KB.Left && !KB.Right) Puppet.GetUp = true;
					PuppetMaster.Master.AddTask(Puppet.Invincible, 1f, false);
					Puppet.PG.PauseDualWielding = false;
				}
			}
		}

		//
		// ─── DIVE INIT ────────────────────────────────────────────────────────────────
		//
		public void Init()
		{
			if (State == DiveState.ready)
			{
				Puppet.PG.PauseDualWielding = true;
				xtimer = new XTimer();
				Puppet.GetUp = false;

				Puppet.RunRigids(Puppet.RigidReset);

				if (!Puppet.LB["Foot"].IsOnFloor && !Puppet.LB["FootFront"].IsOnFloor) return;
				if (Puppet.JumpLocked || Puppet.DisabledMoves) return;

				isAndroid = Puppet.LB["Head"].IsAndroid;

				ActionPose = new MoveSet("dive", false);

				ActionPose.Ragdoll.Rigidity                 = 3.2f;
				ActionPose.Ragdoll.AnimationSpeedMultiplier = 11.5f;
				ActionPose.Ragdoll.UprightForceMultiplier   = 0f;

				ActionPose.Import();

				facing       = Puppet.Facing;
				State        = DiveState.start;
				force        = 0;
				frame        = 0;
				handPlant    = false;
				keepFlipping = true;

				if (KB.Modifier)
				{
					Util.MaxPayne(true);
					DoMaxPayne = true;
				}

				Puppet.Invincible(true);
				Puppet.DisableMoves = Time.time + 2.5f;
				Puppet.JumpLocked   = true;

				if (Puppet.HandThing != null && (bool)Puppet.HandThing?.isEnergySword) Puppet.HandThing?.TurnItemOff();
				Puppet.pauseAiming = true;

			}
		}

		//
		// ─── DIVE GO ────────────────────────────────────────────────────────────────
		//
		public void Go()
		{
			frame++;
			if (KB.Up) force++;

			if (State == DiveState.start)
			{
				if (!xtimer.flag)
				{
					xtimer.flag = true;
					ActionPose.RunMove();
				}
				if (xtimer.time < 0.5f)
				{
					Puppet.RB2["LowerBody"].AddForce(Vector2.down * Time.fixedDeltaTime * Puppet.TotalWeight * 100f);
				}
				else
				if (xtimer.time < 1.0f){
					if (xtimer.counter == 0)
					{
						float force = 4;
						if (isAndroid) force = 5;
						xtimer.IncCount();
						Puppet.RB2["UpperBody"]?.AddForce(((Vector2.right * -facing * 1.5f) + Vector2.up ) * Puppet.TotalWeight * force, ForceMode2D.Impulse);
						if (isAndroid) Puppet.RB2["MiddleBody"]?.AddTorque(60f * -facing * Puppet.TotalWeight);
						ActionPose.ClearMove();
					}
					Puppet.RB2["Head"]?.AddForce(Vector2.right * -facing * Time.fixedDeltaTime * (100 * Puppet.TotalWeight));
					Puppet.RB2["LowerArm"]?.AddForce(Vector2.right * -facing * (1 * Puppet.TotalWeight));
					if (!Puppet.IsAiming) Puppet.RB2["LowerArmFront"]?.AddForce(Vector2.right * -facing * (1 * Puppet.TotalWeight));
					//Puppet.RB2["MiddleBody"]?.AddForce((Vector2.up + (Vector2.right * -facing)) * (100 * Time.fixedDeltaTime * Puppet.TotalWeight));
					//Puppet.RB2["UpperBody"]?.AddForce(Vector2.up * (100 * Time.fixedDeltaTime * Puppet.TotalWeight));
				}
				else if (xtimer.counter == 1)
				{
					xtimer.IncCount();
					ActionPose.ClearMove();
				}


				if (xtimer.time > 1.0f)
				{
					Puppet.RB2["UpperArm"]?.AddForce(Vector2.right * -facing  * (1 * Puppet.TotalWeight));
					if (!Puppet.IsAiming) Puppet.RB2["UpperArmFront"]?.AddForce(Vector2.right * -facing * (1 * Puppet.TotalWeight));
					//Puppet.RB2["UpperBody"]?.AddForce(((Vector2.right * -facing) + Vector2.up)  * (25 * Puppet.TotalWeight));

					if (xtimer.time > 1.1f) {
						State = DiveState.diving;
						frame = 0;
					}

					return;
				}
			}


			if (State == DiveState.diving)
			{
				if (frame == 10) Puppet.PBO.OverridePoseIndex = -1;

				if (frame > 10)
				{
					frame = 0;
					State = DiveState.landed;
					return;
				}
			}


			if (State == DiveState.landed)
			{
				Puppet.DisableMoves = Time.time + 0.5f;

				if (Puppet.LB["LowerArm"].IsOnFloor || Puppet.LB["LowerArmFront"].IsOnFloor)
				{
					if (keepFlipping && handPlant && (KB.Right || KB.Left))
					{
						Puppet.RB2["UpperBody"].AddForce(Vector2.up * 5 * Puppet.TotalWeight);
						Puppet.RB2["LowerBody"].AddTorque(facing * Puppet.TotalWeight * 15);
					}

					if (!handPlant)
					{
						Vector2 tempY = Puppet.RB2["UpperBody"].velocity;
						tempY.y *= 0f;

						Puppet.RB2["UpperBody"].velocity = tempY;

						handPlant = true;
					}
				}

				if (Puppet.LB["Foot"].IsOnFloor || Puppet.LB["FootFront"].IsOnFloor)
				{
					if (keepFlipping && (KB.Right || KB.Left))
					{
							Puppet.RB2["UpperBody"].AddForce(Vector2.up * 25 * Puppet.TotalWeight);
							Puppet.RB2["LowerBody"].AddTorque(facing * Puppet.TotalWeight * 15);
							return;
					}

					if (DoMaxPayne) PuppetMaster.Master.AddTask(Util.MaxPayne, 0.5f, false); 

					Puppet.RB2["UpperBody"].velocity *= 0.0001f;
					Puppet.RB2["LowerBody"].velocity *= 0.0001f;
					Puppet.RB2["MiddleBody"]?.AddTorque(50f * -facing * Puppet.TotalWeight);

					Puppet.PBO.OverridePoseIndex = 0;
					State                        = DiveState.ready;
					Puppet.JumpLocked            = false;

					ActionPose.ClearMove();

					PuppetMaster.Master.AddTask(Finale, 1.5f);

				} else if (Puppet.LB["Head"].IsOnFloor) keepFlipping = false;
			}
		}

		public void Finale()
		{
			Puppet.Invincible(false);
			Puppet.RunLimbs(Puppet.LimbHeal);
			Puppet.RunRigids(Puppet.RigidReset);
			ActionPose.ClearMove();
			if (Puppet.HandThing != null && (bool)Puppet.HandThing?.isEnergySword) Puppet.HandThing?.TurnItemOn(true);
			Puppet.pauseAiming = false;
		}

	}

}
