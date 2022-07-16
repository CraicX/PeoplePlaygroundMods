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
		public Rigidbody2D R;
		public PhysicalBehaviour P;
		public GameObject TeamSelect, SignParimeter;
		public SpriteRenderer SignParimeterSprite;
		public Gadgets Gadget;
		public LightSprite[] Lights;
		public Coroutine AnimateRoutine;
		public bool BeenActivated = false;
		public PersonBehaviour TargetPerson;
		private bool _paused = false;
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
		public float Facing => (transform.localScale.x < 0.0f) ? 1f : -1f;
		public PhysicalBehaviour PB;
		public Coroutine xCoroutine;
		public NpcGadget Expansion;
		public int _colliderId = 0;
		public Collider2D connector;
		public float ConnectCoolDown = 0f;
		public DistanceJoint2D spring;
		public NpcChip ChildChip;
		public Vector3 LastPos;
		public BoxCollider2D box;
		public SignPost signPost = new SignPost();
		public bool SignLeft = false;
		public Vector2 Offset;
		public bool IsSign = false;
		public Gadgets[] Signs = new Gadgets[]
		{
			Gadgets.NoGunSign,
			Gadgets.NoFightSign,
			Gadgets.NoEntrySign,
			Gadgets.HealingSign,
		};

		public static List<NpcGadget> AllSigns = new List<NpcGadget>();


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
			LastPos                  = transform.position;
			Offset                   = Vector2.zero;
		}


		void Update()
		{
			if ( _paused != Global.main.Paused && Signs.Contains(this.Gadget))
			{
				_paused = Global.main.Paused;
				float signFacing = SignLeft ? -1f : 1f;
				SignParimeterSprite.transform.localPosition = new Vector3(5f * signFacing,0f);

				if ( !_paused )
				{
					SignParimeter.SetActive(false);
					foreach (Collider2D c2d in GetComponents<Collider2D>()) {
						ModAPI.Notify(c2d.name);
						GameObject.DestroyImmediate((Object) c2d);
					}

					box	             = gameObject.AddComponent<BoxCollider2D>();
					box.size         = new Vector2( 10f, 3f );
					box.offset       = new Vector2 (5f * signFacing,0f);
					box.isTrigger    = true;
					box.enabled      = true;
				}
				else
				{
					gameObject.FixColliders();
					SignParimeter.SetActive(true);

				}
			}
		}

		public void SetupSign()
		{
			IsSign			  = true;
			R                 = transform.root.GetComponent<Rigidbody2D>();
			P                 = transform.root.GetComponent<PhysicalBehaviour>();
			_paused           = !Global.main.Paused;
			R.bodyType        = RigidbodyType2D.Kinematic;
			signPost.SignType = Gadget;

			Renderer rr = GetComponent<Renderer>();

			if (rr != null) { 

				Bounds bounds = rr.bounds;

				foreach (Collider2D c2d in GetComponents<Collider2D>()) GameObject.Destroy((Object) c2d);

				float signFacing = SignLeft ? 1f : -1f;
				
				box           = gameObject.AddComponent<BoxCollider2D>();
				box.size      = new Vector2( 10f, 3f );
				box.offset    = new Vector2 (5f * signFacing,0f);
				box.isTrigger = true;
				box.enabled   = true;

				SignParimeter = new GameObject("SignParimeter", typeof(SpriteRenderer));
				SignParimeter.transform.SetParent(transform, false);
				SignParimeter.transform.position = transform.position;
				SignParimeter.transform.rotation = transform.rotation;
				SignParimeterSprite        = SignParimeter.GetComponent<SpriteRenderer>();
				SignParimeterSprite.sprite = NpcMain.BlankBlock;
				
				float width  = SignParimeterSprite.sprite.bounds.size.x;
				float height = SignParimeterSprite.sprite.bounds.size.y;

				SignParimeterSprite.transform.localScale = new Vector3(10 / width, 3 / height);
				SignParimeterSprite.transform.localPosition = new Vector3(5 * signFacing,0f);
				SignParimeterSprite.enabled = true;
				SignParimeterSprite.sortingLayerName     = "Background";
                SignParimeterSprite.sortingOrder         = -100;
				ColorUtility.TryParseHtmlString("#EB710055", out Color c);
				SignParimeterSprite.color = c;
				SignParimeter.SetActive(false);

				AllSigns.Add(this);
			}
			
		}

		void OnDestroy()
		{
			AllSigns.Remove(this);
		}

		public void SetupExpansion() => SetupExpansion(Vector2.zero);
		public void SetupExpansion(Vector2 _offset)
		{
			MiscBools = new bool[] {false, false};
			Offset    = _offset;

			//	Setup edge colliders
			Renderer r = GetComponent<Renderer>();
			if (r != null) { 
			
				Bounds bounds = r.bounds;
				foreach (Collider2D c2d in GetComponents<Collider2D>()) c2d.enabled = false;

				BoxCollider2D box = gameObject.GetOrAddComponent<BoxCollider2D>();
				box.size = new Vector2( bounds.size.y, bounds.size.y );
				//box.offset = Offset;
				box.enabled = true;

			}

			//gameObject.FixColliders();
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

		private void OnTriggerEnter2D( Collider2D coll=null )
		{

			if (!IsSign || coll == null) return;
			if (!coll || !coll.gameObject) return;

			if ( coll.gameObject.transform.root.TryGetComponent<NpcBehaviour>( out NpcBehaviour npc ) )
			{
				if (Gadget == Gadgets.NoFightSign) npc.NoFight		   = true;
				else if (Gadget == Gadgets.NoGunSign) npc.NoShoot	   = true;
				else if (Gadget == Gadgets.NoEntrySign) npc.NoEntry    = npc.NoFight = true;
				else if (Gadget == Gadgets.HealingSign) {
					npc.InHealZone = true;
					npc.RunLimbs(npc.LimbRegen, 5000f);
				}
			}
		}

		private void OnCollisionEnter2D(Collision2D coll)
		{
			if (!coll.gameObject) return;
			if (Gadget == Gadgets.Expansion && coll.gameObject.name == "AI Microchip" && Time.time > ConnectCoolDown)
			{
				if (Mathf.Abs(coll.gameObject.transform.rotation.z - transform.rotation.z) < 0.1f &&
					Mathf.Abs(coll.gameObject.transform.position.x - transform.position.x) < 0.05f ) { 

					ModAPI.CreateParticleEffect("Spark", transform.position);
					audioSource.enabled      = true;
					audioSource.PlayOneShot(NpcMain.GetSound("switch", 4));

					xxx.ToggleCollisions(coll.gameObject.transform, transform,false,true);

					MiscBools[0]=true;

					ChildChip = coll.gameObject.GetComponent<NpcChip>();

					//coll.gameObject.transform.position = coll.collider.transform.position + new Vector3(0,0.1f);
					coll.gameObject.transform.position = new Vector2(transform.position.x, coll.collider.transform.position.y -0.1f);
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
						audioSource.enabled      = true;
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
