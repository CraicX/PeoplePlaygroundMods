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
	public class Actions
	{
		public Puppet Puppet                      = (Puppet)null;
		public Crouch crouch                      = new Crouch();
		public Dive dive                          = new Dive();
		public Backflip backflip                  = new Backflip();
		public Prone prone                        = new Prone();
		public Jump jump                          = new Jump();
		public Attack attack                      = new Attack();
		public ThrowItem throwItem                = new ThrowItem();
		public Swing swing                        = new Swing();

		public bool CombatMode
		{
			get { return _CombatMode; }
			set { 
				_CombatMode = value; 
				if (value == false) Puppet.CheckMouseClickStatus();
			}
		}
		private bool _CombatMode = false;

		public Actions(Puppet puppet)
		{
			Puppet = swing.Puppet = crouch.Puppet = backflip.Puppet = dive.Puppet = prone.Puppet = jump.Puppet = attack.Puppet = throwItem.Puppet = puppet;
		}

		public void ClearActions()
		{
			crouch.state    = CrouchState.ready;
			dive.State      = DiveState.ready;
			backflip.State  = BackflipState.ready;
			prone.state     = ProneState.ready;
			jump.State      = JumpState.ready;
			attack.state    = AttackState.ready;
			swing.State     = SwingState.ready;
			throwItem.state = ThrowState.ready;
		}

		//
		// ─── UNITY FIXED UPDATE ────────────────────────────────────────────────────────────────
		//
		public void RunActions()
		{
			if (dive.State            != DiveState.ready)       dive.Go();
			else if (backflip.State   != BackflipState.ready)   backflip.Go();
			else if (jump.State       != JumpState.ready)       jump.Go();
			else if (throwItem.state  != ThrowState.ready)      throwItem.Go();
			else if (attack.state     != AttackState.ready)     attack.Go();
			else if (swing.State      != SwingState.ready)      swing.Go();
			else if (CombatMode) { Puppet.Actions.attack.DoCombatPose(); }

			if (prone.state  != ProneState.ready)  prone.Go();
			if (crouch.state != CrouchState.ready) crouch.Go();
		}

		public void Start()
		{
			swing.Puppet = attack.Puppet = crouch.Puppet = jump.Puppet = dive.Puppet = prone.Puppet = throwItem.Puppet = Puppet;
		}


		//
		// ─── ACTION TASK SYSTEM ────────────────────────────────────────────────────────────────
		//

	}
}