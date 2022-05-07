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
	public enum AttackState
	{
		ready,
		windup,
		hit,
		final,
		wait,
	}

	public class Attack
	{
		public Puppet Puppet = null;
		public AttackState state      = AttackState.ready;
		public AttackIds CurrentAttack;
		public MoveTypes CurrentMove;
		public Thing thing;

		public PersonBehaviour Enemy;
		public Vector2 EnemyTarget;
		public Vector2 TargetPos;
		public float TargetAngle;
		public Dictionary<string, Rigidbody2D> XRB2 = new Dictionary<string, Rigidbody2D>();

		private float floatemp1;
		private float floatemp2;
		private float power      = 0f;
		private float facing     = 1f;
		private int frame        = 0;

		public PuppetHand hand;

		public MoveSet combatPose;
		private MoveSet MS_Attack_1;

		public enum AttackIds
		{
			club,
			thrust,
			thrustx,
			dislodgeKick,
			dislodgeBack,
			dislodgeIchi,
		}

		public enum MoveTypes
		{
			stab,
			slash,
		}

		private Vector2 Vectemp1;
		// private Vector2 Vectemp2;




		//
		// ─── TARGET ENEMY ────────────────────────────────────────────────────────────────
		//
		public void TargetEnemy()
		{
			PersonBehaviour TheChosen = (PersonBehaviour)null;
			PersonBehaviour[] people  = GameObject.FindObjectsOfType<PersonBehaviour>();
			floatemp1                 = float.MaxValue;

			bool lastKnockedOut       = false;

			foreach (PersonBehaviour person in people)
			{
				if (!person.isActiveAndEnabled || !person.IsAlive() || person == Puppet.PBO) continue;

				Vectemp1 = person.transform.GetComponentInChildren<Rigidbody2D>().position;

				if (Puppet.FacingLeft  && Vectemp1.x > Puppet.RB2["Head"].position.x) continue;
				if (!Puppet.FacingLeft && Vectemp1.x < Puppet.RB2["Head"].position.x) continue;

				floatemp2 = (thing.R.position - Vectemp1).sqrMagnitude;

				if (floatemp2 < floatemp1)
				{
					floatemp1 = floatemp2;
					TheChosen = person;

					if (person.Consciousness < 1 || person.ShockLevel > 0.3f)
					{
						lastKnockedOut = true;
					}
				} else if(lastKnockedOut)
				{
					if (person.Consciousness >= 1 && person.ShockLevel < 0.3f)
					{
						if (floatemp2 - floatemp1 < 1f)
						{
							lastKnockedOut = false;
							floatemp1      = floatemp2;
							TheChosen      = person;
						}
					}
				}
			}

			Enemy = TheChosen;

			if (TheChosen == null) {
				EnemyTarget = Puppet.RB2["UpperBody"].position + (Vector2.right * facing * 1.1f);
				return;

			}

			string[] ValidTargets = new string[]
			{
				"Head",
				"UpperBody",
				"MiddleBody",
				"LowerBody",
				"UpperArm",
				"UpperArmFront",
				"LowerArm",
				"LowerArmFront",
			};

			string randomTarget = ValidTargets[UnityEngine.Random.Range(0,ValidTargets.Length - 1)];  


			XRB2.Clear();

			Rigidbody2D[] RBs = Enemy.transform.GetComponentsInChildren<Rigidbody2D>();


			foreach (Rigidbody2D rb in RBs)
			{
				XRB2.Add(rb.name, rb);
			}

			EnemyTarget = XRB2[ randomTarget ].position;

		}

		//
		// ─── ATTACK INIT ────────────────────────────────────────────────────────────────
		//
		public void Init(AttackIds attackId, PuppetHand hand)
		{
			Puppet.GetUp = false;
			if (state != AttackState.ready) return;

			if (!Puppet.CanAttack) return;

			hand.Thing.P.ForceContinuous = true;
			hand.Thing.AttackDamage      = 0f;
			frame                   = 0;
			power                   = 0;
			state                   = AttackState.windup;
			facing                  = Puppet.FacingLeft ? -1 : 1;

			hand.Thing.AttackedList.Clear();

			CurrentAttack = attackId;

			if (attackId == AttackIds.club)
			{
				TargetEnemy();
			}

			else if (attackId == AttackIds.thrust)
			{
				TargetEnemy();
			}
			else if (attackId == AttackIds.dislodgeIchi)
			{

			}

		}


		//
		// ─── ATTACK GO ────────────────────────────────────────────────────────────────
		//
		public void Go()
		{
			frame++;
			if (KB.ActionHeld || KB.Action2Held || KB.MouseDown || KB.Mouse2Down) power++;

			if (CurrentAttack == AttackIds.club) MeleeClub();
			if (CurrentAttack == AttackIds.thrust) MeleeThrust();
			if (CurrentAttack == AttackIds.dislodgeBack) DislodgeBack();
			if (CurrentAttack == AttackIds.dislodgeKick) DislodgeKick();
			if (CurrentAttack == AttackIds.dislodgeIchi) DislodgeIchi();
		}



		// - - - - - - - - - - - - - - - - - -
		// : Identify X
		// - - - - - - - - - - - - - - - - - -
		public void DoCombatPose(bool enableCombat=true)
		{


		}



		// - - - - - - - - - - - - - - - - - -
		//  MELEE:  CLUB
		// - - - - - - - - - - - - - - - - - -
		public void MeleeClub()
		{

		}

		// - - - - - - - - - - - - - - - - - -
		//  MELEE:  THRUST
		// - - - - - - - - - - - - - - - - - -
		public void MeleeThrust()
		{

		}


		// - - - - - - - - - - - - - - - - - -
		//  DISLODGE:  BACK
		// - - - - - - - - - - - - - - - - - -
		public void DislodgeBack()
		{

		}


		// - - - - - - - - - - - - - - - - - -
		//  DISLODGE:  Kick
		// - - - - - - - - - - - - - - - - - -
		public void DislodgeKick()
		{

		}


		// - - - - - - - - - - - - - - - - - -
		//  DISLODGE:  Ichi The Killer
		// - - - - - - - - - - - - - - - - - -
		public void DislodgeIchi()
		{

		}


	}
}
