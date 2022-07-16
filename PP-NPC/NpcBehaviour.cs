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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace PPnpc
{
	[SkipSerialisation]
	public class NpcBehaviour : MonoBehaviour, Messages.IShot
	{

		public static bool GlobalShowStats = false;

		public LightSprite RebirthLight;
		
		public bool _disableFlip   = false;
		public NpcConfig Config   = new NpcConfig();
		private bool _haltAction  = false;
		public float ArsenalTimer = 0;
		public bool ShowingStats  = false;
		public bool InRebirth     = false;
		public bool ShowStats     = false;
		public bool NoFight       = false;
		public bool NoShoot       = false;
		public bool NoEntry       = false;
		public bool InHealZone    = false;
		public bool DoingStrike   = false;

		public int BloodyNose     = 0;

		public NpcHoverStats HoverStats;

		public bool RunningSafeCollisions = false;
		public bool GetUp                 = false;
		public bool HaltAction
		{
			get { return _haltAction; }
			set { _haltAction = value; }
		}

		public Color ChatColor;

		public PersonBehaviour PBO;
		public int NpcId;

		public Transform Head;
		public LimbBehaviour HeadL;

		public NpcBehaviour closestNpc;
		public NpcBehaviour NpcEnemy;
		public NpcMojo Mojo;
		public NpcAction Action;
		public NpcActions Actions;
		public NpcMemory Memory;

		public Collider2D[] MyColliders;

		public NpcGoals Goals;

		public int LastHit	= 0;
		public int LastShot = 0;
		public float LastSmack = 0f;
		public float NoFlipTimeout = 0f;

		public List<Transform> NoGhost = new List<Transform>();

		public bool CanGhost = true;

		private string LastThingSaid = "";
		private string LastCatSaid   = "";
		public float LastCaveman = 0f;

		//
		//  -- SCAN VARS --------------
		//
		public float ScanInterval       = 5;
		public float ScanTimeExpires    = 0;
		public int ScanResultsCount     = 0;
		public Vector2 ScanStart        = Vector2.zero;
		public Vector2 ScanStop         = Vector2.zero;
		public Collider2D[] ScanResults = new Collider2D[50];
		public bool ScannedWall         = false;

		public List<NpcBehaviour> ScannedNpc           = new List<NpcBehaviour>();
		public List<PersonBehaviour> ScannedPeople     = new List<PersonBehaviour>();
		public List<Props> ScannedProps                = new List<Props>();
		public List<PhysicalBehaviour> ScannedThings   = new List<PhysicalBehaviour>();
		public List<PhysicalBehaviour> ScannedPhys	   = new List<PhysicalBehaviour>();

		public List<NpcBehaviour> ScannedNpcIgnored            = new List<NpcBehaviour>();
		public List<PersonBehaviour> ScannedPeopleIgnored      = new List<PersonBehaviour>();
		public List<Props> ScannedPropsIgnored                 = new List<Props>();
		public List<PhysicalBehaviour> ScannedThingsIgnored    = new List<PhysicalBehaviour>();
		public Dictionary<float, NpcBehaviour> TimedNpcIgnored = new Dictionary<float, NpcBehaviour>();
		//
		//  -------------------------
		//
		public AudioSource audioSource;
		public AimStyles CurrentAimStyle = AimStyles.Standard;
		public NpcPose MyNpcPose;

		public Dictionary<string, NpcPose> CommonPoses;

		public bool HasKnife       = false;
		public bool HasClub        = false;
		public bool HasSword       = false;
		public bool HasGun         => (FH && FH.IsHolding && FH.PB && FH.Tool && FH.Tool.props && FH.Tool.props.canShoot) || (BH && BH.IsHolding && BH.PB && BH.Tool && BH.Tool.props && BH.Tool.props.canShoot);
		public bool HasExplosive   = false;
		public bool HasFireF	   = false;

		public float MyBlood       = 0f;
		private float _threatLevel = 1f;

		public bool DisableFlip
		{
			get { 
				if (_disableFlip && Time.time > NoFlipTimeout) _disableFlip = false;
				return _disableFlip; 
			}
			set { 
				_disableFlip = value; 
				if (value) NoFlipTimeout = Time.time + 10f;
			}
		}


		public float TimerIgnorePickup = 0;

		public float ThreatLevel
		{
			get { return _threatLevel; }
		}
		public static ContactFilter2D filter = new ContactFilter2D();

		public bool IsAiming => FH.IsAiming || BH.IsAiming;

		public float TotalWeight   = 0f;

		public static RaycastHit2D[] HitResults   = new RaycastHit2D[10];
		public static Collider2D[] CollideResults = new Collider2D[100];

		public static RagdollPose WalkPose;

		public static bool HasArmory = false;

		public List<NpcBehaviour> MyGroup         = new List<NpcBehaviour>();
		public List<NpcBehaviour> MyFights        = new List<NpcBehaviour>();
		public List<NpcBehaviour> MyFriends       = new List<NpcBehaviour>();
		public List<NpcBehaviour> MyEnemies       = new List<NpcBehaviour>();
		public List<NpcBehaviour> IgnoreNpc       = new List<NpcBehaviour>();

		private readonly Dictionary<string, RigidSnapshot> RigidOriginals = new Dictionary<string, RigidSnapshot>();

		public List<int> EventInfoIds             = new List<int>();
		public EventInfo CurrentEventInfo;

		public int GroupSize          = 1;
		public bool AcknowledgeThreat = false;
		public bool FireProof		  = false;

		public int TeamId = 0;

		public int CollisionCounter = 0;

		public AIMethods AIMethod = AIMethods.Spawn;

		public HingeJoint2D[] LimbJoints;

		public NpcHand FH;
		public NpcHand BH;
		public NpcHand[] Hands;
		NpcHand hand;
		public NpcHand RandomHand => new NpcHand[]{FH, BH}.PickRandom();

		public float MyHeight = 0f;
		public float MyWidth  = 0f;

		public float KickLanded = 0;
		public RectTransform RT;

		float floorChecked = 0f;
		Vector2 _floor;
		public Vector2 Floor() { 
			if (Time.time < floorChecked) return _floor;
			floorChecked = Time.time + 5f;
			_floor = xxx.FindFloor(Head.position);
			return _floor;
		}

		private bool _EKarate   = false;
		private bool _EFirearms = false;
		private bool _EMelee    = false;
		private bool _EMemory   = false;
		private bool _EThrow    = false;
		private bool _ETroll    = false;
		private bool _EHero     = false;

		public bool EnhancementKarate   { get { return _EKarate; } 
			set { 
				if (value && !_EKarate && HoverStats) HoverStats.AddIcon(IconTypes.Upgrade,2);
				_EKarate = value; 
				if (Actions) Actions.RecacheMoves = true;
			}
		}
		public bool EnhancementFirearms { get { return _EFirearms; } 
			set { 
				if (value && !_EFirearms && HoverStats) HoverStats.AddIcon(IconTypes.Upgrade,1);
				_EFirearms = value; 
				if (Actions) Actions.RecacheMoves = true;
				ScannedPropsIgnored.Clear();
				ScannedThingsIgnored.Clear();
			}
		}
		public bool EnhancementMelee    { get { return _EMelee; } 
			set { 
				if (value && !_EMelee && HoverStats) HoverStats.AddIcon(IconTypes.Upgrade,3);
				_EMelee = value; 
				if (Actions) Actions.RecacheMoves = true;
				ScannedPropsIgnored.Clear();
				ScannedThingsIgnored.Clear();
			}
		}
		public bool EnhancementMemory   { get { return _EMemory; } 
			set { 
				if (value && !_EMemory && HoverStats) HoverStats.AddIcon(IconTypes.Upgrade,0);
				_EMemory = value; 
			}
		}

		public bool EnhancementThrow { get { return _EThrow; } 
			set {
				if (value && !_EThrow && HoverStats) HoverStats.AddIcon(IconTypes.Upgrade,0);
				_EThrow = value; 		
			}
		}

		public bool EnhancementHero { get { return _EHero; } 
			set {
				if (value && !_EHero && HoverStats) HoverStats.AddIcon(IconTypes.Upgrade,5);
				_EHero = value; 		
			}
		}

		public bool EnhancementTroll { get { return _ETroll; } 
			set {
				if (value && !_ETroll && HoverStats) HoverStats.AddIcon(IconTypes.Upgrade,4);
				_ETroll = value; 		
			}
		}

		private float _HurtLevel  = 0;
		private float _hurtCacheTime = 0;
		public float HurtLevel
        {
			get
            {
				if (Time.time < _hurtCacheTime) return _HurtLevel;

				float hurt   = 0;
				int wounds   = 0;
				int bruises  = 0;
				foreach ( LimbBehaviour limb in PBO.Limbs ) { 
					wounds += limb.CirculationBehaviour.GunshotWoundCount + limb.CirculationBehaviour.StabWoundCount;
					bruises += limb.BruiseCount;
					if (!limb.IsConsideredAlive || !limb.IsCapable || limb.Broken || limb.IsDismembered) hurt += 2;
				}

				hurt += wounds * 0.5f;
				hurt += bruises * 0.3f;
				if (PBO.Consciousness < 1) hurt += (1 - PBO.Consciousness) * 10;
				hurt += PBO.ShockLevel * 5;

				_HurtLevel = hurt;

				_hurtCacheTime = Time.time + 1f;

				return hurt;
            }
        }

		public Dictionary<Transform, float> BGhost = new Dictionary<Transform, float>();

		public List<StatsIcon> Icons = new List<StatsIcon>();

		public NpcGlobal.NpcTargets MyTargets;

		public static string[] DefenseActions = { "Defend", "DefendPerson", "Retreat", "TakeCover", "Survive", "Fight", "Shoot" };

		public static string[] DontNotify = { "Wait", "Wander", "Thinking", "FrontKick", "SoccerKick", };

		public static string[] CanAttackActions = { "Wait", "Thinking", "Shoot", "Survive", "Fight", "Defend", "Wander", "Attack" };

		public static string[] AttackActions = { "Fight", "Shoot", "FrontKick", "SoccerKick", "Tackle", "Shove", "Caveman", "Club", "Attack"};

		public float LastTimeShot = 0f;

		public ActivityMessages ActivityMessage;



		private bool isSetup     = false;
		public bool PauseHold    = false;
		public bool CancelAction = false;

		public Dictionary<int, Opinion> Interactions = new Dictionary<int, Opinion>();
		public Dictionary<int, float> PeepCollisions = new Dictionary<int, float>();
		public Dictionary<string, Rigidbody2D>   RB  = new Dictionary<string, Rigidbody2D>();
		public Dictionary<string, LimbBehaviour> LB  = new Dictionary<string, LimbBehaviour>();
		public Dictionary<string, LimbPoseSnapshot> PoseSnapshots = new Dictionary<string, LimbPoseSnapshot>();

		public List<EventLog> EventLog = new List<EventLog> ();
		public EventLog LastLog        = new EventLog ();
		public float LastLogTime       = 0;


		private bool _active = false;
		public float _facing = 0f;

		public bool Active
		{
			get { return _active; }
			set { 
				_active = value; 
			}
		}

		private bool _IsDead = false;
		public bool IsDead
		{
			get { return _IsDead; }
			set
			{
				_IsDead = value;
				if ( value ) Death();
			}
		}

		public float Facing
		{
			get { _facing = PBO && PBO.transform.localScale.x < 0.0f ? 1f : -1f; return _facing; }
		}

		public bool IsUpright  => (bool)(!LB["LowerBody"].IsOnFloor
												   && !LB["UpperBody"].IsOnFloor
												   && !LB["Head"].IsOnFloor
												   && (LB["FootFront"].IsOnFloor
												   || LB["Foot"].IsOnFloor));

		public bool IsFlipped                           => (bool)(PBO.transform.localScale.x < 0.0f);
		public void Flip() {
			if (DisableFlip || !OnFeet()) return; 

			Utilities.FlipUtility.Flip(LB["Head"].PhysicalBehaviour); 
			ScanTimeExpires = 0;
		}

		public bool IsAlive			                    => HeadL && HeadL.IsConsideredAlive;
		public bool CanWalk								=> PBO && PBO.IsTouchingFloor && PBO.IsAlive() 
														&& !PBO.Braindead && PBO.Consciousness == 1 
														&& !LB["Foot"].Broken && !LB["FootFront"].Broken
														&& !LB["LowerLeg"].Broken && !LB["LowerLegFront"].Broken
														&& !LB["UpperLeg"].Broken && !LB["UpperLegFront"].Broken
														&& LB["Foot"].IsCapable && LB["FootFront"].IsCapable
														&& LB["LowerLeg"].IsCapable && LB["LowerLegFront"].IsCapable;
		public bool FacingToward( NpcBehaviour npc )	=> npc && npc.Head && FacingToward(npc.Head.position.x);
		public bool FacingToward( Vector2 pos )			=> FacingToward( pos.x );
		public bool FacingToward( float posX )			=> ( ( posX * Facing < Head.position.x * Facing ) );
		void Start()                                    => Invoke("Setup", 2f);
		public NpcHand OpenHand
		{
			get
			{
				NpcHand hand             = Hands.PickRandom();
				if (hand.IsHolding) hand = hand.AltHand;
				return hand;
			}
		}

		public virtual void CustomSetup() {	}


		public void Update()
		{
			//if (Input.GetKeyDown(KeyCode.K)) ResetLimbs();
			if (FireProof)
			{
				PBO.PainLevel       = 0.0f;
				PBO.ShockLevel      = 0.0f;
				PBO.AdrenalineLevel = 1.0f;
			}
			if (NpcMain.DEBUG_DRAW) ModAPI.Draw.Rect(ScanStart,ScanStop);

			//if (GetUp) {
			//	GetUp = false;
			//	if (HurtLevel <= 3) RunLimbs(LimbUp,1.5f);
			//	if (!GetUp)
			//	{
			//		PBO.LinkedPoses[PoseState.Rest].ShouldStumble = true;
			//	}
			//} 
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: DECISIONS
		// ────────────────────────────────────────────────────────────────────────────
		public void DecideWhatToDo(bool SkipScan=false, bool SkipScoring=false)
		{
			CollisionCounter = 0;
			CancelAction     = false;

			Action.NPCTargets.Clear();
			Action.PropTargets.Clear();
			Action.ItemTargets.Clear();
			Action.PeepTargets.Clear();
			

			if (!SkipScan) NPCScanView();
			if (!SkipScoring) Action.CurrentAction = Action.GetBestScore();

			if ( Action.CurrentAction == "Wait" )
			{
				if (FH.IsAiming) {FH.IsAiming = false; FH.FireAtWill = false;}
				if (BH.IsAiming) {BH.IsAiming = false; BH.FireAtWill = false;}

				if (xxx.rr(1,5) == 3 && CanWalk) Action.CurrentAction = "Wander";
			}

			if (Action.PCR != null) Action.StopCoroutine(Action.PCR);

			if (Action.NPCTargets.TryGetValue(Action.CurrentAction, out NpcBehaviour npc))			MyTargets.enemy  = npc;
			if (Action.PropTargets.TryGetValue( Action.CurrentAction, out Props prop))				MyTargets.prop   = prop;
			if (Action.PeepTargets.TryGetValue( Action.CurrentAction, out PersonBehaviour peep))		MyTargets.person = peep;
			if (Action.ItemTargets.TryGetValue( Action.CurrentAction, out PhysicalBehaviour item))	MyTargets.item   = item;

			Action.NPCTargets.Clear();
			Action.PropTargets.Clear();
			Action.PeepTargets.Clear();
			Action.ItemTargets.Clear();

			Action.ActByName(Action.CurrentAction);

		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: N P C   S C A N   A R E A : :  :   :    :     :        :          :
		// ────────────────────────────────────────────────────────────────────────────
		public void NPCScanView()
		{
			if (!PBO) return;
			ScanAhead( Mojo.Stats["Vision"] / 2);


			//NpcGlobal.RescanMap();
			Action.ResetWeights();
			PBO.OverridePoseIndex = -1;

			if (Goals.Scavenge) SvScavenge();
			if (NpcArsenal.Arsenals.Count > 0 && Goals.Scavenge && ArsenalTimer < Time.time && Action.Weights["Scavenge"] <= 0 ) SvGearUp();
			
			if (EnhancementMemory && Memory.LastContact != null) SvLastContact();
			
			
			if (Goals.Recruit) SvRecruit();
			if (Goals.Upgrade) SvUpgrade();
			if (EnhancementFirearms ) SvShoot();
			if (EnhancementKarate) SvKarate();
			if (EnhancementMelee && HasKnife || HasClub || HasSword) SvAttackMelee();
			//if (EnhancementMelee && HasKnife) SvAttackKnife();

			
			SvCheckForThreats();
			SvCheckForInjuries();
			SvCheckForEvents();
			SvCheckEnvironment();
			if (EnhancementTroll) { 
				SvCheckTroll();
				SvCheckWitness();
			}
			//SvCheckRandomScan();

			if (Mojo.Feelings["Bored"] > 75) SvFidget();

			Action.CurrentAction = Action.GetBestScore();
		
				
		}

		public float WallDistance = 0f;

		public void ScanAhead( float distance=10f )
		{
			foreach ( SignPost signPost in NpcGlobal.SignPosts )
			{
				if ( signPost.SignType == Gadgets.NoEntrySign )
				{
					if (Head.position.x > signPost.xStart && 
						Head.position.x < signPost.xEnd)
					{
						Action.CurrentAction = "Leaving area";
						if (!FacingToward(signPost.Sign.position)) Flip();
						StartCoroutine(Actions.ISubWalkTo(signPost.Sign, 0.2f, 0.3f));
					}
				}
			}

			if (Time.time > ScanTimeExpires) {

				ScanTimeExpires = Time.time + ScanInterval;

				ScanStart        = RB["MiddleBody"].position + (Vector2.right * ((distance/1.9f) * -Facing));
				ScanStop		 = new Vector2(distance, 3.5f);

				ScanResultsCount = Physics2D.OverlapBox(ScanStart, ScanStop, 0f, filter, ScanResults);


				ScannedNpc.Clear();
				ScannedPeople.Clear();
				ScannedProps.Clear();
				ScannedThings.Clear();
				ScannedPhys.Clear();

				MyTargets = new NpcGlobal.NpcTargets()
				{
					friendDist  = float.MaxValue,
					enemyDist   = float.MaxValue,
					personDist  = float.MaxValue,
					itemDist    = float.MaxValue,
					propDist    = float.MaxValue,
					item        = null,
					enemy       = null,
					friend      = null,
					person      = null,
					prop        = null,
				};

				ScannedWall = false;
				PhysicalBehaviour px = null;

				for(int i = -1; ++i < ScanResultsCount;) {

					if (ScanResults[i].gameObject.name == "root") continue;
					if (ScanResults[i].gameObject.name.Contains("wall")) {
						ScannedWall  = true;
						WallDistance = Mathf.Abs(Head.position.x - ScanResults[i].gameObject.transform.position.x);
					}
					px = null;
					GameObject groot = ScanResults[i].transform.root.gameObject;

					if (groot != ScanResults[i].gameObject) {

						//  Check for NPC
						if ( groot.TryGetComponent<NpcBehaviour>( out NpcBehaviour npc ) )
						{
							if (npc == this || TimedNpcIgnored.Values.Contains(npc) || ScannedNpcIgnored.Contains(npc) || ScannedNpc.Contains(npc)) continue;

							ScannedNpc.Add(npc);
						}
						else if ( groot.TryGetComponent<PersonBehaviour>( out PersonBehaviour person ) )
						{
							if (!ScannedPeople.Contains(person)) ScannedPeople.Add(person);
						}
						else if ( ScanResults[i].gameObject.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour phys ) )
						{
							if (ScannedThingsIgnored.Contains(phys)) continue;

							px = phys;
							ScannedPhys.Add(phys);
							if ( phys.transform.position.y < Head.position.y )
							{
								if (!ScannedThings.Contains(phys)) ScannedThings.Add(phys);
							}
						}

						if ( !px && ScanResults[i].gameObject.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour phys2 ) )
						{
							if (ScannedThingsIgnored.Contains(phys2)) continue;
							if (!ScannedPhys.Contains(phys2)) ScannedPhys.Add(phys2);
						}


					}
					else
					{
						if ( ScanResults[i].gameObject.TryGetComponent<Props>( out Props prop ) )
						{
							if (!ScannedPropsIgnored.Contains(prop) && prop.P.transform.position.y < Head.position.y) ScannedProps.Add(prop);
						}
						else 
						if ( ScanResults[i].gameObject.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour phys ) )
						{
							if (ScannedThingsIgnored.Contains(phys)) continue;
							if ( phys.transform.position.y < Head.position.y )
							{

								if (xxx.CanHold(phys))
								{

									prop = phys.gameObject.GetOrAddComponent<Props>();
									prop.Init(phys);
									if (!ScannedProps.Contains(prop)) ScannedProps.Add(prop);
								} 
								else
								{
									if (!ScannedThings.Contains(phys)) ScannedThings.Add(phys);
								}
							}
						}
					}
				}
			}
		}

		
		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC Attack
		// ────────────────────────────────────────────────────────────────────────────
		public void SvLastContact()
		{
			if ( !Memory.LastContact || !Memory.LastContact.PBO.IsAlive() ) return;

			if (IsUpright && !NoFight && !Memory.LastContact.NoFight) { 
				Action.NPCTargets["LastAttack"] = Memory.LastContact;
				Action.Weights["LastAttack"] = xxx.rr(1,10);
			}

			if (HasGun && !NoFight && !NoShoot && !Memory.LastContact.NoFight && !Memory.LastContact.NoShoot )
			{
				Action.NPCTargets["LastShoot"] = Memory.LastContact;
				Action.Weights["LastShoot"]    = xxx.rr(1,10);
			}

		}
		
		
		
		public void SvAttackMelee()
		{
			if (NoFight) return;
			if (OnFeet()) 
			{ 
				float distance  = float.MaxValue;
				bool haveTarget = false;
				float temp;

				foreach ( NpcBehaviour npc in ScannedNpc )
				{
					if (!npc || !npc.PBO || npc.NoFight ) continue;
					
					if (Mathf.Abs(Head.position.y - npc.Head.position.y) > 0.4f) continue;
					
					temp = Mathf.Abs(Head.position.x - npc.Head.position.x);


					if ( CanAttackActions.Contains( npc.Action.CurrentAction ) && temp < distance )
					{
						distance = temp;
						haveTarget = true;
						Action.NPCTargets["Attack"] = npc;
					}

				}

				if (haveTarget) Action.Weights["Attack"] = xxx.rr(5,10);
			}

			return;

		}


		



		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC Scavenge
		// ────────────────────────────────────────────────────────────────────────────
		public void SvScavenge()
		{
			if ( ( !FH.IsHolding || !BH.IsHolding ) && ScannedProps.Count > 0 )
			{
				float baseScore = (FH.IsHolding == BH.IsHolding) ? 8:2;

				if (!LB["LowerLeg"].IsCapable || !LB["UpperLeg"].IsCapable) baseScore -= 2;
				if (!LB["LowerLegFront"].IsCapable || !LB["UpperLegFront"].IsCapable) baseScore -= 2;

				if (baseScore <= 0) return;

				bool onlyOneHand = (FH.IsHolding || BH.IsHolding);

				Props selectedProp      = (Props)null;
				float distance          = float.MaxValue;
				float tempdist          = 0;
				Vector3 position        = Head.position;
				float dangerDistance    = float.MaxValue;

				foreach(NpcBehaviour npcX in ScannedNpc)
				{
					if (!npcX || !npcX.PBO ) continue;

					if (npcX.IsAiming)
					{
						tempdist = (position - npcX.Head.position).magnitude;
						if (tempdist < dangerDistance)
						{
							dangerDistance = tempdist;
						}
					}
				}

				foreach ( Props prop in ScannedProps )
				{
					if (!prop || !prop.P) continue;
					if (prop.needsTwoHands && onlyOneHand) continue;
					if (!xxx.CanHold(prop.P) || prop.P.beingHeldByGripper) continue;
					tempdist = (position - prop.transform.position).sqrMagnitude;

					if (tempdist < distance)
					{
						selectedProp = prop;
						distance     = tempdist;
					}
				}

				if (selectedProp) {
					
					baseScore += (distance > dangerDistance) ? -2f : 2f;

					Action.PropTargets["Scavenge"]	= selectedProp;

					if (!IsUpright) baseScore *= 0.5f;

					Action.Weights["Scavenge"] = baseScore;
				} 
			}
		}

		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC GEAR UP
		// ────────────────────────────────────────────────────────────────────────────
		public void SvGearUp()
		{
			if ( IsUpright && !HasGun && NpcArsenal.Arsenals.Count > 0 )
			{
				float baseScore = 0;

				if (FH.IsHolding == BH.IsHolding) baseScore += 3;
				else baseScore += 1;

				if (!LB["LowerLeg"].IsCapable || !LB["UpperLeg"].IsCapable) baseScore -= 1;
				if (!LB["LowerLegFront"].IsCapable || !LB["UpperLegFront"].IsCapable) baseScore -= 1;

				if (baseScore <= 0) return;

				bool onlyOneHand           = (FH.IsHolding || BH.IsHolding);
				NpcArsenal selectedArsenal = (NpcArsenal)null;
				float distance             = float.MaxValue;
				float tempdist             = 0;
				Vector3 position           = Head.position;
				float dangerDistance       = float.MaxValue;

				foreach(NpcBehaviour npcX in ScannedNpc)
				{
					if (!npcX || !npcX.PBO ) continue;

					if (npcX.IsAiming)
					{
						tempdist = (position - npcX.Head.position).sqrMagnitude;
						if (tempdist < dangerDistance) dangerDistance = tempdist;
					}
				}

				if (NpcArsenal.Arsenals.Count == 0) return;

				foreach ( NpcArsenal arsenal in NpcArsenal.Arsenals )
				{
					if (!arsenal.CanNPCVisit(this)) continue;
					tempdist = (position - arsenal.transform.position).sqrMagnitude;

					if ( tempdist < distance )
					{
						selectedArsenal = arsenal;
						distance        = tempdist;
					}
				}

				if (selectedArsenal) {
					if (distance > dangerDistance) baseScore -= 2f; // something dangerous on the way
					
					Action.ItemTargets["GearUp"]        = selectedArsenal.P;
					Action.Weights["GearUp"]            = baseScore;
				}
			}
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC Recruit
		// ────────────────────────────────────────────────────────────────────────────
		public void SvRecruit()
		{
			if ( LB["Head"].IsAndroid ) return;
			if ( ScannedPeople.Count > 0 ) 
			{
				float baseScore = 0;

				if (FH.IsHolding && FH.Tool && FH.Tool.props && FH.Tool.props.canRecruit) baseScore += 1;
				if (BH.IsHolding && BH.Tool && BH.Tool.props && BH.Tool.props.canRecruit) baseScore += 1;

				if (!LB["LowerLeg"].IsCapable || !LB["UpperLeg"].IsCapable) baseScore -= 1;
				if (!LB["LowerLegFront"].IsCapable || !LB["UpperLegFront"].IsCapable) baseScore -= 1;

				if (baseScore <= 0) return;

				float distance		= float.MaxValue;
				float tempdist		= 0;
				Vector3 position	= Head.position;
				PersonBehaviour selectedPerson = null;

				float dangerDistance    = float.MaxValue;

				foreach(NpcBehaviour npcX in ScannedNpc)
				{
					if (!npcX || !npcX.PBO) continue;

					if (npcX.IsAiming)
					{
						tempdist = (position - npcX.Head.position).sqrMagnitude;
						if (tempdist < dangerDistance)
						{
							dangerDistance = tempdist;
						}
					}
				}

				foreach ( PersonBehaviour peep in ScannedPeople)
				{
					if (!peep ) continue;

					tempdist = (position - peep.transform.position).sqrMagnitude;

					if (tempdist < distance)
					{
						selectedPerson = peep;
						distance       = tempdist;
					}
				}

				if (selectedPerson) {
					if (distance > dangerDistance) baseScore -= 2f; // something dangerous on the way
					Action.PeepTargets["Recruit"] = selectedPerson;
					Action.Weights["Recruit"]  = baseScore;
				} 
			}
		}



		public void SvUpgrade()
		{
			foreach ( NpcHand hand in new NpcHand[]{FH, BH} ){
				if ( hand.IsHolding && hand.Tool && hand.Tool.props && hand.Tool.props.canUpgrade) {
					NpcChip chip = hand.Tool.G.GetComponent<NpcChip>();

					bool need = false;

					if (chip.ChipType == Chips.Memory && !EnhancementMemory) need = true;
					else if (chip.ChipType == Chips.Memory && !EnhancementMemory) need = true;
					else if (chip.ChipType == Chips.Troll && !EnhancementTroll) need = true;
					else if (chip.ChipType == Chips.Melee && !EnhancementMelee) need = true;
					else if (chip.ChipType == Chips.Hero && !EnhancementHero) need = true;
					else if (chip.ChipType == Chips.Karate && !EnhancementKarate) need = true;
					else if (chip.ChipType == Chips.Firearms && !EnhancementFirearms) need = true;

					if (need)
					{
						Action.Weights["Upgrade"]     = 10;
						Action.PropTargets["Upgrade"] = hand.Tool.props;
					} else
					{
						hand.Drop();
					}
				}
			}
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC FIDGET
		// ────────────────────────────────────────────────────────────────────────────
		public void SvFidget()
		{
			if ( LB["Head"].IsAndroid ) return;
			if ( xxx.rr( 1, 200 ) < Mojo.Feelings["Bored"] )
			{
				if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[scan]: Fidget()");
				float itemDist;

				List<PhysicalBehaviour> Toys = new List<PhysicalBehaviour>();

				foreach ( PhysicalBehaviour pb in ScannedThings )
				{

					if (!pb || !NpcGlobal.ToyNames.Contains(pb.name)) continue;
					if ( pb.TryGetComponent<JukeboxBehaviour>( out JukeboxBehaviour jb ) )
					{
						if ( jb.audioSource.isPlaying ) continue;
					}
					if (Memory.NoFidget.Contains(pb.GetHashCode())) continue;
					Toys.Add(pb);
				}

				if ( Toys.Count > 0 )
				{
					Action.ItemTargets["Fidget"] = Toys.PickRandom();
					itemDist = (Head.position -Action.ItemTargets["Fidget"].transform.position).sqrMagnitude;

					Action.Weights["Fidget"] = Mojo.Feelings["Bored"] / 50;

					float dangerDistance    = float.MaxValue;
					float tempdist;
					float enemyThreatLevel = 0;

					foreach(NpcBehaviour npcX in ScannedNpc)
					{
						if (!npcX || !npcX.PBO) continue;

						if (npcX.IsAiming)
						{
							tempdist = (Head.position - npcX.Head.position).sqrMagnitude;
							if (tempdist < dangerDistance)
							{
								dangerDistance      = tempdist;
								enemyThreatLevel    = npcX.ThreatLevel;
							}
						}
					}

					if (dangerDistance < itemDist) Action.Weights["Fidget"] -= (enemyThreatLevel - ThreatLevel);

				}
			}
		}
		

		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC KARATE COMBAT
		// ────────────────────────────────────────────────────────────────────────────
		public void SvKarate()
		{
			if (NoFight) return;
			if (OnFeet()) 
			{ 
				float score		= 0;
				float highScore = 0;

				foreach ( NpcBehaviour npc in ScannedNpc )
				{
					if (!npc || !npc.PBO || npc.NoFight) continue;

					if (MyGroup.Contains(npc)) continue;

					if (Memory.Opinion(npc.NpcId) <= 5) continue;

					score = 7;

					if (npc.TeamId > 0 && npc.TeamId == TeamId) continue;

					float distance = Mathf.Abs(Head.position.x - npc.Head.position.x);

					score -= distance;

					if (score > 0)
					{
						if (CanAttackActions.Contains(npc.Action.CurrentAction) )
						{
							if (xxx.rr(1,100) > Mojo.Traits["Brave"]) continue;
							if (xxx.rr(1,120) > Mojo.Feelings["Angry"] + Mojo.Traits["Mean"]) continue;

							score += (Mojo.Feelings["Angry"] / 100);

							score += Memory.Opinion(npc.NpcId);

							if (score > highScore) { 
								Action.Weights["Karate"]	= score;
								Action.NPCTargets["Karate"] = npc;
								highScore = score;
							}
						}
					}
				}
			}
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC CHECK TROLL
		// ────────────────────────────────────────────────────────────────────────────
		public void SvCheckTroll()
		{
			if ( LB["Head"].IsAndroid ) return;
			if (NoFight) return;
			if (OnFeet() && HurtLevel < 7) 
			{ 
				if (FH.IsHolding && BH.IsHolding && FH.Tool.props == BH.Tool.props) return;
				float bestScore = 0;
				foreach ( NpcBehaviour npc in ScannedNpc )
				{
					if (!npc || !npc.PBO ) continue;

					if (MyGroup.Contains(npc)) continue;

					if (npc.TeamId > 0 && npc.TeamId == TeamId) continue;

					float score = 0;
				
					float distance = Mathf.Abs(Head.position.x - npc.Head.position.x);

					if (distance < 3f && npc.FacingToward(this))
					{ 
						score += 1;

						score += Math.Abs(Memory.Opinion(npc.NpcId));

						if (ThreatLevel < npc.ThreatLevel) score -= 1;
						if (npc.HasGun) score -= 1;
						if (HasGun) score += 1;

						if (score > bestScore)
						{
							bestScore = score;
							Action.NPCTargets ["Troll"] = npc;
						}

					}
				}
				
				if (bestScore > 0 && xxx.rr(1,10) > bestScore)
				{
					Action.Weights["Troll"] = bestScore * 2;
				}
			}
		}

		
		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC CHECK WITNESS
		// ────────────────────────────────────────────────────────────────────────────
		public void SvCheckWitness()
		{
			if ( LB["Head"].IsAndroid ) return;
			float bestScore = 0;
			bool hated = false;
			foreach ( NpcBehaviour npc in ScannedNpc )
			{
				if (Memory.LastContact && npc == Memory.LastContact) hated = true;
			}

			foreach ( NpcBehaviour npc in ScannedNpc )
			{
				if (!npc || !npc.PBO ) continue;

				float score = 0;
				
				float distance = Mathf.Abs(Head.position.x - npc.Head.position.x);

				//	Watch a fight
				if (!hated && Mojo.Feelings["Angry"] < 60 && npc.Actions.CurrentAction == "Attack" && Mathf.Abs(npc.Head.position.x - Head.position.x) < 4f)
				{

					if (npc.MyTargets.enemy != this) {
						Action.Weights["WatchFight"]    = Mojo.Feelings["Bored"] * xxx.rr(0.1f, 0.8f);
						Action.NPCTargets["WatchFight"] = npc;
					}
				}

				if (distance < 5f && distance > 1f && npc.Actions.DoingStrike)
				{ 
					//	Dont witness own ass kicking
					//if (npc.MyTargets.enemy && npc.MyTargets.enemy == this) continue;
					score += 1;

					score += Mathf.Abs(Memory.Opinion(npc.NpcId));

					if (score > bestScore)
					{
						bestScore = score;
						Action.NPCTargets ["Witness"] = npc;
					}

				}
			}
				
			if (bestScore > 0 && xxx.rr(1,10) > bestScore)
			{
				Action.Weights["Witness"] = bestScore * 2;
			}
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC SHOOT
		// ────────────────────────────────────────────────────────────────────────────
		public void SvShoot()
		{
			if (NoFight || NoShoot || !HasGun) return;
			float distance           = float.MaxValue;
			float temp	             = 0;
			float threat             = -100;
			Vector3 position         = Head.position;
			NpcBehaviour selectedNpc = (NpcBehaviour)null;

			//	Either by distance or by threat
			int mode = xxx.rr(1,5);

			selectedNpc = null;

			foreach ( NpcBehaviour npc in ScannedNpc )
			{
				if (!npc || !npc.PBO || npc.NoFight || npc.NoShoot) continue;

				if (MyGroup.Contains(npc)) continue;

				if (npc.TeamId > 0 && npc.TeamId == TeamId) continue;

				float dist = float.MaxValue;
				float tmpdist;
				float spooked = 0;
				if (npc.FacingToward(this) && npc.PBO.DesiredWalkingDirection > 1)
				{
					spooked += 3;
					//  npc is walking towards me
					if (npc.HasGun) spooked += 10;
					if (npc.HasKnife) spooked += 5;
					if (npc.HasClub) spooked += 5;
					if (Memory.Opinion(npc.NpcId) > 1) spooked += Memory.Opinion(npc.NpcId);
					if (npc.ThreatLevel > ThreatLevel) spooked += npc.ThreatLevel - ThreatLevel;
					if (HurtLevel > 0) spooked += HurtLevel;

					tmpdist = Mathf.Abs(npc.Head.position.x - Head.position.x);
					if (tmpdist < dist)
					{
						dist = tmpdist;
						Action.NPCTargets["Shoot"] = npc;
					}
				}

				if ( xxx.rr( 1, 20 ) < spooked )
				{
					if (xxx.rr(1,100) < Mojo.Feelings["Angry"] )
					{
						Action.Weights["Shoot"] = 10;
					} else
					{
						Action.Weights["Warn"]    = 10;
						Action.NPCTargets["Warn"] = npc;
					}
				}

				if (Memory.Opinion(npc.NpcId) <= 0) continue;

				if (mode == 1) { 

					temp = (position - npc.Head.transform.position).sqrMagnitude;

					if (temp < distance)
					{
						selectedNpc = npc;
						distance    = temp;
					}

				} else
				{
					if ( npc.ThreatLevel > threat )
					{
						if ( npc.ThreatLevel < -5 )
						{
							if (xxx.rr(1,200) > Mojo.Traits["Mean"] + Mojo.Feelings["Angry"]) continue;
						}


						threat = npc.ThreatLevel;
						selectedNpc = npc;
					}
				}
			}

			if (selectedNpc != null)
			{
				Action.NPCTargets["Shoot"] = selectedNpc;

				Action.Weights["Shoot"] = xxx.rr(1,15);
			}


		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: CHECK FOR THREATS
		// ────────────────────────────────────────────────────────────────────────────
		public bool SvCheckForThreats()
		{
			float distance;

			//float tooManyPeople = ScannedNpc.Count() - Mathf.Clamp(Mojo.Traits["Shy"] / 10, 3, 15);

			//if (tooManyPeople > 0 && xxx.rr(1,tooManyPeople * 2) > 1)
			//{
			//	Action.Weights["Flee"] = xxx.rr(1,3);
			//}

			foreach ( NpcBehaviour npc in ScannedNpc )
			{
				if (!npc || !npc.PBO) continue;

				distance = Mathf.Abs(Head.position.x - npc.Head.position.x);

				if (distance < 1f )
				{
					if (EnhancementMemory)
					{
						int timesAnnoying = Memory.AddNpcStat(npc.NpcId, "Annoying");

						Mojo.Feelings["Angry"] += timesAnnoying;

						if ( EnhancementTroll && xxx.rr( 1, 100 ) > Mojo.Feelings["Angry"] )
						{
							// dont say move if actively engaged
							if (Memory.LastContact != npc) {
								SayRandom("move");	
								if (xxx.rr(1,8) < 4) Memory.LastContact = npc;
							}
						}

						if ( !NoFight && xxx.rr( 1, 100 ) <= Mojo.Feelings["Angry"] )
						{
							Action.Weights["Shove"]      = xxx.rr(3,20);
							Action.NPCTargets["Shove"]	  = npc;
						}

						if (CanWalk) Action.Weights["Wander"] = xxx.rr(1,10);
					} else
					{
						Mojo.Feelings["Angry"]++;
						if ( EnhancementTroll && xxx.rr( 1, 100 ) > Mojo.Feelings["Angry"] )
						{
							SayRandom("move");
							if (xxx.rr(1,10) < 4) Memory.LastContact = npc;
						}
						if ( !NoFight && xxx.rr( 1, 100 ) <= Mojo.Feelings["Angry"] )
						{
							Action.Weights["Shove"]      = xxx.rr(3,20);
							Action.NPCTargets["Shove"]	  = npc;
						}
						if (CanWalk) Action.Weights["Wander"] = xxx.rr(1,3);
					}
				}

				else
				{
					if ( HasGun )
					{
						float dist = float.MaxValue;
						float tmpdist;
						float spooked = 0;
						if (npc.FacingToward(this) && npc.PBO.DesiredWalkingDirection > 1)
						{
							spooked += 3;
							//  npc is walking towards me
							if (npc.HasGun) spooked += 10;
							if (npc.HasKnife) spooked += 5;
							if (npc.HasClub) spooked += 5;
							if (Memory.Opinion(npc.NpcId) > 1) spooked += Memory.Opinion(npc.NpcId);
							if (npc.ThreatLevel > ThreatLevel) spooked += npc.ThreatLevel - ThreatLevel;
							if (HurtLevel > 0) spooked += HurtLevel;

							tmpdist = Mathf.Abs(npc.Head.position.x - Head.position.x);
							if (tmpdist < dist)
							{
								dist = tmpdist;
								Action.NPCTargets["Shoot"] = npc;
							}
						}

						if ( xxx.rr( 1, 100 ) < spooked )
						{
							if (xxx.rr(1,100) < Mojo.Feelings["Angry"] )
							{
								Action.Weights["Shoot"] = 10;
							} else
							{
								Action.Weights["Warn"]    = 10;
								Action.NPCTargets["Warn"] = npc;
							}
						}

					}
					

				}

				//if ( !NoFight && EnhancementKarate && HurtLevel < 3 && IsUpright &&
				//	distance < 2  && 
				//	Facing == npc.Facing && 
				//	npc.Head.position.y < RB["Head"].position.y &&
				//	npc.Head.position.y > RB["LowerBody"].position.y )
				//	{
				//		Action.Weights["Caveman"]      += xxx.rr(1,10);
				//		Action.NPCTargets["Caveman"]	= npc;
				//}
                

				if (npc.HasGun)
				{
					PhysicalBehaviour gun = null;
					if (npc.FH.IsHolding && npc.FH.IsAiming)     gun = npc.FH.Tool.P;
					else if(npc.BH.IsHolding && npc.BH.IsAiming) gun = npc.BH.Tool.P;

					//if ( HurtLevel < 3 && Mathf.Abs( Head.position.x - gun.transform.position.x ) < 2 )
     //               {
     //                   Action.Weights["Tackle"]	+= xxx.rr(1, 15);
     //                   Action.NPCTargets["Tackle"]  = npc;
     //               }

					if ( gun && xxx.AimingTowards( gun.transform, Head ) )
					{
						if ( npc.TeamId > 0 && npc.TeamId == TeamId )
						{
							Action.ItemTargets["TakeCover"] = gun;
							Action.Weights["TakeCover"]    += xxx.rr(1,10);
							return true;
						}
						else
						{
							if (!NoFight && !NoShoot && HasGun) {
								Action.Weights["Fight"]    += xxx.rr(1,10);
								Action.NPCTargets["Fight"]	= npc;
								Action.ItemTargets["Fight"]	= gun;
							}
							else {
								Action.Weights["Survive"]      += xxx.rr(1,10);
								Action.NPCTargets["Survive"]	= npc;
								Action.ItemTargets["Survive"]	= gun;
							}

							return true;
						}
					}

					else if (!gun)
                    {
						//	has gun but not aiming at me
						
						Action.Weights["Caveman"]      += xxx.rr(1,10);
						Action.NPCTargets["Caveman"]	= npc;
						
                    }
				}
			}

			
			return false;
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: CHECK FOR SELF INJURIES
		// ────────────────────────────────────────────────────────────────────────────
		public void SvCheckForInjuries()
		{
			EventInfo fireEvent = new EventInfo()
			{
				EventId = EventIds.Fire,
				Sender  = this,
				PBs     = new List<PhysicalBehaviour>(),
				Expires = Time.time + 30,
			};
			EventInfo medicEvent = new EventInfo()
			{
				EventId  = EventIds.Medic,
				Expires  = Time.time + 10f,
				Sender   = this,
				PBs      = new List<PhysicalBehaviour>(),
				Response = false,
			};

			bool doMedicEvent = false;
			bool doFireEvent  = false;

			foreach ( LimbBehaviour limb in PBO.Limbs )
			{
				if (!limb) continue;
				if ( limb.CirculationBehaviour.BleedingRate > 0 && limb.CirculationBehaviour.BleedingPointCount > 0 )
				{
					doMedicEvent = true;
					medicEvent.PBs.Add(limb.PhysicalBehaviour);
				}

				if ( limb.PhysicalBehaviour.OnFire )
				{
					doFireEvent = true;
					fireEvent.PBs.Add(limb.PhysicalBehaviour);
				}

				if (limb.RoughClassification == LimbBehaviour.BodyPart.Torso && !limb.IsConsideredAlive)
				{
					Death(true);
				}
			}

			if (doMedicEvent) NpcEvents.BroadcastEvent(medicEvent,Head.position,50);
			if (doFireEvent)  {
				NpcEvents.BroadcastEvent(fireEvent,Head.position,150);
				if (HasFireF) {
					Action.Weights["FightFire"] += 100;
					Action.ItemTargets["FightFire"] = LB["MiddleBody"].PhysicalBehaviour;
				}
			}

		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: CHECK FOR EVENTS
		// ────────────────────────────────────────────────────────────────────────────
		public void SvCheckForEvents()
		{
			if ( EventInfoIds.Count > 0 )
			{
				for ( int i = EventInfoIds.Count; --i >= 0; )
				{
					if ( NpcEvents.EventLookup.TryGetValue( EventInfoIds[i], out var eInfo ) )
					{
						EventInfoIds.RemoveAt( i );
						if ( eInfo.EventId == EventIds.Fire )
						{
							PhysicalBehaviour pb = xxx.GetClosestItemFromList(eInfo.PBs, Head.position);

							if ( (FH.IsHolding && FH.Tool && FH.Tool.props.canFightFire) || (BH.IsHolding && BH.Tool && BH.Tool.props.canFightFire) )
							{
								Action.Weights["FightFire"] += 100;
								MyTargets.item	= pb;
								DecideWhatToDo(true);


							} else
							{
								Action.Weights["WatchEvent"] = 2f;
								Action.ItemTargets["WatchEvent"] = pb;
							}
						}

						if ( eInfo.EventId == EventIds.Jukebox )
						{
							Action.Weights["Disco"] = 2f;
							
						}

						if ( false && eInfo.EventId == EventIds.Medic )
						{ 
							if (eInfo.Sender && eInfo.Sender.PBO && eInfo.Sender.PBO.IsAlive()) 
							{
								Action.Weights["Medic"] = 1;
								
								//if (MyGroup.Contains(eInfo.NPCs[0])) Action.Weights["Medic"] += 5;
								//else if (TeamId == eInfo.NPCs[0].TeamId) Action.Weights["Medic"] += 1;

								Action.Weights["Medic"] += eInfo.PBs.Count;

								CurrentEventInfo = eInfo;

								//eInfo.Response = true;

								DecideWhatToDo(true);
							}
						}
					}
				}
			}
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: CHECK ENVIRONMENT
		// ────────────────────────────────────────────────────────────────────────────
		public void SvCheckEnvironment()
		{
			EventInfo fireEvent = new EventInfo()
			{
				EventId = EventIds.Fire,
				Sender  = this,
				PBs     = new List<PhysicalBehaviour>(),
				Expires = Time.time + 30,
			};

			Vector2 closest = Vector2.zero;
			float dist      = float.MaxValue;
			float tempdist  = 0;
			int ItemsOnFire = 0;
			//	Check for items on fire
			foreach ( PhysicalBehaviour pb in ScannedPhys )
			{
				if ( pb.OnFire )
				{
					ItemsOnFire++;
					fireEvent.PBs.Add(pb);
					tempdist = (Head.position - pb.transform.position).sqrMagnitude;
					if (tempdist < dist)
					{
						dist           = tempdist;
						closest        = pb.transform.position;
						Action.ItemTargets["FightFire"] = pb;
					}
				}
			}

			if ( ItemsOnFire > 0 )
			{
				NpcEvents.BroadcastEvent(fireEvent,closest,50f);
				if (HasFireF)
				{
					Action.Weights["FightFire"] += 100;
				}
			}
		}

		public void CheckSigns()
		{
			bool bhealing = InHealZone;
			
			NoShoot = NoFight = NoEntry = InHealZone = false;
			
			if (NpcGadget.AllSigns.Count == 0 || MyColliders.Length == 0) return;
			
			List<Collider2D> colResults = new List<Collider2D>();
			//filter.NoFilter();

			
			
			foreach ( NpcGadget sign in NpcGadget.AllSigns )
			{
				if ( sign.box != null )
				{
					sign.box.OverlapCollider(filter, colResults);
					if (colResults.Intersect(MyColliders).Any()) { 
						if (sign.Gadget == Gadgets.NoGunSign)        NoShoot    = true;
						else if (sign.Gadget == Gadgets.NoFightSign) NoFight    = true;
						else if (sign.Gadget == Gadgets.NoEntrySign) NoEntry    = true;
						else if (sign.Gadget == Gadgets.HealingSign) InHealZone = true;
					}
				}
			}

			if ( InHealZone )
			{
				bool xtraHeal;
				foreach ( LimbBehaviour limb in PBO.Limbs )
				{
					xtraHeal = true;
					if (limb.BruiseCount > 0) {limb.BruiseCount--; xtraHeal = false;}
					if (limb.CirculationBehaviour.StabWoundCount > 0) {limb.CirculationBehaviour.StabWoundCount--; xtraHeal = false;}
					if (limb.CirculationBehaviour.GunshotWoundCount > 0) {limb.CirculationBehaviour.GunshotWoundCount--; }
					if (limb.Health < limb.InitialHealth) {limb.Health += 0.01f; xtraHeal = false;}
					if (limb.PhysicalBehaviour.BurnProgress > 0) {limb.PhysicalBehaviour.BurnProgress -= 0.01f; xtraHeal = false;}
					if (limb.SkinMaterialHandler.AcidProgress > 0) {limb.SkinMaterialHandler.AcidProgress -= 0.01f; xtraHeal = false;}
					if (limb.SkinMaterialHandler.RottenProgress > 0) {limb.SkinMaterialHandler.RottenProgress -= 0.01f; xtraHeal = false;}
					if (xtraHeal && limb.Broken) limb.HealBone();
					if (limb.InitialHealth - limb.Health > 0.5f)  limb.CirculationBehaviour.HealBleeding();
				}
			}
		}


		public void Shot(Shot shot)
		{

			if (!PBO || !PBO.IsAlive()) return;
			if (Time.time - LastTimeShot < 5) return;

			Mojo.Feel("Fear", 2f);
			Mojo.Feel("Angry", 1f);
			Mojo.Feel("Bored", -20f);

			if (HasGun)
			{
				if (FH.IsHolding) FH.FireAtWill = true;
				if (BH.IsHolding) BH.FireAtWill = true;
			}

			Action.ClearAction();
			StartCoroutine(IGuessShooter(shot.normal));
			LastTimeShot = Time.time;

		
		}

		


		public bool CollideNPC( NpcBehaviour otherNPC )
		{
			if (!CanGhost || !otherNPC.CanGhost) return false;

			//if(MyFights.Contains(otherNPC) || !otherNPC.AllowNoCollisions(this)) return false;

			xxx.ToggleCollisions(transform, otherNPC.transform,false, true);

			return true;

		}

		public bool CollidePB( PhysicalBehaviour item )
		{
			if ( item.beingHeldByGripper ) return false;

			//	Examine item

			return false;

		}


		public bool AllowNoCollisions( NpcBehaviour otherNPC )
		{
			if(MyFights.Contains(otherNPC)) return false;

			return true;
		}


		public float IntervalTime = 0f;
		public float LastInterval = 0f;


		public bool CheckInterval( float reqInterval )
		{
			if (Time.time < IntervalTime) return false;

			if ( LastInterval != reqInterval )
			{
				LastInterval = reqInterval;
				IntervalTime = Time.time + reqInterval;
				return false;
			}

			IntervalTime = Time.time + reqInterval;
			return true;

		}

		
		public bool CanThink()
		{
			return PBO && PBO.Consciousness > 0.8f && PBO.ShockLevel < 0.2f && PBO.PainLevel < 0.5f && !PBO.Braindead;
		}

		
		public void Reborn()
		{
			foreach ( LimbBehaviour limb in PBO.Limbs )
			{
				if(limb.IsDismembered)
				{
					foreach( LimbBehaviour limb2 in PBO.Limbs) limb2.Crush();
					GameObject.Destroy(this.gameObject);
					return;
				}
			}

			HasKnife       = false;
			HasClub        = false;
			HasExplosive   = false;
			HasFireF	   = false;
			PBO.Braindead  = false;
			Active         = true;

			if (RebirthLight && RebirthLight.gameObject)  GameObject.Destroy(RebirthLight.gameObject);

			FH.Init();
			BH.Init();
			StopAllCoroutines();
			Action.StopAllCoroutines();

			Transform[] transforms = Head.gameObject.GetComponentsInChildren<Transform>();

			foreach ( Transform t in transforms )
			{
				if (t.name.Contains("ModLight"))
				{
					GameObject.Destroy((UnityEngine.Object)t.gameObject);
				}
			}



			StartCoroutine(CheckStatus());
			StartCoroutine(IFeelings());
			Action.CurrentAction = "Thinking";

			//StartCoroutine( ICheckJoints());
			RunLimbs(LimbFireProof, false);

			RunRigids(RigidMass,-1f);
			
			if (ShowStats)
			{
				HoverStats = Head.gameObject.GetOrAddComponent<NpcHoverStats>();
				NpcHoverStats.Show(this);
			}
		}


		public NpcRebirther FindRebirther()
		{
			NpcRebirther[] Rebirthers = UnityEngine.Object.FindObjectsOfType<NpcRebirther>();
			
			xxx.Shuffle<NpcRebirther>(Rebirthers);

			NpcRebirther myRebirth = null;

			if (Rebirthers.Length > 0) { 
				foreach ( NpcRebirther rebirther in Rebirthers )
				{
					if (!rebirther) continue;
					if (rebirther.RebirtherActive && rebirther.TeamId == TeamId) {
						myRebirth  = rebirther;
						break;
					}
				}
			}

			return myRebirth;
		}

		public IEnumerator IRebirth()
		{
			yield return new WaitForSeconds(xxx.rr(1,10));

			RebirthLight = new LightSprite();

			RebirthLight = ModAPI.CreateLight(transform, Color.white);
			RebirthLight.transform.SetParent(Head);
			RebirthLight.transform.position = Head.position;


			RunLimbs(LimbGhost,true);
			RunLimbs(LimbCure);
			RunLimbs(LimbImmune,true);

			float force = xxx.rr(1,10);

			foreach ( Rigidbody2D r in RB.Values )
			{
				if (r) r.AddForce(Vector2.up * force * xxx.rr(1f,1.1f));
			}

			for (int i = 1; ++i < 50;) 
			{
				RunLimbs(LimbAlphaDelta,-0.034f);
				yield return new WaitForFixedUpdate();
				yield return new WaitForSeconds(0.05f);
			}

			NpcRebirther myRebirther = null;
			
			for (; ; )
			{
				myRebirther = FindRebirther();

				if (myRebirther == null)
				{
					if (RebirthLight && RebirthLight.gameObject) GameObject.Destroy(RebirthLight.gameObject);

					RunLimbs(LimbAlphaFull,true);
					RunLimbs(LimbGhost, false);
					RunLimbs(LimbImmune, false);
					RunLimbs(LimbIchi);

					if (PBO) {
						NpcDeath reaper = PBO.gameObject.AddComponent<NpcDeath>();
						reaper.Config(this);
					}
					yield break;
				}

				float rebirtherPower = myRebirther.PB.charge;
				Vector3 dist;
				float sqr;
				float timer;
				Color c;

				if ( xxx.rr( 1, 100 ) < Mathf.Clamp( rebirtherPower, 5f, 30f ))
				{
					while (PBO && myRebirther && myRebirther.RebirtherActive)
					{
						dist = (myRebirther.transform.position - Head.position);
						sqr  = dist.sqrMagnitude;
						if (RB["UpperBody"])  RB["UpperBody"].AddForce(dist.normalized * Mathf.Clamp(rebirtherPower * 2, 5, 30) * Time.fixedDeltaTime);
						if (RB["LowerBody"])  RB["LowerBody"].AddForce(dist.normalized * Mathf.Clamp(rebirtherPower * 2, 5, 30) * Time.fixedDeltaTime);
						if (RB["MiddleBody"]) RB["MiddleBody"].AddForce(dist.normalized * Mathf.Clamp(rebirtherPower * 2, 5, 30) * Time.fixedDeltaTime);
						if ( sqr < 2000f )
						{
							//Lightsx[0].Highlights[0].Intensity = 1f;
							//Lightsx[0].transform.localScale = new Vector3(sqr/200f, 0.5f);
						}
						if ( sqr < 300f && RB["Head"].velocity.sqrMagnitude > 10) RB["UpperBody"].velocity *= 0.5f;
						if ( sqr < 5f )
						{
							if (RB["UpperBody"])	RB["UpperBody"].velocity *= 0.1f;
						}
						if (sqr < 3f) {
							if (RB["MiddleBody"])  RB["MiddleBody"].velocity *= 0f; 
							myRebirther.StartCoroutine(myRebirther.IGiveRebirth(this));

							yield break;
						}

						yield return new WaitForFixedUpdate();

						
					}
				}
				else
				{
					timer = Time.time + xxx.rr(3f,10f);

					RB["UpperBody"].AddForce( UnityEngine.Random.insideUnitCircle * xxx.rr( 1, Mathf.Clamp(rebirtherPower / 3, 0.1f, 10f) ), ForceMode2D.Impulse );

					while ( PBO && myRebirther && myRebirther.RebirtherActive && Time.time < timer )
					{
						c = Color.HSVToRGB( Mathf.PingPong((Time.time + myRebirther.MiscFloats[0]) * 0.5f, 1), 1, 1);
						RebirthLight.Color = c;
						yield return new WaitForSeconds(0.01f);
					}
				}

			}
			
			
		}

		float glitchfloat = 1;
		int glitchCount   = 0;
		public IEnumerator IGlitch()
		{
			int x = xxx.rr(0,10);
			Color[] colors =
			{
				Color.gray, Color.yellow, Color.blue, Color.green, Color.red, Color.grey, Color.gray, Color.grey, Color.gray, Color.grey
			};

			for (; ; )
			{
				yield return new WaitForSeconds(xxx.rr(1,10) * glitchfloat);
				if (++glitchCount > 50) break;
				glitchfloat *= 0.9f;
				LimbBehaviour limb = PBO.Limbs.PickRandom();
				ModAPI.CreateParticleEffect("Flash", limb.transform.position);
				limb.PhysicalBehaviour.rigidbody.AddForce(UnityEngine.Random.insideUnitCircle * xxx.rr(1,10), ForceMode2D.Impulse);
				ModAPI.CreateParticleEffect("FuseBlown", Head.position);
				LB["Head"].PhysicalBehaviour.spriteRenderer.color = Color.Lerp(LB["Head"].PhysicalBehaviour.spriteRenderer.color, colors[x], xxx.rr(1,10) * Time.fixedDeltaTime);
			}

			// if survived, chance for enhancement reward
			switch ( x )
			{
				case 1:
					Mojo.Stats["Vision"] = xxx.rr(35,70);
					break;

				case 2:
					Mojo.Perks["Heal"]   = 1;
					RunLimbs(LimbRegen, 2f);
					break;

				case 3:
					Mojo.Stats["Aiming"] = xxx.rr(50, 100);
					break;

				case 4:
					Mojo.Stats["Brains"] = xxx.rr(1.0f, 5.0f);
					break;
			}


		}


		public void CalculateThreatLevel()
		{
			Mojo.Feelings["Chicken"] *= 0.5f;
			Mojo.Feel("Tired", 1f);

			_threatLevel = Config.BaseThreatLevel;

			_threatLevel += Mathf.Clamp(10 - HurtLevel, 0, 10);

			_threatLevel += 5f - LB["UpperBody"].BaseStrength;

			MyBlood = 0f;


			foreach (LimbBehaviour limb in PBO.Limbs) MyBlood += limb.CirculationBehaviour.TotalLiquidAmount;

			MyBlood /= PBO.Limbs.Length;


			if (FH.IsHolding && FH.Tool) _threatLevel += FH.Tool.props.ThreatLevel;
			if (BH.IsHolding && BH.Tool) _threatLevel += BH.Tool.props.ThreatLevel;

			_threatLevel *= MyBlood;
			_threatLevel -= Mojo.Feelings["Chicken"];

			if (_threatLevel < 0 && HasGun) _threatLevel = 3;

			if ( LB["Head"].IsAndroid ) _threatLevel *= 10;



			if (!IsUpright) _threatLevel *= 0.9f;
		}


		public IEnumerator IFeelings()
		{
			bool alreadyBlockingCollisions = false;

			for (; ; )
			{
				CheckSigns();

				if ( LB["Foot"] ) LB["Foot"].ImmuneToDamage                   = true;
				if ( LB["FootFront"] ) LB["FootFront"].ImmuneToDamage         = true;
				if ( LB["LowerArm"] ) LB["LowerArm"].ImmuneToDamage           = true;
				if ( LB["LowerArmFront"] ) LB["LowerArmFront"].ImmuneToDamage = true;

				if (!CanGhost)
				{
					if (alreadyBlockingCollisions)
					{
						CanGhost                  = true;
						alreadyBlockingCollisions = false;
					} else
					{
						alreadyBlockingCollisions = true;
					}
				}

				CalculateThreatLevel();
				float[] keys = TimedNpcIgnored.Keys.ToArray();

				//	Clean out expired ignores

				for (int i = keys.Length; --i >= 0;)
				{
					TimedNpcIgnored.Remove(keys[i]);
				}

				//	Clean out expired bGhosts
				if ( BGhost.Count > 0 )
				{
					Transform[] CL = BGhost.Keys.ToArray();
					for (int i = CL.Length; --i >= 0;)
					{
						if ( BGhost[CL[i]] + 2 < Time.time ) BGhost.Remove(CL[i]);
					}
				}

				if (Time.time - NpcEvents.BloodTimer > 1f)
				{
					NpcEvents.CleanBlood();
					NpcEvents.BloodTimer = Time.time;
				}

				if ( BloodyNose > 0 && --BloodyNose == 0 )
				{
					LB["Head"].CirculationBehaviour.HealBleeding();
				}

				yield return new WaitForSeconds(2);
				if (!PBO) GameObject.Destroy(this);
				if (!PBO) GameObject.Destroy(this.gameObject);
			}
		}

		public IEnumerator IGuessShooter(Vector2 direction)
		{
			if (Mathf.Sign(direction.x) == Mathf.Sign(Facing)) {
				yield return new WaitForFixedUpdate();
				yield return new WaitForEndOfFrame();
				Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForEndOfFrame();
			}

			//ScanResultsCount = Physics2D.OverlapBox(ScanStart, ScanStop, 0f, filter, ScanResults);
			ScanTimeExpires = 0f;
			ScanAhead(30);

			NpcBehaviour enemy = null;

			if (MyFights.Count > 0)
			{

				for(int i = MyFights.Count; --i >= 0;)
				{
					if (!MyFights[i]|| !MyFights[i].PBO) continue;
				}
			}

			for ( int i = -1; ++i < ScanResultsCount; )
			{
				if ( ScanResults[i].transform.root.TryGetComponent<NpcBehaviour>( out enemy ) )
				{
					if (MyFights.Contains(enemy) && enemy.IsAiming) { 
						MyTargets.enemy = enemy;
						
						Say(enemy.Config.Name + " shot me!", 3, true);
						
						LastShot = enemy.NpcId;

						if (EnhancementMemory) Memory.AddNpcStat(enemy.NpcId, "ShotMe");

						if (!DefenseActions.Contains(Action.CurrentAction))
						{
							if (Action.PCR != null) Action.StopCoroutine(Action.PCR);
							if (HasGun) Action.PCR = Action.StartCoroutine(Action.IActionSurvive());
							else Action.PCR = Action.StartCoroutine(Action.IActionDefend(MyTargets.enemy));
							yield break;
						}
					}
				}
				if ( ScanResults[i].transform.root.TryGetComponent<GripBehaviour>( out GripBehaviour grip ) )
				{
					if ( grip && grip.isHolding )
					{
						if ( xxx.AimingTowards( grip.CurrentlyHolding.transform, Head ) )
						{
							MyTargets.person = grip.GetComponentInParent<PersonBehaviour>();
							if (Action.PCR != null) Action.StopCoroutine(Action.PCR);
							//Action.PCR = Action.StartCoroutine(Action.IActionDefendPerson(MyTargets.person));
							yield break;
						}
					}
				}
			}

			Say("Who shot me?", 2f, true);
			

			yield break;
		}

		public IEnumerator ISafeCollisions( float seconds = 2f )
		{
			RunningSafeCollisions = true;
			CanGhost = false;
			float timer = seconds + Time.time;

			bool feknFlying = false;

			while ( Time.time < timer )
			{
				yield return new WaitForFixedUpdate();
				if ( RB["Head"].velocity.sqrMagnitude > 20 )
				{
					feknFlying = true;
					break;
				}
			}

			if ( feknFlying )
			{
				Collider2D[] buffer = new Collider2D[8];

				int bufferLength;

				ContactFilter2D contactFilter = new ContactFilter2D()
				{
					layerMask    = LayerMask.GetMask("Objects"),
					useLayerMask = true,
				};

				List<LimbBehaviour> limbs = new List<LimbBehaviour>();
				limbs.AddRange(PBO.Limbs);

				Transform MyRoot = Head.root;
				bool allClear    = true;
				
				timer = seconds + Time.time;

				while ( Time.time < timer )
				{
					yield return new WaitForFixedUpdate();
					for (int i=limbs.Count; --i >= 0;)
					{
						//if (!limb.Collider) continue;

						bufferLength = limbs[i].Collider.OverlapCollider(contactFilter, buffer);
						allClear     = true;

						for ( int j = bufferLength; --j >= 0; )
						{
							if ( buffer[j].transform.root == MyRoot ) continue;

							allClear = false;
							break;
						}

						if ( allClear )
						{
							xxx.FixCollisions( limbs[i].transform);
							limbs.RemoveAt(i);
						}
					}
				}
			}



			RunningSafeCollisions = false;
			CanGhost              = true;

		}

		public Opinion CheckInteractions( NpcBehaviour theNpc )
		{
			//theNpc.Config.Type;

			if (!Interactions.ContainsKey(theNpc.NpcId)) Interactions.Add(theNpc.NpcId, new Opinion());
			return Interactions[theNpc.NpcId];
		}


		public NpcBehaviour[] FindLocalNpc( float range = 3.0f )
		{

			List<NpcBehaviour> localNpcs  = new List<NpcBehaviour>();

			Vector2 myV = LB["Head"].transform.position;
			float dist;
			float smallestDist =float.MaxValue;
			foreach (NpcBehaviour npc in UnityEngine.Object.FindObjectsOfType<NpcBehaviour>())
			{
				// skip themself
				if (npc == null) continue;
				if (npc == this) continue;
				if (npc.PBO == null) continue;
				if (!npc.PBO.IsAlive()) continue;


				dist = Mathf.Abs(Vector2.Distance(myV, npc.LB["Head"].transform.position));

				if (dist < smallestDist)
				{
					smallestDist = dist;
					closestNpc   = npc;
				}

				if (dist < range) localNpcs.Add(npc);
			}

			return localNpcs.ToArray();
		}

		public bool IsFacingTheAction() => IsFacingTheAction(Head.position);


		public bool IsFacingTheAction( Vector2 pos )
		{
			if (pos == null) pos = Head.position;

			float myX = pos.x;
			int x1    = 0;
			int x2    = 0;
			int val   = 0;
			foreach ( NpcBehaviour npc in NpcGlobal.AllNPC )
			{

				if (!npc || npc == this) continue;

				val = ( npc.PBO.OverridePoseIndex != (int)PoseState.Rest ) ? 3 : 1;
				if (npc.MyTargets.enemy && npc.MyTargets.enemy == this) val += 4;
				if (npc.Head.position.x > myX) x1 += val;
				else x2 += val;
			}

			if (x1 == x2) return true;
			return FacingToward(myX + ((x1 > x2) ? 1f : -1f));

		}

		public void ResetCollisions( NpcBehaviour otherNpc )
		{
			StartCoroutine(IResetNpcCollisions(otherNpc.PBO.transform));
		}


		public virtual void Selected(object sender, PhysicalBehaviour pb)
		{
			NpcHoverStats.Toggle(this);
		}


		public void Say( string Msg, float Seconds = 3f, bool BypassChip=false )
		{
			if ( LB["Head"].IsAndroid ) return;
			if (!EnhancementTroll && !BypassChip) return;
			if (Head.gameObject.TryGetComponent<NpcChat>( out _) ) return;
			if (Msg == LastThingSaid) return;

			LastThingSaid = Msg;
			NpcChat chat = Head.gameObject.AddComponent<NpcChat>();
			chat.Say(Msg, Seconds, this);
		}

		public void SayRandom( string cat, float Seconds = 3f, bool BypassChip = false )
		{
			if ( LB["Head"].IsAndroid ) return;

			if (!EnhancementTroll && !BypassChip) return;

			//if (LastCatSaid == cat) return;

			if (Time.time < LastSmack) return;
			LastSmack = Time.time + 3f;

			LastCatSaid = cat;

			string Msg = NpcChat.GetRandomSmack(cat);

			NpcChat chat = Head.gameObject.AddComponent<NpcChat>();
			chat.Say(Msg, Seconds, this);
		}


		public PM3 IsHoldPaused(float IeTimeout, bool dual=false)
		{
			PM3 qResp = PM3.Success;

			if (PauseHold) qResp = PM3.Fail;
			if (qResp == PM3.Success && dual)
			{
				if (FH.Tool == null || FH.Tool.P == null) return PM3.Error;
				if ((FH.Tool.R.position.x * Facing) > (RB["Head"].position.x * Facing) - (0.5f * Facing)) qResp = PM3.Fail;
			}

			if (Time.time > IeTimeout) return PM3.Timeout;
			return qResp;
		}


		public IEnumerator IResetNpcCollisions(Transform tr)
		{
			if (!tr) yield break;
			for (;;) { 

				yield return new WaitForSeconds(5f);
				if (MyGroup.Count > 0)
				{
					Transform root = (tr.root) ? tr.root : tr;

					foreach(NpcBehaviour npc in MyGroup) {
						if (!npc || !npc.Head || !npc.Head.transform.root) continue;
						if (npc.Head.transform.root == root) yield break;
					}
				}
				if (!tr || !PBO) yield break;
				if (!xxx.IsColliding(PBO.transform, tr, true)) {
					xxx.ToggleCollisions(PBO.transform, tr, true, true);

					yield break;
				}
			}
		}


		IEnumerator CheckStatus()
		{
			for (;;)
			{
				yield return new WaitForSeconds(1);
				if (!IsAlive)
				{
					if ( PBO && !PBO.TryGetComponent<NpcDeath>( out _ ) )
					{
						NpcDeath reaper = PBO.gameObject.AddComponent<NpcDeath>();
						reaper.PBO = this.PBO;
						reaper.Config(this);
					}
				}
				else
				{
					if (Action.CurrentAction == "Thinking") DecideWhatToDo();
				}

				yield return new WaitForSeconds(1);
			}
		}

		//
		// ─── RUN RIGIDS ────────────────────────────────────────────────────────────────
		//
		public void RunRigids(Action<Rigidbody2D> action)
		{
			foreach (Rigidbody2D rigid in RB.Values) { if (rigid) action(rigid); }
		}

		public void RunRigids<t>(Action<Rigidbody2D, t> action, t option)
		{
			foreach (Rigidbody2D rigid in RB.Values) { if (rigid) action(rigid, option); }
		}

		public void RigidInertia(Rigidbody2D rb, float option) => rb.inertia = (option == -1) ? RigidOriginals[rb.name].inertia : option;
		public void RigidMass(Rigidbody2D rb, float option) => rb.mass = (option == -1) ? LB[rb.gameObject.name].PhysicalBehaviour.InitialMass : option;
		public void RigidAddMass(Rigidbody2D rb, float option) => rb.mass *= option;
		public void RigidDrag(Rigidbody2D rb, float option) => rb.drag = (option == -1) ? RigidOriginals[rb.name].drag : option;
		public void BodyInertiaFix(Rigidbody2D rb) => SetRigidOriginal(rb.name);
		public void RigidReset(Rigidbody2D rb)
		{
			rb.mass           = RigidOriginals[rb.name].mass;
			rb.drag           = RigidOriginals[rb.name].drag;
			rb.inertia        = RigidOriginals[rb.name].inertia;
			rb.angularDrag    = RigidOriginals[rb.name].angularDrag;
			rb.freezeRotation = false;
		}


		public void RigidStop(Rigidbody2D rb)
		{
			rb.velocity        = Vector2.zero;
			rb.angularVelocity = 0f;
		}
		//
		// ─── SET RIGID ORIGINAL ────────────────────────────────────────────────────────────────
		//
		public void SetRigidOriginal(string rigidName, string propName="")
		{
			List<string> props = new List<string>() { "mass", "drag", "inertia" };

			if (propName != "") {
				props.Clear();
				props.Add(propName);
			}

			foreach (string pname in props)
			{
				if (RigidOriginals.TryGetValue(rigidName, out RigidSnapshot rigidOG))
				{
					switch(pname)
					{
						case "mass":
							RB[ rigidName ].mass = rigidOG.mass;
							break;

						case "drag":
							RB[rigidName].drag = rigidOG.drag;
							break;

						case "inertia":
							RB[rigidName].inertia = rigidOG.inertia;
							break;
					}

				}
			}
		}

		public void RunLimbs(Action<LimbBehaviour> action)
		{
			foreach (LimbBehaviour limb in LB.Values) { if (limb.isActiveAndEnabled) action(limb); }
		}

		public void RunLimbs<t>(Action<LimbBehaviour, t> action, t option)
		{
			foreach (LimbBehaviour limb in LB.Values) { if (limb.isActiveAndEnabled) action(limb, option); }
		}

		public void LimbFireProof(LimbBehaviour limb, bool option) {  

			PhysicalProperties propys           = limb.GetComponent<PhysicalBehaviour>().Properties.ShallowClone();
			propys.Flammability                 = option ? 0f : 1f;
			propys.BurningTemperatureThreshold  = float.MaxValue;
			propys.Burnrate                     = 0.00001f;

			limb.PhysicalBehaviour.Extinguish();
			limb.PhysicalBehaviour.Properties          = propys;
			limb.PhysicalBehaviour.SimulateTemperature = !option;
			limb.PhysicalBehaviour.ChargeBurns         = false;
			limb.PhysicalBehaviour.BurnProgress        = 0f;
			limb.DiscomfortingHeatTemperature          = float.MaxValue;
		}

		public void LimbRecover(LimbBehaviour limb)
		{
			limb.HealBone();
			limb.CirculationBehaviour.HealBleeding();

			limb.Health                                 = limb.InitialHealth;

			limb.Numbness                               =
			limb.PhysicalBehaviour.BurnProgress         =
			limb.SkinMaterialHandler.AcidProgress       =
			limb.SkinMaterialHandler.RottenProgress     = 0.0f;

			limb.CirculationBehaviour.IsPump            = limb.CirculationBehaviour.WasInitiallyPumping;
			limb.CirculationBehaviour.BloodFlow         = 1f;

			limb.BruiseCount                            = 
			limb.CirculationBehaviour.StabWoundCount    =
			limb.CirculationBehaviour.GunshotWoundCount = 0;

			limb.CirculationBehaviour.ForceSetAllLiquid(0f);
			limb.CirculationBehaviour.AddLiquid(limb.GetOriginalBloodType(), 1f);
			limb.CirculationBehaviour.AddLiquid(Liquid.GetLiquid("LIFE SERUM"), 0.1f);
		}


		public void LimbRegen(LimbBehaviour limb, float option) {
			limb.RegenerationSpeed      = option;  
			limb.GForcePassoutThreshold = 100;
			limb.GForceDamageThreshold  = 100;
		}

		public void LimbImmune(LimbBehaviour limb, bool option)     => limb.ImmuneToDamage = option;
		public void LimbIchi( LimbBehaviour limb )
		{
			if (xxx.rr(1,5) != 2) return;
			limb.Slice();
			limb.PhysicalBehaviour.rigidbody.AddForce(UnityEngine.Random.insideUnitCircle * 2, ForceMode2D.Impulse);
			ModAPI.CreateParticleEffect("Spark", limb.transform.position);
		}
		public void LimbHeal(LimbBehaviour limb) { limb.BruiseCount = 0; }
		public void LimbGhost (LimbBehaviour limb, bool option) { 
			limb.gameObject.layer = LayerMask.NameToLayer(option ? "Debris" : "Objects"); 
			if (option) limb.PhysicalBehaviour.MakeWeightless();
			else limb.PhysicalBehaviour.MakeWeightful();
			limb.PhysicalBehaviour.Extinguish();
		}
		public void LimbAlphaDelta(LimbBehaviour limb, float option)
		{
			limb.PhysicalBehaviour.spriteRenderer.color += new Color(0,0,0,option);
		}

		public void LimbAlphaFull(LimbBehaviour limb, bool option)
		{
			Color c = limb.PhysicalBehaviour.spriteRenderer.color;
			c.a = option ? 1 : 0;
			limb.PhysicalBehaviour.spriteRenderer.color = c;
		}

		public void LimbStrenthen (LimbBehaviour limb) { 
			if (limb.BaseStrength < 15f) { 
				limb.BaseStrength = Mathf.Min(15f, limb.BaseStrength + 5f); 
				limb.Wince(10f);
			}
		}

		public void LimbCure( LimbBehaviour limb )
		{
			limb.HealBone();
			limb.PhysicalBehaviour.BurnProgress         =
			limb.SkinMaterialHandler.AcidProgress       =
			limb.SkinMaterialHandler.RottenProgress     = 0.0f;
			limb.BruiseCount                            = 
			limb.CirculationBehaviour.StabWoundCount    =
			limb.CirculationBehaviour.GunshotWoundCount = 0;
			limb.Broken                                 = false;

			limb.PhysicalBehaviour.Extinguish();
			limb.PhysicalBehaviour.burnIntensity		= 0.0f;
			limb.PhysicalBehaviour.BurnProgress			= 0f;

			limb.CirculationBehaviour.HealBleeding();
			limb.CirculationBehaviour.IsPump            = limb.CirculationBehaviour.WasInitiallyPumping;
			limb.CirculationBehaviour.BloodFlow         = 1f;
		}

		public void LimbUp(LimbBehaviour limb, float option)
		{
			if (Vector2.Dot( limb.transform.up, Vector2.down ) < 0.1f ) return;
			//Mathf.Abs(limb.transform.eulerAngles.z) 
			//PBO.LinkedPoses[PoseState.Rest].ShouldStumble = false;
			GetUp = true;

			float num = Mathf.DeltaAngle(limb.transform.eulerAngles.z, 0f) * limb.FakeUprightForce * limb.MotorStrength;
			if ( !float.IsNaN( num ) ) 
			{
				if ( limb.IsAndroid ) num *= 9f;
				limb.PhysicalBehaviour.rigidbody.AddTorque( num * 1.2f * limb.Person.ActivePose.UprightForceMultiplier * option );
			}
		}

		public void SavePoses()
		{
			PoseSnapshots.Clear();
			for ( int i = PBO.Poses.Count; --i >= 1; )
			{
				RagdollPose Pose = PBO.Poses[i];

				if ( Pose == null || Pose.Name == null || Pose.AngleDictionary == null || Pose.AngleDictionary.Count == 0) continue;

				//foreach ( KeyValuePair<LimbBehaviour, RagdollPose.LimbPose> pair in PBO.Poses[i].AngleDictionary )
				//{

				//}

				LimbPoseSnapshot LPS = new LimbPoseSnapshot();

				LPS.DLimbs = new Dictionary<string, RagdollPose.LimbPose>();

				foreach ( KeyValuePair<LimbBehaviour, RagdollPose.LimbPose> pair in Pose.AngleDictionary )
				{
					if ( pair.Key.name.Contains( "Arm" ) )
					{
						LPS.DLimbs.Add(pair.Key.name, pair.Value);
					}
				}

				PoseSnapshots.Add(Pose.Name, LPS);
			}

			//foreach ( KeyValuePair<PoseState, RagdollPose> pair in PBO.LinkedPoses )
			//{
			//    PBO.LinkedPoses[pair.Key].ShouldStumble = false;
			//}

		}

		public void ResetPoses()
		{
			ResetPoses(LB["LowerArm"], LB["UpperArm"], LB["LowerArmFront"], LB["UpperArmFront"]);

		}

		public void ResetPoses( params LimbBehaviour[] limbs )
		{
			string poseName;
			for ( int i = PBO.Poses.Count; --i >= 1; )
			{
				poseName = PBO.Poses[i].Name;
				if ( PoseSnapshots.ContainsKey( poseName ) )
				{
					foreach ( LimbBehaviour limb in limbs )
					{
						if ( PBO.Poses[i].AngleDictionary.ContainsKey( limb ) )
							PBO.Poses[i].AngleDictionary[limb] = PoseSnapshots[poseName].DLimbs[limb.name];
					}
				}
			}
		}
		public List<string> JointDoubleCheck = new List<string>();
		public IEnumerator ICheckJoints()
		{
			if (CheckedJoints) yield break;
			CheckedJoints           = true;
			bool FixConnectedAnchor = false;
			yield return new WaitForSeconds(0.5f);
			LimbBehaviour[] EndLimbs = {LB["LowerArm"], LB["LowerArmFront"], LB["Foot"], LB["FootFront"] };
			string[] LimbNames = {"LowerArm", "LowerArmFront", "Foot", "FootFront"};


			foreach( HingeJoint2D joint in LimbJoints ) {

				if (!joint || joint == null || !joint.enabled ) continue;

				if ( ( joint.connectedAnchor - LimbjointStuff[joint.name].connectedAnchor ).sqrMagnitude > 0.01f )
				{
					if (JointDoubleCheck.Contains(joint.name)) { 
						FixConnectedAnchor = true;
						break;
					}
					else JointDoubleCheck.Add(joint.name);
				}
				
			}

			FixConnectedAnchor = true;

			if ( FixConnectedAnchor )
			{
				Vector3 diff;
				float maxDist = 1f;

				int infiniteLoop = 0;


				while ( maxDist >= 0.01f )
				{
					if (++infiniteLoop > 4) yield break;
					maxDist = 0;
					for ( int i = LimbJoints.Length; --i >= 0; )
					{
						if ( !LimbJoints[i] || LimbJoints[i] == null ) continue;
						diff = LimbjointStuff[LimbJoints[i].name].connectedAnchor - LimbJoints[i].connectedAnchor;

						if ( diff.sqrMagnitude > maxDist ) maxDist = diff.sqrMagnitude;

						if ( LimbNames.Contains( LimbJoints[i].name ) )
						{
							//LimbJoints[i].useLimits = false;
							LimbJoints[i].enabled   = false;

							LimbJoints[i].attachedRigidbody.transform.Translate( diff );

							LimbJoints[i].connectedAnchor = LimbjointStuff[LimbJoints[i].name].connectedAnchor;

							LimbJoints[i].enabled = true;
						}
					}
					
					yield return new WaitForFixedUpdate();

					for ( int i = LimbJoints.Length; --i >= 0; )
					{
						//LimbJoints[i].useLimits = false;
					}

				}

			}


			CheckedJoints = false;
		}

		public bool CheckedJoints = false;

		public void ResetLimbs()
		{
			if (Facing != LimbFacing) return;
			string[] LimbOrder = new string[]
			{
				"MiddleBody",
				"LowerBody",
				"UpperBody",
				"Head",
				"UpperArm",
				"UpperArmFront",
				"LowerArm",
				"LowerArmFront",
				"UpperLeg",
				"UpperLegFront",
				"LowerLeg",
				"LowerLegFront",
				"Foot",
				"FootFront",
			};


			Vector3 pos;
			Transform mbod = LB["MiddleBody"].transform;

			for( int i=0; ++i < 5;) { 

				foreach ( string ln in LimbOrder )
				{
					if (!LimbPositions.ContainsKey(ln)) continue;
					LB[ln].transform.rotation = LimbPositions[ln].rotation;

					pos = LimbPositions[ln].position;
					if (ln != "MiddleBody") LB[ln].transform.position = mbod.position + pos;
					if (!LimbjointStuff.ContainsKey(ln)) continue;
				}
			}
		}


		public bool OnFeet()
		{
			if (!PBO.IsTouchingFloor) return false;

			foreach ( LimbBehaviour limb in PBO.Limbs )
			{
				if (limb.name.Contains("Foot")) continue;
				if (limb.IsOnFloor) return false;
			}

			return true;
		}


		void Setup()
		{
			if ( isSetup )
			{
				StartCoroutine(IGlitch());
				return;
			}

			isSetup		        = true; 


			if ( !Global.main.TryGetComponent<Utilities.FlipUtility>( out _ ) )
			{
				Utilities.FlipUtility Flipper = Global.main.gameObject.GetOrAddComponent<Utilities.FlipUtility>() as Utilities.FlipUtility;
			}

			PBO                 = GetComponent<PersonBehaviour>();
			Rigidbody2D[] RBs   = PBO.transform.GetComponentsInChildren<Rigidbody2D>();
			LimbBehaviour[] LBs = PBO.transform.GetComponentsInChildren<LimbBehaviour>();
			Mojo                = new NpcMojo(this);
			Actions             = gameObject.GetOrAddComponent<NpcActions>();


			Config.SetName(PBO.name);

			Config.KarateVocalsChance = xxx.rr(Config.KarateVocalsRange.Min, Config.KarateVocalsRange.Max);

			TotalWeight = 0f;

			foreach (Rigidbody2D rb in RBs)   {

				RB[rb.name] = rb;
				TotalWeight += rb.mass;

				RigidSnapshot RBOG = new RigidSnapshot()
				{
					drag    = rb.drag,
					inertia = rb.inertia,
					mass    = rb.mass,
				};

				RigidOriginals[rb.name] = RBOG;
			}

			foreach (LimbBehaviour lb in LBs) {

				NpcLimb npcLimb          = lb.gameObject.GetOrAddComponent<NpcLimb>() as NpcLimb;
				npcLimb.NPC              = this;
				npcLimb.LB               = lb;
				npcLimb.LimbName         = lb.name;
				//npcLimb.StartCoroutine(npcLimb.ICheckLodged());

				lb.ShotDamageMultiplier *= 0.01f;	

				LB[lb.name] = lb;

				lb.PhysicalBehaviour.ContextMenuOptions.Buttons.Add(new ContextMenuButton("RenameNPC", "<color=yellow>Rename NPC</color>", "Rename NPC", new UnityAction[1]
				{
					(UnityAction) (() => RenameNPC())
				})
				{
					Condition = (Func<bool>) (() => this.enabled)
				});
			}

			Head		        = LB["Head"].transform;
			NpcId               = PBO.GetHashCode();

			SavePoses();

			if (WalkPose == null) WalkPose = PBO.LinkedPoses[PoseState.Walking];

			FH     = LB["LowerArmFront"].gameObject.GetOrAddComponent<NpcHand>();
			BH     = LB["LowerArm"].gameObject.GetOrAddComponent<NpcHand>();
			Hands  = new NpcHand[]{ FH,BH };
			Action = gameObject.GetOrAddComponent<NpcAction>();
			HeadL  = LB["Head"];

			filter.NoFilter();

			FH.Init();
			BH.Init();

			CustomSetup(); 

			MyWidth      = transform.root.GetComponentInChildren<SpriteRenderer>().bounds.size.x;
			MyHeight     = transform.root.GetComponentInChildren<SpriteRenderer>().bounds.size.y;
			CommonPoses  = NpcPose.SetCommonPoses(this);
			Goals        = new NpcGoals(this);

			NpcEvents.Init();
			NpcEvents.Subscribe(this);

			audioSource              = Head.gameObject.GetOrAddComponent<AudioSource>();
			audioSource.spread       = 10f;
			audioSource.volume       = 1.5f;
			audioSource.minDistance  = 10f;
			audioSource.maxDistance  = 1500f;
			audioSource.spatialBlend = 1f;
			audioSource.dopplerLevel = 0f;
			audioSource.enabled      = true;

			if (Config.AutoStart) Active = true;

			ActivityMessage = new ActivityMessages(this);

			if (ShowStats && GlobalShowStats) { 
				HoverStats = Head.gameObject.GetOrAddComponent<NpcHoverStats>();
				NpcHoverStats.Show(this);
			}

			LimbJoints = PBO.transform.root.GetComponentsInChildren<HingeJoint2D>();

			foreach( HingeJoint2D joint in LimbJoints ) {
				LimbjointStuff[joint.name] = new LimbJointInfo()
				{
					joint            = joint,
					anchor           = joint.anchor,
					connectedAnchor  = joint.connectedAnchor,
					jointEnabled	 = joint.enabled,
					jointAngleLimits = joint.limits,
				};
			}

			Memory = new NpcMemory(this);

			Invoke("Action.KickRecover", 3f);

			Transform midbod = LB["MiddleBody"].transform;

			foreach ( LimbBehaviour limb in PBO.Limbs )
			{
				LimbPosition lp = new LimbPosition();
				lp.position = limb.transform.position - midbod.position;
				lp.rotation = limb.transform.rotation;
				LimbPositions[limb.name] = lp;

				if (limb.name.Contains("Leg") && limb.InitialHealth == 25) limb.Health = limb.InitialHealth = 100;

				//limb.BreakingThreshold *= 2f;
			}
			
			LimbFacing = Facing;

			List<Collider2D> colList = new List<Collider2D>();

			foreach(Collider2D col1 in Head.root.GetComponentsInChildren<Collider2D>()) {
				if (col1 == null) continue;
				if (!(bool)(UnityEngine.Object) col1) continue;
				colList.Add(col1);
			}

			MyColliders = colList.ToArray();

			StartCoroutine(CheckStatus());
			StartCoroutine(IFeelings());

			//RunLimbs(LimbRegen, 2f);

			float[] cc = new float[3];
			float ct = 0;
			while (true)
			{
				ct = 0f;
				for ( int i = 0; i < 3; ++i )
				{
					cc[i] = xxx.rr(0.0f, 1.0f);
					ct   += cc[i];
				}
				if (ct > 1.0f) break;
			}

			ChatColor = new Color(cc[0], cc[1], cc[2]);
		}

		public Dictionary<string, LimbJointInfo> LimbjointStuff = new Dictionary<string, LimbJointInfo>();
		public Dictionary<string, LimbPosition> LimbPositions = new Dictionary<string, LimbPosition>();
		public float LimbFacing;
		public struct LimbJointInfo
		{
			public HingeJoint2D joint;
			public Vector2 anchor;
			public Vector2 connectedAnchor;
			public bool jointEnabled;
			public JointAngleLimits2D jointAngleLimits;
		}

		public struct LimbPosition
		{
			public Quaternion rotation;
			public Vector3 position;
		}

		void RenameNPC()
		{
			DialogBox dialog = null;
			ShowStats = false;

			dialog = DialogBoxManager.TextEntry(
				@"Rename this NPC",
				Config.Name,
				new DialogButton("Save Name", true, new UnityAction[1] { (UnityAction)(() => ApplyName(dialog)) }),
				new DialogButton("Cancel", true, (UnityAction[])Array.Empty<UnityAction>()));

			void ApplyName(DialogBox d)
			{
				Config.Name = d.EnteredText;
				ShowStats   = true;
			}
		}


		public void Death(bool noRebirth=false)
		{
			//IsDead = true;
			if (!noRebirth) xxx.CheckRebirthers();

			if (HoverStats) {
				HoverStats.StopAllCoroutines();
				HoverStats.HideStats();
			}

			if (FH.IsHolding && FH.Tool && FH.Tool.P)
			{
				FH.Drop();
				FH.Tool.P.MakeWeightful();
				FH.Tool.Dropped();
				if (FH.GB) FH.GB.DropObject();
			}
			if (BH.IsHolding && BH.Tool && BH.Tool.P)
			{
				BH.Drop();
				BH.Tool.P.MakeWeightful();
				BH.Tool.Dropped();
				if (BH.GB) BH.GB.DropObject();
			}

			if (FH.GrabJoint)  {
				FH.GrabJoint.enabled = false;
				UnityEngine.Object.DestroyImmediate((UnityEngine.Object)FH.GrabJoint);
			}
			if (BH.GrabJoint)  {
				BH.GrabJoint.enabled = false;
				UnityEngine.Object.DestroyImmediate((UnityEngine.Object)BH.GrabJoint);

			}
			
			RunRigids(RigidMass, -1f);

			LB["LowerArm"].ImmuneToDamage      =
			LB["LowerArmFront"].ImmuneToDamage = 
			LB["UpperArm"].ImmuneToDamage      = 
			LB["UpperArmFront"].ImmuneToDamage = false;

			CanGhost        = true;
			DisableFlip		= false;
			
			FH.ConfigHandForAiming(false);
			BH.ConfigHandForAiming(false);

			


			bool useRebirther = !noRebirth && NpcGlobal.Rebirthers[TeamId];

			foreach(LimbBehaviour limb in PBO.Limbs)
			{
				//if ("HeadFootFrontLowerArmFrontMiddleBody".Contains(limb.name) && !limb.IsDismembered) continue;
				//if (!limb.HasJoint) useRebirther     = false;
				if (limb.IsDismembered) useRebirther = false;
				if (limb.IsAndroid) useRebirther     = false;
			}

			//	Check if rebirther exists
			if ( useRebirther )
			{
				StopAllCoroutines();
				StartCoroutine(IRebirth());
			}
			else
			{

				Active = false;

				if (PBO) PBO.Braindead = true;


				if (PBO) {
					NpcDeath reaper = PBO.gameObject.GetOrAddComponent<NpcDeath>();
					reaper.Config(this);
					GameObject.Destroy(FH);
					GameObject.Destroy(BH);
					GameObject.Destroy(this);
				}

			}
		}

		
	}

}


