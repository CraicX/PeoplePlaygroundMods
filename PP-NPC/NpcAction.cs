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
	public class NpcAction : MonoBehaviour
	{
		public NpcBehaviour NPC;
		public PersonBehaviour PBO;
		public Transform Head;
		public Dictionary<string, LimbBehaviour> LB;
		public Dictionary<string, Rigidbody2D> RB;
		public NpcHand[] Hands;
		public float TotalWeight;

		private NpcHand hand;
		public Dictionary<string, float> Weights = new Dictionary<string, float>();
		public Coroutine PCR;

		public Dictionary<string, NpcBehaviour>			NPCTargets  = new Dictionary<string, NpcBehaviour>();
		public Dictionary<string, Props>				PropTargets = new Dictionary<string, Props>();
		public Dictionary<string, PhysicalBehaviour>    ItemTargets = new Dictionary<string, PhysicalBehaviour>();
		public Dictionary<string, PersonBehaviour>      PeepTargets = new Dictionary<string, PersonBehaviour>();

		public string CurrentAction = "Thinking";

		public static Dictionary<string, string[]> SmackText = new Dictionary<string, string[]>()
		{
			{"misc", new string[]{ "\"Your face makes me want to punch babies\"", "\"You're on the wrong side of town\"", "\"I'll punch you in your goofy face\"", "\"I ought to stoot-slap your *** right now\"", "\"You wanna fat lip?\"", "\"Live or die, man?\"", "\" **** you!\"", "\"I think I'll rearrange your face\"", "\"You had enough yet, dumbass?\"", "\"Come at me bro!\"", "\"I'll kick your ***\"", "\"You have a pretty mouth\"", "\"You have a problem?!\"", "\"What you looking at?\"", "\"I'll punch you in the ****\"", "\"How'd you like a knuckle sandwich?\"", "\"You wanna piece of me?\"", "\"I'm taking you down\"", "\"Settle in for your whompin\"", "\"You wanna scrap?\"", "\"Let's do this\"", "\"You don't know me, fool\"", "\"I'm going to enjoy this\"", "\"Don't even look at me, chump\"", "\"You wanna fight?\"", "\"I'm not scared of you\"", "\"Back up!\"", "\"Yo, back the hell off me\"", "\"Your *** is grass\"", "\"I ain't lost a fight yet\"", "\"Mama said knock you out!\"", "\"You're mine!\"", "\"I'm bringing the pain!\"", "\"You better not get blood on my shirt\"", "\"I eat pieces of **** like you for breakfast!\"", "\"Time for your beatdown\"", "\"Why dont you sit down and shut up\"", "\"Don't make me slap the **** out of you!\"", "\"Your breath smells like gorilla ***\"",  "\"If I had a rubber hose I'd beat you with it\"", "\"Next time I'll break my foot off in yo ***\"", "\"You're going to lose this fight\"", "\"You're weak, punk ***!\"", "\"What are you looking at, butthead?\"" } },
			{"bat", new string[]{"\"Batter up, *****!\"", "\"Batter up!\"", "\"Come get some\"", "\"You better run!\"", "\"Play ball!\""} },
			{"knife", new string[]{"\"I'll cut you sucka!\"", "\"I'll cut your head off!\"", "\"I'm feeling really stabby\"", "\"I'll shiv you fool!\"", "\"I'll gut you like a fish!\"", "\"Ears and noses will be the trophies of the day\"", "\"You see this knife?\"", "\"I'll cut out your tongue\"", "\"I'll festoon my bedchamber with your guts\"", "\"Yo homey! Blood in, blood out!\"" } },
			{"sword", new string[]{"\"Today, I eat man flesh!\"", "\"I cannot sheathe my sword until it's tasted blood!\"", } },
			{"handgun", new string[]{"\"Where's my money, *****?!\"", "\"Dodge this!\"", "\"Taste lead, dirtbag!\"", "\"You feel lucky, punk?\"", "\"Ima pop a cap, fool!\"", "\"My trigger finger is getting itchy, keep talkn\"", "\"You in my hood now, mother flower!\"", "\"I'll bust a cap in yo ***\"", "\"Time to die\"", "\"You're the disease, I'm the cure\"", "\"S#!% just got real!\"", "\"Get off my plane!\"", "\"Your move, creep\""} },
			{"rifle", new string[]{ "\"Who wants some?!\"",  "\"It's time!\"", "\"Knock knock!\"", "\"Eat this!\"", "\"Here comes the pain!\"", "\"How does it feel to be hunted?\"", "\"Welcome to Hell, *****!\"", "\"I'll send you home in a body bag\"",} },
			{"death", new string[] { "\"Suck it!\"", "\"How you like them apples?\"", "\"In Nomine Patris, et Fili, et Spritus Sancti\"", "\"Oops, I did it again\"", "\"Whoops, he dead\"", "\"I have slayed thee\"", "\"Justice has been served!\"", "\"I am invincible!\"", "\"Good, I'm glad he's dead\"", "\"That'll do, pig. That'll do.\"", "\"Nobody puts Baby in the corner\"", "\"Happy trails, buttface\"", "\"Adios, mother flower!\"", "\"Give my regards to King Tut, ***hole!\"", "\"You better not find a rebirther neither!\"", "\"Another one bites the dust!\"", "\"You dead, sucka!\"", "\"I'm the best!\"", "\"You lose!\"", "\"No one's going to your funeral\"",  "\"Rest in pieces\"", "\"It's just been revoked!\"", "\"Hasta la vista, baby\"", "\"Game over!\"", "\"See you in hell\"", "\"This is SPARTA!\"", "\"You have been erased!\"", "\"King Kong aint got **** on me!\"", "\"Too easy!\"", "\"I am a champion!\"", "\"You're worm food now\"" } },
			{"response", new string[] { "\"It take all day to think of that?\"", "\"Good one, idiot!\"", "\"Ooh I'm really scared\"", "\"Kick rocks, jerko\"", "\"Beat it, freak!\"", "\"Look pal, I dont want trouble\"", "\"Say hi to your sister for me\"", "\"Yeah yeah yeah... shut up\"", "\"Kiss my grits!\"", "\"This is your last warning!\"", "\"Shut your tiny mouth now!\"", "\"You're cruisin for a bruisin\"", "\"B****, yo mama!\"", "\"Don't push me, mother flower!\"", "\"Let's not devolve into animals\"", "\"You wanna go?!\"", "\"Oh! I get it... you wanna get knocked the **** out!\"", "\"You can't talk that way to me\"", "\"What the hell is your problem?\"", "\"What? I don't even know who you are!\"", "\"Crap off!\"", "\"You're no longer my best friend\"", "\"You shall not pass!\"", "\"Why dont you say that to my face?\"", "\"You talk'n to me, tough guy?\"", "\"Whatever\"", "\"Sir, you are being very rude!\"", "\"You kiss your mother with that mouth?\"", "\"You can't hurt my feelings\"", "\"Shut that filthy mouth!\"", "\"How dare you!\"", "\"You dont know who you're messing with, punkass\"", "\"Oh, you want to throw down?!\"", "\"You owe me an apology\"", "\"How bout I just beat the dog crap outta you?\"", "\"What belt are you, bro?\"", "\"What?! You wanna rumble?\"", "\"This aint some video game, dude, this is real life!\"", "\"We dont have to do this, man!\"", "\"You drew first blood, not me!\"", "\"nah bruh, I won't even break a sweat\"", "\"You talk too much\"", "\"Give it a rest\"", "\"Enough jibber jabber, lets fight!\"", "\"Yo mama\"", "\"I know karate\"",  } },
			{"mercy", new string[] { "\"Cry baby!\"", "\"Pfft! You too pathetic to smoke!\"", "\"Fine! You better not tell anyone!\"", "\"Next time I wont be so nice\"", "\"Today's your lucky day\"", "\"You ain't even worth it\"", "\"Hahaha, I'm one bad mamma jamma!\"", "\"Don't poop your pants.\"", "\"Alright then, you're cool I guess\"", "\"You are forgiven\"", "\"I forgive you for being a *****\"", "\"I'll let you live but next time give me $20\"", "\"Yeah thats right!\"", "\"Go cry to your mama!\"", "\"OK, chicken ****!\""} },
			{"move", new string[] {"\"Can you move?\"", "\"Excuse me, please\"", "\"Move your *** out my face\"", "\"Hey yo! Personal space!\"", "\"Move it or lose it, sister\"", "\"I'll count to 3 then you better be somewhere else\"", "\"You smell like brocolli\"", "\"I was here first\"", "\"How rude!\"", "\"Can I help you?!\"", "\"Are you trying to piss me off?\"", "\"Did you take my wallet?\"", "\"I dont like you\"", "\"Get out the way, dumbass\"", "\"Did you just grope me?\"", "\"Don't goose me you pervert!\""} },
			{"hurt", new string[] {"\"I think I need medical attention.\"", "\"Hey buddy, you have a medkit?\"", "\"Help me up so I can kick your ***!\"", "\"I dont feel so good\"", "\"Can't say I feel 100%\"", "\"Yeah you would kick a man when he's down\"", "\"Take it easy\"", "\"I'll get my revenge\"", "\"You are so annoying\"", "\"Dude.. get lost\"" } },
			{"down", new string[] { "\"I feel sorry for you\"", "\"Come on get up!\"", "\"You on the ground like a bum!\"", "\"Thats right! Bow before Zod!\"", "\"You lazy piece of ****!\"", "\"Get up, coward!\"", "\"Move along buddy! This aint your house.\"", "\"Look at this chump\"", "\"What's the matter with your legs?\"" , "\"I bet you wish you had legs like mine\"", "\"Get the **** up!\"", "\"You're blocking peoples path, move!\"", "\"I don't have any spare change for you, bum!\"", "\"Do you need some help getting up?\""} },
			{"witness", new string[] { "\"Oooh sweet! I hate that guy\"", "\"Light him on fire!\"", "\"Kill his dumb face!\"", "\"Beat the pudding out that fool!\"", "\"Why you guys fighting?\"", "\"Hey stop that now!\"", "\"Don't do that!\"", "\"I'll take you both on!\"", "\"Good good, feel the hate!\"", "\"Show him who's boss!\"", "\"Bash his face in!\"", "\"That's messed up, man\"", "\"Why dont you just leave him alone\"", "\"He's had enough man!\"", "\"Why dont you pick on someone your own size!\"", "\"Yeah! Kick his ***!\"", "\"Bruh, I bet he kicks your butt\"", "\"This used to be a friendly town\"", "\"Whats wrong with people these days?\"", "\"Boy, this place is rough\"", "\"Why you guys always fighting?\"", "\"Can't we all just get along?\"", "\"Nice! Roll his ***!\"", "\"I ain't seen an *** whoopin like that\"", "\"You wanna take me on next, tough guy?!\"", "\"Why dont you guys shake hands\"", "\"Is that necessary?\"", "\"You guys are so immature!\"" } }
		};


		void Start()
		{
			NPC         = transform.root.GetComponent<NpcBehaviour>();
			PBO         = NPC.PBO;
			LB          = NPC.LB;
			RB          = NPC.RB;
			Head        = NPC.Head;
			Hands       = NPC.Hands;
			TotalWeight = NPC.TotalWeight;

			InitActions();
		}


		public void InitActions()
		{
			Weights = new Dictionary<string, float>();
			
			string allWeights = @"Attack, Caveman, Club, Dead, Defend, Disco, Dying, Fidget, Fight, FightFire, Flee, FrontKick, GearUp,	
								  GroupUp, Knife, Medic, Recruit, Retreat, Scavenge, Shoot, Shove, SoccerKick, Survive,	
								  Tackle, TakeCover, Troll, Wander, WatchEvent, LastContact, Upgrade, Witness";
		
			foreach ( string actName in allWeights.Split( ',' ) )
				Weights[ actName.Trim() ] = 0;

		}

		public void ResetWeights() {
			Weights = Weights.ToDictionary(p => p.Key, p => 0f);
		}



		public bool IsFlipped   => (bool)(PBO.transform.localScale.x < 0.0f);


		public void Flip() {
			if (NPC.DisableFlip) return; 

			FixedJoint2D[] joints = Global.FindObjectsOfType<FixedJoint2D>();

			for ( int i = joints.Length; --i >= 0; )
			{
				if ( joints[i].connectedBody == RB["Head"] ) return;
			}

			Utilities.FlipUtility.Flip(LB["Head"].PhysicalBehaviour); 
			NPC.ScanTimeExpires = 0;
		}


		public float Facing => NPC.Facing;

		public string GetBestScore()
		{
			string bestAct		= "Wait";
			float  bestScore	= 0;

			foreach ( KeyValuePair<string, float> pair in Weights )
			{
				if ( pair.Value > bestScore )
				{
					bestScore = pair.Value;
					bestAct   = pair.Key;
				}
			}

			return bestAct;
		}


		public void ActByName( string actionName )
		{
			switch ( actionName )
			{
				case "Wait":
					PCR = StartCoroutine( IActionWait() );
					break;

				case "Flee":
					PCR = StartCoroutine( IActionFlee() );
					break;

				case "Troll":
					PCR = StartCoroutine( IActionTroll() );
					break;

				case "Witness":
					PCR = StartCoroutine( IActionWitness() );
					break;

				case "Scavenge":
					PCR = StartCoroutine( IActionScavenge() );
					break;

				case "GearUp":
					PCR = StartCoroutine( IActionGearUp() );
					break;

				case "Recruit":
					PCR = StartCoroutine( IActionRecruit() );
					break;

				case "Upgrade":
					PCR = StartCoroutine( IActionUpgrade() );
					break;

				case "Caveman":
					PCR = StartCoroutine( IActionCaveman() );
					break;

				case "Shove":
					PCR = StartCoroutine( IActionShove() );
					break;

				case "Club":
					PCR = StartCoroutine( IActionAttack() );
					//PCR = StartCoroutine( IActionClub() );
					break;

				case "Knife":
					PCR = StartCoroutine( IActionAttack() );
					//PCR = StartCoroutine( IActionKnife() );
					break;

				case "Attack":
					PCR = StartCoroutine( IActionAttack() );
					break;

				//case "GroupUp":
				//	PCR = StartCoroutine( IActionGroupUp() );
				//	break;

				//case "Regroup":
				//	PCR = StartCoroutine( IActionGroupUp(MyTargets.friend) );
				//	break;

				case "Shoot":
					PCR = StartCoroutine( IActionShoot() );
					break;

				case "Survive":
					PCR = StartCoroutine( IActionSurvive() );
					break;

				//case "TakeCover":
				//	PCR = StartCoroutine( IActionTakeCover() );
				//	break;

				case "Defend":
					PCR = StartCoroutine( IActionShoot(true) );
					break;

				//case "DefendPerson":
				//	PCR = StartCoroutine( IActionDefendPerson() );
				//	break;

				case "FightFire":
					PCR = StartCoroutine( IActionFightFire() );
					break;

				case "Wander":
					PCR = StartCoroutine( IActionWander() );
					break;

				case "WatchEvent":
					PCR = StartCoroutine( IActionWatchEvent() );
					break;

				case "Medic":
					PCR = StartCoroutine( IActionMedic() );
					break;

				case "Fidget":
					PCR = StartCoroutine( IActionFidget() );
					break;

				case "Disco":
					PCR = StartCoroutine( IActionDisco() );
					break;

				case "FrontKick":
					PCR = StartCoroutine( IActionKick() );
					break;

				case "SoccerKick":
					PCR = StartCoroutine( IActionStomp() );
					break;
			}
		}


		public void ClearAction()
		{
			NPC.CanGhost = true;
			if (PCR != null) StopCoroutine(PCR);
			CurrentAction = "Thinking";
		}


		



		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: SHOVE     /////////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionShove()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionShove()");

			if (!NPC.MyTargets.enemy) { ClearAction(); yield break; }

			if (!NPC.FacingToward(NPC.MyTargets.enemy))
			{
				NPC.Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}

			Transform enemy = NPC.MyTargets.enemy.Head;

            NPC.LB["LowerArm"].ImmuneToDamage =
			NPC.LB["LowerArmFront"].ImmuneToDamage =
			NPC.LB["UpperArm"].ImmuneToDamage =
			NPC.LB["UpperArmFront"].ImmuneToDamage = true;

			float dist = Mathf.Abs(Head.position.x - enemy.position.x);

			//while ( enemy && ( dist < 0.5f || dist > 1.5f ) )
			//{
			//	if (dist < 0.5f) PBO.DesiredWalkingDirection	= -2f;
			//	else PBO.DesiredWalkingDirection = 2f;

			//	dist = Mathf.Abs(Head.position.x - enemy.position.x);
			//	yield return new WaitForFixedUpdate();
			//}
			
			
			
			if (!enemy || Mathf.Abs(Head.position.y - enemy.position.y) > 1f ) { ClearAction(); yield break; }
			
			NPC.CanGhost = NPC.MyTargets.enemy.CanGhost = false;
			xxx.FixCollisions( LB["LowerArm"].transform );
			xxx.FixCollisions( LB["LowerArmFront"].transform );

			NPC.CommonPoses["push_1"].RunMove();

			yield return new WaitForSeconds(xxx.rr(0.5f, 1.5f));
			
			xxx.ToggleCollisions( LB["LowerArm"].transform,enemy.root,true,false);
			xxx.ToggleCollisions( LB["LowerArmFront"].transform,enemy.root,true,false);
			
			if (!NPC.FacingToward(enemy.position)) { NpcPose.Clear(NPC.PBO); ClearAction(); yield break; }

			dist = Mathf.Abs(Head.position.x - enemy.position.x);

			if (dist > 0.5f) NPC.PBO.DesiredWalkingDirection = 5f;
			
			NPC.CommonPoses["push_2"].RunMove();

			float timer = Time.time + xxx.rr(0.2f,1.0f);

			RB["LowerArm"].mass *= 2;
			RB["UpperArm"].mass *= 2;
			RB["LowerArmFront"].mass *= 2;
			RB["UpperArmFront"].mass *= 2;

			yield return new WaitForSeconds(xxx.rr(0.01f,0.1f));
			
			RB["UpperBody"].AddForce(Vector2.right * -Facing * TotalWeight * 25f);
			RB["LowerBody"].AddForce(Vector2.right * -Facing * TotalWeight * 25f);
			RB["LowerArm"].AddForce(Vector2.right * -Facing * TotalWeight *  13);
			RB["LowerArmFront"].AddForce(Vector2.right * -Facing * TotalWeight * 13);
			yield return new WaitForFixedUpdate();

			RB["LowerArm"].AddForce(Vector2.right * -Facing * TotalWeight *  3, ForceMode2D.Impulse);
			RB["LowerArmFront"].AddForce(Vector2.right * -Facing * TotalWeight * 3, ForceMode2D.Impulse );
			timer = Time.time + 0.2f;
			while ( Time.time < timer )
			{
				RB["UpperBody"].velocity  *= 0.5f;
				RB["LowerBody"].velocity  *= 0.5f;
				RB["MiddleBody"].velocity *= 0.5f;
				yield return new WaitForFixedUpdate();
			}
			NPC.Memory.AddNpcStat(NPC.MyTargets.enemy.NpcId, "HitThem");
			NPC.MyTargets.enemy.Memory.AddNpcStat(NPC.NpcId, "HitMe");

			NpcPose.Clear(NPC.PBO);

			LB["LowerArm"].ImmuneToDamage =
			LB["LowerArmFront"].ImmuneToDamage =
			LB["UpperArm"].ImmuneToDamage =
			LB["UpperArmFront"].ImmuneToDamage = false;

			NPC.CanGhost = NPC.MyTargets.enemy.CanGhost = true;
			
			NPC.RunRigids(NPC.RigidMass, -1f);
			
			NPC.Mojo.Feel("Fear",-2f);
			NPC.Mojo.Feel("Angry",-1f);
			NPC.MyTargets.enemy.Mojo.Feel("Angry",12f);
			
			ClearAction();

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: CAVEMAN     ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionCaveman()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionCaveman()");


			if (!NPC.FacingToward(NPC.MyTargets.enemy))
			{
				NPC.Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}

			NPC.LastCaveman = Time.time + xxx.rr(30f,120f);
			

			Transform enemy = NPC.MyTargets.enemy.Head;

			NpcHand hand = NPC.OpenHand;

			if (hand.IsHolding) { ClearAction(); yield break; }

            NPC.LB["LowerArm"].ImmuneToDamage      =
			NPC.LB["LowerArmFront"].ImmuneToDamage = 
			NPC.LB["UpperArm"].ImmuneToDamage      = 
			NPC.LB["UpperArmFront"].ImmuneToDamage = true;

			bool bothHands = (!hand.AltHand.IsHolding);

			float dist = Mathf.Abs(Head.position.x - enemy.position.x);

			while ( enemy && ( dist < 0.5f || dist > 1.5f ) )
			{
				if (dist < 0.5f) PBO.DesiredWalkingDirection	= -2f;
				else PBO.DesiredWalkingDirection = 2f;

				dist = Mathf.Abs(Head.position.x - enemy.position.x);
				yield return new WaitForFixedUpdate();
			}
			
			PBO.DesiredWalkingDirection = 0f;
			
			if (!enemy || !NPC.FacingToward(enemy.position)) { ClearAction(); yield break; }

			Coroutine grabAct  = StartCoroutine(hand.IGrab(NPC.MyTargets.enemy.RB["Head"]));
			Coroutine grabAct2 = null;

			bool doubleGrab = false;

			if ( bothHands && xxx.rr( 1, 3 ) == 2 )
            {
				grabAct2   = StartCoroutine(hand.AltHand.IGrab(NPC.MyTargets.enemy.RB["Head"], xxx.rr(0.1f, 2f)));
				doubleGrab = true;
            }

			float timeout = Time.time + xxx.rr(15,25);

			LimbBehaviour EnemyLB = NPC.MyTargets.enemy.LB["Head"];

			while ( !hand.IsGrabbing && enemy && Time.time < timeout && NPC.HurtLevel < 5 )
            {
				dist = Mathf.Abs(Head.position.x - enemy.position.x);
				if (dist < 0.5f) PBO.DesiredWalkingDirection	= -4f;
				else if (dist > 1.0f) PBO.DesiredWalkingDirection = 4f;
				else PBO.DesiredWalkingDirection = 0f;
				yield return new WaitForFixedUpdate();
            }

			if (hand.IsGrabbing) { 
				
				NPC.CanGhost                 = false;
				NPC.MyTargets.enemy.CanGhost = false;
				
				xxx.ToggleCollisions(hand.T,NPC.MyTargets.enemy.Head, true, false);
				
				NPC.MyTargets.enemy.DisableFlip = true;
				NPC.DisableFlip                 = true;
				PBO.DesiredWalkingDirection     = -5f;
                hand.RB.mass                   *= 2f;
				bool canPunch					= false;
				if (bothHands) {
					if (!doubleGrab) { 
						hand.AltHand.RB.mass *= 2f;
						canPunch = true;
						xxx.ToggleCollisions(hand.AltHand.T, NPC.MyTargets.enemy.Head.root, true, false);
					}
					else
					{
						xxx.ToggleCollisions( LB["UpperLegFront"].transform, NPC.MyTargets.enemy.Head.root, true, false);
						RB["UpperLegFront"].mass *= 2f;
					}
				}


				float pulldownTime = Time.time + xxx.rr(0.1f, 2f);
				float punchTime    = Time.time + xxx.rr(1f, 4f);
				int punchPower		= 0;
				timeout = Time.time + xxx.rr(5f,10f);
				while (hand.GrabJoint && NPC.HurtLevel < 6 && EnemyLB.IsConsideredAlive && !EnemyLB.IsDismembered && Time.time < timeout)
                {
					if (Facing != NPC.MyTargets.enemy.Facing) break;
					
					
					PBO.DesiredWalkingDirection = -20f;

					if (Time.time > pulldownTime) {
						hand.RB.AddForce(Vector2.right * TotalWeight * xxx.rr(-5.1f, 5.1f), ForceMode2D.Impulse);
						pulldownTime = Time.time + xxx.rr(1.1f, 3f);
				
						if (doubleGrab && Time.time > punchTime )
							hand.AltHand.RB.AddForce(Vector2.down * TotalWeight * xxx.rr(1.1f, 5.1f), ForceMode2D.Impulse);
					}

					if (doubleGrab && !hand.AltHand.GrabJoint) doubleGrab = false;

					if (canPunch) { 
						xxx.ToggleCollisions(hand.AltHand.T, NPC.MyTargets.enemy.Head.root, true, false);
						NPC.Mojo.Feel("Fear",-5f);
						NPC.Mojo.Feel("Angry",-1f);
						if (Time.time > punchTime)
						{
							if ( ++punchPower < 3 )
							{
								hand.AltHand.RB.AddForce(Vector2.right * -Facing * TotalWeight * xxx.rr(2.1f, 4.1f),ForceMode2D.Impulse);
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
						
					} else
					{
						if (Time.time > punchTime) { 
							if ( Time.time > punchTime + 2 )
							{
								if (++punchPower < 3) { 

									RB["UpperLegFront"].AddForce(Vector2.right * -Facing * TotalWeight * xxx.rr(2.1f, 4.1f),ForceMode2D.Impulse);
								} else
								{
									punchPower = 0;
									punchTime = Time.time + xxx.rr(1.1f, 3f);
									PBO.DesiredWalkingDirection = -10f;
								}
							} else { 
							
								PBO.DesiredWalkingDirection = 0f;
								LB["UpperLegFront"].InfluenceMotorSpeed(Mathf.DeltaAngle(
							LB["UpperLegFront"].Joint.jointAngle, 0.015928f * NPC.PBO.AngleOffset), 15f);

								LB["LowerLegFront"].InfluenceMotorSpeed(Mathf.DeltaAngle(
							LB["LowerLegFront"].Joint.jointAngle, 83.00495f * NPC.PBO.AngleOffset), 15f);

							}
						} 
					}

					yield return new WaitForFixedUpdate();
                }

				if (EnemyLB && EnemyLB.IsDismembered)
                {
					ModAPI.Notify("Decapetated");

					PBO.DesiredWalkingDirection = 0f;
					yield return new WaitForSeconds(1);
					if (doubleGrab && hand.AltHand.GrabJoint)  UnityEngine.Object.DestroyImmediate((UnityEngine.Object)hand.AltHand.GrabJoint);

					NPC.CommonPoses["soccerkick"].RunMove();
					yield return new WaitForSeconds(1);
					if (hand.GrabJoint)  UnityEngine.Object.DestroyImmediate((UnityEngine.Object)hand.GrabJoint);
					yield return new WaitForSeconds(0.1f);
					NpcPose.Clear(PBO);
					RB["Foot"].AddForce(Vector2.down * TotalWeight * 2 *  xxx.rr(5.0f,9.9f), ForceMode2D.Impulse);
					yield return new WaitForSeconds(1f);

                }
			}

			if (grabAct2 != null) StopCoroutine(grabAct2);
			if (grabAct != null)  StopCoroutine(grabAct);
			
			if (hand.AltHand.GrabJoint)  {
				hand.AltHand.GrabJoint.enabled = false;
				UnityEngine.Object.DestroyImmediate((UnityEngine.Object)hand.AltHand.GrabJoint);
			}
			if (hand.GrabJoint)  {
				hand.GrabJoint.enabled = false;
				UnityEngine.Object.DestroyImmediate((UnityEngine.Object)hand.GrabJoint);

			}
			
			PBO.DesiredWalkingDirection = 0;
			
			NPC.RunRigids(NPC.RigidMass, -1f);

			LB["LowerArm"].ImmuneToDamage      =
			LB["LowerArmFront"].ImmuneToDamage = 
			LB["UpperArm"].ImmuneToDamage      = 
			LB["UpperArmFront"].ImmuneToDamage = false;

			NPC.CanGhost        = true;
			NPC.DisableFlip		= false;
			
			NPC.LB["LowerArmFront"].Broken = NPC.LB["LowerArm"].Broken = NPC.LB["UpperArmFront"].Broken = NPC.LB["UpperArm"].Broken = false;

			NPC.FH.ConfigHandForAiming(false);
			NPC.BH.ConfigHandForAiming(false);

			if (NPC.MyTargets.enemy) { 
				NPC.MyTargets.enemy.DisableFlip = false;
				NPC.MyTargets.enemy.CanGhost    = true;
            }

			ClearAction();

		}

		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: MELEE CLUB  ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionClub()
		{
			MinMax minMax = new MinMax(0.5f, 2.0f);

			NpcBehaviour enemy = NPC.MyTargets.enemy;

			NpcHand hand;

			hand = NPC.RandomHand;

			if (!hand.IsHolding || !hand.Tool.props.Traits["club"] ) hand = hand.AltHand;

			bool twoBats = (hand.AltHand.IsHolding && hand.AltHand.Tool.props.Traits["club"] );

			bool isCrouched = false;

			int movesCount = xxx.rr(1,3);
			Coroutine batSwing, batSwingAlt;
			float timeout, dist;

			if (xxx.rr(1,5) == 2) NPC.SayRandom( "bat" );

			while ( enemy )
			{
				if ( !NPC.FacingToward( enemy ) )
				{
					NPC.Flip();
					yield return new WaitForFixedUpdate();
					yield return new WaitForFixedUpdate();
				}

				timeout = Time.time + xxx.rr(5,7);

				do {

					dist = Mathf.Abs(Head.position.x - enemy.Head.position.x);
				
					if (dist < minMax.Min)				PBO.DesiredWalkingDirection	= -2f;
					else if (dist > minMax.Max + 0.5f)	PBO.DesiredWalkingDirection	= 2f;

					yield return new WaitForFixedUpdate();

				}  while ( Time.time < timeout && enemy && (dist < minMax.Min || dist > minMax.Max) );
				
				PBO.DesiredWalkingDirection = 0;

				if (enemy) {
					hand.swingState = SwingState.ready;

					batSwing = StartCoroutine(IActionClubStrikes(enemy.Head, hand));
					if (twoBats) {
						hand.AltHand.swingState = SwingState.ready;
						batSwingAlt = StartCoroutine(IActionClubStrikes(enemy.Head, hand.AltHand, true));
					}

					timeout = Time.time + xxx.rr(5,7);

					while(enemy && hand.swingState != SwingState.idle && Time.time < timeout)
					{
						dist = Mathf.Abs(Head.position.x - enemy.Head.position.x);
				
						if (dist < minMax.Min)				PBO.DesiredWalkingDirection	= -2f;
						else if (dist > minMax.Max + 0.5f)	{
							if ( !NPC.FacingToward( enemy ) )
							{
								NPC.Flip();
								yield return new WaitForFixedUpdate();
								yield return new WaitForFixedUpdate();
							}

							PBO.DesiredWalkingDirection	= 2f;

						}
						else
						{
							PBO.DesiredWalkingDirection = 0f;

							if (Head.position.y - enemy.Head.position.y > 0.4f)
							{

								// target to hit is lower, so duck
								while(enemy && Head.position.y - enemy.Head.position.y > 0.4f && Time.time < timeout) { 
									NPC.CommonPoses["crouch"].RunMove();
									isCrouched = true;
									yield return new WaitForFixedUpdate();
								}
								if (isCrouched) NpcPose.Clear(NPC.PBO);
							}
						}
						
						yield return new WaitForFixedUpdate();
					}
				}
				
				if (--movesCount <= 0 || !enemy) break;
			}

			NpcPose.Clear(NPC.PBO);


			ClearAction();
			
			
			yield return null;

		}


		public IEnumerator ISubActWalkTo( Transform trans, MinMax minMax, float timeOut=10f )
		{
			timeOut += Time.time;
			float dist;
			while ( trans && Time.time < timeOut)
			{
				dist = Mathf.Abs(Head.position.x - trans.position.x);

				if (dist < minMax.Min)				PBO.DesiredWalkingDirection	= -2f;
				else if (dist > minMax.Max + 0.5f)	PBO.DesiredWalkingDirection	= 2f;
				else break;

				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();

			}

			PBO.DesiredWalkingDirection = 0;
			
		}

		public IEnumerator ISubActGrab( Rigidbody2D rb, NpcHand hand, float timeOut )
		{
			timeOut += Time.time;
			float dist;

			Coroutine grabAct  = StartCoroutine(hand.IGrab(NPC.MyTargets.enemy.RB["Head"]));

			while ( !hand.IsGrabbing && rb && Time.time < timeOut )
            {
				dist	= Mathf.Abs(Head.position.x - rb.position.x);
				if (dist < 0.5f) PBO.DesiredWalkingDirection	  = -4f;
				else if (dist > 1.0f) PBO.DesiredWalkingDirection = 4f;
				else PBO.DesiredWalkingDirection                  = 0f;
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
            }
			
		}

		public IEnumerator ISubActStab( Transform trans, NpcHand hand, float timeOut=7f )
		{
			timeOut += Time.time;

			NpcPose stabPose = new NpcPose(NPC,"stab_" + hand.HandShortId, false);

			float Timer = Time.time + xxx.rr(0.5f, 2f);

			NpcTool knife = hand.Tool;

			knife.props.NoGhost = true;

			yield return new WaitForSeconds(xxx.rr(0.5f,1.0f));
				
			xxx.ToggleCollisions(knife.T,trans.root, true, false);
				
			NPC.DisableFlip                 = true;

			while ( Time.time < timeOut && (Time.time < Timer || Mathf.Abs(hand.T.position.x - trans.position.x) > 1f ))
			{
				stabPose.CombineMove();
				yield return new WaitForFixedUpdate();
			}

			NpcPose.Clear(PBO);

			Timer = Time.time + xxx.rr(0.1f, 0.3f);
			while (Time.time < Timer) { 
				hand.RB.AddForce(Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * 300f);
				yield return new WaitForFixedUpdate();
			}
			
			yield return new WaitForSeconds(xxx.rr(0.5f, 2f));

			Timer = Time.time + 0.2f;
			while (Time.time < Timer) { 
				hand.RB.AddForce(Vector2.right * Facing * TotalWeight * Time.fixedDeltaTime * 400f);
				yield return new WaitForFixedUpdate();
			}

			yield return new WaitForSeconds(xxx.rr(0.5f, 2f));

			knife.props.NoGhost = false;
		}




		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: MELEE KNIFE ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionKnife()
		{
			MinMax minMax      = new MinMax(0.5f, 1.0f);
			NpcBehaviour enemy = NPC.MyTargets.enemy;
			NpcHand hand       = NPC.RandomHand;

			if (!hand.IsHolding || !hand.Tool.props.isShiv ) hand = hand.AltHand;

			bool twoKnives = (hand.AltHand.IsHolding && hand.AltHand.Tool.props.isShiv );
			int movesCount = xxx.rr(1,3);
			NpcTool knife  = hand.Tool;


			if (xxx.rr(1,5) == 2) NPC.SayRandom( "knife" );

			if ( !NPC.FacingToward( enemy ) )
			{
				NPC.Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}


			yield return StartCoroutine( 
				ISubActWalkTo(enemy.Head, new MinMax(0.5f, 0.9f), xxx.rr(5,7))	);


			if (Mathf.Abs(Head.position.x - enemy.Head.position.x) > 1f) { ClearAction(); yield break; }

			string[] targets = {"Head", "UpperBody", "MiddleBody", "LowerBody"};

			string target;
			

			if (!twoKnives && xxx.rr(1,3)==2 && enemy.Facing == Facing) 
			{ 
				yield return StartCoroutine(
				ISubActGrab( enemy.RB["Head"], hand.AltHand, xxx.rr(5,7)) );

				xxx.ToggleCollisions(hand.T, enemy.Head.root, true, false);
				
				knife.props.NoGhost = true;
				
				NPC.CanGhost                    = false;
				enemy.CanGhost                  = false;
				
				xxx.ToggleCollisions(knife.T,enemy.Head.root, true, false);
				
				enemy.DisableFlip               = true;
				NPC.DisableFlip                 = true;

				int stabs = xxx.rr(2,4);
				float timer;
				while ( hand.AltHand.IsGrabbing && hand.AltHand.GrabJoint && --stabs >= 0)
				{
					if (Facing != NPC.MyTargets.enemy.Facing) break;

					target    = targets.PickRandom();
					
					timer = Time.time + 0.5f;
					while ( Time.time < timer )
					{
						hand.RB.AddForce(Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * 500f);
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

					
					//knife.R.AddForce(stabDirec * TotalWeight * xxx.rr(2.1f, 4.1f),ForceMode2D.Impulse);


					//knife.R.AddForce(Vector2.right * Facing * TotalWeight * 2,ForceMode2D.Impulse);

					//yield return new WaitForSeconds(xxx.rr(0.5f,2.5f));
				}

				if (hand.AltHand.GrabJoint)  {
					hand.AltHand.GrabJoint.enabled = false;
					UnityEngine.Object.DestroyImmediate((UnityEngine.Object)hand.AltHand.GrabJoint);
				}
			}
			else
			{
				xxx.ToggleCollisions(hand.T, enemy.Head.root, true, false);
				
				knife.props.NoGhost = true;
				
				NPC.CanGhost                    = false;
				enemy.CanGhost                  = false;
				
				xxx.ToggleCollisions(knife.T,enemy.Head.root, true, false);
				
				enemy.DisableFlip               = true;
				NPC.DisableFlip                 = true;

				target    = targets.PickRandom();
				
				yield return StartCoroutine(
				ISubActStab( enemy.RB[target].transform, hand ));

			}

			NpcPose.Clear(NPC.PBO);

			knife.props.NoGhost = false;
			NPC.CanGhost        = true;
			enemy.CanGhost      = true;
			enemy.DisableFlip   = false;
			NPC.DisableFlip     = false;

			ClearAction();
			
			
			yield return null;

		}

		

		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: CLUB STRIKES  /////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionClubStrikes( Transform trans, NpcHand hand, bool isAlt=false )
		{
			hand.swingState = SwingState.preswing;
			yield return new WaitForSeconds(xxx.rr(0.1f,1f));

			if (!hand.IsHolding) {
				hand.swingState = SwingState.idle;
				yield break;
			}
			hand.ConfigHandForAiming(true);
			float swingStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;

			NpcTool club = hand.Tool;

			club.props.NoGhost = false;

			SwingStyle swingStyle = SwingStyle.over;

			if (Mathf.Abs(trans.position.x - Head.position.x) < 1f) swingStyle = SwingStyle.jab;
			if (isAlt) swingStyle = SwingStyle.jab;
			NpcPose swingPose = new NpcPose(NPC, "club_" + swingStyle.ToString() + "_" + hand.HandShortId, false);
			swingPose.Import();

			

			float timer = Time.time + xxx.rr(0.2f,0.4f);


			while ( Time.time < timer )
			{
				yield return new WaitForFixedUpdate();
				swingPose.CombineMove();
			}
			hand.swingState = SwingState.cocked;
			club.props.P.MakeWeightful();

			//hand.LB.Broken = hand.uArmL.Broken = true;
			//NpcPose.Clear(NPC.PBO);
			

			yield return new WaitForFixedUpdate();
			club.props.NoGhost = true;
			xxx.FixCollisions(club.P.transform);
			xxx.ToggleCollisions( club.P.transform, Head.root, false, false);
			
			hand.swingState = SwingState.swing;

			float Timeout = Time.time + xxx.rr(5,10);
			
			if (swingStyle == SwingStyle.over) { 
				while (trans && Mathf.Abs(trans.position.x - Head.position.x) > 2.0f && Time.time < Timeout)
				{
					swingPose.CombineMove();
					yield return new WaitForFixedUpdate();
				}
				if (!trans) { yield break;}
				timer = Time.time + 0.1f;
				club.props.P.MakeWeightful();
				while (Time.time < timer) { 
					club.props.P.rigidbody.AddForce(Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * 300f);
					//hand.RB.AddForce(Vector2.up * TotalWeight * Time.fixedDeltaTime * 300f);
					yield return new WaitForFixedUpdate();
				}
				
				yield return new WaitForFixedUpdate();

				Vector3 dir = (trans.position - Head.position).normalized;
				xxx.ToggleCollisions(trans.root, hand.Tool.T, true, false);
				club.props.P.rigidbody.AddForce(((Vector3.right * -Facing) + Vector3.down + dir).normalized * TotalWeight * 3f * swingStrength, ForceMode2D.Impulse);
				RB["UpperBody"].mass *= 2;
				RB["UpperBody"].velocity *= 0.01f;
			}

			if ( swingStyle == SwingStyle.jab )
			{
				Timeout = Time.time + xxx.rr(5,10);
				while (trans && Mathf.Abs(trans.position.x - Head.position.x) > 2.0f && Time.time < Timeout)
				{
					swingPose.CombineMove();
					yield return new WaitForFixedUpdate();
				}
				if (!trans) { yield break;}
				
				Vector3 dir = (trans.position - Head.position).normalized;
				
				club.props.P.MakeWeightful();
				club.R.mass *= 2;
				
				timer = Time.time + 0.1f;
				while (Time.time < timer) { 
					club.props.P.rigidbody.AddForce(dir * TotalWeight * Time.fixedDeltaTime * 200f);
					yield return new WaitForFixedUpdate();
				}

				xxx.ToggleCollisions(trans.root, hand.Tool.T, true, false);
				club.props.P.rigidbody.AddForce(dir * TotalWeight * 4f * swingStrength, ForceMode2D.Impulse);
			}



			yield return new WaitForSeconds(xxx.rr(0.3f, 1.5f));
			hand.swingState = SwingState.recover;					
			club.R.mass = club.P.InitialMass;
			club.props.P.MakeWeightless();
			club.props.NoGhost = false;

			hand.LB.Broken = hand.uArmL.Broken = false;
			yield return new WaitForSeconds(xxx.rr(0.3f, 1.5f));
			hand.swingState = SwingState.idle;
			RB["UpperBody"].mass = LB["UpperBody"].PhysicalBehaviour.InitialMass;
			club.props.NoGhost = false;
			hand.ConfigHandForAiming(false);

			NPC.Mojo.Feel("Fear",-5f);
			NPC.Mojo.Feel("Angry",-1f);

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: SWORDSTRIKES  /////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionSwordStrikes( Transform trans, NpcHand hand, bool isAlt=false )
		{
			hand.swingState = SwingState.preswing;
			yield return new WaitForSeconds(xxx.rr(0.1f,1f));

			if (!hand.IsHolding) {
				hand.swingState = SwingState.idle;
				yield break;
			}
			hand.ConfigHandForAiming(true);
			float swingStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;

			NpcTool sword = hand.Tool;

			sword.props.NoGhost = false;

			SwingStyle swingStyle = SwingStyle.over;

			if (Mathf.Abs(trans.position.x - Head.position.x) < 1f) swingStyle = SwingStyle.jab;
			if (isAlt) swingStyle = SwingStyle.jab;
			NpcPose swingPose = new NpcPose(NPC, "club_" + swingStyle.ToString() + "_" + hand.HandShortId, false);
			swingPose.Import();

			

			float timer = Time.time + xxx.rr(0.2f,0.4f);


			while ( Time.time < timer )
			{
				yield return new WaitForFixedUpdate();
				swingPose.CombineMove();
			}
			hand.swingState = SwingState.cocked;
			sword.props.P.MakeWeightful();

			//hand.LB.Broken = hand.uArmL.Broken = true;
			//NpcPose.Clear(NPC.PBO);
			

			yield return new WaitForFixedUpdate();
			sword.props.NoGhost = true;
			xxx.FixCollisions(sword.P.transform);
			xxx.ToggleCollisions( sword.P.transform, Head.root, false, false);
			
			hand.swingState = SwingState.swing;

			float Timeout = Time.time + xxx.rr(5,10);
			
			if (swingStyle == SwingStyle.over) { 
				while (trans && Mathf.Abs(trans.position.x - Head.position.x) > 2.0f && Time.time < Timeout)
				{
					swingPose.CombineMove();
					yield return new WaitForFixedUpdate();
				}
				if (!trans) { yield break;}
				timer = Time.time + 0.1f;
				sword.props.P.MakeWeightful();
				while (Time.time < timer) { 
					sword.props.P.rigidbody.AddForce(Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * 300f);
					//hand.RB.AddForce(Vector2.up * TotalWeight * Time.fixedDeltaTime * 300f);
					yield return new WaitForFixedUpdate();
				}
				
				yield return new WaitForFixedUpdate();

				Vector3 dir = (trans.position - Head.position).normalized;
				xxx.ToggleCollisions(trans.root, hand.Tool.T, true, false);
				sword.props.P.rigidbody.AddForce(((Vector3.right * -Facing) + Vector3.down + dir).normalized * TotalWeight * 3f * swingStrength, ForceMode2D.Impulse);
				RB["UpperBody"].mass *= 2;
				RB["UpperBody"].velocity *= 0.01f;
			}

			if ( swingStyle == SwingStyle.jab )
			{
				Timeout = Time.time + xxx.rr(5,10);
				while (trans && Mathf.Abs(trans.position.x - Head.position.x) > 2.0f && Time.time < Timeout)
				{
					swingPose.CombineMove();
					yield return new WaitForFixedUpdate();
				}
				if (!trans) { yield break;}
				
				Vector3 dir = (trans.position - Head.position).normalized;
				
				sword.props.P.MakeWeightful();
				sword.R.mass *= 2;
				
				timer = Time.time + 0.1f;
				while (Time.time < timer) { 
					sword.props.P.rigidbody.AddForce(dir * TotalWeight * Time.fixedDeltaTime * 200f);
					yield return new WaitForFixedUpdate();
				}

				xxx.ToggleCollisions(trans.root, hand.Tool.T, true, false);
				sword.props.P.rigidbody.AddForce(dir * TotalWeight * 4f * swingStrength, ForceMode2D.Impulse);
			}



			yield return new WaitForSeconds(xxx.rr(0.3f, 1.5f));
			hand.swingState = SwingState.recover;					
			sword.R.mass = sword.P.InitialMass;
			sword.props.P.MakeWeightless();
			sword.props.NoGhost = false;

			hand.LB.Broken = hand.uArmL.Broken = false;
			yield return new WaitForSeconds(xxx.rr(0.3f, 1.5f));
			hand.swingState = SwingState.idle;
			RB["UpperBody"].mass = LB["UpperBody"].PhysicalBehaviour.InitialMass;
			sword.props.NoGhost = false;
			hand.ConfigHandForAiming(false);

			NPC.Mojo.Feel("Fear",-5f);
			NPC.Mojo.Feel("Angry",-1f);

		}


		
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: FIDGET      ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionFidget()
		{

			if (!NPC.MyTargets.item)
			{
				ClearAction();
				yield break;
			}

			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionFidget()");

			if (NPC.MyTargets.item.spriteRenderer) { 
				NPC.MyTargets.item.spriteRenderer.sortingLayerName = "Background";
				NPC.MyTargets.item.spriteRenderer.sortingOrder     = -10;
			}


			float dist       = float.MaxValue;
			bool pickedHand  = false;
			Coroutine pointy = null;

			if (new string[]{"Jukebox","Television","Radio" }.Contains( NPC.MyTargets.item.name ) )
			{
				NPC.ActivityMessage.Set("Fidget with " + NPC.MyTargets.item.name, 5f);
			} else
			{
				NPC.Say("Whats this thing do?", 5f, true);
				if (NPC.EnhancementMemory) { 
					NPC.audioSource.enabled = true;
					NPC.audioSource.PlayOneShot(NpcMain.GetSound("whistle"),1f);
				}
				yield return new WaitForSeconds(3);
			}

			if (!NPC.FacingToward(NPC.MyTargets.item.transform.position)) {

				NPC.Flip();
				yield return new WaitForFixedUpdate();
			}

			float timeout = Time.time + 10f;

			while ( NPC.MyTargets.item && !NPC.CancelAction && Time.time < timeout )
			{
				dist = Mathf.Abs(Head.position.x - NPC.MyTargets.item.transform.position.x);

				if (dist > 1.7f)
				{
					if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 2f;

				} 
				else
				if (dist < 1.0f)
				{
					if (PBO.DesiredWalkingDirection > -1f) PBO.DesiredWalkingDirection = -2f;
				}
				else
				{
					PBO.DesiredWalkingDirection = 0f;
					if ( !pickedHand )
					{
						pickedHand               = true;
						hand                     = NPC.RandomHand;
						if (hand.IsHolding) hand = hand.AltHand;
						pointy                   = hand.StartCoroutine(hand.IPointArm(NPC.MyTargets.item.transform));
						yield return new WaitForSeconds(2);



						//NPC.MyTargets.item.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
						NPC.Mojo.Feelings["Bored"] = 0;
						yield return new WaitForSeconds(1);
						JukeboxBehaviour juke = null;
						if (NPC.MyTargets.item )
						{
							switch ( NPC.MyTargets.item.name )
							{
								case "Jukebox":
									if ( !NPC.MyTargets.item.TryGetComponent<JukeboxBehaviour>( out juke ) )
									{
										ClearAction();
										yield break;
									}

									juke.tracks = NpcMain.SoundBank["jukebox"];
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
										//    Locations = {NPC.MyTargets.item.transform.position },
										//    PBs       = {juke.physicalBehaviour },
										//    Sender    = this,
										//};

										//NpcEvents.BroadcastEvent( MyEvent, NPC.MyTargets.item.transform.position, 20f );
									}
									break;


								case "Television":
									if ( NPC.MyTargets.item.TryGetComponent<TelevisionBehaviour>( out TelevisionBehaviour tv) )
									{
										if ( tv.Activated )
										{
											NpcEvents.BroadcastEvent(new EventInfo() { 
												EventId   = EventIds.TV,
												Expires   = 30f,
												Locations = {tv.transform.position },
												PBs       = {NPC.MyTargets.item},
												Sender    = NPC,
											}, tv.transform.position, 10f);
										}
									}
									break;	
									
								default:
									if (NPC.MyTargets.item.transform.position.y < LB["UpperBody"].transform.position.y )
									{
										NPC.CommonPoses["crouch"].RunMove();
									} 

									if (NPC.MyTargets.item.name == "Launch Platform")
									{
										NPC.CommonPoses["prone"].RunMove();
										NPC.RB["UpperBody"].AddForce(new Vector2(1 * -Facing, -1) * TotalWeight, ForceMode2D.Impulse);
										yield return new WaitForFixedUpdate();
										NPC.NoGhost.Add(NPC.MyTargets.item.transform.root);
										xxx.ToggleCollisions(Head, NPC.MyTargets.item.transform,true, true);
									}

									NPC.Say("What's a " + NPC.MyTargets.item.name + "?", 2f, true);
									yield return new WaitForSeconds(2.5f);
									string[] phrases =
									{
										"Looks like some kind of fancy microwave.",
										"I Wonder if I should push this?",
										"This must be one of those smart toilets",
										"This is mines now! I'm keeping this.",
										"This would look nicely in my apartment",
										"Well, lets find out",
										"This hunk of junk dont even work right",
										"This better be worth my time.",
										"Who built this?",
										"Whats going to happen?",
										"Is this thing safe?",
									};
									NPC.Say(phrases.PickRandom(), 4f, true);
									yield return new WaitForSeconds(4);
									NPC.Memory.NoFidget.Add(NPC.MyTargets.item.GetHashCode());
									NPC.MyTargets.item.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
									hand.StopCoroutine(pointy);
									yield return new WaitForFixedUpdate();
									NPC.NoGhost.Remove(NPC.MyTargets.item.transform.root);


									break;

							}
						}

						NPC.CancelAction = true;
					}
					break;
				}
				if ( NPC.MyTargets.item.TryGetComponent<JukeboxBehaviour>( out JukeboxBehaviour jb ) )
				{
					if ( jb.audioSource.isPlaying ) { NPC.Mojo.Feel("Bored", -10);  break; }
				}
				yield return new WaitForFixedUpdate();
			}

			CurrentAction         = "Thinking";
			//PBO.OverridePoseIndex       = (int)PoseState.Rest;
			PBO.DesiredWalkingDirection = 0;

			yield break;
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: TROLL       ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionTroll()
		{
			if ( !NPC.MyTargets.enemy )
			{
				ClearAction();
				yield break;
			}

			List<string> MyCats = new List<string>() {"misc"};

			if (NPC.HurtLevel > 2)
			{
				MyCats.Add("hurt");
			}

			if (!NPC.MyTargets.enemy.OnFeet()) { MyCats.Clear(); MyCats.Add("down"); }

			List<string> Sayings = new List<string>();

			foreach ( string cat in MyCats )
			{
				Sayings.AddRange(SmackText[cat] );
			}
			
			string SmackTalk = Sayings.PickRandom();

			NPC.Say(SmackTalk, 3f);

			NpcPose[] TrollPose = new NpcPose[13];

			for ( int i = 1; i < TrollPose.Length; i++ )
			{
				TrollPose[i] = new NpcPose(NPC, "troll_" + i, false);
				TrollPose[i].Ragdoll.ShouldStandUpright       = true;
				TrollPose[i].Ragdoll.State                    = PoseState.Rest;
				TrollPose[i].Ragdoll.Rigidity                 = 5f;
				TrollPose[i].Ragdoll.AnimationSpeedMultiplier = 5f;
				TrollPose[i].Ragdoll.UprightForceMultiplier   = 2f;
				TrollPose[i].Import();
			};

			TrollPose[6].Ragdoll.UprightForceMultiplier   = 10f;

			NPC.Mojo.Feel("Angry", -1f);

			NPC.MyTargets.enemy.Mojo.Feel("Angry", xxx.rr(1,5));

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
				NPC.MyTargets.enemy.Mojo.Feel("Fear",1f);
				NPC.MyTargets.enemy.Mojo.Feel("Angry",3f);
				NPC.Mojo.Feel("Angry",-1f);
				NPC.Mojo.Feel("Fear",-5f);
			}

			NPC.Memory.AddNpcStat(NPC.MyTargets.enemy.NpcId, "Troll");

			yield return new WaitForFixedUpdate();

			if (!NPC.FacingToward(NPC.MyTargets.enemy.Head.position)) { NPC.Flip(); yield return new WaitForFixedUpdate(); }

			int trollMode = xxx.rr(1,5);
			float seconds = 1;
			float timer;
			if ( trollMode == 1 )
			{
				seconds = Time.time + xxx.rr(1.1f, 4.1f);
				
				TrollPose[1].RunMove();

				while ( Time.time < seconds )
				{
					RB["LowerArm"].AddForce(Vector2.up * TotalWeight * Time.fixedDeltaTime * 100 * xxx.rr(0.1f, 0.2f), ForceMode2D.Impulse);
					yield return new WaitForSeconds(xxx.rr(0.1f, 0.5f));
					RB["Head"].AddForce(Vector2.right * TotalWeight * Time.fixedDeltaTime * 100 * xxx.rr(0.1f, 0.2f), ForceMode2D.Impulse);
					yield return new WaitForSeconds(xxx.rr(0.1f, 0.5f));
					LB["LowerBody"].Broken = true;
					yield return new WaitForSeconds(xxx.rr(0.01f, 0.1f));
					LB["LowerBody"].Broken = false;
				}

			}

			if ( trollMode == 2 )
			{
				seconds = xxx.rr(1.1f, 4.1f);
				
				TrollPose[2].RunMove();
				yield return new WaitForSeconds(0.4f);
				TrollPose[3].RunMove();
				yield return new WaitForSeconds(0.2f);
				RB["LowerArmFront"].AddForce(Vector2.down * TotalWeight * 1.1f, ForceMode2D.Impulse);

			}

			if ( trollMode == 3 )
			{
				
				TrollPose[4].RunMove();
				yield return new WaitForSeconds(0.4f);
				LB["LowerArm"].Broken = LB["LowerArmFront"].Broken = true;
				TrollPose[5].RunMove();
				yield return new WaitForSeconds(0.1f);
				LB["LowerArm"].Broken = LB["LowerArmFront"].Broken = false;
				TrollPose[5].RunMove();
				RB["LowerArmFront"].AddForce(Vector2.down * TotalWeight * 0.4f, ForceMode2D.Impulse);
				RB["LowerArm"].AddForce(Vector2.down * TotalWeight * 0.4f, ForceMode2D.Impulse);
				timer = Time.time + 0.1f;

				while ( Time.time < timer )
				{
					LB["UpperArm"].Broken = LB["UpperArmFront"].Broken = true;
					//RB["LowerArmFront"].AddForce((Vector2.down + Vector2.left) * TotalWeight * Time.fixedDeltaTime * 100f);
					//RB["LowerArm"].AddForce((Vector2.down + Vector2.left) * TotalWeight * Time.fixedDeltaTime * 100f);
					yield return new WaitForFixedUpdate();
				}
				LB["UpperArm"].Broken = LB["UpperArmFront"].Broken = false;
				seconds = Time.time + xxx.rr(1.1f, 4.1f);
				NpcPose.Clear(PBO);
				while ( Time.time < seconds )
				{
					RB["Head"].AddForce(Vector2.right * TotalWeight * Time.fixedDeltaTime * 100 * xxx.rr(0.1f, 0.2f), ForceMode2D.Impulse);
					yield return new WaitForSeconds(xxx.rr(0.1f, 0.5f));
					RB["LowerBody"].AddTorque(xxx.rr(10,20) );
					yield return new WaitForSeconds(xxx.rr(0.1f, 0.5f));
				}
			}

			if ( trollMode == 4 )
			{
				seconds = Time.time + xxx.rr(0.5f, 1.1f);
				
				TrollPose[6].RunMove();
				
				while ( Time.time < seconds )
				{
					RB["LowerArm"].AddForce(UnityEngine.Random.insideUnitCircle * TotalWeight * 0.1f, ForceMode2D.Impulse);
					yield return new WaitForSeconds(xxx.rr(0.01f, 0.2f));
				}
			}


			if ( trollMode == 5 )
			{
				seconds = Time.time + xxx.rr(0.5f, 3.1f);
				
				TrollPose[10].RunMove();
				yield return new WaitForSeconds(0.1f);
				while ( Time.time < seconds )
				{
					TrollPose[11].RunMove();
					RB["LowerArm"].AddForce(Vector2.up + UnityEngine.Random.insideUnitCircle, ForceMode2D.Impulse);
					yield return new WaitForSeconds(xxx.rr(0.1f, 0.5f));
					TrollPose[xxx.rr(11,12)].RunMove();
					RB["LowerArmFront"].AddForce(Vector2.up + UnityEngine.Random.insideUnitCircle, ForceMode2D.Impulse);
					yield return new WaitForSeconds(xxx.rr(0.1f, 0.5f));
				}
				
				
			}

			if ( trollMode == 15 )
			{
				TrollPose[6].RunMove();
				timer = Time.time + xxx.rr(0.1f,0.15f);
				while (Time.time < timer) {
					RB["LowerArm"].AddForce(UnityEngine.Random.insideUnitCircle * TotalWeight * 0.1f, ForceMode2D.Impulse);
					yield return new WaitForSeconds(xxx.rr(0.3f,0.5f));
				}
				
					
				timer = Time.time + xxx.rr(0.1f,0.15f);
				TrollPose[7].RunMove();
				while (Time.time < timer) {
					RB["LowerArmFront"].AddForce(UnityEngine.Random.insideUnitCircle * TotalWeight * 0.1f, ForceMode2D.Impulse);
					yield return new WaitForSeconds(xxx.rr(0.3f,0.5f));
				}

				timer = Time.time + xxx.rr(0.3f,0.5f);
				TrollPose[8].RunMove();
				while (Time.time < timer) {
					RB["LowerArm"].AddForce(UnityEngine.Random.insideUnitCircle * TotalWeight * 0.1f, ForceMode2D.Impulse);
					yield return new WaitForSeconds(xxx.rr(0.3f,0.5f));
				}

				timer = Time.time + xxx.rr(0.3f,0.5f);
				TrollPose[6].RunMove();
				while ( Time.time < seconds )
				{
					RB["LowerArm"].AddForce(UnityEngine.Random.insideUnitCircle * TotalWeight * 0.1f, ForceMode2D.Impulse);
					yield return new WaitForSeconds(xxx.rr(0.01f, 0.2f));
				}


				LB["UpperArm"].Broken = LB["UpperArmFront"].Broken = true;


				yield return new WaitForSeconds(xxx.rr(0.3f,0.5f));

				LB["UpperArm"].Broken = LB["UpperArmFront"].Broken = false;

				
			}

			NpcPose.Clear(PBO);
			LB["LowerBody"].Broken = false;
			CurrentAction = "Thinking";
			PBO.DesiredWalkingDirection     = 0;

			if ( NPC.MyTargets.enemy && NPC.MyTargets.enemy.EnhancementTroll )
			{
				if (NPC.MyTargets.enemy.Memory.GetNpcStat(NPC.NpcId, "HitThem")< 3)
				{
					NPC.MyTargets.enemy.Action.ClearAction();
					if (!NPC.MyTargets.enemy.FacingToward(Head.position))
					{
						NPC.MyTargets.enemy.Flip();
						yield return new WaitForFixedUpdate();
						yield return new WaitForFixedUpdate();
					}
					yield return new WaitForSeconds(1);
					if (NPC.MyTargets.enemy.HurtLevel < 3) NPC.MyTargets.enemy.SayRandom("response");
					else NPC.MyTargets.enemy.SayRandom("hurt");

				}
			}

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: WITNESS     ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionWitness()
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
				Sayings.AddRange(SmackText[cat] );
			}
			
			string SmackTalk = Sayings.PickRandom();

			NPC.Say(SmackTalk, 3f);

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

			NPC.Mojo.Feel("Bored", -5f);
			NPC.Mojo.Feel("Fear", 1f);

			NPC.MyTargets.enemy.Mojo.Feel("Angry", xxx.rr(-5,-1));

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

			int wMode = xxx.rr(1,8);
			float seconds = 1;
			
			if ( wMode == 1 )
			{
				seconds = Time.time + xxx.rr(2.1f, 4.1f);
				wPose[1].RunMove();
			}
			if ( wMode == 2 )
			{
				seconds = Time.time + xxx.rr(2.1f, 4.1f);
				wPose[2].RunMove();
			}
			if ( wMode == 3 )
			{
				seconds = Time.time + xxx.rr(2.1f, 4.1f);
				wPose[3].RunMove();
			}
			if ( wMode == 4 )
			{
				seconds = Time.time + xxx.rr(2.1f, 4.1f);
				wPose[4].RunMove();
			}

			while ( Time.time < seconds )
			{
				yield return new WaitForSeconds(0.1f);
			}
			
			NpcPose.Clear(PBO);
			ClearAction();

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: GEAR UP     ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionGearUp()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionGearUp()");
			if (!NPC.MyTargets.item)
			{
				ClearAction();
				yield break;
			}

			NpcArsenal arsenal = null;

			foreach ( NpcArsenal arse in NpcArsenal.Arsenals )
			{
				if (arse.P == NPC.MyTargets.item) arsenal = arse;
			}



			float dist = float.MaxValue;

			

			while ( NPC.MyTargets.item && arsenal.PropsList.Count > 0 && !NPC.CancelAction)
			{
				Vector3 target = arsenal.transform.position;

				if (!NPC.FacingToward(target)) {

					NPC.Flip();
					yield return new WaitForFixedUpdate();
				}

				dist = Head.position.x - target.x;

				if (Mathf.Abs(dist) > 1.00f)
				{
					if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 10f;

					yield return new WaitForSeconds(0.5f);
				}


				yield return new WaitForFixedUpdate();

				if (Mathf.Abs(dist) < 1.0f) break;

				//if (NPC.CheckInterval(1.5f) && NPC.CheckRandomSituations(true)) yield break;

			}

			NpcHand hand = Hands.PickRandom();

			StartCoroutine(hand.IPointArm(arsenal.transform, 2));

			//	Check if we got the item
			arsenal.DropItem(Hands);

			NPC.ArsenalTimer = Time.time + 160f;

			CurrentAction = "Thinking";
			//PBO.OverridePoseIndex       = (int)PoseState.Rest;
			PBO.DesiredWalkingDirection = 0;
		}



		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: RECRUIT   /////////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionRecruit()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionRecruit()");
			if (!NPC.MyTargets.person)
			{
				ClearAction();
				yield break;
			}

			float dist = float.MaxValue;

			Transform head = NPC.MyTargets.person.Limbs[0].transform;




			NpcHand hand = (NPC.FH.IsHolding && NPC.FH.Tool.props.canRecruit) ? NPC.FH : NPC.BH;

			if ( xxx.rr( 1, 4 ) == 2 )
			{
				hand.Tool.TossTo(head.position, 5,5);
				yield return new WaitForSeconds(2);
				ClearAction();
				yield break;
			}

			NPC.NoGhost.Add(head.root);

			hand.Tool.NoGhost.Add(head.root);

			NpcChip chip = hand.Tool.P.GetComponent<NpcChip>();
			if (!chip) { ClearAction(); yield break; }


			xxx.ToggleCollisions(head,Head,true,true);
			xxx.ToggleCollisions(head.root,hand.Tool.P.transform,true,false);

			Coroutine pointy  = null;

			float Timeout = Time.time + 5f;

			while ( head && NPC.MyTargets.person && !NPC.CancelAction && hand.GB.isHolding && Time.time < Timeout)
			{
				Vector3 target = head.position;



				if (NPC.CheckInterval(0.5f) && NPC.TeamId != chip.TeamId ) chip.Use(null);

				dist = Head.position.x - target.x;

				if (Mathf.Abs(dist) < 2.0f && pointy == null) {

					pointy  = hand.StartCoroutine(hand.IPoint(head, 1.5f));

				}

				if (Mathf.Abs(dist) > 0.5f)
				{
					if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 2f;

				} 
				else
				if (Mathf.Abs(dist) < 0.1f)
				{
					if (PBO.DesiredWalkingDirection > -1f) PBO.DesiredWalkingDirection = -2f;
				}
				else
				{
					PBO.DesiredWalkingDirection = 0f;
					yield return new WaitForSeconds(3.5f);
					if (pointy != null) hand.StopCoroutine(pointy);
					break;
				}



				if (Mathf.Abs(dist) < 0.5f) break;

				if (!NPC.FacingToward(target)) {

					NPC.Flip();
					yield return new WaitForFixedUpdate();
				}

				yield return new WaitForFixedUpdate();


			}
			if (head) NPC.NoGhost.Remove(head.root);
			if (pointy != null) hand.StopCoroutine(pointy);
			//	Check if we got the item

			CurrentAction = "Thinking";
			//PBO.OverridePoseIndex       = (int)PoseState.Rest;
			PBO.DesiredWalkingDirection = 0;
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: UPGRADE     ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionUpgrade()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionUpgrade()");
			if (!NPC.MyTargets.prop)
			{
				ClearAction();
				yield break;
			}

			

			NpcHand hand = (NPC.FH.IsHolding && NPC.FH.Tool.props == NPC.MyTargets.prop) ? NPC.FH : NPC.BH;

			NpcChip chip = NPC.MyTargets.prop.P.GetComponent<NpcChip>();

			NPC.Say("And now I shall become even more powerful!");

			hand.LB.Broken = hand.uArmL.Broken = true;
			
			float timeout = Time.time + 6f;
			xxx.ToggleCollisions(chip.gameObject.transform, Head,true,false);
			while ( hand.GB.isHolding && Time.time < timeout )
			{
				hand.RB.AddForce( (Head.position - hand.RB.transform.position) * TotalWeight * Time.fixedDeltaTime * 150);
				yield return new WaitForFixedUpdate();
			}

			hand.LB.Broken = hand.uArmL.Broken = false;
			CurrentAction = "Thinking";
			//PBO.OverridePoseIndex       = (int)PoseState.Rest;
			PBO.DesiredWalkingDirection = 0;
		}

		
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: KICK        ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionKick()
		{
			if (!NPC.MyTargets.enemy)
			{
				ClearAction();
				yield break;
			}
			//kickType = Kicks.Soccer;
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionKick()");

			if (!NPC.FacingToward(NPC.MyTargets.enemy))
			{
				NPC.Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}

			MinMax minMax = new MinMax();

			minMax.Min = 1.0f;
			minMax.Max = 1.5f;

			float timeout = Time.time + 3f;

			//	reposition self to kick 
			float dist = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);

			while ( dist < minMax.Min || dist > minMax.Max )
			{
				if (dist < minMax.Min)				PBO.DesiredWalkingDirection	= -2f;
				else if (dist > minMax.Max + 0.5f)	PBO.DesiredWalkingDirection	= 2f;
				else break;
				if (!NPC.MyTargets.enemy) yield break;

				dist = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);

				if (Time.time > timeout)
				{
					ClearAction();
					yield break;
				}

				yield return new WaitForFixedUpdate();
			}

			if (!NPC.MyTargets.enemy)
			{
				ClearAction();
				yield break;
			}

			if (!NPC.FacingToward(NPC.MyTargets.enemy))
			{
				NPC.Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}

			if (!NPC.MyTargets.enemy)
			{
				ClearAction();
				yield break;
			}

			NPC.CanGhost = false;

			NPC.Mojo.Feelings["Chicken"] = 0;


			NPC.CalculateThreatLevel();

			NPC.CommonPoses["frontkick"].RunMove();
			yield return new WaitForSeconds(xxx.rr(0.5f,1.0f));

			if (!NPC.MyTargets.enemy)
			{
				ClearAction();
				yield break;
			}

			dist = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);

			for(int i = xxx.rr(1,5); --i >= 0;) { 
				if ( dist > minMax.Max + 0.5f )
				{
					RB["UpperBody"].AddForce(new Vector3(-Facing, -1) * TotalWeight * 2f, ForceMode2D.Impulse);
				
					yield return new WaitForSeconds(xxx.rr(0.1f,0.3f));
					dist = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);
				}
			}

			if ( dist < minMax.Max + 0.5f )
			{
				LB["Foot"].ImmuneToDamage = LB["FootFront"].ImmuneToDamage = true;
				LB["LowerLeg"].ImmuneToDamage = LB["LowerLegFront"].ImmuneToDamage = true;
				LB["UpperLeg"].ImmuneToDamage = LB["UpperLegFront"].ImmuneToDamage = true;

				NpcPose.Clear(PBO);

				yield return new WaitForFixedUpdate();

				float origMass = RB["FootFront"].mass;

				float kickStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;

				RB["Foot"].mass *= (2 + kickStrength);

				if (NPC.IsUpright) { 
					xxx.FixCollisions( LB["Foot"].transform );

					Vector2 kickDir = new Vector2(-NPC.Facing, xxx.rr(-0.5f,0.5f));
					RB["Foot"].AddForce(kickDir * NPC.TotalWeight * xxx.rr(5.1f,15.1f) * kickStrength, ForceMode2D.Impulse);
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

				if (NPC.MyTargets.enemy && !NPC.MyTargets.enemy.RunningSafeCollisions)
				{
					NPC.MyTargets.enemy.StartCoroutine(NPC.MyTargets.enemy.ISafeCollisions());
				}

				timeout = Time.time + 5f;

				while ( !LB["Foot"].IsOnFloor && Time.time < timeout )
				{
					yield return new WaitForSeconds(0.5f);
				}

				RB["Foot"].mass = origMass;
				
			}

			NPC.CanGhost = true;

			LB["UpperLegFront"].Broken = false;
			LB["LowerLegFront"].Broken = false;

			yield return new WaitForSeconds(1);

			//LB["Foot"].ImmuneToDamage     = LB["FootFront"].ImmuneToDamage	  = false;
			//LB["LowerLeg"].ImmuneToDamage = LB["LowerLegFront"].ImmuneToDamage = false;

			Invoke("KickRecover", 1.0f);

			if (NPC.EnhancementTroll && xxx.rr(1,7) == 2)
			{
				CurrentAction = "Troll";
				
				StartCoroutine(IActionTroll());
				yield break;
			}

			CurrentAction = "Thinking";

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: STOMP       ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionStomp()
		{
			if (!NPC.MyTargets.enemy)
			{
				ClearAction();
				yield break;
			}

			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionStomp()");

			if (!NPC.FacingToward(NPC.MyTargets.enemy))
			{
				NPC.Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}

			bool doKick = ( Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.RB["Foot"].position.x) >
				 Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.RB["Head"].position.x) ) ;

			if ((Head.position.x < NPC.MyTargets.enemy.RB["Foot"].position.x && Head.position.x > NPC.MyTargets.enemy.RB["Head"].position.x) ||
				(Head.position.x > NPC.MyTargets.enemy.RB["Foot"].position.x && Head.position.x < NPC.MyTargets.enemy.RB["Head"].position.x)) doKick = false;

			MinMax minMax = new MinMax()
			{
				Min = doKick ? 0.2f : 0.4f,
				Max = doKick ? 0.5f : 1.0f,
			};

			float origMass     = RB["FootFront"].mass;
			float kickStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;
			float timeout      = Time.time + 5f;
			float dist         = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);

			while ( dist < minMax.Min || dist > minMax.Max )
			{
				if (dist < minMax.Min)				PBO.DesiredWalkingDirection	= -2f;
				else if (dist > minMax.Max + 0.5f)	PBO.DesiredWalkingDirection	= 2f;
				else break;
				if (Time.time > timeout || !NPC.MyTargets.enemy)
				{
					ClearAction();
					yield break;
				}

				dist = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);

				yield return new WaitForFixedUpdate();
			}




			NPC.Mojo.Feelings["Chicken"] = 0;

			dist = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);

			LB["Foot"].ImmuneToDamage		= LB["FootFront"].ImmuneToDamage		= true;
			LB["LowerLeg"].ImmuneToDamage	= LB["LowerLegFront"].ImmuneToDamage = true;



			if (doKick)
			{ 
				if( NPC.MyTargets.enemy.Facing != NPC.Facing) { 
					NPC.CommonPoses["soccerkick"].RunMove();
					yield return new WaitForSeconds(xxx.rr(1.0f,1.2f));
				} 
				
				NPC.CanGhost = false;

				NpcPose.Clear(PBO);

				yield return new WaitForFixedUpdate();


				if (!NPC.MyTargets.enemy)
				{
					ClearAction();
					yield break;
				}
				xxx.ToggleCollisions( RB["FootFront"].transform, NPC.MyTargets.enemy.Head, true, false);
				xxx.ToggleCollisions( RB["LowerLegFront"].transform, NPC.MyTargets.enemy.Head, true, false);

				RB["FootFront"].mass *= (1 + kickStrength);
				RB["Foot"].mass *= (2 + kickStrength);
				if (NPC.IsUpright) {
					if (NPC.EnhancementTroll && xxx.rr(1,7) == 2)
					{
						NPC.Say("Kiss my Converse!", 2);
					}
				
					
					xxx.FixCollisions( LB["FootFront"].transform );
					xxx.FixCollisions( LB["LowerLegFront"].transform );
					xxx.FixCollisions( LB["UpperLegFront"].transform );
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

				timeout = Time.time + 5f;

				while ( !LB["FootFront"].IsOnFloor && Time.time < timeout )
				{
					yield return new WaitForSeconds(0.5f);
				}

				

			}
			else
			{

				//	STOMP
				//
				NPC.CommonPoses["frontkick"].RunMove();
				float force = xxx.rr(1.2f,1.7f);
				RB["UpperBody"].AddForce(Vector2.up  * TotalWeight * force, ForceMode2D.Impulse);
				RB["MiddleBody"].AddForce(Vector2.up * TotalWeight * force, ForceMode2D.Impulse);
				RB["LowerBody"].AddForce(Vector2.up  * TotalWeight * force, ForceMode2D.Impulse);

				yield return new WaitForFixedUpdate();

				Vector3 diff          = Vector3.right + Vector3.down;
				Vector3 angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg * Facing ));

				float timer = Time.time + 0.5f;

				while ( Time.time < timer ) { 
					RB["MiddleBody"].MoveRotation(Quaternion.RotateTowards(
					RB["MiddleBody"].transform.rotation, Quaternion.Euler(angleVelocity), 3f * Time.fixedDeltaTime * 100f));
					yield return new WaitForFixedUpdate();
				}
				NPC.CommonPoses["soccerkick"].RunMove();
				yield return new WaitForFixedUpdate();

				RB["Foot"].mass *= (2 + kickStrength);

				xxx.FixCollisions( LB["FootFront"].transform );
				xxx.FixCollisions( LB["Foot"].transform );
				xxx.ToggleCollisions( LB["Foot"].transform, NPC.MyTargets.enemy.Head, true, false );
				if (NPC.MyTargets.enemy && !NPC.MyTargets.enemy.RunningSafeCollisions)
				{
					NPC.MyTargets.enemy.StartCoroutine(NPC.MyTargets.enemy.ISafeCollisions(1));
				}
				yield return new WaitForFixedUpdate();
			NpcPose.Clear(PBO);

				RB["Foot"].AddForce(Vector2.down * TotalWeight * kickStrength *  xxx.rr(5.0f,9.9f), ForceMode2D.Impulse);

				yield return new WaitForSeconds(1f);

			}

			//CanGhost = true;

			
			NpcPose.Clear(PBO);

			RB["FootFront"].mass       = origMass;
			RB["Foot"].mass            = origMass;

			LB["UpperLegFront"].Broken = false;
			LB["LowerLegFront"].Broken = false;
			
			xxx.ToggleCollisions( RB["FootFront"].transform, NPC.MyTargets.enemy.Head, false, false);
			xxx.ToggleCollisions( RB["LowerLegFront"].transform, NPC.MyTargets.enemy.Head, false, false);


			yield return new WaitForSeconds(1);



			//LB["Foot"].ImmuneToDamage     = LB["FootFront"].ImmuneToDamage	  = false;
			//LB["LowerLeg"].ImmuneToDamage = LB["LowerLegFront"].ImmuneToDamage = false;
			NPC.GetUp = true;
			Invoke("KickRecover", 1.0f);

			CurrentAction = "Thinking";

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: ATTACK      ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionAttack()
		{
			if (!NPC.MyTargets.enemy)
			{
				ClearAction();
				yield break;
			}

			NpcBehaviour enemy = NPC.MyTargets.enemy;

			if (!NPC.FacingToward(enemy))
			{
				NPC.Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}

			yield return StartCoroutine( 
				ISubActWalkTo(enemy.Head, new MinMax(0.5f, 0.9f), xxx.rr(5,7))	);


			if (Mathf.Abs(Head.position.x - enemy.Head.position.x) > 1f) { ClearAction(); yield break; }

			Coroutine attack;

			int attackCount = xxx.rr(2,5);

			while (--attackCount >= 0 || !enemy) 
			{ 

				NpcHand hand = Hands.PickRandom();

				if (!hand.IsHolding) hand = hand.AltHand;
				if (!hand.IsHolding) yield break;

				string[] targets;
				string target;

				NpcTool weapon = hand.Tool;

				//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
				//	SHIV                ///////////////////////////////////
				//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
				if ( weapon.props.isShiv )
				{
					if (xxx.rr(1,5) == 3) NPC.SayRandom( "knife" );
					weapon.props.NoGhost = true;
					enemy.CanGhost       = false;
				
					xxx.ToggleCollisions(weapon.T,enemy.Head.root, true, false);
				
					enemy.DisableFlip               = true;
					NPC.DisableFlip                 = true;


					if (xxx.rr(1,3)==2 && enemy.Facing == Facing) 
					{ 
						yield return StartCoroutine(
						ISubActGrab( enemy.RB["Head"], hand.AltHand, xxx.rr(5,7)) );

						xxx.ToggleCollisions(hand.T, enemy.Head.root, true, false);
				
						weapon.props.NoGhost = true;
				
						NPC.CanGhost                    = false;
						enemy.CanGhost                  = false;
				
						xxx.ToggleCollisions(weapon.T,enemy.Head.root, true, false);
				
						enemy.DisableFlip               = true;
						NPC.DisableFlip                 = true;

						int stabs = xxx.rr(2,4);
						float timer;
						while ( hand.AltHand.IsGrabbing && hand.AltHand.GrabJoint && --stabs >= 0)
						{
							if (Facing != NPC.MyTargets.enemy.Facing) break;

							timer = Time.time + 0.5f;
							while ( Time.time < timer )
							{
								hand.RB.AddForce(Vector2.right * -Facing * TotalWeight * Time.fixedDeltaTime * 500f);
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

						if (hand.AltHand.GrabJoint)  {
							hand.AltHand.GrabJoint.enabled = false;
							UnityEngine.Object.DestroyImmediate((UnityEngine.Object)hand.AltHand.GrabJoint);
						}

					} else { 

						targets = new string[] {"Head", "UpperBody", "MiddleBody", "LowerBody"};
						target  = targets.PickRandom();
				
						StartCoroutine( 
						ISubActWalkTo(enemy.Head, new MinMax(0.5f, 0.9f), xxx.rr(5,7))	);
				
						yield return StartCoroutine(
						ISubActStab( enemy.RB[target].transform, hand ));
					}
				}
				//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
				//	BAT                 ///////////////////////////////////
				//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
				else if ( weapon.props.Traits["club"] )
				{
					if (xxx.rr(1,5) == 3) NPC.SayRandom( "bat" );
					hand.swingState = SwingState.ready;

					StartCoroutine( 
					ISubActWalkTo(enemy.Head, new MinMax(0.5f, 0.9f), xxx.rr(5,7))	);

					yield return StartCoroutine(IActionClubStrikes(enemy.Head, hand));
				}
				//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
				//	SWORD               ///////////////////////////////////
				//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
				else if ( weapon.props.Traits["sword"] )
				{
					if (xxx.rr(1,5) == 3) NPC.SayRandom( "sword" );
					hand.swingState = SwingState.ready;

					enemy.DisableFlip               = true;
					NPC.DisableFlip                 = true;

					StartCoroutine( 
					ISubActWalkTo(enemy.Head, new MinMax(0.9f, 1.2f), xxx.rr(5,7))	);

					yield return StartCoroutine(IActionSwordStrikes(enemy.Head, hand));
				}


				NpcPose.Clear(NPC.PBO);

				weapon.props.NoGhost = false;
				NPC.CanGhost         = true;
				enemy.CanGhost       = true;
				enemy.DisableFlip    = false;
				NPC.DisableFlip      = false;

			}

			ClearAction();
			

		}


		public IEnumerator IActionAttackx()
		{
			if (!NPC.MyTargets.enemy)
			{
				ClearAction();
				yield break;
			}

			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionAttack()");

			if (!NPC.FacingToward(NPC.MyTargets.enemy))
			{
				NPC.Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}

			bool doOverheadSwing= ( Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.RB["Foot"].position.x) >
				 Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.RB["Head"].position.x) ) ;

			if ((Head.position.x < NPC.MyTargets.enemy.RB["Foot"].position.x && Head.position.x > NPC.MyTargets.enemy.RB["Head"].position.x) ||
				(Head.position.x > NPC.MyTargets.enemy.RB["Foot"].position.x && Head.position.x < NPC.MyTargets.enemy.RB["Head"].position.x)) doOverheadSwing = false;

			MinMax minMax = new MinMax()
			{
				Min = doOverheadSwing ? 0.2f : 0.4f,
				Max = doOverheadSwing ? 0.5f : 1.0f,
			};

			float origMass     = RB["FootFront"].mass;
			float kickStrength = (NPC.Mojo.Stats["Strength"] / 100) + 1;
			float timeout      = Time.time + 5f;
			float dist         = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);

			while ( dist < minMax.Min || dist > minMax.Max )
			{
				if (dist < minMax.Min)				PBO.DesiredWalkingDirection	= -2f;
				else if (dist > minMax.Max + 0.5f)	PBO.DesiredWalkingDirection	= 2f;

				if (Time.time > timeout || !NPC.MyTargets.enemy)
				{
					ClearAction();
					yield break;
				}

				dist = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);

				yield return new WaitForFixedUpdate();
			}

			Props SProp;
			if (NPC.FH.IsHolding && NPC.FH.Tool.props.canStrike) SProp = NPC.FH.Tool.props;
			if (NPC.FH.IsHolding && NPC.FH.Tool.props.canStrike) SProp = NPC.BH.Tool.props;


			NPC.Mojo.Feelings["Chicken"] = 0;

			dist = Mathf.Abs(Head.position.x - NPC.MyTargets.enemy.Head.position.x);

			LB["Foot"].ImmuneToDamage		= LB["FootFront"].ImmuneToDamage		= true;
			LB["LowerLeg"].ImmuneToDamage	= LB["LowerLegFront"].ImmuneToDamage = true;



			if (doOverheadSwing)
			{ 
				if( NPC.MyTargets.enemy.Facing != NPC.Facing) { 
					NPC.CommonPoses["low_club_1"].RunMove();
					yield return new WaitForSeconds(xxx.rr(1.0f,1.2f));
				} 
				
				NPC.CanGhost = false;

				NpcPose.Clear(PBO);

				yield return new WaitForFixedUpdate();


				if (!NPC.MyTargets.enemy)
				{
					ClearAction();
					yield break;
				}
				xxx.ToggleCollisions( RB["FootFront"].transform, NPC.MyTargets.enemy.Head, true, false);
				xxx.ToggleCollisions( RB["LowerLegFront"].transform, NPC.MyTargets.enemy.Head, true, false);

				RB["FootFront"].mass *= (1 + kickStrength);
				RB["Foot"].mass *= (2 + kickStrength);
				if (NPC.IsUpright) {
					
					xxx.FixCollisions(hand.PB.transform);
					
					yield return new WaitForFixedUpdate();
					Vector2 swingDir = hand.transform.position - NPC.MyTargets.enemy.Head.position;

					

				}

				timeout = Time.time + 5f;

				while ( !LB["FootFront"].IsOnFloor && Time.time < timeout )
				{
					yield return new WaitForSeconds(0.5f);
				}

			}
			else
			{

				//	STOMP
				//
				NPC.CommonPoses["frontkick"].RunMove();
				float force = xxx.rr(1.2f,1.7f);
				RB["UpperBody"].AddForce(Vector2.up  * TotalWeight * force, ForceMode2D.Impulse);
				RB["MiddleBody"].AddForce(Vector2.up * TotalWeight * force, ForceMode2D.Impulse);
				RB["LowerBody"].AddForce(Vector2.up  * TotalWeight * force, ForceMode2D.Impulse);

				yield return new WaitForFixedUpdate();

				Vector3 diff          = Vector3.right + Vector3.down;
				Vector3 angleVelocity = new Vector3(0f, 0f, (Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg * Facing ));

				float timer = Time.time + 0.5f;

				while ( Time.time < timer ) { 
					RB["MiddleBody"].MoveRotation(Quaternion.RotateTowards(
					RB["MiddleBody"].transform.rotation, Quaternion.Euler(angleVelocity), 3f * Time.fixedDeltaTime * 100f));
					yield return new WaitForFixedUpdate();
				}
				NPC.CommonPoses["soccerkick"].RunMove();
				yield return new WaitForFixedUpdate();

				RB["Foot"].mass *= (2 + kickStrength);

				xxx.FixCollisions( LB["FootFront"].transform );
				xxx.FixCollisions( LB["Foot"].transform );
				xxx.ToggleCollisions( LB["Foot"].transform, NPC.MyTargets.enemy.Head, true, false );
				if (NPC.MyTargets.enemy && !NPC.MyTargets.enemy.RunningSafeCollisions)
				{
					NPC.MyTargets.enemy.StartCoroutine(NPC.MyTargets.enemy.ISafeCollisions(1));
				}
				yield return new WaitForFixedUpdate();
			NpcPose.Clear(PBO);

				RB["Foot"].AddForce(Vector2.down * TotalWeight * kickStrength *  xxx.rr(5.0f,9.9f), ForceMode2D.Impulse);

				yield return new WaitForSeconds(1f);

			}

			//CanGhost = true;

			
			NpcPose.Clear(PBO);

			RB["FootFront"].mass       = origMass;
			RB["Foot"].mass            = origMass;

			LB["UpperLegFront"].Broken = false;
			LB["LowerLegFront"].Broken = false;
			
			xxx.ToggleCollisions( RB["FootFront"].transform, NPC.MyTargets.enemy.Head, false, false);
			xxx.ToggleCollisions( RB["LowerLegFront"].transform, NPC.MyTargets.enemy.Head, false, false);


			yield return new WaitForSeconds(1);



			//LB["Foot"].ImmuneToDamage     = LB["FootFront"].ImmuneToDamage	  = false;
			//LB["LowerLeg"].ImmuneToDamage = LB["LowerLegFront"].ImmuneToDamage = false;

			Invoke("KickRecover", 1.0f);

			CurrentAction = "Thinking";

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: SHOOT       ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionShoot(bool isDefense=false)
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionShoot()");
			CurrentAction = "Shoot";

			LimbBehaviour limbTarget = null;

			if (!NPC.MyTargets.enemy || !NPC.MyTargets.enemy.PBO)
			{
				StartCoroutine(IActionWait());
				yield break;
			}

			float dist;
			NPC.FH.FixHandsPose((int)PoseState.Walking);
			NPC.BH.FixHandsPose((int)PoseState.Walking);

			if (!NPC.MyTargets.enemy.MyFights.Contains(NPC)) {
				NPC.MyTargets.enemy.MyFights.Add(NPC);
			}
			if ( NPC.MyTargets.enemy && xxx.ValidateEnemyTarget(NPC.MyTargets.enemy.PBO) && NPC.MyTargets.enemy.ThreatLevel > -5 )
			{
				NpcHand hand = Hands.PickRandom();
				if (!hand.IsHolding || !hand.Tool.props.canShoot) hand = hand.AltHand;


				if (NPC.FH.IsHolding || NPC.BH.IsHolding)
				{ 

					if (xxx.rr(1,5) == 2) NPC.SayRandom( hand.Tool.props.Traits["handgun"] ? "handgun" : "rifle" );

					//float aimChance = CheckMyShot(NPC.MyTargets.enemy.Head, hand.Tool.T);

					bool showMercy = xxx.rr(1,100) >= (NPC.Mojo.Traits["Mean"] + NPC.Mojo.Feelings["Angry"]) / 2;

					bool doDualGrip = false;

					while ( NPC.CanThink() && NPC.MyTargets.enemy && NPC.MyTargets.enemy.PBO && xxx.ValidateEnemyTarget(NPC.MyTargets.enemy.PBO) && !NPC.CancelAction )
					{
						if (NPC.MyTargets.enemy.ThreatLevel <= -5 && showMercy) {

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

						doDualGrip = false;


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
						float hesitate = xxx.rr(0.5f,3.1f);


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
								doCommonPose = true;
								hesitate    += 1f;
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

							if (showMercy && (NPC.MyTargets.enemy.ThreatLevel <= -5 )) {

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

					if (NPC.MyTargets.enemy && (!xxx.ValidateEnemyTarget(NPC.MyTargets.enemy.PBO) || NPC.MyTargets.enemy.ThreatLevel <= -5 )) { 
						if (xxx.rr(1,5) == 2) NPC.SayRandom("mercy");
						float igTime = Time.time + xxx.rr(5,30);
						NPC.TimedNpcIgnored[igTime] = NPC.MyTargets.enemy;

						if (NPC.MyTargets.enemy.MyFights.Contains(NPC)) NPC.MyTargets.enemy.MyFights.Remove(NPC);
						if (NPC.MyFights.Contains(NPC.MyTargets.enemy)) NPC.MyFights.Remove(NPC.MyTargets.enemy);
						hand.Stop();
						ClearAction();
						yield break;
					}

				}

				if (NPC.MyTargets.enemy) { 
					float igTime = Time.time + xxx.rr(5,30);
					NPC.TimedNpcIgnored[igTime] = NPC.MyTargets.enemy;
				}

				if (NPC.MyTargets.enemy.MyFights.Contains(NPC)) NPC.MyTargets.enemy.MyFights.Remove(NPC);
				if (NPC.MyFights.Contains(NPC.MyTargets.enemy)) NPC.MyFights.Remove(NPC.MyTargets.enemy);

				yield return new WaitForFixedUpdate();

				yield return new WaitForSeconds(xxx.rr(0.1f, 3f));

			}


			CurrentAction = "Thinking";
		}

	
		public IEnumerator ISpinIdleWeapon()
		{
			yield break;

			for (; ; ) 
			{
				string[] okActions = { "Wait", "Wander", "Thinking", "WatchEvent", "Fidget", };

				do
				{
					yield return new WaitForSeconds(xxx.rr(1f,3f));
				} while (!okActions.Contains(CurrentAction));

				
				yield return new WaitForEndOfFrame();

				NpcHand hand = NPC.FH.IsHolding ? NPC.FH : NPC.BH;
				Props prop = hand.Tool.props;

				//if (!hand.IsHolding || hand.Tool.props.canStrike) yield break;

				float timer = Time.time + 1f;

				while ( Time.time < timer )
				{
					//hand.RB.AddForce(Vector2.up * TotalWeight * Time.fixedDeltaTime * 100);
					yield return new WaitForFixedUpdate();
				}

				//WheelJoint2D wJoint = hand.gameObject.AddComponent<WheelJoint2D>() as WheelJoint2D;
				//wJoint.transform.SetParent(hand.transform, false);
				//wJoint.suspension = new JointSuspension2D()
	//            {
				//	dampingRatio = 0.7f,
				//	angle        = 90f,
				//	frequency    = 6f,
				//};
				//SpringJoint2D joint = null;
				HingeJoint2D joint = null;
				//hand.RB.AddForce(Vector2.down * TotalWeight);
				
				if ( hand.GB.TryGetComponent<FixedJoint2D>( out FixedJoint2D fJoint ) )
				{
					xxx.ToggleCollisions(fJoint.connectedBody.transform, Head, false, true);
					//joint = hand.gameObject.AddComponent<SpringJoint2D>();
					//joint.anchor = fJoint.anchor;
					//joint.connectedBody   = fJoint.connectedBody;//NPC.CurrentEventInfo.PBs[0].rigidbody;
					//joint.connectedAnchor = fJoint.connectedAnchor; //endPoint + direction;
					//joint.autoConfigureConnectedAnchor  =true;

					joint = hand.gameObject.AddComponent<HingeJoint2D>();
					joint.anchor = fJoint.anchor;
					joint.connectedBody   = fJoint.connectedBody;//NPC.CurrentEventInfo.PBs[0].rigidbody;
					joint.connectedAnchor = fJoint.connectedAnchor; //endPoint + direction;
					joint.autoConfigureConnectedAnchor  =true;
					joint.useLimits = false;
					
					xxx.ToggleWallCollisions(joint.connectedBody.transform);
					
					
					//springJoint2D.anchor = ActiveSingleSelected.transform.InverseTransformPoint( startPos );

					//AppliedBandageBehaviour bandageBehaviour = joint.gameObject.AddComponent<AppliedBandageBehaviour>();
					//bandageBehaviour.WireColor               = Color.white;
					//bandageBehaviour.WireMaterial            = Resources.Load<Material>("Materials/BandageWire");
					//bandageBehaviour.WireWidth               = 0.09f;
					//bandageBehaviour.typedJoint              = joint;

					//wJoint                              = hand.gameObject.AddComponent<WheelJoint2D>() as WheelJoint2D;
					//wJoint.connectedBody                = fJoint.connectedBody;
					//wJoint.anchor                       = fJoint.anchor;
					//wJoint.connectedAnchor              = fJoint.connectedAnchor;
					//wJoint.autoConfigureConnectedAnchor = true;
					fJoint.enabled                      = false;
	 //               wJoint.enabled                      = true;
					//wJoint.enableCollision              = false;
					//wJoint.useMotor                     = false;
					//wJoint.connectedBody.gameObject.SetLayer(3);

				} else {
					yield break;
				}

				//float duration = 2.5f;

				//float startRotation = hand.Tool.T.eulerAngles.x;
				//float endRotation = startRotation + 360.0f;
				//float t = 0.0f;
				//while ( t  < duration )
				//{
				//	t += Time.deltaTime;
				//	float xRotation = Mathf.Lerp(startRotation, endRotation, t / duration) % 360.0f;
				//	 hand.Tool.T.eulerAngles = new Vector3(xRotation, 0, 0);
				//	yield return new WaitForFixedUpdate();
				//}
			
				//yield return new WaitForFixedUpdate();

				//hand.Tool.R.AddTorque(5 * Facing);

				//yield return new WaitForSeconds(0.3f);

				//hand.Tool.R.angularVelocity *= 0.01f;

				//yield return new WaitForFixedUpdate();
				NPC.DisableFlip = true;
				hand.Tool.R.AddTorque(10f);
				yield return new WaitForSeconds(2);
				fJoint.enabled = true;
				
				if (joint) GameObject.DestroyImmediate(joint);
				yield return new WaitForFixedUpdate();
				NPC.DisableFlip = false;
				
				if (hand.Tool.props.angleHold == 0f)
				{
					hand.Tool.props.angleHold = (hand.Tool.props.holdToSide) ? 5.0f : 95.0f;
					hand.Tool.props.angleAim  = 95.0f;
				}

				float ToolRotation = hand.IsAiming ? hand.Tool.props.angleAim :hand.Tool.props.angleHold;

				ToolRotation += hand.Tool.props.angleOffset;

				Vector3 hpos = hand.Tool.HoldingPosition;

				hand.Tool.T.rotation = Quaternion.Euler(0.0f, 0.0f,hand. Tool.IsFlipped ? hand.RB.rotation + ToolRotation : hand.RB.rotation - ToolRotation);

				hand.Tool.T.position += hand.GB.transform.TransformPoint(hand.GB.GripPosition) - hand.Tool.T.TransformPoint((Vector3)hpos);



				//hand.Tool.T.rotation = Quaternion.Euler( 0.0f, 0.0f, hand.Tool.IsFlipped ? hand.RB.rotation + hand.Tool.angleHold : hand.RB.rotation - hand.Tool.angleHold );

				//hand.Tool.T.position += hand.GB.transform.TransformPoint( hand.GB.GripPosition ) - hand.Tool.T.TransformPoint( (Vector3)hpos );

				hand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

				yield return new WaitForFixedUpdate();

				hand.GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);


				//hand.StartCoroutine(hand.IResetPosition());

			}
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: IDLE WAIT   ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionWait()
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

					//if (Time.frameCount % 50 == 0 && SvCheckForThreats() || SvShoot()) {

					//	if (NpcMain.DEBUG_LOGGING) Debug.Log(NpcId + "[Wander]: CheckForThreats || SvShoot()");

					//	DecideWhatToDo(true);
					//	PBO.DesiredWalkingDirection = 0f;
					//}

				}

				if (Time.frameCount % 50 == 0 && (NPC.SvCheckForThreats() || NPC.SvShoot())) {

					if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[Wander]: CheckForThreats || SvShoot()");

					NPC.DecideWhatToDo(true);
					PBO.DesiredWalkingDirection = 0f;
				}
			}

			//if (NPC.FH.IsHolding) { 
			//	if (NPC.FH.Tool.R.position.x * -Facing < Head.position.x * -Facing) NPC.FH.StartCoroutine(NPC.FH.IResetPosition());
			//	else if ( RB["LowerArmFront"].position.x * -Facing < Head.position.x * -Facing ) 
			//	{
			//		NPC.FH.GB.DropObject();
			//		NPC.FH.StartCoroutine(NPC.FH.IResetPosition());
			//	}
			//}

			//if (NPC.BH.IsHolding) { 
			//	if (NPC.BH.Tool.R.position.x * -Facing < Head.position.x * -Facing) NPC.BH.StartCoroutine(NPC.BH.IResetPosition());
			//	else if ( RB["LowerArmFront"].position.x * -Facing < Head.position.x * -Facing ) 
			//	{
			//		NPC.BH.GB.DropObject();
			//		NPC.BH.StartCoroutine(NPC.BH.IResetPosition());
			//	}
			//}

			NPC.Mojo.Feel("Tired", -1 * Time.time - startTime);
			if (NPC.HurtLevel < 5) { 
				NPC.Mojo.Feel("Angry", -0.1f * Time.time - startTime);
				NPC.Mojo.Feel("Fear", -0.1f * Time.time - startTime);
			}
			

			CurrentAction = "Thinking";
		}

		
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: WANDER      ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionWander()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionWander()");
			yield return new WaitForFixedUpdate();

			float wanderTime = Time.time + xxx.rr(0.5f, 5f);

			int fightCount = NPC.MyFights.Count;

			while ( Time.time < wanderTime && NPC.MyFights.Count == fightCount && !NPC.CancelAction )
			{
				yield return new WaitForFixedUpdate();
				

				if (PBO.DesiredWalkingDirection < 1f) {
					PBO.DesiredWalkingDirection = 10f;
					NPC.Mojo.Feel("Tired", 1);
					NPC.Mojo.Feel("Bored", -1);
				}

				if (Time.frameCount % 50 == 0 && (NPC.SvCheckForThreats() || NPC.SvShoot())) {

					NPC.DecideWhatToDo(true);
					PBO.DesiredWalkingDirection = 0f;
				}
			}
			NPC.Mojo.Feel("Tired", wanderTime * 0.1f);
			CurrentAction = "Thinking";
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: FLEE        ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionFlee()
		{
			Flip();
			yield return new WaitForSeconds(xxx.rr(0.5f,3f));
			float wanderTime = Time.time + xxx.rr(2.5f, 5f);

			while ( Time.time < wanderTime && !NPC.CancelAction )
			{
				yield return new WaitForFixedUpdate();

				if (PBO.DesiredWalkingDirection < 1f) {
					PBO.DesiredWalkingDirection = 10f;
					NPC.Mojo.Feel("Tired", 1);
					NPC.Mojo.Feel("Bored", -1);
				}
			}
			NPC.Mojo.Feel("Tired", wanderTime * 0.1f);
			CurrentAction = "Thinking";
		}

		
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: SCAVENGE    ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionScavenge()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionScavenge()");
			if (!NPC.MyTargets.prop)
			{
				ClearAction();
				yield break;
			}

			float dist = float.MaxValue;

			xxx.ToggleCollisions(NPC.MyTargets.prop.P.transform, Head, true);

			while ( NPC.MyTargets.prop && !NPC.MyTargets.prop.P.beingHeldByGripper && !NPC.CancelAction)
			{
				Vector3 target = NPC.MyTargets.prop.transform.position;

				if (!NPC.FacingToward(target)) {

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

				//if (NPC.CheckInterval(1.5f) && NPC.CheckRandomSituations(true)) yield break;

			}

			//	Check if we got the item

			CurrentAction = "Thinking";
			//PBO.OverridePoseIndex       = (int)PoseState.Rest;
			PBO.DesiredWalkingDirection = 0;
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: SURVIVE     ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionSurvive()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionSurvive()");

			yield return new WaitForFixedUpdate();

			if ( !NPC.MyTargets.enemy )
			{
				ClearAction();
				yield break;
			}

			switch( xxx.rr(1,3))
			{
				case 1:
					NPC.CommonPoses["survive"].RunMove();
					break;

				case 2:
					if (NPC.MyTargets.item) { 
						RB["UpperBody"].AddForce((NPC.MyTargets.item.transform.position - Head.position).normalized * TotalWeight * 2f, ForceMode2D.Impulse);
						yield return new WaitForFixedUpdate();
						NPC.CommonPoses["prone"].RunMove();
					} else
					{
						PBO.OverridePoseIndex = (int)PoseState.Flat;
					}
					break;

				case 3:
					NPC.CommonPoses["takecover"].RunMove();
					break;
			}

			float MaxTimeWait = Time.time + 10f;

			bool TriggerRecover = false;

			while (NPC.MyTargets.enemy && Time.time < MaxTimeWait && !NPC.CancelAction )
			{
				if ( !NPC.MyFights.Contains( NPC.MyTargets.enemy ) && !TriggerRecover )
				{
					TriggerRecover = true;
					MaxTimeWait    = Time.time + xxx.rr(2,5);
				}

				NPC.Mojo.Feel("Chicken", Time.fixedDeltaTime * NPC.Mojo.Feelings["Fear"] );

				RB["LowerArm"].AddForce(Vector2.down * TotalWeight * Time.fixedDeltaTime * xxx.rr(100f, 300f));
				RB["LowerArmFront"].AddForce(Vector2.down * TotalWeight * Time.fixedDeltaTime * xxx.rr(100f, 300f));

				//MyNpcPose.RunMove();

				//if (NPC.CheckInterval(1.5f) && NPC.CheckRandomSituations(true)) {
				//	NpcPose.Clear(PBO);
				//	yield break;
				//}

				yield return new WaitForFixedUpdate();
			}

			NpcPose.Clear(PBO);

			CurrentAction = "Thinking";
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: FIGHT FIRE  ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionFightFire()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionFightFire()");
			CurrentAction = "FightFire";
			float dist    = 0f;
			NpcHand hand  = (NPC.FH.IsHolding && NPC.FH.Tool.props.canFightFire) ? NPC.FH : NPC.BH;
			hand.AimStyle = AimStyles.Spray;
			while ( xxx.NPCOnFire( NPC ) )
			{
				//	Self caught on fire
				NPC.ActivityMessage.Set("I'm on fire!", 5f);

				Utilities.FlipUtility.ForceFlip(hand.Tool.P);
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
				yield return new WaitForFixedUpdate();
			}

			if (!NPC.MyTargets.item || !NPC.MyTargets.item.OnFire || !Head)
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
				NPC.FireProof = true;
				while ( NPC.MyTargets.item && NPC.MyTargets.item.OnFire )
				{
					//Vector3 target = NPC.MyTargets.item.transform.position;
					//Vector3 target = NPC.MyTargets.item.transform.position;
					Vector3 target = NPC.MyTargets.item.colliders[0].ClosestPoint(Head.position);
					if (!NPC.FacingToward(target)) {

						NPC.Flip();
						yield return new WaitForFixedUpdate();
						yield return new WaitForFixedUpdate();
					}

					dist = Head.position.x - target.x;

					if (Mathf.Abs(dist) > 2.0f)
					{
						if (PBO.DesiredWalkingDirection < 1f) PBO.DesiredWalkingDirection = 5f;

						yield return new WaitForSeconds(0.1f);
					}

					hand.Target(NPC.MyTargets.item);

					yield return new WaitForFixedUpdate();

					if (Mathf.Abs(dist) < 2.0f) break;

				}

				PBO.DesiredWalkingDirection = 0;
				//PBO.OverridePoseIndex = (int)PoseState.Rest;
				if (!NPC.MyTargets.item) ClearAction();
				hand.Target(NPC.MyTargets.item);

				float walkTimer = Time.time + 3f;

				bool isOnFire = true;
				bool startCountdown = false;
				float extCountdown = 0f;
				while ( NPC.MyTargets.item && isOnFire )
				{
					if (!NPC.MyTargets.item.OnFire) { 
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
				NPC.ScanTimeExpires = 0;

				int ScanResultsCount = Physics2D.OverlapBox(NPC.ScanStart, NPC.ScanStop, 0f, NpcBehaviour.filter, NPC.ScanResults);

				dist               = float.MaxValue;
				float tmp;
				bool foundFire = false;

				for ( int i = -1; ++i < ScanResultsCount; )
				{
					if ( NPC.ScanResults[i].transform.root.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour pb ) )
					{
						if ( pb && pb.OnFire )
						{
							tmp = (pb.transform.position - Head.position).sqrMagnitude;
							if ( tmp > dist ) {
								dist = tmp;
								NPC.MyTargets.item = pb;
							}
							foundFire = true;
						}
					}
				}

				if (!foundFire) break;
			}

			NPC.FireProof                   = false;
			CurrentAction = "Thinking";
		}

		
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: WATCH EVENT ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionWatchEvent()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionWatchEvent()");
			CurrentAction = "WatchEvent";
			float dist = 0f;

			if (!NPC.MyTargets.item || !NPC.MyTargets.item.OnFire)
			{
				StartCoroutine(IActionWait());
				yield break;
			}

			while ( NPC.MyTargets.item && !NPC.CancelAction)
			{
				Vector3 target = NPC.MyTargets.item.transform.position;

				if (!NPC.FacingToward(target)) {

					NPC.Flip();
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

			CurrentAction = "Thinking";
		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: DISCO       ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionDisco()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionDisco()");

			CurrentAction = "Disco";
			NPC.LastInterval  = 0;

			float DanceTimer = Time.time + xxx.rr(5,30);

			if (! NPC.MyTargets.item.TryGetComponent<JukeboxBehaviour>( out JukeboxBehaviour juke ) )
			{
				ClearAction();
				yield break;
			}


			while ( juke && juke.audioSource.isPlaying && Time.time > DanceTimer )
			{
				float interval = xxx.rr(0.5f, 2.5f);
				while ( !NPC.CheckInterval( interval ) )
				{
					NPC.MyNpcPose = new NpcPose(NPC, "disco1", false);

					yield return new WaitForFixedUpdate();
				}

				NPC.LastInterval = 0;

				interval = xxx.rr(0.5f, 2.5f);

				while ( !NPC.CheckInterval( interval ) )
				{
					NPC.MyNpcPose = new NpcPose(NPC, "disco2", false);

					yield return new WaitForFixedUpdate();
				}
			}



			CurrentAction = "Thinking";

		}


		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: DEFEND      ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionDefend(NpcBehaviour npcEnemy = null)
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionDefend()");
			CurrentAction = "Defend";

			if (npcEnemy == null) npcEnemy = NPC.MyTargets.enemy;

			NPC.FH.FixHandsPose((int)PoseState.Walking);
			NPC.BH.FixHandsPose((int)PoseState.Walking);

			if (!npcEnemy)
			{
				StartCoroutine(IActionWait());
				yield break;
			}

			float dist;

			if (!NPC.FacingToward(npcEnemy)) {
				Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}


			NpcHand hand              = Hands.PickRandom();
			if (!hand.IsHolding) hand = hand.AltHand;


			if (hand.IsHolding)
			{ 
				//float aimChance = CheckMyShot(MyTargets.enemy.Head, hand.Tool.T);

				float hesitate = Time.time + xxx.rr(0.5f,1.1f);

				hand.Target(npcEnemy.PBO.Limbs.PickRandom().PhysicalBehaviour);

				while ( npcEnemy && xxx.ValidateEnemyTarget(npcEnemy.PBO) && npcEnemy.ThreatLevel > -5 )
				{
					hand.FireAtWill = Time.time > hesitate;

					dist = Head.position.x - NPC.MyTargets.enemy.Head.position.x;

					if (!NPC.FacingToward(NPC.MyTargets.enemy.Head.position)) {

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

			CurrentAction = "Thinking";
		}

		
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	ACTION: MEDIC       ///////////////////////////////////
		//	- - - - - - - - - - - - - - - - - - - - - - - - - - - -
		public IEnumerator IActionMedic()
		{
			if (NpcMain.DEBUG_LOGGING) Debug.Log(NPC.NpcId + "[action]: IActionMedic()");
			CurrentAction = "Medic";

			NpcBehaviour hurtNpc = NPC.CurrentEventInfo.NPCs[0];
			if (!hurtNpc || !hurtNpc.PBO) {
				CurrentAction = "Thinking";
				yield break;

			}
			yield return new WaitForFixedUpdate();

			float wanderTime = Time.time + xxx.rr(0.5f, 10f);

			float dist;

			if ( !NPC.FacingToward( hurtNpc ) )
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
				NPC.MyNpcPose = new NpcPose(NPC, "medic", false);
				NPC.MyNpcPose.Ragdoll.ShouldStandUpright       = false;
				NPC.MyNpcPose.Ragdoll.State                    = PoseState.Rest;
				NPC.MyNpcPose.Ragdoll.Rigidity                 = 2.3f;
				NPC.MyNpcPose.Ragdoll.AnimationSpeedMultiplier = 2.5f;
				NPC.MyNpcPose.Ragdoll.UprightForceMultiplier   = 2f;
				NPC.MyNpcPose.Import();

				NPC.MyNpcPose.RunMove();
				yield return new WaitForSeconds( 3f );

				if (NPC.FH.IsHolding) NPC.FH.Drop();
				if (NPC.BH.IsHolding) NPC.BH.Drop();

			}

			//LB["UpperArm"].Broken = LB["UpperArmFront"].Broken = true;
			Vector2 position = NPC.CurrentEventInfo.PBs[0].transform.position;

			if ( !NPC.FacingToward( position ) )
			{
				Flip();
				yield return new WaitForEndOfFrame();
				yield return new WaitForFixedUpdate();
			}

			while ( hurtNpc && hurtNpc.PBO && NPC.CurrentEventInfo.PBs.Count > 0 )
			{
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


						SpringJoint2D joint = NPC.CurrentEventInfo.PBs[0].gameObject.AddComponent<SpringJoint2D>();

						joint.autoConfigureConnectedAnchor = false;

						joint.anchor = startPoint + (direction * -1);

						joint.connectedBody   = NPC.CurrentEventInfo.PBs[0].rigidbody;
						joint.connectedAnchor = endPoint + direction;

						//springJoint2D.anchor = ActiveSingleSelected.transform.InverseTransformPoint( startPos );

						AppliedBandageBehaviour bandageBehaviour = joint.gameObject.AddComponent<AppliedBandageBehaviour>();
						bandageBehaviour.WireColor               = Color.white;
						bandageBehaviour.WireMaterial            = Resources.Load<Material>("Materials/BandageWire");
						bandageBehaviour.WireWidth               = 0.09f;
						bandageBehaviour.typedJoint              = joint;
					}



					NPC.CurrentEventInfo.PBs.RemoveAt(0);

					yield return new WaitForFixedUpdate();
					yield return new WaitForEndOfFrame();
				}
			}


			CurrentAction = "Thinking";
		}
		
		
		public void KickRecover()
		{
			string[] limbs = {"Foot", "FootFront", "UpperLeg", "UpperLegFront", "LowerLeg", "LowerLegFront"};
			
			NPC.Mojo.Feel("Fear",-5f);
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
	}

}