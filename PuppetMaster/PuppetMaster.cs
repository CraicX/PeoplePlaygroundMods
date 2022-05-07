//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using UnityEngine;
using System.Collections.Generic;
using System;

namespace PuppetMaster
{
	[SkipSerialisation]
	public class PuppetMaster : MonoBehaviour
	{
		private static PuppetMaster _instance;


		public static PuppetMaster Instance { get { return _instance; } }

		public bool Activated                      = false;
		public static bool LastPauseState                 = false;
		public static bool IsGamePaused                   = false;
		public static bool CheckDisabledCollisions        = false;

		public static int ActivePuppet                    = 0;

		public static PhysicalBehaviour LastClickedItem   = null;
		public static PersonBehaviour LastClickedPerson   = null;
		public static Puppet Puppet;
		public static ChaseCamBehaviour ChaseCam          = null;


		public static bool isMaxPayne                     = false;
		public static PuppetMaster Master;

		public static float HeldMouseButton               = 0f;
		public static bool TempInputDisable               = false;



		private readonly List<Task> Tasks  = new List<Task>();
		private bool hasTasks     = false;

		private string debugID;

		public static float GrabCooldown = 0f;

		public static bool CanBePuppet(PersonBehaviour person) => (bool) (person.isActiveAndEnabled && person.IsAlive());


		private void Awake()
		{
			if (_instance != null && _instance != this)
			{
				Util.Notify("Destroying 2nd PuppetMaster", VerboseLevels.Full);
				Destroy(this.gameObject);
			}
			else
			{
				_instance   = this;
				Master      = this;
				debugID     = "<color=green>PuppetMaster " + GetHashCode().ToString().Substring(1, 3) + "</color>: ";
				Util.Notify(debugID + "New Instance", VerboseLevels.Full);




			}
		}




		//
		// ─── UNITY UPDATE ────────────────────────────────────────────────────────────────
		//
		private void Update()
		{
			if (!Activated) return;

			if (hasTasks) RunTasks();
			if (KB.NotPlaying) 
			{
				if (LastPauseState == false)
				{
					KB.ReplaceAllKeys();
					LastPauseState = true;

					KB.EnableMouse();
					KB.EnableNumberKeys(); 
				}
			}
			else if (LastPauseState == true) { 

				LastPauseState = false;
				KB.SwapBindings(true);

			}

			KB.CheckKeys();

			if (KB.PClick) {
				CheckNewClicks();
			}

			//  Allow to use original keys if held button on item for 2 seconds
			if (KB.MouseDown && !KB.isMouseDisabled && !TempInputDisable && Time.time - KB.KeyTimes.Mouse1 > 1) {

				KB.SwapBindings(false);
				TempInputDisable = true;
			}

			if (CheckDisabledCollisions && Time.frameCount % 10 == 0) Util.FixDisabledCollisions();

			if (Puppet != null && (bool)Puppet?.IsActive) {

				if (TempInputDisable) {
					if (!KB.MouseDown)
					{
						TempInputDisable = false;
						KB.SwapBindings(true);
					}


				} else { 
					Puppet.CheckControls();
				}
			}
			else if (Time.frameCount % 100 == 0)
			{
				//if (Puppet?.PBO == null)
				//{
				//    if (Puppet?.HandThing != null)
				//    {
				//        Util.Notify("Fixed Held Thing: " + Puppet.HandThing.P.name, VerboseLevels.Full);
				//        Puppet.HandThing.P.beingHeldByGripper = false;
				//    }
				//}
			}
		}

		void FixedUpdate()
		{
			if (FlipUtility.FlipState != FlipStates.ready) FlipUtility.RunFlip();
		}


		private void RunTasks()
		{
			hasTasks = false;

			for (int i = Tasks.Count; --i >= 0;)
			{
				hasTasks = true;
				if (Tasks[i].Run()) Tasks.RemoveAt(i);
			}
		}



		public static void StartChaseCam()
		{
			Util.cam = Global.main.CameraControlBehaviour;

			ChaseCam = Global.main.gameObject.GetOrAddComponent<ChaseCamBehaviour>();

			Master.AddTask(Master.ActivateChaseCam, 0.5f);
		}

		public void ActivateChaseCam()
		{
			ChaseCam.enabled = true;
		}


		//
		// ─── CHECK NEW CLICKS ────────────────────────────────────────────────────────────────
		//
		public static void CheckNewClicks()
		{
			if ( Puppet != null && Puppet.IsActive && Puppet.PG.IsAiming ) return;

			PersonBehaviour LCP = Util.GetClickedPerson();
			if (LCP != null)
			{

				if (LCP == LastClickedPerson)
				{
					//  This person was clicked twice, so make them the puppet
					if (CanBePuppet(LCP)) ActivatePuppet(LCP);

					LCP = null;

					return;
				}

				LastClickedPerson = LCP;
			}
			else
			{
				PhysicalBehaviour LCI = Util.GetClickedItem();

				if (LCI != null && (bool)Puppet?.IsActive && !Puppet.SpecialMode)
				{
					if (LCI == LastClickedItem && CanHoldItem(LCI))
					{
						//  Item was clicked twice, so pick it up automatically
						Puppet.PG.Hold(LCI.gameObject.GetOrAddComponent<Thing>(),KB.Control ? Puppet.PG.BH : Puppet.PG.FH);

						return;
					}

					//  Validate clicked item
					if (CanHoldItem(LCI)) LastClickedItem = LCI;

					else if (Garage.CanPuppetDrive(LCI)) return;
				}
			}
		}


		public void AddTask(Action<bool> action, float time, bool option)
		{
			Task task = new Task();
			task.AddTask(action, time, option);
			Tasks.Add(task);
			hasTasks = true;
		}

		public void AddTask(Action action, float time)
		{
			Task task = new Task();
			task.AddTask(action, time);
			Tasks.Add(task);
			hasTasks = true;
		}


		//
		// ─── ACTIVATE PUPPET ────────────────────────────────────────────────────────────────
		//
		public static void ActivatePuppet(PersonBehaviour person)
		{
			List<string>ForcedRemoves = new List<string>()
			{
				"ACTIVEHUMAN",
				"EVALTHREAT",
				"MCDOWELLS.MCNPC",
			};

			int puppetId = person.GetHashCode();

			Puppet _puppet;

			if ( !person.TryGetComponent<Puppet>( out _puppet ) )
			{
				_puppet = person.gameObject.AddComponent<Puppet>() as Puppet;
				_puppet.Init(person);
				Puppet = _puppet;
			}
			else { 
				_puppet.enabled = true;
				_puppet.Reset();
				Puppet = _puppet;
			}

			//  Disable any other puppet
			//Puppet[] puppets = Global.main.gameObject.GetComponentsInChildren<Puppet>();
			Puppet[] puppets = Resources.FindObjectsOfTypeAll<Puppet>();

			foreach ( Puppet xpuppet in puppets )
			{
				if (xpuppet != Puppet && xpuppet.enabled) {
					Util.Notify("Deactivating Puppet: " + Puppet.PBO.GetHashCode(), VerboseLevels.Full);
					xpuppet.IsActive = false;
					xpuppet.enabled  = false;
				}
			}

			if (ChaseCam == null) ChaseCam = Global.main.gameObject.GetOrAddComponent<ChaseCamBehaviour>();

			ChaseCam.SetPuppet(Puppet, true);

			//  Disable ActiveHumans for this puppet
			MonoBehaviour[] components = Puppet.PBO.GetComponents<MonoBehaviour>();

			if (components.Length > 0)
			{
				for (int i = components.Length; --i >= 0;)
				{
					if (ForcedRemoves.Contains(components[i].GetType().ToString().ToUpper()))
					{
						UnityEngine.Object.DestroyImmediate((UnityEngine.Object)components[i]);
					}
				}
			}

			Puppet.IsActive        = true;
			ActivePuppet           = puppetId;

		}

		public void ShutDown()
		{
			ChaseCam?.Stop();
		}


		//
		// ─── CAN HOLD ITEM ────────────────────────────────────────────────────────────────
		//
		public static bool CanHoldItem(PhysicalBehaviour item)
		{
			if (!item.Selectable || item.HoldingPositions.Length == 0 || item.penetrations.Count > 0 || Time.time < GrabCooldown) {
				return false;
			}

			if (item.TryGetComponent<FreezeBehaviour>(out _)) return false;

			return true;
		}

		public class Task
		{
			public float Xtime;
			public object TaskAction;
			public bool hasParam = false;
			public bool boolParam;

			public void AddTask(Action action, float time)
			{
				Xtime      = Time.time + time;
				TaskAction = action;
			}

			public void AddTask(Action<bool> action, float time, bool param) 
			{
				hasParam        = true;
				Xtime           = Time.time + time;
				TaskAction      = action;
				boolParam       = param;
			}

			public bool Run()
			{
				if (Time.time >= Xtime) {
					if (hasParam) { 
						Action<bool> action = (Action<bool>)TaskAction;
						action(boolParam);
					} else {
						Action action = (Action)TaskAction;
						action();
					}
					return true;
				}

				return false;
			}
		}
	}
}