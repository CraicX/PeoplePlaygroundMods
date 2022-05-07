//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetMaster
{

	public static class FlipUtility
	{
		//public static List<Attachment> Attachments      = new List<Attachment>();
		public static List<PhysicalBehaviour> NoFlip               = new List<PhysicalBehaviour>();
		public static List<PersonBehaviour> FlipListPeople         = new List<PersonBehaviour>();
		public static List<RandomCarTextureBehaviour> FlipListCars = new List<RandomCarTextureBehaviour>();
		public static List<PhysicalBehaviour> FlipListItems        = new List<PhysicalBehaviour>();

		public static Dictionary<int, Attachment[]> AttachmentList = new Dictionary<int, Attachment[]>();

		const string limbNamesList                      = "LowerBody,MiddleBody,UpperBody,UpperArm,UpperArmFront,Head";

		public static FlipStates FlipState              = FlipStates.ready;

		public static FlipCount FlipCounts              = new FlipCount()
		{
			Cars   = 0,
			Items  = 0,
			People = 0,
		};



		public static void FlipClear()
		{
			FlipListPeople.Clear();
			FlipListItems.Clear();
			FlipListCars.Clear();
			NoFlip.Clear();
			AttachmentList.Clear();
		}

		public static void FlipInit(List<PhysicalBehaviour> PBList)
		{
			FlipInit(PBList.ToArray());
		}

		public static void FlipInit(params PhysicalBehaviour[] PBs)
		{
			if (PBs == null || PBs.Length == 0) return;

			FlipClear();
			FlipCounts.Cars = FlipCounts.Items = FlipCounts.People = 0;

			PersonBehaviour PBO;
			RandomCarTextureBehaviour CAR;

			FlipState = FlipStates.ready;



			foreach (PhysicalBehaviour pb in PBs)
			{
				if (PBO = pb.gameObject.GetComponentInParent<PersonBehaviour>())
				{
					if (!FlipListPeople.Contains(PBO)) {
						FlipListPeople.Add(PBO);
						AttachmentList.Add(PBO.GetInstanceID(),GetAttachments(PBO));
					}
					NoFlip.Add(pb);
					continue;
				} 

				if ( pb.TryGetComponent<FixedJoint2D>( out FixedJoint2D joint ) )
				{
				   if ( PBO = joint.connectedBody.GetComponentInParent<PersonBehaviour>() )
				   {
						if (!FlipListPeople.Contains(PBO)) {
							FlipListPeople.Add(PBO);
							AttachmentList.Add(PBO.GetInstanceID(),GetAttachments(PBO));
						}
						NoFlip.Add(pb);
						continue; 
				   }
				}

				if ( CAR = pb.transform.root.GetComponentInChildren<RandomCarTextureBehaviour>() )
				{
					if (!FlipListCars.Contains(CAR)) FlipListCars.Add(CAR);
					NoFlip.Add(pb);
				}

				FlipListItems.Add(pb);
			}


			FlipState = FlipStates.preflip;
		}

		public static Attachment[] GetAttachments( PersonBehaviour PBO )
		{
			List<Attachment> attList = new List<Attachment>();

			FixedJoint2D[] joints = PBO.GetComponentsInChildren<FixedJoint2D>();

			foreach (FixedJoint2D joint in joints) {
				if ( joint.connectedBody.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour PB ) )
				{
					if (PB.beingHeldByGripper) continue;
					Attachment att = new Attachment(PB.transform, joint.attachedRigidbody, joint);
					if (!attList.Contains(att)) attList.Add(att);
				}
			}

			foreach (FixedJoint2D joint in UnityEngine.Object.FindObjectsOfType<FixedJoint2D>())
			{
				if (joint.connectedBody == null) continue;
				if (joint.gameObject == null) continue;

				if (!joint.gameObject.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour attObj)) continue;

				if (attObj.beingHeldByGripper) continue;

				if (!joint.connectedBody.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour body)) continue;
				if (body.beingHeldByGripper) continue;
				if (body.name == "Brain" || attObj.name == "Brain") continue;
				if (!body.transform.root.TryGetComponent<PersonBehaviour>(out PersonBehaviour PBATT)) continue;

				if (PBATT != PBO) continue;

				if (body.beingHeldByGripper) continue;

				Attachment att = new Attachment(joint.attachedRigidbody.transform, body.rigidbody, joint);
				if (!attList.Contains(att)) attList.Add(att);
			}

			return attList.ToArray();
		}

		public static void RunFlip()
		{
			switch ( FlipState )
			{
				case FlipStates.preflip:
					SetJoints(false);
					FlipState = FlipStates.flip;
					break;

				case FlipStates.flip:
					foreach (PersonBehaviour PBO in FlipListPeople)         if (FlipPerson(PBO))    FlipCounts.People++;
					foreach (RandomCarTextureBehaviour CAR in FlipListCars) if (FlipCar(CAR))       FlipCounts.Cars++;
					foreach (PhysicalBehaviour PB in FlipListItems)         if (FlipItem(PB))       FlipCounts.Items++;
					FlipState = FlipStates.postflip;
					break;

				case FlipStates.postflip:
					ResetAttachments();
					SetAttachmentParents(false);
					SetJoints(true);

					FlipClear();

					FlipState = FlipStates.ready;
					break;
			}

		}


		public static bool FlipPerson(PersonBehaviour PBO)
		{
			Vector3 flipScale = PBO.transform.localScale;

			flipScale.x *= -1;

			Transform headT = PBO.transform.Find("Head");
			Vector3 moveB   = headT.position;
			PhysicalBehaviour heldItem;

			foreach (LimbBehaviour limb in PBO.Limbs)
			{
				if (limb.HasJoint)
				{
					limb.BreakingThreshold *= 8;

					if (!limbNamesList.Contains(limb.name))
					{
						JointAngleLimits2D t = limb.Joint.limits;
						t.min *= -1f;
						t.max *= -1f;

						limb.Joint.limits        = t;
						limb.OriginalJointLimits = new Vector2(limb.OriginalJointLimits.x * -1f, limb.OriginalJointLimits.y * -1f);
					}
				}
			}

			PBO.transform.localScale = flipScale;

			Vector3 moveA   = PBO.transform.Find("Head").position;
			Vector2 moveDif = moveB - moveA;

			PBO.AngleOffset *= -1f;
			PBO.transform.position = new Vector2(PBO.transform.position.x + moveDif.x, PBO.transform.position.y);

			foreach (LimbBehaviour limb in PBO.Limbs) if (limb.HasJoint) limb.Broken = false;

			int itemId = 0;
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
								GB.GetComponentInParent<Rigidbody2D>().rotation + 95.0f * (PBO.transform.localScale.x < 0.0f ? 1.0f : -1.0f)
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
								GB.GetComponentInParent<Rigidbody2D>().rotation + 95.0f * (PBO.transform.localScale.x < 0.0f ? 1.0f : -1.0f)
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

			return true;
		}

		public static bool FlipItem(PhysicalBehaviour PB)
		{
			if (NoFlip.Contains(PB)) return false;

			NoFlip.Add(PB);

			Vector3 scale = PB.transform.localScale;

			scale.x *= -1;

			PB.transform.localScale = scale;

			return true;
		}

		public static bool FlipCar(RandomCarTextureBehaviour CAR)
		{
			//
			//  Obviously theres a more efficient, clever way to do this
			//  but me aint figured that out yet
			//
			//  If the car is moving, or not on a perfectly level surface,
			//  then shit gets craycray
			//
			Vector3 body      = CAR.Body.transform.position;
			Vector3 frontDoor = CAR.FrontDoor.transform.position;
			Vector3 backDoor  = CAR.BackDoor.transform.position;
			Vector3 bonnet    = CAR.Bonnet.transform.position;
			Vector3 boot      = CAR.Boot.transform.position;
			Vector3 theScale  = CAR.transform.localScale;

			theScale.x *= -1.0f;

			float flipMod = (theScale.x < 0.0f) ? -1.0f : 1.0f;

			CAR.transform.localScale = theScale;

			Vector3 bodyFlipped = CAR.Body.transform.position;

			float distance = body.x - frontDoor.x;
			if (Math.Abs(distance) < 1.0f)
			{
				theScale = CAR.FrontDoor.transform.localScale;
				theScale.x *= -1;

				CAR.FrontDoor.transform.localScale = theScale;
				CAR.FrontDoor.transform.position   = new Vector3(bodyFlipped.x - (-0.6f * flipMod), bodyFlipped.y + 0.05f);
				CAR.FrontDoor.transform.rotation   = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			}

			distance = body.x - backDoor.x;
			if (Math.Abs(distance) < 1.5f)
			{
				theScale = CAR.BackDoor.transform.localScale;
				theScale.x *= -1;

				CAR.BackDoor.transform.localScale = theScale;
				CAR.BackDoor.transform.position   = new Vector3(bodyFlipped.x - (1.05f * flipMod), bodyFlipped.y + 0.05f);
				CAR.BackDoor.transform.rotation   = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			}

			distance = body.x - bonnet.x;
			if (Math.Abs(distance) < 3.0f)
			{
				theScale = CAR.Bonnet.transform.localScale;
				theScale.x *= -1;

				CAR.Bonnet.transform.localScale = theScale;
				CAR.Bonnet.transform.position   = new Vector3(bodyFlipped.x - (-2.4f * flipMod), bodyFlipped.y + 0.1f);
				CAR.Bonnet.transform.rotation   = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			}

			distance = body.x - boot.x;
			if (Math.Abs(distance) < 3.1f)
			{
				theScale = CAR.Boot.transform.localScale;
				theScale.x *= -1;

				CAR.Boot.transform.localScale = theScale;
				CAR.Boot.transform.position   = new Vector3(bodyFlipped.x - (3.0f * flipMod), bodyFlipped.y + 0.2f);
				CAR.Boot.transform.rotation   = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			}

			foreach (Joint2D TireJoint in CAR.GetComponents<Joint2D>())
			{
				GameObject Tire = TireJoint.connectedBody.gameObject;

				if (Tire.name == "Wheel1")
				{
					Tire.transform.position = new Vector3(bodyFlipped.x - (2.0f * flipMod), Tire.transform.position.y);
				}
				else if (Tire.name == "Wheel2")
				{
					Tire.transform.position = new Vector3(bodyFlipped.x - (-2.2f * flipMod), Tire.transform.position.y);
				}

			}

			return true;
		}

		public static void SetJoints(bool enable=true)
		{
			foreach ( KeyValuePair<int, Attachment[]> pair in AttachmentList )
			{
				foreach (Attachment attachment in pair.Value)
				{
					if (!enable) attachment.Joint.enabled = false;
					else { 
						//Vector3 scale = attachment.Joint.transform.localScale;
						//scale.x *= -1;

						//attachment.Joint.transform.localScale = scale;
						NoFlip.Add(attachment.G.GetComponent<PhysicalBehaviour>());
						attachment.Joint.enabled = true;
					}

				}
			}
		}

		public static void ResetAttachments()
		{
			foreach ( KeyValuePair<int, Attachment[]> pair in AttachmentList )
			{
				foreach (Attachment A in pair.Value)
				{
					Vector3 lopo      = A.Offset;
					lopo.x           *= A.Facing;
					A.T.localPosition = lopo;
				}
			}
		}

		public static void SetAttachmentParents(bool setTrans = true)
		{
			foreach ( KeyValuePair<int, Attachment[]> pair in AttachmentList )
			{
				foreach (Attachment A in pair.Value)
				{
					if (setTrans && !A.hasParent)       A.T.parent = A.Parent;
					else if (!setTrans && !A.hasParent) A.T.parent = null;
				}
			}
		}

	}

	public struct FlipCount
	{
		public int People;
		public int Items;
		public int Cars;
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