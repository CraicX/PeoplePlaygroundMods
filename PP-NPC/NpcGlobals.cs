//                             ___           ___         ___     
//  Feel free to use and      /\  \         /\  \       /\__\    
//  modify any of this code   \:\  \       /::\  \     /:/  /    
//                             \:\  \     /:/\:\__\   /:/  /     
//   ________  ________    _____\:\  \   /:/ /:/  /  /:/  /  ___ 
//  |\   __  \|\   __  \  /::::::::\__\ /:/_/:/  /  /:/__/  /\__\
//  \ \  \|\  \ \  \|\  \ \:\~~\~~\/__/ \:\/:/  /   \:\  \ /:/  /
//   \ \   ____\ \   ____\ \:\  \        \::/__/     \:\  /:/  / 
//    \ \  \___|\ \  \___|  \:\  \        \:\  \      \:\/:/  /  
//     \ \__\    \ \__\      \:\__\        \:\__\      \::/  /   
//      \|__|     \|__|       \/__/         \/__/       \/__/     
//
//
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI;

namespace PPnpc
{
	public struct NpcGoals
	{
		public NpcBehaviour NPC;
		public bool Scavenge;
		public bool Attack;
		public bool Shoot;
		public bool Recruit;
		public bool Upgrade;

		public NpcGoals( NpcBehaviour npc )
		{
			NPC      = npc;
			Scavenge = true;
			Attack   = false;
			Shoot	 = false;
			Upgrade  = false;
			Recruit  = false;
		}

	}

	

	public enum EventIds
	{
		Death,
		Birth,
		Killed,
		Attacked,
		TalkSmack,
		Gun,
		Melee,
		Explosive,
		Gathering,
		Battle,
		Fire,
		Medic,
		Jukebox,
		TV,
	}
	public enum IconTypes
	{
		Misc,
		Enhancement,
		Upgrade,
	}

	public struct LimbHit
    {
		public NpcBehaviour Attacker;
		public string LimbName;
		public float Force;
		public float HealthStart;
		public float HealthFinish;
		public float TimeImpact;
    };

    public enum AimStyles
	{
		Standard,
		Crouched,
		Proned,
		Spray,
		Rockets,
		PhysicsGun,
	};

	public enum Chips
	{
		AI,
		Memory,
		Karate,
		Firearms,
		Melee,
		Engineer,
		Troll,
		Hero,
	}

	public enum Gadgets
	{
		AIChip,
		Rebirther,
		Expansion,
		Memory,
		Karate,
		Firearms,
		Melee,
		
	}

	public enum AIMethods
	{
		AIChip,
		Syringe,
		Spawn,
	}

	public enum PM3
	{
		Success,
		Fail,
		Error,
		Timeout,
		Busy,
	}

	public enum NpcNotifyLevels
	{
		Off,
		Minimal,
		Normal,
		Debug,
	}

	public struct WeaponBasics
	{
		public bool CanShoot;
		public bool IsAutomatic;
		public bool CanStab;
		public bool CanExplode;

		public WeaponBasics( bool _canShoot, bool _isAutomatic, bool _canStab, bool _canExplode )
		{
			CanExplode  = _canExplode;
			CanShoot    = _canShoot;
			CanStab     = _canStab;
			IsAutomatic = _isAutomatic;
		}
	}

	public struct Opinion
	{
		public float FriendScore;
		public float EnemyScore;
	}



	public struct EventLog
	{
		public EventIds EventId;
		public int NpcId;
		public string NpcType;
		public float Timestamp;
		public float Importance;
	}



	public struct LimbPoseSnapshot
	{
		public Dictionary<string, RagdollPose.LimbPose> DLimbs;
	}

	public struct ActivityMessages
	{
		public NpcBehaviour NPC;
		public string msg;
		public float time;

		public ActivityMessages( NpcBehaviour _npc )
		{
			NPC  = _npc;
			msg  = "";
			time = 0f;
		}

		public void Set( string message, float seconds )
		{
			msg  = message;
			time = Time.time + seconds;
			if (NPC.HoverStats) NPC.HoverStats.ShowText();
		}
	}

	public struct RigidSnapshot
	{
		public float inertia;
		public float mass;
		public float drag;
		public float angularDrag;

		public RigidSnapshot( Rigidbody2D rbIn )
		{
			inertia     = rbIn.inertia;
			mass        = rbIn.mass;
			drag        = rbIn.drag;
			angularDrag = rbIn.angularDrag;
		}

		public void Reset( Rigidbody2D rbIn )
		{
			rbIn.inertia     = inertia;
			rbIn.mass        = mass;
			rbIn.drag        = drag;
			rbIn.angularDrag = angularDrag;
		}
	}

	public struct MinMax
	{
		public float Min;
		public float Max;

		public MinMax( float min, float max )
		{
			Min = min;
			Max = max;
		}
	}

	public struct EventInfo
	{
		public EventIds EventId;
		public NpcBehaviour Sender;
		public List<Vector2> Locations;
		public List<NpcBehaviour> NPCs;
		public List<PhysicalBehaviour> PBs;
		public float Expires;
		public bool Response;
	}



	public class NpcConfig
	{
		public NpcNotifyLevels NotifyLevel     = NpcNotifyLevels.Debug;
		public bool AutoStart                  = true;
		public bool ShowStatsOnClick           = true;
		public bool PermaDeath		           = true;
		public float ChanceForLimbCrushOnDeath = 100;
		public float DecomposeRate			   = 100;
		public MinMax SecondsBeforeCrush       = new MinMax(11,60);
		public string Name                     = "?";
		public string NpcType                  = "Default";
		public float BaseThreatLevel           = 5f;
		public float AimingSkill               = 0.9f;
		public MinMax KarateVocalsRange        = new MinMax(50,99);
		public float KarateVocalsChance        = 50;
		private static Dictionary<string,int> Names = new Dictionary<string,int>();
		public string[] FriendlyTypes;
		public string[] HostileTypes;

		public void SetName( string _name )
		{
			if (Names.ContainsKey(_name)) {
				Names[_name] += 1;
				Name = _name + " #" + Names[_name];
			}
			else {
				Names[_name] = 1;
				Name = _name;
			}
		}
	}

	public static class NpcGlobal
	{
		public static int MaxTeamId      = 4;
		public static float SpawnFrame   = 0f;
		public static int LastClicked    = 0;
		public static float LastScanTime = 0;

		public static List<NpcBehaviour> AllNPC        = new List<NpcBehaviour>();
		public static List<PersonBehaviour> AllPersons = new List<PersonBehaviour>();
		public static List<PhysicalBehaviour> AllItems = new List<PhysicalBehaviour>();
		public static List<PhysicalBehaviour> NewItems = new List<PhysicalBehaviour>();

		public static bool[] Rebirthers = new bool[]{false,false,false,false,false};

		public static string[] ItemsNoCollide;

		public static string[] ToyNames =
		{
			"Jukebox", "Radio", "Small Button", "Television", "Tesla Coil", "Thruster", "Thrusterbed", "Accumulator", "Liquidentifier",
			"Siren", "Blood Tank", "Button", "Decimator", "Electromagnet", "Defibrillator", "Fan", "Atom Bomb", "EMP Generator", "Spot Light",
			"Propeller", "Activation Toggle", "Industrial Generator", "Industrial Gyrostabiliser", "Lagbox", "Life Detector", "Lightbulb", 
			"Heart Monitor", "Launch Platform", "Metronome", "Liquid Valve", "Centrifuge", "Gate", "Pressure Valve", "Activation Fuse", 
			"Laser Pointer", "Timed Gate", "Lagbox" , "Heating Element", "Cooling Element", "Gyroscope Stabiliser", "Holographic Display",
			"Floodlight", "Servo", "Rotor", "Hover Thruster"


		};

		public static string[] NoClip =
        {
			"Jukebox", "Radio", "Small Button", "Television", "Tesla Coil", "Thruster", "Thrusterbed", "Wooden Chair", "Bus Chair",
			"Rebirther", "1000kg Weight", "Atom Bomb", "Battery", "Floodlight", "Generator", "Gyroscope Stabiliser", "Holographic Display", 
			"Industrial Generator", "Industrial Gyrostabiliser", "Key Trigger", "Lagbox", "Life Detector", "Lightbulb",
			"Metal Detector", "Metronome", "Mini Thruster", "Mirror", "Motion Detector", "Plastic Barrel", "Pumpkin", "Red Barrel",
			"Rotor", "Shock Detector", "Servo", "Timed Gate", "Toggleable Mirror", "Text Display", "Wooden Table", "Siren", "Blood Tank", 
			"Accumulator", "Liquidentifier", "Decimator", "Electromagnet", "Defibrillator", "Fan", "Fire Detector", "EMP Generator", 
			"Electricity Transformer", "Crystal", "Crossbow Bolt", "Crate", "Clamp", "BR Signal Converter", "Bowling Pin", "Bicycle",
			"BG Signal Converter", "Balloon", "Activation Transformer", "Activation Fuse", "Small Bush", "Large Bush", "Tall Tree",
			"Trunk",  "Launch Platform", "Lightbulb", "Spot Light", "Heating Element", "Cooling Element"
        };

		public static string[] NoClipPartial =
        {
			"twig", "branch", "cathode",
        };

		//public static void RescanMap(bool ForceRefresh=false)
		//{
		//	if ( NewItems.Count > 0 && Time.frameCount > SpawnFrame + 5)
		//	{
		//		AllItems.AddRange(NewItems.Where( x => xxx.CanHold(x)));
		//		NewItems.Clear();
		//	}
		//	if (Time.time - LastScanTime < 5f && !ForceRefresh) return;

		//	//	Get all Persons and remove NPC and dead
		//	AllPersons = new List<PersonBehaviour>(UnityEngine.Object.FindObjectsOfType<PersonBehaviour>());
		//	AllPersons.RemoveAll(x => !x.IsAlive() || x.TryGetComponent<NpcBehaviour>(out _));

		//	AllNPC = new List<NpcBehaviour>(UnityEngine.Object.FindObjectsOfType<NpcBehaviour>());
		//	AllNPC.RemoveAll(x => !x.IsAlive);

		//	//	Get all interesting items and remove things that cant be held
		//	AllItems.Clear();
		//	AllItems.AddRange(Global.main.PhysicalObjectsInWorld.Where( x => xxx.CanHold(x)) );

		//	LastScanTime = Time.time;
		//}

		public struct NpcTargets
		{
			public NpcBehaviour friend;
			public float friendDist;
			public NpcBehaviour enemy;
			public float enemyDist;
			public PersonBehaviour person;
			public float personDist;
			public PhysicalBehaviour item;
			public float itemDist;
			public Props prop;
			public float propDist;
		}




		//public static Transform GetClosestEnemy (Transform sourceT, Transform[] enemies)
		//{
		//	Transform bestTarget = null;
		//	float closestDistanceSqr = Mathf.Infinity;
		//	Vector3 currentPosition = sourceT.position;
		//	foreach(Transform potentialTarget in enemies)
		//	{
		//		Vector3 directionToTarget = potentialTarget.position - currentPosition;
		//		float dSqrToTarget = directionToTarget.sqrMagnitude;
		//		if(dSqrToTarget < closestDistanceSqr)
		//		{
		//			closestDistanceSqr = dSqrToTarget;
		//			bestTarget = potentialTarget;
		//		}
		//	}

		//	return bestTarget;
		//}

		//public static NpcTargets GetClosest(NpcBehaviour MyNpc, bool onlyFacing=false)
		//{
		//	NpcTargets targets = new NpcTargets()
		//	{
		//		friendDist  = float.MaxValue,
		//		enemyDist   = float.MaxValue,
		//		personDist  = float.MaxValue,
		//		itemDist    = float.MaxValue,
		//	};

		//	List<NpcBehaviour> npcs = new List<NpcBehaviour>();
		//	List<float> npcDist     = new List<float>();


		//	MyNpc.MyFriends.Clear();
		//	MyNpc.MyEnemies.Clear();

		//	int TeamId = MyNpc.TeamId;
		//	string compare1 = MyNpc.name; 
		//	string compare2 = MyNpc.Config.NpcType;
		//	Vector3 MyPos   = MyNpc.Head.position;
		//	float dist;

		//	foreach ( NpcBehaviour otherNpc in AllNPC ) { 
		//		if (!otherNpc || otherNpc == MyNpc) continue;
		//		if (onlyFacing && MyNpc.Facing * otherNpc.Head.position.x > MyNpc.Facing * MyNpc.Head.position.x) continue;

		//		if (otherNpc.TeamId > 0 && otherNpc.TeamId == TeamId) {

		//			if (MyNpc.MyGroup.Contains(otherNpc)) continue;

		//			MyNpc.MyFriends.Add(otherNpc);

		//			dist = ((MyPos - otherNpc.Head.position).sqrMagnitude);

		//			if (dist < targets.friendDist)
		//			{
		//				targets.friend     = otherNpc; 
		//				targets.friendDist = dist;
		//			}
		//		}
		//		else
		//		{
		//			MyNpc.MyEnemies.Add(otherNpc);

		//			if (otherNpc.ThreatLevel < 0) continue;

		//			dist = ((MyPos - otherNpc.Head.position).sqrMagnitude);

		//			if (dist < targets.enemyDist)
		//			{
		//				targets.enemy     = otherNpc; 
		//				targets.enemyDist = dist;
		//			}
		//		}
		//	}

		//	foreach ( PersonBehaviour person in AllPersons )
		//	{
		//		if (!person || person == MyNpc.PBO) continue;
		//		if (onlyFacing && MyNpc.Facing * person.transform.position.x > MyNpc.Facing * MyNpc.Head.position.x) continue;

		//		dist = ((MyPos - person.Limbs[0].transform.position).sqrMagnitude);

		//		if (dist < targets.personDist)
		//		{
		//			targets.person     = person; 
		//			targets.personDist = dist;
		//		}
		//	}

		//	foreach ( PhysicalBehaviour pbItem in AllItems )
		//	{
		//		if (!pbItem || !xxx.CanHold(pbItem) || Mathf.Abs(pbItem.transform.position.y - MyNpc.Head.position.y) > 2f) continue;
		//		if (onlyFacing && MyNpc.Facing * pbItem.transform.position.x > MyNpc.Facing * MyNpc.Head.position.x) continue;

		//		dist = ((MyPos - pbItem.transform.position).sqrMagnitude);

		//		if (dist < targets.personDist)
		//		{
		//			targets.item     = pbItem; 
		//			targets.itemDist = dist;
		//		}
		//	}

		//	return targets;
		//}
	}


}
