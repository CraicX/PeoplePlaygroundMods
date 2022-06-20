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
using System.Collections;
using System.Linq;

namespace PPnpc
{
	public class NpcArsenal : MonoBehaviour
	{
		public static List<NpcArsenal> Arsenals = new List<NpcArsenal>();

		public Rigidbody2D R;
		public PhysicalBehaviour P;

		public static bool isPaused = false;

		private bool _paused = false;

		public NpcGadget Expansion = null;

		public GameObject TeamSelect;
		public GameObject OnOffLight;

		public SpriteRenderer[] SRS;

		public List<Props> PropsList	  = new List<Props>();
		public List<Props> BorrowedProps  = new List<Props>();

		public EdgeCollider2D edge;

		public Collider2D[] ECols = new Collider2D[24];

		public bool HasChips     = false;
		public bool HasGuns      = false;
		public bool HasMelee     = false;
		public bool HasMedic     = false;
		public bool HasChuckable = false;

		public List<NpcBehaviour> Customers = new List<NpcBehaviour> ();
		public int CustomerCount = 0;

		public ContactFilter2D contactFilter;
		public ContactFilter2D contactFilterX;

		public float LastParticleEffect = 0;

		public Dictionary<Props, ArmoryItemPosition> ArmoryPositions = new Dictionary<Props, ArmoryItemPosition>();
		public Dictionary<Props, float> IdleDroppedList              = new Dictionary<Props, float>();

		public void AddToArsenal( object sender,  UserSpawnEventArgs args )
		{
			if ( args.Instance.TryGetComponent( out PhysicalBehaviour pb ) ) {

				if ( xxx.IsColliding( pb.transform, transform ) && xxx.CanHold( pb ) )
				{
					StartCoroutine( IAddToArsenal( pb ) );
				}
			}
		}

		private bool _teamSwitch  = false;
		private bool _powerSwitch = false;

		

		void Start()
		{
			R = GetComponent<Rigidbody2D>();
			P = GetComponent<PhysicalBehaviour>();

			R.bodyType = RigidbodyType2D.Static;
			StartCoroutine(IAnimatePegBoard());

			ModAPI.OnItemSpawned += AddToArsenal; 
			
			
			gameObject.SetLayer(_paused ? 9 : 2);

			

			//	Setup edge colliders
			Renderer r = GetComponent<Renderer>();
			if (r != null) { 
			
				Bounds bounds = r.bounds;

				edge = gameObject.AddComponent<EdgeCollider2D>();

				edge.points = new Vector2[]
				{
					 transform.InverseTransformPoint(new Vector2(bounds.min.x, bounds.min.y)),
					 transform.InverseTransformPoint(new Vector2(bounds.max.x, bounds.min.y)),
					 transform.InverseTransformPoint(new Vector2(bounds.max.x, bounds.max.y)),
					 transform.InverseTransformPoint(new Vector2(bounds.min.x, bounds.max.y)),
					 transform.InverseTransformPoint(new Vector2(bounds.min.x, bounds.min.y)),
				};
				
				edge.isTrigger  = false;
				edge.enabled    = true;
			}

			ContactFilter2D contactFilterX = new ContactFilter2D()
			{
				layerMask    = LayerMask.GetMask("Objects"),
				useLayerMask = false,
			};
			contactFilterX.NoFilter();

			ContactFilter2D contactFilter = new ContactFilter2D()
			{
				layerMask    = LayerMask.GetMask("Objects"),
				useLayerMask = true,
			};
			contactFilter.NoFilter();

			if (!NpcArsenal.Arsenals.Contains(this)) NpcArsenal.Arsenals.Add(this);

			StartCoroutine(ICheckMaintenance());
		}



		void Update()
		{
			if ( _paused != Global.main.Paused )
			{
				_paused = Global.main.Paused;

				if (Expansion.ChildChip)
				{
					if (_paused) Expansion.ChildChip.transform.SetParent(Expansion.transform);
					else Expansion.ChildChip.transform.parent = null;
				}

				gameObject.SetLayer(_paused ? 9 : 2);

				for ( int i = PropsList.Count; --i >= 0; )
				{
					if ( !PropsList[i] || PropsList[i] == null ) PropsList.RemoveAt(i);
				}

				foreach ( Props prop in PropsList )
				{
					if (!prop) continue;

					prop.gameObject.SetLayer(_paused ? 9 : 2);
				}

				ValidatePegBoard();

				//StartCoroutine(IValidatePegBoard());


			}
		}

		public void DropItem( NpcHand[] hands )
		{
			Props prop;
			int handsLeft = 2;

			foreach( NpcHand hand in hands )
			{
				if (hand.IsHolding) handsLeft--;
			}

			for ( int i = PropsList.Count; --i >= 0; )
			{
				prop = PropsList.PickRandom();
				if (prop.needsTwoHands) {
					if (handsLeft >= 2) {
						ReleaseProp(prop);
						handsLeft -= 2;
						StartCoroutine(IBringItemToHand(prop, hands));
						break;
					}
				} else
				{
					if (handsLeft >= 1)
					{
						ReleaseProp(prop);
						handsLeft -= 1;
						StartCoroutine(IBringItemToHand(prop, hands));
						break;
					}
				}
			}
		}

		public IEnumerator IBringItemToHand( Props prop, NpcHand[] hands )
		{
			float dist = float.MaxValue;

			Vector3 position = Vector3.zero;

			foreach ( NpcHand hand in hands )
			{
				if (!hand || hand.IsHolding) continue;

				float tdist = (hand.LB.transform.position - prop.P.transform.position).sqrMagnitude;

				if (tdist < dist) {
					dist     = tdist;
					position = hand.LB.transform.position * hand.NPC.Facing;
				}

			}

			if ( position != Vector3.zero )
			{
				float timer = Time.time + 0.5f;
				while ( prop.P && !prop.P.beingHeldByGripper )
				{
					prop.P.rigidbody.AddForce( Vector2.down * Time.fixedDeltaTime * 1 );
					yield return new WaitForFixedUpdate();
					if (Time.time > timer) yield break;
				}
			}


		}

		public void ReleaseProp( Props prop )
		{
			if (!prop || !prop.P) return;

			prop.P.gameObject.SetLayer(9);
			prop.P.rigidbody.bodyType = RigidbodyType2D.Dynamic;
			prop.P.transform.parent   = null;
			while(prop.P.TryGetComponent<FixedJoint2D>(out FixedJoint2D joint)) GameObject.DestroyImmediate(joint);
			//prop.P.rigidbody.AddForce(UnityEngine.Random.insideUnitCircle * 1f, ForceMode2D.Impulse);
			prop.P.MakeWeightful();
			PropsList.Remove(prop);
			if (!BorrowedProps.Contains(prop)) BorrowedProps.Add(prop);
			if (IdleDroppedList.ContainsKey(prop)) IdleDroppedList.Remove(prop);
		}


		public bool CanNPCVisit( NpcBehaviour npc )
		{
			if ( CanSupplyNpc(npc) && CustomerCount <= 2)
			{
				if (Expansion && Expansion.ChildChip && npc.TeamId == Expansion.ChildChip.TeamId) 
				{ 
					Customers.Add( npc );
					CustomerCount = Customers.Count;
				}

				return true;
			}

			return false;
		}


		public bool InPegBoard( Props prop )
		{
			Collider2D[] colSet1 = gameObject.GetComponents<Collider2D>();
			Collider2D[] colSet2 = prop.P.transform.root.GetComponents<Collider2D>();

			ContactFilter2D filter = new ContactFilter2D();
			filter.NoFilter();

			List<Collider2D> colResults = new List<Collider2D>();

			foreach (Collider2D col1 in colSet1)
			{
				col1.OverlapCollider(filter, colResults);
				if (colResults.Intersect(colSet2).Any()) return true;
			}
			return false;
		}


		public bool CanSupplyNpc( NpcBehaviour NPC )
		{
			bool canSupply =  ( (NPC.EnhancementFirearms && HasGuns)  ||
				 (NPC.EnhancementMelee    && HasMelee) || 
				 (NPC.EnhancementThrow    && HasChuckable)
				) ;

			return canSupply;
				 
		}
		
		public void ValidatePegBoard()
		{
			
			for ( int i = PropsList.Count; --i >= 0; )
			{
				if ( !PropsList[i] || PropsList[i] == null ) {
					PropsList.RemoveAt(i);
				}

				if (  !xxx.IsColliding( PropsList[i].transform, transform, false ) )
				{
					PropsList[i].P.rigidbody.bodyType = RigidbodyType2D.Dynamic;
					PropsList[i].P.transform.parent   = null;
					PropsList[i].P.gameObject.SetLayer(9);
							
					if (PropsList[i].P.TryGetComponent<FixedJoint2D>(out FixedJoint2D joint)) GameObject.Destroy(joint);
				}

			}
			

			int bufferCount = (edge.OverlapCollider(contactFilter, ECols));

			if (bufferCount > 0)
			{
				for ( int j = bufferCount; --j >= 0; )
				{
					if ( ECols[j] && ECols[j].gameObject.TryGetComponent<Props>( out Props prop ) )
					{
						if (PropsList.Count == 0) continue;

						if (PropsList.Contains(prop))
						{
							PropsList.Remove(prop);

							prop.P.rigidbody.bodyType = RigidbodyType2D.Dynamic;
							prop.P.transform.parent   = null;

							prop.P.gameObject.SetLayer(9);
							
							if (prop.P.TryGetComponent<FixedJoint2D>(out FixedJoint2D joint)) GameObject.Destroy(joint);
						}
					}
				}
			}

			bufferCount = ( gameObject.GetComponents<Rigidbody2D>()[0].OverlapCollider(contactFilter, ECols));

			if (bufferCount > 0)
			{
				for ( int j = bufferCount; --j >= 0; )
				{
					if ( ECols[j] && ECols[j].gameObject.TryGetComponent<Props>( out Props prop ) )
					{
						if (PropsList.Count == 0) continue;

						if (!PropsList.Contains(prop))
						{
							AddToArsenalX( prop.P );
						}
					}
				}
			}




			foreach ( PhysicalBehaviour pb in Global.main.PhysicalObjectsInWorld )
			{
				if ( xxx.IsColliding( pb.transform, transform ) && xxx.CanHold( pb ) )
				{
					//StartCoroutine( IAddToArsenal( pb ) );
					AddToArsenalX( pb );
				}
				
			}


			HasChips = HasChuckable = HasGuns = HasMelee = HasMedic = false;

			for ( int i = PropsList.Count; --i >= 0; )
			{
				if ( PropsList[i].canThrow  ) HasChuckable = true;
				if ( PropsList[i].canShoot  ) HasGuns      = true;
				if ( PropsList[i].canStrike ) HasMelee     = true;
			}


		}



		IEnumerator IValidatePegBoard()
		{
			
			for ( int i = PropsList.Count; --i >= 0; )
			{
				if ( !PropsList[i] || PropsList[i] == null ) {
					PropsList.RemoveAt(i);
				}

				if (  !xxx.IsColliding( PropsList[i].transform, transform, false ) )
				{
					PropsList[i].P.rigidbody.bodyType = RigidbodyType2D.Dynamic;
					PropsList[i].P.transform.parent   = null;
					PropsList[i].P.gameObject.SetLayer(9);
							
					if (PropsList[i].P.TryGetComponent<FixedJoint2D>(out FixedJoint2D joint)) GameObject.Destroy(joint);
				}

			}
			

			int bufferCount = (edge.OverlapCollider(contactFilter, ECols));

			if (bufferCount > 0)
			{
				for ( int j = bufferCount; --j >= 0; )
				{
					if ( ECols[j] && ECols[j].gameObject.TryGetComponent<Props>( out Props prop ) )
					{
						if (PropsList.Count == 0) continue;

						if (PropsList.Contains(prop))
						{
							PropsList.Remove(prop);

							prop.P.rigidbody.bodyType = RigidbodyType2D.Dynamic;
							prop.P.transform.parent   = null;

							prop.P.gameObject.SetLayer(9);
							
							if (prop.P.TryGetComponent<FixedJoint2D>(out FixedJoint2D joint)) GameObject.Destroy(joint);
						}
					}
				}
			}

			bufferCount = ( gameObject.GetComponents<Rigidbody2D>()[0].OverlapCollider(contactFilter, ECols));

			if (bufferCount > 0)
			{
				for ( int j = bufferCount; --j >= 0; )
				{
					if ( ECols[j] && ECols[j].gameObject.TryGetComponent<Props>( out Props prop ) )
					{
						if (PropsList.Count == 0) continue;

						if (!PropsList.Contains(prop))
						{
							StartCoroutine( IAddToArsenal( prop.P ) );
						}
					}
				}
			}




			foreach ( PhysicalBehaviour pb in Global.main.PhysicalObjectsInWorld )
			{
				if ( xxx.IsColliding( pb.transform, transform ) && xxx.CanHold( pb ) )
				{
					StartCoroutine( IAddToArsenal( pb ) );
					yield return new WaitForFixedUpdate();
				}
				////if (item.rigidbody.bodyType == RigidbodyType2D.Static) continue;
				//if ( item.TryGetComponent<Props>( out Props prop ) )
	//            {
				//	if (PropsList.Contains(prop)) continue;
				//	if ( xxx.IsColliding( item.transform, P.transform.root , false )  )
				//	{
				//		ModAPI.Notify("Adding to Aresenal: " + item.name);
				//		StartCoroutine( IAddToArsenal( item ) );
				//	}
				//}
			}

			//Collider2D[] BOXES = gameObject.GetComponents<Collider2D>();
			
			//foreach ( Collider2D box in BOXES )
   //         {
			//	if (box == edge) continue;

			//	bufferCount       = box.OverlapCollider(contactFilterX, ECols);

			//	for ( int j = bufferCount; --j >= 0; )
			//	{
			//		if ( ECols[j].gameObject.TryGetComponent<Props>( out Props prop ) )
			//		{
			//			ModAPI.Notify (prop.name);
			//			if (PropsList.Contains(prop)) continue;

			//			StartCoroutine(IAddToArsenal(prop.P));
			//		}
			//	}
			//}

			


			
			yield return new WaitForFixedUpdate();
		}

		IEnumerator IAddToArsenal(PhysicalBehaviour pb)
		{
			yield return new WaitForFixedUpdate();

			Props prop = pb.gameObject.GetOrAddComponent<Props>();
			prop.Init(pb);

			int i = (edge.OverlapCollider(contactFilter, ECols));

			bool isOnEdge = false;

			if (i > 0)
			{

				for ( int j = 0; j < i; j++ )
				{
					if ( ECols[j].gameObject == pb.gameObject ) isOnEdge = true;
				}
			}

			if (isOnEdge) yield break;

			prop.P.rigidbody.bodyType = RigidbodyType2D.Static;
			prop.P.gameObject.SetLayer(2);
			prop.P.transform.SetParent(P.transform);
			
			FixedJoint2D joint = prop.P.gameObject.AddComponent<FixedJoint2D>() as FixedJoint2D;

			joint.connectedBody                = R;
			joint.autoConfigureConnectedAnchor = true;

			SpriteRenderer sr       = prop.P.gameObject.GetComponent<SpriteRenderer>();
			sr.sortingLayerName     = "Background";
			sr.sortingOrder         = -9;

			Effect(prop.P.rigidbody.position);

			if (!PropsList.Contains(prop)) PropsList.Add(prop);

			if (!ArmoryPositions.ContainsKey(prop)) ArmoryPositions.Add(prop, new ArmoryItemPosition(prop.transform));

			ValidatePegBoard();

			yield break;
		}

		void Effect( Vector2 location )
		{
			if (Time.time - LastParticleEffect < 1f) return;
			LastParticleEffect = Time.time;

			ModAPI.CreateParticleEffect("Disintegration", location);
		}

		void AddToArsenalX( PhysicalBehaviour pb )
		{
			Props prop = pb.gameObject.GetOrAddComponent<Props>();
			prop.Init(pb);

			int i = (edge.OverlapCollider(contactFilter, ECols));

			bool isOnEdge = false;

			if (i > 0)
			{

				for ( int j = 0; j < i; j++ )
				{
					if ( ECols[j].gameObject == pb.gameObject ) isOnEdge = true;
				}
			}

			if (isOnEdge) return;

			prop.P.rigidbody.bodyType = RigidbodyType2D.Static;
			prop.P.gameObject.SetLayer(2);
			prop.P.transform.SetParent(P.transform);
			
			FixedJoint2D joint = prop.P.gameObject.AddComponent<FixedJoint2D>() as FixedJoint2D;

			joint.connectedBody                = R;
			joint.autoConfigureConnectedAnchor = true;

			SpriteRenderer sr       = prop.P.gameObject.GetComponent<SpriteRenderer>();
			sr.sortingLayerName     = "Background";
			sr.sortingOrder         = -9;

			Effect(prop.P.rigidbody.position);
			

			if (!PropsList.Contains(prop)) PropsList.Add(prop);
			if (!ArmoryPositions.ContainsKey(prop)) ArmoryPositions.Add(prop, new ArmoryItemPosition(prop.transform));

			NpcBehaviour.HasArmory = true;
		}

		IEnumerator ICheckMaintenance()
		{
			Props prop;

			for (; ; )
			{
				yield return new WaitForSeconds(5);

				for ( int i = Customers.Count; --i >= 0; )
				{
					if ( Customers[i].Action.CurrentAction != "GearUp" )
					{
						Customers.RemoveAt(i);
						CustomerCount = Customers.Count;
					}
				}

				for ( int i = BorrowedProps.Count; --i >= 0; )
				{
					if ( !BorrowedProps[i] || !BorrowedProps[i].P )
					{
						BorrowedProps.RemoveAt(i);
						continue;
					}

					prop = BorrowedProps[i];

					if ( prop.P.beingHeldByGripper ) {
						if ( IdleDroppedList.ContainsKey( prop ) )
						{
							IdleDroppedList.Remove(prop);
							continue;
						}
					}

					if ( IdleDroppedList.TryGetValue( BorrowedProps[i], out float timer ) )
					{
						if (Time.time > timer)
						{
							StartCoroutine( IRetrieveProp( BorrowedProps[i] ));
							BorrowedProps.RemoveAt(i);
							continue;
						}
					}
					else
					{
						IdleDroppedList.Add( BorrowedProps[i], Time.time + 60 );
					}

				}
			}
		}

		public IEnumerator IRetrieveProp( Props prop )
		{
			yield return new WaitForFixedUpdate();
			Vector2 direction;

			while ( prop && prop.P )
			{
				yield return new WaitForFixedUpdate();
				direction = transform.position - prop.transform.position;

				if ( direction.sqrMagnitude > 10 )
				{
					prop.P.rigidbody.AddForce( direction * Time.fixedDeltaTime * 100 );
				} else
				{
					prop.P.rigidbody.bodyType = RigidbodyType2D.Static;
					prop.P.gameObject.SetLayer(2);
					prop.P.transform.SetParent(P.transform);
					prop.P.transform.position = P.transform.position;

					if (ArmoryPositions.ContainsKey(prop)) 
						ArmoryPositions[prop].Reposition(prop.P.transform);

					FixedJoint2D joint = prop.P.gameObject.AddComponent<FixedJoint2D>() as FixedJoint2D;

					joint.connectedBody                = R;
					joint.autoConfigureConnectedAnchor = true;

					SpriteRenderer sr       = prop.P.gameObject.GetComponent<SpriteRenderer>();
					sr.sortingLayerName     = "Background";
					sr.sortingOrder         = -9;

					Effect(prop.P.rigidbody.position);
					

					if (!PropsList.Contains(prop)) PropsList.Add(prop);
					if (IdleDroppedList.ContainsKey(prop)) IdleDroppedList.Remove(prop);
					yield break;
				}
			}
		}

		public struct ArmoryItemPosition
		{
			public Vector3 Lopo;
			public Quaternion Rota; 
			public Vector3 Loco;

			public ArmoryItemPosition( Transform T )
			{
				Lopo = T.localPosition;
				Loco = T.localScale;
				Rota = T.rotation;
			}

			public void Reposition( Transform T )
			{
				T.localPosition = Lopo;
				T.localScale    = Loco;
				T.rotation      = Rota;
			}
		}

		

		IEnumerator IAnimatePegBoard()
		{
			
			Color32 c;
			for (; ; )
			{
				if (!P) GameObject.Destroy(this);
				yield return new WaitForSeconds(0.1f);

				if ( Expansion && Expansion.MiscBools[0] && Expansion.ChildChip && Expansion.ChildChip.SRS[1])
				{
					if (!_teamSwitch)
					{
						SRS[1].material  = ModAPI.FindMaterial("VeryBright");
						_teamSwitch      = true;
					}

					c = Expansion.ChildChip.SRS[1].material.color;
					c.a = 20;
					SRS[1].color = c;
				}
				else { 
					if (_teamSwitch) { 
						SRS[1].color     = new Color(0.2f,0.2f,0.2f,1f);
						SRS[1].material  = ModAPI.FindMaterial("Sprites-Default");
						_teamSwitch      = false;
					}
				}

				if (P.charge > 1f)
				{
					if (!_powerSwitch) { 
						SRS[2].color	 = new Color(0f,1f,0f,0.1f);
						SRS[2].material  = ModAPI.FindMaterial("VeryBright");
						_powerSwitch = true;
					}
				} 
				else
				{
					if (_powerSwitch) {
						SRS[2].color	 = new Color(0.4f,0.4f,0.4f,1f);
						SRS[2].material  = ModAPI.FindMaterial("Sprites-Default");
						_powerSwitch = false;
					}
				}
			}
		}

		void OnDestroy()
		{
			ModAPI.OnItemSpawned -= AddToArsenal; 
			if (NpcArsenal.Arsenals.Contains(this)) NpcArsenal.Arsenals.Remove(this);
		}

	}

}