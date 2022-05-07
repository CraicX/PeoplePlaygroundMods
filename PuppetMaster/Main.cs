//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|                   
//
//  Inspired from great mods & modders
//  
//  "AutoAim" by Puppyguard & ofc the Legendary "Active Humans!" by quaq + Many more
//
//  Please feel free to copy/use/modify any code created in this mod.  
//
using System;
using UnityEngine;
namespace PuppetMaster
{
	public class JTMain
	{
		public static int MaxThingsOnGround        = 20;

		public static Sprite SpeedometerSprite;
		public static AudioClip DoorClose;
		public static AudioClip CarIdle;
		public static AudioClip CarEngine;
		public static AudioClip CarUnlock1;
		public static AudioClip CarUnlock2;
		public static AudioClip CarStart;
		public static AudioClip InvGun;
		public static AudioClip InvRifle;
		public static AudioClip InvShotgun;
		public static AudioClip InvKnife;
		public static AudioClip InvSword;
		public static SpriteRenderer SpeedometerSR;

		public static bool[] Notified = {false, false, false, false};
		public static VerboseLevels Verbose   = VerboseLevels.Minimal;

		public static void OnLoad()
		{
			int savedVerbose = PlayerPrefs.GetInt("PB_Verbose", (int)VerboseLevels.Minimal);

			if (Enum.IsDefined(typeof(VerboseLevels),savedVerbose))
				Verbose = (VerboseLevels)savedVerbose;



			KB.InitControls();
		}

		public static void Main()
		{
			SpeedometerSprite      = ModAPI.LoadSprite("shtuff/speedometer.png", 1f, false);
			DoorClose              = ModAPI.LoadSound("shtuff/close-door.mp3");
			CarIdle                = ModAPI.LoadSound("shtuff/car-idle.mp3");
			CarEngine              = ModAPI.LoadSound("shtuff/car-engine.mp3");
			CarUnlock1             = ModAPI.LoadSound("shtuff/car-unlock1.mp3");
			CarUnlock2             = ModAPI.LoadSound("shtuff/car-unlock2.mp3");
			CarStart               = ModAPI.LoadSound("shtuff/car-start.mp3");
			InvGun                 = ModAPI.LoadSound("shtuff/gun-out.mp3");
			InvShotgun             = ModAPI.LoadSound("shtuff/shotgun-out.mp3");
			InvRifle               = ModAPI.LoadSound("shtuff/rifle-out.mp3");
			InvKnife               = ModAPI.LoadSound("shtuff/knife-out.mp3");
			InvSword               = ModAPI.LoadSound("shtuff/sword-out.mp3");

			ModAPI.Register<PuppetSwitch>();
		}
	}


	public class PuppetSwitch : MonoBehaviour
	{
		private static bool _announcements = false;

		void Update()
		{
			if (InputSystem.Down("PM-TogglePM")) TogglePuppetMode();
		}

		private void TogglePuppetMode()
		{
			if (KB.Modifier)
			{
				// Set verbose mode
				int verbose = ((int)JTMain.Verbose + 1) % 3;

				JTMain.Verbose = (VerboseLevels)verbose;

				Util.Notify("<color=yellow>VERBOSE LEVEL:</color> " + Enum.GetName(typeof(VerboseLevels), verbose), VerboseLevels.Off);

				PlayerPrefs.SetInt("PB_Verbose", verbose);

				return;

			}

			PuppetMaster pm = Global.main.gameObject.GetOrAddComponent<PuppetMaster>();

			if (pm.Activated)
			{
				KB.SwapBindings(false);
				pm.Activated = false;
				pm.ShutDown();

				Util.Notify("Puppet Master: <color=red>Off</color>", VerboseLevels.Minimal);

				ModAPI.OnItemSelected -= null;

			}
			else
			{
				PuppetMaster.ChaseCam?.Stop();

					ModAPI.OnItemSelected += (sender, phys) =>
					{

						if ( !PuppetMaster.Master.Activated 
						|| KB.NotPlaying 
						|| PuppetMaster.Puppet == null 
						|| !KB.Modifier  
						|| !PuppetMaster.CanHoldItem( phys )
						|| phys == Util.GetClickedItem()
						) return;

						PuppetMaster.Puppet.PG.Hold( phys.gameObject.GetOrAddComponent<Thing>(), KB.Control ? PuppetMaster.Puppet.PG.BH : PuppetMaster.Puppet.PG.FH );

						return;
					};


				KB.SwapBindings(true);
				pm.Activated = true;

				if (JTMain.Verbose == VerboseLevels.Full && !_announcements) {
					Util.Notify("<color=red>PM Notifications are set to <color=yellow>FULL<color=red> Press ALT-F7 for less notifications</color>");
					_announcements = true;
				}

				Util.Notify("Puppet Master: <color=green>On</color>", VerboseLevels.Minimal);



			}
		}
	}
}