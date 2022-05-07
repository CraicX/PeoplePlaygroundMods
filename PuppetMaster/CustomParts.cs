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
	[SkipSerialisation]
	public class CustomParts
	{
		public List<CustomPart> Parts = new List<CustomPart>();
		public List<int> PartIDs      = new List<int>();

		public GameObject[] GameObjects               = new GameObject[0];
		public PhysicalBehaviour[] PhysicalBehaviours = new PhysicalBehaviour[0];
		public Transform[] Transforms                 = new Transform[0];


		public bool Add(GameObject g, Rigidbody2D ParentBody)
		{
			int id = g.GetInstanceID();
			if (PartIDs.Contains(id)) return false;

			if (g.name == "Brain" || ParentBody.name == "Brain")
			{
				return false;
			}

			if ( ParentBody.name.Contains( "ArmFront" ) )
			{

				if ( g.TryGetComponent<SpriteRenderer>( out SpriteRenderer SR ) )
				{
					SR.sortingOrder = (int)ParentBody.GetComponent<SpriteRenderer>()?.sortingOrder + 1;
				}
			}

			PartIDs.Add(id);

			bool hasParent  = g.transform.parent != null;

			if (!hasParent) g.transform.parent = ParentBody.transform;

			CustomPart part = new CustomPart()
			{
				G           = g,
				T           = g.transform,
				Angle       = g.transform.localRotation,
				Offset      = g.transform.localPosition,
				Parent      = ParentBody.transform,
				hasParent   = hasParent,
			};


			Parts.Add(part);

			if (!hasParent) g.transform.parent = null;

			int arPtr = PartIDs.Count;

			Array.Resize(ref GameObjects, arPtr);
			Array.Resize(ref Transforms, arPtr);
			Array.Resize(ref PhysicalBehaviours, arPtr);
			arPtr--;
			GameObjects[arPtr]                = part.G;
			Transforms[arPtr]                 = part.T;
			PhysicalBehaviours[arPtr]         = part.G.GetComponent<PhysicalBehaviour>();

			return true;
		}

		public void PreFlip()
		{
			foreach (CustomPart part in Parts)
			{
				if (part.G.TryGetComponent<FixedJoint2D>(out FixedJoint2D joint))
				{
					joint.enabled = false;
				}

				if (!part.hasParent)
				{
					part.T.parent = part.Parent;
				}
			}
		}

		public void Flip()
		{
			foreach (CustomPart part in Parts)
			{
				Vector3 scale = part.T.localScale;

				scale.x *= -1;

				part.T.rotation      = part.T.parent.rotation;
				part.T.localRotation = part.Angle;
			}
		}

		public void PostFlip()
		{
			foreach (CustomPart part in Parts)
			{
				Vector3 lopo         = part.Offset;
				lopo.x              *= part.Facing;
				part.T.localPosition = lopo;

				if (part.G.TryGetComponent<FixedJoint2D>(out FixedJoint2D joint)) joint.enabled = true;

				if (!part.hasParent) part.T.parent = null;
			}
		}


	}
}
