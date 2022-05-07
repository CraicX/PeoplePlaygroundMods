//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using UnityEngine;

namespace PuppetMaster
{
	public enum InventoryStates
	{
		Idle,
		StoringItems,
		SelectingItem,
		RunningQueue,
	}

	public enum ForcedHoldingPositions
	{
		OneHand,
		TwoHand,
		Default,
	}

	public enum PM3
	{
		Success,
		Fail,
		Error,
		Timeout,
		Busy,
	}

	public enum HoldPoses
	{
		Default,
		Handgun,
		Sword,
		Bat,
		Pipe,
	}

	public enum AimModes
	{
		Off,
		Manual,
		Always,
		Delayed,
	}

	public enum Emotes
	{
		None,
		Waving,
		Victory,
		Prone,
		Dive1,
		Dive2,
	}

	public enum VerboseLevels
	{
		Off,
		Minimal,
		Full,
	}

	public enum HoldingPositions
	{
		Auto,
		PointingDown,
		PointingForward,
		BothHands,
	}
	public enum ItemTypes
	{
		All,
		VehicleSafe,
	}

	public enum ChaseModes
	{
		Idle,
		Puppet,
		Item,
		Action,
		Vehicle,
	}

	public enum CustomModes
	{
		Off,
		GroundPound,
		SonicBoom,
		BikeThrow,
		DistantAiming,
	}

	public enum HoldStyle
	{
		Front,
		Back,
		Dual,
	}

	public enum Hands
	{
		Front,
		Back,
	}

	public enum JointTypes
	{
		FixedJoint,
		HingeJoint,
	}

	public enum FlipStates
	{
		ready,
		refresh,
		preflip,
		flip,
		postflip,
	}

	public struct HPose
	{
		public bool LowerArmBroken;
		public bool UpperArmBroken;
		public bool DropOnDives;
		public Vector3 FrontLowerPos;
		public Vector3 FrontUpperPos;
		public Vector3 BackLowerPos;
		public Vector3 BackUpperPos;
		public float FrontLowerForce;
		public float FrontUpperForce;
		public float BackLowerForce;
		public float BackUpperForce;

		public HPose( Vector2 upper, Vector2 lower )
		{
			LowerArmBroken = UpperArmBroken = DropOnDives = false;
			FrontLowerPos = BackLowerPos = lower;
			FrontUpperPos = BackUpperPos = upper;
			FrontLowerForce = FrontUpperForce = BackLowerForce = BackUpperForce = 10f;
		}


	}

	public struct HoldingQueue
	{
		public Thing thing;
		public HoldStyle holdStyle;

		public HoldingQueue( Thing _thing, HoldStyle _holdStyle )
		{
			thing     = _thing;
			holdStyle = _holdStyle;
		}
	}
	public struct LimbSnapshot
	{
		public float Health;
		public float Numbness;
		public float BreakingThreshold;
		public float Vitality;
		public float RegenerationSpeed;
		public float BaseStrength;
		public bool Broken;
		public bool Frozen;
		public ushort BruiseCount;
		public float jLimitMin;
		public float jLimitMax;
		public float FakeUprightForce;
		public PhysicalProperties Properties;
	}
	public struct RigidSnapshot
	{
		public float inertia;
		public float mass;
		public float drag;
		public float angularDrag;

		public RigidSnapshot( Rigidbody2D rbIn )
		{
			inertia     = rbIn.inertia;
			mass        = rbIn.mass;
			drag        = rbIn.drag;
			angularDrag = rbIn.angularDrag;
		}

		public void Reset( Rigidbody2D rbIn )
		{
			rbIn.inertia     = inertia;
			rbIn.mass        = mass;
			rbIn.drag        = drag;
			rbIn.angularDrag = angularDrag;
		}



	}

	public struct CustomPart
	{
		public GameObject G;
		public Transform T;
		public Transform Parent;
		public Vector3 Offset;
		public Quaternion Angle;
		public bool hasParent;
		public float Facing => T.localScale.x < 0.0f ? 1f : -1f;

	}

}
