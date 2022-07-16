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
	public class NpcTool : MonoBehaviour
	{
		public NpcHand Hand;

		public PhysicalBehaviour P;
		public Rigidbody2D R;
		public Transform T;
		public GameObject G;
		public Props props;

		public float angleHold   = 95f;
		public bool CanAim       = false;
		public bool CanStrike    = false;

		public bool IsFlipped => (bool)(T.localScale.x < 0.0f);
		public float Facing => (T.localScale.x < 0.0f) ? 1f : -1f;

		public Vector3 HoldingPosition;
		public Vector3 AltHoldingPosition;
		public float lastFire;

		public Vector3 ThrowTargetPos;
		public Vector3 ThrowStartPos;
		public float ThrowSpeed;
		public float ThrowArc;
		public bool ThrownTool = false;
		public bool TossedTool = false;
		public Vector3 ThrowNextPos;
		public Vector2 ThrowVelocity;
		public Coroutine validate;
		private int nogrip = 0;

		public Dictionary<Transform, float> BGhost = new Dictionary<Transform, float>();

		void OnGripped( GripBehaviour grip )
        {
			//ModAPI.Notify(P.name + " -> Picked up by: " + grip.name);
        }

		void FixedUpdate()
		{
			if (Hand == null)
			{
				UnityEngine.Object.Destroy(this);
			}

			if (TossedTool) CalculateTrajectory();
			if (ThrownTool) CheckLaunch();
		}

		void CheckLaunch()
		{
			if (P.beingHeldByGripper) return;

			ThrownTool = false;

			P.rigidbody.velocity = ThrowVelocity;
		}

		void CalculateTrajectory()
		{
			float x0     = ThrowStartPos.x;
			float x1     = ThrowTargetPos.x;
			float dist   = x1 - x0;
			float nextX  = Mathf.MoveTowards(P.transform.position.x, x1, ThrowSpeed * Time.deltaTime);
			float baseY  = Mathf.Lerp(ThrowStartPos.y, ThrowTargetPos.y, (nextX - x0) / dist);
			float arc    = ThrowArc * (nextX - x0) * (nextX - x1) / (-0.25f * dist * dist);
			ThrowNextPos = new Vector3(nextX, baseY + arc, P.transform.position.z);

			// Rotate to face the next position, and then move there
			P.transform.rotation = xxx.LookAt2D(ThrowNextPos - P.transform.position);
			P.transform.position = ThrowNextPos;

			// Do something when we reach the target
			if (ThrowNextPos == ThrowTargetPos) ThrownTool = false;
		}


		public void Dropped()
		{
			if(Hand && Hand.AltHand && Hand.AltHand.Tool == this) {
				Hand = Hand.AltHand;
				return;
			}
			else Hand = null;
			PersonBehaviour[] People = UnityEngine.Object.FindObjectsOfType<PersonBehaviour>();
			if (P) P.MakeWeightful();

			if ( P && People.Length > 0)
			{
				foreach ( PersonBehaviour peep in People )
				{
					if (!peep) continue;
					xxx.ToggleCollisions(P.transform, peep.Limbs[0].transform, true );
				}
			}
		}


		public void SetTool( PhysicalBehaviour item, NpcHand hand )
		{
			if (Hand != null && Hand != hand && Hand.NPC != hand.NPC)
			{
				if (hand.Tool == this) hand.Drop();
			}

			G  = item.gameObject;
			P  = item;
			R  = item.rigidbody;
			T  = G.transform;

			props = G.GetOrAddComponent<Props>();

			props.Init(item);

			hand.Tool  = this;
			this.Hand  = hand;
			
			hand.NPC.Actions.RecacheMoves = true;

			Array.Sort( P.HoldingPositions,(a,b) => a.x.CompareTo(b.x) );

			HoldingPosition = P.HoldingPositions[0];
			Array.Reverse(P.HoldingPositions);
			AltHoldingPosition = P.HoldingPositions[0];

			//StopAllCoroutines();
			if (validate != null) StopCoroutine(validate);
			validate = StartCoroutine(IValidate());
		}



		public void TossTo( Vector3 target, float speed, float arc )
		{
			Hand.Drop();
			ThrowTargetPos = target;
			ThrowSpeed     = speed;
			ThrowArc       = arc;
			ThrowStartPos  = P.transform.position;
			ThrownTool     = true;
		}

		public void ThrowAt( Vector3 target, float speed, float arc )
		{
			Hand.Drop();
			ThrowTargetPos = target;
			ThrowSpeed     = speed;
			ThrowArc       = arc;

			ThrowStartPos  = P.transform.position;

			float gravity     = Physics2D.gravity.magnitude; 
			float distance    = Mathf.Abs(target.x - ThrowStartPos.x); 
			float maxHeight   = distance / 4; 
			float startSpeedY = Mathf.Sqrt(2.0f * gravity * maxHeight); 
			float time        = (startSpeedY + Mathf.Sqrt(startSpeedY *  startSpeedY
								- 2*gravity * target.y))/gravity; 
			float startSpeedX = distance / time; 


			// choose direction  
			if (target.x - ThrowStartPos.x > 0.0f){
				// right direction 
				ThrowVelocity = new Vector2(startSpeedX, startSpeedY); 
			} 
			else { 
				// left direction 
				ThrowVelocity = new Vector2(-startSpeedX, startSpeedY); 
			} 

			ThrownTool = true;


		}

		public void PrepWeaponStrike( NpcBehaviour enemy, Transform EnemyTrans )
		{
			if (xxx.IsColliding(T,EnemyTrans)) return;
			xxx.ToggleCollisions(EnemyTrans, T, true, false);
			enemy.BGhost[T]    = Time.time;
			BGhost[EnemyTrans] = Time.time;
		}


		public Dictionary<int, float> JammedCollisions = new Dictionary<int, float>();

		float lastColCheck = 0;
		
		void OnCollisionStay2D( Collision2D collision )
		{
			if (collision == null || !collision.gameObject ) return;

			if (Time.frameCount % 2 == 0) return;

			int hash = collision.otherRigidbody.transform.root.GetHashCode();

			if ( JammedCollisions.TryGetValue( hash, out float jammed ) )
			{
				if (Time.time - jammed > 0.5f)
				{
					xxx.ToggleCollisions(collision.otherRigidbody.transform, T,false,false);
					JammedCollisions.Remove( hash );
				}
				
				return;
			}

			JammedCollisions[hash] = Time.time;
		}


		private void OnCollisionEnter2D(Collision2D coll=null)
		{
			if (coll.gameObject.layer == 11) {
				if ( coll.gameObject.name.Contains( "wall" )) xxx.ToggleCollisions(T,coll.transform,false,true);
				return;		
			}
			string lCase = coll.gameObject.name.ToLower();

			if (NpcGlobal.NoClip.Contains(coll.gameObject.name) || NpcGlobal.NoClipPartial.Any(lCase.Contains)) { 
				if (coll.gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer srx)) { 
					srx.sortingLayerName = "Background";
					srx.sortingOrder     = -10;
				}
				xxx.ToggleCollisions(T, coll.transform,false, false);
				return;
			} 

			Transform[] Ts;

			
			if (!Hand || !Hand.NPC || !Hand.NPC.PBO) return;
			
			if ( coll.transform == coll.transform.root )
				Ts = new Transform[] { coll.transform };
			else
				Ts = new Transform[] { coll.transform, coll.transform.root };

			if (BGhost.Keys.Intersect(Ts).Any()) return;


			if ( coll.gameObject.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour phys ) )
			{
				if (phys.beingHeldByGripper)
				{
					xxx.ToggleCollisions(T,coll.transform,false, true);
					return;
				}
			}

			if (props.NoGhost) return;

			//  Disable collisions between this held item and whoever we bumped into (if we're not fighting)
			//
			if (coll.transform.root.gameObject.TryGetComponent<NpcBehaviour>(out NpcBehaviour enemy)) {



			}
			
			xxx.ToggleCollisions(T,coll.transform,false, true);

		}


		public IEnumerator IValidate()
		{
			for (; ; )
			{
				if (P && !P.beingHeldByGripper)
				{
					if (++nogrip > 2) { 
						Hand = null;
						xxx.FixCollisions(T);
						P.MakeWeightful();
						GameObject.Destroy(this);
						yield break;
					}
				} else nogrip = 0;

				if (!Hand 
					|| !Hand.NPC 
					|| !Hand.NPC.PBO 
					|| !Hand.NPC.PBO.IsAlive()
					|| Hand.Tool != this)
				{
					if ( P )
					{
						if (NpcMain.DEBUG_LOGGING) Debug.Log(name + "[action]: IValidate:nohand()");
						xxx.FixCollisions(T);
						P.MakeWeightful();
						xxx.ToggleWallCollisions(T, true);
						if (P.gameObject.TryGetComponent<LayerSerialisationBehaviour>(out LayerSerialisationBehaviour component))
						{
							P.gameObject.layer = 9;
							UnityEngine.Object.Destroy((UnityEngine.Object)component);
						}
						UnityEngine.Object.Destroy(this);
					}
				} 

				//	Clean out expired bGhosts
				if ( BGhost.Count > 0 )
				{
					Transform[] CL = BGhost.Keys.ToArray();
					for (int i = CL.Length; --i >= 0;)
					{
						if ( BGhost[CL[i]] + 2 < Time.time ) BGhost.Remove(CL[i]);
					}
				}

				yield return new WaitForSeconds(1);
			}

		}

		public void Flip()
		{
			Vector3 scale = T.localScale;
			scale.x      *= -1;
			T.localScale = scale;
		}

		public float BurstTime     = 0f;
		public bool BurstActivated = false;
		public float BurstCooldown = 0f;

		public void Activate(bool continuous=false)
		{
			if ( !P )
			{
				Hand.Drop();
				GameObject.Destroy(gameObject);
				GameObject.Destroy(this);
				return;
			}
			if (!continuous && Time.time < lastFire ) return;
			if ( continuous && Time.time < BurstCooldown) return;

			if (!BurstActivated)
			{
				BurstTime      = Time.time + xxx.rr(0.1f,2.0f);
				BurstActivated = true;
			}

			if (Time.time > BurstTime)
			{
				BurstActivated = false;
				BurstCooldown  = Time.time + xxx.rr(0.1f, 3.0f);
			}

			lastFire = Time.time + xxx.rr(0.0f, 1.0f);


			P.SendMessage(continuous ? "UseContinuous" : "Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

			Hand.NPC.Mojo.Feelings["Angry"]   *= 0.1f;
			Hand.NPC.Mojo.Feelings["Fear"]  *= 1.05f;
		}


		//public bool hasDetails      = false;
		//public bool isEnergySword   = false;
		//public bool protectFromFire = false;
		//public bool canBeDamaged    = false;
		//public bool canShoot        = false;
		//public bool canStrike       = false;
		//public bool isAutomatic     = false;
		////public float angleHold    = 95f;
		//public float  angleAim      = 0f;
		//public bool isFlashlight    = false;
		//public bool canAim          = false;
		//public bool holdToSide      = false;
		//public float size           = 0;
		//public bool canStab         = false;
		//public bool canSlice        = false;
		//public bool canFightFire    = false;
		//public bool isShiv          = false;
		//public float angleOffset    = 0f;
		//public bool isSyringe       = false;
		//public bool isMedic         = false;
		//public bool isChainSaw      = false;
		//public bool xtraLarge       = false;


		//
		// ─── GET DETAILS ────────────────────────────────────────────────────────────────
		//

		void OnDestroy()
		{
			PhysicalBehaviour pb = gameObject.GetComponentInParent<PhysicalBehaviour>();
			if (pb)
			{
				pb.MakeWeightful();
				xxx.ToggleWallCollisions(pb.transform, true);
				if (pb.gameObject.TryGetComponent<LayerSerialisationBehaviour>(out LayerSerialisationBehaviour component))
				{
					P.gameObject.layer = 9;
				}
			}
		}

		public void XDestroy()
		{

		}


	}
}
