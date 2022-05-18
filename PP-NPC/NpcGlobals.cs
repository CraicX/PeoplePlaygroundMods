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

namespace PPnpc
{

	public struct ActWeight
	{
		public float Fight;
		public float Defend;
		public float DefendPerson;
		public float GroupUp;
		public float Scavenge;
		public float Retreat;
		public float Wander;
		public float Survive;
		public float Dying;
		public float Dead;
		public float TakeCover;
		public float FightFire;
		public float WatchEvent;
		public float Medic;
		public float Shoot;
		public float Recruit;
		public float Fidget;
		public float Disco;
		private float _startweight;
		public ActWeight( float startWeight )
		{
			Fight = Defend = DefendPerson = GroupUp = Scavenge = Retreat = Wander = Survive = TakeCover = Dying = Dead = 
				FightFire = WatchEvent = Medic = Shoot = Recruit = Fidget = Disco = _startweight = startWeight;
		}

		public void reset() => Fight = Defend = DefendPerson = GroupUp = Scavenge = Retreat = Survive = Dying = Dead = 
			TakeCover = FightFire = WatchEvent = Medic = Shoot = Recruit = Fidget = Disco = _startweight;
	}

	public struct NpcGoals
    {
		public NpcBehaviour NPC;
		public bool Scavenge;
		public bool Attack;
		public bool Shoot;
		public bool Recruit;
		
		public NpcGoals( NpcBehaviour npc )
        {
			NPC      = npc;
			Scavenge = true;
			Attack   = false;
			Shoot	 = false;
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

	

    public enum QueueState
	{
		Stopped,
		Idle,
		BuildingQueue,
		CheckingQueue,
		RunningQueue,
	}

	public enum NpcActions
	{
		Walking,
		Waiting,
		Sitting,
		Sleeping,
		PickupClosestItem,
		Confront,
		Attack,
		AimAt,
		Shoot,
		WoundEnemy,
		StartShit,
		Cry,
		Flip,
		Socialize,
		Cancel,
	}

	public enum NpcPrimaryActions
	{
		Wait,
		Wander,
		Scavenge,
		Defend,
		DefendPerson,
		Retreat,
		Fight,
		Destroy,
		GroupUp,
		Regroup,
		Thinking,
		Survive,
		TakeCover,
		Dying,
		Dead,
		FightFire,
		WatchEvent,
		Dive,
		Medic,
		Shoot,
		Recruit,
		Fidget,
		Disco,
	}

	public enum AimStyles
	{
		Standard,
		Crouched,
		Proned,
		Spray,
		Rockets,
	};

	

    public enum Gadgets
	{
		AIChip,
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

	public struct QueueAct
	{
		public NpcActions Action;
		public NpcBehaviour NPC;
		public float TimeStart;
		public float TimeStop;
		public float Seconds;
		public bool FlagBool;
		public int FlagInt;
		public float FlagFloat;
		public Vector2 FlagVector;
		public NpcTool Thing;
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

	public class NpcEnhancements
	{
		private NpcBehaviour parent;
		private float _vision     = 35f;
		private float _strength   = 10f;
		private float _processing = 5f;
		private float _aiming     = 50f;
		private bool _medic       = false;

		public float Vision
        {
			get { return _vision; }
			set { _vision = value; }
        }
		public float Strength
        {
			get { return _strength; }
			set { _strength = value; }
        }
		public float Processing
        {
			get { return _processing; }
			set { _processing = value; }
        }
		public float Aiming
        {
			get { return _aiming; }
			set { _aiming = value; }

        }
		public bool Medic
        {
			get { return _medic; }
			set { _medic = value; }
        }

		public NpcEnhancements(NpcBehaviour npc)
        {
			parent = npc;
        }
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

		public static string[] ItemsNoCollide;
        
		public static string[] ToyNames =
        {
			"Jukebox", "Radio", "Small Button", "Television", "Tesla Coil", "Thruster", "Thrusterbed", "Wooden Chair", "Bus Chair"
        };

		public static void RescanMap(bool ForceRefresh=false)
		{
			if ( NewItems.Count > 0 && Time.frameCount > SpawnFrame + 5)
			{
				AllItems.AddRange(NewItems.Where( x => xxx.CanHold(x)));
				NewItems.Clear();
			}
			if (Time.time - LastScanTime < 5f && !ForceRefresh) return;

			//	Get all Persons and remove NPC and dead
			AllPersons = new List<PersonBehaviour>(UnityEngine.Object.FindObjectsOfType<PersonBehaviour>());
			AllPersons.RemoveAll(x => !x.IsAlive() || x.TryGetComponent<NpcBehaviour>(out _));

			AllNPC = new List<NpcBehaviour>(UnityEngine.Object.FindObjectsOfType<NpcBehaviour>());
			AllNPC.RemoveAll(x => !x.IsAlive);

			//	Get all interesting items and remove things that cant be held
			AllItems.Clear();
			AllItems.AddRange(Global.main.PhysicalObjectsInWorld.Where( x => xxx.CanHold(x)) );

			LastScanTime = Time.time;
		}

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


		

		public static Transform GetClosestEnemy (Transform sourceT, Transform[] enemies)
		{
			Transform bestTarget = null;
			float closestDistanceSqr = Mathf.Infinity;
			Vector3 currentPosition = sourceT.position;
			foreach(Transform potentialTarget in enemies)
			{
				Vector3 directionToTarget = potentialTarget.position - currentPosition;
				float dSqrToTarget = directionToTarget.sqrMagnitude;
				if(dSqrToTarget < closestDistanceSqr)
				{
					closestDistanceSqr = dSqrToTarget;
					bestTarget = potentialTarget;
				}
			}
	 
			return bestTarget;
		}

		public static NpcTargets GetClosest(NpcBehaviour MyNpc, bool onlyFacing=false)
		{
			NpcTargets targets = new NpcTargets()
			{
				friendDist  = float.MaxValue,
				enemyDist   = float.MaxValue,
				personDist  = float.MaxValue,
				itemDist    = float.MaxValue,
			};

			List<NpcBehaviour> npcs = new List<NpcBehaviour>();
			List<float> npcDist     = new List<float>();


			MyNpc.MyFriends.Clear();
			MyNpc.MyEnemies.Clear();

			int TeamId = MyNpc.TeamId;
			string compare1 = MyNpc.name; 
			string compare2 = MyNpc.Config.NpcType;
			Vector3 MyPos   = MyNpc.Head.position;
			float dist;

			foreach ( NpcBehaviour otherNpc in AllNPC ) { 
				if (!otherNpc || otherNpc == MyNpc) continue;
				if (onlyFacing && MyNpc.Facing * otherNpc.Head.position.x > MyNpc.Facing * MyNpc.Head.position.x) continue;
				
				if (otherNpc.TeamId > 0 && otherNpc.TeamId == TeamId) {
					
					if (MyNpc.MyGroup.Contains(otherNpc)) continue;

					MyNpc.MyFriends.Add(otherNpc);

					dist = ((MyPos - otherNpc.Head.position).sqrMagnitude);

					if (dist < targets.friendDist)
					{
						targets.friend     = otherNpc; 
						targets.friendDist = dist;
					}
				}
				else
				{
					MyNpc.MyEnemies.Add(otherNpc);
					
					if (otherNpc.ThreatLevel < 0) continue;

					dist = ((MyPos - otherNpc.Head.position).sqrMagnitude);

					if (dist < targets.enemyDist)
					{
						targets.enemy     = otherNpc; 
						targets.enemyDist = dist;
					}
				}
			}

			foreach ( PersonBehaviour person in AllPersons )
			{
				if (!person || person == MyNpc.PBO) continue;
				if (onlyFacing && MyNpc.Facing * person.transform.position.x > MyNpc.Facing * MyNpc.Head.position.x) continue;

				dist = ((MyPos - person.Limbs[0].transform.position).sqrMagnitude);

				if (dist < targets.personDist)
				{
					targets.person     = person; 
					targets.personDist = dist;
				}
			}

			foreach ( PhysicalBehaviour pbItem in AllItems )
			{
				if (!pbItem || !xxx.CanHold(pbItem) || Mathf.Abs(pbItem.transform.position.y - MyNpc.Head.position.y) > 2f) continue;
				if (onlyFacing && MyNpc.Facing * pbItem.transform.position.x > MyNpc.Facing * MyNpc.Head.position.x) continue;

				dist = ((MyPos - pbItem.transform.position).sqrMagnitude);

				if (dist < targets.personDist)
				{
					targets.item     = pbItem; 
					targets.itemDist = dist;
				}
			}

			return targets;
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
}
