//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System.Collections.Generic;
using UnityEngine;

namespace PuppetMaster
{

	public class PuppetAim : MonoBehaviour
	{
		public Puppet Puppet;
		public PuppetGrip PG;

		public AimModes AimMode => PG.AimMode;

		public PuppetHand FH => PG.FH;
		public PuppetHand BH => PG.BH;

		public float Facing => Puppet.Facing;

		public bool IsPaused = false;

		private Vector2 M2Pos;
		private Vector3 M3Pos;

		private Vector2 V2Pos;
		private Vector3 V3Pos;
		private Vector3 V3Vel;

		private float fAngle;
		private float fDist;
		private float fNum;

		private bool fAim;
		private bool bAim;

		private HingeJoint2D hinge;


		private void Start()
		{
			if( !TryGetComponent<PuppetGrip>(out PuppetGrip PG) ) Util.DestroyNow(this);
			Puppet = PG.Puppet;

		}


		private void FixedUpdate()
		{
			if ( AimMode == AimModes.Manual )
			{
				V2Pos = Puppet.LB["UpperBody"].transform.position * Facing;
				if (Global.main.MousePosition.x * Facing > V2Pos.x -0.5f ) return;
				if (Mathf.Abs(Vector2.Distance(Global.main.MousePosition * Facing, V2Pos )) < 2.5f) return;

				fAim = bAim = false;

				// - - - - -
				// FRONT HAND
				//
				if (KB.Shift && FH.CanAim)
				{
					fAim = true;

					if (FH.HoldStyle == HoldStyle.Dual) bAim = true;
				} 


				// - - - - -
				// BACK HAND
				//
				if (KB.Control && BH.CanAim)
				{
					bAim = true;

					if (BH.HoldStyle == HoldStyle.Dual) fAim = true;
				} 

				BH.IsAiming = bAim;
				FH.IsAiming = fAim;
				PG.IsAiming = bAim || fAim;

				if (KB.Shift && fAim)   PointThingAtMouse(FH);
				if (KB.Control && bAim) PointThingAtMouse(BH);

			}
		}





		private void PauseAiming()
		{
			if (IsPaused) return;

			Puppet.PG.IsAiming = false;
			IsPaused           = true;

		}


		public void PointThingAtMouse( PuppetHand pHand )
		{
			IsPaused = false;


			if ( Time.frameCount % 100 == 0  && FH.IsAiming == BH.IsAiming && FH.Thing != BH.Thing)
			{
				// Modify aiming offset wee bit so they dont shoot exactly same spot when yielding 2 weapons
				FH.Positions[0] = Random.insideUnitCircle * 0.5f;
				BH.Positions[0] = Random.insideUnitCircle * 0.5f;
			}

			fDist = PG.DualWield ? -0.2f : (Facing < 0 ? 0.5f : -0.1f);


			M3Pos    = ((Global.main.MousePosition + pHand.Positions[0]) - pHand.Thing.tr.position) * -Facing;

			M3Pos.y += fDist;

			fAngle   = Mathf.Atan2(M3Pos.y, M3Pos.x) * Mathf.Rad2Deg;

			if (pHand.HoldStyle != HoldStyle.Dual) pHand.RB.AddForce(M3Pos.normalized * -Facing * Time.fixedDeltaTime * Puppet.TotalWeight * 35f);

			fNum     = Mathf.DeltaAngle(pHand.Thing.tr.eulerAngles.z, fAngle) * Puppet.TotalWeight * Time.fixedDeltaTime * 15f;


			if (Time.frameCount % 1000 == 0) fDist = float.MinValue;

			if ( !float.IsNaN( fNum ) ) pHand.Thing.R.AddTorque(Mathf.Clamp(fNum, -15f, 15f));
		}


		public void PointArmAtMouse( PuppetHand pHand )
		{
			if (Time.frameCount % 100 == 0 && FH.IsAiming == BH.IsAiming )
			{
				// Modify aiming offset wee bit so they dont shoot exactly same spot when yielding 2 weapons
				pHand.Positions[0] = Random.insideUnitCircle * 0.5f;
			}

			if (Global.main.MousePosition.x * Facing > (pHand.T.position.x * Facing))
			{
				PauseAiming();
				return;
			}

			IsPaused = false;

			V3Pos = (pHand.T.position - (Global.main.MousePosition + pHand.Positions[0]));

			fDist = V3Pos.magnitude;

			//  Prevent spasticated behavior
			if( Mathf.Abs(fDist) < 1.0f ) { 
				PauseAiming();
				return;
			}

			if (Puppet.FacingLeft) V3Vel = new Vector3(0f, 0f, (Mathf.Atan2(V3Pos.y, V3Pos.x) * Mathf.Rad2Deg) - (85.5f + (16.5f * Facing)));
			else V3Vel                   = new Vector3(0f, 0f, (Mathf.Atan2(V3Pos.y, V3Pos.x) * Mathf.Rad2Deg) - (85.5f + (10.5f * Facing)));

			pHand.RB.MoveRotation(Quaternion.Euler(V3Vel));
		}


		public void Dispose()
		{
			Util.Destroy(this);
		}
	}

}
