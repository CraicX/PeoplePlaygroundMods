//      __  ___      _____                 
//     /  |/  /_  __/ / (_)___ _____ _____ 
//    / /|_/ / / / / / / / __ `/ __ `/ __ \
//   / /  / / /_/ / / / / /_/ / /_/ / / / /
//  /_/  /_/\__,_/_/_/_/\__, /\__,_/_/ /_/ 
//                     /____/
//                                 PPG Mod
//
using System;
using UnityEngine;

namespace Mulligan
{
	public class Mulligan
	{
		public static int  verboseLevel         = 1;
		public static bool holdingShift         = false;
		public static bool Shift                = false;
		public static bool Alt                  = false;
		public static bool Control              = false;
		public static bool Modifier             = false;

		public static void OnLoad()
		{
			BindKeys();
		}

		public static void Main()
		{
			ModAPI.Register<MulliganBehaviour>();
		}

		public static bool KeyCheck(string _name)
		{
			if (DialogBox.IsAnyDialogboxOpen || Global.ActiveUiBlock) return false;

			KeyCode xModifierx = KeyCode.None;

			Shift    = Alt = Control = Modifier = false;
			Shift    = (Input.GetKeyDown(KeyCode.LeftShift)   || Input.GetKey(KeyCode.RightShift));
			Control  = (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
			Alt      = (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));

			if (Input.GetKey(KeyCode.LeftAlt))  xModifierx = KeyCode.LeftAlt;
			if (Input.GetKey(KeyCode.RightAlt)) xModifierx = KeyCode.RightAlt;


			holdingShift = ( Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift) );

			if (InputSystem.Down(_name))
			{
				// Make sure no modifier keys are being pressed unless its in the schematic
				//
				InputAction action = InputSystem.Actions[_name];

				return (bool)(action.SecondaryKey != KeyCode.None || action.SecondaryKey == xModifierx);
			}

			return false;
		}

		private static void BindKeys()
		{
			string[] KeySchematic =
			{
				"HealSelected > Heal Selected Limbs             > Keypad1",
				"HealAll      > Heal All Humans                 > Keypad2",
				"Follow       > Follow Object                   > Keypad3",
				"FlipSelected > Flip Selected Items             > Keypad4",
				"GrabItemF    > Auto-equip closest item Front   > Keypad5",
				"GrabItemB    > Auto-equip closest item Back    > Keypad6",
				"FreezeItems  > Freeze Selected Items           > Keypad7",
				"CarSpeed     > Adjust Car Speed                > Keypad8",
				"ActivateHeld > Activate Items in Hands         > Keypad9",
				"Strengthen   > Strengthen Selected Peeps       > KeypadPeriod",
				"Mend         > Mend Selected Limbs             > KeypadPeriod > LeftAlt",
				"LayerBottom  > Move to bottom layer            > End",
				"LayerTop     > Move to top layer               > Home",
				"LayerUp      > Move Selected Up 1 Layer        > PageUp",
				"LayerDown    > Move Selected Down 1 Layer      > PageDown",
				"LayerFG      > Move to foreground              > PageUp   > LeftAlt",
				"LayerBG      > Move Selected to background     > PageDown > LeftAlt",
				"TglCollision > Toggle Collisions               > Backslash",
				"ShowInfo     > Show the Inspector              > F2",
				"QuickSave    > Save entire scene to memory     > Keypad0",
				"QuickReset   > Reset the scene                 > KeypadMultiply",
				"SaveScene    > Save scene to disk              > F5",
				"LoadScene    > Load scene from disk            > F10",
				"PoseMode     > Cycle Pose Modes                > F12",
				"DelItems     > Delete Items                    > Delete",
				"DelXFItems   > Delete Items Not Frozen         > Delete > LeftAlt",
				"SelectSame   > Select same items               > Mouse3",
				"PeepWalk     > Action walk                     > Keypad1 > LeftAlt",
				"PeepFlat     > Action lay still                > Keypad2 > LeftAlt",
				"PeepSit      > Action sit down                 > Keypad3 > LeftAlt",
				"PeepCower    > Action cower                    > Keypad4 > LeftAlt",
				"PeepStumble  > Action stumble around           > Keypad5 > LeftAlt",
				"PeepPain     > Action feel the pain            > Keypad6 > LeftAlt",
				"PeepFlailing > Action flail                    > Keypad7 > LeftAlt",
				"PeepSwimming > Action swim                     > Keypad8 > LeftAlt",
				"CycleVerbose > Cycle Notification Levels       > Keypad0 > LeftAlt",
			};

			string            inputKey;
			InputAction       action;

			foreach (string keyMapping in KeySchematic)
			{
				string[] KeyParts = keyMapping.Split('>');

				inputKey = "Mul-" + KeyParts[0].Trim();

				if (!InputSystem.Has(inputKey))
				{
					ModAPI.RegisterInput("[Mul] " + KeyParts[1].Trim(), inputKey, (KeyCode)Enum.Parse(typeof(KeyCode), KeyParts[2].Trim()));

					if (KeyParts.Length == 4)
					{
						InputAction actionCreate = new InputAction(
							(KeyCode)Enum.Parse(typeof(KeyCode), KeyParts[2].Trim()),
							(KeyCode)Enum.Parse(typeof(KeyCode), KeyParts[3].Trim())
						);


						InputSystem.Actions.Add(inputKey, actionCreate);
					}
				} else
				{
					action = InputSystem.Actions[inputKey];
					ModAPI.RegisterInput("[Mul] " + KeyParts[1].Trim(), inputKey, action.Key);

					InputAction actionNew  = InputSystem.Actions[inputKey];
					actionNew.SecondaryKey = action.SecondaryKey;
				}
			}
		}
	}
}



