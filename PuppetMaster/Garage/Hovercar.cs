//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetMaster
{
	[SkipSerialisation]
	public class PuppetHovercar : MonoBehaviour
	{
		public Puppet Puppet;
		public Transform HovercarT;

		public PhysicalBehaviour Body;
		public PhysicalBehaviour Front;
		public PhysicalBehaviour Back;
		public PhysicalBehaviour Engine;
		public PhysicalBehaviour Hull;
		public Sprite FDSprite;

		public MoveSet APHovercar;

		public bool Registered = false;

		public HovercarBehaviour HoverB;
		public float Speed;
		public bool ParkingBrake;
		public HingeJoint2D Hinge;
		public JointMotor2D Motor;

		public float Facing => (float)HovercarT.localScale.x < 0.0f ? 1f : -1f;



		public float BaseSpeed          = 900f;
		public float FactorPower        = 2f;
		public float ForceMultiplier    = 0f;
		public float SpeedupCounter     = 0f;
		public float VelocityRetention  = 0f;
		public float TopSpeed           = 100f;
		public float WheelSpan          = 0f;
		public float CurrentSpeed       = 0f;
		public bool GoingForward        = true;

		private Vector2 vec;



		//private List<GameObject> CustomBikeParts = new List<GameObject>();


		private float HovercarTimer  = 0f;
		private float HovercarTimer2 = 0f;
		private bool pullPopped      = false;
		private bool initSet         = false;
		private int skipFrames       = 0;
		private int counter          = 0;
		private bool doFlip          = false;
		private bool playedClip      = false;
		private bool downFixed       = false;
		private int currentStep      = 0;
		private float engineSFX1     = 0f;
		private float engineSFX2     = 0f;


		public float TotalWeight = 0f;
		public MoveSet[] APBike = new MoveSet[2];

		[SkipSerialisation]
		public enum HovercarStates
		{
			Ready,
			Summon,
			GettingIn,
			Driving,
			GettingOut,
			SendOff,
		}

		public HovercarStates state = HovercarStates.Ready;




		//
		// ─── UNITY FIXED UPDATE ────────────────────────────────────────────────────────────────
		//
		[SkipSerialisation]
		public void FixedUpdate()
		{
			if (skipFrames > 0)
			{
				--skipFrames;
				return;
			}
			if (doFlip)
			{
				//FlipHovercar(true);
				skipFrames = 3;
				return;
			}

			if (KB.Throw)
			{
				//APCar[1].RunMove();
				//Vector2 lbod = Puppet.RB2["LowerBody"].position;
				//Vector2 lcar = Body.rigidbody.position;

				//ModAPI.Notify("Body:" + lbod + " - Car:" + lcar + " - diff:" + (lbod - lcar));

				//Vector2 lfoot = Puppet.RB2["Foot"].position;

				//ModAPI.Notify("foot:" + lfoot + " - Car:" + lcar + " - diff:" + (lfoot - lcar));
			}


			if (Time.frameCount % 100 == 0)
			{
				CheckStatus();
			}
			else if (state == HovercarStates.GettingIn)
			{
				GetInVehicle();
			}
			else if (state == HovercarStates.Driving)
			{
				UpdateSpeedView();

				if (KB.Alt)
				{
					if (KB.Down)
					{
						Vector2 force = Body.rigidbody.velocity;
						Body.rigidbody.AddForceAtPosition(-force * 2.5f, Body.rigidbody.position);
					}
					else if (KB.Up)
					{
						Vector2 force = Body.rigidbody.velocity;
						Body.rigidbody.AddForceAtPosition((-force) + (Vector2.down * 5f), Body.rigidbody.position);
					}
					else if (KB.Left && KB.Right)
					{
						state         = HovercarStates.GettingOut;
						HovercarTimer = Time.time + 3f;
						initSet       = false;
						return;
					}
					else if (KB.Left || KB.Right)
					{
						if (CurrentSpeed < 1) Body.rigidbody.velocity = Vector2.zero;
					}

					else
					{
						//if (!HoverB.IsBrakeEngaged) HoverB.IsBrakeEngaged = true;
						//BRAKES
					}
				}
				else
				{
					//if (HoverB.IsBrakeEngaged) HoverB.IsBrakeEngaged = false;

				}
				if ((KB.Right && !Puppet.FacingLeft) || (KB.Left && Puppet.FacingLeft))
				{
					// going forward
					//if (!GoingForward) HoverB.MotorSpeed = 0;

					GoingForward     = true;
					HoverB.Activated = true;

					SetSpeed((KB.Shift ? 100 : 10) * -1);

					if (KB.Shift && CurrentSpeed < 15)
					{
					}

				}
				else if ((KB.Right && Puppet.FacingLeft) || (KB.Left && !Puppet.FacingLeft))
				{
					//  going backward
					//if (GoingForward) HoverB.MotorSpeed = 0;

					GoingForward     = false;
					HoverB.Activated = true;

					SetSpeed((KB.Shift ? 100 : 10) * 1);

					if (KB.Shift && CurrentSpeed < 10)
					{
					}
				}
				else
				{
					HoverB.Activated = false;
					//HoverB.MotorSpeed = CurrentSpeed * -105;
					//if (Time.frameCount % 100 == 0) ModAPI.Notify("CurrentSpeed:" + CurrentSpeed + " - MotorSpeed:" + HoverB.MotorSpeed);

				}


				if (KB.Up)
				{
					if (pullPopped)
					{
						Body.rigidbody.AddTorque(200f * -Facing * TotalWeight * Puppet.TotalWeight * Time.fixedDeltaTime, ForceMode2D.Force);
					}
					else
					{
						Body.rigidbody.AddTorque(25f * -Facing * TotalWeight * Puppet.TotalWeight * Time.fixedDeltaTime, ForceMode2D.Impulse);
						pullPopped = true;
					}
				}
				else pullPopped = false;

				if (KB.Down)
				{
					if (downFixed)
					{
						Body.rigidbody.AddTorque(200f * Facing * TotalWeight * Puppet.TotalWeight * Time.fixedDeltaTime, ForceMode2D.Force);
					}
					else
					{
						Body.rigidbody.AddTorque(25f * Facing * TotalWeight * Puppet.TotalWeight * Time.fixedDeltaTime, ForceMode2D.Impulse);
						downFixed = true;
					}

				}

				//if (KB.Aim)
				//{
				//    if (Puppet.IsAiming)
				//    {
				//        Puppet.LB["UpperArmFront"].Broken = true;
				//        Puppet.LB["LowerArmFront"].Broken = true;
				//    }
				//}



				if (state == HovercarStates.GettingOut)
				{
					if (Time.time > HovercarTimer)
					{
						if (!Util.IsColliding(HovercarT, Puppet.RB2["Head"].transform.root, false))
						{
							Util.ToggleCollisions(Body.transform, Puppet.RB2["LowerBody"].transform, true, false);

							Effects.Speedometer(Effects.SpeedometerTypes.Off, 0);

							state = HovercarStates.Ready;
						}
					}
				}
			}
		}



		private void UpdateSpeedView()
		{
			CurrentSpeed = Body.rigidbody.velocity.magnitude;
			if (CurrentSpeed == 0) CurrentSpeed = 0.01f;

			float xBuffer = Mathf.Clamp(CurrentSpeed / 5f, 2f, 5f);

			PuppetMaster.ChaseCam.SpeedAhead = xBuffer;


			Effects.Speedometer(Effects.SpeedometerTypes.Bike, CurrentSpeed);

		}


		public void SetSpeed(float speed, bool delta = true, bool turnOn = true)
		{
			if (CurrentSpeed > 30 && delta) speed *= 0.1f;

			//if (!delta) HoverB.MotorSpeed = speed;
			//else HoverB.MotorSpeed += speed;
		}


		public void CheckStatus()
		{
			if (HovercarT == null || Body == null)
			{
				Puppet.IsInVehicle = false;

				APHovercar.ClearMove();

				Puppet.RunRigids(Puppet.RigidReset);

				Util.Destroy(this);
			}
		}

		//
		// ─── REGISTER HOVERCAR ────────────────────────────────────────────────────────────────
		//
		public bool RegisterHovercar()
		{

			if (Registered)
			{
				state         = HovercarStates.GettingIn;
				HovercarTimer = Time.time + 3f;
				initSet       = false;
				return false;
			}

			APHovercar                                  = new MoveSet("hovercar", false);
			APHovercar.Ragdoll.ShouldStandUpright       = false;
			APHovercar.Ragdoll.State                    = PoseState.Sitting;
			APHovercar.Ragdoll.Rigidity                 = 2.2f;
			APHovercar.Ragdoll.AnimationSpeedMultiplier = 2.5f;
			APHovercar.Import();


			TotalWeight = 0;
			HovercarT = transform;

			foreach (PhysicalBehaviour pb in HovercarT.gameObject.GetComponentsInChildren<PhysicalBehaviour>())
			{
				pb.spriteRenderer.sortingLayerName = "Background";

				if (pb.name == "Body")              Body   = pb;
				if (pb.name == "voorkant dinges")   Front  = pb;
				if (pb.name == "onderkant ding")    Back   = pb;
				if (pb.name == "Engine")            Engine = pb;
				if (pb.name == "Hull")              Hull   = pb;

				TotalWeight += pb.rigidbody.mass;
			}

			HoverB = gameObject.GetComponentInChildren<HovercarBehaviour>();

			DisableCollisions();

			Transform rootx  = Body.transform.root;
			Vector3 pos      = Body.transform.position;
			pos.y           += 2.5f;
			rootx.position   = pos;

			state = HovercarStates.Ready;
			Registered = true;

			return true;
		}

		//public void ApplyCarLayers()
		//{
		//    foreach (PhysicalBehaviour pb in HovercarT.gameObject.GetComponentsInChildren<PhysicalBehaviour>())
		//    {
		//        if (CarLayers.ContainsKey(pb.name))
		//        {
		//            pb.spriteRenderer.sortingOrder = CarLayers[pb.name];
		//            pb.spriteRenderer.sortingLayerName = "Background";
		//        }
		//    }
		//}

		public void DisableCollisions()
		{
			Util.ToggleCollisions(HovercarT, Puppet.RB2["Head"].transform.root, false, true);

			foreach (Transform tr in Puppet.customParts.Transforms)
			{
				if (tr != null) Util.ToggleCollisions(HovercarT, tr, false, false);
			}

			if (Puppet.HandThing != null) DisableHeldItemCollisions(Puppet.HandThing);
		}

		public void DisableHeldItemCollisions(Thing thing)
		{
			Util.ToggleCollisions(thing.tr, HovercarT, false, true);
		}



		//
		// ─── EXIT VEHICLE ────────────────────────────────────────────────────────────────
		//
		public void ExitVehicle()
		{
			SetSpeed(0f, false, false);
			DisableCollisions();

			Puppet.PBO.OverridePoseIndex = -1;

			Puppet.RunRigids(Puppet.RigidReset);

			state              = HovercarStates.GettingOut;
			HovercarTimer      = Time.time + 1f;
			Puppet.IsInVehicle = false;

			Action leaveHovercar = () => {
				Puppet.IsInVehicle = false;
				Puppet.PBO.OverridePoseIndex = -1;
			};

			Effects.Speedometer(Effects.SpeedometerTypes.Off, 0);

			PuppetMaster.Master.AddTask(leaveHovercar, 1.0f);

			if (Puppet.HandThing.JointType == JointTypes.HingeJoint)
				Puppet.HandThing.ChangeJoint(JointTypes.FixedJoint);

		}




		//
		// ─── GET IN Vehicle ────────────────────────────────────────────────────────────────
		//
		[SkipSerialisation]
		public void GetInVehicle(bool init = false)
		{
			if (Time.time > HovercarTimer)
			{
				currentStep = 4;
				HovercarTimer = Time.time + 100;
				return;
			}

			if (init)
			{
				if (!Util.IsColliding(Puppet.LB["Head"].transform.root, HovercarT, false))
				{
					//state = HovercarStates.CloseDoor;
					HovercarTimer = Time.time + 3f;
					counter = 0;
					return;
				}

				currentStep = 0;
				Puppet.PBO.OverridePoseIndex = -1;
			}



			if (currentStep == 0)
			{
				Vector2 DoorTarget = Body.rigidbody.position + new Vector2(0.5f, -0.1f);
				Vector2 PuppetPos = Puppet.RB2["LowerBody"].position;

				float dist = Vector2.Distance(DoorTarget, PuppetPos);

				if (dist < 0.2f)
				{
					Puppet.RunRigids(Puppet.RigidStop);
					Puppet.PBO.OverridePoseIndex = -1;
					APHovercar.RunMove();
					currentStep = 1;
					return;
				}
				bool doWalk = false;
				if (DoorTarget.x > PuppetPos.x) doWalk = !Puppet.FacingLeft;
				else doWalk = Puppet.FacingLeft;

				Puppet.PBO.OverridePoseIndex = doWalk ? (int)PoseState.Walking : -1;

				float power = doWalk ? 2.2f : 350f;
				Vector2 dir = DoorTarget - PuppetPos;

				Puppet.RB2["LowerLeg"].AddForce(dir * Puppet.TotalWeight * Time.deltaTime * power);
				Puppet.RB2["UpperBody"].AddForce(dir * Puppet.TotalWeight * Time.deltaTime * power);
				Puppet.RB2["Head"].AddForce(Vector2.up * Puppet.TotalWeight * Time.deltaTime * power);
				return;
			}

			if (currentStep == 1)
			{
				Puppet.RunRigids(Puppet.RigidInertia, 0.1f);

				APHovercar.RunMove();
				Util.Notify("Getting in Car", VerboseLevels.Full);

				Puppet.Inventory.PutAwayItems(ItemTypes.VehicleSafe);

				if (Puppet.HandThing != null) DisableHeldItemCollisions(Puppet.HandThing);

				currentStep = 2;

				HovercarTimer2 = Time.time + 0.2f;

				return;
			}

			if (currentStep == 2)
			{
				if (Time.time > HovercarTimer2)
				{
					currentStep = 3; //return;
					APHovercar.RunMove();
					HovercarTimer2 = Time.time + 1.0f;
					return;

				}
			}

			if (currentStep == 3)
			{
				//APCar[1].RunMove();
				Vector2 pos = Body.rigidbody.position + new Vector2(-0.1f * Facing, -0.11f);

				Puppet.RB2["LowerBody"].MovePosition(pos);
				Puppet.RB2["LowerBody"].MoveRotation(-18f * Puppet.Facing);

				float dist = Vector2.Distance(pos, Puppet.RB2["LowerBody"].position);

				if (dist < 0.2f)
				{
					Puppet.PBO.Consciousness   = 1f;
					Puppet.PBO.ShockLevel      = 0.0f;
					Puppet.PBO.PainLevel       = 0.0f;
					Puppet.PBO.OxygenLevel     = 1f;
					Puppet.PBO.AdrenalineLevel = 1f;

					currentStep = 4;

					Puppet.IsInVehicle = true;
				}
				return;
			}

			if (currentStep == 4)
			{

				if (!Puppet.IsInVehicle) APHovercar.ClearMove();

				state         = HovercarStates.Driving;
				HovercarTimer = Time.time + 3f;
				counter       = 0;
				return;
			}




		}


		void FlipCar(bool AfterFrame = false)
		{
			if (!AfterFrame)
			{
				Transform carx = Body.transform.root;
				carx.position = Body.transform.position;
				doFlip = true;
				return;
			}

			Vector3 flipper = Body.transform.root.localScale;
			flipper.x *= -1;

			Vector3 pos = Body.transform.position;

			Body.transform.root.localScale = flipper;
			Body.transform.root.position = pos;

			doFlip = false;
		}

		void OnDestroy()
		{
			if (Puppet != null && Puppet.IsInVehicle)
			{
				Puppet.IsInVehicle = false;
				Puppet.PBO.OverridePoseIndex = -1;
			}

		}

		//
		// ─── COLLISIONS ────────────────────────────────────────────────────────────────
		//
		private void OnCollisionEnter2D(Collision2D coll = null)
		{
			if (coll == null) return;

			if (coll.gameObject.name == "LowerBody" && state == HovercarStates.Ready)
			{
				DisableCollisions();

				if (Facing != Puppet.Facing) FlipCar();

				Puppet.Actions.jump.State     = JumpState.ready;
				Puppet.Actions.dive.State     = DiveState.ready;
				Puppet.Actions.backflip.State = BackflipState.ready;
				Puppet.PBO.OverridePoseIndex  = -1;

				Puppet.RunRigids(Puppet.RigidReset);

				counter = 0;
				state = HovercarStates.GettingIn;
			}
		}
	}
}
