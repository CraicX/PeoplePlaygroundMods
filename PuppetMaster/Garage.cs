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
	public static class Garage
	{
		public static Puppet Puppet => PuppetMaster.Puppet;
		public static PuppetBike Bike;
		public static PuppetCar Car;
		public static PuppetHovercar Hovercar;

		public static bool CanPuppetDrive(PhysicalBehaviour PB)
		{
			string ItemClicked = PB.transform.root.name;

			//  Check for clicked Vehicles
			if (ItemClicked == "Car")
			{
				//  Car was clicked
				if (Puppet != null && Puppet.IsActive)
				{
					PuppetCar clickedCar = PB.transform.root.gameObject.GetOrAddComponent<PuppetCar>();

					if (Car != null && Car != clickedCar)
					{
						Car.enabled = false;
						Util.DestroyNow(Car);
					}

					Car        = clickedCar;
					if (Car.Puppet != null && Car.Puppet != Puppet) Car.Registered = false;
					Car.Puppet = Puppet;

					if (Car.RegisterCar()) Util.Notify("You have a new <color=yellow>car</color>");

					return true;
				}
			}


			if (ItemClicked == "Bicycle")
			{
				//  Bicycle was clicked
				if (Puppet != null && Puppet.IsActive)
				{
					//PuppetBike clickedBike = PB.transform.root.gameObject.GetOrAddComponent<PuppetBike>();
					PuppetBike clickedBike = PB.transform.root.Find("Frame")?.gameObject.GetOrAddComponent<PuppetBike>();

					if (Bike != null && Bike != clickedBike)
					{
						Bike.enabled = false;
						Util.DestroyNow(Bike);
					}

					Bike        = clickedBike;
					Bike.Puppet = Puppet;

					if (Bike.RegisterBike()) Util.Notify("You have a new <color=yellow>bike</color>");

					return true;
				}
			}


			if (ItemClicked == "Hovercar")
			{
				//  Bicycle was clicked
				if (Puppet != null && Puppet.IsActive)
				{
					//PuppetBike clickedBike = PB.transform.root.gameObject.GetOrAddComponent<PuppetBike>();
					PuppetHovercar clickedHovercar = PB.transform.root.gameObject.GetOrAddComponent<PuppetHovercar>();

					if (Hovercar != null && Hovercar != clickedHovercar)
					{
						Hovercar.enabled = false;
						Util.DestroyNow(Hovercar);
					}

					Hovercar = clickedHovercar;
					if (Hovercar.Puppet != null && Hovercar.Puppet != Puppet) Hovercar.Registered = false;
					Hovercar.Puppet = Puppet;

					if (Hovercar.RegisterHovercar()) Util.Notify("You have a new <color=yellow>hover-car</color>");

					return true;
				}
			}




			return false;
		}

		public static void NoCollisionsWithHeldItem(Thing thing)
		{
			if (Bike      != null) Bike.DisableHeldItemCollisions(thing);
			if (Car       != null) Car.DisableHeldItemCollisions(thing); 
			if (Hovercar  != null) Hovercar.DisableHeldItemCollisions(thing); 
		}
	}

}
