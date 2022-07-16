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
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System;

namespace PPnpc
{
	public static class xxx
	{

		public static Props GetItemProps( PhysicalBehaviour pb )
		{
			if (pb.TryGetComponent<Props>(out Props props)) return props;

			Props prop = pb.gameObject.AddComponent<Props>() as Props;

			prop.Init(pb);

			return prop;

		}

		public static void CheckRebirthers()
		{
			NpcRebirther[] Rebirthers = UnityEngine.Object.FindObjectsOfType<NpcRebirther>();

			//Shuffle(Rebirthers);

			NpcGlobal.Rebirthers = new bool[]{false,false,false,false,false};

			foreach ( NpcRebirther rebirther in Rebirthers)
			{
				if (rebirther.RebirtherActive) {
					NpcGlobal.Rebirthers[rebirther.TeamId] = true;

				}
			}
		}


		public static void Shuffle<T> (T[] array)
		{
			System.Random rng = new System.Random();

			int n = array.Length;
			while (n > 1) 
			{
				int k = rng.Next(n--);
				T temp = array[n];
				array[n] = array[k];
				array[k] = temp;
			}
		}


		public static Quaternion LookAt2D(Vector2 forward) => Quaternion.Euler(0, 0, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg);

		public static Dictionary<int, WeaponBasics> WeaponBasicsCache = new Dictionary<int, WeaponBasics>();
		public static WeaponBasics GetWeaponBasics( PhysicalBehaviour weapon )
		{
			int weaponHash = weapon.name.GetHashCode();
			if ( WeaponBasicsCache.TryGetValue( weaponHash, out WeaponBasics weaponB ) ) return weaponB;

			WeaponBasics basics = new WeaponBasics()
			{
				CanExplode  = false,
				CanShoot    = false,
				CanStab     = false,
				IsAutomatic = false,
			};

			string AutoFire       = @"ACCELERATORGUNBEHAVIOUR,FLAMETHROWERBEHAVIOUR,PHYSICSGUNBEHAVIOUR,MINIGUNBEHAVIOUR,TEMPERATURERAYGUNBEHAVIOUR";
			string ManualFire	  = @"ARCHELIXCASTERBEHAVIOUR,BEAMFORMERBEHAVIOUR,ROCKETLAUNCHERBEHAVIOUR,GENERICSCIFIWEAPON40BEHAVIOUR,
									LIGHTNINGGUNBEHAVIOUR,PULSEDRUMBEHAVIOUR,HEALTHGUNBEHAVIOUR,FREEZEGUNBEHAVIOUR";

			MonoBehaviour[] Components = weapon.GetComponents<MonoBehaviour>();

			if (Components.Length > 0)
			{
				for (int i = Components.Length; --i >= 0;)
				{
					string compo = Components[i].GetType().ToString().ToUpper();

					if (AutoFire.Contains(compo))
					{
						basics.CanShoot    = true;
						basics.IsAutomatic = true;
					}

					if (ManualFire.Contains(compo))
					{
						basics.CanShoot    = true;
						basics.IsAutomatic = false;
					}
				}
			}

			CanShoot[] ShootComponents = weapon.GetComponents<CanShoot>();

			if ( ShootComponents.Length > 0 ) basics.CanShoot    = true;

			if (weapon.TryGetComponent(out FirearmBehaviour FBH))
			{
				basics.CanShoot             = true;
				basics.IsAutomatic          = FBH.Automatic;
			}
			else if (weapon.TryGetComponent(out ProjectileLauncherBehaviour PLB))
			{
				basics.CanShoot             = true;
				basics.IsAutomatic          = PLB.IsAutomatic;
			}
			else if (weapon.TryGetComponent(out BlasterBehaviour BB))
			{
				basics.CanShoot             = true;
				basics.IsAutomatic          = BB.Automatic;
			}

			WeaponBasicsCache.Add(weaponHash, basics);

			return basics;
		}


		public static Vector2 XYbounds = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

		public static float xClamp(float inx) => Mathf.Clamp( inx, 0, XYbounds.x);
		public static float yClamp(float iny) => Mathf.Clamp( iny, 0, XYbounds.y);
		public static float rr(float nMin, float nMax) => UnityEngine.Random.Range(nMin, nMax);
		public static int rr(int nMin, int nMax) => UnityEngine.Random.Range(nMin, nMax);

		public static ContactFilter2D filter = new ContactFilter2D();

		public static List<Collider2D> ColliderList = new List<Collider2D>();

		public static void FixCollisions( Transform t1 )
		{
			Collider2D[] colSet2  = GameObject.FindObjectsOfType<Collider2D>();
			Collider2D[] colSet1  = t1.GetComponentsInChildren<Collider2D>();

			Transform selfRoot = t1.root;

			foreach (Collider2D col1 in colSet1)
			{
				if (col1 == null) continue;
				if (!(bool)(UnityEngine.Object) col1) continue;

				foreach (Collider2D col2 in colSet2)
				{
					if (col2 == null || col1 == col2 || col1.transform.root == col2.transform.root) continue;
					if ((bool)(UnityEngine.Object) col2) { 
						//IgnoreCollisionStackController.RequestDontIgnoreCollision( col1, col2 );
						Physics2D.IgnoreCollision(col1,col2,false);
					}
				}
			}
		}




		public static bool AimingTowards( Transform gun, Transform target)
		{
			float facing = (gun.transform.localScale.x < 0.0f) ? 1f : -1f;
			int numRes = Physics2D.Raycast((Vector2) gun.transform.position, (Vector2) gun.transform.TransformVector(Vector3.right), filter, NpcBehaviour.HitResults, 10f);

			Transform t = target == target.root ? target : target.root;

			for ( int i = -1; ++i < numRes; )
			{
				if (NpcBehaviour.HitResults[i].transform.root == t && Mathf.Abs(NpcBehaviour.HitResults[i].transform.position.y - target.position.y) < 1f ) return true;
			}

			return false;
		}



		public static float GetThreatLevelOfItem( PhysicalBehaviour item )
		{
			float score = 1;

			if (item.Properties.Sharp && item.StabCausesWound) score += 2;
			if (item.TryGetComponent<SharpOnAllSidesBehaviour>( out _)) score += 2;
			if (item.TryGetComponent<FirearmBehaviour>( out _)) score += 5;

			return score * (1+item.ObjectArea);


		}


		public static float CalculateThreatLevel( NpcBehaviour npc )
		{
			if (!npc || !npc.PBO ) return 0f;

			float threatLevel = npc.Config.BaseThreatLevel;

			threatLevel += npc.PBO.AverageHealth - 14f;

			threatLevel += 5f - npc.LB["UpperBody"].BaseStrength;

			foreach ( LimbBehaviour limb in npc.PBO.Limbs )
			{
				if (limb  && !limb.Broken && !limb.IsConsideredAlive) threatLevel += 0.1f;
			}

			if (npc.FH.IsHolding && npc.FH.Tool) threatLevel += npc.FH.Tool.props.ThreatLevel;
			if (npc.BH.IsHolding && npc.BH.Tool) threatLevel += npc.BH.Tool.props.ThreatLevel;

			threatLevel -= npc.Mojo.Feelings["Chicken"];

			if (!npc.IsUpright) threatLevel *= 0.9f;

			return threatLevel;
		}

		public static bool CanHold(PhysicalBehaviour PB)
		{
			if (!PB || PB.beingHeldByGripper 
				|| PB.TryGetComponent<FreezeBehaviour>(out _)
				|| PB.TryGetComponent<HingeJoint2D>(out _)
				|| PB.TryGetComponent<FixedJoint2D>(out _)
				|| PB.gameObject.layer != 9 
				|| PB.InitialMass > 2f
				|| PB.penetrations.Count > 0
				|| PB.HoldingPositions.Length == 0 ) return false;

			return true;
		}

		public static bool CanTarget( NpcTool gun, Transform target )
		{
			LayerMask layers = LayerMask.GetMask("Objects", "Background");

			Vector2 direction, spacer;

			if (gun.Facing > 0) {
				direction = (target.position - gun.P.transform.position) * gun.Facing;
				spacer    = new Vector2(-1.5f,0f);
			}
			else {
				direction = (target.position - gun.P.transform.position) * -gun.Facing;
				spacer    = new Vector2(1.5f,0f);
			}

			RaycastHit2D ray = Physics2D.Raycast((Vector2)gun.transform.position + spacer, direction.normalized, 50f, layers);
			if (!ray) return false;

			if (ray.collider.transform.root == target.root) return true;

			// Debug.Log(gun.P.name + ":" + ray.collider.transform.root.name + " -> " + ray.collider.gameObject.name);

			return false; 

		}




		public static bool NPCOnFire( NpcBehaviour npc )
		{
			if (!npc || !npc.PBO) return false;
			foreach ( LimbBehaviour limb in npc.PBO.Limbs )
			{
				if (!limb) continue;
				if (limb.PhysicalBehaviour.OnFire) return true;
			}

			return false;
		}

		public static bool ValidateEnemyTarget( PersonBehaviour enemy )
		{
			return ( enemy 
				&& enemy.IsAlive()
				&& enemy.Consciousness > 0.3f 
			);
		}

		public static PhysicalBehaviour ClosestItemNamed( string itemName, NpcBehaviour npc )
		{
			PhysicalBehaviour[] Items = UnityEngine.Object.FindObjectsOfType<PhysicalBehaviour>();

			PhysicalBehaviour closestItem = null;

			Vector2 loc = npc.transform.position;

			float dist = float.MaxValue;
			float temp = float.MaxValue;

			foreach ( PhysicalBehaviour item in Items )
			{
				if ( item.name.ToLower().Contains( itemName.ToLower() ) )
				{
					temp = Vector2.Distance(loc, item.transform.position);
					if ( temp < dist )
					{
						dist        = temp;
						closestItem = item;
					}
				}
			}

			return closestItem;
		}



		public static NpcBehaviour ClosestNpc( NpcBehaviour ogNpc, NpcBehaviour[] NpcList, bool mustBeFacing = false )
		{
			Vector3 MyPos = ogNpc.Head.position;

			NpcBehaviour closeNpc = null;
			float dist = float.MaxValue;
			float tmp;
			for ( int i = NpcList.Length; --i >= 0; )
			{
				if (!NpcList[i]) continue;
				if (mustBeFacing && !ogNpc.FacingToward(NpcList[i].Head.position)) continue;
				tmp = ((MyPos - NpcList[i].Head.position).sqrMagnitude);
				if (tmp < dist)
				{
					dist = tmp;
					closeNpc = NpcList[i];
				}
			}

			return closeNpc;
		}

		public static NpcBehaviour ClosestNpc( NpcBehaviour ogNpc, List<NpcBehaviour> NpcList, bool mustBeFacing = false )
		{
			Vector3 MyPos = ogNpc.Head.position;

			NpcBehaviour closeNpc = null;
			float dist = float.MaxValue;
			float tmp;
			for ( int i = NpcList.Count; --i >= 0; )
			{
				if (!NpcList[i]) continue;
				if (mustBeFacing && !ogNpc.FacingToward(NpcList[i].Head.position)) continue;
				tmp = ((MyPos - NpcList[i].Head.position).sqrMagnitude);
				if (tmp < dist)
				{
					dist = tmp;
					closeNpc = NpcList[i];
				}
			}

			return closeNpc;
		}

		public static NpcBehaviour ClosestNpc( NpcBehaviour ogNpc )
		{
			Vector2 pos = ogNpc.transform.position;

			NpcBehaviour[] locals = ogNpc.FindLocalNpc(10);
			NpcBehaviour closeNpc = null;
			float dist = float.MaxValue;
			float tmp;
			for ( int i = locals.Length; --i >= 0; )
			{
				tmp = Vector2.Distance(locals[i].transform.position,pos);
				if (tmp < dist)
				{
					dist = tmp;
					closeNpc = locals[i];
				}
			}

			return closeNpc;

		}

		public static PhysicalBehaviour GetClosestItemFromList(List<PhysicalBehaviour> list, Vector3 pos)
		{
			PhysicalBehaviour closestItem = null;
			float dist = float.MaxValue;
			float tmp;

			foreach ( PhysicalBehaviour item in list )
			{
				tmp = (pos - item.transform.position).sqrMagnitude;
				if (tmp < dist)
				{
					dist = tmp;
					closestItem = item;
				}
			}

			return closestItem;
		}

		public static void ToggleCollisions(Transform t1, Transform t2, bool enable=false, bool goRoot=true)
		{
			if (t1 == null || t2 == null || t1 == t2) return;

			if (goRoot)
			{
				Collider2D[] colSet1;
				Collider2D[] colSet2;
				colSet1 = t1.root.GetComponentsInChildren<Collider2D>();
				colSet2 = t2.root.GetComponentsInChildren<Collider2D>();

				foreach (Collider2D col1 in colSet1)
				{
					if (col1 == null) continue;
					if (!(bool)(UnityEngine.Object) col1) continue;

					foreach (Collider2D col2 in colSet2)
					{
						if (col2 == null) continue;
						if ((bool)(UnityEngine.Object) col2) 
							Physics2D.IgnoreCollision(col1,col2,!enable);
					}
				}
			}

			Collider2D[] colSet3;
			Collider2D[] colSet4;

			colSet3 = t1.GetComponentsInChildren<Collider2D>();
			colSet4 = t2.GetComponentsInChildren<Collider2D>();

			foreach (Collider2D col1 in colSet3)
			{
				if (col1 == null) continue;
				if (!(bool)(UnityEngine.Object) col1) continue;

				foreach (Collider2D col2 in colSet4)
				{
					if (col2 == null) continue;
					if ((bool)(UnityEngine.Object) col2) 
						Physics2D.IgnoreCollision(col1,col2,!enable);
				}
			}
		}


		public static void ToggleWallCollisions(Transform t, bool enable=false, bool goRoot=true)
		{
			Collider2D[] colSet1;

			if (goRoot) colSet1 = t.root.GetComponentsInChildren<Collider2D>();
			else colSet1 = t.GetComponentsInChildren<Collider2D>();

			foreach( Collider2D col1 in colSet1) 
			{
				if (col1 == null) continue;
				foreach (PhysicalBehaviour PB in Global.main.PhysicalObjectsInWorld)
				{
					if (PB == null) continue;
					if (PB.gameObject.layer != 11) continue;

					foreach (Collider2D col2 in PB.GetComponentsInChildren<Collider2D>())
					{
						if ((bool)(UnityEngine.Object)col2)
							Physics2D.IgnoreCollision(col1, col2, !enable);
					}
				}
			}
		}


		public static bool IsTouching( PhysicalBehaviour p1, PhysicalBehaviour p2 )
		{
			if (!p1 || !p2) return false;
			
			for ( int i1 = p1.colliders.Length; --i1 >= 0; )
			{
				for ( int i2 = p2.colliders.Length; --i2 >= 0; )
				{
					if (Physics2D.Distance( p1.colliders[i1], p2.colliders[i2] ).isOverlapped) return true;
				}
			}
			return false;
		}


		public static bool IsColliding(Transform t1, Transform t2, bool goRoot=true)
		{
			Collider2D[] colSet1;
			Collider2D[] colSet2;
			
			if (goRoot)
			{
				if (!t1||!t2||!t1.root||!t2.root) return false;
				colSet1 = t1.root.GetComponentsInChildren<Collider2D>();
				colSet2 = t2.root.GetComponentsInChildren<Collider2D>();
			} else
			{
				if (!t1||!t2) return false;
				colSet1 = t1.GetComponentsInChildren<Collider2D>();
				colSet2 = t2.GetComponentsInChildren<Collider2D>();
			}

			filter.NoFilter();

			List<Collider2D> colResults = new List<Collider2D>();

			foreach (Collider2D col1 in colSet1)
			{
				col1.OverlapCollider(filter, colResults);
				if (colResults.Intersect(colSet2).Any()) return true;

			}

			return false;
		}

		public static PersonBehaviour GetClickedPerson()
		{
			foreach (PhysicalBehaviour PB in Global.main.PhysicalObjectsInWorld)
			{
				if (!PB.Selectable || PB.colliders.Length == 0) continue;

				foreach (Collider2D collider in PB.colliders)
				{
					if (!collider) continue;

					if (collider.OverlapPoint((Vector2)Global.main.MousePosition))
					{
						return PB.GetComponentInParent<PersonBehaviour>();
					}
				}
			}

			return null;
		}

		 //
		// ─── GET CLICKED ITEM ────────────────────────────────────────────────────────────────
		//
		public static PhysicalBehaviour GetClickedItem()
		{
			foreach (PhysicalBehaviour PB in Global.main.PhysicalObjectsInWorld)
			{
				if (!PB.Selectable || PB.colliders.Length == 0) continue;

				foreach (Collider2D collider in PB.colliders)
				{
					if (collider && collider.OverlapPoint((Vector2)Global.main.MousePosition)) return PB;
				}
			}

			return null;
		}





		public static Vector2 FindFloor( Vector2 startPos )
		{
			RaycastHit2D hit = Physics2D.Raycast((Vector2) startPos, (Vector2) Vector2.down, 5f, 11);

			if (hit.collider != null) return hit.transform.position;
			else return Vector2.zero;
		}


		//
		// ─── RUN LIMBS ────────────────────────────────────────────────────────────────
		//
		public  static void RunLimbs(LimbBehaviour[] LBs , Action<LimbBehaviour> action)
		{
			foreach (LimbBehaviour limb in LBs) { action(limb); }
		}



		public static void LimbGhost(LimbBehaviour limb, bool option) { limb.gameObject.layer = LayerMask.NameToLayer(option ? "Debris" : "Objects"); }
		public static void LimbKill(LimbBehaviour limb) { limb.Health = 0; limb.IsLethalToBreak = true; limb.Broken = true; }
		public static void LimbCrush(LimbBehaviour limb) { limb.Crush(); }


		//
		// ─── RUN RIGIDS ────────────────────────────────────────────────────────────────
		//
		public static void RunRigids(Rigidbody2D[] RBs, Action<Rigidbody2D> action)
		{
			foreach (Rigidbody2D rigid in RBs) { action(rigid); }
		}


		public static string GetTeamColor( int teamId )
		{
			string[] TeamColors =
			{
				"#000000",
				"#FF0000",
				"#0000FF",
				"#00CC00",
				"#FFFF00",
				"#FF00FF",
				"#00FFFF",
			};

			if (TeamColors.Length >= teamId) return TeamColors[teamId];

			return "#000000";

		}




		public static LineRenderer InitLine(GameObject lineOwner)
		{
			LineRenderer lr = lineOwner.GetOrAddComponent<LineRenderer>();

			lr.enabled             = false;
			lr.startColor          = new Color(1f, 0.1f, 0.1f, 1f);
			lr.endColor            = new Color(0.1f, 0.1f, 1f, 1f);
			//lr.startColor        = new Color(1f, 0.3f, 0f, 0.25f);
			//lr.endColor          = new Color(1f, 0.4f, 0f, 0.5f);
			lr.startWidth          = 0.06f;
			lr.endWidth            = 0.06f;
			lr.numCornerVertices   = 0;
			lr.numCapVertices      = 0;
			lr.useWorldSpace       = true;
			lr.alignment           = LineAlignment.View;
			lr.sortingOrder        = 2;
			lr.material            = Resources.Load<Material>("Materials/PhaseLink");
			//lr.material          = ModAPI.FindMaterial("Sprites-Default");
			lr.textureMode         = LineTextureMode.DistributePerSegment;
			lr.textureMode         = LineTextureMode.Tile;
			lr.hideFlags           = HideFlags.HideAndDontSave;

			return lr;
		}
	}

	

	public class NpcDeath : MonoBehaviour
	{
		public PersonBehaviour PBO;
		public float DecomposeRate = 1.0f;
		public MinMax SecondsBeforeCrush = new MinMax(1.0f, 5.0f);

		void Start()
		{
			if ( !gameObject.transform.root.TryGetComponent<PersonBehaviour>( out PersonBehaviour _pbo) )
			{
				GameObject.Destroy(this);
				StartCoroutine(IReaper());
			}
			PBO = _pbo;
		}

		public void Config( NpcBehaviour npc ) => Config(npc.Config);
		public void Config( NpcConfig npcConfig )
		{
			StopAllCoroutines();
			DecomposeRate      = npcConfig.DecomposeRate;
			SecondsBeforeCrush = npcConfig.SecondsBeforeCrush;
			StartCoroutine(IReaper());
		}

		IEnumerator IReaper()
		{
			if ( gameObject.TryGetComponent<NpcBehaviour>( out NpcBehaviour npx ) )
			{
				GameObject.DestroyImmediate(npx);
			}

			if (!PBO) {
				if ( transform.root.TryGetComponent<PersonBehaviour>( out PersonBehaviour _pbo ) )
				{
					PBO = _pbo;
				} else yield break;

			}

			float timeExplode = Time.time + xxx.rr(SecondsBeforeCrush.Min, SecondsBeforeCrush.Max);
			while ( Time.time < timeExplode )
			{
				foreach( LimbBehaviour limb in PBO.Limbs ) {
					limb.SkinMaterialHandler.RottenProgress *= DecomposeRate;
				}
				yield return new WaitForSeconds(1);
			}


			foreach( LimbBehaviour limb in PBO.Limbs ) {
					
				limb.CirculationBehaviour.Disintegrate();

				for ( int i = 0; i < limb.ConnectedLimbs.Count; i++ )
				{
					LimbBehaviour limbBehaviour = limb.ConnectedLimbs[i];
					if ( limb.NodeBehaviour.IsConnectedTo( limbBehaviour.NodeBehaviour ) && limbBehaviour.HasJoint && limbBehaviour.Joint.connectedBody == limb.PhysicalBehaviour.rigidbody )
					{
						limbBehaviour.Joint.breakForce = 0f;
					}
				}
				limb.NodeBehaviour.DisconnectFromEverything();
				if ( limb.HasJoint )
				{
					limb.Joint.breakForce = 0f;
					limb.Joint.breakTorque = 0f;
				}	limb.CirculationBehaviour.Disintegrate();
					limb.enabled = false;

			}

			timeExplode = Time.time + xxx.rr(1,3);
			while ( Time.time < timeExplode )
			{
				foreach( LimbBehaviour limb in PBO.Limbs ) {
					limb.transform.localScale *= 0.9f;
				}

				yield return new WaitForSeconds(0.1f);
			}

			foreach ( LimbBehaviour limb in PBO.Limbs )
			{
				limb.PhysicalBehaviour.rigidbody.bodyType = RigidbodyType2D.Static;
				limb.transform.position = new Vector3(-100f, -100f);
			}
				
			foreach( LimbBehaviour limb in PBO.Limbs ) {
				limb.StopAllCoroutines();
				limb.transform.localScale = Vector2.zero;
				yield return new WaitForFixedUpdate();
				limb.Crush();
			}


			GameObject.Destroy(PBO);
		}

	}
}