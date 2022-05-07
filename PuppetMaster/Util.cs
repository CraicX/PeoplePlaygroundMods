//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using System.Linq;

namespace PuppetMaster
{
	public static class Util
	{

		public static float OGSlowmotionTimescale   = 0f;
		public static float SlowmotionTimescale     = 50.1f;
		public static CameraControlBehaviour cam;
		private static readonly List<DisabledCollision> DisabledCollisions = new List<DisabledCollision>();
		public static void Destroy(UnityEngine.Object o) => UnityEngine.Object.Destroy((UnityEngine.Object)o);
		public static void DestroyNow(UnityEngine.Object o) => UnityEngine.Object.DestroyImmediate((UnityEngine.Object)o);




		public static void Notify( string msg, VerboseLevels verboseLevel=VerboseLevels.Minimal)
		{
			if ((int)JTMain.Verbose >= (int)verboseLevel) ModAPI.Notify( msg );

			if (JTMain.Verbose >= VerboseLevels.Full) Debug.Log(msg);

		}


		//
		// ─── GET CLICKED PERSON ────────────────────────────────────────────────────────────────
		//
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


		//
		// ─── FLIP ITEM ────────────────────────────────────────────────────────────────
		//
		public static void FlipItem(PhysicalBehaviour PB)
		{
			Vector3 scale = PB.transform.localScale;

			scale.x *= -1;

			PB.transform.localScale = scale;
		}

		public static void FlipItem(GameObject G)
		{
			Vector3 scale = G.transform.localScale;

			scale.x *= -1;

			G.transform.localScale = scale;
		}


		//
		// ─── DISABLE COLLISION ────────────────────────────────────────────────────────────────
		//
		public static void DisableCollision(Transform item1, PhysicalBehaviour item2, bool autoFix = true)
		{
			if (item1 == null || item2 == null) return;

			Collider2D[] noCollide = item1.root.GetComponentsInChildren<Collider2D>();

			foreach (Collider2D col1 in item2.transform.root.GetComponentsInChildren<Collider2D>())
			{
				if (col1 == null) continue;
				foreach (Collider2D col2 in noCollide)
				{
					if (col2 == null) continue;
					if ((bool)(UnityEngine.Object)col2 && (bool)(UnityEngine.Object)col1)
						Physics2D.IgnoreCollision(col1, col2);
				}
			}

			if (autoFix)
			{
				DisabledCollisions.Add(new Util.DisabledCollision(item1, item2));
				PuppetMaster.CheckDisabledCollisions = true;
			}

		}

		//
		// ─── FIX DISABLED COLLISIONS ────────────────────────────────────────────────────────────────
		//
		public static void FixDisabledCollisions()
		{
			if (!PuppetMaster.CheckDisabledCollisions) return;

			if (Thing.delayCollisions > Time.time) return;

			bool stillActives = false;

			if (DisabledCollisions.Count > 0)
			{
				for (int i = DisabledCollisions.Count; --i >= 0;)
				{
					if (DisabledCollisions[i].Check()) DisabledCollisions.RemoveAt(i);
					else stillActives = true;
				}
			}

			if (!stillActives) PuppetMaster.CheckDisabledCollisions = false;
		}


		//
		// ─── TOGGLE WALL COLLISIONS ────────────────────────────────────────────────────────────────
		//
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

		//
		// ─── TOGGLE COLLISIONS ────────────────────────────────────────────────────────────────
		//
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


		public static bool IsColliding(Transform t1, Transform t2, bool goRoot=true)
		{
			Collider2D[] colSet1;
			Collider2D[] colSet2;

			if (goRoot)
			{
				colSet1 = t1.root.GetComponentsInChildren<Collider2D>();
				colSet2 = t2.root.GetComponentsInChildren<Collider2D>();
			} else
			{
				colSet1 = t1.GetComponentsInChildren<Collider2D>();
				colSet2 = t2.GetComponentsInChildren<Collider2D>();
			}

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

		public static Vector2 FindFloor( Vector2 startPos )
		{
			RaycastHit2D hit = Physics2D.Raycast((Vector2) startPos, (Vector2) Vector2.down, 5f, 11);

			if (hit.collider != null) return hit.transform.position;
			else return Vector2.zero;
		}

		//
		// ─── MAX PAYNE MODE ────────────────────────────────────────────────────────────────
		//
		public static void MaxPayne(bool turnOn = true)
		{
			if (OGSlowmotionTimescale == 0f) OGSlowmotionTimescale = Global.main.SlowmotionTimescale;

			if (turnOn)
			{
				//if (timeScale == 0)   timeScale = SlowmotionTimescale;
				//Global.main.SlowmotionTimescale = timeScale;

				if (!Global.main.SlowMotion) Global.main.ToggleSlowmotion();

				return;
			}

			if (Global.main.SlowMotion) Global.main.ToggleSlowmotion();

			Global.main.SlowmotionTimescale = OGSlowmotionTimescale;
		}


		public static Vector3 ClampPos(Vector3 v)
		{

			Bounds box = cam.BoundingBox;

			Vector3 vect = new Vector3(
			Mathf.Clamp(v.x,
				box.center.x - box.extents.x - cam.Extend,
				box.center.x + box.extents.x + cam.Extend),
			Mathf.Clamp(v.y,
				box.center.y - box.extents.y - cam.Extend,
				box.center.y + box.extents.y + cam.Extend),
			-10f);

			return vect;
		}


		//
		// ─── GET OVERALL HEALTH ────────────────────────────────────────────────────────────────
		//
		public static int GetOverallHealth(PersonBehaviour PB)
		{
			float totalHealth   = 0f;
			float currentHealth = 0f;
			for (int i = PB.Limbs.Length; --i >= 0;) 
			{
				totalHealth   += PB.Limbs[i].InitialHealth;
				currentHealth += PB.Limbs[i].Health;
			}

			if (currentHealth == 0) return 0;

			return Mathf.RoundToInt((currentHealth / totalHealth) * 100);
		}


		//
		// ─── STORE HOLDING POSITIONS ────────────────────────────────────────────────────────────────
		//
		public static void SaveHoldingPositions()
		{
			return;
			//if (Thing.ManualPositions.Count == 0) return;

			//string holdingPositions = "";

			//foreach (KeyValuePair<int, Vector2> pair in Thing.ManualPositions)
			//{
			//    string holdPos = pair.Key.ToString() + ":" + pair.Value.x.ToString() + ":" + pair.Value.y.ToString() + ",";
			//    holdingPositions += holdPos;
			//}

			//PlayerPrefs.SetString("JTHoldingPositions", holdingPositions);

			//Notify("[<color=yellow>" + Thing.ManualPositions.Count + "</color>] Holding Positions saved", VerboseLevels.Minimal);
		}


		public static void FireProof(PhysicalBehaviour pb)
		{
			PhysicalProperties propys          = pb.Properties.ShallowClone();
			propys.Flammability                = 0f;
			propys.BurningTemperatureThreshold = float.MaxValue;
			propys.Burnrate                    = 0.00001f;

			pb.Extinguish();
			pb.Properties          = propys;
			pb.ChargeBurns         = false;
			pb.BurnProgress        = 0f;
		}

		public static void LoadHoldingPositions()
		{

			return;
			//string holdingPositions = PlayerPrefs.GetString("JTHoldingPositions");

			//if (holdingPositions.Length == 0) return;

			//Thing.ManualPositions.Clear();

			//string[] AllPo = holdingPositions.Split(',');

			//for (int i = AllPo.Length; --i >= 0;)
			//{
			//    string[] segments = AllPo[i].Split(':');    

			//    if (segments.Length != 3) continue;

			//    if (int.TryParse(segments[0], out int hash) && float.TryParse(segments[1], out float x) && float.TryParse(segments[2], out float y))
			//    {
			//         Thing.ManualPositions.Add( hash, new Vector2(x,y));
			//    }
			//}

			//Notify("[<color=yellow>" + Thing.ManualPositions.Count + "</color>] Holding Positions loaded", VerboseLevels.Minimal);

		}


		//
		// ─── STRUCT: DISABLED COLLISION ────────────────────────────────────────────────────────────────
		//
		public struct DisabledCollision
		{
			private readonly Transform obj1;
			private readonly PhysicalBehaviour obj2;
			private readonly float Timestamp;
			public DisabledCollision(Transform _obj1, PhysicalBehaviour _obj2)
			{
				obj1      = _obj1;
				obj2      = _obj2;
				Timestamp = Time.time;
			}
			public bool Check()
			{

				if (obj1 == null || obj2 == null) return true;
				if (Time.time - Timestamp < 1.0f) return false;
				if (obj2.colliders.Length > 0)
				{

					foreach (Collider2D col1 in obj2.colliders)
					{
						foreach (Collider2D col2 in obj1.GetComponentsInChildren<Collider2D>())
						{
							if ((bool)(UnityEngine.Object)col1 && (bool)(UnityEngine.Object)col2 && col1.bounds.Intersects(col2.bounds)) return false;
						}
					}
					foreach (Collider2D col1 in obj2.colliders)
					{
						foreach (Collider2D col2 in obj1.GetComponentsInChildren<Collider2D>())
						{
							if ((bool)(UnityEngine.Object)col1 && (bool)(UnityEngine.Object)col2)
								Physics2D.IgnoreCollision(col1, col2, false);
						}
					}
				}
				return true;
			}

		}
	}

	public class XTimer
	{
		float _timer;
		public bool flag   = false;
		public int counter = 0;

		public XTimer() => _timer = Time.time;
		public void Restart() {_timer = Time.time; flag = false; }
		public float time => Time.time - _timer;
		public int IncCount() => ++counter;
	}
}
