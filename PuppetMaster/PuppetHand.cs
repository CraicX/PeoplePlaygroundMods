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
	public class PuppetHand
	{
		private Thing _thing;
		private bool _isAiming = false;
		public PuppetGrip PG;
		public GripBehaviour GB;
		public LimbBehaviour LB;
		public Rigidbody2D RB;
		public PhysicalBehaviour PB;
		public Transform T;

		public PhysicalBehaviour uArm;
		public LimbBehaviour uArmL;

		public bool IsFiring = false;


		public HoldStyle HoldStyle;
		public bool IsHolding;

		public bool[] Flags        = { false, false, false, false };
		public Vector3[] Positions = { Vector2.zero, Vector2.zero };
		public float[] MiscValues  = {0f, 0f, 0f};
		public int[] MiscInts      = {0,0,0};

		public float BypassCheck = 0f;




		public RagdollPose.LimbPose PoseL;
		public RagdollPose.LimbPose PoseU;

		public Thing Thing
		{
			get { return _thing; }
			set { _thing = value; }
		}
		public bool IsAiming
		{
			get { return _isAiming; }
			set { 
				if (_isAiming != value) PG.ConfigHandForAiming(this, value);
				_isAiming = value;
			}
		}
		public void Drop()    => PG.Drop(this);
		public bool CanAim    => (bool)(Thing != null && Thing.canAim    && LB != null && LB.IsConsideredAlive); 
		public bool CanAttack => (bool)(Thing != null && Thing.canStrike && LB != null && LB.IsConsideredAlive); 
		public string HandId => (this == PG.FH) ? "Front" : "Back";
		public PuppetHand AltHand => PG.GetAltHand(this);

		public PuppetHand( GripBehaviour grip )
		{
			GB        = grip;
			PB        = grip.PhysicalBehaviour;
			LB        = PB.GetComponent<LimbBehaviour>();
			RB        = PB.GetComponent<Rigidbody2D>();
			T         = PB.transform;
			Thing     = (Thing)null;
			IsHolding = false;
			HoldStyle = grip.name.Contains("Front") ? HoldStyle.Front : HoldStyle.Back;
			uArm      = T.parent.Find(HoldStyle == HoldStyle.Front ? "UpperArmFront" : "UpperArm").GetComponent<PhysicalBehaviour>();
			uArmL     = uArm.GetComponent<LimbBehaviour>();
			PG        = LB.Person.GetComponent<PuppetGrip>();

			PoseL = PG.Puppet.PBO.Poses[0].AngleDictionary[LB];
			PoseU = PG.Puppet.PBO.Poses[0].AngleDictionary[uArmL];

			if ( GB.isHolding )
			{
				PhysicalBehaviour item = (PhysicalBehaviour)GB.CurrentlyHolding;
				Thing                  = item.gameObject.GetOrAddComponent<Thing>();
				IsHolding              = true;
			}
		}

		public bool Validate()
		{
			if (!GB.isHolding)
			{
				Thing     = null;
				IsHolding = false;
				return false;
			} else
			{
				if ( Thing == null || Thing.P == null || Thing.P != GB.CurrentlyHolding )
				{
					Thing = GB.CurrentlyHolding.gameObject.GetOrAddComponent<Thing>();
					Thing.GetDetails();
					Thing.SetHand(PG, this);
					IsHolding = true;
				}
				return true;
			}
		}

		public void Check()
		{
			if ( Time.time < BypassCheck ) return;
			if ( GB.isHolding )
			{
				if (!IsHolding) { 
					IsHolding = true;

					bool checkDual = false;

					if (Thing != null && Thing.P == GB.CurrentlyHolding) checkDual = true;
					else if (GB.CurrentlyHolding.TryGetComponent<Thing>(out Thing thing)) {
						Thing     = thing;
						checkDual = true;
					}

					if (checkDual)
					{
						if (AltHand.IsHolding && AltHand.Thing == Thing)
						{
							HoldStyle         = HoldStyle.Dual;
							AltHand.HoldStyle = HoldStyle.Dual;
							PG.ConfigHandForAiming(this, true);
							PG.ConfigHandForAiming(AltHand, true);
						}
						return;
					}

					Thing = GB.CurrentlyHolding.gameObject.AddComponent<Thing>();
					Thing.SetHand(PG,this);
					Util.Notify(HandId + ") Found holding: <color=orange>" + Thing.name + "</color>" , VerboseLevels.Full);
				}

			}
			else
			{
				if (IsHolding) {
					string itemName = "unknown";

					if (Thing != null) {
						itemName = Thing.name;
						Thing.Dropped();
					}

					IsHolding = false;

					Util.Notify(HandId + ") No longer holding: <color=orange>" + itemName + "</color>" , VerboseLevels.Full);
				}
			}
		}
	}
}
