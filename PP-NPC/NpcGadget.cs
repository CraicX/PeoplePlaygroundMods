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
		public Transform TargetTrans;
		public SpriteRenderer[] SRS;
		public AudioClip[] AudioClips;
		public Color[] MiscColors;
		private AudioSource audioSource;
		private int _teamId = 0;
		private bool AlreadyAI = false;
		public Dictionary<string, Rigidbody2D>   RB  = new Dictionary<string, Rigidbody2D>();
		public Dictionary<string, LimbBehaviour> LB  = new Dictionary<string, LimbBehaviour>();
		public float Facing;
		public GameObject AIgo;


		public int TeamId { 
			get { return Mathf.Clamp(_teamId, 0, NpcGlobal.MaxTeamId); } 
			set { 
				if (value > NpcGlobal.MaxTeamId) value = 0; 
				_teamId = value;
				if (_teamId > 0)
				{
					string teamColor = xxx.GetTeamColor(_teamId);
					if (ColorUtility.TryParseHtmlString(teamColor + "FF", out Color color))  MiscColors[0] = color;
					if (ColorUtility.TryParseHtmlString(teamColor + "99", out Color color2)) MiscColors[1] = color2;

				}
			} 
		}

		public void Init( Gadgets _gadget )
		{
			Gadget = _gadget;

			switch ( Gadget )
			{
				case Gadgets.AIChip:
					SetupAIChip();
					break;
			}
		}

		public void Shot(Shot shot)
		{
			
			Damaged();
		}
		

		public void Damaged()
		{
			if ( Gadget == Gadgets.AIChip ) { 
				List<Collider2D> colliders = new List<Collider2D>();
				ModAPI.CreateParticleEffect("IonExplosion", transform.position);
				Physics2D.OverlapBox(transform.position, new Vector2(10,5),0f, new ContactFilter2D(), colliders);
				foreach ( Collider2D collider in colliders )
				{
					if ( collider.gameObject.TryGetComponent<PhysicalBehaviour>( out PhysicalBehaviour phys ) )
					{
						phys.SendMessage("OnEMPHit", SendMessageOptions.DontRequireReceiver);
					}
				}
				GameObject.Destroy(this.gameObject);
			}
		}

		public void Use(ActivationPropagation activation)
		{
			audioSource.PlayOneShot(AudioClips[0], 2f);
			TeamId += 1;
			string teamColor = xxx.GetTeamColor(TeamId);
			MiscStrings[0] = teamColor;
			MiscStrings[1] = teamColor;
		}


		void Awake()
		{
			audioSource              = gameObject.AddComponent<AudioSource>();
			audioSource.spread       = 35f;
			audioSource.volume       = 1f;
			audioSource.minDistance  = 15f;
			audioSource.spatialBlend = 1f;
			audioSource.dopplerLevel = 0f;
		}

		

		public void SetupAIChip()
		{
			//	Add fekn lights
			Lights = new LightSprite[2];

			Lights[0]            = ModAPI.CreateLight(transform, Color.Lerp(new Color32(0,0,50,255), new Color32(0,0,255,255),Time.fixedDeltaTime * 100));
			Lights[0].Brightness = 1.5f;
			Lights[0].Radius     = 0.225f;
			Lights[0].transform.SetParent(this.gameObject.transform);

			
			Lights[1]            = ModAPI.CreateLight(transform, Color.Lerp(new Color32(50,0,0,255), new Color32(255,0,0,255),Time.fixedDeltaTime * 100));
			
			Lights[1].Brightness = 0.5f;
			Lights[1].Radius     = 0.225f;
			Lights[1].transform.SetParent(this.gameObject.transform);

			SRS = new SpriteRenderer[2];
			
			GameObject TeamSelect = gameObject.transform.Find("TeamSelect").gameObject;
			SRS[1] = TeamSelect.GetComponent<SpriteRenderer>();



			MiscFloats = new float[]{ Time.time };
			MiscStrings = new string[]
			{
				"000000", "000000"
			};


			MiscColors = new Color[] { Color.red, Color.green, Color.blue };

			
			
			AnimateRoutine = StartCoroutine(IAnimateAIChip());

		}

		public IEnumerator IAnimateAIChip()
		{
			Vector3 pos = Vector3.zero;


			for(;;)
			{
				if (!BeenActivated) { 
					Lights[0].Radius = 0.225f * Mathf.Cos(Time.frameCount);
					Lights[0].Brightness = 1 + Mathf.Sin(Time.frameCount);
				}
				if (TeamId == 0) { 
					SRS[1].material.SetColor("_Color", Color.HSVToRGB( Mathf.PingPong((Time.time + MiscFloats[0]) * 0.1f, 1), 1, 1));

					Lights[1].Color = Color.HSVToRGB( Mathf.PingPong((Time.time + MiscFloats[0]), 1), 1, 1);
				} else
				{
					SRS[1].material.SetColor("_Color",Color.Lerp(MiscColors[0], MiscColors[1], Mathf.PingPong((Time.time + MiscFloats[0]) , 1)));
				}

				yield return new WaitForEndOfFrame();// WaitForSeconds(0.1f);
				yield return new WaitForFixedUpdate();// WaitForSeconds(0.1f);

				if ( BeenActivated )
				{
					SRS[1].sortingOrder = -1;
					//RB["LowerArm"].AddForce((RB["Head"].position - RB["LowerArm"].position ) * 150f * Time.fixedDeltaTime);
					//RB["LowerArmFront"].AddForce((RB["Head"].position - RB["LowerArmFront"].position ) * 200f * Time.fixedDeltaTime);
					//RB["UpperArm"].AddForce(new Vector2(1,1) * Facing * 150f * Time.fixedDeltaTime);
					//RB["UpperArmFront"].AddForce(new Vector2(1,1) * Facing * 170f * Time.fixedDeltaTime);
					if (!AlreadyAI && TargetPerson.IsTouchingFloor) { 
						RB["UpperBody"].AddForce(new Vector2(0,3) * 170f * Time.fixedDeltaTime);
						TargetPerson.OverridePoseIndex = (int)PoseState.Protective;
					}

					//transform.position -= Vector3.up * Time.fixedDeltaTime * 0.3f;
					transform.position = Vector2.Lerp(transform.position,TargetTrans.position, Time.fixedDeltaTime );
					Lights[0].Color = Lights[1].Color;

					if (AlreadyAI) TargetPerson.OverridePoseIndex = (int)PoseState.WrithingInPain;
					
					pos.x = Mathf.Cos(Time.frameCount);
					pos.y = Mathf.Sin(Time.frameCount);
					float dist1 = AlreadyAI ? xxx.rr(0.1f,1.0f) : 0.23f;
					float dist2 = AlreadyAI ? xxx.rr(0.1f,1.0f) : 0.33f;
					Lights[1].transform.position = transform.position + (pos * dist1); 
					Lights[1].Brightness += Time.fixedDeltaTime * 2;

					pos.y = Mathf.Cos(Time.frameCount);
					pos.x = Mathf.Sin(Time.frameCount);

					Lights[0].transform.position = transform.position + (pos * dist2); 
					Lights[0].Brightness += Time.fixedDeltaTime * 2;

					if (Time.frameCount % 50 == 0) {
						ModAPI.CreateParticleEffect("FuseBlown", transform.position);
						ModAPI.CreateParticleEffect("BloodExplosion", transform.position);
					}

					if ( Time.time > Timer2 && !AlreadyAI ) TargetPerson.OverridePoseIndex = -1;

					if ( Time.time > Timer1 )
					{

						if (Time.time > Timer1 + 2f ) { 

							yield return new WaitForEndOfFrame();

							if(gameObject.TryGetComponent<FixedJoint2D>( out FixedJoint2D joint2D)) {
								GameObject.Destroy(joint2D);
								Lights[0].enabled = Lights[1].enabled = false;
								GameObject.Destroy(Lights[0].gameObject);
								GameObject.Destroy(Lights[1].gameObject);

								gameObject.GetComponent<Renderer>().enabled = false;
								gameObject.transform.localScale = Vector3.zero;
								
								yield return new WaitForFixedUpdate();
							}
							
							if ( AlreadyAI )
							{
								audioSource.PlayOneShot(AudioClips[3], 1f);

								ExplosiveBehaviour explosion = TargetPerson.Limbs[0].gameObject.GetOrAddComponent<ExplosiveBehaviour>();
								explosion.Range              = 5f;
								explosion.ShockwaveStrength  = 1.5f;
								explosion.Delay              = xxx.rr(0.01f,2.0f);
								explosion.FragmentForce      = 2.5f;
								explosion.ShockwaveLiftForce = 1f;
								explosion.BurnPower          = 0f;
								explosion.DismemberChance    = 0.9f;
								explosion.ArmOnAwake         = true;
								explosion.Use(null);
								yield return new WaitForSeconds(3);
								break;
							}

							foreach( LimbBehaviour limbx in TargetPerson.Limbs ) limbx.Broken = false;
							
							bool alreadySet = TargetPerson.gameObject.TryGetComponent<NpcBehaviour>(out _);

							NpcBehaviour npc = TargetPerson.gameObject.GetOrAddComponent<NpcBehaviour>();
							audioSource.PlayOneShot(AudioClips[2], 1f);
							yield return new WaitForSeconds(3);
				
							yield return new WaitForFixedUpdate();
							npc.enabled  = true;
							npc.TeamId   = TeamId;
							npc.AIMethod = AIMethods.AIChip;
							if (alreadySet) npc.StartCoroutine(npc.IGlitch());
							break;
						}
					}
				}
			}
			
			StopAllCoroutines();
			Destroy(this);
			DestroyImmediate(gameObject);

		}


		


		

		private void OnCollisionEnter2D(Collision2D coll=null)
		{
			if (BeenActivated) return;
			if (coll == null || coll.collider == null) return;
			if ( coll.gameObject && coll.transform && coll.transform.root && coll.transform.root.TryGetComponent<PersonBehaviour>( out PersonBehaviour person) )
			{
				

				if ( person.IsAlive() )
				{
					if (coll.gameObject.TryGetComponent<LimbBehaviour>(out LimbBehaviour lb))
					{ 
						if ( lb.RoughClassification == LimbBehaviour.BodyPart.Head )
						{

							PhysicalBehaviour pb = gameObject.GetComponent<PhysicalBehaviour>();
							GripBehaviour[] AllGrips = UnityEngine.Object.FindObjectsOfType<GripBehaviour>();
							for ( int i = AllGrips.Length; --i >= 0; )
                            {
								if(AllGrips[i].isHolding && AllGrips[i].CurrentlyHolding == pb) AllGrips[i].DropObject();
								if ( AllGrips[i].transform.root.TryGetComponent<NpcBehaviour>( out NpcBehaviour holdingNpc ) )
                                {
									holdingNpc.CancelAction = true;
                                }
                            }
							SRS[1].sortingOrder = -1;
							
							gameObject.GetComponent<SpriteRenderer>().sortingOrder     = -1;
							gameObject.GetComponent<SpriteRenderer>().sortingLayerName = lb.PhysicalBehaviour.spriteRenderer.sortingLayerName;
							
							BeenActivated = true;
							transform.SetParent(lb.transform);
							ModAPI.CreateParticleEffect("FuseBlown", transform.position);

							//person.OverridePoseIndex = (int)PoseState.Stumbling;

							//	Already has AI
							if (person.TryGetComponent<NpcBehaviour>( out NpcBehaviour npc)) { 
								AlreadyAI = true;
								npc.DisableFlip = true;
								ModAPI.CreateParticleEffect("HugeZap", transform.position);
								person.OverridePoseIndex = (int)PoseState.WrithingInPain;
							}
							
							audioSource.PlayOneShot(AudioClips[1], 1f);
							
							
							FixedJoint2D joint = gameObject.GetOrAddComponent<FixedJoint2D>();
							
							//joint.anchor                       = lb.transform.InverseTransformPoint(transform.position);
							
							joint.autoConfigureConnectedAnchor = true;
							joint.connectedBody = lb.gameObject.GetOrAddComponent<Rigidbody2D>( );

							gameObject.SetLayer(LayerMask.NameToLayer("Debris"));
							TargetPerson = lb.Person;
							TargetTrans  = lb.transform;
							
							if (TeamId          != 0 && MiscColors[0] != null) Lights[1].Color = Lights[0].Color = MiscColors[0];
							else Lights[1].Color = Lights[0].Color = Color.white;
							Lights[0].Brightness = Lights[1].Brightness = AlreadyAI ? 5.5f : 1.5f;
							Lights[0].Radius     = Lights[1].Radius     = 0.125f;


							Timer1 = Time.time + xxx.rr(1f,3f);
							Timer2 = Time.time + xxx.rr(0.1f,1f);

							Rigidbody2D[] RBs   = TargetPerson.transform.GetComponentsInChildren<Rigidbody2D>();
							LimbBehaviour[] LBs = TargetPerson.transform.GetComponentsInChildren<LimbBehaviour>();
							RB.Clear();
							LB.Clear();
							foreach (Rigidbody2D rb in RBs) RB[rb.name] = rb;
							foreach (LimbBehaviour lbx in LBs) LB[lbx.name] =  lbx;

							//LB["LowerArm"].Broken      = true;
							//LB["LowerArmFront"].Broken = true;

							Facing = TargetPerson.transform.localScale.x < 0.0f ? -1f : 1f;
							
						}
					}
				}
			}


			else
			{
				if ( coll.contacts[0].relativeVelocity.magnitude > 12f ) Damaged();
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
						audioSource.PlayOneShot(NpcMain.AudioSuccess, 1f);
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
