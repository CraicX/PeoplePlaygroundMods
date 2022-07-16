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
using UnityEngine.Events;


namespace PPnpc
{
	[SkipSerialisation]
	public class NpcLimb : MonoBehaviour, Messages.IStabbed
	{
		public string LimbName;
		public LimbBehaviour LB;
		public NpcBehaviour NPC;
		public float timeOut = 0f;
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

		public void Stabbed(Stabbing stab)
		{
			if (!NPC.PBO || !NPC.PBO.IsAlive()) return;

			NPC.Mojo.Feel("Fear", 2f);
			NPC.Mojo.Feel("Angry", 1f);
			NPC.Mojo.Feel("Bored", -20f);

			if (NPC.HasGun)
			{
				if (NPC.FH.IsHolding) NPC.FH.FireAtWill = true;
				if (NPC.BH.IsHolding) NPC.BH.FireAtWill = true;
			}

			if ( stab.stabber.transform.root.gameObject.TryGetComponent<NpcBehaviour>( out NpcBehaviour stabber ) )
			{
				if (NPC.EnhancementMemory) NPC.Memory.AddNpcStat(stabber.NpcId, "StabMe");
				if (stabber.EnhancementMemory) stabber.Memory.AddNpcStat(NPC.NpcId, "StabThem");
				if ( NPC.MyTargets.enemy != stabber )
				{
					NPC.MyTargets.enemy    = stabber;
					NPC.Memory.LastContact = stabber;
				}

			}

		} 
		

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


		private void OnCollisionEnter2D( Collision2D coll = null )
		{
			if (!NPC.PBO.IsAlive()) return;

			if (coll.gameObject.layer != 9) {
				if ( coll.gameObject.name.Contains( "wall" ) && Time.time > timeOut )
				{
					NPC.CancelAction = true;
					NPC.PBO.DesiredWalkingDirection = 0;
					return;
				}
			}

			Transform[] Ts;

			if ( coll.transform == coll.transform.root )
				Ts = new Transform[] { coll.transform };
			else
				Ts = new Transform[] { coll.transform, coll.transform.root };


			lCase = coll.gameObject.name.ToLower();

			if (NpcGlobal.NoClip.Contains(coll.gameObject.name) || 
				NpcGlobal.NoClipPartial.Any(lCase.Contains)) { 
				
				if (coll.gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer srx)) { 
					srx.sortingLayerName = "Background";
					srx.sortingOrder     = -10;
				}
				
				xxx.ToggleCollisions(transform, coll.transform,false, true);
			} 

			if ( coll.transform.root.gameObject.TryGetComponent<NpcBehaviour>( out NpcBehaviour enemy ) )
			{
				float mag = coll.contacts[0].relativeVelocity.sqrMagnitude;

				if (mag > 15 && enemy.DoingStrike) {
					NPC.Memory.LastContact = enemy;
					NPC.Memory.AddNpcStat(enemy.NpcId, "HitMe");

					if ( enemy.Actions.CurrentAction == "FrontKick" && coll.gameObject.name == "FootFront" ) { 
						if (!NPC.RunningSafeCollisions) NPC.StartCoroutine(NPC.ISafeCollisions());
					}
					
					if (LB.RoughClassification == LimbBehaviour.BodyPart.Head && enemy.Facing != NPC.Facing)
					{
						NPC.Mojo.Feel("Angry", mag * 1.4f);
						if ( xxx.rr( 1, 500 ) < mag )
						{
							//LB.CirculationBehaviour.Cut((Vector2) LB.transform.position, UnityEngine.Random.insideUnitCircle);

							if ( LB.CirculationBehaviour.BleedingPointCount == 0 )
							{
								LB.CirculationBehaviour.Cut( (Vector2)coll.contacts[0].point, Vector2.down);
								LB.CirculationBehaviour.BleedingRate = Mathf.Clamp( mag * 0.1f, 0.1f, 5f );
								LB.CirculationBehaviour.BloodLossRateMultiplier *= 0.1f;
								LB.CirculationBehaviour.CreateBleedingParticle((Vector2)coll.contacts[0].point, UnityEngine.Random.insideUnitCircle);
								NPC.BloodyNose += 2;
							} else
							{
								LB.CirculationBehaviour.CreateBleedingParticle((Vector2)coll.contacts[0].point, UnityEngine.Random.insideUnitCircle);
							}
						}
					}

					if (LB.RoughClassification == LimbBehaviour.BodyPart.Torso) { 

						NPC.Mojo.Feel("Angry", mag * 0.4f);

						foreach(NpcHand hand in NPC.Hands) { 
							if (mag > 1000 && xxx.rr(1,3) == 2 ) 
							{
								//ModAPI.Notify("Hit Strength: " + mag);
								Vector2 dropForce = UnityEngine.Random.insideUnitCircle;
								dropForce.x = Mathf.Abs(dropForce.x) * -NPC.Facing;
								if (hand.IsHolding && hand.Tool && hand.Tool.P) hand.Tool.P.MakeWeightful();
								hand.Drop();

								NPC.TimerIgnorePickup = Time.time + 1f;
								hand.PB.rigidbody.AddForce(dropForce * xxx.rr(5.0f,10.0f));
							}
						}
					}
				}

				if (!NPC.BGhost.Keys.Intersect(Ts).Any()) xxx.ToggleCollisions(transform, enemy.transform,false, true);
					
				return;
			}

			if ( coll.transform.root.gameObject.TryGetComponent<PersonBehaviour>( out PersonBehaviour person ) )
			{
				xxx.ToggleCollisions(transform, coll.transform,false, true);
				return;
			}

			//if ( coll.gameObject.TryGetComponent<NpcTool>( out NpcTool tool ) )
			//{
			//	if (tool.BGhost.Keys.Intersect(Ts).Any()) return;
			//}
			if ( coll.gameObject.TryGetComponent<Props>( out Props prop ) )
			{
				if (NPC.BGhost.Keys.Intersect(Ts).Any())  return; 

				if ( prop.NoGhost ) return;

				if ((NPC.Action.CurrentAction == "Scavenge" || NPC.Action.CurrentAction == "GearUp" || NPC.Action.CurrentAction == "Wander" || NPC.ArsenalTimer > Time.time) )
				{
					if (prop.canUpgrade) {
						if (LB.RoughClassification == LimbBehaviour.BodyPart.Head) return;
						xxx.ToggleCollisions(transform, prop.P.transform,false, false);
					}

					if (Time.time > NPC.TimerIgnorePickup) NPC.OpenHand.Hold( prop );
				}
				xxx.ToggleCollisions(transform, prop.P.transform,false, true);

				if ( LB.RoughClassification == LimbBehaviour.BodyPart.Head  )
				{

					//	got clocked in the head and has a chip
					if ( NPC.AIMethod == AIMethods.AIChip && coll.contacts[0].relativeVelocity.magnitude > 20f ) StartCoroutine( "IShortCircuit" );
				}
			}
		}


		
		
	}
}
