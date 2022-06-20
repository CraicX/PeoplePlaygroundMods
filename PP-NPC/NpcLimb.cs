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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace PPnpc
{
	[SkipSerialisation]
	public class NpcLimb : MonoBehaviour
	{
		public string LimbName;
		public LimbBehaviour LB;
		public NpcBehaviour NPC;
		public float timeOut  = 0f;
		private string lCase = "";
		Dictionary<Collision2D, float> LodgedLimbs = new Dictionary<Collision2D, float>();
		
		public LimbHit Hit = new LimbHit()
		{
			Force = 0,
			HealthFinish = 0,
			HealthStart = 0,
			TimeImpact = 0,
			LimbName = "",
		};


		public void MalfuncExplode() => LB.Crush();
		public void MalfuncIgnite()  => LB.PhysicalBehaviour.Ignite();
		public void MalfuncSlice()   => LB.Slice();
		public void MalfuncSizzle()  => LB.PhysicalBehaviour.Sizzle();

		IEnumerator IShortCircuit()
		{
			string[] EffectList = {"Spark", "FuseBlown", "BigZap", "BrokenElectronicsSpark"};

			float effectDuration = Time.time + xxx.rr(1f,10f);

			while ( Time.time < effectDuration )
			{
				yield return new WaitForSeconds(xxx.rr(0.1f, 3.1f));

				ModAPI.CreateParticleEffect(EffectList.PickRandom(), LB.transform.position);

				switch(xxx.rr(1,50))
				{
					case 2:
						NPC.HeadL.Crush();
						break;

					case 3: 
						NPC.HeadL.Slice();
						break;

					default:
						NPC.HeadL.PhysicalBehaviour.Sizzle();
						break;


				}


			}


			yield break;
		}

		List<Collision2D> CollClear = new List<Collision2D>();
		public IEnumerator ICheckLodged()
		{
			float timer = Time.time - 1f;
			CollClear.Clear();

			foreach ( KeyValuePair<Collision2D, float> pair in LodgedLimbs )
			{
				yield return new WaitForFixedUpdate();

				if ( pair.Value < timer )
				{
					xxx.ToggleCollisions(pair.Key.transform, LB.transform, false, false);
					CollClear.Add(pair.Key);
				}
			}

			for ( int i = CollClear.Count; --i >= 0; )
			{
				LodgedLimbs.Remove(CollClear[i]);
			}

			yield return new WaitForSeconds(5);

		}
		

		

		public void OnEMPHit()
		{
			//	If NPC was given the AIChip and is hit with EMP, it should malfunc
			if ( LB.RoughClassification == LimbBehaviour.BodyPart.Head && NPC.AIMethod == AIMethods.AIChip )
			{
				LB.Wince(1000);
				StartCoroutine("IShortCircuit");
			}
		}

		private void OnCollisionExit2D( Collision2D coll = null )
		{
			if (!coll.collider || !coll.gameObject) return;
			if (!NPC.PBO.IsAlive()) return;

			if (LodgedLimbs.ContainsKey(coll)) LodgedLimbs.Remove(coll);

			if (coll.gameObject.layer != 9) return;

			if ( Time.time - Hit.TimeImpact < 2f )
			{
				Hit.HealthFinish = LB.Health;

				float dmg = Hit.HealthStart - Hit.HealthFinish;
				if ( dmg > 5 && Hit.Force == 0 )
				{
					Hit.Force = dmg;
					if ( Hit.Attacker.EnhancementMemory ) { 
						Hit.Attacker.Memory.AddNpcStat(NPC.NpcId, "HitThem");
						Hit.Attacker.Memory.LastContact = NPC;
					}
					
					if (NPC.EnhancementMemory ) { 
						NPC.Memory.AddNpcStat(Hit.Attacker.NpcId, "HitMe");
						NPC.Memory.LastContact = Hit.Attacker;
					}
				}
			}

		}

		void Start()
		{
			Hit.HealthStart  = LB.InitialHealth;
			Hit.HealthFinish = LB.Health;
		}

		private void OnCollisionEnter2D(Collision2D coll=null)
		{
		
			if (!NPC.PBO.IsAlive()) return;
			if (coll.gameObject.layer != 9) {
				if ( coll.gameObject.name.Contains( "wall" ) && Time.time > timeOut )
				{
					NPC.CancelAction = true;
					timeOut = Time.time + 1;
					NPC.PBO.DesiredWalkingDirection = 0;
					return;
				}
			}
			
			lCase = coll.gameObject.name.ToLower();

			if(lCase == "root") return;
			
			if (LB.InternalTemperature > 1000) { NPC.RunLimbs(NPC.LimbIchi); }

			if (NpcGlobal.NoClip.Contains(coll.gameObject.name) || NpcGlobal.NoClipPartial.Any(lCase.Contains)) { 
				if (coll.gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer srx)) { 
					srx.sortingLayerName = "Background";
					srx.sortingOrder     = -10;
				}
				if (!NPC.NoGhost.Contains(coll.transform.root))
					xxx.ToggleCollisions(transform, coll.transform,false, true);
			} 

			

			if (lCase.Contains("debris") ) xxx.ToggleCollisions(transform, coll.gameObject.transform,false, true);

			if (NPC.Action.CurrentAction == "Scavenge" && NPC.MyTargets.prop) 
			{
				if (++NPC.CollisionCounter > 10) {
					NPC.ScannedPropsIgnored.Add(NPC.MyTargets.prop);
					NPC.Action.ClearAction();
					return;
				}
			}

			if (coll == null) return;

			NpcBehaviour otherNpc;
			otherNpc = coll.gameObject.GetComponentInParent<NpcBehaviour>();

			if ( otherNpc && otherNpc != NPC )
			{
				
				if (Time.time - Hit.TimeImpact > 1f)
				{
					Hit.TimeImpact  = Time.time;
					Hit.Attacker    = otherNpc;
					Hit.LimbName    = coll.gameObject.name;
					Hit.HealthStart = LB.Health;
					Hit.Force       = 0;
				}

				

				if( !NPC.CheckedJoints ) StartCoroutine(NPC.ICheckJoints());

				LodgedLimbs[coll] = Time.time;

				if ( NPC.CollideNPC( otherNpc ) )
				{
					float mag = coll.contacts[0].relativeVelocity.sqrMagnitude;
				
					if (Hit.HealthFinish != LB.Health)
					{
						if (NPC.EnhancementMemory) NPC.Memory.AddNpcStat(otherNpc.NpcId, "HitMe");
						if (otherNpc.EnhancementMemory) otherNpc.Memory.AddNpcStat(NPC.NpcId, "HitThem");
						NPC.Memory.LastContact = Hit.Attacker;
					}

					
					if ( otherNpc.Action.CurrentAction == "FrontKick" && coll.gameObject.name == "FootFront" )
					{
						if (!NPC.RunningSafeCollisions) NPC.StartCoroutine(NPC.ISafeCollisions());
						NPC.LastHit = otherNpc.NpcId;

						
					}

					//if (mag > 20 && (NPC.PrimaryAction == NpcPrimaryActions.FrontKick)) {
					//	NPC.KickLanded = Time.time; 
					//	//ModAPI.Notify("Good Kick: " + mag);
					//}

					if (LB.RoughClassification == LimbBehaviour.BodyPart.Torso) { 

						if (mag > 20) NPC.MyTargets.enemy = otherNpc;
					
						NPC.Mojo.Feel("Annoyed", mag * 0.3f);
						NPC.Mojo.Feel("Angry", mag * 0.4f);

						foreach(NpcHand hand in NPC.Hands) { 
							if (hand.IsHolding && xxx.rr( 1, 100 ) < mag)
							{

								Vector2 dropForce = UnityEngine.Random.insideUnitCircle;
								dropForce.x = Mathf.Abs(dropForce.x) * -NPC.Facing;
								if ( hand.AltHand.GB.isHolding && hand.AltHand.GB.CurrentlyHolding == hand.GB.CurrentlyHolding )
								{
									hand.AltHand.Drop();
								}

								hand.Drop();
								NPC.TimerIgnorePickup = Time.time + 1f;
								hand.PB.rigidbody.AddForce(dropForce * xxx.rr(5.0f,10.0f));
							}
						}
					}
				}

				//xxx.ToggleCollisions(transform,coll.transform,false,false);

				//if (NPC.PeepCollisions.ContainsKey(otherNpc.NpcId) && NPC.PeepCollisions[otherNpc.NpcId] > Time.time) return;

				//NPC.PeepCollisions[otherNpc.NpcId] = Time.time + 3f;

				//Opinion opinion = NPC.CheckInteractions(otherNpc);

				float points = 5;

				if (NPC.Config.NpcType != otherNpc.Config.NpcType) {
					points *= 1.5f;
					NPC.Mojo.Feel("Fear", points * 0.3f);
				}

				//if (otherNpc.CurrentAct == NpcActions.Walking) points *= 0.7f;
				if (NPC.Action.CurrentAction == "Wander") points *= 0.9f;

				NPC.Mojo.Feel("Annoyed", points * 0.3f);
				NPC.Mojo.Feel("Angry", points * 0.3f);

				//NPC.ResetCollisions(otherNpc);
			}
			else if ( coll.transform.root.TryGetComponent<PersonBehaviour>(out PersonBehaviour person) )
			{
				if (!person.IsAlive())
				{
					xxx.ToggleCollisions(transform, person.transform,false, true);
					return;
				} else
				{
					if (!NPC.NoGhost.Contains(coll.transform.root) && coll.contacts[0].relativeVelocity.magnitude > 2f )
					{
						xxx.ToggleCollisions(transform, person.transform,false, true);
						return;
					}
				}
			}
			else
			{
				if ((NPC.Action.CurrentAction == "Scavenge" || NPC.Action.CurrentAction == "GearUp" || NPC.Action.CurrentAction == "Wander") && coll.gameObject.TryGetComponent<Props>( out Props prop ) )
				{
					if (Time.time > NPC.TimerIgnorePickup) NPC.OpenHand.Hold( prop );
					xxx.ToggleCollisions(transform, prop.P.transform,false, true);
				}

				if ( LB.RoughClassification == LimbBehaviour.BodyPart.Head && NPC.AIMethod == AIMethods.AIChip )
				{
					//	got clocked in the head and has a chip
					if ( coll.contacts[0].relativeVelocity.magnitude > 10f ) StartCoroutine( "IShortCircuit" );
				}
			}
		}

		
	}
}
