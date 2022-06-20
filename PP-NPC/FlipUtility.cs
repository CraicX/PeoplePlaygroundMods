
using System.Collections.Generic;
using UnityEngine;

namespace Utilities
{
	// ─────────────────────────────────────────────────────────────────────────────────────
	//   :::::: Usage:     
	//	
	//   Utilities.FlipUtility.Flip( <PhysicalBehaviour> [,<PhysicalBehaviour>] [,...] );	
	//
	//   Utilities.FlipUtility.Flip( <PhysicalBehaviour[]> );
	//
	// ─────────────────────────────────────────────────────────────────────────────────────
	//
	public class FlipUtility : MonoBehaviour
	{
		public static bool HaveQueuedJobs     = false;
		
		static List<FlipJob> FlipQueue = new List<FlipJob>();
		static FlipUtility _instance;
		public static FlipUtility Instance { get { return _instance; } }
		
		public static void Flip( params PhysicalBehaviour[] ItemsToFlip )  
		{ 
			FlipQueue.Add(new FlipJob(ItemsToFlip));  
			HaveQueuedJobs = true; 
		}

		public static void ForceFlip( params PhysicalBehaviour[] ItemsToFlip )
        {
			foreach ( PhysicalBehaviour item in ItemsToFlip )
            {
				Vector3 scale = item.transform.localScale;

				scale.x *= -1;
			
				item.transform.localScale = scale;
            }
        }

		void Awake()
		{
			if (_instance != null && _instance != this) UnityEngine.Object.Destroy(this.gameObject);
			else _instance   = this;
		}

		void Update()
		{
			if (!HaveQueuedJobs) return;

			foreach ( FlipJob job in FlipQueue )
			{
				switch ( job.State )
				{
					case FlipStates.preflip:
						job.SetJoints(false);
						job.State = FlipStates.flip;
						break;

					case FlipStates.flip:
						job.FlipPersons();
						job.FlipItems();
						job.State = FlipStates.postflip;
						break;

					case FlipStates.postflip:
						job.ResetAttachments();
						job.SetAttachmentParents(false);
						job.SetJoints(true);
						job.State = FlipStates.finished;
						break;
				}
			}

			FlipQueue.RemoveAll(x => x.State == FlipStates.finished);

			HaveQueuedJobs = FlipQueue.Count > 0;

		}


		class FlipJob
		{
			public FlipStates State = FlipStates.ready;
		
			private List<Transform> Vehicles                  = new List<Transform>();
			private List<PersonBehaviour> Persons             = new List<PersonBehaviour>();
			private List<PhysicalBehaviour> Items             = new List<PhysicalBehaviour>();
			private List<PhysicalBehaviour> NoFlip            = new List<PhysicalBehaviour>();
			private Dictionary<int, Attachment[]> Attachments = new Dictionary<int, Attachment[]>();

			static string limbNamesList	= "LowerBody,MiddleBody,UpperBody,UpperArm,UpperArmFront,Head";


			public FlipJob( PhysicalBehaviour[] PBs )
			{
				PersonBehaviour PBO;
				RandomCarTextureBehaviour CAR;

				foreach (PhysicalBehaviour pb in PBs)
				{
					if (PBO = pb.gameObject.GetComponentInParent<PersonBehaviour>())
					{
						if (!Persons.Contains(PBO)) {
							Persons.Add(PBO);
							Attachments.Add(PBO.GetInstanceID(),GetAttachments(PBO));
						}

						NoFlip.Add(pb);
						continue;
					} 

					if ( pb.TryGetComponent<FixedJoint2D>( out FixedJoint2D joint ) )
					{
					   if ( PBO = joint.connectedBody.GetComponentInParent<PersonBehaviour>() )
					   {
							if (!Persons.Contains(PBO)) {
								Persons.Add(PBO);
								Attachments.Add(PBO.GetInstanceID(),GetAttachments(PBO));
							}

							if (PBs.Length > 1) NoFlip.Add(pb);
							continue; 
					   }
					}

					if ( CAR = pb.transform.root.GetComponentInChildren<RandomCarTextureBehaviour>() )
					{
						Transform VehicleRoot = CAR.transform.root;

						if (!Vehicles.Contains(VehicleRoot)) Vehicles.Add(VehicleRoot);
						NoFlip.Add(pb);
						continue;
					}

					Items.Add(pb);
					
				}

				State = FlipStates.preflip;
			}

			static Attachment[] GetAttachments( PersonBehaviour PBO )
			{
				List<Attachment> attList = new List<Attachment>();
			
				FixedJoint2D[] joints    = PBO.GetComponentsInChildren<FixedJoint2D>();

				foreach (FixedJoint2D joint in joints) 
				{
					if (!joint || !joint.connectedBody) continue;
					if ( joint.connectedBody.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour PB ) )
					{
						if (PB.beingHeldByGripper) continue;
					
						Attachment att = new Attachment(PB.transform, joint.attachedRigidbody, joint);
				
						if (!attList.Contains(att)) attList.Add(att);
					}
				}

				foreach (FixedJoint2D joint in UnityEngine.Object.FindObjectsOfType<FixedJoint2D>())
				{
					if (!joint || !joint.gameObject || !joint.connectedBody ) continue;

					if (!joint.gameObject.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour attObj)) continue;

					if (attObj.beingHeldByGripper) continue;

					if (!joint.connectedBody.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour body)) continue;
				
					if (body.beingHeldByGripper || body.name == "Brain" || attObj.name == "Brain" || body.beingHeldByGripper) continue;
				
					if (!body.transform.root.TryGetComponent<PersonBehaviour>(out PersonBehaviour PBATT)) continue;

					if (PBATT != PBO) continue;

					Attachment att = new Attachment(joint.attachedRigidbody.transform, body.rigidbody, joint);
				
					if (!attList.Contains(att)) attList.Add(att);
				}

				return attList.ToArray();
			}

			public void SetJoints(bool enable=true)
			{
				foreach ( KeyValuePair<int, Attachment[]> pair in Attachments )
				{
					foreach (Attachment attachment in pair.Value)
					{
						if (!attachment.Joint) continue;
						if (!enable && attachment.Joint) attachment.Joint.enabled = false;
						else { 
							if (!attachment.G) continue;
							if (!attachment.G.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour attObj)) continue;
							NoFlip.Add(attObj);
							attachment.Joint.enabled = true;
						}
					}
				}
			}


			public void FlipPersons() => Persons.ForEach(p => FlipPerson(p));
			public void FlipItems() => Items.ForEach(p => FlipItem(p));

			public void FlipItem(PhysicalBehaviour PB, bool force=false)
			{
				if (!force && NoFlip.Contains(PB)) return;

				NoFlip.Add(PB);

				Vector3 scale = PB.transform.localScale;

				scale.x *= -1;
			
				PB.transform.localScale = scale;
			}

			public void FlipPerson(PersonBehaviour PBO)
			{
				
				//
				//	Get Held Item's current rotation offset
				//
				Dictionary<GripBehaviour, float> rOffsets = new Dictionary<GripBehaviour, float>();
				foreach ( GripBehaviour GB in PBO.gameObject.GetComponentsInChildren<GripBehaviour>() )
				{
					if (GB.isHolding)
					{
						rOffsets[GB] = (GB.CurrentlyHolding.rigidbody.rotation - GB.GetComponentInParent<Rigidbody2D>().rotation) * (PBO.transform.localScale.x < 0.0f ? 1.0f : -1.0f);
					}
				}


				Vector3 flipScale = PBO.transform.localScale;

				flipScale.x *= -1;

				Transform headT = PBO.transform.Find("Head");
				Vector3 moveB   = headT.position;
				PhysicalBehaviour heldItem;

				foreach (LimbBehaviour limb in PBO.Limbs)
				{
					if (!limb.HasJoint) continue;
					limb.BreakingThreshold *= 8;

					if (limbNamesList.Contains(limb.name)) continue;
				
					JointAngleLimits2D t = limb.Joint.limits;
					t.min *= -1f;
					t.max *= -1f;
							
					limb.Joint.limits        = t;
					limb.OriginalJointLimits = new Vector2(limb.OriginalJointLimits.x * -1f, limb.OriginalJointLimits.y * -1f);
				}

				PBO.transform.localScale = flipScale;

				Vector3 moveA   = PBO.transform.Find("Head").position;
				Vector2 moveDif = moveB - moveA;

				PBO.AngleOffset *= -1f;
				PBO.transform.position = new Vector2(PBO.transform.position.x + moveDif.x, PBO.transform.position.y);

				foreach (LimbBehaviour limb in PBO.Limbs) if (limb.HasJoint) limb.Broken = false;

				int itemId      = 0;
				bool multiItems = false;

				foreach ( GripBehaviour GB in PBO.gameObject.GetComponentsInChildren<GripBehaviour>() )
				{
					if (GB.isHolding)
					{
						if (itemId == 0) itemId = GB.CurrentlyHolding.GetHashCode();
						else multiItems = GB.CurrentlyHolding.GetHashCode() != itemId;
					}
				}
				if ( !multiItems )
				{
					foreach ( GripBehaviour GB in PBO.gameObject.GetComponentsInChildren<GripBehaviour>() )
					{
						if ( GB.isHolding )
						{
							heldItem = GB.CurrentlyHolding;

							GB.DropObject();
							//GB.SendMessage("Use", new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);

							if ( !NoFlip.Contains( heldItem ) )
							{
								NoFlip.Add( heldItem );

								//  Flip Item
								Vector3 theScale = heldItem.transform.localScale;
								theScale.x *= -1.0f;
								heldItem.transform.localScale = theScale;

								//  Set new item rotation
								heldItem.transform.rotation = Quaternion.Euler(
									0.0f, 0.0f,
									GB.GetComponentInParent<Rigidbody2D>().rotation + rOffsets[GB] * (PBO.transform.localScale.x < 0.0f ? 1.0f : -1.0f)
								);

								//  Move item to flipped position
								Vector2 GripPoint = heldItem.GetNearestLocalHoldingPoint(GB.transform.TransformPoint(GB.GripPosition), out float distance);
								heldItem.transform.position += GB.transform.TransformPoint(GB.GripPosition) -
									heldItem.transform.TransformPoint(GripPoint);

							}

							GB.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
						}
					}
				}
				else
				{
					// DO multi item flip steps
					Dictionary<GripBehaviour, PhysicalBehaviour> heldThings = new Dictionary<GripBehaviour, PhysicalBehaviour>();

					foreach ( GripBehaviour GB in PBO.gameObject.GetComponentsInChildren<GripBehaviour>() )
					{
						if ( GB.isHolding )
						{
							heldItem = GB.CurrentlyHolding;

							heldThings.Add(GB, heldItem );

							heldItem.gameObject.SetLayer(2);
							GB.DropObject();

							if ( !NoFlip.Contains( heldItem ) )
							{
								NoFlip.Add( heldItem );

								//  Flip Item
								Vector3 theScale = heldItem.transform.localScale;
								theScale.x *= -1.0f;
								heldItem.transform.localScale = theScale;

								//  Set new item rotation
								heldItem.transform.rotation = Quaternion.Euler(
									0.0f, 0.0f,
									GB.GetComponentInParent<Rigidbody2D>().rotation + rOffsets[GB] * (PBO.transform.localScale.x < 0.0f ? 1.0f : -1.0f)
								);

								//  Move item to flipped position
								Vector2 GripPoint = heldItem.GetNearestLocalHoldingPoint(GB.transform.TransformPoint(GB.GripPosition), out float distance);
								heldItem.transform.position += GB.transform.TransformPoint(GB.GripPosition) -
									heldItem.transform.TransformPoint(GripPoint);

							}
						}
					}

					foreach ( KeyValuePair<GripBehaviour, PhysicalBehaviour> pair in heldThings )
					{
						pair.Value.gameObject.SetLayer(9);
						pair.Key.SendMessage("Use", (object)new ActivationPropagation(), SendMessageOptions.DontRequireReceiver);
					}
				}
			}

			public void ResetAttachments()
			{
				foreach ( KeyValuePair<int, Attachment[]> pair in Attachments )
				{
					foreach (Attachment A in pair.Value)
					{
						Vector3 lopo      = A.Offset;
						lopo.x           *= A.Facing;
						A.T.localPosition = lopo;
					}
				}
			}

			public void SetAttachmentParents(bool setTrans = true)
			{
				foreach ( KeyValuePair<int, Attachment[]> pair in Attachments )
					foreach (Attachment A in pair.Value) 
						if (!A.hasParent) A.T.parent = setTrans ? null : A.Parent;
			}
		}
	}


	public enum FlipStates
	{
		ready,
		preflip,
		flip,
		postflip,
		finished,
	}
		

	public struct Attachment
	{
		public Transform T;
		public Transform Parent;
		public GameObject G;
		public Vector3 Offset;
		public Quaternion Angle;
		public bool hasParent;
		public FixedJoint2D Joint;
		public float Facing => T.localScale.x < 0.0f ? 1f : -1f;

		public Attachment(Transform t, Rigidbody2D parent, FixedJoint2D _joint)
		{
			T = t;
			G = t.gameObject;
				
			hasParent = t.parent != null;
			if (!hasParent) t.SetParent(parent.transform);

			Angle    = t.localRotation;
			Offset   = t.localPosition;
			Parent   = parent.transform;
			Joint    = _joint;
		}
	}
}