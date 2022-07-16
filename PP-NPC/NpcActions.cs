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
	public class NpcActions : MonoBehaviour
	{
		public NpcBehaviour NPC;
		public PersonBehaviour PBO;
		public Transform Head;
		public NpcMojo Mojo;
		public NpcMemory Memory;
		public Dictionary<string, LimbBehaviour> LB;
		public Dictionary<string, Rigidbody2D> RB;
		public NpcHand[] Hands;
		public float TotalWeight;

		List<string> attacks     = new List<string>();

		private NpcHand hand;
		public Dictionary<string, float> Weights = new Dictionary<string, float>();
		public Coroutine PCR;

		public Dictionary<string, NpcBehaviour>			NPCTargets  = new Dictionary<string, NpcBehaviour>();
		public Dictionary<string, Props>				PropTargets = new Dictionary<string, Props>();
		public Dictionary<string, PhysicalBehaviour>    ItemTargets = new Dictionary<string, PhysicalBehaviour>();
		public Dictionary<string, PersonBehaviour>      PeepTargets = new Dictionary<string, PersonBehaviour>();

		public float LastTimeHit    = 0f;
		public float DelayWalk      = 0.01f;
		public float DelayAttack    = 0.2f;

		public string CurrentAction = "";
		public float HeadDistance(NpcBehaviour enemy) => Mathf.Abs(enemy.Head.position.x - Head.position.x);


		public struct MoveWeight
		{
			public string name;
			public string category;
			public string condition;
			public int weight;
			public MoveWeight( string _name, string _cat, int _weight, string _condition="" )
			{
				name      = _name;
				category  = _cat;
				weight    = _weight;
				condition = _condition;
			}
		};

		public List<MoveWeight> MoveWeights = new List<MoveWeight>()
		{
			new MoveWeight("backchoke", "back", 3, "karate,nodual"),
			new MoveWeight("backpunch", "back", 3, "karate,nodual"),
			new MoveWeight("backstab", "back", 10, "knife,nodual"),
			new MoveWeight("club-over", "mid", 10, "club"),
			new MoveWeight("club-over", "up", 10, "club"),
			new MoveWeight("club-jab", "mid", 10, "club"),
			new MoveWeight("club-jab", "up", 10, "club"),
			new MoveWeight("frontkick", "mid", 3, "karate"),
			new MoveWeight("frontpunch", "up", 5, "karate"),
			new MoveWeight("frontpunch", "mid", 5, "karate"),
			new MoveWeight("frontkick", "up", 3, "karate"),
			new MoveWeight("shoot", "all", 10, "gun"),
			new MoveWeight("shove", "mid", 1),
			new MoveWeight("shove", "up", 1),
			new MoveWeight("soccerkick", "down", 1, "karate"),
			new MoveWeight("soccerkick", "mid", 1, "karate"),
			new MoveWeight("stab", "mid", 3, "knife"),
			new MoveWeight("stab", "up", 3, "knife"),
			new MoveWeight("stomp", "down", 2, "karate"),
			new MoveWeight("stomp", "down", 2, "karate"),
			new MoveWeight("sword", "mid", 2, "sword"),
			new MoveWeight("sword", "up", 2, "sword"),
		};
		
		public Dictionary<string, List<string>> MoveList = new Dictionary<string, List<string>>()
		{
			{"all",  new List<string>() },
			{"up",   new List<string>() },
			{"back", new List<string>() },
			{"down", new List<string>() },
			{"mid",  new List<string>() },
		};

		public bool RecacheMoves = true;


		public bool DoingStrike
		{
			get { return NPC.DoingStrike; }
			set { NPC.DoingStrike = value;}
		}



		void Start()
		{
			NPC         = transform.root.GetComponent<NpcBehaviour>();
			PBO         = NPC.PBO;
			LB          = NPC.LB;
			RB          = NPC.RB;
			Head        = NPC.Head;
			Hands       = NPC.Hands;
			TotalWeight = NPC.TotalWeight;
			Mojo        = NPC.Mojo;
			Memory      = NPC.Memory;

			string[] allActions = {
				"Attack", "Caveman", "Club", "Dead", "Defend", "Disco", "Dying", "Fidget", "Fight", "FightFire", "Flee", "FrontKick", "GearUp",	
				"GroupUp", "Knife", "Medic", "Recruit", "Retreat", "Scavenge", "Shoot", "Shove", "SoccerKick", "Survive",	
				"Tackle", "TakeCover", "Troll", "Wander", "WatchEvent", "LastContact", "Upgrade", "Warn", "WatchFight", "Witness"};

			foreach ( string actName in allActions ) Weights[ actName.Trim() ] = 0;

		}


		public void ResetWeights() {
			Weights = Weights.ToDictionary(p => p.Key, p => 0f);
		}


		public bool IsFlipped   => (bool)(PBO.transform.localScale.x < 0.0f);


		public void Flip() => NPC.Flip();


		public HeightPos EnemyHeightPos(NpcBehaviour enemy)
		{
			float diff = Mathf.Abs(Head.position.y - enemy.Head.position.y);
			//ModAPI.Notify("Enemy Y Pos:" + diff);

			if (diff > 1.0f) return HeightPos.Laying;
			else if (diff > 0.6f) return HeightPos.Sitting;
			return HeightPos.Standing;
		}


		public float Facing => NPC.Facing;


		public void ClearAction()
		{
			NPC.CanGhost = true;
			if (PCR != null) StopCoroutine(PCR);
			NPC.Action.CurrentAction = CurrentAction = "Thinking";
			NPC.Action.ClearAction();
		}


		public int GetAttackLevel( NpcBehaviour enemy )
		{
			if (!enemy) return 0;

			float factor	= (Mojo.Traits["Mean"] + Mojo.Feelings["Angry"] * 0.05f);
			if (NPC.EnhancementMemory) factor += NPC.Memory.Opinion(enemy.NpcId);
			factor += NPC.HurtLevel;
			factor += xxx.rr(-3, 3);
			return Mathf.RoundToInt(Mathf.Clamp(factor, 1, 10));
		}


		public void ClearGrab()
		{
			if (NPC.FH.GrabAct != null)  NPC.FH.StopCoroutine(NPC.FH.GrabAct);
			if (NPC.BH.GrabAct != null)  NPC.BH.StopCoroutine(NPC.BH.GrabAct);
			
			if (NPC.FH.GrabJoint)  {
				NPC.FH.GrabJoint.enabled = false;
				UnityEngine.Object.DestroyImmediate((UnityEngine.Object)NPC.FH.GrabJoint);
			}
			if (NPC.BH.GrabJoint)  {
				NPC.BH.GrabJoint.enabled = false;
				UnityEngine.Object.DestroyImmediate((UnityEngine.Object)NPC.BH.GrabJoint);
			}
		}


		public void KickRecover()
		{
			string[] limbs = {"UpperLeg", "UpperLegFront", "LowerLeg", "LowerLegFront"};
			
			NPC.Mojo.Feel("Angry",-0.5f);

			foreach ( string limbName in limbs )
			{
				LB[limbName].ImmuneToDamage = false;
				if ( LB[limbName].RegenerationSpeed < 0.5f )
					LB[limbName].RegenerationSpeed = 0.5f;
			}

			PBO.AdrenalineLevel += 0.5f;

			NPC.GetUp = true;

		}


		public void PunchRecover()
		{
			string[] limbs = {"UpperArm", "UpperArmFront", "LowerArm", "LowerArmFront"};
			
			NPC.Mojo.Feel("Angry",-0.5f);

			foreach ( string limbName in limbs )
			{
				LB[limbName].ImmuneToDamage = false;
				LB[limbName].HealBone();

				if ( LB[limbName].RegenerationSpeed < 0.5f )
					LB[limbName].RegenerationSpeed = 0.5f;
			}

			PBO.AdrenalineLevel += 0.5f;

			NPC.GetUp = true;
		}


		public void PrepStrike( NpcBehaviour enemy, Transform EnemyTrans, Transform LimbTrans )
		{
			xxx.ToggleCollisions(EnemyTrans, LimbTrans, true, false);
			enemy.BGhost[LimbTrans] =  Time.time;
			NPC.BGhost[EnemyTrans] = Time.time;
		}


		public void CacheMoveList()
		{
			RecacheMoves = false;

			Dictionary<string, bool> conditions = new Dictionary<string, bool>()
			{
				{"karate", NPC.EnhancementKarate },
				{"knife",  NPC.HasKnife },
				{"club",   NPC.HasClub },
				{"sword",  NPC.HasSword },
				{"gun",    NPC.HasGun },
				{"nodual", NPC.FH.IsHolding && NPC.BH.IsHolding && NPC.FH.Tool == NPC.BH.Tool },
			};

			foreach ( string mlName in MoveList.Keys ) MoveList[ mlName ].Clear();

			foreach ( MoveWeight mw in MoveWeights )
			{
				bool okToAdd = true;
				if (mw.condition != "") { 
					foreach( string test in mw.condition.Split(',')) { 
						if (!conditions[test]) okToAdd = false; break; 
					}
				}
				
				if (!okToAdd) continue;
				for (int i=mw.weight; --i >= 0; ) {
					MoveList[mw.category].Add(mw.name);
				}
			}
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: PANIC       ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IPanic( NpcBehaviour enemy=null )
		{
			CurrentAction = "Panic";
			switch(xxx.rr(1,4)) {
				case 1:
					PBO.OverridePoseIndex = (int)PoseState.Flailing;
					break;

				case 2:
					PBO.OverridePoseIndex = (int)PoseState.WrithingInPain;
					break;

				default: 
					PBO.OverridePoseIndex = (int)PoseState.Stumbling;
					break;

			}

			yield return null;

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: WARN        ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IWarn( NpcBehaviour enemy=null )
		{
			CurrentAction = "Warn";
			
			if (enemy != null) NPC.MyTargets.enemy = enemy;

			PBO.DesiredWalkingDirection = 0;

			if ( NPC.MyTargets.enemy )
			{
				NpcHand hand = Hands.PickRandom();
				if (!hand.IsHolding || !hand.Tool.props.canShoot) hand = hand.AltHand;


				if (NPC.FH.IsHolding || NPC.BH.IsHolding)
				{ 

					NPC.SayRandom( "warn" );

					bool doDualGrip = false;

					hand.Tool.props.AimStyle(NPC, NPC.MyTargets.enemy);

					hand.Target( NPC.MyTargets.enemy.LB["Head"].PhysicalBehaviour);
					
					if (doDualGrip) hand.AltHand.Target(NPC.MyTargets.enemy.LB["Head"].PhysicalBehaviour);

					float timer = Time.time + xxx.rr(1,5);

					while ( Time.time < timer )
					{
						yield return new WaitForFixedUpdate();
					}

					if ( NPC.MyTargets.enemy.PBO.DesiredWalkingDirection < 1f )
					{
						NPC.SayRandom( "mercy" );
						ClearAction();
						yield break;
					}

					hand.FireAtWill = true;

					StartCoroutine(IShoot());

				}
			}

			yield return null;

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: ATTACK      ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IAttack( NpcBehaviour enemy=null, int timesToAttack = 0 )
		{
			CurrentAction = "Attack";

			LB["FootFront"].ImmuneToDamage = LB["Foot"].ImmuneToDamage = true;
			LB["LowerArm"].ImmuneToDamage  = LB["LowerArmFront"].ImmuneToDamage = true;

			if (enemy != null) NPC.MyTargets.enemy = enemy;
			
			if (enemy == null) enemy = NPC.MyTargets.enemy;
			
			if (timesToAttack == 0) timesToAttack = GetAttackLevel(enemy);

			if (RecacheMoves) CacheMoveList();

			NpcHand hand;

			while (--timesToAttack >= 0 && enemy && !NPC.NoFight && PBO.Consciousness > 0.9f && NPC.OnFeet())
			{
				enemy = NPC.MyTargets.enemy;

				if (!enemy || !NPC.IsUpright) break;

				if (!NPC.FacingToward(NPC.MyTargets.enemy))
				{
					NPC.Flip();
					yield return new WaitForSeconds(0.1f);
				}
				ClearGrab();
				yield return StartCoroutine(ISubWalkTo(enemy.Head, 0.5f, 2.0f, 7));
				ClearGrab();
				attacks.Clear();

				attacks = MoveList["all"].ToList();
				if (!enemy) break;
				if (enemy.HurtLevel > 1 && !enemy.OnFeet()) {
					if (Mathf.Abs(enemy.Head.position.y - enemy.RB["LowerBody"].position.y ) <  0.2f)
						attacks.AddRange(MoveList["down"].ToList());
					else
						attacks.AddRange(MoveList["mid"].ToList());
				} else
				{
					if (enemy.Facing == Facing) attacks.AddRange(MoveList["back"].ToList());
					attacks.AddRange(MoveList["up"].ToList());
				}

				string attackChoice = attacks.PickRandom();
				
				NPC.Action.CurrentAction = attackChoice;

				switch( attackChoice )
				{
					case "shove":
						yield return StartCoroutine(ISubWalkTo(enemy.Head));
						yield return StartCoroutine(ISubShove( enemy ));
					break;

					case "club-over":
						hand = NPC.RandomHand;
						if (!hand.IsHolding || !hand.Tool || !hand.Tool.props.Traits["club"] ) hand = hand.AltHand;
						if (!hand.IsHolding || !hand.Tool || !hand.Tool.props.Traits["club"]) continue;
						hand.ConfigHandForAiming(true);
						yield return StartCoroutine(ISubWalkTo(enemy.Head, 1.5f, 2.5f ));
						yield return StartCoroutine(ISubClubOverhead( enemy, hand, 5 ));
					break;

					case "club-jab":
						hand = NPC.RandomHand;
						if (!hand.IsHolding || !hand.Tool || !hand.Tool.props.Traits["club"] ) hand = hand.AltHand;
						if (!hand.IsHolding || !hand.Tool || !hand.Tool.props.Traits["club"]) continue;
						yield return StartCoroutine(ISubWalkTo(enemy.Head, 1f, 1.5f ));
						yield return StartCoroutine(ISubClubJab( enemy, hand, 5 ));
					break;

					case "stab":
						hand = NPC.RandomHand;
						if (!hand.IsHolding || !hand.Tool || !hand.Tool.props.canStab) hand = hand.AltHand;
						if (!hand.IsHolding || !hand.Tool || !hand.Tool.props.canStab) continue;
						yield return StartCoroutine(ISubWalkTo(enemy.Head));
						yield return StartCoroutine(ISubStab(enemy, hand));
					break;

					case "backpunch":
						if (enemy.DisableFlip) continue;
						hand = NPC.OpenHand;
						yield return StartCoroutine(ISubGrab( enemy.RB["Head"], hand));
						if (hand.IsGrabbing)
						{
							yield return StartCoroutine(ISubBackPunch( enemy, xxx.rr(10,15)));
						}
						ClearGrab();
						hand.ConfigHandForAiming(false);
					break;

					case "backstab":
						if (enemy.DisableFlip) continue;
						hand = NPC.OpenHand;
						yield return StartCoroutine(ISubGrab( enemy.RB["Head"], hand));
						if (hand.IsGrabbing)
						{
							yield return StartCoroutine(ISubBackStab( enemy, xxx.rr(10,15)));
						}
						ClearGrab();
						hand.ConfigHandForAiming(false);
					break;

					case "frontkick":
						yield return StartCoroutine(ISubWalkTo(enemy.Head, 1.0f, 2.0f, 7));
						yield return StartCoroutine(ISubFrontKick(enemy));
					break;

					case "frontpunch":
						yield return StartCoroutine(ISubWalkTo(enemy.Head, 1.0f, 3.0f, 7));
						yield return StartCoroutine(ISubFrontPunch(enemy, xxx.rr(1,5), 10));
					break;

					case "soccerkick":
						yield return StartCoroutine(ISubWalkTo(enemy.Head, 0.2f, 0.3f, 7));
						if (NPC.FacingToward(enemy.Head.position) && NPC.FacingToward( enemy.RB["Foot"].position))
							yield return StartCoroutine(ISubSoccerKick(enemy));
					break;

					case "stomp":
						yield return StartCoroutine( ISubWalkTo( enemy.RB["MiddleBody"].transform, 0.1f, 0.5f, 7));
						if (!enemy.OnFeet()) yield return StartCoroutine(ISubStomp(enemy));
					break;

					case "backchoke":
						if (!enemy.DisableFlip && NPC.MyHeight < enemy.MyHeight) { 
							yield return StartCoroutine( ISubWalkTo( enemy.Head, 0.3f, 1.2f, 7));
							yield return StartCoroutine(ISubBackChoke(enemy));
						}
					break;

					case "shoot":
						hand = NPC.RandomHand;
						if (!hand.IsHolding || !hand.Tool || !hand.Tool.props.canShoot ) hand = hand.AltHand;
						if (!hand.IsHolding || !hand.Tool || !hand.Tool.props.canShoot ) continue;
						yield return StartCoroutine(ISubShoot(enemy, hand));
					break;

				}

				DoingStrike = false;

				yield return new WaitForSeconds(xxx.rr(DelayAttack, DelayAttack + 1.5f));

				if (Mathf.Abs(Head.position.x - enemy.Head.position.x) > 3f) break;

			}

			if (!enemy || !enemy.PBO.IsAlive()) NPC.SayRandom("death");

			KickRecover();
			ClearAction();
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: WANDER      ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IWander( Transform trans = null )
		{
			CurrentAction = "Walkabout";
			float boring = 0;
			Vector3 position = Vector3.zero;

			if ( trans == null )
			{
				//	Decide a fun place to go thats not too crowded
				foreach ( NpcGadget sign in NpcGadget.AllSigns )
				{
					if (sign.Gadget == Gadgets.HealingSign)
					{
						if (NPC.HurtLevel > 3) position = sign.transform.position;
					}

					if ( sign.Gadget == Gadgets.NoEntrySign )
					{
						if (sign.SignLeft && Facing > 0) position = sign.transform.position + (Vector3.right * -Facing * 2);
						else
						{
							if ( Mathf.Abs( Head.position.x - sign.transform.position.x ) < 1 )
							{
								Flip();
								yield return new WaitForSeconds(1);
								PBO.DesiredWalkingDirection += 10f;
								yield return new WaitForSeconds(xxx.rr(3.0f, 5.0f));
							}
						}
					}
				}

				if ( position == Vector3.zero )
				{
					if (NPC.ScannedWall && NPC.WallDistance < 1f)
					{
						Flip();
						NPC.ScannedWall     = false;
						NPC.ScanTimeExpires = 0f;
						yield return new WaitForSeconds(1);
					}

					int peeps = NPC.ScannedNpc.Count;

					if (peeps > 0) { 
						if (peeps > 5)
						{
							float dist = 0, tmpx = 0;
							float myx = Head.transform.position.x;
							foreach ( NpcBehaviour nxpc in NPC.ScannedNpc )
							{
								tmpx = Mathf.Abs(myx - nxpc.Head.position.x);
								if (tmpx > dist) {
									dist = tmpx;
									position = nxpc.Head.position + (Vector3.right * -Facing * 2);
								}
							}
						} else
						{
							NpcBehaviour somenpc = NPC.ScannedNpc.PickRandom();
							
							if (somenpc && somenpc.PBO) position = somenpc.Head.position + new Vector3(xxx.rr(-2f,2f), 0f);
						}
					}

					if ( position == Vector3.zero )
					{ 
						position = new Vector3(xxx.rr(1f,8f) * -Facing, 0f);
					}
				}
				
			} else position = trans.position;

			float scanInterval = 1f;
			float wanderTime   = Time.time + xxx.rr(5f,20f);
			float scanTime     = Time.time + scanInterval;
			
			bool dualgripping  = NPC.BH.IsHolding && NPC.FH.IsHolding && NPC.FH.Tool == NPC.BH.Tool;
			if (dualgripping) StartCoroutine(ISubDualWield(wanderTime));

			while ( Time.time < wanderTime && !NPC.CancelAction )
			{
				yield return new WaitForFixedUpdate();
				

				if (PBO.DesiredWalkingDirection < 1f) {
					PBO.DesiredWalkingDirection = 10f;
					NPC.Mojo.Feel("Tired", 1);
					NPC.Mojo.Feel("Bored", -1);
				}

				if (Time.time > scanTime) { 
					
					scanTime = scanInterval + Time.time;
					NPC.ScanAhead();

					if (NPC.SvCheckForThreats()) {
						if (xxx.rr(1,30) < NPC.Mojo.Feelings["Angry"] ) break;
					}
					//NPC.DecideWhatToDo(true);
				}


				yield return new WaitForSeconds(scanInterval * 0.5f);

				boring += Time.fixedDeltaTime;

				if (xxx.rr(1,100) < NPC.Mojo.Feelings["Tired"]) break;
				if (xxx.rr(1,25) < boring) break;
			}

			ClearAction();

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: WATCH FIGHT ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IWatchFight()
		{
			CurrentAction = "Watch Fight";
			if ( !NPC.MyTargets.enemy ) { ClearAction(); yield break; }

			NpcBehaviour enemy, enemy2;

			enemy  = NPC.MyTargets.enemy;
			enemy2 = enemy.MyTargets.enemy;
			float dist;
			float prefDistMin = xxx.rr(3.0f, 6.0f);
			float prefDistMax = prefDistMin + 1.5f;
			float timeOut     = Time.time + xxx.rr(5,10);

			for ( int i = xxx.rr( 1, 4 ); --i >= 0; )
			{
				if (!enemy || !enemy2 || enemy.Actions.CurrentAction != "Attack") { ClearAction(); yield break; }

				if (enemy && !NPC.FacingToward(enemy)) Flip();
				
				while(enemy && enemy2 && (!enemy.Actions.DoingStrike || !enemy2.Actions.DoingStrike) && Time.time < timeOut)  
				{ 
					if (enemy.Actions.CurrentAction != "Attack" || enemy.MyTargets.enemy == NPC) break;
					dist = Mathf.Abs(Head.position.x - enemy.Head.position.x);

					if (dist < prefDistMin - 2.5f){      PBO.DesiredWalkingDirection = -5f; break; }
					else if (dist > prefDistMax + 2.5f){ PBO.DesiredWalkingDirection = 5f;  break; }

					if ( NPC.HasGun )
					{
						float myx = Head.position.x;
						float cdist = float.MaxValue;
						NpcBehaviour badguy = null;
						foreach ( NpcBehaviour nxpc in NPC.ScannedNpc )
						{
							if (nxpc && nxpc.PBO && nxpc.FacingToward(Head.position) && nxpc.PBO.DesiredWalkingDirection > 1.5f) 
							{ 
								float tmpx = Mathf.Abs(myx - nxpc.Head.position.x);
								if (tmpx > cdist) {
									cdist = tmpx;
									badguy = nxpc;
								}
							}
						}
						if (badguy != null && cdist < 8)
						{
							NPC.MyTargets.enemy = badguy;
							StartCoroutine(IWarn());
							yield break;
						}
					}

					yield return new WaitForSeconds(0.05f);
				}

				if (enemy && !NPC.FacingToward(enemy)) Flip();
				bool didStrike  = false;
				while(enemy && enemy2 && (enemy.Actions.DoingStrike || enemy2.Actions.DoingStrike) && Time.time < timeOut)
				{
					didStrike = true;
					if (enemy.Actions.CurrentAction != "Attack") break;
					yield return new WaitForSeconds(0.05f);
				}

				if (didStrike) NPC.SayRandom("witness");

				if (enemy && !NPC.FacingToward(enemy)) Flip();

				yield return StartCoroutine(ISubWitness());

			}

			ClearAction();
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: SHOOT       ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IShoot(bool isDefense=false)
		{
			CurrentAction = "Shoot";

			LimbBehaviour limbTarget = null;

			if (!NPC.MyTargets.enemy || !NPC.MyTargets.enemy.PBO)
			{
				ClearAction();
				yield break;
			}

			float dist;
			NPC.FH.FixHandsPose((int)PoseState.Walking);
			NPC.BH.FixHandsPose((int)PoseState.Walking);

			if (!NPC.MyTargets.enemy.MyFights.Contains(NPC)) {
				NPC.MyTargets.enemy.MyFights.Add(NPC);
			}
			if ( NPC.MyTargets.enemy && xxx.ValidateEnemyTarget(NPC.MyTargets.enemy.PBO) )
			{
				NpcHand hand = Hands.PickRandom();
				if (!hand.IsHolding || !hand.Tool.props.canShoot) hand = hand.AltHand;


				if (NPC.FH.IsHolding || NPC.BH.IsHolding)
				{ 

					if (xxx.rr(1,5) == 2) NPC.SayRandom( hand.Tool.props.Traits["handgun"] ? "handgun" : "rifle" );

					//float aimChance = CheckMyShot(NPC.MyTargets.enemy.Head, hand.Tool.T);

					bool showMercy = xxx.rr(1,100) >= (NPC.Mojo.Traits["Mean"] + NPC.Mojo.Feelings["Angry"]) / 2;
					if ( NPC.LB["Head"].IsAndroid ) showMercy = false;
					bool doDualGrip = false;

					while ( NPC.CanThink() && NPC.MyTargets.enemy && NPC.MyTargets.enemy.PBO && xxx.ValidateEnemyTarget(NPC.MyTargets.enemy.PBO) && !NPC.CancelAction )
					{
						if (NPC.MyTargets.enemy.ThreatLevel < NPC.ThreatLevel / 2 && showMercy && NPC.MyTargets.enemy.Actions.CurrentAction == "survive") {

							float igTime = Time.time + xxx.rr(5,30);

							NPC.TimedNpcIgnored[igTime] = NPC.MyTargets.enemy;
							NPC.ScanTimeExpires = 0;
							hand.Stop();
							if (NPC.MyTargets.enemy) { 
								NPC.ActivityMessage.Set("Show mercy", 5f);
								if (NPC.MyTargets.enemy.MyFights.Contains(NPC)) NPC.MyTargets.enemy.MyFights.Remove(NPC);
								if (NPC.MyFights.Contains(NPC.MyTargets.enemy)) NPC.MyFights.Remove(NPC.MyTargets.enemy);
								if (xxx.rr(1,5) == 2) NPC.SayRandom("mercy");
							}
							ClearAction();
							yield break;

						}

						hand.FireAtWill = false;
						doDualGrip      = false;


						if (!NPC.FacingToward(NPC.MyTargets.enemy.Head.position)) { NPC.Flip(); yield return new WaitForFixedUpdate(); }
						
						//
						//	Decide which hand to use
						//
						hand = Hands.PickRandom();
						if (!hand || !hand.IsHolding || !hand.Tool.props.canShoot) hand = hand.AltHand;
						else
						{
							if (hand.AltHand.IsHolding && hand.AltHand.Tool.props.canShoot && hand.Tool != hand.AltHand.Tool && xxx.rr(1,5) == 2) doDualGrip = true;
						}

						//
						//	Decide Aiming Pose
						//
						if (!hand || !hand.Tool || !hand.Tool.P) break;

						NPC.CurrentAimStyle = hand.Tool.props.AimStyle(NPC, NPC.MyTargets.enemy);

						//
						//	Decide how long to hesitate before firing
						//
						float hesitate = xxx.rr(0.5f,1.5f);


						//
						//	Decide which limb to aim for
						//
						for ( int i = 0; ++i <= 5; )
						{
							limbTarget = NPC.MyTargets.enemy.PBO.Limbs.PickRandom();
							if ( !limbTarget || !limbTarget.PhysicalBehaviour || !xxx.CanTarget( hand.Tool, limbTarget.transform ) )
							{
								limbTarget = null;
								continue;
							}
							break;
						}
						if (!limbTarget || !limbTarget.PhysicalBehaviour) break;

						hand.Target(limbTarget.PhysicalBehaviour);
						if (doDualGrip) hand.AltHand.Target(limbTarget.PhysicalBehaviour);

						//
						//	Decide what distance we should be at
						//
						int minDist       = 3;
						int maxDist       = 10;
						bool doCommonPose = false;
						float timerHesitate = 0;

						switch( NPC.CurrentAimStyle )
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
								hesitate    += 1f;
								doCommonPose = true;
								break;
						}
						float prefDistance    = xxx.rr(minDist, maxDist);
						float threshDist      = Mathf.RoundToInt((maxDist - minDist) / 2);
						bool poseLocked		  = false;

						hand.ConfigHandForAiming(true);

						NPC.ActivityMessage.Set("Shoot " + NPC.MyTargets.enemy.Config.Name + " in " + limbTarget.name, 5f);


						//
						//	Decide how long to stay in this pose
						//
						float aimTime = Time.time + xxx.rr(5,15);
						timerHesitate = Time.time + hesitate;

						while ( NPC.CanThink() && NPC.MyTargets.enemy && NPC.MyTargets.enemy.PBO && Time.time < aimTime && !NPC.CancelAction && xxx.ValidateEnemyTarget(NPC.MyTargets.enemy.PBO))
						{
							NPC.ScanTimeExpires = 0;

							dist = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);
							
							if (NPC.MyTargets.enemy.ThreatLevel < NPC.ThreatLevel / 2 && showMercy && NPC.MyTargets.enemy.Actions.CurrentAction == "survive") {

								if (xxx.rr(1,5) == 2) NPC.SayRandom("mercy");

								hand.Stop();
								if (doDualGrip) hand.AltHand.Stop();

								if (NPC.MyTargets.enemy) { 
									if (NPC.MyTargets.enemy.MyFights.Contains(NPC)) NPC.MyTargets.enemy.MyFights.Remove(NPC);
									if (NPC.MyFights.Contains(NPC.MyTargets.enemy)) NPC.MyFights.Remove(NPC.MyTargets.enemy);
								}

								if (PBO) NpcPose.Clear(PBO);
								float igTime = Time.time + xxx.rr(5,30);
								NPC.TimedNpcIgnored[igTime] = NPC.MyTargets.enemy;
								ClearAction();
								yield break;
							}


							if (dist < 0.5f) {
								hand.FireAtWill = hand.AltHand.FireAtWill = false;


								NPC.CommonPoses["frontkick"].RunMove();
								yield return new WaitForSeconds(xxx.rr(0.7f,1.0f));

								NpcPose.Clear(PBO);
								float origMass = RB["FootFront"].mass;
								float kickStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;

								RB["Foot"].mass *= (2 + kickStrength);

								if (NPC.IsUpright) { 
									xxx.FixCollisions( LB["Foot"].transform );

									Vector2 kickDir = new Vector2(-Facing, xxx.rr(-0.5f,0.5f));
									RB["Foot"].AddForce(kickDir * TotalWeight * xxx.rr(5.1f,15.1f) * kickStrength, ForceMode2D.Impulse);

									for (int i = 0; ++i <= 5;) { 
										yield return new WaitForFixedUpdate();
										RB["UpperLegFront"].velocity *= 0f;
										RB["LowerLegFront"].velocity *= 0f;
										RB["FootFront"].velocity     *= 0f;
										RB["UpperBody"].velocity     *= 0f;
										RB["LowerBody"].velocity     *= 0f;
										RB["MiddleBody"].velocity    *= 0f;
									}
								}

								RB["Foot"].mass = origMass;
								hand.FireAtWill = true;
							}

							if (!poseLocked && Mathf.Abs(dist) > prefDistance + threshDist) 
							{
							//    if (PBO.ActivePose != PBO.Poses[0]) {
							//        NpcPose.Clear(PBO);
							//    }
								if (PBO.DesiredWalkingDirection < 1f)  PBO.DesiredWalkingDirection = 10f;
							} 
							else if (!poseLocked && Mathf.Abs(dist) < prefDistance - threshDist)
							{
								NpcPose.Clear(PBO);
								if (PBO.DesiredWalkingDirection > -1f) PBO.DesiredWalkingDirection = -5f;
							}
							else
							{
								PBO.DesiredWalkingDirection = 0f;
								// if ( !poseLocked && ( CurrentAimStyle != AimStyles.Standard || !isDefense ) ) timerHesitate = Time.time + hesitate;
								if ( doCommonPose )
								{
									if ( NPC.CurrentAimStyle == AimStyles.Crouched || NPC.CurrentAimStyle == AimStyles.Rockets ) NPC.CommonPoses["crouch"].RunMove();
									else if ( NPC.CurrentAimStyle == AimStyles.Proned )
									{
										if ( !poseLocked )
										{
											if ( RB["UpperBody"] ) RB["UpperBody"].AddForce( ( NPC.MyTargets.enemy.Head.position - Head.position ).normalized * TotalWeight * 2.5f, ForceMode2D.Impulse );
											if ( RB["Foot"] ) RB["Foot"].AddForce( ( NPC.MyTargets.enemy.Head.position - Head.position ).normalized * TotalWeight * -2f, ForceMode2D.Impulse );
											yield return new WaitForFixedUpdate();
											if (!hand.FireAtWill && Time.time > timerHesitate) {
												hand.FireAtWill = true;
												if (doDualGrip) hand.AltHand.FireAtWill = true;

											}
										}
										NPC.CommonPoses["prone"].RunMove();
										if ( LB["Head"].IsOnFloor ) break;
									}
								}

								if ( NPC.CurrentAimStyle != AimStyles.Standard ) poseLocked = true;

							}
							if (!hand.FireAtWill && Time.time > timerHesitate) {
								hand.FireAtWill = true;
								if (doDualGrip) hand.AltHand.FireAtWill = true;
							}

							yield return new WaitForFixedUpdate();
						}
					}
					if (PBO) NpcPose.Clear(PBO);

					hand.Stop();
					if (doDualGrip) hand.AltHand.Stop();

					yield return new WaitForEndOfFrame();

					if (NPC.MyTargets.enemy && (!xxx.ValidateEnemyTarget(NPC.MyTargets.enemy.PBO))) { 
						if (xxx.rr(1,5) == 2) NPC.SayRandom("mercy");
						float igTime = Time.time + xxx.rr(5,30);
						NPC.TimedNpcIgnored[igTime] = NPC.MyTargets.enemy;
						hand.Stop();
						ClearAction();
						yield break;
					}

				}

				if (NPC.MyTargets.enemy) { 
					float igTime = Time.time + xxx.rr(5,30);
					NPC.TimedNpcIgnored[igTime] = NPC.MyTargets.enemy;
				}

				yield return new WaitForFixedUpdate();

				yield return new WaitForSeconds(xxx.rr(0.1f, 1f));

			}


			ClearAction();
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: IDLE WAIT   ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IWait()
		{
			float startTime = Time.time;
			float waitTime = Time.time + xxx.rr(0.5f, 2f);

			float flipTime = 0;

			int fightCount = NPC.MyFights.Count;

			float pspaceCheck = Time.time + 0.1f;

			NpcTool tool = null;
			

			float idleTime = Time.time + xxx.rr(0.1f, Mathf.Clamp(NPC.Mojo.Feelings["Tired"] * 0.1f, 0.2f, 4f));
			float miscWaitAct = Time.time + xxx.rr(1.1f,3.1f);

			while ( Time.time < waitTime && NPC.MyFights.Count == fightCount && !NPC.CancelAction )
			{
				yield return new WaitForFixedUpdate();

				if (NPC.HasClub && NPC.FH.IsHolding != NPC.BH.IsHolding) { 
				
					if (Time.time > idleTime ) {
						idleTime = Time.time + xxx.rr(0.1f, 3f);
						
						if (xxx.rr(1,3) == 2) {
							if (NPC.FH.IsHolding) {
								NPC.CommonPoses["bat_idle_fh"].RunMove();
								tool = NPC.FH.Tool; 
							}
							else {
								NPC.CommonPoses["bat_idle_bh"].RunMove();
								tool = NPC.BH.Tool;
							}
				
							//NPC.FH.Tool.R.AddForce(Vector2.down, ForceMode2D.Impulse);

							float timer = Time.time + xxx.rr(0.5f, 4.1f);
							Vector2 v2;
							v2 = Vector2.zero;
					
							while ( Time.time < timer )
							{
								v2.y += Mathf.Cos(Time.frameCount)  * 2;
								tool.R.AddForce(v2);
								yield return new WaitForFixedUpdate();
							}
						}
					}
				}

				NpcPose.Clear(PBO);

				if (Time.time > pspaceCheck) { 

					NPC.Mojo.Feel("Bored", 1);

					pspaceCheck = Time.time + 0.1f;

				}

				if ( Time.time > flipTime )
				{
					flipTime = Time.time + xxx.rr(2.5f, 5.1f);

					float flipChance = NPC.IsUpright ? 20 : 5;



					if (xxx.rr(1f,100f) <= flipChance )
					{
						NPC.Flip();
						yield return new WaitForFixedUpdate();
					} 
					else if (NPC.ScannedWall && NPC.IsUpright){

						NPC.Flip();
						yield return new WaitForFixedUpdate();
					}

				}

				if (Time.frameCount % 50 == 0 ) { 
					NPC.SvCheckForThreats(); 
					NPC.SvShoot();
					NPC.DecideWhatToDo(true);
					PBO.DesiredWalkingDirection = 0f;
				}
			}


			NPC.Mojo.Feel("Tired", -1 * Time.time - startTime);
			if (NPC.HurtLevel < 5) { 
				NPC.Mojo.Feel("Angry", -0.1f * Time.time - startTime);
				NPC.Mojo.Feel("Fear", -0.1f * Time.time - startTime);
			}
			

			ClearAction();
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: WITNESS     ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IWitness()
		{
			if ( !NPC.MyTargets.enemy )
			{
				ClearAction();
				yield break;
			}

			List<string> MyCats = new List<string>() {"witness"};

			List<string> Sayings = new List<string>();

			foreach ( string cat in MyCats )
			{
				Sayings.AddRange(NpcChat.SmackText[cat] );
			}
			
			string SmackTalk = Sayings.PickRandom();

			NPC.Say(SmackTalk, 3f);

			NPC.Mojo.Feel("Bored", -5f);
			NPC.Mojo.Feel("Fear", 1f);
			NPC.Mojo.Feel("Angry", xxx.rr(1,3));

			NPC.MyTargets.enemy.Mojo.Feel("Angry", -1f);

			if (!NPC.FacingToward(NPC.MyTargets.enemy.Head.position)) { NPC.Flip(); yield return new WaitForFixedUpdate(); }

			float dist = Head.position.x - NPC.MyTargets.enemy.Head.position.x;

			if (Mathf.Abs(dist) < 1.5f)
			{
				if (PBO.DesiredWalkingDirection > 1f) PBO.DesiredWalkingDirection = -3f;

				yield return new WaitForSeconds(0.5f);
			}


			if ( NPC.MyTargets.enemy.EnhancementMemory )
			{
				NPC.MyTargets.enemy.Memory.AddNpcStat(NPC.NpcId, "Trolled");
				NPC.MyTargets.enemy.Memory.LastContact = NPC;
				NPC.MyTargets.enemy.Mojo.Feel("Angry",3f);
			}
			
			NPC.Memory.AddNpcStat(NPC.MyTargets.enemy.NpcId, "Troll");

			yield return new WaitForFixedUpdate();

			if (!NPC.FacingToward(NPC.MyTargets.enemy.Head.position)) { NPC.Flip(); yield return new WaitForFixedUpdate(); }

			yield return StartCoroutine( ISubWitness() );

			ClearAction();

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: WITNESS       ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubWitness( int moveSet=-1 )
		{
			if (moveSet == -1) moveSet = xxx.rr(1,8);

			NpcPose[] wPose = new NpcPose[5];

			for ( int i = 1; i < wPose.Length; i++ )
			{
				wPose[i] = new NpcPose(NPC, "witness_" + i, false);
				wPose[i].Ragdoll.ShouldStandUpright       = true;
				wPose[i].Ragdoll.State                    = PoseState.Rest;
				wPose[i].Ragdoll.Rigidity                 = 5f;
				wPose[i].Ragdoll.AnimationSpeedMultiplier = 5f;
				wPose[i].Ragdoll.UprightForceMultiplier   = 2f;
				wPose[i].Import();
			};

			float seconds = 1;

			if ( moveSet > 0 && moveSet < 5 )
			{
				seconds = Time.time + xxx.rr(2.1f, 4.1f);
				wPose[moveSet].RunMove();
			} else
			{
				seconds += Time.time;
			}

			while ( Time.time < seconds ) yield return new WaitForSeconds(0.1f);
			
			NpcPose.Clear(PBO);
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: STOMP         ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubStomp( NpcBehaviour enemy, float timeOut = 3f )
		{
			DoingStrike = true;
			float origMass     = LB["FootFront"].PhysicalBehaviour.InitialMass;
			float kickStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;
			float dist         = Mathf.Abs(Head.position.x - enemy.RB["MiddleBody"].position.x);

			LB["FootFront"].ImmuneToDamage = true;

			NPC.CommonPoses["frontkick"].RunMove();
			float force = xxx.rr(1.2f,1.7f);
			RB["UpperBody"].AddForce(Vector2.up  * TotalWeight * force, ForceMode2D.Impulse);
			RB["MiddleBody"].AddForce(Vector2.up * TotalWeight * force, ForceMode2D.Impulse);
			RB["LowerBody"].AddForce(Vector2.up  * TotalWeight * force, ForceMode2D.Impulse);

			yield return new WaitForFixedUpdate();

			//Vector3 diff          = (Vector3.right * -Facing) + Vector3.down;
			//Vector3 angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg * -Facing ));

			float timer = Time.time + 0.5f;

			while ( Time.time < timer ) {
				//RB["MiddleBody"].MoveRotation(Quaternion.RotateTowards(
				//RB["MiddleBody"].transform.rotation, Quaternion.Euler(angleVelocity), 3f * Time.fixedDeltaTime * 100f));
				RB["MiddleBody"].angularVelocity = 0f;
				yield return new WaitForFixedUpdate();
			}
			NPC.CommonPoses["soccerkick"].RunMove();
			yield return new WaitForFixedUpdate();

			RB["Foot"].mass = LB["Foot"].PhysicalBehaviour.InitialMass * (2 + kickStrength);

			PrepStrike(enemy,enemy.Head.root, RB["Foot"].transform );
			
			if (enemy && !enemy.RunningSafeCollisions)
			{
				enemy.StartCoroutine(enemy.ISafeCollisions(1));
			}
			yield return new WaitForFixedUpdate();

			NpcPose.Clear(PBO);

			RB["Foot"].AddForce(Vector2.down * TotalWeight * kickStrength *  xxx.rr(5.0f,9.9f), ForceMode2D.Impulse);

			yield return new WaitForSeconds(1f);
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: SOCCER KICK   ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubSoccerKick( NpcBehaviour enemy, float timeOut = 3f )
		{
			DoingStrike = true;
			LB["Foot"].ImmuneToDamage = true;
			NPC.CommonPoses["soccerkick"].RunMove();
			yield return new WaitForSeconds(xxx.rr(0.5f,1.2f));
				
			NPC.CanGhost = false;

			NpcPose.Clear(PBO);

			yield return new WaitForFixedUpdate();

			timeOut += Time.time;

			float origMass     = LB["Foot"].PhysicalBehaviour.InitialMass;
			float kickStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;
			float dist         = Mathf.Abs(Head.position.x - enemy.Head.position.x);
			
			if (!enemy || dist > 1f) { DoingStrike = false; yield break; }
			
			PrepStrike(enemy,enemy.Head.root, RB["FootFront"].transform );
			PrepStrike(enemy,enemy.Head.root, RB["LowerLegFront"].transform );

			RB["FootFront"].mass = LB["Foot"].PhysicalBehaviour.InitialMass * (1 + kickStrength);
			RB["Foot"].mass = LB["Foot"].PhysicalBehaviour.InitialMass * (2 + kickStrength);

			if (NPC.IsUpright) {
				if (NPC.EnhancementTroll && xxx.rr(1,7) == 2)
				{
					NPC.Say("Kiss my Converse!", 2);
				}
					
				yield return new WaitForFixedUpdate();
				Vector2 kickDir = new Vector2(1.1f * -Facing, -0.3f);

				RB["FootFront"].AddForce(new Vector2(1.2f * -Facing, -0.2f) * TotalWeight * 1f , ForceMode2D.Impulse);
				RB["Foot"].velocity     *= 0.01f;
				RB["LowerLeg"].velocity *= 0.01f;
				RB["UpperLeg"].velocity *= 0.01f;

				RB["MiddleBody"].AddForce(new Vector2(1.2f * Facing, 0.2f) * TotalWeight , ForceMode2D.Impulse);
				yield return new WaitForFixedUpdate();

				kickDir.y += 0.1f;

				RB["FootFront"].AddForce(new Vector2(1.6f * -Facing, 0.1f) * TotalWeight * 1f , ForceMode2D.Impulse);
				RB["Foot"].velocity     *= 0.01f;
				RB["LowerLeg"].velocity *= 0.01f;
				RB["UpperLeg"].velocity *= 0.01f;

				RB["MiddleBody"].AddForce(new Vector2(1.2f * Facing, 0.2f) * TotalWeight , ForceMode2D.Impulse);
				yield return new WaitForFixedUpdate();

				RB["FootFront"].AddForce(new Vector2(3.5f * -Facing, 0.1f) * TotalWeight * 1f , ForceMode2D.Impulse);
				RB["Foot"].velocity     *= 0.01f;
				RB["LowerLeg"].velocity *= 0.01f;
				RB["UpperLeg"].velocity *= 0.01f;

				RB["MiddleBody"].AddForce(new Vector2(1.2f * Facing, 0.2f) * TotalWeight , ForceMode2D.Impulse);
				yield return new WaitForFixedUpdate();

				RB["FootFront"].AddForce(new Vector2(3.5f * -Facing, 1.1f) * TotalWeight * 1f , ForceMode2D.Impulse);
				RB["Foot"].velocity     *= 0.01f;
				RB["LowerLeg"].velocity *= 0.01f;
				RB["UpperLeg"].velocity *= 0.01f;

			}

			timeOut = Time.time + 5f;

			while ( !LB["FootFront"].IsOnFloor && Time.time < timeOut )
			{
				yield return new WaitForSeconds(0.5f);
			}
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: FRONT KICK    ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubFrontKick( NpcBehaviour enemy, float timeOut = 5f )
		{
			if (!enemy) yield break;
			
			float dist = HeadDistance(enemy);

			if (dist > 4f) yield break;

			DoingStrike = true;
			NPC.CommonPoses["frontkick"].RunMove();
			PrepStrike(enemy,enemy.Head.root, RB["LowerArmFront"].transform );
			yield return new WaitForSeconds(xxx.rr(0.5f,1.0f));

			PrepStrike(enemy,enemy.Head.root, RB["LowerArmFront"].transform );
			
			for(int i = xxx.rr(1,5); --i >= 0;) { 
				if ( dist > 2f )
				{
					RB["UpperBody"].AddForce(new Vector3(-Facing, -1) * TotalWeight * 3.0f, ForceMode2D.Impulse);
				}
				else if ( dist < 0.5f )
				{
					RB["UpperBody"].AddForce(new Vector3(Facing, -1) * TotalWeight * 2.0f, ForceMode2D.Impulse);
				}
				yield return new WaitForSeconds(xxx.rr(0.1f,0.3f));
				dist = HeadDistance(enemy);
				PrepStrike(enemy,enemy.Head.root, RB["LowerArmFront"].transform );
			}
			float timer = Time.time + 1f;
			bool ok2kick = false;
			while ( Time.time < timer )
			{
				if ( xxx.IsColliding( RB["Foot"].transform, enemy.Head.root, false))
				{
					RB["Foot"].AddForce(Vector2.right * Facing * TotalWeight * Time.fixedDeltaTime * 100f);
				} else
				{
					ok2kick = true;
					break;
				}
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}


			if ( ok2kick && dist < 2f && dist > 0.3f)
			{
				LB["Foot"].ImmuneToDamage     = LB["FootFront"].ImmuneToDamage     = true;
				LB["LowerLeg"].ImmuneToDamage = LB["LowerLegFront"].ImmuneToDamage = true;
				LB["UpperLeg"].ImmuneToDamage = LB["UpperLegFront"].ImmuneToDamage = true;

				NpcPose.Clear(PBO);

				yield return new WaitForFixedUpdate();

				float origMass = LB["Foot"].PhysicalBehaviour.InitialMass;

				float kickStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;

				//RB["Foot"].mass = RB["Foot"].mass = LB["Foot"].PhysicalBehaviour.InitialMass * (kickStrength);

				if (NPC.IsUpright && (Time.time - LastTimeHit > 0.5f)) { 
					
					PrepStrike(enemy,enemy.Head.root, RB["Foot"].transform );
					Vector2 kickDir = new Vector2(-NPC.Facing, xxx.rr(-0.5f,0.5f));
					RB["Foot"].AddForce(kickDir * NPC.TotalWeight * xxx.rr(3.1f,6.1f) * kickStrength, ForceMode2D.Impulse);
					NPC.audioSource.enabled = true;
					
					if (xxx.rr(1,100) <= NPC.Config.KarateVocalsChance) { 
						if (!LB["Head"].IsAndroid) NPC.audioSource.PlayOneShot(NpcMain.GetSound("k"),1f);
						else NPC.audioSource.PlayOneShot(NpcMain.GetSound("rk"),2f);
					}

					for (int i = 0; ++i <= 5;) { 
						yield return new WaitForFixedUpdate();
						RB["UpperLegFront"].velocity *= 0f;
						RB["LowerLegFront"].velocity *= 0f;
						RB["FootFront"].velocity     *= 0f;
						RB["UpperBody"].velocity     *= 0f;
						RB["LowerBody"].velocity     *= 0f;
						RB["MiddleBody"].velocity    *= 0f;
					}
				}

				if (enemy && !enemy.RunningSafeCollisions)
				{
					enemy.StartCoroutine(enemy.ISafeCollisions());
				}

				timeOut = Time.time + 5f;

				while ( !LB["Foot"].IsOnFloor && Time.time < timeOut )
				{
					yield return new WaitForSeconds(0.5f);
				}

				RB["Foot"].mass = LB["Foot"].PhysicalBehaviour.InitialMass;
				
			}

			NpcPose.Clear(PBO);

			NPC.CanGhost = true;

			LB["UpperLegFront"].Broken = false;
			LB["LowerLegFront"].Broken = false;

			Invoke("KickRecover", 1.0f);
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: FRONT PUNCH   ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubFrontPunch( NpcBehaviour enemy, int punchCount = 1, float timeOut = 5f )
		{
			while ( --punchCount >= 0 )
			{
				if (!enemy) yield break;

				if (!NPC.FacingToward(enemy)) yield break;
			
				float dist = Mathf.Abs(Head.position.x - enemy.Head.position.x);

				if (dist > 5f) yield break;

				DoingStrike = true;

				bool doRev = xxx.rr(1,3)==2;

				NpcHand hand = doRev ? NPC.BH:NPC.FH;

				string rt = doRev ? "" : "Front";

				NpcPose PunchPose1 = new NpcPose(NPC, "punch_1_" +( doRev ? "bh":"fh"), false );
				NpcPose PunchPose2 = new NpcPose(NPC, "punch_2_" +( doRev ? "bh":"fh"), false );

				PunchPose1.Import();
				PunchPose2.Import();

				float timer = Time.time + 0.1f;
				timeOut += Time.time;

				while ( Time.time < timeOut )
				{
					PunchPose1.CombineMove();
					if (dist > 1.5f)      PBO.DesiredWalkingDirection = 2f;
					else if (dist < 0.1f) PBO.DesiredWalkingDirection = -2f;
					else if (Time.time > timer) break;
					yield return new WaitForFixedUpdate();
					dist = Mathf.Abs(Head.position.x - enemy.Head.position.x);
				}
			
				PunchPose1.RunMove();
				yield return new WaitForSeconds(xxx.rr(0.1f,0.3f));

				PrepStrike(enemy,enemy.Head.root, RB["LowerArm" + rt].transform );
			
				PunchPose2.RunMove();
			
				LB["LowerArm" + rt].Broken         = LB["UpperArm" + rt].Broken         = true;
				LB["LowerArm" + rt].ImmuneToDamage = LB["UpperArm" + rt].ImmuneToDamage = true;
				string target                      = "Head";
				if (enemy.IsUpright) target        = new string[] {"Head", "UpperBody", "MiddleBody", "LowerBody"}.PickRandom();

				float strength = (NPC.Mojo.Stats["Strength"] * 0.05f) + xxx.rr(1.0f, 2.0f);

				Vector2 dir = (enemy.RB[target].position - RB["LowerArm" + rt].position).normalized;
				timer = Time.time + 0.1f;
				while ( Time.time < timer && Mathf.Abs(enemy.Head.transform.position.x - Head.position.x) < 1.5f)
				{
					RB["UpperBody"].AddForce( Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * 20f * strength );
					RB["LowerBody"].AddForce( Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * 20f * strength );
					RB["MiddleBody"].AddForce( Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * 20f * strength );
					RB["LowerArm" + rt].AddForce( dir * TotalWeight * Time.fixedDeltaTime * 100f * strength );
					yield return new WaitForFixedUpdate();
				}

				if (NPC.FacingToward(enemy) && Mathf.Abs(enemy.Head.transform.position.x - Head.position.x) < 1.5f && Time.time < timeOut)
				{
					RB["LowerArm" + rt].mass *= 2f;
					timer = Time.time + 0.2f;
					float dist2;

					if (hand.IsHolding)
					{
						hand.Tool.PrepWeaponStrike(enemy, enemy.Head.root);
					}

					HeightPos hps = EnemyHeightPos(enemy);
					if (hps == HeightPos.Laying) {
						NpcPose.Clear(PBO);
						NPC.CanGhost = true;
						RB["LowerArm"].mass      = LB["LowerArm"].PhysicalBehaviour.InitialMass;
						RB["LowerArmFront"].mass = LB["LowerArmFront"].PhysicalBehaviour.InitialMass;
						DoingStrike = false;
						Invoke("PunchRecover", 1.0f);
						yield break;
					}


					PrepStrike(enemy,enemy.Head.root, RB["LowerArm" + rt].transform );
					PrepStrike(enemy,enemy.Head.root, RB["MiddleBody"].transform );
					while (Time.time < timer) { 
						dist2 = Mathf.Abs((enemy.RB[target].position - RB["LowerArm" + rt].position).magnitude);
						if (dist2 < 0.5f && dist2 > 0.3f)
						{
							if (hps == HeightPos.Standing) RB["LowerArm" + rt].AddForce(Vector2.right * -Facing  * TotalWeight * strength, ForceMode2D.Impulse);
							else
							{
								dir = (enemy.RB["Head"].position - RB["LowerArm" + rt].position).normalized;
								RB["LowerArm" + rt].AddForce(dir * TotalWeight * TotalWeight * strength, ForceMode2D.Impulse);
							}
						
							break;
						}
						RB["LowerArm" + rt].AddForce(dir * TotalWeight * Time.fixedDeltaTime * 500f);
						yield return new WaitForFixedUpdate();
					}
				}
				LB["LowerArm" + rt].Broken         = LB["UpperArm" + rt].Broken         = false;

				yield return new WaitForSeconds(xxx.rr(0.1f,0.3f));
				NpcPose.Clear(PBO);
			
				NPC.CanGhost = true;

				DoingStrike = false;

				Invoke("PunchRecover", 1.0f);

			}

			RB["LowerArm"].mass      = LB["LowerArm"].PhysicalBehaviour.InitialMass;
			RB["LowerArmFront"].mass = LB["LowerArmFront"].PhysicalBehaviour.InitialMass;

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: BACK CHOKE    ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubBackChoke( NpcBehaviour enemy, float timeOut = 10f )
		{
			DoingStrike = true;
			Vector2 dist       = (enemy.RB["UpperBody"].position - RB["LowerBody"].position) + Vector2.up;

			if (dist.sqrMagnitude > 6f) yield break;


			NPC.CommonPoses["frontkick"].RunMove();
			float force = 1f;
			RB["UpperBody"].AddForce(dist * TotalWeight * force, ForceMode2D.Impulse);
			RB["MiddleBody"].AddForce(dist * TotalWeight * force, ForceMode2D.Impulse);
			RB["LowerBody"].AddForce(dist * TotalWeight * force, ForceMode2D.Impulse);

			Dictionary<string, int> limbLayers = new Dictionary<string, int>();

			foreach ( string k in LB.Keys )
			{
				limbLayers[k] = LB[k].PhysicalBehaviour.spriteRenderer.sortingOrder;
			}		

			yield return new WaitForFixedUpdate();

			Vector3 diff          = Vector3.right + Vector3.down;
			Vector3 angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diff.y, diff.x)  * Mathf.Rad2Deg  ));
			float timer           = Time.time + 0.5f;
			bool collided         = false;
			
			NpcPose BackChokePose = new NpcPose(NPC,"backchoke_1");
			BackChokePose.RunMove();

			while ( Time.time < timer ) {

				yield return new WaitForFixedUpdate();

				if ( xxx.IsTouching( LB["UpperArmFront"].PhysicalBehaviour, enemy.LB["Head"].PhysicalBehaviour ) &&
					 xxx.IsTouching( LB["UpperArmFront"].PhysicalBehaviour, enemy.LB["UpperBody"].PhysicalBehaviour ) &&
					 (xxx.IsTouching( LB["LowerBody"].PhysicalBehaviour, enemy.LB["UpperBody"].PhysicalBehaviour ) || 
					 xxx.IsTouching( LB["LowerBody"].PhysicalBehaviour, enemy.LB["MiddleBody"].PhysicalBehaviour )))
				{ collided = true; break; }
			}

			if ( !collided || enemy.Facing != Facing)
			{
				NpcPose.Clear(PBO);
				yield break;
			}

			HingeJoint2D ArmJoint = null, BodyJoint = null, FootJoint = null;
			FixedJoint2D LegJoint = null;
			DistanceJoint2D BJoint = null;

			enemy.DisableFlip                     = true;
			NPC.DisableFlip                       = true;

			ArmJoint = RB["UpperArmFront"].gameObject.AddComponent<HingeJoint2D>();
			ArmJoint.useLimits = true;
			ArmJoint.connectedBody = enemy.RB["Head"];
			ArmJoint.breakForce = 1000f;
			ArmJoint.breakTorque = 1000f;
			ArmJoint.enabled = true;

			LB["UpperArmFront"].PhysicalBehaviour.spriteRenderer.sortingOrder = 
			LB["LowerLegFront"].PhysicalBehaviour.spriteRenderer.sortingOrder = 
			LB["LowerBody"].PhysicalBehaviour.spriteRenderer.sortingOrder = 
			LB["LowerArmFront"].PhysicalBehaviour.spriteRenderer.sortingOrder =
			LB["UpperLegFront"].PhysicalBehaviour.spriteRenderer.sortingOrder = 25;

			yield return new WaitForFixedUpdate();

			NpcPose BackChokePose2 = new NpcPose(NPC,"backchoke_2");
			BackChokePose2.RunMove();
			bool lockedLeg = false;

			LB["LowerArmFront"].PhysicalBehaviour.spriteRenderer.sortingOrder = -10;
			float xout;
			xout = Time.time + xxx.rr(2f,5f);


			while ( Time.time < xout )
			{
				if (PBO.Consciousness < 0.8f) break;
				if ( !lockedLeg && xxx.IsTouching( LB["FootFront"].PhysicalBehaviour, enemy.LB["UpperLegFront"].PhysicalBehaviour ) )
				{
					lockedLeg              = true;
					LegJoint               = RB["LowerLegFront"].gameObject.AddComponent<FixedJoint2D>();
					LegJoint.connectedBody = enemy.RB["UpperLegFront"];
					LegJoint.breakForce    = 1000f;
					LegJoint.breakTorque   = 1000f;
					LegJoint.enabled       = true;
					break;
				}

				yield return new WaitForFixedUpdate();
			}
			
			if (lockedLeg)
			{
				if (xxx.rr(1,3) == 2) {
					enemy.Actions.ClearAction();
					if (enemy.Actions.PCR != null) enemy.Actions.StopCoroutine(enemy.Actions.PCR);
					enemy.Actions.StartCoroutine(enemy.Actions.IPanic());
				}
				xout = Time.time + xxx.rr(5f,15f);
				bool chokingOut = false;
				while ( Time.time < xout )
				{
					if (PBO.Consciousness < 0.8f) break;
					enemy.PBO.OxygenLevel -= Time.fixedDeltaTime * 0.3f;
					if (enemy.PBO.OxygenLevel <= 0.001) {
						enemy.PBO.Consciousness -= Time.fixedDeltaTime * 0.1f ;
						if (!chokingOut)
						{
							chokingOut = true;
							NPC.SayRandom("choke");
						}
					}
					yield return new WaitForFixedUpdate();
				}
			}
			
			yield return new WaitForFixedUpdate();

			NpcPose.Clear(PBO);
			if (BodyJoint) UnityEngine.Object.Destroy(BodyJoint);
			if (BJoint) UnityEngine.Object.Destroy(BJoint);
			if (ArmJoint) UnityEngine.Object.Destroy(ArmJoint);
			if (LegJoint) UnityEngine.Object.Destroy(LegJoint);
			if (FootJoint) UnityEngine.Object.Destroy(FootJoint);
			yield return new WaitForSeconds(1);
			NPC.DisableFlip = false;
			if (enemy) enemy.DisableFlip = false;
			
			
			foreach ( string k in LB.Keys )
			{
				LB[k].PhysicalBehaviour.spriteRenderer.sortingOrder = limbLayers[k];
			}


		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: BACK STAB     ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubBackStab( NpcBehaviour enemy, float timeOut = 10f )
		{
			DoingStrike = true;

			timeOut += Time.time;

			hand	= NPC.BH.IsGrabbing ? NPC.FH : NPC.BH;

			float strength = (NPC.Mojo.Stats["Strength"] / 100) + 1;

			NpcTool knife = hand.Tool;

				
			NPC.CanGhost                    = false;
			enemy.CanGhost                  = false;
				
			//xxx.ToggleCollisions(knife.T,enemy.Head.root, true, false);
			knife.PrepWeaponStrike(enemy, enemy.Head.root);
				
			enemy.DisableFlip               = true;
			NPC.DisableFlip                 = true;

			int stabs = xxx.rr(2,4);
			float timer;
			while ( hand.AltHand.IsGrabbing && hand.AltHand.GrabJoint && --stabs >= 0)
			{
				knife.PrepWeaponStrike(enemy, enemy.Head.root);
				if (xxx.rr(1,50) == 10) NPC.MyTargets.enemy.SayRandom("grabbed");

				if (Facing != enemy.Facing) break;
					
				timer = Time.time + 0.5f;
				while ( Time.time < timer )
				{
					hand.RB.AddForce(Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * 500f * strength);
					yield return new WaitForFixedUpdate();
				}

				timer = Time.time + 0.3f;
				while ( Time.time < timer )
				{
					hand.RB.AddForce(Vector2.down * TotalWeight * Time.fixedDeltaTime * 500f);
					yield return new WaitForFixedUpdate();
				}
				timer = Time.time + 0.3f;
				while ( Time.time < timer )
				{
					hand.RB.AddForce(Vector2.right * Facing * TotalWeight * Time.fixedDeltaTime * 200f);
					yield return new WaitForFixedUpdate();
				}

				yield return new WaitForSeconds(xxx.rr(0.5f,2.5f));
			}


			if (enemy) { 
				enemy.DisableFlip = false;
				enemy.CanGhost    = true;
            }

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: BACK PUNCH    ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubBackPunch( NpcBehaviour enemy, float timeOut=10f )
		{
			DoingStrike = true;

			timeOut += Time.time;

			hand			= NPC.FH.IsGrabbing ? NPC.FH : NPC.BH;
			NPC.CanGhost    = false;
			enemy.CanGhost  = false;
				
			if (hand.AltHand.IsHolding)
			{
				//xxx.ToggleCollisions(hand.AltHand.Tool.T,enemy.Head.root, true, false);
				hand.AltHand.Tool.PrepWeaponStrike(enemy, enemy.Head.root);
			}

			//xxx.ToggleCollisions(hand.T,enemy.Head, true, false);
			PrepStrike(enemy,enemy.Head.root, hand.AltHand.T);
				
			NPC.MyTargets.enemy.DisableFlip = true;
			NPC.DisableFlip                 = true;
			PBO.DesiredWalkingDirection     = -5f;
            hand.RB.mass                   *= 2f;

			float pulldownTime  = Time.time + xxx.rr(0.1f, 2f);
			float punchTime     = Time.time + xxx.rr(1f, 4f);
			int punchPower		= 0;
			float strength      = (NPC.Mojo.Stats["Strength"] * 0.05f) + xxx.rr(1, 3);

			LB["LowerArm"].ImmuneToDamage      =
			LB["LowerArmFront"].ImmuneToDamage = 
			LB["UpperArm"].ImmuneToDamage      = 
			LB["UpperArmFront"].ImmuneToDamage = true;

			while (hand.GrabJoint && NPC.HurtLevel < 6 && enemy.LB["Head"].IsConsideredAlive && !enemy.LB["Head"].IsDismembered && Time.time < timeOut)
            {
				if (Facing != enemy.Facing) break;
					
					
				PBO.DesiredWalkingDirection = -20f;

				if (Time.time > pulldownTime) {
					hand.RB.AddForce(Vector2.right * TotalWeight * xxx.rr(-5.1f, 5.1f), ForceMode2D.Impulse);
					pulldownTime = Time.time + xxx.rr(1.1f, 3f);
				}

				//xxx.ToggleCollisions(hand.AltHand.T, enemy.Head.root, true, false);
	
				NPC.Mojo.Feel("Angry",-1f);
				if (Time.time > punchTime)
				{
					float timer = Time.time + 0.05f;
					while ( Time.time < timer )
					{
						PrepStrike(enemy,enemy.Head.root, hand.AltHand.T);
						hand.AltHand.RB.AddForce(Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * strength * 100);
						yield return new WaitForFixedUpdate();
					}

					if ( ++punchPower < 2 )
					{
						PrepStrike(enemy,enemy.Head.root, hand.AltHand.T);
	
						hand.AltHand.RB.AddForce(Vector2.right * -Facing * TotalWeight * strength,ForceMode2D.Impulse);
						if (xxx.rr(1,50) == 10) NPC.MyTargets.enemy.SayRandom("grabbed");

					}else
					{
						punchPower = 0;

						punchTime = Time.time + xxx.rr(1.1f, 3f);
					}
				}
				else
				{

					hand.AltHand.LB.InfluenceMotorSpeed(Mathf.DeltaAngle(
				hand.AltHand.LB.Joint.jointAngle, -95.21517f * NPC.PBO.AngleOffset), 15f);

					hand.AltHand.uArmL.InfluenceMotorSpeed(Mathf.DeltaAngle(
				hand.AltHand.uArmL.Joint.jointAngle, 57.40377f * NPC.PBO.AngleOffset), 15f);

				
				}

				yield return new WaitForFixedUpdate();
			}

			
			
			PBO.DesiredWalkingDirection = 0;
			
			NPC.RunRigids(NPC.RigidMass, -1f);

			LB["LowerArm"].ImmuneToDamage      =
			LB["LowerArmFront"].ImmuneToDamage = 
			LB["UpperArm"].ImmuneToDamage      = 
			LB["UpperArmFront"].ImmuneToDamage = false;

			NPC.CanGhost        = true;
			NPC.DisableFlip		= false;
			
			if (hand.AltHand.IsHolding)
			{

			}


			NPC.LB["LowerArmFront"].Broken = NPC.LB["LowerArm"].Broken = NPC.LB["UpperArmFront"].Broken = NPC.LB["UpperArm"].Broken = false;

			if (enemy) { 
				enemy.DisableFlip = false;
				enemy.CanGhost    = true;
            }
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: GRAB          ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubGrab( Rigidbody2D rb, NpcHand hand, float timeOut=10f )
		{
			timeOut += Time.time;
			float dist;

			hand.GrabAct  = hand.StartCoroutine(hand.IGrab(NPC.MyTargets.enemy.RB["Head"]));

			while ( !hand.IsGrabbing && rb && Time.time < timeOut )
            {
				dist	= Mathf.Abs(Head.position.x - rb.position.x);
				if (dist < 0.5f) PBO.DesiredWalkingDirection	  = -4f;
				else if (dist > 1.0f) PBO.DesiredWalkingDirection = 4f;
				else PBO.DesiredWalkingDirection                  = 0f;
				yield return new WaitForSeconds(DelayWalk);
            }
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: SHOOT         ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubShoot( NpcBehaviour enemy, NpcHand hand, float timeOut=10f )
		{
			timeOut += Time.time;

			hand.ConfigHandForAiming(true);

			Vector2 dir;
			string targetstr = "Head";
			if (enemy.IsUpright) targetstr = new string[] {"Head", "UpperBody", "MiddleBody", "LowerBody"}.PickRandom();
			Transform target = enemy.RB[targetstr].transform;

			float timer = xxx.rr(0.5f,1f) + Time.time;
			hand.LB.Broken = true;
			
			do { 
				dir = Vector2.MoveTowards(hand.T.position, target.position, 3f * Time.fixedDeltaTime);
				hand.RB.MovePosition(dir);
				yield return new WaitForFixedUpdate();
			} while (-Facing * hand.T.position.x < -Facing * target.position.x && Time.time < timer);
			if (enemy) {
				hand.Tool.Activate(hand.Tool.props.isAutomatic);
			}

			yield return new WaitForSeconds(xxx.rr(0.1f,1.0f));

			hand.ConfigHandForAiming(true);
			hand.LB.Broken = false;

		}

		
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: SHOVE         ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubShove( NpcBehaviour enemy )
		{
			DoingStrike = true;
			Transform enemyHead = enemy.Head;

            NPC.LB["LowerArm"].ImmuneToDamage =
			NPC.LB["LowerArmFront"].ImmuneToDamage =
			NPC.LB["UpperArm"].ImmuneToDamage =
			NPC.LB["UpperArmFront"].ImmuneToDamage = true;

			float dist = Mathf.Abs(Head.position.x - enemyHead.position.x);

			float strength = (NPC.Mojo.Stats["Strength"] * 0.05f) + xxx.rr(0.5f, 1.5f);
			
			float minTime = Time.time + xxx.rr(0.5f, 1.0f);
			float Timeout = Time.time + 5f;

			while (enemy && Mathf.Abs(Head.position.y - enemyHead.position.y) > 1f && Time.time < Timeout) 
			{ 
				NPC.CommonPoses["push_1"].CombineMove();
				yield return new WaitForFixedUpdate();
			}

			if (!enemy || Mathf.Abs(Head.position.y - enemyHead.position.y) > 1f ) yield break;

			while ( Time.time < minTime )
			{
				NPC.CommonPoses["push_1"].CombineMove();
				yield return new WaitForFixedUpdate();
			}
			
			PrepStrike(enemy,enemy.Head.root, RB["LowerArm"].transform );
			PrepStrike(enemy,enemy.Head.root, RB["LowerArmFront"].transform );
			
			if (!NPC.FacingToward(enemyHead.position)) { yield break; }

			NPC.CommonPoses["push_2"].RunMove();

			float timer = Time.time + xxx.rr(0.2f,1.0f);

			yield return new WaitForSeconds(xxx.rr(0.01f,0.1f));
			
			bool madeContact = false;

			Transform h1 = NPC.FH.T, h2 = NPC.BH.T, enemyRoot = enemyHead.root;

			for ( int i = 0; i < 3; i++ )
			{
				RB["UpperBody"].AddForce(Vector2.right * -Facing * TotalWeight * (strength + 5f));
				RB["LowerBody"].AddForce(Vector2.right * -Facing * TotalWeight * (strength + 5f));
				RB["LowerArm"].AddForce(Vector2.right * -Facing * TotalWeight *  (strength + 5f));
				RB["LowerArmFront"].AddForce(Vector2.right * -Facing * TotalWeight * (strength + 5f));

				if (xxx.IsColliding(h1, enemyRoot, false) || xxx.IsColliding(h2, enemyRoot, false)) madeContact = true;

				yield return new WaitForFixedUpdate();
			}

			if (madeContact)
			{
				if (xxx.rr(1,3)==2) RB["LowerArm"].AddForce(Vector2.right * -Facing * TotalWeight *  strength, ForceMode2D.Impulse );
				if (xxx.rr(1,3)==2) RB["LowerArmFront"].AddForce(Vector2.right * -Facing * TotalWeight * strength, ForceMode2D.Impulse );
				
				enemy.Actions.LastTimeHit = Time.time;

				timer = Time.time + 0.2f;
				while ( Time.time < timer )
				{
					if(Time.time - LastTimeHit < 0.01f) break;
					RB["UpperBody"].velocity  *= 0.5f;
					RB["LowerBody"].velocity  *= 0.5f;
					RB["MiddleBody"].velocity *= 0.5f;
					yield return new WaitForFixedUpdate();
				}
				NPC.Memory.AddNpcStat(NPC.MyTargets.enemy.NpcId, "HitThem");
				enemy.Memory.AddNpcStat(NPC.NpcId, "HitMe");
			}

			NpcPose.Clear(NPC.PBO);

			NPC.CanGhost = NPC.MyTargets.enemy.CanGhost = true;
			
			NPC.RunRigids(NPC.RigidMass, -1f);
			
			NPC.Mojo.Feel("Angry",-1f);
			enemy.Mojo.Feel("Angry",12f);
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: STAB          ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubStab( NpcBehaviour enemy, NpcHand hand, float timeOut=7f )
		{
			DoingStrike = true;
			timeOut += Time.time;

			NpcPose stabPose = new NpcPose(NPC,"stab_" + hand.HandShortId, false);

			float Timer = Time.time + xxx.rr(0.5f, 2f);

			float strength = (NPC.Mojo.Stats["Strength"] * 10);

			NpcTool knife = hand.Tool;


			yield return new WaitForSeconds(xxx.rr(0.5f,1.0f));
				
			//xxx.ToggleCollisions(knife.T,trans.root, true, false);
			knife.PrepWeaponStrike(enemy, enemy.Head.root);
				
			NPC.DisableFlip                 = true;

			while ( Time.time < timeOut && (Time.time < Timer || Mathf.Abs(hand.T.position.x - enemy.Head.position.x) > 1f ))
			{
				stabPose.CombineMove();
				yield return new WaitForFixedUpdate();
			}

			NpcPose.Clear(PBO);

			Timer = Time.time + xxx.rr(0.1f, 0.3f);
			while (Time.time < Timer) { 
				hand.RB.AddForce(Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * (strength + xxx.rr(10,100)));
				yield return new WaitForFixedUpdate();
			}
			
			yield return new WaitForSeconds(xxx.rr(0.5f, 2f));

			Timer = Time.time + 0.2f;
			while (Time.time < Timer) { 
				hand.RB.AddForce(Vector2.right * Facing * TotalWeight * Time.fixedDeltaTime * 400f);
				yield return new WaitForFixedUpdate();
			}

			yield return new WaitForSeconds(xxx.rr(0.5f, 1f));

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: CLUB OVERHEAD   -------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubClubOverhead( NpcBehaviour enemy, NpcHand hand, float timeOut=7f )
		{
			DoingStrike = true;
			hand.swingState = SwingState.preswing;

			hand.ConfigHandForAiming(true);
			float swingStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;

			NpcTool club = hand.Tool;


			SwingStyle swingStyle = SwingStyle.over;

			NpcPose swingPose = new NpcPose(NPC, "club_" + swingStyle.ToString() + "_" + hand.HandShortId, false);
			swingPose.Import();

			float timer = Time.time + xxx.rr(0.5f,1.0f);
			float dist  = Mathf.Abs(Head.position.x - enemy.Head.position.x);

			while ( Time.time < timer )
			{
				swingPose.CombineMove();
				if (dist > 3.5f)      PBO.DesiredWalkingDirection = 2f;
				else if (dist < 1f) PBO.DesiredWalkingDirection = -2f;
				else if (Time.time > timer) break;
				yield return new WaitForFixedUpdate();
				dist = Mathf.Abs(Head.position.x - enemy.Head.position.x);
			}

			hand.swingState = SwingState.cocked;
			club.P.MakeWeightful();

			//hand.LB.Broken = hand.uArmL.Broken = true;
			//NpcPose.Clear(NPC.PBO);
			

			yield return new WaitForFixedUpdate();

			club.PrepWeaponStrike(enemy, enemy.Head.root);

			hand.swingState = SwingState.swing;

			float Timeout = Time.time + xxx.rr(5,10);
			
			
			if (!enemy) { yield break;}
			
			
			club.PrepWeaponStrike(enemy, enemy.Head.root);
			
			timer = Time.time + 0.1f;
			club.P.MakeWeightful();

			while (Time.time < timer) { 
				club.R.AddForce(Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * 300f);
				//hand.RB.AddForce(Vector2.up * TotalWeight * Time.fixedDeltaTime * 300f);
				yield return new WaitForFixedUpdate();
			}
				
			yield return new WaitForFixedUpdate();

			Vector3 dir = (enemy.Head.position - Head.position).normalized;
			club.PrepWeaponStrike(enemy, enemy.Head.root);
			club.R.AddForce(((Vector3.right * -Facing) + Vector3.down + dir).normalized * TotalWeight * 2f * swingStrength, ForceMode2D.Impulse);
			//RB["UpperBody"].mass *= 2;
			RB["UpperBody"].velocity *= 0.01f;

			yield return new WaitForSeconds(xxx.rr(0.3f, 1.5f));
			hand.swingState = SwingState.recover;					
			club.R.mass = club.P.InitialMass;
			club.P.MakeWeightless();

			hand.LB.Broken = hand.uArmL.Broken = false;
			yield return new WaitForSeconds(xxx.rr(0.3f, 1.5f));
			hand.swingState = SwingState.idle;
			RB["UpperBody"].mass = LB["UpperBody"].PhysicalBehaviour.InitialMass;
			hand.ConfigHandForAiming(false);

			NPC.Mojo.Feel("Angry",-1f);
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: CLUB JAB      ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubClubJab( NpcBehaviour enemy, NpcHand hand, float timeOut=7f )
		{
			DoingStrike = true;
			hand.swingState = SwingState.preswing;
			//yield return new WaitForSeconds(xxx.rr(0.1f,1f));

			hand.ConfigHandForAiming(true);
			float swingStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;

			NpcTool club = hand.Tool;


			SwingStyle swingStyle = SwingStyle.jab;

			NpcPose swingPose = new NpcPose(NPC, "club_" + swingStyle.ToString() + "_" + hand.HandShortId, false);
			swingPose.Import();

			float timer = Time.time + xxx.rr(0.5f,1.0f);
			float dist  = Mathf.Abs(Head.position.x - enemy.Head.position.x);

			while ( Time.time < timer )
			{
				swingPose.CombineMove();
				if (dist > 1.5f)      PBO.DesiredWalkingDirection = 2f;
				else if (dist < 1f) PBO.DesiredWalkingDirection = -2f;
				else if (Time.time > timer) break;
				yield return new WaitForFixedUpdate();
				dist = Mathf.Abs(Head.position.x - enemy.Head.position.x);
			}
			
			hand.swingState = SwingState.cocked;
			club.P.MakeWeightful();

			yield return new WaitForFixedUpdate();

			club.PrepWeaponStrike(enemy, enemy.Head.root);

			hand.swingState = SwingState.swing;

			float Timeout = Time.time + xxx.rr(5,10);

			//Timeout = Time.time + xxx.rr(5,10);
			//while (enemy.Head && Mathf.Abs(enemy.Head.position.x - Head.position.x) > 2.0f && Time.time < Timeout)
			//{
			//	swingPose.CombineMove();
			//	yield return new WaitForFixedUpdate();
			//}
			if (!enemy.Head || !NPC.FacingToward(enemy) || Mathf.Abs(enemy.Head.position.x - Head.position.x) > 3f) { yield break;}
				
			Vector3 dir = (enemy.Head.position - Head.position).normalized;
				
			club.P.MakeWeightful();
			club.R.mass *= 2;
				
			timer = Time.time + 0.1f;
			while (Time.time < timer) { 
				club.R.AddForce(dir * TotalWeight * Time.fixedDeltaTime * 200f);
				yield return new WaitForFixedUpdate();
			}

			//xxx.ToggleCollisions(enemy.Head.root, club.T, true, false);
			club.PrepWeaponStrike(enemy, enemy.Head.root);
			club.R.AddForce(dir * TotalWeight * 4f * swingStrength, ForceMode2D.Impulse);


			yield return new WaitForSeconds(xxx.rr(0.3f, 1.5f));
			hand.swingState = SwingState.recover;					
			club.R.mass = club.P.InitialMass;
			club.P.MakeWeightless();

			hand.LB.Broken = hand.uArmL.Broken = false;
			yield return new WaitForSeconds(xxx.rr(0.3f, 1.5f));
			hand.swingState = SwingState.idle;
			RB["UpperBody"].mass = LB["UpperBody"].PhysicalBehaviour.InitialMass;
			hand.ConfigHandForAiming(false);

			NPC.Mojo.Feel("Angry",-1f);
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: DUAL WIELD      -------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubDualWield( float timeOut )
		{
			string gripPose    = NPC.FH.IsPrimaryGrip ? "dualgrip_fh" : "dualgrip_bh";
			
			NPC.FH.ConfigHandForAiming(false);
			NPC.BH.ConfigHandForAiming(false);

			while (Time.time < timeOut) {
				
				NPC.CommonPoses[gripPose].CombineMove();
				yield return new WaitForFixedUpdate();
			
			}
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	SUBACT: WALK TO       ---------------------------------
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator ISubWalkTo( Transform target, float minDist = 0.5f, float maxDist = 1.0f, float timeOut = 10f, bool tilTimeout=false )
		{
			bool neChecked = false;
			timeOut       += Time.time;
			
			float dist;

			NPC.FH.ConfigHandForAiming(false);
			NPC.BH.ConfigHandForAiming(false);

			bool dualgripping = (NPC.BH.IsHolding && NPC.FH.IsHolding && NPC.FH.Tool == NPC.BH.Tool);
			string gripPose   = NPC.FH.IsPrimaryGrip ? "dualgrip_fh" : "dualgrip_bh";
			float heckleTime  = Time.time + xxx.rr(2f,3f);

			while ( target && Time.time < timeOut)
			{
				if (Time.time > heckleTime)
				{
					if (CurrentAction == "Attack") { 
						if ( target.root.TryGetComponent<NpcBehaviour>( out NpcBehaviour enemy ) )
						{
							if ( enemy.Facing == Facing )
							{
								NPC.SayRandom("call");
								enemy.Memory.AddNpcStat(NPC.NpcId, "Trolled");
								if (xxx.rr(1,100) < enemy.Mojo.Feelings["Angry"] ) enemy.Memory.LastContact = NPC;
								enemy.Mojo.Feel("Angry",3f);
								NPC.Mojo.Feel("Angry",1f);
								NPC.Memory.AddNpcStat(enemy.NpcId, "Troll");
							}
							
							heckleTime = Time.time + xxx.rr(3f,5f);
						}
					}

				}

				if (NPC.NoEntry && !neChecked)
				{
					neChecked = true;
					foreach ( NpcGadget sign in NpcGadget.AllSigns )
					{
						if ( sign.Gadget == Gadgets.NoEntrySign )
						{
							List<Collider2D> colResults = new List<Collider2D>();
							sign.box.OverlapCollider(NpcBehaviour.filter, colResults);
							if (colResults.Intersect(NPC.MyColliders).Any())
							{
								timeOut += 10;
								target   = sign.transform;

								if (!NPC.FacingToward(target.position))
								{
									Flip();
									yield return new WaitForFixedUpdate();
									yield return new WaitForFixedUpdate();
								}
								break;
							}
						}
					}
				}

				dist = Mathf.Abs(Head.position.x - target.position.x);

				if (dist < minDist)			PBO.DesiredWalkingDirection	= -2f;
				else if (dist > maxDist)	PBO.DesiredWalkingDirection	= 2f;
				else if (!tilTimeout) break;
				else PBO.DesiredWalkingDirection = 0;
				
				if (dualgripping) NPC.CommonPoses[gripPose].CombineMove();

				yield return new WaitForSeconds(DelayWalk);

			}

			PBO.DesiredWalkingDirection = 0;
		}

	}
}