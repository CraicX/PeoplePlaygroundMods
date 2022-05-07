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
using UnityEngine;


namespace PPnpc
{
   public class NpcLimb : MonoBehaviour
	{
		public string LimbName;
		public LimbBehaviour LB;
		public NpcBehaviour NPC;
		public float timeOut = 0f;

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

		private void OnDamage()
		{
			if (!LB.Person.IsAlive()) NPC.Death();

			
		}

		private void OnWaterImpact()
		{
			if (xxx.rr(1,5) == 3) StartCoroutine("IShortCircuit");
		}

		private void OnEMPHit()
		{
			//	If NPC was given the AIChip and is hit with EMP, it should malfunc
			if ( LB.RoughClassification == LimbBehaviour.BodyPart.Head && NPC.AIMethod == AIMethods.AIChip )
			{
				LB.Wince(1000);
				StartCoroutine("IShortCircuit");
			}
		}

		private void OnCollisionEnter2D(Collision2D coll=null)
		{
			if (!NPC.PBO.IsAlive()) return;
			if (coll.gameObject.layer != 9) {
				if ( coll.gameObject.name.ToLower().Contains( "wall" ) && Time.time > timeOut )
				{
					timeOut = Time.time + 1;
					NPC.PBO.DesiredWalkingDirection = 0;
					return;
				}
			}

			if (coll == null) return;

			NpcBehaviour otherNpc;
			otherNpc = coll.gameObject.GetComponentInParent<NpcBehaviour>();

			if ( otherNpc && otherNpc != NPC )
			{
				NPC.CollideNPC(otherNpc);

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
				if (NPC.PrimaryAction == NpcPrimaryActions.Wander) points *= 0.9f;

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
					if (coll.contacts[0].relativeVelocity.magnitude > 2f )
					{
						xxx.ToggleCollisions(transform, person.transform,false, true);
						return;
					}
				}
			}
			else
			{
				if ( coll.gameObject.TryGetComponent<Props>( out Props prop ) )
				{
					NPC.OpenHand.Hold( prop );
					xxx.ToggleCollisions(transform, prop.P.transform,false, true);
				}

				if ( LB.RoughClassification == LimbBehaviour.BodyPart.Head && NPC.AIMethod == AIMethods.AIChip )
				{
					//	got clocked in the head and has a chip
					if ( coll.contacts[0].relativeVelocity.magnitude > 6f ) StartCoroutine( "IShortCircuit" );
				}
			}
		}
	}
}
