//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetMaster
{
	public static class KB
	{
		public const float DoubleTap        = 0.5f;
		public static bool UsingCustom      = false;
		public static bool PClick           = false;
		public static bool Up               = false;
		public static bool Down             = false;
		public static bool Left             = false;
		public static bool Right            = false;
		public static bool Activate         = false;
		public static bool ActivateHeld     = false;
		public static bool Action           = false;
		public static bool Action2          = false;
		public static bool ActionHeld       = false;
		public static bool Action2Held      = false;
		public static bool Throw            = false;
		public static bool Aim              = false;
		public static bool AimDown          = false;
		public static bool Shift            = false;
		public static bool Control          = false;
		public static bool Alt              = false;
		public static bool Modifier         = false;
		public static bool MouseDown        = false;
		public static bool Mouse2Down       = false;
		public static bool MouseUp          = false;
		public static bool Mouse2Up         = false;
		public static bool AnyKey           = false;
		public static bool Emote            = false;
		public static bool Inventory        = false;
		public static bool isNumDisabled    = false;
		public static bool isMouseDisabled  = false;


		private static bool startClickTime  = false;

		public static bool NotPlaying => (bool)(DialogBox.IsAnyDialogboxOpen || 
												Global.ActiveUiBlock || 
												Global.main.Paused || 
												TriggerEditorBehaviour.IsBeingEdited ||
												Global.main.GetPausedMenu());

		public static class KeyTimes
		{
			public static float Left        = 0f;
			public static float Right       = 0f;
			public static float Up          = 0f; 
			public static float Down        = 0f;
			public static float Mouse1      = 0f;
		}

		public static class KeyCombos
		{
			public static bool DoubleRight  = false;
			public static bool DoubleLeft   = false;
			public static bool DoubleUp     = false;
			public static bool DoubleDown   = false;
		}

		public static Dictionary<string, InputAction> DefaultBindings   = new Dictionary<string, InputAction>();
		public static Dictionary<string, InputAction> PuppetBindings    = new Dictionary<string, InputAction>();
		public static Dictionary<string, KeyCode> DisabledKeys          = new Dictionary<string, KeyCode>();
		public static Dictionary<string, KeyCode> DisabledMouse         = new Dictionary<string, KeyCode>();
		public static List<KeyCode> PuppetKeys                          = new List<KeyCode>();



		//
		// ─── CHECK KEYS ────────────────────────────────────────────────────────────────
		//
		public static void CheckKeys()
		{
			PClick = Left = Right = Up = Down = Activate = Action = Action2 = ActionHeld = Action2Held = Aim = AimDown =
			Shift = Control = Alt = Modifier = MouseDown = Mouse2Down = MouseUp = Mouse2Up = Emote = Inventory = PClick = false;


			if (NotPlaying) return;
			if (Input.GetKey(KeyCode.None)) return;

			Shift    = (Input.GetKey(KeyCode.LeftShift)   || Input.GetKey(KeyCode.RightShift));
			Alt      = (Input.GetKey(KeyCode.LeftAlt)     || Input.GetKey(KeyCode.RightAlt));
			Control  = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
			Modifier = Shift || Alt || Control;

			MouseDown       = Input.GetMouseButton(0);
			Mouse2Down      = Input.GetMouseButton(1);
			MouseUp         = Input.GetMouseButtonUp(0);
			Mouse2Up        = Input.GetMouseButtonUp(1);
			PClick          = Modifier && MouseUp;
			Up              = InputSystem.Held("PM-MoveUp");
			Down            = InputSystem.Held("PM-MoveDown");
			Left            = InputSystem.Held("PM-MoveLeft");
			Right           = InputSystem.Held("PM-MoveRight");
			Throw           = InputSystem.Held("PM-Throw");
			Emote           = InputSystem.Held("PM-Emote");
			ActionHeld      = InputSystem.Held("PM-Action");
			Action2Held     = InputSystem.Held("PM-Action2");
			Inventory       = InputSystem.Held("PM-Inventory");
			ActivateHeld    = InputSystem.Held("PM-Activate");
			AimDown         = InputSystem.Held("PM-Aim");
			Aim             = InputSystem.Down("PM-Aim");
			Activate        = InputSystem.Down("PM-Activate");
			Action          = InputSystem.Down("PM-Action");
			Action2         = InputSystem.Down("PM-Action2");

			AnyKey = Up || Down || Left || Right || Activate || Action || Action2 || ActionHeld || Action2Held || Throw || Aim || 
			Emote || MouseDown || Mouse2Down || MouseUp || Mouse2Up || Inventory || PClick;

			if (MouseDown && !startClickTime) { startClickTime = true; KeyTimes.Mouse1 = Time.time; }
			if (MouseUp) startClickTime = false;

			if (InputSystem.Down("PM-MoveUp") )
			{
				KeyCombos.DoubleUp     = (Time.time - KeyTimes.Up < DoubleTap);
				KeyTimes.Up            = Time.time;
			}

			if (InputSystem.Down("PM-MoveDown"))
			{
				KeyCombos.DoubleDown   = (Time.time - KeyTimes.Down < DoubleTap);
				KeyTimes.Down          = Time.time;
			}

			if (InputSystem.Down("PM-MoveLeft"))
			{
				KeyCombos.DoubleLeft   = (Time.time - KeyTimes.Left < DoubleTap);
				KeyTimes.Left          = Time.time;
			}

			if (InputSystem.Down("PM-MoveRight"))
			{
				KeyCombos.DoubleRight  = (Time.time - KeyTimes.Right < DoubleTap);
				KeyTimes.Right         = Time.time;
			}
		}


		//
		// ─── INIT CONTROLS ────────────────────────────────────────────────────────────────
		//
		public static void InitControls()
		{
			string[] ControlSchematic =
			{
				"MoveLeft    > Move Puppet Left        > A",
				"MoveRight   > Move Puppet Right       > D",
				"MoveUp      > Move Up / Jump          > W",
				"MoveDown    > Move Down / Duck        > S",
				"Activate    > Activate Item           > L",
				"Action      > Fire / Attack           > K",
				"Action2     > Quick Attack            > J",
				"Throw       > Throw Item              > T",
				"Aim         > Aim Weapon              > R",
				"Inventory   > Inventory               > BackQuote",
				"Emote       > Emote (Followed by #)   > Semicolon",
				"TogglePM    > Toggle Puppet Master    > F7",
			};

			string inputKey;
			InputAction action;

			foreach (string controlMapping in ControlSchematic)
			{
				string[] KeyParts = controlMapping.Split('>');

				inputKey = "PM-" + KeyParts[0].Trim();

				if (!InputSystem.Has(inputKey))
				{
					ModAPI.RegisterInput("[PM] " + KeyParts[1].Trim(), inputKey, (KeyCode)Enum.Parse(typeof(KeyCode), KeyParts[2].Trim()));
				}
				else
				{
					action = InputSystem.Actions[inputKey];
					ModAPI.RegisterInput("[PM] " + KeyParts[1].Trim(), inputKey, action.Key);

					InputAction actionNew  = InputSystem.Actions[inputKey];
					actionNew.SecondaryKey = action.SecondaryKey;
				}
			}

			//  Save the Default Bindings
			foreach (KeyValuePair<string, InputAction> xaction in InputSystem.Actions)
			{
				if (xaction.Key.Contains("PM") && !xaction.Key.Contains("TogglePM"))
				{
					PuppetBindings.Add(xaction.Key, xaction.Value);
					PuppetKeys.Add(xaction.Value.Key);
				}

				else DefaultBindings.Add(xaction.Key, xaction.Value);
			}
		}


		//
		// ─── SWAP BINDINGS ────────────────────────────────────────────────────────────────
		//
		public static void SwapBindings(bool useCustom = false)
		{
			Util.Notify("<color=red>Swapping Bindings to:</color> " + (useCustom ? "CUSTOM" : "DEFAULT"), VerboseLevels.Full); 

			UsingCustom = useCustom;

			foreach (KeyValuePair<string, InputAction> keyset in PuppetBindings)
			{
				if (useCustom) 
				{ 
					if (!InputSystem.Has(keyset.Key)) 
					{ 
						InputSystem.Actions.Add((string)keyset.Key, (InputAction)keyset.Value);
					}
				}
				else 
				{
					if (!InputSystem.Has(keyset.Key)) 
					{ 
						InputSystem.Actions.Remove(keyset.Key);
					}
				}
			}

			foreach (KeyValuePair<string, InputAction> keyset in DefaultBindings)
			{
				if (useCustom)
				{
					if (PuppetKeys.Contains(keyset.Value.Key))
					{
						if (InputSystem.Has(keyset.Key)) InputSystem.Actions[keyset.Key].Key = KeyCode.Clear;
					}
				}

				else
				{
					if (!InputSystem.Has(keyset.Key)) InputSystem.Actions.Add((string)keyset.Key, (InputAction)keyset.Value);

					else InputSystem.Actions[keyset.Key].Key = keyset.Value.Key;
				}
			}
		}


		//
		// ─── DISABLE NUMBER KEYS ────────────────────────────────────────────────────────────────
		//
		public static void DisableNumberKeys()
		{
			if (isNumDisabled) return;

			isNumDisabled = true;

			DisabledKeys.Clear();

			foreach (KeyValuePair<string, InputAction> actionKey in InputSystem.Actions)
			{
				if ( (int)actionKey.Value.Key >= 48 && (int)actionKey.Value.Key <= 57 )
				{
					DisabledKeys.Add(actionKey.Key, actionKey.Value.Key);
					InputSystem.Actions[actionKey.Key].Key = KeyCode.Clear;
				}
			}
		}


		//
		// ───  DISABLE MOUSE ────────────────────────────────────────────────────────────────
		//
		public static void DisableMouse()
		{
			if (isMouseDisabled) return;

			isMouseDisabled = true;

			DisabledMouse.Clear();

			foreach (KeyValuePair<string, InputAction> actionKey in InputSystem.Actions)
			{
				if (actionKey.Value.Key == KeyCode.Mouse0 || actionKey.Value.Key == KeyCode.Mouse1 )
				{
					DisabledMouse.Add(actionKey.Key, actionKey.Value.Key);
					InputSystem.Actions[actionKey.Key].Key = KeyCode.Clear;
				}
			}
		}


		//
		// ─── ENABLE MOUSE ────────────────────────────────────────────────────────────────
		//
		public static void EnableMouse()
		{
			if (!isMouseDisabled) return;

			isMouseDisabled = false;

			foreach (KeyValuePair<string, KeyCode> keyset in DisabledMouse)
			{
				InputSystem.Actions[keyset.Key].Key = keyset.Value;
			}
		}





		//
		// ─── ENABLE NUMBER KEYS ────────────────────────────────────────────────────────────────
		//
		public static void EnableNumberKeys()
		{
			if (!isNumDisabled) return;

			isNumDisabled = false;

			foreach (KeyValuePair<string, KeyCode> keyset in DisabledKeys)
			{
				InputSystem.Actions[keyset.Key].Key = keyset.Value;
			}
		}


		//
		// ─── CHECK NUMBER KEY ────────────────────────────────────────────────────────────────
		//
		public static int CheckNumberKey()
		{
			for (int i = 48; i <= 57; i++)
			{
				if (Input.GetKeyDown((KeyCode)i)) return i - 48;
			}

			return -1;
		}


		//
		// ─── REPLACE ALL KEYS ────────────────────────────────────────────────────────────────
		//
		public static void ReplaceAllKeys()
		{

			EnableMouse();
			EnableNumberKeys();

			foreach (KeyValuePair<string, InputAction> keyset in DefaultBindings)
			{
					if (PuppetKeys.Contains(keyset.Value.Key))
					{
					if (!InputSystem.Has(keyset.Key))
					{
						InputSystem.Actions.Add((string)keyset.Key, (InputAction)keyset.Value);
					}
					else
					{
						InputSystem.Actions[keyset.Key].Key = keyset.Value.Key;
					}
				}
			}
		}
	}
}
