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
	[SkipSerialisation]
	public class ChaseCamBehaviour : MonoBehaviour
	{
		public GameObject ChaseObj;
		public PhysicalBehaviour ChasePB;
		public PhysicalBehaviour ChaseTarget;
		public PhysicalBehaviour ChaseItem;
		public Rigidbody2D RealPuppet;
		public Transform CTT;


		private Puppet _puppet;
		private ChaseModes _chaseMode    = ChaseModes.Idle;
		private CustomModes _customModes = CustomModes.Off;
		private bool _customCamActive    = false;

		public float yOffsetTemp         = 0f;
		public float yOffsetPct          = 50f;
		public float yFactor             = 0f;
		public float LookAheadX          = 2.0f;
		public float SpeedAhead          = 2.0f;
		public float LookAheadFast       = 2.0f;
		public float ItemTimeout         = 2.0f;
		public float yThreshold          = 1.0f;
		public float xThreshold          = 2.0f;
		public float screenY             = Screen.height;
		public float smoothSpeed         = 0.5f;
		public float Extend              = 50f;
		public Vector3 TargetPos         = Vector3.zero;
		public Vector3 CamPos            = Vector3.zero;
		public bool LockY                = false;
		private float ZoomCustom         = 0f;
		private float miscFloat          = 0f;
		private bool IsMoving            = false;
		private bool checkedMouse        = false;
		private bool targetClicked       = false;
		private readonly float maxSpeed  = 5.0f;
		private float notMoving          = 0f;
		private float SpeedControl       = 3;
		private bool startSlow           = false;
		private float timeSlow           = 0;
		private bool yShift              = false;
		public bool FreezeY              = false;
		private int NotMoving            = 0;

		public float yOffset;
		public Camera GCam;
		public float CamSize;
		public float GCZoom;
		public float GCZoomV;
		private Vector3 velocity;

		public BoxCollider2D mapBounds;

		public Bounds BoundingBox = new Bounds(
			new Vector3(595f, 497f, 0f),
			new Vector3(1200f, 990f, 0f));

		//
		// ─── CHASECAM PROPS ────────────────────────────────────────────────────────────────
		//
		public Puppet Puppet
		{
			get
			{
				if (_puppet == null) _puppet = PuppetMaster.Puppet;
				return _puppet;
			}
			set { _puppet = value; }
		}

		public bool CustomCamActive
		{
			get { return _customCamActive; }
			set
			{
				if (_customCamActive != value)
				{
					if (value == true) ActivateChaseCam();
					else DeactivateChaseCam();
				}
				_customCamActive = value;
			}
		}

		public CustomModes CustomMode
		{
			get { return _customModes; }
			set { 
				ZoomCustom   = GCam.orthographicSize;
				_customModes = value; 

				if ( value == CustomModes.SonicBoom ) miscFloat = Time.time + 2f;

				// if ( value == CustomModes.Off ) GCam.orthographicSize = GCZoom;
			}
		}
		public ChaseModes ChaseMode
		{
			get { return _chaseMode; }
			set { 
				_chaseMode = value;
				if ( value == ChaseModes.Vehicle )
				{
					GCZoomV       = GCam.orthographicSize;
					LookAheadFast = LookAheadX;     
				}
				else if ( value == ChaseModes.Item )
				{
					GCZoomV       = GCam.orthographicSize;
				}
			}
		}


		//
		// ─── UNITY UPDATE ────────────────────────────────────────────────────────────────
		//
		public void Update()
		{
			if (!_customCamActive || DialogBox.IsAnyDialogboxOpen || Global.ActiveUiBlock || Global.main.UILock) return;


			if (Input.GetKey(KeyCode.None)) return;

			if (InputSystem.Held("zoomIn"))  ZoomCam(Time.unscaledDeltaTime  * 2);
			if (InputSystem.Held("zoomOut")) ZoomCam(-Time.unscaledDeltaTime * 2);

			if (Mathf.Abs(Input.mouseScrollDelta.y) > 0f ) ZoomCam(Input.mouseScrollDelta.y);
			if (Mathf.Abs(Input.mouseScrollDelta.y) < 0f ) ZoomCam(Input.mouseScrollDelta.y);

			if (InputSystem.Held("panDown"))    DisableCam();
			if (InputSystem.Held("panUp"))      DisableCam();
			if (InputSystem.Held("panLeft"))    DisableCam();
			if (InputSystem.Held("panRight"))   DisableCam();
			if (InputSystem.Held("pan"))        DisableCam();
		}


		//
		// ─── UNITY LATE UPDATE ────────────────────────────────────────────────────────────────
		//
		void LateUpdate()
		{
			if (!_customCamActive || DialogBox.IsAnyDialogboxOpen || Global.ActiveUiBlock || Global.main.UILock) return;

			if (CustomMode != CustomModes.Off) RunCustomMode();

			//  Prevent chasecam following puppet when its being dragged around the screen
			if (KB.MouseDown)
			{
				if (!checkedMouse) CheckWhatsClicked();
				if (targetClicked) return;
			}
			else checkedMouse = targetClicked = false;

			if     (ChaseMode == ChaseModes.Puppet)  TrackPuppet();
			else if(ChaseMode == ChaseModes.Vehicle) TrackVehicle();
			else if(ChaseMode == ChaseModes.Item)    TrackItem();


		}

		//
		// ─── ZOOM CAM ────────────────────────────────────────────────────────────────
		//
		public void ZoomCam(float amount)
		{
			if (ChaseMode == ChaseModes.Puppet)
				GCZoom = Mathf.Clamp(GCZoom - (amount * 0.1f * UserPreferenceManager.Current.ZoomSensitivity) * GCam.orthographicSize, 2, 16);

			else if (ChaseMode == ChaseModes.Vehicle)
				GCZoomV = Mathf.Clamp(GCZoomV - (amount * 0.1f * UserPreferenceManager.Current.ZoomSensitivity) * GCam.orthographicSize, 2, 16);

			else if (ChaseMode == ChaseModes.Item)
				GCZoomV = Mathf.Clamp(GCZoomV - (amount * 0.2f * UserPreferenceManager.Current.ZoomSensitivity) * GCam.orthographicSize, 2, 16);

		}

		public void UpdateZoom(bool doImmediately=false)
		{
			if (doImmediately) GCam.orthographicSize = Mathf.Clamp(GCZoom, 2,16);

			else GCam.orthographicSize = Mathf.Clamp(Mathf.Lerp(GCam.orthographicSize, GCZoom, Time.deltaTime * 3),2,16);

			yOffset  = GCam.orthographicSize * yFactor;
			IsMoving = true;
		}


		//
		// ─── CHECK WHATS CLICKED ────────────────────────────────────────────────────────────────
		//
		private bool CheckWhatsClicked()
		{
			checkedMouse  = true;
			targetClicked = false;

			if (Puppet == null) Puppet = PuppetMaster.Puppet;

			Collider2D[] NoCollide = Puppet.PBO.transform.root.GetComponentsInChildren<Collider2D>();

			foreach (Collider2D collider in NoCollide)
			{
				if (collider == null || !collider ) continue;

				if ((bool)collider?.OverlapPoint((Vector2)Global.main.MousePosition))
				{
					targetClicked = true;
					break;
				}
			}

			if (Puppet.IsInVehicle)
			{
				if (Garage.Bike?.state != BikeStates.Ready)
					NoCollide = Garage.Bike.BikeT.GetComponentsInChildren<Collider2D>();

				else if (Garage.Car?.state != CarStates.Ready)
					NoCollide = Garage.Car.CarT.GetComponentsInChildren<Collider2D>();

				foreach (Collider2D collider in NoCollide)
				{
					if (collider == null || !collider) continue;

					if ((bool)collider?.OverlapPoint((Vector2)Global.main.MousePosition))
					{
						targetClicked = true;
						break;
					}
				}
			}

			if (ChaseMode == ChaseModes.Item )
			{
				if (ChaseItem != null) { 
					NoCollide = ChaseItem.transform.root.GetComponents<Collider2D>();
					foreach (Collider2D collider in NoCollide)
					{
						if (collider == null || !collider) continue;

						if ((bool)collider?.OverlapPoint((Vector2)Global.main.MousePosition))
						{
							targetClicked = true;
							break;
						}
					}
				}
			}

			return targetClicked;
		}

		//
		// ─── SET PUPPET ────────────────────────────────────────────────────────────────
		//
		public void SetPuppet(Puppet puppet, bool resetY)
		{
			Puppet          = puppet;
			ChaseTarget     = puppet.LB["LowerBody"].PhysicalBehaviour;
			CTT             = ChaseTarget.transform;
			CustomCamActive = true;
			IsMoving        = true;
			notMoving       = 0f;


			//  Set the constant Yoffset
			if (resetY)
			{
				GCZoom  = GCam.orthographicSize;
				yOffset = GCam.transform.position.y - CTT.position.y;
				yFactor = yOffset / GCZoom;
				CamPos = ChaseTarget.transform.position;

			}// else 
			UpdateZoom(true);

			CamPos = ChaseTarget.transform.position;

			//  have camera look ahead the direction puppet is facing
			//  have camera Keep the chosen Y offset from the players position
			CamPos.x += LookAheadX * -Puppet.Facing;
			CamPos.y += yOffset;

			GCam.transform.position = CamPos;

			ChaseMode = ChaseModes.Puppet;

		}


		//
		// ─── TRACK PUPPET ────────────────────────────────────────────────────────────────
		//
		public void TrackPuppet()
		{
			if (CTT == null) Stop();
			TargetPos = CTT.position;
			CamPos    = GCam.transform.position;

			Vector2 targetVelocity = ChaseTarget.rigidbody.velocity;

			float yVal = yOffset;

			if ( !FreezeY )
			{
				if (!yShift && yOffsetTemp == 0) {
					if (targetVelocity.y < -7f) yShift  = true;
				}
				else {

					yVal = Mathf.Clamp(targetVelocity.y * 0.1f, -15f, 0);

					if (targetVelocity.y > -2f) yShift = false;
				}


				if (yOffsetTemp != 0f) yVal = yOffsetTemp * GCZoom;

				TargetPos.y += yVal;

			} else LockY = true;

			TargetPos.x += LookAheadX * -Puppet.Facing;

			float diffX            = Mathf.Abs(CamPos.x - TargetPos.x);
			float diffY            = Mathf.Abs(CamPos.y - TargetPos.y);

			float camtude          = GCam.velocity.magnitude;
			float targetude        = targetVelocity.magnitude;

			float vDistance        = Vector3.SqrMagnitude(CamPos - TargetPos);
			float vMaxSpeed        = Mathf.Max(maxSpeed, vDistance);

			if (targetude > camtude) { SpeedControl = Mathf.Clamp(SpeedControl + 0.01f, 0.01f, 2f); startSlow = false; }
			else
			{
				if (!startSlow) { startSlow = true; timeSlow = Time.time; }
				else { if (Time.time - timeSlow > 1) SpeedControl = Mathf.Clamp(SpeedControl - 0.02f, 0.1f, 1f); }
			}

			if (IsMoving || diffX > xThreshold || diffY > yThreshold)
			{
				if (LockY || diffY > yThreshold) LockY = true;
				else TargetPos.y = CamPos.y;

				IsMoving = true;

				GCam.transform.position = Vector3.SmoothDamp(
					Util.ClampPos(GCam.transform.position),
					TargetPos,
					ref velocity,
					Time.deltaTime,
					vMaxSpeed * SpeedControl
				);

				if (camtude < 1f && ++NotMoving > 50)
				{
					IsMoving     = false;
					NotMoving    = 0;
					LockY        = false;
					SpeedControl = 0.01f;
				}
			}

			if (CustomMode == CustomModes.Off && GCZoom != GCam.orthographicSize) UpdateZoom(); 

		}

		//
		// ─── TRACK ITEM ────────────────────────────────────────────────────────────────
		//
		public void TrackItem()
		{
			if ( ChaseItem == null )
			{
				if ( notMoving == 0 ) notMoving = Time.time;
				if ( Time.time - notMoving > 2f ) StopQuickChase();
				return;
			}

			if (KB.AnyKey)
			{
				StopQuickChase();
				return;
			}

			TargetPos = ChaseItem.transform.position;
			CamPos    = GCam.transform.position;

			float vDistance = Vector2.Distance(TargetPos, GCam.transform.position);

			GCam.transform.position = Vector3.SmoothDamp(
				Util.ClampPos(GCam.transform.position),
				TargetPos,
				ref velocity,
				Time.deltaTime,
				2000f
			);


			if (vDistance > 0.1f) notMoving = Time.time;
			else if (Time.time - notMoving > 2f)
			{
				StopQuickChase();
				return;
			}
			if (CustomMode == CustomModes.Off && GCZoomV != GCam.orthographicSize)
			{
				GCam.orthographicSize = Mathf.Clamp(Mathf.Lerp(GCam.orthographicSize, GCZoomV, Time.deltaTime * 3),2,16);
				yOffset = GCam.orthographicSize * yFactor;
			}
		}


		//
		// ─── TRACK VEHICLE ────────────────────────────────────────────────────────────────
		//
		public void TrackVehicle()
		{
			TargetPos = CTT.position;
			CamPos    = GCam.transform.position;

			Vector2 targetVelocity = ChaseTarget.rigidbody.velocity;

			float yVal = yOffset;

			if (yOffsetTemp > 0f) yVal = yOffsetTemp;

			if (!yShift)
			{
				if (targetVelocity.y < -7f) yShift  = true;
			}
			else {

				yVal = Mathf.Clamp(targetVelocity.y * 0.1f, -15f, 0);

				if (targetVelocity.y > -2f) yShift = false;
			}

			//  have camera look ahead the direction puppet is facing
			float vAhead   = targetVelocity.x * Time.deltaTime * GCZoomV * 1.5f;
			float absAhead = Mathf.Clamp(Mathf.Abs(vAhead), LookAheadFast, 20f);

			if (Mathf.Abs(LookAheadFast - vAhead) > 5) absAhead = LookAheadFast;
			LookAheadFast = absAhead;

			vAhead = absAhead * -Puppet.Facing;

			TargetPos.x += vAhead;
			TargetPos.y += yVal;

			GCam.transform.position = Vector3.SmoothDamp(
				Util.ClampPos(GCam.transform.position),
				TargetPos,
				ref velocity,
				Time.deltaTime,
				2000f
			);

			if (CustomMode == CustomModes.Off && GCZoomV != GCam.orthographicSize)
			{
				GCam.orthographicSize = Mathf.Clamp(Mathf.Lerp(GCam.orthographicSize, GCZoomV, Time.deltaTime * 3),2,16);
				yOffset = GCam.orthographicSize * yFactor;
			}
		}

		//
		// ─── RUN CUSTOM MODES ────────────────────────────────────────────────────────────────
		//
		public void RunCustomMode()
		{ 
			if (CustomMode == CustomModes.DistantAiming)
			{
				if (Input.mousePosition.y >= Screen.height * 0.95f ||
					Input.mousePosition.x >= Screen.width * 0.95f  ||
					Input.mousePosition.x <= 10f || Input.mousePosition.y <= 10f)
				{
					ZoomCustom -= -Time.unscaledDeltaTime * 2;
					GCam.orthographicSize = Mathf.Clamp(Mathf.Lerp(GCam.orthographicSize, ZoomCustom, Time.deltaTime * 8),2,16);
				}

				return;
			}
			else 
			if ( CustomMode == CustomModes.GroundPound )
			{
				Time.timeScale = Mathf.Lerp(Time.timeScale,2.5f,Time.deltaTime * 5);
				if ( KB.AnyKey && !KB.Down ) CustomMode = CustomModes.Off;

				ZoomCustom -= -Time.unscaledDeltaTime * 2;
				GCam.orthographicSize = Mathf.Clamp( Mathf.Lerp( GCam.orthographicSize, ZoomCustom, Time.deltaTime * 18 ), 4, 16 );

			}
			else
			if ( CustomMode == CustomModes.SonicBoom )
			{
				Time.timeScale = 0.3f;
				if ( KB.AnyKey && !KB.Down ) CustomMode = CustomModes.Off;

				ZoomCustom -= -Time.unscaledDeltaTime * 2;
				GCam.orthographicSize = Mathf.Clamp( Mathf.Lerp( GCam.orthographicSize, ZoomCustom, Time.deltaTime * 18 ), 4, 16 );

				if ( Time.time > miscFloat ) CustomMode = CustomModes.Off;
			}
		}

		public void QuickChase(PhysicalBehaviour target)
		{
			ChaseItem = target;
			ChaseMode = ChaseModes.Item;
		}

		public void StopQuickChase()
		{
			SetPuppet(Puppet, false);
		}

		public void DisableCam()
		{
			Global.main.CameraControlBehaviour.CurrentlyFollowing.Clear();
			Global.main.CameraControlBehaviour.CurrentlyFollowing.Add(ChaseTarget);
			Stop();
		}

		public void DeactivateChaseCam()
		{
			Global.main.CameraControlBehaviour.enabled = true;
			GCam = null;
		}

		public void ActivateChaseCam()
		{
			Global.main.CameraControlBehaviour.enabled = false;

			GCam        = Global.main.camera;
			GCZoom      = GCam.orthographicSize;
			CamPos      = GCam.transform.position;
		}

		public void Stop()
		{
			Global.main.CameraControlBehaviour.enabled = true;
			CustomCamActive = false;
			GCam            = null;
			Util.Destroy(this);
		}


	}

}
