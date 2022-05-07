//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System;
using UnityEngine;

namespace PuppetMaster
{
	public enum ThrowState
	{
		ready,
		aim,
		windup,
		fire,
		bikewindup,
		bikefire,
	}

	public class ThrowItem
	{
		private XTimer xTimer         = new XTimer();
		public ThrowState _state      = ThrowState.ready;
		public Puppet Puppet          = null;

		public int frame              = 0;

		public float power            = 2.2f;
		public float facing           = 1f;

		public PuppetHand Hand;
		private float initGravity;

		protected Material lineMaterial;

		private LineRenderer lr;

		private Vector2 ThrowStartPos;
		private Vector2 ThrowEndPos;

		private Vector2 HeadPos;

		private Vector2 _velocity;

		private MoveSet AP_Throw1;
		private MoveSet AP_Throw2;

		public Thing thing;


		public ThrowState state
		{
			get { return _state; }
			set { 
				_state = value;
				if (Puppet != null) { 
					if (Puppet.PG.IsAiming) Puppet.PG.AimMode = AimModes.Off;
					Puppet.IsAiming =
					Puppet.IsPointing = 
					Puppet.Actions.CombatMode = false;
				}
			}
		}



		//
		// ─── THROW INIT ────────────────────────────────────────────────────────────────
		//
		public void Init(PuppetHand hand)
		{
			Hand   = hand;
			frame  = 0;
			xTimer = new XTimer();

			if (state == ThrowState.ready)
			{

				if (!Hand.IsHolding || Hand.Thing == null) return;

				thing = Hand.Thing;

				//if (Puppet.PG.IsAiming) Puppet.PG.IsAiming = false;


				//Hand.Thing.doNotDispose = true;
				frame                   = 0;
				facing                  = Puppet.FacingLeft ? -1 : 1;


				// - - - - - - - - - - - - 
				//  throwing from bike
				//
				if (Puppet.IsInVehicle)
				{
					state = ThrowState.bikewindup;
					//ThrowStartPos = Puppet.RB2["Head"].position;

					//Vector2 mousePo = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

					//ThrowEndPos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
					//Vector2 _velocity = (ThrowEndPos - ThrowStartPos) * Garage.Bike.CurrentSpeed;
				} 
				else
				{
					state = ThrowState.aim;

					AP_Throw1 = new MoveSet("throw_1", false);
					AP_Throw1.Ragdoll.Rigidity                 = 2.2f;
					AP_Throw1.Ragdoll.AnimationSpeedMultiplier = 10.5f;
					AP_Throw1.Import();

					AP_Throw2                                  = new MoveSet("throw_2", false);
					AP_Throw2.Ragdoll.Rigidity                 = 2.2f;
					AP_Throw2.Ragdoll.AnimationSpeedMultiplier = 10.5f;
					AP_Throw2.Import();

					InitLine();

					Puppet.BlockMoves = true;
				}

				initGravity = Hand.Thing.P.InitialGravityScale;

			}
		}

		public void Go2()
		{
			if (state == ThrowState.aim)
			{
				if ( !xTimer.flag )
				{
					xTimer.flag = true;
					xTimer.Restart();
					AP_Throw1.RunMove();
					HeadPos  = PuppetMaster.Puppet.RB2["Head"].position;
				}

				if (facing == 1f  && Global.main.MousePosition.x < HeadPos.x) { lr.enabled = false; return; }
				if (facing == -1f && Global.main.MousePosition.x > HeadPos.x) { lr.enabled = false; return; }

				lr.enabled      = true;

				//  If mouse is at edge of screen lets zoom out
				if (Input.mousePosition.y >= Screen.height * 0.95f ||
					Input.mousePosition.x >= Screen.width * 0.95f  ||
					Input.mousePosition.x <= 10f || Input.mousePosition.y <= 10f) {

					if (PuppetMaster.ChaseCam.CustomMode != CustomModes.DistantAiming)
						PuppetMaster.ChaseCam.CustomMode  = CustomModes.DistantAiming;
				}

				ThrowStartPos        = HeadPos;
				ThrowEndPos          = Global.main.MousePosition;
				_velocity            = (ThrowEndPos - ThrowStartPos) * power;
				Vector3[] trajectory = Plot(ThrowStartPos, _velocity, 1000);
				lr.positionCount     = trajectory.Length;

				lr.SetPositions(trajectory);

				if (!KB.Throw)
				{
					frame = 0;
					state = ThrowState.windup;
					return;
				}

			}

		}



		//
		// ─── THROW GO ────────────────────────────────────────────────────────────────
		//
		public void Go()
		{
			if (Hand.Thing == null)
			{
				PuppetMaster.Puppet.PBO.OverridePoseIndex = 0;
				lr.enabled                                = false;
				state                                     = ThrowState.ready;
				PuppetMaster.Puppet.BlockMoves            = false;
				AP_Throw2.ClearMove();
				AP_Throw1.ClearMove();
				return;
			}

			frame++;

			if (state == ThrowState.aim)
			{
				AP_Throw1.RunMove();

				if (KB.MouseDown || KB.Action) Hand.Thing.P.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

				if (!KB.Throw)
				{
					frame = 0;
					state = ThrowState.windup;
					return;
				}

				if (frame == 10)
				{
					ThrowStartPos     = PuppetMaster.Puppet.RB2["Head"].position;
					ThrowEndPos       = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
					//Vector2 _velocity = (ThrowEndPos - ThrowStartPos) * Garage.Bike.CurrentSpeed;
				}

				if (frame > 10)
				{
					if (!lr.enabled)
					{
						lr.enabled    = true;
						ThrowStartPos = PuppetMaster.Puppet.RB2["Head"].position;
					}

					lr.enabled = true;

					//Vector2 mousePo = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
					Vector2 mousePo = Global.main.MousePosition;
					if (facing == 1f  && mousePo.x < Puppet.RB2["Head"].position.x) { lr.enabled = false; return; }
					if (facing == -1f && mousePo.x > Puppet.RB2["Head"].position.x) { lr.enabled = false; return; }

					//  If mouse is at edge of screen lets zoom out
					if (Input.mousePosition.y >= Screen.height * 0.95f ||
						Input.mousePosition.x >= Screen.width * 0.95f  ||
						Input.mousePosition.x <= 10f || Input.mousePosition.y <= 10f)
					{
						if (PuppetMaster.ChaseCam.CustomMode != CustomModes.DistantAiming)
							PuppetMaster.ChaseCam.CustomMode  = CustomModes.DistantAiming;


						//if (Global.main.camera.orthographicSize < 40f ) Global.main.camera.orthographicSize *= 1.01f;

						//Vector3 camTarget = Global.main.camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Global.CameraPosition.z));
						//Vector3 oldCamPos = Global.main.camera.transform.position;

						//camTarget.z = 0f;
						//Vector3 direction = camTarget - oldCamPos;

						//Global.CameraPosition += direction * Time.deltaTime;
						//Global.CameraPosition = Util.ClampPos(Global.CameraPosition);


					}// else if (PuppetMaster.ChaseCam.CustomMode != CustomModes.DistantAiming)

					ThrowEndPos          = Global.main.MousePosition;
					Vector2 _velocity    = (ThrowEndPos - ThrowStartPos)* power;

					Vector3[] trajectory = Plot(ThrowStartPos, _velocity, 1000);

					lr.positionCount     = trajectory.Length;

					lr.SetPositions(trajectory);
				}

				return;
			}

			if (state == ThrowState.windup)
			{
				if (frame == 1)
				{
					if (PuppetMaster.ChaseCam.CustomMode == CustomModes.DistantAiming)
						PuppetMaster.ChaseCam.CustomMode  = CustomModes.Off;

					AP_Throw2.RunMove();
				}
				if (frame > 4)
				{
					Puppet.RunRigids(Puppet.RigidReset);

					float armForce = (ThrowEndPos - ThrowStartPos).magnitude;

					Puppet.PBO.OverridePoseIndex = -1;

					Hand.RB.AddTorque(Mathf.Clamp(armForce, 5f, 10f) * Puppet.TotalWeight * -facing);
				}
				if ( frame > 5 )
				{
					if ( Hand.RB.position.x * -Puppet.Facing > Puppet.RB2["Head"].position.x * -Puppet.Facing )
					{
						Hand.Drop();
						frame = 0;
						state = ThrowState.fire;
						PuppetMaster.ChaseCam.QuickChase(thing.P);

					}
				}

				//if (frame >= 12)
				//{
				//    //thing.R.interpolation = RigidbodyInterpolation2D.Extrapolate;


				//    frame = 0;
				//    state = ThrowState.fire;
				//}
				return;
			}


			if (state == ThrowState.fire)
			{
				if ( frame == 1 )
				{
					thing.R.velocity      = Vector2.zero;

					if (KB.Modifier) {
						Util.MaxPayne(true);
						PuppetMaster.isMaxPayne = true;
					}
				}

				if ( frame == 2 )
				{

					Puppet.PBO.OverridePoseIndex    = 0;
					lr.enabled                      = false;
					state                           = ThrowState.ready;
					Puppet.BlockMoves               = false;

					_velocity                       = (ThrowEndPos - ThrowStartPos) * power;

					thing.R.velocity                = _velocity;

					thing.JustThrown();
				}
			}

			//if (state == ThrowState.bikewindup)
			//{
			//    if (frame == 1)
			//    {
			//        thing.ChangeJoint(JointTypes.FixedJoint);
			//    }
			//    if (frame == 2)
			//    {
			//        Hand.uArmL.Broken = true;
			//    }
			//    if (frame > 3)
			//    {
			//        Hand.uArm.rigidbody.AddTorque(10f * Puppet.TotalWeight * -facing);
			//    }
			//    if (frame > 9)
			//    {
			//        ThrowStartPos  = Puppet.RB2["Head"].position;
			//        ThrowEndPos    = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
			//        float armForce = (ThrowEndPos - ThrowStartPos).magnitude;

			//    }
			//    if (frame > 10)
			//    {
			//        Puppet.PG.Drop(thing);
			//        thing.BreakConnections();
			//        frame = 0;
			//        state = ThrowState.bikefire;
			//    }
			//    return;
			//}


			//if (state == ThrowState.bikefire)
			//{
			//    if (frame == 1)
			//    {
			//        Hand.uArmL.Broken = false;
			//        thing.R.interpolation         = RigidbodyInterpolation2D.Extrapolate;
			//        thing.R.velocity              = Vector2.zero;
			//    }
			//    if (KB.Modifier)
			//    {
			//        Util.MaxPayne(true);
			//        PuppetMaster.isMaxPayne = true;
			//    }

			//    if (frame == 10) {


			//        PuppetMaster.ChaseCam.ChaseItem  = thing.P;
			//        PuppetMaster.ChaseCam.CustomMode = CustomModes.BikeThrow;

			//        state                = ThrowState.ready;
			//        Vector2 _velocity    = (ThrowEndPos - ThrowStartPos) * power * 2;
			//        thing.R.velocity = _velocity;

			//        thing.JustThrown();

			//    }
			//}
		}

		//
		// ─── THROW Plot ────────────────────────────────────────────────────────────────
		//
		public Vector3[] Plot(Vector2 pos, Vector2 velocity, int steps)
		{
			Vector3[] results    = new Vector3[steps];
			float timestep       = Time.fixedDeltaTime / Physics2D.velocityIterations;
			Vector2 gravityAccel = Physics2D.gravity * initGravity * timestep * timestep;
			//float drag           = 1f - timestep * initDrag;
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

		private void InitLine()
		{
			if (lr != null) return;

			lr = Puppet.PBO.gameObject.GetOrAddComponent<LineRenderer>();

			lr.enabled           = false;
			lr.startColor        = new Color(1f, 0.1f, 0.1f, 1f);
			lr.endColor        = new Color(0.1f, 0.1f, 1f, 1f);
			//lr.startColor        = new Color(1f, 0.3f, 0f, 0.25f);
			//lr.endColor          = new Color(1f, 0.4f, 0f, 0.5f);
			lr.startWidth        = 0.06f;
			lr.endWidth          = 0.06f;
			lr.numCornerVertices = 0;
			lr.numCapVertices    = 0;
			lr.useWorldSpace     = true;
			lr.alignment         = LineAlignment.View;
			lr.sortingOrder      = 2;
			lr.material          = Resources.Load<Material>("Materials/PhaseLink");

			//lr.material          = ModAPI.FindMaterial("Sprites-Default");
			lr.textureMode       = LineTextureMode.DistributePerSegment;
			lr.textureMode       = LineTextureMode.Tile;
			lr.hideFlags         = HideFlags.HideAndDontSave;
		}
	}
}
