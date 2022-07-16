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
								  GroupUp, Karate, Knife, LastAttack, LastShoot, Medic, Recruit, Retreat, Scavenge, Shoot, Shove, SoccerKick, Survive,	
								  Tackle, TakeCover, Troll, Wander, WatchEvent, LastContact, Upgrade, Warn, WatchFight, Witness";
		
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
					PCR = StartCoroutine( NPC.Actions.IWait() );
					break;

				case "Flee":
					PCR = StartCoroutine( NPC.Actions.IWander() );
					break;

				case "Troll":
					PCR = StartCoroutine( IActionTroll() );
					break;

				case "Witness":
					PCR = StartCoroutine( NPC.Actions.IWitness() );
					break;

				case "WatchFight":
					PCR = StartCoroutine( NPC.Actions.IWatchFight() );
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
				case "Shove":
				case "FrontKick":
				case "Club":
				case "Knife":
				case "Attack":
				case "LastAttack":
				case "Karate":
				case "SoccerKick":
					PCR = StartCoroutine( NPC.Actions.IAttack() );
					break;

				case "Warn":
					PCR = StartCoroutine( NPC.Actions.IWarn() );
					break;


				case "Fight":
				case "Shoot":
				case "Defend":
				case "LastShoot":
					PCR = StartCoroutine( NPC.Actions.IShoot() );
					break;

				case "Survive":
					PCR = StartCoroutine( IActionSurvive() );
					break;

				case "FightFire":
					PCR = StartCoroutine( IActionFightFire() );
					break;

				case "Wander":
					PCR = StartCoroutine( NPC.Actions.IWander() );
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
			}
		}


		public void ClearAction()
		{
			NPC.CanGhost = true;
			if (PCR != null) StopCoroutine(PCR);
			CurrentAction = "Thinking";
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

			NpcTool sword         = hand.Tool;
			SwingStyle swingStyle = SwingStyle.over;

			if (Mathf.Abs(trans.position.x - Head.position.x) < 1f) swingStyle = SwingStyle.jab;
			
			if (isAlt) swingStyle = SwingStyle.jab;
			NpcPose swingPose     = new NpcPose(NPC, "club_" + swingStyle.ToString() + "_" + hand.HandShortId, false);
			swingPose.Import();

			float timer = Time.time + xxx.rr(0.2f,0.4f);

			while ( Time.time < timer )
			{
				yield return new WaitForFixedUpdate();
				swingPose.CombineMove();
			}

			hand.swingState = SwingState.cocked;
			sword.props.P.MakeWeightful();

			yield return new WaitForFixedUpdate();
			
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
					yield return new WaitForFixedUpdate();
				}
				
				yield return new WaitForFixedUpdate();

				Vector3 dir = (trans.position - Head.position).normalized;
				xxx.ToggleCollisions(trans.root, hand.Tool.T, true, false);
				sword.props.P.rigidbody.AddForce(((Vector3.right * -Facing) + Vector3.down + dir).normalized * TotalWeight * 3f * swingStrength, ForceMode2D.Impulse);
				RB["UpperBody"].mass     *= 2;
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
			sword.R.mass    = sword.P.InitialMass;
			sword.props.P.MakeWeightless();

			hand.LB.Broken = hand.uArmL.Broken = false;
			yield return new WaitForSeconds(xxx.rr(0.3f, 1.5f));
			hand.swingState = SwingState.idle;
			RB["UpperBody"].mass = LB["UpperBody"].PhysicalBehaviour.InitialMass;
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

			if (new string[]{"Jukebox","Television","Radio"}.Contains( NPC.MyTargets.item.name ) )
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
									juke.PlayIndex( xxx.rr( 0, NpcMain.SoundBank["jukebox"].Length ));
									//juke.NextSong();

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
									
									NPC.SayRandom( "fidget" );

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
				Sayings.AddRange(NpcChat.SmackText[cat] );
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

			PBO.DesiredWalkingDirection = 0;
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

			foreach ( SignPost signPost in NpcGlobal.SignPosts )
			{
				if ( signPost.SignType == Gadgets.NoEntrySign && NPC.FacingToward( signPost.Sign.position ) && 
					Mathf.Abs(signPost.Sign.position.x - Head.position.x) < 5 )
				{
					if (signPost.pointingLeft && signPost.Sign.position.x > Head.position.x ||
						!signPost.pointingLeft && signPost.Sign.position.x < Head.position.x)
					{
						NPC.Flip();
						yield return new WaitForFixedUpdate();
						yield return new WaitForFixedUpdate();
					}
				}
			}

			while ( Time.time < wanderTime && NPC.MyFights.Count == fightCount && !NPC.CancelAction )
			{
				yield return new WaitForFixedUpdate();
				

				if (PBO.DesiredWalkingDirection < 1f) {
					PBO.DesiredWalkingDirection = 10f;
					NPC.Mojo.Feel("Tired", 1);
					NPC.Mojo.Feel("Bored", -1);
				}

				if (Time.frameCount % 50 == 0) { 
					NPC.SvCheckForThreats();
					NPC.SvShoot();

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

			}

			//	Check if we got the item

			CurrentAction               = "Thinking";
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

				hand.IsAiming   = false;
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