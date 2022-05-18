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
		public List<Transform> NoGhost = new List<Transform>();


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
			Hand = null;
        }

		public void SetTool( PhysicalBehaviour item, NpcHand hand )
		{
			if (Hand != null && Hand != hand && Hand.NPC != hand.NPC)
            {
				if (hand.Tool == this) hand.Drop();
            }

			G          = item.gameObject;
			P          = item;
			R          = item.rigidbody;
			T          = G.transform;
			
			props = G.GetOrAddComponent<Props>();

			props.Init(item);

			hand.Tool = this;
			this.Hand  = hand;

			Array.Sort( P.HoldingPositions,(a,b) => a.x.CompareTo(b.x) );
			
			HoldingPosition = P.HoldingPositions[0];
			Array.Reverse(P.HoldingPositions);
			AltHoldingPosition = P.HoldingPositions[0];

			

			StartCoroutine(IValidate());
		}

		public bool IsFlipped => (bool)(T.localScale.x < 0.0f);


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

			float gravity = Physics2D.gravity.magnitude; 
			float distance = Mathf.Abs(target.x - ThrowStartPos.x); 
			float maxHeight  = distance / 4; 
			float startSpeedY = Mathf.Sqrt(2.0f * gravity * maxHeight); 
			float time = (startSpeedY + Mathf.Sqrt(startSpeedY *  startSpeedY
							- 2*gravity * target.y))/gravity; 
			float startSpeedX = distance / time; 


			// choose direction  
			if (target.x - ThrowStartPos.x > 0.0f){
				// right direction 
				ThrowVelocity = new Vector2(startSpeedX, startSpeedY); 
			} 
			// left direction 
			else { 
				ThrowVelocity = new Vector2(-startSpeedX, startSpeedY); 
			} 

			ThrownTool = true;


        }

		
		private void OnCollisionEnter2D(Collision2D coll=null)
		{
			bool fixNoCollide = false;
			if (coll.gameObject.layer == 11) {
				if ( coll.gameObject.name.Contains( "wall" )) xxx.ToggleCollisions(T,coll.transform,false,true);
				return;		
			}
			if (NpcGlobal.ToyNames.Contains(coll.gameObject.name)) {
				xxx.ToggleCollisions(T, coll.transform,false, true);
            }
			if ( coll.gameObject.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour phys ) )
			{
				if (phys.beingHeldByGripper)
				{
					fixNoCollide = true;
				}
			}
			
			//  Disable collisions between this held item and whoever we bumped into (if we're not fighting)
			//
			NpcBehaviour otherNpc = coll.gameObject.GetComponentInParent<NpcBehaviour>();
			if ( otherNpc )
			{
				if (!Hand.NPC.MyFights.Contains(otherNpc)) fixNoCollide = true;
			} else
			{
				PersonBehaviour person = coll.gameObject.GetComponentInParent<PersonBehaviour>();
				if ( person && !NpcBehaviour.DefenseActions.Contains(Hand.NPC.PrimaryAction))
					fixNoCollide = true;
			}

			 if (fixNoCollide) {
				if (NoGhost.Contains(coll.transform) || NoGhost.Contains(coll.transform.root)) return;
				
				xxx.ToggleCollisions(T,coll.transform,false, true);

			}



		}

		public IEnumerator IValidate()
		{
			for (; ; )
			{
				if (!Hand 
					|| !Hand.NPC 
					|| !Hand.NPC.PBO 
					|| !Hand.NPC.PBO.IsAlive() )
				{
					if ( P )
					{
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
			if (!continuous && Time.time < lastFire ) return;
			if (continuous && Time.time < BurstCooldown) return;

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
			Hand.NPC.Mojo.Feelings["Annoyed"] *= 0.05f;
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
