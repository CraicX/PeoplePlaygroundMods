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
	public enum CarStates
	{
		Ready,
		Summon,
		OpenDoor,
		CloseDoor,
		GettingIn,
		Driving,
		GettingOut,
		SendOff,
	}

	[SkipSerialisation]
	public class PuppetCar : MonoBehaviour
	{
		[SkipSerialisation] public Puppet Puppet;
		[SkipSerialisation] public Transform CarT;

		[SkipSerialisation] public PhysicalBehaviour Body;
		[SkipSerialisation] public PhysicalBehaviour FrontDoor;
		[SkipSerialisation] public PhysicalBehaviour BackDoor;
		[SkipSerialisation] public PhysicalBehaviour Bonnet;
		[SkipSerialisation] public PhysicalBehaviour Boot;
		[SkipSerialisation] public PhysicalBehaviour Engine;
		[SkipSerialisation] public PhysicalBehaviour GasTank;
		[SkipSerialisation] public PhysicalBehaviour Wheel1;
		[SkipSerialisation] public PhysicalBehaviour Wheel2;
		[SkipSerialisation] public Sprite FDSprite;

		[SkipSerialisation] public MoveSet[] APCar = new MoveSet[2];

		[SkipSerialisation] public bool Registered = false;

		[SkipSerialisation] public CarBehaviour CarB;
		[SkipSerialisation] public float Speed;
		[SkipSerialisation] public bool ParkingBrake;
		[SkipSerialisation] public HingeJoint2D Hinge;
		[SkipSerialisation] public JointMotor2D Motor;

		[SkipSerialisation] public float BurnoutStart = 0;

		[SkipSerialisation] public float Facing => (float)CarT.localScale.x < 0.0f ? 1f : -1f;

		[SkipSerialisation] public float SpeedupCounter = 0f;
		[SkipSerialisation] public float TopSpeed       = 100f;
		[SkipSerialisation] public float WheelSpan      = 0f;
		[SkipSerialisation] public float CurrentSpeed   = 0f;
		[SkipSerialisation] public bool GoingForward    = true;

		[SkipSerialisation] public bool DoChaseCam = true;

		private Vector2 vec;
		private AudioSource AudioSFX2;
		private AudioSource AudioSFX3;
		private AudioSource AudioSFX1;



		//private List<GameObject> CustomBikeParts = new List<GameObject>();


		private float carTimer    = 0f;
		private float carTimer2   = 0f;
		private bool pullPopped   = false;
		private bool initSet      = false;
		private int skipFrames    = 0;
		private int counter       = 0;
		private bool doFlip       = false;
		private bool playedClip   = false;
		private bool downFixed    = false;
		private int currentStep   = 0;
		private float engineSFX1  = 0f;
		private float engineSFX2  = 0f;

		private Dictionary<string, int> CarLayers = new Dictionary<string, int>()
		{
			{"Wheel1",    -1 },
			{"Wheel2",    -1 },
			{"Bonnet",    -4 },
			{"Boot",      -3 },
			{"Body",      -1 },
			{"BackDoor",  -1 },
			{"FrontDoor", -1 },
			{"Gastank",   -5 },
			{"Engine",    -5 },
		};
		[SkipSerialisation] public float TotalWeight = 0f;



		[SkipSerialisation] public CarStates state = CarStates.Ready;




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
				FlipCar(true);
				skipFrames = 3;
				return;
			}


			if (Time.frameCount % 100 == 0)
			{
				CheckStatus();
			}

			if (state == CarStates.OpenDoor)
			{
				OpenCarDoor();
			}
			else if (state == CarStates.GettingIn)
			{
				GetInCar();
			}
			else if (state == CarStates.CloseDoor)
			{
				CloseCarDoor();
			}
			else if (state == CarStates.Driving)
			{
				UpdateSpeedView();

				if (KB.Alt)
				{
					PhysicalBehaviour Wheel;
					if (KB.Down)
					{
						Wheel = Facing < 0 ? Wheel2 : Wheel1;

						if (Wheel != null)
						{
							Wheel.rigidbody.angularVelocity = 0f;
							Vector2 force = Wheel.rigidbody.velocity;
							Wheel.rigidbody.AddForceAtPosition(-force * 2.5f, Wheel.rigidbody.position);
						}
					}
					else if (KB.Up)
					{
						Wheel = Facing < 0 ? Wheel1 : Wheel2;

						if (Wheel != null)
						{
							Wheel.rigidbody.angularVelocity = 0f;
							Vector2 force = Wheel.rigidbody.velocity;
							Wheel.rigidbody.AddForceAtPosition((-force) + (Vector2.down * 5f), Wheel.rigidbody.position);
							if (Mathf.Abs(force.magnitude) > 5f) Wheel.Sizzle();
						}
					}
					else if (KB.Left && KB.Right)
					{
						state    = CarStates.OpenDoor;
						carTimer = Time.time + 3f;
						initSet  = false;
						return;
					}
					else if (KB.Left || KB.Right)
					{
						if (CurrentSpeed < 1.5f) { 
							if (BurnoutStart == 0 || Time.time - BurnoutStart > 20) BurnoutStart = Time.time;
							Effects.Burnout(Wheel1, BurnoutStart);
							Effects.Burnout(Wheel2, BurnoutStart);
							if (CurrentSpeed < 1) Body.rigidbody.velocity = Vector2.zero;
						} else
						{
							BurnoutStart = 0;
						}
					}

					else
					{
						if (!CarB.IsBrakeEngaged) CarB.IsBrakeEngaged = true;
						Wheel1.rigidbody.angularVelocity = 0f;
						Wheel2.rigidbody.angularVelocity = 0f;
						Vector2 force = Wheel1.rigidbody.velocity;
						Wheel1.rigidbody.AddForceAtPosition((-force) + (Vector2.down * 5f), Wheel1.rigidbody.position);
						Wheel2.rigidbody.AddForceAtPosition((-force) + (Vector2.down * 5f), Wheel2.rigidbody.position);
						if (Mathf.Abs(force.magnitude) > 5f) {
							Wheel1.Sizzle();
							Wheel2.Sizzle();
						}

					}
				} else
				{
					BurnoutStart = 0;
					if (CarB.IsBrakeEngaged) CarB.IsBrakeEngaged = false;

				}
				if ((KB.Right && !Puppet.FacingLeft) || (KB.Left && Puppet.FacingLeft))
				{
					// going forward
					if (CarB.IsBrakeEngaged) CarB.IsBrakeEngaged = false;
					if (!GoingForward) CarB.MotorSpeed = 0;

					GoingForward   = true;
					CarB.Activated = true;

					SetSpeed((KB.Shift ? 100 : 10) * -1);

					if (KB.Shift && CurrentSpeed < 15)
					{
						Wheel1.Sizzle();
						Wheel2.Sizzle();
					}

				} 
				else if((KB.Right && Puppet.FacingLeft) || (KB.Left && !Puppet.FacingLeft))
				{
					//  going backward
					if (GoingForward) CarB.MotorSpeed = 0;

					GoingForward   = false;
					CarB.Activated = true;

					SetSpeed((KB.Shift ? 100 : 10) * 1);

					if (KB.Shift && CurrentSpeed < 3)
					{
						Wheel1.Sizzle();
						Wheel2.Sizzle();
					}
				}
				else
				{
					CarB.Activated              = false;
					CarB.MotorSpeed             = CurrentSpeed * -105;
					//if (Time.frameCount % 100 == 0) ModAPI.Notify("CurrentSpeed:" + CurrentSpeed + " - MotorSpeed:" + CarB.MotorSpeed);

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



			if (state == CarStates.GettingOut)
				{
					if (Time.time > carTimer)
					{
						if (!Util.IsColliding(CarT, Puppet.RB2["Head"].transform.root, false))
						{
							Util.ToggleCollisions(Body.transform, Puppet.RB2["LowerBody"].transform, true, false);

							Effects.Speedometer(Effects.SpeedometerTypes.Off, 0);

							state = CarStates.Ready;
						}
					}
				}
			}
		}

		struct engineSFXi
		{
			public float speed;
			public float denom;
			public float start;
		}

		engineSFXi[] engineSFX = 
		{
			new engineSFXi{speed=0, denom=15,start=1},
			new engineSFXi{speed=20,denom=17,start=0},
			new engineSFXi{speed=60,denom=25,start=-1.5f},
			new engineSFXi{speed=100,denom=30,start=-3},
		};

		private void UpdateSpeedView()
		{
			CurrentSpeed     = Body.rigidbody.velocity.magnitude;
			if (CurrentSpeed == 0) CurrentSpeed = 0.01f;

			float xBuffer    = Mathf.Clamp(CurrentSpeed / 5f, 2f, 5f);
			engineSFXi sfx;

			PuppetMaster.ChaseCam.SpeedAhead = xBuffer;


			Effects.Speedometer(Effects.SpeedometerTypes.Bike, CurrentSpeed);

			if (CurrentSpeed < 20) sfx       = engineSFX[0];
			else if (CurrentSpeed < 50) sfx  = engineSFX[1];
			else if (CurrentSpeed < 120) sfx = engineSFX[2];
			else sfx                         = engineSFX[3];

			engineSFX1 = (CurrentSpeed / sfx.denom) + sfx.start;

			AudioSFX2.pitch = engineSFX1;

			if (KB.Right || KB.Left) {

				if (!AudioSFX2.isPlaying) AudioSFX2.Play();
				AudioSFX2.volume = 0.3f;


			} else
			{
				//if (AudioSFX2.isPlaying) AudioSFX2.Stop();
				AudioSFX2.volume = 0.25f;
			}

			//AudioSFX2.pitch  = ((CurrentSpeed / (10 + engineSFX1)) % 1f) - (engineSFX1 / 2);
			//AudioSFX3.pitch = (CurrentSpeed / 70) % 1 + 1;
		}


		public void SetSpeed(float speed, bool delta = true, bool turnOn = true)
		{
			if (CurrentSpeed > 30 && delta) speed *= 0.1f;

			if (!delta) CarB.MotorSpeed = speed;
			else CarB.MotorSpeed       += speed;
		}


		public void CheckStatus()
		{
			if (CarT == null || Body == null || Puppet == null)
			{
				Puppet.IsInVehicle = false;

				APCar[0].ClearMove();
				APCar[1].ClearMove();

				Puppet.RunRigids(Puppet.RigidReset);

				Util.Destroy(this);
			}
		}

		//
		// ─── REGISTER CAR ────────────────────────────────────────────────────────────────
		//
		public bool RegisterCar()
		{

			if (Registered) {
				state    = CarStates.OpenDoor;
				carTimer = Time.time + 3f;
				initSet  = false;
				return false;
			}

			for (int i = 0; i <= 1; i++)
			{
				string moveName                           = string.Format("car_{0}", i + 1);
				APCar[i]                                  = new MoveSet(moveName, false);
				APCar[i].Ragdoll.ShouldStandUpright       = false;
				APCar[i].Ragdoll.State                    = PoseState.Sitting;
				APCar[i].Ragdoll.Rigidity                 = 2.2f;
				APCar[i].Ragdoll.AnimationSpeedMultiplier = 2.5f;
				APCar[i].Import();
			}


			TotalWeight       = 0;
			CarT              = transform;

			foreach (PhysicalBehaviour pb in CarT.gameObject.GetComponentsInChildren<PhysicalBehaviour>())
			{
				pb.spriteRenderer.sortingLayerName = "Background";

				if (pb.name  == "Body")     Body        = pb;
				if (pb.name  == "FrontDoor")FrontDoor   = pb;
				if (pb.name  == "BackDoor") BackDoor    = pb;
				if (pb.name  == "Boot")     Boot        = pb;
				if (pb.name  == "Bonnet")   Bonnet      = pb;
				if (pb.name  == "Wheel1")   Wheel1      = pb;
				if (pb.name  == "Wheel2")   Wheel2      = pb;
				if (pb.name  == "Engine")   Engine      = pb;
				if (pb.name  == "Gastank")  GasTank     = pb;
				TotalWeight += pb.rigidbody.mass;

				if (CarLayers.ContainsKey(pb.name)) pb.spriteRenderer.sortingOrder = CarLayers[pb.name];
			}

			CarB  = gameObject.GetComponentInChildren<CarBehaviour>();
			Speed = CarB.MotorSpeed;

			AudioSFX1              = gameObject.AddComponent<AudioSource>();
			AudioSFX2              = gameObject.AddComponent<AudioSource>();
			AudioSFX3              = gameObject.AddComponent<AudioSource>();

			AudioSFX2              = Engine.gameObject.AddComponent<AudioSource>();
			AudioSFX2.clip         = JTMain.CarEngine;
			AudioSFX2.loop         = true;
			AudioSFX2.maxDistance  = 15f;
			AudioSFX2.minDistance  = 0.1f;
			AudioSFX2.dopplerLevel = 3f;
			AudioSFX2.volume       = 0.4f;
			//AudioSFX2.

			AudioSFX3              = Engine.gameObject.AddComponent<AudioSource>();
			AudioSFX3.clip         = JTMain.CarIdle;
			AudioSFX3.loop         = true;
			AudioSFX3.maxDistance  = 15f;
			AudioSFX3.minDistance  = 1.4f;
			AudioSFX3.dopplerLevel = 3f;

			Util.FireProof(Wheel1);
			Util.FireProof(Wheel2);

			DisableCollisions();

			Transform carx = Body.transform.root;
			Vector3 pos    = Body.transform.position;
			pos.y         += 2.5f;
			carx.position  = pos;

			state      = CarStates.Ready;
			Registered = true;

			AudioSFX1.PlayOneShot(UnityEngine.Random.Range(1,3) == 1 ? JTMain.CarUnlock1 : JTMain.CarUnlock2);

			return true;
		}

		public void ApplyCarLayers()
		{
			foreach (PhysicalBehaviour pb in CarT.gameObject.GetComponentsInChildren<PhysicalBehaviour>())
			{
				if (CarLayers.ContainsKey(pb.name)) {
					pb.spriteRenderer.sortingOrder     = CarLayers[pb.name];
					pb.spriteRenderer.sortingLayerName = "Background";
				}
			}
		}

		public void DisableCollisions()
		{
			Util.ToggleCollisions(CarT, Puppet.RB2["Head"].transform.root, false, true);

			foreach (Transform tr in Puppet.customParts.Transforms)
			{
				if (tr != null) Util.ToggleCollisions(CarT, tr, false, false);
			}

			if (Puppet.HandThing != null) DisableHeldItemCollisions(Puppet.HandThing);
		}

		public void DisableHeldItemCollisions(Thing thing)
		{
			Util.ToggleCollisions(thing.tr, CarT, false, true);
		}



		//
		// ─── EXIT CAR ────────────────────────────────────────────────────────────────
		//
		public void ExitCar()
		{
			SetSpeed(0f, false, false);
			DisableCollisions();

			Puppet.PBO.OverridePoseIndex = -1;

			Puppet.RunRigids(Puppet.RigidReset);

			state               = CarStates.GettingOut;
			carTimer            = Time.time + 1f;
			Puppet.IsInVehicle  = false;

			Action leaveCar                  = () => {
				Puppet.IsInVehicle           = false;
				Puppet.PBO.OverridePoseIndex = -1;
			};

			Effects.Speedometer(Effects.SpeedometerTypes.Off, 0);

			PuppetMaster.Master.AddTask(leaveCar, 1.0f);

			if (Puppet.HandThing.JointType == JointTypes.HingeJoint)
				Puppet.HandThing.ChangeJoint(JointTypes.FixedJoint);

		}


		public void OpenCarDoor()
		{

			if (Puppet.Facing != Facing)
			{
				FlipCar();
				return;
			}

			bool hasDoor = (Vector2.Distance(FrontDoor.rigidbody.position, Body.rigidbody.position) < 3.5f);

			if (hasDoor && Body.rigidbody.velocity.magnitude > 10 )
			{
				// Blow off the door
				HingeJoint2D[] joints = FrontDoor.GetComponents<HingeJoint2D>();
				foreach (HingeJoint2D joint in joints) {

					Util.ToggleCollisions(FrontDoor.transform, CarT, false);

					ModAPI.CreateParticleEffect("Flash", joint.transform.position);
					ModAPI.CreateParticleEffect("Spark", joint.transform.position);
					Util.DestroyNow((UnityEngine.Object)joint);
				}
				FrontDoor.rigidbody.AddForce(Vector2.down * 15f, ForceMode2D.Impulse);
				FrontDoor.rigidbody.AddTorque(Facing * -12, ForceMode2D.Impulse);
				Puppet.ChaseVehicle = false;

				ApplyCarLayers();
				return;
			}

			Transform tr = FrontDoor.transform;

			if (hasDoor) { 

				FrontDoor.spriteRenderer.sortingOrder     = 10;
				FrontDoor.spriteRenderer.sortingLayerName = "Foreground";
				Body.spriteRenderer.sortingOrder          = -1;
				Body.spriteRenderer.sortingLayerName      = "Background";

				if (!initSet) { 
					Vector3 bodyFlipped = Body.transform.position;
					tr.position         = new Vector3(bodyFlipped.x + (Mathf.Lerp(-0.6f * Facing, -2.1f * Facing, Time.deltaTime * 3) ), bodyFlipped.y + 0.045f);
					initSet             = true;
					if (Puppet.IsInVehicle) APCar[1].ClearMove();
					playedClip = false;
				}

				tr.localScale    = Vector3.Lerp(tr.localScale, new Vector3(0.5f, 1f), Time.deltaTime * 3);
				tr.position      = Vector3.Lerp(tr.position, new Vector3(Body.transform.position.x + (-1.1f * Facing), tr.position.y, 0), Time.deltaTime * 3);
			}


			if (!hasDoor || Math.Round(tr.localScale.x,1) == 0.5f || Time.time > carTimer )
			{
				DisableCollisions();

				counter = 0;
				initSet  = false;
				if (Puppet.IsInVehicle)
				{
					CarB.Activated      = false;
					CarB.IsBrakeEngaged = false;
					carTimer            = Time.time + 2f;
					Puppet.IsInVehicle  = false;
					state               = CarStates.CloseDoor;

					APCar[1].ClearMove();
					FrontDoor.spriteRenderer.sortingOrder     = -1;
					FrontDoor.spriteRenderer.sortingLayerName = "Background";
					return;
				} else
				{
					carTimer = Time.time + 8f;
					state    = CarStates.GettingIn;
					GetInCar(true);
					return;
				}
			}

			//ModAPI.Notify("localScale" + tr.localScale);
		}


		public void CloseCarDoor()
		{
			bool hasDoor = (Vector2.Distance(FrontDoor.rigidbody.position, Body.rigidbody.position) < 1.5f);

			Transform tr = FrontDoor.transform;

			if (hasDoor) { 

				//FrontDoor.spriteRenderer.sortingOrder       = -1;
				//FrontDoor.spriteRenderer.sortingLayerName   = "Background";

				tr.localScale           = Vector3.Lerp(tr.localScale, new Vector3(1f, 1f), Time.deltaTime * 3);
				tr.position             = Vector3.Lerp(tr.position, new Vector3(Body.transform.position.x + (-0.6f * Facing), tr.position.y, 0), Time.deltaTime * 3);
			}

			if (Puppet.IsInVehicle)
			{
				Vector2 pos = Body.rigidbody.position + (new Vector2(-0.1f * Facing, -0.12f) );
				Vector2 pos2 = Body.rigidbody.position + (new Vector2(-1f * Facing, -0.12f) );
				Puppet.RB2["Foot"].MovePosition(pos2);
				Puppet.RB2["FootFront"].MovePosition(pos2);
				Puppet.RB2["LowerBody"].MovePosition(pos);
				Puppet.RB2["LowerBody"].MoveRotation(-18f * Puppet.Facing);
				if (Vector2.Distance(pos, Puppet.RB2["LowerBody"].position) < 0.2f) Puppet.RunRigids(Puppet.RigidStop);
				if (Time.time > carTimer2)
				{
					currentStep = 4;
					foreach (KeyValuePair<string, LimbBehaviour> pair in Puppet.LB)
					{
						if (!Util.IsColliding(pair.Value.transform, Body.transform, false))
						{
							carTimer *= 0.09f;
							Util.ToggleCollisions(pair.Value.transform, Body.transform, true, false);
						}
					}
				}
			}

			if (Time.time > carTimer / 1.8f)
			{
				if (!playedClip && hasDoor) AudioSFX1.PlayOneShot(JTMain.DoorClose);
				playedClip = true;

			}

			if (Time.time > carTimer)
			{
				if (hasDoor) { 
					Vector3 bodyFlipped = Body.transform.position;
					Vector3 theScale    = tr.localScale;

					tr.localScale       = theScale;
					tr.position         = new Vector3(bodyFlipped.x - (0.6f * Facing), bodyFlipped.y + 0.05f);
					tr.rotation         = Quaternion.Euler(0.0f, 0.0f, 0.0f);
				}

				if (Puppet.IsInVehicle) {
					state                                     = CarStates.Driving;
					BackDoor.spriteRenderer.sortingOrder      = 10;
					BackDoor.spriteRenderer.sortingLayerName  = "Foreground";
					Body.spriteRenderer.sortingOrder          = 10;
					Body.spriteRenderer.sortingLayerName      = "Foreground";
					AudioSFX1.volume            = 0.5f;
					AudioSFX1.PlayOneShot(JTMain.CarStart);
					AudioSFX3.PlayDelayed(0.5f);
				}
				else {
					ApplyCarLayers();
					DisableCollisions();
					state = CarStates.Ready;
					Puppet.PBO.OverridePoseIndex = -1;
					Puppet.RunRigids(Puppet.RigidReset);
					Effects.Speedometer(Effects.SpeedometerTypes.Off, 0);
					//AudioSFX1.PlayOneShot(CarB.EngineShutoff);
					AudioSFX3.Stop();
					AudioSFX2.Stop();
					CarB.IsBrakeEngaged = false;
					CarB.Activated = false;
				}
			}


			//ModAPI.Notify("localScale" + FrontDoor.transform.localScale);
		}

		//
		// ─── GET IN CAR ────────────────────────────────────────────────────────────────
		//
		[SkipSerialisation]
		public void GetInCar(bool init=false)
		{
			if (Time.time > carTimer)
			{
				currentStep = 4;
				carTimer = Time.time + 100;
				return;
			}

			if (init)
			{
				if (!Util.IsColliding(Puppet.LB["Head"].transform.root, CarT, false))
				{
					state = CarStates.CloseDoor;
					carTimer = Time.time + 3f;
					counter = 0;
					return;
				}

				currentStep = 0;
				Puppet.PBO.OverridePoseIndex = -1;
			}



			if (currentStep == 0)
			{
				Vector2 DoorTarget = Body.rigidbody.position + new Vector2(0.5f, -0.1f);
				Vector2 PuppetPos  = Puppet.RB2["LowerBody"].position;

				float dist = Vector2.Distance(DoorTarget, PuppetPos);

				if (dist < 0.2f)
				{
					Puppet.RunRigids(Puppet.RigidStop);
					Puppet.PBO.OverridePoseIndex = -1;
					APCar[0].RunMove();
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

				APCar[0].RunMove();
				Util.Notify("Getting in Car", VerboseLevels.Full);

				Puppet.Inventory.PutAwayItems(ItemTypes.VehicleSafe);

				if (Puppet.HandThing != null) DisableHeldItemCollisions(Puppet.HandThing);

				currentStep = 2;

				carTimer2 = Time.time + 0.2f;

				return;
			}

			if (currentStep == 2)
			{
				if (Time.time > carTimer2) {
					currentStep = 3; //return;
					APCar[1].RunMove();
					carTimer2 = Time.time + 1.0f;
					return;

				}
			}

			if (currentStep == 3)
			{
				APCar[1].RunMove();
				Vector2 pos = Body.rigidbody.position + new Vector2(-0.1f * Facing, -0.11f) ;

				Puppet.RB2["LowerBody"].MovePosition(pos);
				Puppet.RB2["LowerBody"].MoveRotation(-18f * Puppet.Facing);

				float dist = Vector2.Distance(pos, Puppet.RB2["LowerBody"].position);

				if (dist < 0.2f) {
					Puppet.PBO.Consciousness   = 1f;
					Puppet.PBO.ShockLevel      = 0.0f;
					Puppet.PBO.PainLevel       = 0.0f;
					Puppet.PBO.OxygenLevel     = 1f;
					Puppet.PBO.AdrenalineLevel = 1f;

					currentStep = 4;

					Puppet.IsInVehicle = Puppet.ChaseVehicle = true;
				}
				return;
			}

			if (currentStep == 4)
			{

				if (!Puppet.IsInVehicle) APCar[0].ClearMove();
				state    = CarStates.CloseDoor;
				carTimer = Time.time + 3f;
				counter  = 0;
				return;
			}




		}


		void FlipCar(bool AfterFrame = false)
		{
			if (!AfterFrame)
			{
				Transform carx    = Body.transform.root;
				carx.position     = Body.transform.position;
				doFlip            = true;
				return;
			}

			Vector3 flipper = Body.transform.root.localScale;
			flipper.x *= -1;

			Vector3 pos = Body.transform.position;

			Body.transform.root.localScale = flipper;
			Body.transform.root.position   = pos;

			doFlip = false;
		}

		void OnDestroy()
		{
			if (Puppet != null && Puppet.IsInVehicle) {
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

			if (coll.gameObject.name == "LowerBody" && state == CarStates.Ready)
			{
				DisableCollisions();

				if (Facing != Puppet.Facing) FlipCar();

				Puppet.Actions.jump.State     = JumpState.ready;
				Puppet.Actions.dive.State     = DiveState.ready;
				Puppet.Actions.backflip.State = BackflipState.ready;
				Puppet.PBO.OverridePoseIndex  = -1;

				Puppet.RunRigids(Puppet.RigidReset);

				counter = 0;
				state = CarStates.GettingIn;
			}
		}
	}
}
