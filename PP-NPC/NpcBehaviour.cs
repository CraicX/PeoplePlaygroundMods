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

namespace PPnpc
{
	public class NpcBehaviour : MonoBehaviour, Messages.IStabbed, Messages.IShot
	{


		public bool DisableFlip = false;
		public NpcConfig Config = new NpcConfig();

		private bool _haltAction = false;

		public bool ShowingStats = false;

		public NpcHoverStats HoverStats;

		public bool HaltAction
		{
			get { return _haltAction; }
			set { _haltAction = value; }
		}
		
		public PersonBehaviour PBO;
		public int NpcId;

		public Transform Head;
		public LimbBehaviour HeadL;

		public NpcBehaviour closestNpc;
		public NpcBehaviour NpcEnemy;
		public NpcMojo Mojo;

		public NpcGoals Goals;

		public NpcEnhancements Enhancements;
		public List<Transform> NoGhost = new List<Transform>();

		

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

		public List<NpcBehaviour> ScannedNpcIgnored           = new List<NpcBehaviour>();
		public List<PersonBehaviour> ScannedPeopleIgnored     = new List<PersonBehaviour>();
		public List<Props> ScannedPropsIgnored                = new List<Props>();
		public List<PhysicalBehaviour> ScannedThingsIgnored   = new List<PhysicalBehaviour>();
		
		//
		//  -------------------------
		//

		public AimStyles CurrentAimStyle = AimStyles.Standard;
		public NpcPose MyNpcPose;

		public Dictionary<string, NpcPose> CommonPoses = new Dictionary<string, NpcPose>();

		public bool HasKnife       = false;
		public bool HasClub        = false;
		public bool HasGun         = false;
		public bool HasExplosive   = false;
		public bool HasFireF	   = false;

		public float MyBlood       = 0f;
		private float _threatLevel = 1f;

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

		public NpcHand FH;
		public NpcHand BH;
		public NpcHand[] Hands;
		NpcHand hand;
		public NpcHand RandomHand => new NpcHand[]{FH, BH}.PickRandom();

		public float MyHeight = 0f;
		public float MyWidth  = 0f;
		public RectTransform RT;

		float floorChecked = 0f;
		Vector2 _floor;
		public Vector2 Floor() { 
			if (Time.time < floorChecked) return _floor;
			floorChecked = Time.time + 5f;
			_floor = xxx.FindFloor(Head.position);
			return _floor;
		}

		public NpcActions CurrentAct;

		public NpcGlobal.NpcTargets MyTargets;

		private NpcPrimaryActions _primaryAction      = NpcPrimaryActions.Thinking;
		private NpcPrimaryActions _lastPrimaryAction  = NpcPrimaryActions.Thinking;

		private Coroutine PrimaryActionCoroutine;

		public static NpcPrimaryActions[] DefenseActions = {
			NpcPrimaryActions.Defend, 
			NpcPrimaryActions.DefendPerson, 
			NpcPrimaryActions.Retreat, 
			NpcPrimaryActions.TakeCover, 
			NpcPrimaryActions.Survive, 
			NpcPrimaryActions.Fight, 
		};

		private static NpcPrimaryActions[] DontNotify = {
			NpcPrimaryActions.Wait, 
			NpcPrimaryActions.Wander, 
			NpcPrimaryActions.Thinking,
		};

		public ActWeight MyActWeights = new ActWeight(0);


		public float LastTimeShot = 0f;
		

		public NpcPrimaryActions PrimaryAction
		{
			get { return _primaryAction; }
			set { 
				_primaryAction = value;
				//if (NpcGlobal.LastClicked == this.NpcId  && value != NpcPrimaryActions.Wait && value != NpcPrimaryActions.Thinking) Mojo.ShowStats();
				if (ShowingStats && HoverStats) HoverStats.ShowText();
				//if (NpcGlobal.LastClicked == NpcId && !DontNotify.Contains(value) && _primaryAction != value) {
				
				//	if (_lastPrimaryAction != value) {
				//		_primaryAction = _lastPrimaryAction = value;
				//	}

				//}
				_primaryAction = value; 

			}
		}

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
			get { _facing = PBO.transform.localScale.x < 0.0f ? 1f : -1f; return _facing; }
		}

		public bool IsUpright  => (bool)(!LB["LowerBody"].IsOnFloor
												   && !LB["UpperBody"].IsOnFloor
												   && !LB["Head"].IsOnFloor
												   && LB["FootFront"].IsOnFloor
												   && LB["Foot"].IsOnFloor);

		public bool IsFlipped                           => (bool)(PBO.transform.localScale.x < 0.0f);
		public void Flip() {
			if (DisableFlip) return; 
			Utilities.FlipUtility.Flip(LB["Head"].PhysicalBehaviour); 
			ScanTimeExpires = 0;
		}

		public bool IsAlive			                    => HeadL && HeadL.IsConsideredAlive;
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
		public NpcHand GetAltHand( NpcHand hand )       => (hand == FH) ? BH : FH;

		public virtual void CustomSetup() {	}
		

		public void Update()
		{
			if (FireProof)
			{
				PBO.PainLevel       = 0.0f;
				PBO.ShockLevel      = 0.0f;
				PBO.AdrenalineLevel = 1.0f;
			}
		}
			

		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: DECISIONS
		// ────────────────────────────────────────────────────────────────────────────
		public void DecideWhatToDo(bool SkipScan=false, bool SkipScoring=false)
		{
			CollisionCounter = 0;
			CancelAction     = false;

			if (!SkipScan) NPCScanView();
			if (!SkipScoring) PrimaryAction = DecideBestAction();

			if ( PrimaryAction == NpcPrimaryActions.Wait )
			{
				if (xxx.rr(1,5) == 3) {
					PrimaryAction = NpcPrimaryActions.Wander;
				}
			}

			if (PrimaryActionCoroutine != null) StopCoroutine(PrimaryActionCoroutine);

			switch ( PrimaryAction )
			{
				case NpcPrimaryActions.Wait:
					PrimaryActionCoroutine = StartCoroutine( IActionWait() );
					break;

				case NpcPrimaryActions.Scavenge:
					PrimaryActionCoroutine = StartCoroutine( IActionScavenge() );
					break;

				case NpcPrimaryActions.Recruit:
					PrimaryActionCoroutine = StartCoroutine( IActionRecruit() );
					break;

				case NpcPrimaryActions.GroupUp:
					PrimaryActionCoroutine = StartCoroutine( IActionGroupUp() );
					break;

				case NpcPrimaryActions.Regroup:
					PrimaryActionCoroutine = StartCoroutine( IActionGroupUp(MyTargets.friend) );
					break;

				case NpcPrimaryActions.Shoot:
					PrimaryActionCoroutine = StartCoroutine( IActionShoot() );
					break;

				case NpcPrimaryActions.Survive:
					PrimaryActionCoroutine = StartCoroutine( IActionSurvive() );
					break;

				case NpcPrimaryActions.TakeCover:
					PrimaryActionCoroutine = StartCoroutine( IActionTakeCover() );
					break;

				case NpcPrimaryActions.Defend:
					PrimaryActionCoroutine = StartCoroutine( IActionShoot(true) );
					break;

				case NpcPrimaryActions.DefendPerson:
					PrimaryActionCoroutine = StartCoroutine( IActionDefendPerson() );
					break;

				case NpcPrimaryActions.FightFire:
					PrimaryActionCoroutine = StartCoroutine( IActionFightFire() );
					break;

				case NpcPrimaryActions.Wander:
					PrimaryActionCoroutine = StartCoroutine( IActionWander() );
					break;

				case NpcPrimaryActions.WatchEvent:
					PrimaryActionCoroutine = StartCoroutine( IActionWatchEvent() );
					break;

				case NpcPrimaryActions.Medic:
					PrimaryActionCoroutine = StartCoroutine( IActionMedic() );
					break;

				case NpcPrimaryActions.Fidget:
					PrimaryActionCoroutine = StartCoroutine( IActionFidget() );
					break;

				case NpcPrimaryActions.Disco:
					PrimaryActionCoroutine = StartCoroutine( IActionDisco() );
					break;
			}
			
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: N P C   S C A N   A R E A : :  :   :    :     :        :          :
		// ────────────────────────────────────────────────────────────────────────────
		public void NPCScanView()
		{
			if (!PBO) return;
			ScanAhead(Enhancements.Vision / 2);


			//NpcGlobal.RescanMap();
			MyActWeights.reset();
			PBO.OverridePoseIndex = -1;

			
			if ( ScannedNpc.Count + ScannedPeople.Count + ScannedProps.Count == 0 )
			{
				//	This direction is boring AF, turn around
				//if (ScannedWall) Flip();
			}
			

			if (Goals.Scavenge) SvScavenge();
			if (Goals.Recruit) SvRecruit();
			if (true || Goals.Shoot) SvShoot();

			//SvGroupUp();
			//SvAttack();
			SvCheckForThreats();
			SvCheckForInjuries();
			SvCheckForEvents();
			SvCheckEnvironment();
			//SvCheckRandomScan();

			if (Mojo.Feelings["Bored"] > 50) SvFidget();

			NpcPrimaryActions tempAction = DecideBestAction();

			PrimaryAction = tempAction;

		}

		public void ScanAhead( float distance=10f )
		{
			if (Time.time > ScanTimeExpires) {
				
				ScanTimeExpires = Time.time + ScanInterval;
				
				ScanStart        = RB["MiddleBody"].position + (Vector2.right * ((distance/1.9f) * -Facing));
				ScanStop		 = new Vector2(distance, 3.5f);
				
				ScanResultsCount = Physics2D.OverlapBox(ScanStart, ScanStop, 0f, filter, ScanResults);

				ScannedNpc.Clear();
				ScannedPeople.Clear();
				ScannedProps.Clear();
				ScannedThings.Clear();

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

				for(int i = -1; ++i < ScanResultsCount;) {

					if (ScanResults[i].gameObject.name == "root") continue;
					if (ScanResults[i].gameObject.name.Contains("wall")) ScannedWall = true;

					GameObject groot = ScanResults[i].transform.root.gameObject;

					if (groot != ScanResults[i].gameObject) {

						//  Check for NPC
						if ( groot.TryGetComponent<NpcBehaviour>( out NpcBehaviour npc ) )
						{
							if (npc == this) continue;
							ScannedNpc.Add(npc);
						}
						else if ( groot.TryGetComponent<PersonBehaviour>( out PersonBehaviour person ) )
						{
							ScannedPeople.Add(person);
						}
						else if ( ScanResults[i].gameObject.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour phys ) )
						{
							ScannedThings.Add(phys);
						}
					}
					else
					{
						if ( ScanResults[i].gameObject.TryGetComponent<Props>( out Props prop ) )
						{
							if (!ScannedPropsIgnored.Contains(prop)) ScannedProps.Add(prop);
						}
						else 
						if ( ScanResults[i].gameObject.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour phys ) )
						{
							if (xxx.CanHold(phys))
							{
								prop = phys.gameObject.GetOrAddComponent<Props>();
								prop.Init(phys);
								ScannedProps.Add(prop);
							} 
							else
							{
								ScannedThings.Add(phys);
							}
						}
					}
				}
			}
		}

		public NpcPrimaryActions DecideBestAction()
		{
			float currentScore = 0;
			NpcPrimaryActions tempAction      = NpcPrimaryActions.Wait;

			if ( MyActWeights.Shoot > currentScore )
			{
				tempAction    = NpcPrimaryActions.Shoot;
				currentScore  = MyActWeights.Shoot;
			}

			if (MyActWeights.Retreat > currentScore)
			{
				tempAction    = NpcPrimaryActions.Retreat;
				currentScore  = MyActWeights.Retreat;
			}

			if ( MyActWeights.Defend > currentScore )
			{
				tempAction    = NpcPrimaryActions.Fight;
				currentScore  = MyActWeights.Fight;
			}

			if ( MyActWeights.DefendPerson > currentScore )
			{
				tempAction    = NpcPrimaryActions.DefendPerson;
				currentScore  = MyActWeights.DefendPerson;
			}

			if ( MyActWeights.GroupUp > currentScore )
			{
				tempAction    = NpcPrimaryActions.GroupUp;
				currentScore  = MyActWeights.GroupUp;
			}

			if ( MyActWeights.Scavenge > currentScore )
			{
				tempAction    = NpcPrimaryActions.Scavenge;
				currentScore  = MyActWeights.Scavenge;
			}

			if ( MyActWeights.Recruit > currentScore )
			{
				tempAction    = NpcPrimaryActions.Recruit;
				currentScore  = MyActWeights.Recruit;
			}

			if ( MyActWeights.Survive > currentScore )
			{
				tempAction    = NpcPrimaryActions.Survive;
				currentScore  = MyActWeights.Survive;
			}

			if ( MyActWeights.TakeCover > currentScore )
			{
				tempAction    = NpcPrimaryActions.TakeCover;
				currentScore  = MyActWeights.TakeCover;
			}

			if ( MyActWeights.WatchEvent > currentScore )
			{
				tempAction    = NpcPrimaryActions.WatchEvent;
				currentScore  = MyActWeights.WatchEvent;
			}

			if ( MyActWeights.Disco > currentScore )
			{
				tempAction    = NpcPrimaryActions.Disco;
				currentScore  = MyActWeights.Disco;
			}

			if ( MyActWeights.Fidget > currentScore )
			{
				tempAction    = NpcPrimaryActions.Fidget;
				currentScore  = MyActWeights.Fidget;
			}

			if ( MyActWeights.FightFire > 0 )
			{
				tempAction    = NpcPrimaryActions.FightFire;
				currentScore  = MyActWeights.FightFire;
			}

			return tempAction;
		}
		

		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC Scavenge
		// ────────────────────────────────────────────────────────────────────────────
		public void SvScavenge()
		{
			if ( ( !FH.IsHolding || !BH.IsHolding ) && ScannedProps.Count > 0 )
			{
				float baseScore = 0;
				
				if (FH.IsHolding == BH.IsHolding) baseScore += 6;
				else baseScore += 3;

				if (!LB["LowerLeg"].IsCapable || !LB["UpperLeg"].IsCapable) baseScore -= 3;
				if (!LB["LowerLegFront"].IsCapable || !LB["UpperLegFront"].IsCapable) baseScore -= 3;

				if (baseScore <= 0) return;

				bool onlyOneHand = (FH.IsHolding || BH.IsHolding);

				Props selectedProp = (Props)null;
				float distance = float.MaxValue;
				float tempdist = 0;
				Vector3 position = Head.position;

				float dangerDistance    = float.MaxValue;
				
				foreach(NpcBehaviour npcX in ScannedNpc)
                {
					if (!npcX || !npcX.PBO || ScannedNpcIgnored.Contains(npcX)) continue;

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
					if (!prop || !prop.P || ScannedPropsIgnored.Contains(prop) || ScannedThingsIgnored.Contains(prop.P)) continue;
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
					if (distance > dangerDistance) baseScore -= 3f;	// something dangerous on the way
					MyTargets.prop        = selectedProp;
					MyTargets.propDist    = distance;
					MyActWeights.Scavenge = baseScore;
				} 
			}
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC Recruit
		// ────────────────────────────────────────────────────────────────────────────
		public void SvRecruit()
		{
			if ( ScannedPeople.Count > 0 ) 
			{
				float baseScore = 0;
				
				if (FH.IsHolding && FH.Tool && FH.Tool.props && FH.Tool.props.canRecruit) baseScore += 3;
				if (BH.IsHolding && BH.Tool && BH.Tool.props && BH.Tool.props.canRecruit) baseScore += 3;

				if (!LB["LowerLeg"].IsCapable || !LB["UpperLeg"].IsCapable) baseScore -= 3;
				if (!LB["LowerLegFront"].IsCapable || !LB["UpperLegFront"].IsCapable) baseScore -= 3;

				if (baseScore <= 0) return;

				float distance = float.MaxValue;
				float tempdist = 0;
				Vector3 position = Head.position;
				PersonBehaviour selectedPerson = null;

				float dangerDistance    = float.MaxValue;
				
				foreach(NpcBehaviour npcX in ScannedNpc)
                {
					if (!npcX || !npcX.PBO || ScannedNpcIgnored.Contains(npcX)) continue;

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
					if (!peep || ScannedPeopleIgnored.Contains(peep)) continue;

					tempdist = (position - peep.transform.position).sqrMagnitude;

					if (tempdist < distance)
					{
						selectedPerson = peep;
						distance       = tempdist;
					}
				}

				if (selectedPerson) {
					if (distance > dangerDistance) baseScore -= 3f;	// something dangerous on the way
					MyTargets.person      = selectedPerson;
					MyTargets.personDist  = distance;
					MyActWeights.Recruit  = baseScore;
				} 
			}
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC GROUPUP
		// ────────────────────────────────────────────────────────────────────────────
		public void SvFidget()
		{
			if ( xxx.rr( 1, 200 ) < Mojo.Feelings["Bored"] )
            {
				if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[scan]: Fidget()");

				List<PhysicalBehaviour> Toys = new List<PhysicalBehaviour>();

				foreach ( PhysicalBehaviour pb in ScannedThings )
                {
					if (ScannedThingsIgnored.Contains(pb) || !NpcGlobal.ToyNames.Contains(pb.name)) continue;

					Toys.Add(pb);
                }

				if ( Toys.Count > 0 )
                {
					MyTargets.item = Toys.PickRandom();
					MyTargets.itemDist = (Head.position - MyTargets.item.transform.position).sqrMagnitude;

					MyActWeights.Fidget = Mojo.Feelings["Bored"] / 10;

					float dangerDistance    = float.MaxValue;
					float tempdist;
					float enemyThreatLevel = 0;
				
					foreach(NpcBehaviour npcX in ScannedNpc)
					{
						if (!npcX || !npcX.PBO || ScannedNpcIgnored.Contains(npcX)) continue;

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

					if (dangerDistance < MyTargets.itemDist) MyActWeights.Fidget -= (enemyThreatLevel - ThreatLevel);

                }
            }

		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC GROUPUP
		// ────────────────────────────────────────────────────────────────────────────
		public float SvGroupUp()
		{
			if ( MyTargets.friend && !MyGroup.Contains(MyTargets.friend) )
			{
				MyActWeights.GroupUp += 5;

				if (FH.IsHolding || BH.IsHolding) MyActWeights.GroupUp += 5;
				if (MyTargets.friend.FH.IsHolding || MyTargets.friend.BH.IsHolding) MyActWeights.GroupUp += 5;

				if (MyTargets.friend.PrimaryAction == NpcPrimaryActions.Defend) MyActWeights.GroupUp += 10;
				if (MyTargets.friend.PrimaryAction == NpcPrimaryActions.Fight) MyActWeights.GroupUp += 5;
				if (MyTargets.friend.PrimaryAction == NpcPrimaryActions.Retreat) MyActWeights.GroupUp -= 5;

				if (MyTargets.friend.GroupSize > 1) MyActWeights.GroupUp += 2 * MyTargets.friend.GroupSize;
				
				if (MyTargets.enemy)
				{
					if (MyTargets.enemyDist < MyTargets.friendDist) MyActWeights.GroupUp -= (MyTargets.enemy.ThreatLevel * 2);
				}

				if ( MyTargets.person )
				{
					if ( MyTargets.personDist < MyTargets.friendDist )
					{
						float score     = 0;
						float itemCount = 0;
						foreach ( GripBehaviour grip in MyTargets.person.GetComponentsInChildren<GripBehaviour>() )
						{
							if (grip.isHolding)
							{
								++itemCount;
								score += xxx.GetThreatLevelOfItem(grip.CurrentlyHolding);
							}
						}

						score /= itemCount;

						MyActWeights.GroupUp -= score;
					}
				}
			}

			return MyActWeights.GroupUp;
		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: NPC SHOOT
		// ────────────────────────────────────────────────────────────────────────────
		public bool SvShoot()
		{
			float distance           = float.MaxValue;
			float tempdist           = 0;
			Vector3 position         = Head.position;
			NpcBehaviour selectedNpc = (NpcBehaviour)null;

			if (!HasGun) return false;

			foreach ( NpcBehaviour npc in ScannedNpc )
			{
				if (!npc || !npc.PBO) continue;

				if (MyGroup.Contains(npc)) continue;

				if (npc.TeamId > 0 && npc.TeamId == TeamId) continue;

				tempdist = (position - npc.Head.transform.position).sqrMagnitude;

				if (tempdist < distance)
				{
					selectedNpc = npc;
					distance    = tempdist;
				}
			}

			if (selectedNpc != null)
			{
				MyTargets.enemy     = selectedNpc;
				MyTargets.enemyDist = distance;

				
				MyActWeights.Shoot = 5;

				return true;
			}
			
			return false;


			//if ( MyTargets.enemy && MyTargets.enemy.PBO )  
			//{
			//	MyActWeights.Fight = ThreatLevel;
				
			//	float EnemyThreatLevel = MyTargets.enemy.ThreatLevel;
			//	if ( MyTargets.enemy.GroupSize > 1 )
			//	{
			//		foreach ( NpcBehaviour gman in MyTargets.enemy.MyGroup )
			//		{
			//			if (gman) EnemyThreatLevel += gman.ThreatLevel;
			//		}
			//	}

			//	if ( GroupSize > 1 )
			//	{
			//		foreach ( NpcBehaviour gman in MyGroup )
			//		{
			//			if (gman) MyActWeights.Fight += gman.ThreatLevel;
			//		}
			//	}

			//	MyActWeights.Fight -= EnemyThreatLevel;

			//	//if (AcknowledgeThreat = true && (FH.IsHolding || BH.IsHolding)) MyActWeights.Fight += 100;

			//	if ((!FH.IsHolding || !FH.Tool || !FH.Tool.props.canShoot) && (!BH.IsHolding || !BH.Tool || !BH.Tool.props.canShoot)) MyActWeights.Fight = 0;

			//}

		}


		// ────────────────────────────────────────────────────────────────────────────
		//   :::::: CHECK FOR THREATS
		// ────────────────────────────────────────────────────────────────────────────
		public bool SvCheckForThreats()
		{
			foreach ( NpcBehaviour npc in ScannedNpc )
			{
				if (npc.HasGun)
				{
					
					PhysicalBehaviour gun = null;
					if (npc.FH.IsHolding && npc.FH.IsAiming) gun = npc.FH.Tool.P;
					else if(npc.BH.IsHolding && npc.BH.IsAiming) gun = npc.BH.Tool.P;
					

					if ( gun && xxx.AimingTowards( gun.transform, Head, 50 ) )
					{
						if ( npc.TeamId > 0 && npc.TeamId == TeamId )
						{
							MyTargets.item = gun;
							MyActWeights.TakeCover += 10;
							return true;
						}
						else
						{
							if (HasGun) MyActWeights.Defend += 10;
							else MyActWeights.Survive += 10;
							
							MyTargets.item  = gun;
							MyTargets.enemy = npc;
							return true;
						}
					}
				}
			}

			foreach ( PersonBehaviour person in ScannedPeople )
			{
				if (!person) continue;

				foreach ( GripBehaviour grip in person.GetComponentsInChildren<GripBehaviour>() )
				{
					if (!grip.isHolding) continue;

					if ( grip.CurrentlyHolding.TryGetComponent<Props>( out Props prop ) )
					{
						if ( prop.canShoot && xxx.AimingTowards( prop.transform, Head, 20 ) )
						{
							if (HasGun) MyActWeights.DefendPerson += 10;
							else MyActWeights.Survive += 10;
							MyTargets.person = person;
							return true;
						}
					}
				}
			}

			//if ( MyFights.Count > 0 )
			//{
			//	AcknowledgeThreat = false;
				
			//	foreach ( NpcBehaviour npcEnemy in MyFights )
			//	{
			//		if (!npcEnemy  || !npcEnemy.PBO ) continue;

			//		if ( FacingToward( npcEnemy ) )
			//		{
			//			if ( (npcEnemy.FH.IsAiming && npcEnemy.FH.Tool.props.canShoot) || (npcEnemy.BH.IsAiming && npcEnemy.BH.Tool.props.canShoot) ) 
			//			{
			//				if ( FH.IsHolding || BH.IsHolding )
			//				{
			//					MyActWeights.Defend = 10 + (ThreatLevel - npcEnemy.ThreatLevel);
			//				}
			//				else
			//				{
			//					AcknowledgeThreat = true;
			//					MyActWeights.Survive += npcEnemy.ThreatLevel;
			//				}
			//			}
			//		}
			//	}
			//}
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
			}
			
			if (doMedicEvent) NpcEvents.BroadcastEvent(medicEvent,Head.position,50);
			if (doFireEvent)  {
				NpcEvents.BroadcastEvent(fireEvent,Head.position,150);
				if (HasFireF) {
					MyActWeights.FightFire += 100;
					MyTargets.item = LB["MiddleBody"].PhysicalBehaviour;
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
								PrimaryAction	= NpcPrimaryActions.FightFire;
								MyActWeights.FightFire += 100;
								MyTargets.item	= pb;
								DecideWhatToDo(true);


							} else
							{
								MyActWeights.WatchEvent = 2f;
							}
						}

						if ( eInfo.EventId == EventIds.Jukebox )
                        {
							MyActWeights.Disco = 2f;
                        }

						if ( false && eInfo.EventId == EventIds.Medic )
						{ 
							if (eInfo.Sender && eInfo.Sender.PBO && eInfo.Sender.PBO.IsAlive()) 
							{

								MyActWeights.Medic = 1;
								
								//if (MyGroup.Contains(eInfo.NPCs[0])) MyActWeights.Medic += 5;
								//else if (TeamId == eInfo.NPCs[0].TeamId) MyActWeights.Medic += 1;

								MyActWeights.Medic += eInfo.PBs.Count;

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
		//   :::::: CHECK RANDOM SITUATIONS
		// ────────────────────────────────────────────────────────────────────────────
		public void SvCheckRandomScan()
		{
			//ScanAhead();
		
			//for ( int i = -1; ++i < ScanResultsCount; )
			//{
			//	if ( ScanResults[i].transform.root.TryGetComponent<PersonBehaviour>( out PersonBehaviour person ) )
			//	{
			//		if (person == PBO) continue;
			//		foreach ( GripBehaviour grip in person.GetComponentsInChildren<GripBehaviour>() )
			//		{
			//			if (grip.isHolding)
			//			{
			//				//	person is holding someTool
			//				float heldThreat = xxx.GetThreatLevelOfItem(grip.CurrentlyHolding);

			//				if ( heldThreat > 4 )
			//				{
			//					// gun ?
			//					if ( xxx.AimingTowards( grip.CurrentlyHolding.transform, Head ) )
			//					{
			//						if ( grip.CurrentlyHolding.TryGetComponent<NpcBehaviour>( out NpcBehaviour npc ) )
			//						{
			//							if ((!npc.FH.IsAiming || !npc.FH.Tool.props.canShoot) && (!npc.BH.IsAiming || !npc.BH.Tool.props.canShoot)) continue;
			//						}

			//						if (!xxx.GetWeaponBasics(grip.CurrentlyHolding).CanShoot) continue;
			//						if (FH.IsHolding || BH.IsHolding)
			//						{
			//							MyActWeights.DefendPerson += 8;
			//							MyTargets.person = person;
			//						}
			//						else { 
			//							MyActWeights.TakeCover += 1;
			//							MyTargets.item = grip.CurrentlyHolding;
			//						}
			//					}
			//				}
			//			}
			//		}
			//	}
			//	else 
			//	if ( ScanResults[i].transform.root.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour pb ) )
			//	{
			//		int itemsOnFire = 0;
			//		bool okToReport = false;
			//		bool checkIfReported = true;
			//		int EventIdUniq = Time.frameCount;

			//		if ( pb.OnFire ) {
			//			MyActWeights.WatchEvent = 1;
			//			Debug.Log("FOUND FIRE!");
			//			//if (PBO.DesiredWalkingDirection > 0) PBO.DesiredWalkingDirection = -10f;
			//			itemsOnFire++;

			//			if (checkIfReported) {
			//				okToReport      = NpcEvents.CanReportEvent(EventIds.Fire);
			//				checkIfReported = false;
			//			}

			//			if ( (FH.IsHolding && FH.Tool && FH.Tool.props.canFightFire) || (BH.IsHolding && BH.Tool && BH.Tool.props.canFightFire) )
			//			{
			//				MyActWeights.FightFire++;
			//				MyTargets.item	= pb;
			//			}

			//			if (okToReport) NpcEvents.SetEventInfo(EventIdUniq, pb);
			//		} else if (xxx.CanHold(pb) && MyActWeights.Survive > 1f)
			//		{
			//			MyTargets.item = pb;
						
			//			if (PrimaryActionCoroutine != null) StopCoroutine(PrimaryActionCoroutine);
			//			PrimaryActionCoroutine = StartCoroutine( IActionDive(pb.transform.position) );
			//			return;

			//			////	Look for obstacle
			//			//if ( pb.ObjectArea > 3 
			//			//	|| (pb.gameObject.layer != 9 && pb.gameObject.layer != 10) 
			//			//	|| ( pb.gameObject.layer != 10 && pb.TryGetComponent<FreezeBehaviour>(out _) ) )
			//			//                  {
			//			//	if (Mathf.Abs(pb.transform.position.x - Head.position.x) < 1f)  Flip();
			//			//                  }
			//		}
			//	}
			//}
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
			foreach ( PhysicalBehaviour pb in ScannedThings )
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
						MyTargets.item = pb;
					}
				}
			}

			if ( ItemsOnFire > 0 )
			{
				NpcEvents.BroadcastEvent(fireEvent,closest,50f);
				if (HasFireF)
				{
					MyActWeights.FightFire += 100;
				}
			}
		}



		

		public NpcPrimaryActions CheckGroupAction()
		{
			
			NpcPrimaryActions tempAction = NpcPrimaryActions.Wander;

			if ( MyGroup.Count > 0 )
			{
				//	Make sure no group member has gone off too far

				float maxDist = 0f;
				float tmpDist = 0f;

				foreach ( NpcBehaviour npc in MyGroup )
				{
					if (!npc) continue;

					tmpDist = (Head.position - npc.Head.position).sqrMagnitude;
							
					if (tmpDist > maxDist)
					{
						MyTargets.friend = npc;
						maxDist          = tmpDist;
					}
				}

				if (maxDist > 20f)
				{
					tempAction   = NpcPrimaryActions.Regroup;
				}
			}

			return tempAction;
		}

		
		
		
		int ThreatFrameCount = 0;


		public void Shot(Shot shot)
		{
			if (Time.time - LastTimeShot < 5) return;

			FunFacts.Inc(NpcId, "BeenShot");
			Mojo.Feel("Fear", 2f);
			Mojo.Feel("Angry", 1f);

			ActivityMessage.Set("I'm shot!", 3f);

			if (HasGun)
            {
				if (FH.IsHolding) FH.FireAtWill = true;
				if (BH.IsHolding) BH.FireAtWill = true;
            }

			if ( !DefenseActions.Contains( PrimaryAction ) )
			{
				StartCoroutine(IGuessShooter(shot.normal));
				LastTimeShot = Time.time;
			}
		}

		public void Stabbed(Stabbing stab)
		{
			FunFacts.Inc(NpcId, "BeenStabbed");
		} 


		public bool CheckRandomSituations(bool performAct=false)
		{
			// ────────────────────────────────────────────────────────────────────────────
			//   :::::: CHECK RANDOM SITUATIONS
			// ────────────────────────────────────────────────────────────────────────────
			//ScanAhead();
			//int itemsOnFire      = 0;
			//int EventIdUniq      = NpcId + Time.frameCount;
			
			//bool okToReport 	 = false;
			//bool checkIfReported = true;

			//MyActWeights.reset();

			//for ( int i = -1; ++i < ScanResultsCount; )
			//{
			//	if ( !ScanResults[i].enabled ) continue;
			//	if ( ScanResults[i].transform.root.TryGetComponent<NpcBehaviour>( out NpcBehaviour npcX ) )
			//	{
			//		if ( FacingToward( npcX ) )
			//		{
			//			if ( npcX.FH.IsAiming || npcX.BH.IsAiming )
			//			{
			//				if ( ++ThreatFrameCount >= 2 ) { 
			//					if ( FH.IsHolding || BH.IsHolding )
			//					{
			//						MyActWeights.Defend = 10 + (ThreatLevel - npcX.ThreatLevel);
			//					}
			//					else
			//					{
			//						AcknowledgeThreat = true;
			//						MyActWeights.Survive += npcX.ThreatLevel;
			//					}
			//				}
			//			} else ThreatFrameCount = 0;
			//		}
			//	}
			//	else if ( ScanResults[i] && ScanResults[i].transform && ScanResults[i].transform.root 
			//		&& ScanResults[i].transform.root.TryGetComponent<PersonBehaviour>( out PersonBehaviour person ) )
			//	{
			//		if (person == PBO) continue;
			//		foreach ( GripBehaviour grip in person.GetComponentsInChildren<GripBehaviour>() )
			//		{
			//			if (grip.isHolding)
			//			{
			//				//	person is holding someTool
			//				if (!grip.CurrentlyHolding) continue;
			//				float heldThreat = xxx.GetThreatLevelOfItem(grip.CurrentlyHolding);

			//				if ( heldThreat > 4 )
			//				{
			//					// gun ?
			//					if (!xxx.GetWeaponBasics(grip.CurrentlyHolding).CanShoot) continue;
			//					if ( xxx.AimingTowards( grip.CurrentlyHolding.transform, Head ) )	
			//					{
			//						if (++ThreatFrameCount >= 3) { 
			//							if (FH.IsHolding || BH.IsHolding)
			//							{
			//								MyActWeights.DefendPerson += 8;
			//								MyTargets.person = person;
			//							}
			//							else { 
			//								MyActWeights.TakeCover += 1;
			//								MyTargets.item = grip.CurrentlyHolding;
										
			//								if (MyTargets.friend && MyTargets.friend.PBO == person) MyTargets.person = person;
			//							} 
			//						}
			//					} else ThreatFrameCount = 0;
			//				}
			//			}
			//		}
			//	}

			//	if ( ScanResults[i].TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour pb ) )
			//	{
			//		//  Check if something is on fire

			//		if ( pb.OnFire ) {
			//			MyActWeights.WatchEvent = 1;
			//			Debug.Log("Found Fire X");
			//			//if (PBO.DesiredWalkingDirection > 0) PBO.DesiredWalkingDirection = -10f;
			//			itemsOnFire++;

			//			if (checkIfReported) {
			//				okToReport      = NpcEvents.CanReportEvent(EventIds.Fire);
			//				checkIfReported = false;
			//			}

			//			if ( (FH.IsHolding && FH.Tool && FH.Tool.props.canFightFire) || (BH.IsHolding && BH.Tool && BH.Tool.props.canFightFire) )
			//			{
			//				MyActWeights.FightFire++;
			//				MyTargets.item	= pb;
			//				performAct = true;
			//			}

			//			if (okToReport) NpcEvents.SetEventInfo(EventIdUniq, pb);
			//		} else if (xxx.CanHold(pb) && MyActWeights.Survive > 1f)
			//		{
			//			MyTargets.item = pb;
						
			//			if (PrimaryActionCoroutine != null) StopCoroutine(PrimaryActionCoroutine);
			//			PrimaryActionCoroutine = StartCoroutine( IActionDive(pb.transform.position) );
			//			return true;

			//			////	Look for obstacle
			//			//if ( pb.ObjectArea > 3 
			//			//	|| (pb.gameObject.layer != 9 && pb.gameObject.layer != 10) 
			//			//	|| ( pb.gameObject.layer != 10 && pb.TryGetComponent<FreezeBehaviour>(out _) ) )
			//			//                  {
			//			//	if (Mathf.Abs(pb.transform.position.x - Head.position.x) < 1f)  Flip();
			//			//                  }
			//		}
			//	}
			//}

			////if ( okToReport && itemsOnFire > 0 )
			//if (okToReport && itemsOnFire > 0)
			//{
			//	NpcEvents.SetEventInfo(EventIdUniq, EventIds.Fire);

			//	NpcEvents.BroadcastRadius(EventIdUniq, Head.position, 50f);
			//}



			//if ( performAct )
			//{
			//	PrimaryAction = DecideBestAction(MyActWeights);
				
			//	if (PrimaryAction == NpcPrimaryActions.Wait) return false;

			//	DecideWhatToDo(true);
			//	return true;
			//}

			return false;
		}


		

		void FixedUpdate()
		{
			if (NpcMain.DEBUG_DRAW) ModAPI.Draw.Rect(ScanStart,ScanStop);
		}
		

		


		public bool CollideNPC( NpcBehaviour otherNPC )
		{
			if(MyFights.Contains(otherNPC) || !otherNPC.AllowNoCollisions(this)) return false;

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

		public IEnumerator IActionWait()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionWait()");

			float waitTime = Time.time + xxx.rr(0.5f, 2f);

			float flipTime = 0;

			int fightCount = MyFights.Count;

			float pspaceCheck = Time.time + 0.1f;
			
			int resultCount ;
			

			while ( Time.time < waitTime && MyFights.Count == fightCount && !CancelAction )
			{
				yield return new WaitForFixedUpdate();

				if (Time.time > pspaceCheck) { 

					Mojo.Feel("Bored", 1);

					Mojo.Feel("Tired", -1);

					pspaceCheck = Time.time + 0.1f;

					//resultCount = Physics2D.OverlapCapsuleNonAlloc(Head.position, new Vector2(2,1), CapsuleDirection2D.Horizontal, 0f, CollideResults);

					//for ( int i = resultCount; --i >= 0; )
					//{
					//	if (CollideResults[i].transform.root.TryGetComponent<PersonBehaviour>(out PersonBehaviour person) && person != PBO) {
					//		if (Mathf.Abs(CollideResults[i].attachedRigidbody.velocity.magnitude) > 0.05f) continue;
					//		PBO.DesiredWalkingDirection = (FacingToward(CollideResults[i].transform.position)) ? -4f : 4f;
					//		break;
					//	} 
					//}
				}

				if ( Time.time > flipTime )
				{
					flipTime = Time.time + xxx.rr(2.5f, 5.1f);
					if (xxx.rr(1,10) == 2 )
					{
						Flip();
						yield return new WaitForFixedUpdate();
					} 
					else if (ScannedWall ){

						Flip();
						yield return new WaitForFixedUpdate();
					}

					//if (Time.frameCount % 50 == 0 && SvCheckForThreats() || SvShoot()) {

					//	if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[Wander]: CheckForThreats || SvShoot()");

					//	DecideWhatToDo(true);
					//	PBO.DesiredWalkingDirection = 0f;
					//}

				}

				if (Time.frameCount % 50 == 0 && (SvCheckForThreats() || SvShoot())) {

					if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[Wander]: CheckForThreats || SvShoot()");

					DecideWhatToDo(true);
					PBO.DesiredWalkingDirection = 0f;
				}
			}

			PrimaryAction = NpcPrimaryActions.Thinking;
		}


		public IEnumerator IActionWander()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionWander()");
			yield return new WaitForFixedUpdate();

			float wanderTime = Time.time + xxx.rr(0.5f, 5f);

			int fightCount = MyFights.Count;

			while ( Time.time < wanderTime && MyFights.Count == fightCount && !CancelAction )
			{
				yield return new WaitForFixedUpdate();

				if (PBO.DesiredWalkingDirection < 1f) {
					PBO.DesiredWalkingDirection = 10f;
					Mojo.Feel("Tired", 1);
					Mojo.Feel("Bored", -1);
				}

				if (Time.frameCount % 50 == 0 && (SvCheckForThreats() || SvShoot())) {

					if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[Wander]: CheckForThreats || SvShoot()");

					DecideWhatToDo(true);
					PBO.DesiredWalkingDirection = 0f;
				}
			}
			Mojo.Feel("Tired", wanderTime * 0.1f);
			PrimaryAction = NpcPrimaryActions.Thinking;
		}


		public IEnumerator IActionMedic()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionMedic()");
			PrimaryAction = NpcPrimaryActions.Medic;

			NpcBehaviour hurtNpc = CurrentEventInfo.NPCs[0];
			if (!hurtNpc || !hurtNpc.PBO) {
				PrimaryAction = NpcPrimaryActions.Thinking;
				yield break;

			}
			yield return new WaitForFixedUpdate();

			float wanderTime = Time.time + xxx.rr(0.5f, 10f);

			float dist;

			if ( !FacingToward( hurtNpc ) )
			{
				Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForEndOfFrame();
			}

			while ( Time.time < wanderTime && hurtNpc && hurtNpc.PBO )
			{
				dist = Head.position.x - hurtNpc.Head.position.x;

				

				//if (CheckRandomSituations(true)) yield break;

				if (Mathf.Abs(dist) > 0.05f)
				{
					if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 10f;
					
					yield return new WaitForSeconds(0.5f);
				}
			}

			if ( hurtNpc && hurtNpc.PBO )
			{
				//	Build Medic Pose
				MyNpcPose = new NpcPose(this, "medic", false);
				MyNpcPose.Ragdoll.ShouldStandUpright       = false;
				MyNpcPose.Ragdoll.State                    = PoseState.Rest;
				MyNpcPose.Ragdoll.Rigidity                 = 2.3f;
				MyNpcPose.Ragdoll.AnimationSpeedMultiplier = 2.5f;
				MyNpcPose.Ragdoll.UprightForceMultiplier   = 2f;
				MyNpcPose.Import();

				MyNpcPose.RunMove();
				yield return new WaitForSeconds( 3f );

				if (FH.IsHolding) FH.Drop();
				if (BH.IsHolding) BH.Drop();

			}

			//LB["UpperArm"].Broken = LB["UpperArmFront"].Broken = true;
			Vector2 position = CurrentEventInfo.PBs[0].transform.position;

			if ( !FacingToward( position ) )
			{
				Flip();
				yield return new WaitForEndOfFrame();
				yield return new WaitForFixedUpdate();
			}

			while ( hurtNpc && hurtNpc.PBO && CurrentEventInfo.PBs.Count > 0 )
			{
				//ModAPI.Notify("Bandaging limb: " + CurrentEventInfo.PBs[0].name);

				float bandageTimer = Time.time + 5;

				while ( hurtNpc && hurtNpc.PBO && Time.time > bandageTimer )
				{
					RB["LowerArm"].AddForce((position - RB["LowerArm"].position) * Time.fixedDeltaTime * 10f);
					RB["LowerArmFront"].AddForce((position - RB["LowerArmFront"].position) * Time.fixedDeltaTime * 10f);
					yield return new WaitForFixedUpdate();
				}

				//  Create bandage
				if ( hurtNpc && hurtNpc.PBO )
				{
					LimbBehaviour limb = hurtNpc.PBO.GetComponent<LimbBehaviour>();

					foreach ( GameObject bp in limb.CirculationBehaviour.BleedingParticles )
					{
						Vector2 startPoint = limb.transform.position;
						Vector2 endPoint   = bp.transform.position;
						Vector2 direction  = startPoint - endPoint;


						SpringJoint2D joint = CurrentEventInfo.PBs[0].gameObject.AddComponent<SpringJoint2D>();
					
						joint.autoConfigureConnectedAnchor = false;
				
						joint.anchor = startPoint + (direction * -1);
					
						joint.connectedBody   = CurrentEventInfo.PBs[0].rigidbody;
						joint.connectedAnchor = endPoint + direction;

						//springJoint2D.anchor = ActiveSingleSelected.transform.InverseTransformPoint( startPos );
					
						AppliedBandageBehaviour bandageBehaviour = joint.gameObject.AddComponent<AppliedBandageBehaviour>();
						bandageBehaviour.WireColor               = Color.white;
						bandageBehaviour.WireMaterial            = Resources.Load<Material>("Materials/BandageWire");
						bandageBehaviour.WireWidth               = 0.09f;
						bandageBehaviour.typedJoint              = joint;
					}

					

					CurrentEventInfo.PBs.RemoveAt(0);

					yield return new WaitForFixedUpdate();
					yield return new WaitForEndOfFrame();
				}
			}


			PrimaryAction = NpcPrimaryActions.Thinking;
		}

		public void ClearAction()
        {
			if (PrimaryActionCoroutine != null) StopCoroutine(PrimaryActionCoroutine);
			PrimaryAction = NpcPrimaryActions.Thinking;
        }

		public IEnumerator IActionSurvive()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionSurvive()");
			
			yield return new WaitForFixedUpdate();

			if ( !MyTargets.enemy )
			{
				ClearAction();
				yield break;
			}

			switch( xxx.rr(1,3))
			{
				case 1:
					CommonPoses["survive"].RunMove();
					break;

				case 2:
					if (MyTargets.item) { 
						RB["UpperBody"].AddForce((MyTargets.item.transform.position - Head.position).normalized * TotalWeight * 2f, ForceMode2D.Impulse);
						yield return new WaitForFixedUpdate();
						CommonPoses["prone"].RunMove();
					} else
                    {
						PBO.OverridePoseIndex = (int)PoseState.Flat;
                    }
					break;

				case 3:
					CommonPoses["takecover"].RunMove();
					break;
			}

			float MaxTimeWait = Time.time + 10f;

			bool TriggerRecover = false;

			while (MyTargets.enemy && Time.time < MaxTimeWait && !CancelAction )
			{
				if ( !MyFights.Contains( MyTargets.enemy ) && !TriggerRecover )
				{
					TriggerRecover = true;
					MaxTimeWait    = Time.time + xxx.rr(2,5);
				}

				Mojo.Feel("Chicken", Time.fixedDeltaTime * 5f);

				RB["LowerArm"].AddForce(Vector2.down * TotalWeight * Time.fixedDeltaTime * xxx.rr(100f, 300f));
				RB["LowerArmFront"].AddForce(Vector2.down * TotalWeight * Time.fixedDeltaTime * xxx.rr(100f, 300f));

				//MyNpcPose.RunMove();

				if (CheckInterval(1.5f) && CheckRandomSituations(true)) {
					NpcPose.Clear(PBO);
					yield break;
				}

				yield return new WaitForFixedUpdate();
			}

			NpcPose.Clear(PBO);

			PrimaryAction = NpcPrimaryActions.Thinking;
		}


		public IEnumerator IActionTakeCover()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionTakeCover()");
			yield return new WaitForFixedUpdate();


			if ( !MyTargets.item)
			{
				StartCoroutine(IActionWait());
				yield break;
			}

			switch( xxx.rr(1,3))
			{
				case 1:
					CommonPoses["crouch"].RunMove();
					break;

				case 2:
					RB["UpperBody"].AddForce((MyTargets.item.transform.position - Head.position).normalized * TotalWeight * 2f, ForceMode2D.Impulse);
					yield return new WaitForFixedUpdate();
					CommonPoses["prone"].RunMove();
					break;

				case 3:
					CommonPoses["takecover"].RunMove();
					break;
			}
			
			float MaxTimeWait = Time.time + 10f;

			while (MyTargets.item && Time.time < MaxTimeWait && !CancelAction)
			{
				if ( !xxx.AimingTowards(MyTargets.item.transform, Head))
				{
					MaxTimeWait    = Time.time + xxx.rr(5,10);
				}

				yield return new WaitForFixedUpdate();

				Mojo.Feel("Fear", Time.fixedDeltaTime);
			}

			NpcPose.Clear(PBO);

			PrimaryAction = NpcPrimaryActions.Thinking;
		}


		public IEnumerator IActionScavenge()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionScavenge()");
			if (!MyTargets.prop)
			{
				ClearAction();
				yield break;
			}

			float dist = float.MaxValue;

			while ( MyTargets.prop && !MyTargets.prop.P.beingHeldByGripper && !CancelAction)
			{
				Vector3 target = MyTargets.prop.transform.position;

				if (!FacingToward(target)) {
					
					Flip();
					yield return new WaitForFixedUpdate();
				}

				dist = Head.position.x - target.x;

				if (Mathf.Abs(dist) > 0.05f)
				{
					if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 10f;
					
					yield return new WaitForSeconds(0.5f);
				}
				

				yield return new WaitForFixedUpdate();
				
				if (Mathf.Abs(dist) < 0.5f) break;

				if (CheckInterval(1.5f) && CheckRandomSituations(true)) yield break;
				
			}

			//	Check if we got the item
			if (Mathf.Abs(dist) > 1f) Mojo.Feel("Annoyed", 5f);

			PrimaryAction               = NpcPrimaryActions.Thinking;
			//PBO.OverridePoseIndex       = (int)PoseState.Rest;
			PBO.DesiredWalkingDirection = 0;
		}


		public IEnumerator IActionRecruit()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionRecruit()");
			if (!MyTargets.person)
			{
				ClearAction();
				yield break;
			}

			float dist = float.MaxValue;

			Transform head = MyTargets.person.Limbs[0].transform;
			

			NpcHand hand = (FH.IsHolding && FH.Tool.props.canRecruit) ? FH : BH;

			NoGhost.Add(head.root);

			hand.Tool.NoGhost.Add(head.root);

			NpcGadget gadget = hand.Tool.P.GetComponent<NpcGadget>();
			if (!gadget) { ClearAction(); yield break; }


			xxx.ToggleCollisions(head,Head,true,true);
			xxx.ToggleCollisions(head.root,hand.Tool.P.transform,true,false);

			Coroutine pointy = null;

			while ( MyTargets.person && !CancelAction && hand.GB.isHolding)
			{
				Vector3 target = head.position;

				if (!FacingToward(target)) {
					
					Flip();
					yield return new WaitForFixedUpdate();
				}

				if (CheckInterval(0.5f) && TeamId != gadget.TeamId ) gadget.Use(null);

				dist = Head.position.x - target.x;

				if (Mathf.Abs(dist) < 2.0f && pointy == null) pointy = hand.StartCoroutine(hand.IPoint(head));

				if (Mathf.Abs(dist) > 0.5f)
				{
					if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 5f;
					
					yield return new WaitForSeconds(0.1f);
				} 
				else
				if (Mathf.Abs(dist) < 0.1f)
                {
					if (PBO.DesiredWalkingDirection > -1f) PBO.DesiredWalkingDirection = -5f;
                }
				else
                {
					PBO.DesiredWalkingDirection = 0f;
					yield return new WaitForSeconds(3.5f);
					if (pointy != null) hand.StopCoroutine(pointy);
					break;
                }
				

				
				if (Mathf.Abs(dist) < 0.5f) break;

				yield return new WaitForFixedUpdate();

				
			}
			NoGhost.Remove(head.root);
			//	Check if we got the item

			PrimaryAction               = NpcPrimaryActions.Thinking;
			//PBO.OverridePoseIndex       = (int)PoseState.Rest;
			PBO.DesiredWalkingDirection = 0;
		}


		private float IntervalTime = 0f;
		private float LastInterval = 0f;

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

		public IEnumerator IActionGroupUp(bool regroup=false)
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionGroupup()");
			PrimaryAction = regroup ? NpcPrimaryActions.Regroup : NpcPrimaryActions.GroupUp;
			float dist = 0f;

			if (regroup)
			{
				float fTemp   = 0f;

				Vector3 MyPos = Head.position;

				for ( int i = MyGroup.Count; --i >= 0; )
				{
					if ( !MyGroup[i] )
					{
						MyGroup.RemoveAt(i);
						continue;
					}
					fTemp = ((MyPos - MyGroup[i].Head.position).sqrMagnitude);
					if (fTemp < dist)
					{
						MyTargets.friend = MyGroup[i];
						dist             = fTemp;
					}
				}
			}

			if (!MyTargets.friend)
			{
				StartCoroutine(IActionWait());
				yield break;
			}


			while ( MyTargets.friend && MyTargets.friend.PBO )
			{
				Vector3 target = MyTargets.friend.Head.position;

				if (!FacingToward(target)) {
					
					Flip();
					yield return new WaitForFixedUpdate();
					yield return new WaitForFixedUpdate();
				}

				dist = Head.position.x - target.x;

				if (Mathf.Abs(dist) > 1.0f)
				{
					if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 10f;
					
					yield return new WaitForSeconds(0.5f);
				}

				yield return new WaitForFixedUpdate();
				
				if (Mathf.Abs(dist) < 1.0f) break;

				if (CheckInterval(1.4f) && CheckRandomSituations(true)) yield break;
			}

			if (MyTargets.friend && Mathf.Abs(Head.position.x - MyTargets.friend.Head.position.x) < 0.5f)
			{
				if (!MyGroup.Contains(MyTargets.friend))	MyGroup.Add(MyTargets.friend);

				xxx.ToggleCollisions(Head, MyTargets.friend.Head,false, true);

			} 

			PBO.DesiredWalkingDirection = 0;
			PrimaryAction               = NpcPrimaryActions.Thinking;
		}

		
		public bool CanThink()
		{
			return PBO && PBO.Consciousness > 0.8f && PBO.ShockLevel < 0.2f && PBO.PainLevel < 0.5f && !PBO.Braindead;
		}


		public IEnumerator IActionShoot(bool isDefense=false)
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionShoot()");
			PrimaryAction = NpcPrimaryActions.Fight;

			
			if (!MyTargets.enemy || !MyTargets.enemy.PBO)
			{
				StartCoroutine(IActionWait());
				yield break;
			}
		
			float dist;
			FH.FixHandsPose((int)PoseState.Walking);
			BH.FixHandsPose((int)PoseState.Walking);

			if (!MyTargets.enemy.MyFights.Contains(this)) {
				MyTargets.enemy.MyFights.Add(this);
			}
			if ( MyTargets.enemy && xxx.ValidateEnemyTarget(MyTargets.enemy.PBO) && MyTargets.enemy.ThreatLevel > 0 )
			{
				NpcHand hand = Hands.PickRandom();
				if (!hand.IsHolding || !hand.Tool.props.canShoot) hand = hand.AltHand;

		
				if (FH.IsHolding || BH.IsHolding)
				{ 
					//float aimChance = CheckMyShot(MyTargets.enemy.Head, hand.Tool.T);
					
					bool showMercy = xxx.rr(1,100) <= Mojo.Traits["Mean"] + Mojo.Feelings["Angry"] / 2;

					while ( CanThink() && MyTargets.enemy && MyTargets.enemy.PBO && xxx.ValidateEnemyTarget(MyTargets.enemy.PBO) && !CancelAction )
					{
						if (MyTargets.enemy.ThreatLevel <= 0 && showMercy) break;

						hand.FireAtWill = false;

						if (!FacingToward(MyTargets.enemy.Head.position)) { Flip(); yield return new WaitForFixedUpdate(); }
						//
						//	Decide which hand to use
						//
						hand = Hands.PickRandom();
						if (!hand || !hand.IsHolding || !hand.Tool.props.canShoot) hand = hand.AltHand;

						//
						//	Decide Aiming Pose
						//
						if (!hand || !hand.Tool || !hand.Tool.P) break;

						CurrentAimStyle = hand.Tool.props.AimStyle(this, MyTargets.enemy);

						//
						//	Decide how long to hesitate before firing
						//
						float hesitate = xxx.rr(0.5f,3.1f);


						//
						//	Decide which limb to aim for
						//
						LimbBehaviour limbTarget = MyTargets.enemy.PBO.Limbs.PickRandom();
						if (!limbTarget || !limbTarget.PhysicalBehaviour) break;

						hand.Target(limbTarget.PhysicalBehaviour);
										
						//
						//	Decide what distance we should be at
						//
						int minDist       = 3;
						int maxDist       = 10;
						bool doCommonPose = false;
						float timerHesitate = 0;

						switch( CurrentAimStyle )
						{
							case AimStyles.Rockets:
								minDist      = 10;
								maxDist      = 25;
								hesitate    += 3f;
								doCommonPose = true;
								break;

							case AimStyles.Proned:
								minDist      = 4;
								maxDist      = 13;
								hesitate    += 2f;
								doCommonPose = true;
								break;

							case AimStyles.Crouched:
								minDist      = 4;
								maxDist      = 12;
								doCommonPose = true;
								hesitate    += 1f;
								break;
						}
						float prefDistance    = xxx.rr(minDist, maxDist);
						float threshDist      = Mathf.RoundToInt((maxDist - minDist) / 2);
						bool poseLocked		  = false;


						//
						//	Decide how long to stay in this pose
						//
						float aimTime = Time.time + xxx.rr(5,15);
						timerHesitate = Time.time + hesitate;
						

						while ( CanThink() && MyTargets.enemy && MyTargets.enemy.PBO && xxx.ValidateEnemyTarget(MyTargets.enemy.PBO) && Time.time < aimTime && !CancelAction)
						{
							dist = Head.position.x - MyTargets.enemy.Head.position.x;

							if (!poseLocked && Mathf.Abs(dist) > prefDistance + threshDist) 
							{
							//    if (PBO.ActivePose != PBO.Poses[0]) {
							//        NpcPose.Clear(PBO);
							//    }
								if (PBO.DesiredWalkingDirection < 1f)  PBO.DesiredWalkingDirection = 10f;
							} 
							else if (!poseLocked && Mathf.Abs(dist) < prefDistance - threshDist)
							{
							//    NpcPose.Clear(PBO);
								if (PBO.DesiredWalkingDirection > -1f) PBO.DesiredWalkingDirection = -10f;
							}
                            else
                            {
                                PBO.DesiredWalkingDirection = 0f;
                                // if ( !poseLocked && ( CurrentAimStyle != AimStyles.Standard || !isDefense ) ) timerHesitate = Time.time + hesitate;
                                if ( doCommonPose )
                                {

                                    if ( CurrentAimStyle == AimStyles.Crouched || CurrentAimStyle == AimStyles.Rockets ) CommonPoses["crouch"].RunMove();
                                    else if ( CurrentAimStyle == AimStyles.Proned )
                                    {
                                        if ( !poseLocked )
                                        {
                                            if ( RB["UpperBody"] ) RB["UpperBody"].AddForce( ( MyTargets.enemy.Head.position - Head.position ).normalized * TotalWeight * 2.5f, ForceMode2D.Impulse );
                                            if ( RB["Foot"] ) RB["Foot"].AddForce( ( MyTargets.enemy.Head.position - Head.position ).normalized * TotalWeight * -2f, ForceMode2D.Impulse );
                                            yield return new WaitForFixedUpdate();
											if (!hand.FireAtWill && Time.time > timerHesitate) hand.FireAtWill = true;
                                        }
                                        CommonPoses["prone"].RunMove();
                                        if ( LB["Head"].IsOnFloor ) break;
                                    }

                                }

                                if ( CurrentAimStyle != AimStyles.Standard ) poseLocked = true;

                            }
                            if (!hand.FireAtWill && Time.time > timerHesitate) hand.FireAtWill = true;

							yield return new WaitForFixedUpdate();
						}

					}
					if (PBO) NpcPose.Clear(PBO);
					
					hand.IsAiming = hand.AltHand.IsAiming = false;

					//hand.AimAt = null;

					hand.FireAtWill = hand.AltHand.FireAtWill = false;
					yield return new WaitForEndOfFrame();
					
				}
				if (MyTargets.enemy) { 
					if (MyTargets.enemy.MyFights.Contains(this)) MyTargets.enemy.MyFights.Remove(this);
					if (MyFights.Contains(MyTargets.enemy)) MyFights.Remove(MyTargets.enemy);
				}


				yield return new WaitForFixedUpdate();

				yield return new WaitForSeconds(xxx.rr(0.1f, 3f));
				
			}

			
			PrimaryAction               = NpcPrimaryActions.Thinking;
		}



		public IEnumerator IActionDefend(NpcBehaviour npcEnemy = null)
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionDefend()");
			PrimaryAction = NpcPrimaryActions.Defend;

			if (npcEnemy == null) npcEnemy = MyTargets.enemy;

			FH.FixHandsPose((int)PoseState.Walking);
			BH.FixHandsPose((int)PoseState.Walking);

			if (!npcEnemy)
			{
				StartCoroutine(IActionWait());
				yield break;
			}

			float dist;

			if (!FacingToward(npcEnemy)) {
				Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}


			NpcHand[] hands           = {FH,BH};
			NpcHand hand              = hands.PickRandom();
			if (!hand.IsHolding) hand = hand.AltHand;


			if (hand.IsHolding)
			{ 
				//float aimChance = CheckMyShot(MyTargets.enemy.Head, hand.Tool.T);

				float hesitate = Time.time + xxx.rr(0.5f,1.1f);

				hand.Target(npcEnemy.PBO.Limbs.PickRandom().PhysicalBehaviour);

				while ( npcEnemy && xxx.ValidateEnemyTarget(npcEnemy.PBO) && npcEnemy.ThreatLevel > 0 )
				{
					hand.FireAtWill = Time.time > hesitate;

					dist = Head.position.x - MyTargets.enemy.Head.position.x;

					if (!FacingToward(MyTargets.enemy.Head.position)) {
					
						Flip();
						yield return new WaitForFixedUpdate();
					}

					if (Mathf.Abs(dist) < 10f) 
					{
						if (PBO.DesiredWalkingDirection > -1f) PBO.DesiredWalkingDirection = -10f;
						
					} 

					yield return new WaitForFixedUpdate();
				}

				PBO.DesiredWalkingDirection = 0;
					
				hand.IsAiming = false;

				//hand.AimAt = null;

				hand.FireAtWill = false;

				yield return new WaitForEndOfFrame();
					
			}

			yield return new WaitForFixedUpdate();

			yield return new WaitForSeconds(xxx.rr(0.1f, 3f));
			
			PrimaryAction   = NpcPrimaryActions.Thinking;
		}

		public IEnumerator IActionDive( Vector3 position )
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionDive()");
			PrimaryAction = NpcPrimaryActions.Dive;
			LastInterval = 0;

			CommonPoses["dive"].RunMove();

			Vector2 dir = (Head.position - position);
			float force = dir.magnitude;

			while ( !CheckInterval( 1f ) )
			{
				RB["LowerBody"].AddForce(Vector2.down * Time.fixedDeltaTime * TotalWeight * 100f);
				yield return new WaitForFixedUpdate();
			}


			RB["UpperBody"].AddForce(((Vector2.right * -Facing * 1.5f) + Vector2.up ) * TotalWeight * force, ForceMode2D.Impulse);
			yield return new WaitForFixedUpdate();
			CommonPoses.Clear();
			yield return new WaitForFixedUpdate();

			LastInterval = 0;

			while ( !CheckInterval( 0.5f ) )
			{
				RB["Head"]?.AddForce(Vector2.right * -Facing * Time.fixedDeltaTime * (100 * TotalWeight));
				if (!BH.IsAiming) RB["LowerArm"]?.AddForce(Vector2.right * -Facing * (1 * TotalWeight));
				if (!FH.IsAiming) RB["LowerArmFront"]?.AddForce(Vector2.right * -Facing * (1 * TotalWeight));
				yield return new WaitForFixedUpdate();
			}

			LastInterval = 0;

			while ( !CheckInterval( 0.5f ) )
			{
				if (!BH.IsAiming) RB["UpperArm"]?.AddForce(Vector2.right * -Facing  * (1 * TotalWeight));
				if (!FH.IsAiming) RB["UpperArmFront"]?.AddForce(Vector2.right * -Facing * (1 * TotalWeight));
				yield return new WaitForFixedUpdate();
			}
			
			PrimaryAction   = NpcPrimaryActions.Thinking;

		}


		public IEnumerator IActionDisco()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionDisco()");
			
			PrimaryAction = NpcPrimaryActions.Disco;
			LastInterval  = 0;

			float DanceTimer = Time.time + xxx.rr(5,30);

			if (! MyTargets.item.TryGetComponent<JukeboxBehaviour>( out JukeboxBehaviour juke ) )
            {
				ClearAction();
				yield break;
            }


			while ( juke && juke.audioSource.isPlaying && Time.time > DanceTimer )
            {
				float interval = xxx.rr(0.5f, 2.5f);
				while ( !CheckInterval( interval ) )
                {
					MyNpcPose = new NpcPose(this, "disco1", false);

					yield return new WaitForFixedUpdate();
                }
				
				LastInterval = 0;

				interval = xxx.rr(0.5f, 2.5f);

				while ( !CheckInterval( interval ) )
                {
					MyNpcPose = new NpcPose(this, "disco2", false);

					yield return new WaitForFixedUpdate();
                }
            }

			
			
			PrimaryAction   = NpcPrimaryActions.Thinking;

		}

		public IEnumerator IActionDefendPerson(PersonBehaviour pbEnemy= null)
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionDefendPerson()");
			PrimaryAction = NpcPrimaryActions.DefendPerson;

			if (pbEnemy == null) pbEnemy = MyTargets.person;

			FH.FixHandsPose((int)PoseState.Walking);
			BH.FixHandsPose((int)PoseState.Walking);

			if (!pbEnemy)
			{
				StartCoroutine(IActionWait());
				yield break;
			}

			float dist;

			if (!FacingToward(pbEnemy.transform.position)) {
				Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}


			NpcHand[] hands           = {FH,BH};
			NpcHand hand              = hands.PickRandom();
			if (!hand.IsHolding) hand = hand.AltHand;


			if (hand.IsHolding)
			{ 
				//float aimChance = CheckMyShot(MyTargets.enemy.Head, hand.Tool.T);

				float hesitate = Time.time + xxx.rr(0.5f,1.1f);

				hand.Target(pbEnemy.Limbs.PickRandom().PhysicalBehaviour);
					

				while ( pbEnemy && xxx.ValidateEnemyTarget(pbEnemy) )
				{
					hand.FireAtWill = Time.time > hesitate;

					dist = Head.position.x - pbEnemy.Limbs[0].transform.position.x;

					if (!FacingToward(pbEnemy.Limbs[0].transform.position)) {
					
						Flip();
						yield return new WaitForFixedUpdate();
					}

					if (Mathf.Abs(dist) < 5f) 
					{
						if (PBO.DesiredWalkingDirection > -1f) PBO.DesiredWalkingDirection = -10f;
						
					} 

					yield return new WaitForFixedUpdate();
				}

				PBO.DesiredWalkingDirection = 0;
					
				hand.IsAiming = false;

				//hand.AimAt = null;

				hand.FireAtWill = false;

				yield return new WaitForEndOfFrame();
					
			}

			yield return new WaitForFixedUpdate();

			yield return new WaitForSeconds(xxx.rr(0.1f, 3f));
			
			PrimaryAction   = NpcPrimaryActions.Thinking;
		}

		
		public IEnumerator IActionFidget()
        {

			if (!MyTargets.item)
			{
				ClearAction();
				yield break;
			}

			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionFidget()");

			MyTargets.item.spriteRenderer.sortingLayerName = "Background";
			MyTargets.item.spriteRenderer.sortingOrder     = -10;
			

			float dist       = float.MaxValue;
			bool pickedHand  = false;
			Coroutine pointy = null;
			
			if (new string[]{"Jukebox","Television","Radio" }.Contains( MyTargets.item.name ) )
            {
				ActivityMessage.Set("Fidget with " + MyTargets.item.name, 5f);
            } else
            {
				ClearAction();
				yield break;
            }

			if (!FacingToward(MyTargets.item.transform.position)) {
					
				Flip();
				yield return new WaitForFixedUpdate();
			}

			while ( MyTargets.item && !CancelAction )
			{
				dist = Vector2.Distance(Head.position, MyTargets.item.transform.position);

				if (Mathf.Abs(dist) > 1.7f)
				{
					if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 5f;
					
				} 
				else
				if (Mathf.Abs(dist) < 1.0f)
                {
					if (PBO.DesiredWalkingDirection > -1f) PBO.DesiredWalkingDirection = -5f;
                }
				else
                {
					PBO.DesiredWalkingDirection = 0f;
					if ( !pickedHand )
					{
						pickedHand               = true;
						hand                     = RandomHand;
						if (hand.IsHolding) hand = hand.AltHand;
						pointy                   = hand.StartCoroutine(hand.IPointArm(MyTargets.item.transform));
						yield return new WaitForSeconds(2);
						

						
						//MyTargets.item.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
						Mojo.Feelings["Bored"] = 0;
						yield return new WaitForSeconds(1);
						JukeboxBehaviour juke = null;
						if (MyTargets.item )
                        {
                            switch ( MyTargets.item.name )
                            {
								case "Jukebox":
									if ( !MyTargets.item.TryGetComponent<JukeboxBehaviour>( out juke ) )
									{
										ClearAction();
										yield break;
									}

									juke.tracks = NpcMain.JukeBox;
									juke.NextSong();

									hand.StopCoroutine(pointy);

                                    if ( juke && juke.audioSource && juke.audioSource.isPlaying )
                                    {
                                        EventInfo MyEvent = new EventInfo();
										MyEvent.Expires = 10;
										MyEvent.EventId = EventIds.Jukebox;
										//MyEvent
                                        //{
                                        //    EventId   = EventIds.Jukebox,
                                        //    Expires   = 30f,
                                        //    Locations = {MyTargets.item.transform.position },
                                        //    PBs       = {juke.physicalBehaviour },
                                        //    Sender    = this,
                                        //};

                                        //NpcEvents.BroadcastEvent( MyEvent, MyTargets.item.transform.position, 20f );
                                    }
                                    break;


								case "Television":
									if ( MyTargets.item.TryGetComponent<TelevisionBehaviour>( out TelevisionBehaviour tv) )
                                    {
										if ( tv.Activated )
                                        {
											NpcEvents.BroadcastEvent(new EventInfo() { 
												EventId   = EventIds.TV,
												Expires   = 30f,
												Locations = {tv.transform.position },
												PBs       = {MyTargets.item},
												Sender    = this,
											}, tv.transform.position, 10f);
                                        }
									}
									break;			

                            }
                        }

                        CancelAction = true;
					}
					break;
				}

				yield return new WaitForFixedUpdate();
			}

			PrimaryAction               = NpcPrimaryActions.Thinking;
			//PBO.OverridePoseIndex       = (int)PoseState.Rest;
			PBO.DesiredWalkingDirection = 0;

			yield break;
        }


		public IEnumerator IActionFightFire()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionFightFire()");
			PrimaryAction = NpcPrimaryActions.FightFire;
			float dist    = 0f;
			NpcHand hand  = (FH.IsHolding && FH.Tool.props.canFightFire) ? FH : BH;
			hand.AimStyle = AimStyles.Spray;
			while ( xxx.NPCOnFire( this ) )
            {
				//	Self caught on fire
				ActivityMessage.Set("I'm on fire!", 5f);

				Utilities.FlipUtility.ForceFlip(hand.Tool.P);
				Debug.Log("Flipped " + hand.Tool.P.name);
				yield return new WaitForEndOfFrame();
				

				Vector3 pos1 = Head.position + (Vector3.right * -Facing * 2f);
				Vector3 pos2 = LB["Foot"].transform.position;
					
				float fireTime = Time.time + 5;
				while (Time.time < fireTime)
				{
					hand.AimTarget = pos1;
					hand.AimWeapon();
					hand.Tool.Activate(true);
					yield return new WaitForFixedUpdate();
				}
				
				Utilities.FlipUtility.ForceFlip(hand.Tool.P);
				Debug.Log("Flipped " + hand.Tool.P.name);

				fireTime = Time.time + 5;
				while (Time.time < fireTime)
				{
					hand.AimTarget = pos2;
					hand.AimWeapon();
					hand.Tool.Activate(true);
					yield return new WaitForFixedUpdate();
				}

				
			}

			if (hand.Tool.Facing != Facing)
			{
				Utilities.FlipUtility.ForceFlip(hand.Tool.P);
				Debug.Log("Flipped " + hand.Tool.P.name);
				yield return new WaitForFixedUpdate();
			}

			if (!MyTargets.item || !MyTargets.item.OnFire || !Head)
			{
				ClearAction();
				yield break;
			}


			if (!hand.Tool || !hand.Tool.P)
			{
				ClearAction();
				yield break;
			}

			for (; ; )
			{
				yield return new WaitForFixedUpdate();
				FireProof = true;
				while ( MyTargets.item && MyTargets.item.OnFire )
				{
					//Vector3 target = MyTargets.item.transform.position;
					//Vector3 target = MyTargets.item.transform.position;
					Vector3 target = MyTargets.item.colliders[0].ClosestPoint(Head.position);
					if (!FacingToward(target)) {
					
						Flip();
						yield return new WaitForFixedUpdate();
						yield return new WaitForFixedUpdate();
					}

					dist = Head.position.x - target.x;

					if (Mathf.Abs(dist) > 2.0f)
					{
						if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 5f;
					
						yield return new WaitForSeconds(0.1f);
					}

					hand.Target(MyTargets.item);

					yield return new WaitForFixedUpdate();
				
					if (Mathf.Abs(dist) < 2.0f) break;

				}
				
				PBO.DesiredWalkingDirection = 0;
				//PBO.OverridePoseIndex = (int)PoseState.Rest;
				if (!MyTargets.item) ClearAction();
				hand.Target(MyTargets.item);

				float walkTimer = Time.time + 3f;

				bool isOnFire = true;
				bool startCountdown = false;
				float extCountdown = 0f;
				while ( MyTargets.item && isOnFire )
				{
					if (!MyTargets.item.OnFire) { 
						if (!startCountdown)
						{
							startCountdown = true;
							extCountdown = Time.time + xxx.rr(1.0f, 2.0f);
						
						} else if (Time.time > extCountdown) isOnFire = false;
						
					}

					hand.Tool.Activate(true);
					if (Time.time > walkTimer)
					{
						walkTimer = Time.time + 3f;
						PBO.DesiredWalkingDirection += 5f;
					}
					yield return new WaitForFixedUpdate();

				}
				ScanTimeExpires = 0;
				
				int ScanResultsCount = Physics2D.OverlapBox(ScanStart, ScanStop, 0f, filter, ScanResults);

				dist               = float.MaxValue;
				float tmp;
				bool foundFire = false;

				for ( int i = -1; ++i < ScanResultsCount; )
				{
					if ( ScanResults[i].transform.root.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour pb ) )
					{
						if ( pb && pb.OnFire )
						{
							tmp = (pb.transform.position - Head.position).sqrMagnitude;
							if ( tmp > dist ) {
								dist = tmp;
								MyTargets.item = pb;
							}
							foundFire = true;
						}
					}
				}
			
				if (!foundFire) break;
			}

			FireProof                   = false;
			PrimaryAction               = NpcPrimaryActions.Thinking;
		}


		public IEnumerator IActionWatchEvent()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[action]: IActionWatchEvent()");
			PrimaryAction = NpcPrimaryActions.WatchEvent;
			float dist = 0f;

			if (!MyTargets.item || !MyTargets.item.OnFire)
			{
				StartCoroutine(IActionWait());
				yield break;
			}

			while ( MyTargets.item && !CancelAction)
			{
				Vector3 target = MyTargets.item.transform.position;

				if (!FacingToward(target)) {
					
					Flip();
					yield return new WaitForFixedUpdate();
					yield return new WaitForFixedUpdate();
				}

				dist = Head.position.x - target.x;

				if (Mathf.Abs(dist) > 2.5f)
				{
					if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 10f;
					
					yield return new WaitForSeconds(0.5f);
				}

				yield return new WaitForFixedUpdate();
				
				if (Mathf.Abs(dist) < 3.0f) break;

			}

			PBO.DesiredWalkingDirection = 0;
			yield return new WaitForSeconds(xxx.rr(2,5));

			PrimaryAction               = NpcPrimaryActions.Thinking;
		}

		float glitchfloat = 1;
		int glitchCount = 0;
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
					Enhancements.Vision = xxx.rr(35,70);
					break;

				case 2:
					Enhancements.Medic = true;
					break;

				case 3:
					Enhancements.Aiming = xxx.rr(50, 100);
					break;

				case 4:
					Enhancements.Processing = xxx.rr(1.0f, 5.0f);
					break;
            }


        }


        public void CalculateThreatLevel()
        {
			Mojo.Feelings["Chicken"] *= 0.5f;
			Mojo.Feel("Tired", 1f);
			Mojo.Feel("Hungry", xxx.rr(0.1f, 1.1f));

			_threatLevel = Config.BaseThreatLevel;
			
			_threatLevel += PBO.AverageHealth - 14f;

			_threatLevel += 5f - LB["UpperBody"].BaseStrength;

			MyBlood = 0f;
			foreach (LimbBehaviour limb in PBO.Limbs)
			MyBlood += limb.CirculationBehaviour.TotalLiquidAmount;

			MyBlood /= PBO.Limbs.Length;
				

			if (FH.IsHolding && FH.Tool) _threatLevel += FH.Tool.props.ThreatLevel;
			if (BH.IsHolding && BH.Tool) _threatLevel += BH.Tool.props.ThreatLevel;
			_threatLevel *= MyBlood;
			_threatLevel -= Mojo.Feelings["Chicken"];

			//if (!npc.IsUpright) threatLevel *= 0.5f;
        }


		public IEnumerator IFeelings()
		{
			for (; ; )
			{
				
				CalculateThreatLevel();

				yield return new WaitForSeconds(5);
			}
		}

		public IEnumerator IGuessShooter(Vector2 direction)
		{
			if (Mathf.Sign(direction.x) == Mathf.Sign(Facing)) {
				Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForEndOfFrame();
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
						ActivityMessage.Set(enemy.Config.Name + " shot me!", 2);
						if (!DefenseActions.Contains(PrimaryAction))
                        {
							if (PrimaryActionCoroutine != null) StopCoroutine(PrimaryActionCoroutine);
							if (HasGun) PrimaryActionCoroutine = StartCoroutine(IActionSurvive());
							else PrimaryActionCoroutine = StartCoroutine(IActionDefend(MyTargets.enemy));
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
							if (PrimaryActionCoroutine != null) StopCoroutine(PrimaryActionCoroutine);
							PrimaryActionCoroutine = StartCoroutine(IActionDefendPerson(MyTargets.person));
							yield break;
						}
					}
				}
			}
			
			ActivityMessage.Set("Who shot me?", 1f);

			yield break;
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
			int x1 = 0;
			int x2 = 0;
			int val = 0;
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
					if (PrimaryAction == NpcPrimaryActions.Thinking) DecideWhatToDo();
				}

				yield return new WaitForSeconds(1);
			}
		}

		//
		// ─── RUN RIGIDS ────────────────────────────────────────────────────────────────
		//
		public void RunRigids(Action<Rigidbody2D> action)
		{
			foreach (Rigidbody2D rigid in RB.Values) { action(rigid); }
		}

		public void RunRigids<t>(Action<Rigidbody2D, t> action, t option)
		{
			foreach (Rigidbody2D rigid in RB.Values) { action(rigid, option); }
		}

		public void RigidInertia(Rigidbody2D rb, float option) => rb.inertia = (option == -1) ? RigidOriginals[rb.name].inertia : option;
		public void RigidMass(Rigidbody2D rb, float option) => rb.mass = (option == -1) ? RigidOriginals[rb.name].mass : option;
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
			
			Config.SetName(PBO.name);

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
				lb.ShotDamageMultiplier *= 0.01f;	

				LB[lb.name] = lb;
			}
			
			Head		        = LB["Head"].transform;
			
			NpcId               = PBO.GetHashCode();

			SavePoses();

			if (WalkPose == null) WalkPose = PBO.LinkedPoses[PoseState.Walking];
			
			FH    = LB["LowerArmFront"].gameObject.GetOrAddComponent<NpcHand>();
			BH    = LB["LowerArm"].gameObject.GetOrAddComponent<NpcHand>();
			Hands = new NpcHand[]{ FH,BH };

			HeadL = LB["Head"];

			filter.NoFilter();

			FH.Init();
			BH.Init();

			CustomSetup(); 
 


			//Mojo.ShowStats();


			MyWidth  = transform.root.GetComponentInChildren<SpriteRenderer>().bounds.size.x;
			MyHeight = transform.root.GetComponentInChildren<SpriteRenderer>().bounds.size.y;


			//	Build Crouch Pose
			CommonPoses["crouch"]	= new NpcPose(this, "crouch", false);
			CommonPoses["crouch"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["crouch"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["crouch"].Ragdoll.Rigidity                 = 1.7f;
			CommonPoses["crouch"].Ragdoll.AnimationSpeedMultiplier = 2.5f;
			CommonPoses["crouch"].Ragdoll.UprightForceMultiplier   = 1.4f;
			CommonPoses["crouch"].Import();

			CommonPoses["takecover"]	= new NpcPose(this, "takecover", false);
			CommonPoses["takecover"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["takecover"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["takecover"].Ragdoll.Rigidity                 = 1.7f;
			CommonPoses["takecover"].Ragdoll.AnimationSpeedMultiplier = 2.5f;
			CommonPoses["takecover"].Ragdoll.UprightForceMultiplier   = 1.4f;
			CommonPoses["takecover"].Import();

			CommonPoses["survive"]	= new NpcPose(this, "survive", false);
			CommonPoses["survive"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["survive"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["survive"].Ragdoll.Rigidity                 = 1.7f;
			CommonPoses["survive"].Ragdoll.AnimationSpeedMultiplier = 2.5f;
			CommonPoses["survive"].Ragdoll.UprightForceMultiplier   = 1.4f;
			CommonPoses["survive"].Import();

			//	Build Prone Pose
			CommonPoses["prone"]	= new NpcPose(this, "prone", false);
			CommonPoses["prone"].Ragdoll.ShouldStandUpright       = false;
			CommonPoses["prone"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["prone"].Ragdoll.Rigidity                 = 2.3f;
			CommonPoses["prone"].Ragdoll.AnimationSpeedMultiplier = 2.5f;
			CommonPoses["prone"].Ragdoll.UprightForceMultiplier   = 0f;
			CommonPoses["prone"].Import();

			CommonPoses["dive"]	= new NpcPose(this, "dive", false);
			CommonPoses["dive"].Ragdoll.ShouldStandUpright       = false;
			CommonPoses["dive"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["dive"].Ragdoll.Rigidity                 = 2.3f;
			CommonPoses["dive"].Ragdoll.AnimationSpeedMultiplier = 2.5f;
			CommonPoses["dive"].Ragdoll.UprightForceMultiplier   = 0f;
			CommonPoses["dive"].Import();

			Goals        = new NpcGoals(this);
			Enhancements = new NpcEnhancements(this);


			FunFacts.Init(NpcId,Config.Name);


			NpcEvents.Init();
			NpcEvents.Subscribe(this);

			StartCoroutine(CheckStatus());
			StartCoroutine(IFeelings());


			if (Config.AutoStart) Active = true;

			ActivityMessage = new ActivityMessages(this);
			
			//HoverStats = Head.gameObject.GetOrAddComponent<NpcHoverStats>();
			NpcHoverStats.Show(this);
			//HoverStats.ToggleStats(this);

		}

		public void Death()
		{
			if (HoverStats) {
				HoverStats.StopAllCoroutines();
				HoverStats.HideStats();
			}
			Active = false;
					
			if (PBO) PBO.Braindead = true;
			if (FH.IsHolding && FH.Tool && FH.Tool.P)
			{
				FH.Tool.P.MakeWeightful();
				if (FH.GB) FH.GB.DropObject();
			}
			if (BH.IsHolding && BH.Tool && BH.Tool.P)
			{
				BH.Tool.P.MakeWeightful();
				if (BH.GB) BH.GB.DropObject();
			}

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


