//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace PuppetMaster
{
	public class Hero
	{
		public Puppet Puppet;
		public HeroWeapon[] HeroWeapons = new HeroWeapon[2];

		public static string HeroId => "PM_" + PuppetMaster.Puppet.PBO.name.GetHashCode();
		public static bool HeroCheck() => PlayerPrefs.HasKey(HeroId);

		public static Dictionary<string, string> LimbNameMap =>
			new Dictionary<string, string>()
			{
				{"UpperArmFront", "Front Upper Arm"},
				{"UpperArm",      "Back Upper Arm"},
				{"LowerArm",      "Back Lower Arm"},
				{"LowerArmFront", "Front Lower Arm"},
				{"LowerLegFront", "Front Lower Leg"},
				{"LowerLeg",      "Lower Leg"},
				{"UpperLeg",      "Upper Leg"},
				{"UpperLegFront", "Front Upper Leg"},
				{"FootFront",     "Front Foot"},
				{"Foot",          "Back Foot"},
				{"UpperBody",     "Upper Body"},
				{"MiddleBody",    "Middle Body"},
				{"LowerBody",     "Lower Body"},
			};

		public Hero()
		{
			HeroWeapons[0] = new HeroWeapon()
			{
				IsFiring = false,
				LimbName = "",
				num      = 0,
			};

			HeroWeapons[1] = new HeroWeapon()
			{
				IsFiring = false,
				LimbName = "",
				num = 0,
			};


		}


		public static void SetupLimbButton(LimbBehaviour limb)
		{
			string ld = limb.name;
			if (LimbNameMap.ContainsKey(limb.name)) ld = LimbNameMap[limb.name];

			limb.PhysicalBehaviour.ContextMenuOptions.Buttons.Add(
					new ContextMenuButton("weaponize1", "Weaponize Attack #1", "Your <color=orange>" + ld + "</color> is weapon #1", new UnityAction[1] {
					(UnityAction) (() => PuppetMaster.Puppet.SetHeroAttack(0, limb.name) )}));

			limb.PhysicalBehaviour.ContextMenuOptions.Buttons.Add(
					new ContextMenuButton("weaponize2", "Weaponize Attack #2", "Your <color=orange>" + ld + "</color> is weapon #2", new UnityAction[1] {
					(UnityAction) (() => PuppetMaster.Puppet.SetHeroAttack(1, limb.name) )}));
		}

		public void HeroSetup(int num, string limbName, bool announce = true)
		{
			HeroWeapons[num] = new HeroWeapon()
			{
				IsFiring = false,
				LimbName = limbName,
				num      = num,
			};

			if (announce)
			{
				string HeroSetting = "";

				for (int i = 0; i < HeroWeapons.Length; i++)
				{
					if (HeroWeapons[i].LimbName == "") continue;

					HeroSetting += i + ":" + HeroWeapons[i].LimbName + ";";
				}

				if (HeroSetting != "")
				{
					PlayerPrefs.SetString(HeroId, HeroSetting);
				}



				string ld = limbName;

				if (LimbNameMap.ContainsKey(limbName)) ld = LimbNameMap[limbName];

				Util.Notify("Your <color=orange>" + ld + "</color> set to <color=yellow>Attack #" + num + "</color>", VerboseLevels.Minimal);
			}
		}

		public void LoadHero()
		{
			Puppet = PuppetMaster.Puppet;

			if (!PlayerPrefs.HasKey(HeroId)) return;

			string HeroValue = PlayerPrefs.GetString(HeroId);

			string[] HeroLimbs = HeroValue.Split(';');

			foreach (string HeroLimb in HeroLimbs)
			{
				string[] pieces = HeroLimb.Split(':');
				if (pieces.Length == 2)
				{
					int.TryParse(pieces[0], out int limbNum);
					HeroSetup(limbNum, pieces[1], false);
				}
			}
		}

		public void HeroAimStart()
		{
			KB.DisableMouse();

			string heroAiming = "Head,LowerArm,LowerArmFront,UpperBody";

			Puppet.CanHeroAim = new string[] { HeroWeapons[0].LimbName, HeroWeapons[1].LimbName }.Any(heroAiming.Contains);

		}

		public void HeroAimStop()
		{
			Puppet.HeroMode =
			Puppet.LB["LowerArmFront"].Broken =
			Puppet.LB["UpperArmFront"].Broken =
			Puppet.LB["LowerArm"].Broken =
			Puppet.LB["UpperArm"].Broken = false;

			Puppet.CheckMouseClickStatus();

			Puppet.RunRigids(Puppet.RigidReset);
		}


		public struct HeroWeapon
		{
			public string LimbName;
			public bool IsFiring;
			public int num;
			public bool CanAim => "Head,LowerArm,LowerArmFront,UpperBody".Contains(LimbName);
		}

		public void HeroAimRun()
		{
			bool[] AimHero    = new bool[] { false, false };
			string[] UpperArm = { "", "" };

			if (Puppet == null) Puppet = PuppetMaster.Puppet;

			Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			for (int i = 0; i <= 1; i++)
			{
				if (HeroWeapons[i].LimbName.Contains("LowerArm"))
				{

					if ((i == 0 && (KB.ActionHeld || KB.MouseDown)) ||
						(i == 1 && (KB.Action2Held || KB.Mouse2Down)))
					{
						HeroWeapons[i].IsFiring = true;
						AimHero[i] = true;

						UpperArm[i] = HeroWeapons[i].LimbName.Replace("Lower", "Upper");

						Puppet.LB[HeroWeapons[i].LimbName].Broken   = Puppet.LB[UpperArm[i]].Broken   = true;
						Puppet.RB2[HeroWeapons[i].LimbName].inertia = Puppet.RB2[UpperArm[i]].inertia = 0.00125f;

						for (int j = 0; j <= 1; j++)
						{
							string limbName = j == 0 ? HeroWeapons[i].LimbName : UpperArm[i];
							Vector3 diffy   = ((Vector3)Puppet.RB2[limbName].position - Camera.main.ScreenToWorldPoint(Input.mousePosition));

							Vector3 angleVelocity;

							if (Puppet.FacingLeft) angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diffy.y, diffy.x) * Mathf.Rad2Deg) - (85.5f + (16.5f * Puppet.Facing)));
							else angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diffy.y, diffy.x) * Mathf.Rad2Deg) - (85.5f + (10.5f * Puppet.Facing)));

							Puppet.RB2[limbName].MoveRotation(Quaternion.Euler(angleVelocity));
						}

					}
					else
					if ((i == 0 && (KB.MouseUp  || HeroWeapons[i].IsFiring)) ||
						(i == 1 && (KB.Mouse2Up || HeroWeapons[i].IsFiring)))
					{

						HeroWeapons[i].IsFiring = false;

						if (HeroWeapons[i].LimbName != "") { 

							if (HeroWeapons[i].LimbName.Contains("LowerArm"))
								Puppet.LB[HeroWeapons[i].LimbName].Broken = Puppet.LB[HeroWeapons[i].LimbName.Replace("Lower", "Upper")].Broken = false;

							Puppet.LB[HeroWeapons[i].LimbName].gameObject.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
						}

					}

				}
				else if (HeroWeapons[i].LimbName == "Head")
				{
					if ((i == 0 && (KB.ActionHeld  || KB.MouseDown)) ||
						(i == 1 && (KB.Action2Held || KB.Mouse2Down)))
					{
						float angle = mouse.y - Puppet.RB2["Head"].position.y;

						Puppet.RB2["Head"].AddTorque(Mathf.Clamp(angle * -Puppet.Facing, -5, 5) * Puppet.TotalWeight);

						HeroWeapons[i].IsFiring = true;
						break;
					}
					else
					if ((i == 0 && (KB.MouseUp  || HeroWeapons[i].IsFiring)) ||
						(i == 1 && (KB.Mouse2Up || HeroWeapons[i].IsFiring)))
					{
						HeroWeapons[i].IsFiring = false;
						Puppet.LB[HeroWeapons[i].LimbName].gameObject.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
					}
				}
				else
				{
					if ((i == 0 && (KB.ActionHeld  || KB.MouseDown)) ||
						(i == 1 && (KB.Action2Held || KB.Mouse2Down)))
					{
						if (HeroWeapons[i].LimbName  != "") 
						{
							float angle = mouse.y - Puppet.RB2[HeroWeapons[i].LimbName].position.y;

							Puppet.RB2[HeroWeapons[i].LimbName].AddTorque(Mathf.Clamp(angle * -Puppet.Facing, -5, 5) * Puppet.TotalWeight);

							HeroWeapons[i].IsFiring = true;
						}
						break;
					}
					else
					if ((i == 0 && (KB.MouseUp  || HeroWeapons[i].IsFiring)) ||
						(i == 1 && (KB.Mouse2Up || HeroWeapons[i].IsFiring)))
					{
						HeroWeapons[i].IsFiring = false;
						if (HeroWeapons[i].LimbName != "") {
							Puppet.LB[HeroWeapons[i].LimbName].gameObject.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
						}
					}
				}
			}
		}
	}
}
