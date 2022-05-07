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
	public enum BackflipState
	{
		ready,
		start,
		flipping,
		landed,
	};

	public class Backflip
	{
		private BackflipState _state = BackflipState.ready;

		public BackflipState State
		{
			get { return _state; }
			set
			{
				_state = value;
				if (_state != BackflipState.ready) Puppet.IsInvincible = true;
				else {
					PuppetMaster.Master.AddTask(Puppet.Invincible, 1f, false);
					if (!KB.Left && !KB.Right) Puppet.GetUp = true;
					Puppet.PG.PauseDualWielding = false;
				}
			}
		}


		private XTimer xTimer          = new XTimer();
		public Puppet Puppet           = null;
		public int frame               = 0;
		public float force             = 0;
		private float facing           = 1;
		public MoveSet[] APBackflip    = new MoveSet[3];
		public float rotoSpeed         = 0f;
		private bool isAndroid         = false;

		//
		// ─── Backflip INIT ────────────────────────────────────────────────────────────────
		//
		public void Init()
		{
			if (State == BackflipState.ready)
			{
				Puppet.PG.PauseDualWielding = true;

				isAndroid = Puppet.LB["Head"].IsAndroid;
				Puppet.GetUp = false;

				Puppet.RunRigids(Puppet.RigidReset);

				xTimer = new XTimer();

				Puppet.GetUp = false;
				if (!Puppet.LB["Foot"].IsOnFloor && !Puppet.LB["FootFront"].IsOnFloor) return;
				if (Puppet.JumpLocked || Puppet.DisabledMoves) return;

				APBackflip[0] = new MoveSet("bflip_1", false);
				APBackflip[0].Ragdoll.ShouldStandUpright       = false;
				APBackflip[0].Ragdoll.Rigidity                 = 5.2f;
				APBackflip[0].Ragdoll.AnimationSpeedMultiplier = 12.5f;
				APBackflip[0].Import();

				APBackflip[1] = new MoveSet("bflip_2", false);
				APBackflip[1].Ragdoll.ShouldStandUpright       = false;
				APBackflip[1].Ragdoll.Rigidity                 = 2.2f;
				APBackflip[1].Ragdoll.AnimationSpeedMultiplier = 4.5f;
				APBackflip[1].Import();

				APBackflip[2] = new MoveSet("bflip_3", false);
				APBackflip[2].Ragdoll.ShouldStandUpright       = false;
				APBackflip[2].Ragdoll.Rigidity                 = 4.2f;
				APBackflip[2].Ragdoll.AnimationSpeedMultiplier = 4.5f;
				APBackflip[2].Import();


				State        = BackflipState.start;
				force        = 0;
				frame        = 0;

				facing = Puppet.Facing;

				if (KB.Modifier)
				{
					Util.MaxPayne(true);
				}

				Puppet.RunLimbs(Puppet.LimbImmune, true);

				Puppet.DisableMoves = Time.time + 2.5f;
				Puppet.JumpLocked   = true;

				if (Puppet.HandThing != null && (bool)Puppet.HandThing?.isEnergySword) Puppet.HandThing?.TurnItemOff();
				Puppet.pauseAiming = true;

				Puppet.LB["Foot"].ImmuneToDamage = true;
				Puppet.LB["FootFront"].ImmuneToDamage = true;

				rotoSpeed = 0f;

			}
		}

		//
		// ─── Backflip GO ────────────────────────────────────────────────────────────────
		//
		public void Go()
		{
			frame++;
			if (KB.Up) force++;

			if ( xTimer.counter == 0 )
			{
				if (!xTimer.flag)
				{
					xTimer.flag = true;
					APBackflip[0].RunMove();
				}


				float limbAngle = Mathf.DeltaAngle(Puppet.LB["LowerBody"].transform.eulerAngles.z, -10f * facing) * 1f;
				Puppet.RB2["LowerBody"]?.AddTorque(limbAngle * Time.fixedDeltaTime * Puppet.TotalWeight);

				limbAngle = Mathf.DeltaAngle(Puppet.LB["MiddleBody"].transform.eulerAngles.z, -10f * facing) * 1f;
				Puppet.RB2["MiddleBody"]?.AddTorque(limbAngle * Time.fixedDeltaTime * Puppet.TotalWeight);

				Puppet.RB2["LowerBody"]?.AddForce(Vector2.down * Puppet.TotalWeight * 10);
				if (xTimer.time > 0.3f) APBackflip[1].RunMove();
				if (xTimer.time > 0.4f)
				{
					xTimer.Restart();
					xTimer.IncCount();
				}
				return;
			}

			if ( xTimer.counter == 1 )
			{

				if (!xTimer.flag)
				{
					xTimer.flag = true;
					APBackflip[1].RunMove();
					if (isAndroid) Puppet.RB2["UpperBody"]?.AddForce((Vector2.up * 1.6f + (Vector2.right * facing * 1.5f)) * Puppet.TotalWeight * 4.5f, ForceMode2D.Impulse);
					else Puppet.RB2["UpperBody"]?.AddForce((Vector2.up * 1.6f + (Vector2.right * facing * 1.5f)) * Puppet.TotalWeight * 3f, ForceMode2D.Impulse);
					Puppet.RB2["Foot"]?.AddForce(Vector2.down * Puppet.TotalWeight * 2);
					Puppet.RB2["FootFront"]?.AddForce(Vector2.down * Puppet.TotalWeight * 2);
					if (!Puppet.IsAiming) Puppet.RB2["LowerArmFront"]?.AddForce(Vector2.up * Puppet.TotalWeight * 1);
					Puppet.RB2["LowerArm"]?.AddForce(Vector2.up * Puppet.TotalWeight * 1);

				} else
				{
					float xforce = 100f;
					if (xTimer.time > 0.05f) xforce = 300f;
					if (!Puppet.IsAiming) Puppet.RB2["LowerArmFront"]?.AddForce(Vector2.down * Puppet.TotalWeight);

					Puppet.RB2["LowerArm"]?.AddForce(Vector2.down * Puppet.TotalWeight);
					Puppet.RB2["UpperBody"]?.AddForce(Vector2.down * Time.fixedDeltaTime * xforce);
				}

				if (xTimer.time < 0.08f) 
				{ 
					//float limbAngle = Mathf.DeltaAngle(Puppet.LB["LowerBody"].transform.eulerAngles.z, -90f * facing) * 12f;
					Puppet.RB2["LowerBody"]?.AddTorque(-30 * facing * Time.fixedDeltaTime * Puppet.TotalWeight * 30);

					//limbAngle = Mathf.DeltaAngle(Puppet.LB["MiddleBody"].transform.eulerAngles.z, -90f * facing) * 12f;
					//Puppet.RB2["MiddleBody"]?.AddTorque(limbAngle * Time.fixedDeltaTime * Puppet.TotalWeight);
				}
				if (xTimer.time > 0.3f || (bool)Puppet.LB["LowerArmFront"]?.IsOnFloor || (bool)Puppet.LB["LowerArm"]?.IsOnFloor )
				{
					if (KB.Right || KB.Left || KB.Up)
					{
						Puppet.RB2["Foot"]?.AddForce((Vector2.right * facing) * Puppet.TotalWeight, ForceMode2D.Impulse);
						Puppet.RB2["FootFront"]?.AddForce((Vector2.right * facing ) * Puppet.TotalWeight, ForceMode2D.Impulse);
					}

					xTimer.Restart();
					xTimer.IncCount();

					if (!Puppet.IsAiming) Puppet.RB2["LowerArmFront"]?.AddForce(Vector2.up * Puppet.TotalWeight * 1);

					Puppet.RB2["LowerArm"]?.AddForce(Vector2.up * Puppet.TotalWeight * 1);
					Puppet.RB2["MiddleBody"]?.AddForce((Vector2.up) * Puppet.TotalWeight * 130f );
				}

				Puppet.RB2["UpperBody"]?.AddForce((Vector2.right * facing) * Puppet.TotalWeight * Time.fixedDeltaTime * 200f);
				return;
			}

			if ( xTimer.counter == 2 )
			{
				//Puppet.RB2["LowerBody"].AddTorque(limbAngle * Time.fixedDeltaTime * Puppet.TotalWeight);
				if (!xTimer.flag)
				{
					xTimer.flag = true;
					APBackflip[2].RunMove();
				}

				if ( (Puppet.LB["Foot"].IsOnFloor || Puppet.LB["FootFront"].IsOnFloor))
				{
					Puppet.RB2["Head"].velocity       *= 0.5f;
					Puppet.RB2["UpperBody"].velocity  *= 0.5f;
					Puppet.RB2["MiddleBody"].velocity *= 0.5f;
					Puppet.RB2["LowerBody"].velocity  *= 0.5f;
					Puppet.RB2["LowerArm"].AddForce(Vector2.down * Puppet.TotalWeight * 10);
					if (!Puppet.IsAiming) Puppet.RB2["LowerArmFront"].AddForce(Vector2.down * Puppet.TotalWeight *10);

					if (KB.Right || KB.Left || KB.Up)
					{
						xTimer.Restart();
						xTimer.counter = 1;
						return;
					}

					//xTimer.Restart();
					//xTimer.IncCount();
					APBackflip[2].ClearMove();
				}

				if (xTimer.time > 1.3)
				{
					xTimer.Restart();
					xTimer.IncCount();
				}
				return;
			}

			if ( xTimer.counter == 3 )
			{
				if (!xTimer.flag)
				{
					State = BackflipState.ready;

					Puppet.JumpLocked = false;

					PuppetMaster.Master.AddTask(Util.MaxPayne, 0.5f, false);

					Puppet.RB2["UpperBody"].velocity *= 0.0001f;

					Puppet.RunRigids(Puppet.RigidDrag, 0.5f);

					PuppetMaster.Master.AddTask(Finale, 1.5f);

					if (Puppet.HandThing != null && (bool)Puppet.HandThing?.isEnergySword) Puppet.HandThing?.TurnItemOn(true);
				}

				return;
			}

		}

		public void Finale()
		{
			Puppet.RunLimbs(Puppet.LimbImmune, false);
			Puppet.RunLimbs(Puppet.LimbHeal);
			Puppet.RunRigids(Puppet.RigidReset);
			APBackflip[2].ClearMove();
			Puppet.pauseAiming = false;
		}

	}

}
