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
	public enum JumpState
	{
		ready,
		start,
		launch,
		goingUp,
		goingDown,
		gPoundStart,
		gPoundDown,
		sonicBoom,
		swordDown,
		swordKill,
		attackDown,
		landed,
	};

	public class Jump
	{
		public GameObject Glow;
		public int frame              = 0;
		public Puppet Puppet          = null;
		public float jumpStrength     = 0;
		public float torque           = 0;
		public float facing           = 1;
		public float sonicForce       = 0;
		public float power            = 0;
		public float timeRecover      = 0;

		public PuppetHand attackHand;

		private MoveSet ActionPose;
		private Vector3 diff;
		private Vector3 angleVelocity;
		private bool gripAltHand      = false;

		private bool miscBool   = false;
		private float miscFloat = 0f;

		private Collider2D altHandCollider;
		private TrailRenderer trailRenderer;
		private List<PersonBehaviour> Enemies = new List<PersonBehaviour>();
		private List<Rigidbody2D> Skulls      = new List<Rigidbody2D>();

		private JumpState _state = JumpState.ready;
		public JumpState State {  
			get { return _state; }
			set { 
				_state = value; 
				if (_state != JumpState.ready) Puppet.IsInvincible = true;
				else {
					Puppet.PG.PauseDualWielding = false;
					PuppetMaster.Master.AddTask(Puppet.Invincible, 1f, false);
					Puppet.IsInvincible = false;
				}
			}
		}


		public void Init()
		{
			if (State == JumpState.ready)
			{
				Puppet.PG.PauseDualWielding = true;
				Puppet.GetUp = false;
				Enemies.Clear();
				if (!Puppet.LB["Foot"].IsOnFloor && !Puppet.LB["FootFront"].IsOnFloor) return;
				if (Puppet.JumpLocked || Puppet.DisabledMoves) return;

				//  Make sure player is not holding up from something previously executed
				if (Time.time - KB.KeyTimes.Up > 1f) return;



				ActionPose = new MoveSet("jump", false);

				ActionPose.Ragdoll.Rigidity                 = 1.3f;
				ActionPose.Ragdoll.AnimationSpeedMultiplier = 0.5f;
				ActionPose.Ragdoll.UprightForceMultiplier   = 2f;
				ActionPose.Import();

				ActionPose.RunMove();

				jumpStrength                 = 0;
				State                        = JumpState.start;
				frame                        = 0;
				torque                       = 0;
				facing                       = Puppet.FacingLeft ? -1 : 1;

				Puppet.BlockMoves  = true;
				Puppet.pauseAiming = true;
			}
		}



		//
		// ─── JUMP GO ────────────────────────────────────────────────────────────────
		//
		public void Go()
		{

			if (Puppet.Actions.attack.CurrentAttack == Attack.AttackIds.dislodgeIchi)
			{
				State = JumpState.ready;
				Puppet.BlockMoves = false;
				return;
			}

			frame++;

			Vector2 direction  = new Vector2(KB.Right ? 3 : KB.Left ? -3 : 1 * facing, 10);
			float keyDirection = KB.Left ? 1f : (KB.Right ? -1f : 0);

			if ((KB.Left && facing < 0) || (KB.Right && facing > 0)) direction.x *= 2;
			//else if  direction.x *= 2;

			//  = = = = = = = = = = = = =
			//  START
			//  - - - - - - - - - - - - -
			if (State == JumpState.start)
			{

				if (KB.Up)
				{
					jumpStrength += 0.01f;
				}
				else
				{
					if (jumpStrength <= 0f)
					{
						State             = JumpState.ready;
						Puppet.BlockMoves = false;

						return;
					}

					if (jumpStrength > 3.5f) jumpStrength = 3.5f;

					State = JumpState.launch;
					frame = 0;
					//Puppet.Invincible(true);
					return;

				}
			}

			//  = = = = = = = = = = = = =
			//  LAUNCH
			//  - - - - - - - - - - - - -
			if (State == JumpState.launch)
			{
				// Puppet.PBO.OverridePoseIndex = -1;

				if (KB.Left || KB.Right)
				{
					torque = keyDirection * 10 * Puppet.TotalWeight * jumpStrength * 2f;
					Puppet.RB2["MiddleBody"].AddTorque(torque, ForceMode2D.Force);
					Puppet.RB2["UpperBody"].AddTorque(torque, ForceMode2D.Force);
					Puppet.RB2["LowerBody"].AddTorque(torque, ForceMode2D.Force);
				}

				Puppet.RB2["Head"].AddRelativeForce(direction * (20 * (jumpStrength * Puppet.TotalWeight)));
				Puppet.RB2["UpperBody"].AddRelativeForce(direction * (20 * (jumpStrength * Puppet.TotalWeight)));
				Puppet.RB2["LowerBody"].AddRelativeForce(direction * (20 * (jumpStrength * Puppet.TotalWeight)));
				Puppet.RB2["Foot"].AddRelativeForce(direction * (20 * (jumpStrength * Puppet.TotalWeight)));
				Puppet.RB2["FootFront"].AddRelativeForce(direction * (20 * (jumpStrength * Puppet.TotalWeight)));

				if (KB.Left)
				{
					Puppet.RB2["Head"].AddForce(Vector2.left * jumpStrength * Puppet.TotalWeight * 50);
					Puppet.RB2["Foot"].AddForce(Vector2.right * jumpStrength * Puppet.TotalWeight * 50);
					Puppet.RB2["FootFront"].AddForce(Vector2.right * jumpStrength * Puppet.TotalWeight * 50);
				}
				if (KB.Right)
				{
					Puppet.RB2["Head"].AddForce(Vector2.right * jumpStrength * Puppet.TotalWeight * 50);
					Puppet.RB2["Foot"].AddForce(Vector2.left * jumpStrength * Puppet.TotalWeight * 50);
					Puppet.RB2["FootFront"].AddForce(Vector2.left * jumpStrength * Puppet.TotalWeight * 50);
				}
				State = JumpState.goingUp;
				frame = 0;
				return;
			}

			//  = = = = = = = = = = = = =
			//  GOING UP
			//  - - - - - - - - - - - - -
			if (State == JumpState.goingUp)
			{
				if (KB.Down)
				{
					frame = 0;
					State = JumpState.gPoundStart;
					return;
				}

				if (frame == 1)
				{
					ActionPose = new MoveSet("jump_spin", false);
					ActionPose.Ragdoll.ShouldStandUpright       = false;
					ActionPose.Ragdoll.State                    = PoseState.Protective;
					ActionPose.Ragdoll.Rigidity                 = 2.3f;
					ActionPose.Ragdoll.AnimationSpeedMultiplier = 2.5f;
					ActionPose.Ragdoll.UprightForceMultiplier   = 0f;
					ActionPose.Import();
				}

				if (KB.Action2Held || (Puppet.PG.AimMode != AimModes.Off && KB.Mouse2Down))
				{
					attackHand = Puppet.PG.GetAttackHand();

					if (attackHand != null && attackHand.Thing.canStab)
					{
						frame = 0;
						State = JumpState.swordDown;
						return;
					}
				}
				if (KB.ActionHeld || (Puppet.PG.AimMode != AimModes.Off && KB.MouseDown))
				{
					attackHand = Puppet.PG.GetAttackHand();

					if (attackHand != null && attackHand.Thing.canStab)
					{
						frame = 0;
						State = JumpState.attackDown;
						return;
					}
				}

				ActionPose.RunMove();

				if (frame < 10)
				{
					Puppet.RB2["LowerBody"].AddForce(direction * (jumpStrength * Puppet.TotalWeight));
				}

				if (frame == 10) {
					State = JumpState.goingDown;
					frame = 0;
					return;
				}

				if (KB.Left || KB.Right)
				{
					if (KB.Modifier) Swing();
					else
					{
						Vector2 jDir = KB.Right ? Vector2.right : Vector2.left;
						Puppet.RB2["LowerBody"].AddForce(jDir * Puppet.TotalWeight * Time.deltaTime * 200);

						if ((keyDirection < 0 && Puppet.RB2["MiddleBody"].angularVelocity > -300) ||
							(keyDirection > 0 && Puppet.RB2["MiddleBody"].angularVelocity < 300))
						{
							torque = keyDirection * 10 * Puppet.TotalWeight * jumpStrength * 0.32f;
							Puppet.RB2["MiddleBody"].AddTorque(torque, ForceMode2D.Force);
							Puppet.RB2["UpperBody"].AddTorque(torque, ForceMode2D.Force);
							Puppet.RB2["LowerBody"].AddTorque(torque, ForceMode2D.Force);
						}
					}
				}
			}

			//  = = = = = = = = = = = = =
			//  GOING DOWN
			//  - - - - - - - - - - - - -
			if (State == JumpState.goingDown)
			{
				if (KB.Down)
				{
					frame = 0;
					State = JumpState.gPoundStart;
					return;
				}

				if (KB.Action2Held || (Puppet.PG.AimMode != AimModes.Off && KB.Mouse2Down))
				{
					attackHand = Puppet.PG.GetAttackHand();

					if (attackHand != null && attackHand.Thing.canStab)
					{
						frame = 0;
						State = JumpState.swordDown;
						return;
					}
				}
				if (KB.ActionHeld || (Puppet.PG.AimMode != AimModes.Off && KB.MouseDown))
				{
					attackHand = Puppet.PG.GetAttackHand();

					if (attackHand != null && attackHand.Thing.canStab)
					{
						frame = 0;
						State = JumpState.attackDown;
						return;
					}
				}

				if (Puppet.PBO.IsTouchingFloor)
				{
					State = JumpState.landed;
					return;
				}

				if (KB.Left || KB.Right)
				{
					if (KB.Alt) Swing();
					else
					{
						if (jumpStrength == 0) jumpStrength = 1f;

						Vector2 jDir = KB.Right ? Vector2.right : Vector2.left;
						Puppet.RB2["LowerBody"].AddForce(jDir * Puppet.TotalWeight * Time.deltaTime * 200);

						keyDirection = KB.Left ? 1f : (KB.Right ? -1f : 0);

						if ((keyDirection < 0 && Puppet.RB2["MiddleBody"].angularVelocity > -300) ||
							(keyDirection > 0 && Puppet.RB2["MiddleBody"].angularVelocity < 300))
						{
							torque       = keyDirection * 10 * Puppet.TotalWeight * jumpStrength * 0.32f;
							Puppet.RB2["MiddleBody"].AddTorque(torque, ForceMode2D.Force);
							Puppet.RB2["UpperBody"].AddTorque(torque, ForceMode2D.Force);
							Puppet.RB2["LowerBody"].AddTorque(torque, ForceMode2D.Force);
						}
					}
				}
			}

			//  = = = = = = = = = = = = =
			//  GROUND POUND START
			//  - - - - - - - - - - - - -
			if (State == JumpState.gPoundStart)
			{
				sonicForce += Time.fixedDeltaTime * 2;

				if (frame == 1)
				{
					sonicForce = 0;
					ActionPose = new MoveSet("groundpound_1", false);

					ActionPose.Ragdoll.ShouldStandUpright       = false;
					ActionPose.Ragdoll.Rigidity                 = 2.3f;
					ActionPose.Ragdoll.AnimationSpeedMultiplier = 1.5f;
					ActionPose.Ragdoll.UprightForceMultiplier   = 0f;
					ActionPose.Import();

					ActionPose.RunMove();

					Puppet.RB2["MiddleBody"].inertia = 0.11125f;
				}

				if (!KB.Down)
				{
					ActionPose.ClearMove();
					State = JumpState.goingDown;
					frame = 0;
					Effects.DoTrail(Puppet.RB2["UpperBody"], true);
					return;
				}


				Vector2 ubody = Puppet.RB2["UpperBody"].velocity;
				ubody.x *= 0.1f;
				Puppet.RB2["UpperBody"].velocity = ubody;

				if (ubody.y < -0.5f)
				{
					frame = 0;
					State = JumpState.gPoundDown;
					return;
				} 
				else if (ubody.y < 0.5f)
				{
					diff = Vector3.right;
					angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg));
				}
				else
				{
					diff = Vector3.right;
					angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg));
				}

				Puppet.RB2["MiddleBody"].MoveRotation(
					Quaternion.RotateTowards(
						Puppet.RB2["MiddleBody"].transform.rotation, Quaternion.Euler(angleVelocity * facing), 5f));

				if (Puppet.PBO.IsTouchingFloor)
				{
					State = JumpState.landed;
					return;
				}

			}


			//  = = = = = = = = = = = = =
			//  GROUND POUND DOWN
			//  - - - - - - - - - - - - -
			if (State == JumpState.gPoundDown)
			{
				if (frame == 1)
				{
					PuppetMaster.ChaseCam.CustomMode  = CustomModes.GroundPound;

					//Puppet.Invincible(true);

					ActionPose = new MoveSet("groundpound_2", false);

					ActionPose.Ragdoll.ShouldStandUpright       = false;
					ActionPose.Ragdoll.Rigidity                 = 2.3f;
					ActionPose.Ragdoll.AnimationSpeedMultiplier = 2.5f;
					ActionPose.Ragdoll.UprightForceMultiplier   = 0f;
					ActionPose.Import();

					ActionPose.RunMove();

					Puppet.HandThing?.DisableSelfDamage(true);
				}


				if (frame == 10) trailRenderer = Effects.DoTrail(Puppet.RB2["UpperBody"]);

				if (!KB.Down)
				{
					ActionPose.ClearMove();
					State = JumpState.goingDown;
					frame = 0;
					Effects.DoTrail(Puppet.RB2["UpperBody"], true);
					return;

				}

				Vector2 ubody = Puppet.RB2["UpperBody"].velocity;


				sonicForce += Time.fixedDeltaTime * 2;

				Puppet.RunRigids(Puppet.RigidAddMass, 1.01f);

				diff          = Vector3.right + Vector3.down;

				angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg * facing ));

				Puppet.RB2["MiddleBody"].MoveRotation(Quaternion.RotateTowards(
					Puppet.RB2["MiddleBody"].transform.rotation, Quaternion.Euler(angleVelocity), 3f));

				Quaternion aQuaternion = Quaternion.Inverse(Puppet.RB2["MiddleBody"].transform.rotation);
				Quaternion bQuaternion = Quaternion.Inverse(Quaternion.Euler(angleVelocity));
				Quaternion deltaQuaternion = bQuaternion * Quaternion.Inverse(aQuaternion);
				if (Mathf.Abs(deltaQuaternion.z) <= 0.05f) Puppet.RB2["MiddleBody"].freezeRotation = true;

				if (Puppet.PBO.IsTouchingFloor)
				{
					DustClouds dustStorm = Puppet.RB2["LowerArmFront"].gameObject.AddComponent<DustClouds>();

					dustStorm.ConfigDust(sonicForce,1);
					if (ubody.y > -1f)
					{
						State  = JumpState.sonicBoom;
						frame  = 0;
						Puppet.RunRigids(Puppet.RigidStop);
						return;
					}
				}

				ubody.y = Mathf.Clamp(ubody.y *= 1.25f, -100f, 100.5f);

				Puppet.RB2["UpperBody"].velocity = ubody;
			}

			//  = = = = = = = = = = = = =
			//  SONIC BOOM
			//  - - - - - - - - - - - - -
			if (State == JumpState.sonicBoom)
			{

				if (frame == 1)
				{
					PuppetMaster.ChaseCam.CustomMode = CustomModes.SonicBoom;
					//Puppet.RunLimbs(Puppet.LimbGhost, true);

					trailRenderer.emitting = false;

					Vector2 effectPosition = Puppet.RB2["LowerArmFront"].transform.position;
					effectPosition.y -= 0.5f;

					ModAPI.CreateParticleEffect("IonExplosion", effectPosition);

					Effects.DoNotKill.AddRange(Puppet.PBO.transform.root.GetComponentsInChildren<Collider2D>());

					Effects.DoPulseExplosion(Puppet.RB2["LowerArmFront"].transform.position, sonicForce, sonicForce * 2, true);

					ActionPose = new MoveSet("groundpound_3", false);
					ActionPose.Ragdoll.ShouldStandUpright       = false;
					ActionPose.Ragdoll.State                    = PoseState.Sitting;
					ActionPose.Ragdoll.Rigidity                 = 2.3f;
					ActionPose.Ragdoll.AnimationSpeedMultiplier = 1.5f;
					ActionPose.Ragdoll.UprightForceMultiplier   = 0f;
					ActionPose.Import();

					ActionPose.RunMove();
					timeRecover        = Time.time + 2f;
					miscBool = false;
					miscFloat = 0;


					//PuppetMaster.Master.AddTask(Puppet.handThing.DisableSelfDamage,2f,false);

					//ChaseCamX.lockedOn = false;
				}
				if (frame == 2) Puppet.RunLimbs(Puppet.LimbRecover);


				if (Time.time > (timeRecover - 1))
				{ 
					if (!miscBool) {
						ActionPose.ClearMove(); 
						Puppet.RunRigids(Puppet.BodyInertiaFix);
					}
					Puppet.RunLimbs(Puppet.LimbUp,2.5f);
					miscBool = true; 
				}

				if (Time.time > timeRecover)
				{
					Puppet.RunLimbs(Puppet.LimbCure);

					Puppet.RB2["MiddleBody"].freezeRotation = false;

					Effects.DoTrail(Puppet.RB2["UpperBody"], true);

					State = JumpState.landed;

					return;
				}
			}


			//  = = = = = = = = = = = = =
			//  SWORD DOWN
			//  - - - - - - - - - - - - -
			if (State == JumpState.swordDown)
			{
				if (frame == 1)
				{
					//if (Puppet.GripB == null) {
					//    gripAltHand     = true;
					//    if (!Puppet.RB2["LowerHand"].TryGetComponent<Collider2D>(out altHandCollider))
					//        altHandCollider = null;
					//}

					Puppet.Actions.attack.CurrentMove           = Attack.MoveTypes.stab;

					power                                       = 0;
					ActionPose                                  = new MoveSet("jumpsword_1", false);
					ActionPose.Ragdoll.ShouldStandUpright       = false;
					ActionPose.Ragdoll.Rigidity                 = 2.3f;
					ActionPose.Ragdoll.AnimationSpeedMultiplier = 2.5f;
					ActionPose.Ragdoll.UprightForceMultiplier   = 0f;
					ActionPose.Import();
					ActionPose.RunMove();

					Puppet.RB2["MiddleBody"].inertia = 0.11125f;

					Puppet.PG.FixPosition(attackHand, false, true);

					PuppetMaster.ChaseCam.yOffsetTemp = -0.5f;
				}

				if (frame == 2)
				{
					//if (Puppet.HandThing.P == null) Puppet.DropThing();
				}

				//if (gripAltHand) 
				//{
				//    if (altHandCollider != null)
				//    {
				//        for (int i=Puppet.HandThing.ItemColliders.Length; --i >= 0;) 
				//        { 
				//            if (gripAltHand && altHandCollider.IsTouching(Puppet.HandThing.ItemColliders[i]))
				//            {
				//                gripAltHand = false;
				//                Puppet.HandThing.P.MakeWeightful();
				//                Puppet.LB["LowerArm"].SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

				//            }

				//        }
				//    } else gripAltHand = false;
				//}

				power                       += 1f;
				Puppet.HandThing.AttackDamage = power;

				diff          = Vector3.down;
				angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg));


				Puppet.RB2["MiddleBody"].MoveRotation(
					Quaternion.RotateTowards(
						Puppet.RB2["MiddleBody"].transform.rotation, Quaternion.Euler(angleVelocity * facing), 10f));

				if (Puppet.PBO.IsTouchingFloor)
				{
					State = JumpState.landed;
					return;
				}

				if (KB.Left) Puppet.RB2["UpperBody"].AddForce(Vector2.left * Puppet.TotalWeight * 5f);
				if (KB.Right) Puppet.RB2["UpperBody"].AddForce(Vector2.right * Puppet.TotalWeight * 5f);

			}


			//  = = = = = = = = = = = = =
			//  ATTACK DOWN
			//  - - - - - - - - - - - - -
			if (State == JumpState.attackDown)
			{
				if (frame == 1)
				{
					if (attackHand.Thing.IsFlipped != Puppet.IsFlipped)
					{
						Puppet.PG.FixPosition(attackHand);
					}
					if (attackHand.Thing.isShiv)
					{
						frame = 0;
						State = JumpState.swordDown;
						return;
					}
					attackHand.Thing.HitLimb = null;
					FindEnemies();
					//if (Puppet.GripB == null)
					//{
					//    gripAltHand = true;
					//    if (!Puppet.RB2["LowerHand"].TryGetComponent<Collider2D>(out altHandCollider))
					//        altHandCollider = null;
					//}

					Puppet.Actions.attack.CurrentMove = Attack.MoveTypes.stab;

					power = 0;
					ActionPose = new MoveSet("jumpsword_2", false);
					ActionPose.Ragdoll.ShouldStandUpright = false;
					ActionPose.Ragdoll.Rigidity = 2.3f;
					ActionPose.Ragdoll.AnimationSpeedMultiplier = 2.5f;
					ActionPose.Ragdoll.UprightForceMultiplier = 0f;
					ActionPose.Import();
					ActionPose.RunMove();

					Puppet.RB2["MiddleBody"].inertia = 0.11125f;

					PuppetMaster.ChaseCam.yOffsetTemp = -0.5f;

				}

				power += 1f;
				Puppet.HandThing.AttackDamage = power;

				diff = Vector3.left;
				angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg));


				Puppet.RB2["MiddleBody"].MoveRotation(
					Quaternion.RotateTowards(
						Puppet.RB2["MiddleBody"].transform.rotation, Quaternion.Euler(angleVelocity * facing), 10f));

				if (Puppet.PBO.IsTouchingFloor)
				{
					State = JumpState.landed;
					return;
				}

				if (KB.Left)  Puppet.RB2["UpperBody"].AddForce(Vector2.left * Puppet.TotalWeight * 5f);
				else if (KB.Right) Puppet.RB2["UpperBody"].AddForce(Vector2.right * Puppet.TotalWeight * 5f);
				else
				{
					Rigidbody2D enemyRB = ClosestSkull();

					if (enemyRB != null)
					{
						Vector2 toEnemy = attackHand.Thing.R.position - enemyRB.position;
						toEnemy.y       = 0f;
						Puppet.RB2["UpperBody"].AddForce(toEnemy * -facing * Puppet.TotalWeight * 1.5f * toEnemy.magnitude * Time.deltaTime);
					}
				}

				if (attackHand.Thing.HitLimb != null)
				{
					attackHand.Thing.HitLimb.Slice();
					State = JumpState.swordKill;
					frame = 0;
					return;
				}

			}


			//  = = = = = = = = = = = = =
			//  SWORD KILL
			//  - - - - - - - - - - - - -
			if (State == JumpState.swordKill)
			{

				if (frame == 1)
				{

					PuppetMaster.ChaseCam.yOffsetTemp = 0.0f;

					Puppet.JumpLocked = false;
					State             = JumpState.ready;
					frame             = 0;
					Puppet.BlockMoves = false;

					ActionPose.ClearMove();

					Puppet.RunRigids(Puppet.BodyInertiaFix);
					Puppet.RunRigids(Puppet.RigidMass, -1f);

					if (jumpStrength < 0.2f) jumpStrength = 0.2f;

					Puppet.DisableMoves = Time.time + jumpStrength / 2;

					//PuppetMaster.Master.AddTask(Puppet.Invincible,1f,false);

					Util.MaxPayne(false);

				}

				diff = Vector3.up;
				angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg));


				Puppet.RB2["MiddleBody"].MoveRotation(
					Quaternion.RotateTowards(
						Puppet.RB2["MiddleBody"].transform.rotation, Quaternion.Euler(angleVelocity * facing), 10f));

				if (frame >= 20)
				{
					State = JumpState.landed;
				}

			}

			//  = = = = = = = = = = = = =
			//  LANDED
			//  - - - - - - - - - - - - -
			if (State == JumpState.landed)
			{
				PuppetMaster.ChaseCam.yOffsetTemp = 0.0f;

				Puppet.JumpLocked    = false;
				State                = JumpState.ready;
				frame                = 0;
				Puppet.BlockMoves    = false;

				ActionPose.ClearMove();

				Puppet.RunRigids(Puppet.BodyInertiaFix);
				//Puppet.RunRigids(Puppet.RigidMass,-1f);

				if (jumpStrength < 0.2f) jumpStrength = 0.2f;

				Puppet.DisableMoves = Time.time + jumpStrength / 2;
				Puppet.RunLimbs(Puppet.LimbRecover);
				//Puppet.RunLimbs(Puppet.LimbReset);
				PuppetMaster.Master.AddTask(Puppet.Invincible, 1f, false);

				Util.MaxPayne(false);

				Puppet.pauseAiming = false;
				Puppet.GetUp = true;

			}
		}



		public void Swing()
		{
			State = JumpState.landed;

			Puppet.Actions.swing.Init();
		}




		//
		// ─── CHECK FOR ENEMY ────────────────────────────────────────────────────────────────
		//
		public void FindEnemies()
		{
			Skulls.Clear();
			Enemies.AddRange(GameObject.FindObjectsOfType<PersonBehaviour>());

			for (int i = Enemies.Count; --i >= 0;)
			{
				if (Enemies[i].IsAlive() && Enemies[i] != Puppet.PBO) {

					Skulls.Add(Enemies[i].transform.GetChild(5).GetComponent<Rigidbody2D>());

				} else
				{
					Enemies.RemoveAt(i);

				}
			}
		}

		public Rigidbody2D ClosestSkull()
		{
			if (Skulls.Count == 0) return null;

			float closestDistance = float.MaxValue;

			Rigidbody2D rb = null;

			for (int i = Skulls.Count; --i >= 0;)
			{
				float distance = Mathf.Abs((Skulls[i].position - Puppet.HandThing.R.position).sqrMagnitude);

				if (distance < closestDistance)
				{
					rb              = Skulls[i];
					closestDistance = distance;
				}
			}

			return rb;

		}

		public bool CheckForEnemy()
		{
			//PersonBehaviour TheChosen   = (PersonBehaviour)null;
			PersonBehaviour[] people    = GameObject.FindObjectsOfType<PersonBehaviour>();



			//float floatemp1 = float.MaxValue;
			//
			//bool lastKnockedOut = false;

			foreach (PersonBehaviour person in people)
			{
				if (!person.isActiveAndEnabled || !person.IsAlive() || person == Puppet.PBO) continue;

				Vector2 Vectemp1 = person.transform.GetComponentInChildren<Rigidbody2D>().position;

				if (Vectemp1.y > Puppet.RB2["Head"].position.y) continue;
				if (Puppet.RB2["Head"].position.y - Vectemp1.y < 1f) continue;
				if (Mathf.Abs(Vectemp1.x - Puppet.RB2["Head"].position.x) > 1f) continue;

				return true;

				//float floatemp2 = (thing.R.position - Vectemp1).sqrMagnitude;

				//if (floatemp2 < floatemp1)
				//{
				//    floatemp1 = floatemp2;
				//    TheChosen = person;

				//    if (person.Consciousness < 1 || person.ShockLevel > 0.3f)
				//    {
				//        lastKnockedOut = true;
				//    }
				//}
				//else if (lastKnockedOut)
				//{
				//    if (person.Consciousness >= 1 && person.ShockLevel < 0.3f)
				//    {
				//        if (floatemp2 - floatemp1 < 1f)
				//        {
				//            lastKnockedOut = false;
				//            floatemp1 = floatemp2;
				//            TheChosen = person;
				//        }
				//    }
			}

			return false;

			//Enemy = TheChosen;

			//if (TheChosen == null)
			//{
			//    EnemyTarget = Puppet.RB2["UpperBody"].position + (Vector2.right * facing * 1.5f);
			//    return;

			}
		}
}
