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
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace PPnpc
{
	public class NpcGadget : MonoBehaviour
	{
		public Gadgets Gadget;
		public LightSprite[] Lights;
		public Coroutine AnimateRoutine;
		public bool BeenActivated = false;
		public PersonBehaviour TargetPerson;
		public float Timer1;
		public float Timer2;
		public float[] MiscFloats;
		public string[] MiscStrings;
		public bool[] MiscBools = {false, false, false };
		public Transform TargetTrans;
		public SpriteRenderer[] SRS;
		private AudioSource audioSource;
		private int _teamId = 0;
		private bool AlreadyAI = false;
		public Dictionary<string, Rigidbody2D>   RB  = new Dictionary<string, Rigidbody2D>();
		public Dictionary<string, LimbBehaviour> LB  = new Dictionary<string, LimbBehaviour>();
		public float Facing;
		public PhysicalBehaviour PB;
		public Coroutine xCoroutine;
		public NpcGadget Expansion;
		public int _colliderId = 0;
		public Collider2D connector;
		public float ConnectCoolDown = 0f;
		public DistanceJoint2D spring;
		public GameObject TeamSelect;
		public NpcChip ChildChip;

		public int TeamId { 
			get { return Mathf.Clamp(_teamId, 0, NpcGlobal.MaxTeamId); } 
			set { 
				if (value > NpcGlobal.MaxTeamId) value = 0; 
				_teamId = value;
				if (_teamId > 0)
				{
					string teamColor = xxx.GetTeamColor(_teamId);
				}
			} 
		}


		void Start()
		{
			audioSource              = gameObject.AddComponent<AudioSource>();
			audioSource.spread       = 35f;
			audioSource.volume       = 1f;
			audioSource.minDistance  = 15f;
			audioSource.spatialBlend = 1f;
			audioSource.dopplerLevel = 0f;
		}



		public void SetupExpansion()
		{
			MiscBools = new bool[] {false, false};
		}


		void OnJointBreak2D (Joint2D brokenJoint) {

			ConnectCoolDown = Time.time + 0.5f;

			if (Gadget == Gadgets.Expansion)
			{
				ModAPI.CreateParticleEffect("Spark", transform.position);
				MiscBools[0] = false;

				xxx.ToggleCollisions(brokenJoint.connectedBody.transform, transform, true);

			} 
		}

		public void MoveChild(Vector3 pos)
		{
			transform.position -= pos;
			if (ChildChip) ChildChip.MoveChild(pos);
		}

		private void OnTriggerEnter2D()
		{
			return;
		}

		private void OnCollisionEnter2D(Collision2D coll)
		{
			if (!coll.gameObject) return;

			if (Gadget == Gadgets.Expansion && coll.gameObject.name == "AI Microchip" && Time.time > ConnectCoolDown)
			{
				if (Mathf.Abs(coll.gameObject.transform.rotation.z - transform.rotation.z) < 0.1f &&
					Mathf.Abs(coll.gameObject.transform.position.x - transform.position.x) < 0.05f ) { 

					ModAPI.CreateParticleEffect("Spark", transform.position);

					audioSource.PlayOneShot(NpcMain.GetSound("switch", 4));

					xxx.ToggleCollisions(coll.gameObject.transform, transform,false,true);

					MiscBools[0]=true;

					ChildChip = coll.gameObject.GetComponent<NpcChip>();

					coll.gameObject.transform.position = transform.position + new Vector3(0,0.1f);
					coll.gameObject.transform.rotation = transform.rotation;

					FixedJoint2D joint                 = gameObject.AddComponent<FixedJoint2D>();
					joint.connectedBody                = coll.gameObject.GetComponent<Rigidbody2D>();
					joint.autoConfigureConnectedAnchor = true;
					joint.breakForce                   = 1f;
				}
			}
		}



		public void SetupAISerum(PersonBehaviour person)
		{
			TargetPerson = person;

			if ( TargetPerson.IsAlive() )
			{
				BeenActivated = true;

				//	Already has AI
				if (TargetPerson.TryGetComponent<NpcBehaviour>( out NpcBehaviour npc)) { 
					AlreadyAI                = true;
					npc.DisableFlip          = true;
					person.OverridePoseIndex = (int)PoseState.WrithingInPain;
				}
			}

			AnimateRoutine = StartCoroutine(IAnimateAISerum());

			Timer1 = Time.time + 4;
		}

		public IEnumerator IAnimateAISerum()
		{
			for(;;)
			{
				yield return new WaitForFixedUpdate();

				if (Time.time > Timer1 )
				{
					if ( !AlreadyAI )
					{
						ModAPI.CreateParticleEffect("Flash", TargetPerson.Limbs[0].transform.position);
						audioSource.PlayOneShot(NpcMain.GetSound("success"), 0.5f);
						NpcBehaviour npc = TargetPerson.transform.root.gameObject.GetOrAddComponent<NpcBehaviour>();
						yield return new WaitForSeconds( 3 );
						yield return new WaitForFixedUpdate();

						npc.enabled = true;
						npc.TeamId = TeamId;
						npc.AIMethod = AIMethods.Syringe;
						StopAllCoroutines();
						Destroy( this );
					} else
					{
						NpcBehaviour npcx = TargetPerson.transform.root.gameObject.GetOrAddComponent<NpcBehaviour>();
						if (npcx.AIMethod == AIMethods.AIChip) npcx.StartCoroutine(npcx.IGlitch());
					}
					break;
				}
			}
		}
	}


	public class AISyringe : SyringeBehaviour
	{
		public override string GetLiquidID() => AISerum.ID;
		public class AISerum : Liquid
		{
			public const string ID = "AI SERUM";

			public AISerum()
			{
				Color = new UnityEngine.Color(0.733f, 0.211f, 0.027f);
			}

			public override void OnEnterLimb(LimbBehaviour limb)
			{
				NpcGadget gadget = limb.Person.gameObject.GetOrAddComponent<NpcGadget>();
				gadget.SetupAISerum(limb.Person);
			}

			public override void OnUpdate(BloodContainer container) {

			}
			public override void OnEnterContainer(BloodContainer container) {
					//var glow = ModAPI.CreateLight(container.transform, Color.red, 5, 1);
			}
			public override void OnExitContainer(BloodContainer container) {}

		}
	}
}
