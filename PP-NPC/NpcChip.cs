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
using System.Collections.Generic;
using System.Collections;

namespace PPnpc
{
	public class NpcChip : MonoBehaviour
	{
		public Chips ChipType = Chips.Engineer;
		
		public NpcGadget Expansion;

		public Coroutine AnimateRoutine;
		public Coroutine xCoroutine;
		
		public PhysicalBehaviour PB;
		[SkipSerialisation]
		public PersonBehaviour TargetPerson;
		[SkipSerialisation]
		public Transform TargetTrans;
		[SkipSerialisation]
		public DistanceJoint2D spring;
		public GameObject TeamSelect;
		
		public Collider2D connector;
		public EdgeCollider2D edge;
		public SpriteRenderer sr;
		
		private AudioSource audioSource;
		
		public LightSprite[] Lights;
		public Color[] MiscColors;
		public SpriteRenderer[] SRS;

		public float[] MiscFloats;
		public string[] MiscStrings;
		public bool[] MiscBools;
		public float[] MiscTimers    = {0f,0f,0f};
		
		[SkipSerialisation]
		public bool BeenActivated    = false;
		[SkipSerialisation]
		private bool AlreadyAI       = false;
		private int _teamId          = 0;
		public bool IsChip           = false;
		[SkipSerialisation]
		public int _colliderId       = 0;
		public float ConnectCoolDown = 0f;

		public NpcChip ChildChip     = null;
		public NpcChip ParentChip    = null;
		
		[SkipSerialisation]
		public Dictionary<string, Rigidbody2D>   RB  = new Dictionary<string, Rigidbody2D>();
		[SkipSerialisation]
		public Dictionary<string, LimbBehaviour> LB  = new Dictionary<string, LimbBehaviour>();
		
		public float Facing;
		
		[SkipSerialisation]
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

		public void Init( Chips _chip )
		{
			ChipType = _chip;

			switch ( ChipType )
			{
				case Chips.Memory:
				case Chips.Firearms:
				case Chips.Karate:
				case Chips.Melee:
				case Chips.Troll:
				case Chips.Hero:
					SRS    = new SpriteRenderer[] { gameObject.GetComponent<SpriteRenderer>() };
					PB     = gameObject.GetComponent<PhysicalBehaviour>();
					IsChip = true;
					break;
			}
		}


		public void AddBoxTop()
		{
			//if (gameObject.TryGetComponent<EdgeCollider2D>(out _)) return;
			edge                = gameObject.GetOrAddComponent<EdgeCollider2D>();
			sr                  = gameObject.GetComponent<SpriteRenderer>();
			edge.isTrigger      = true;

			float y             = (sr.bounds.max.y - sr.bounds.min.y) / 2f;
			float x             = (sr.bounds.max.x - sr.bounds.min.x) / 2f;
			
			edge.points         = new Vector2[]
			{
				new Vector2(-x, y),
				new Vector2(x, y),
			};

			edge.enabled = true;
			connector    = edge;
		}


		public void Shot(Shot shot)
		{
			Damaged();
		}


		public void Damaged()
		{
			if ( ChipType == Chips.AI ) { 
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
			else if ( ChipType == Chips.Karate )
			{
				List<Collider2D> colliders = new List<Collider2D>();
				ModAPI.CreateParticleEffect("IonExplosion", transform.position);
				Physics2D.OverlapBox(transform.position, new Vector2(10,5),0f, new ContactFilter2D(), colliders);
				foreach ( Collider2D collider in colliders )
				{
					if ((bool)(UnityEngine.Object)collider.attachedRigidbody)
					{
						Vector3 vector3 = collider.transform.position - transform.position;
						float num2      = (float)((-(double)vector3.magnitude + (double)3) * ((double)4 / (double)3));

						collider.attachedRigidbody.AddForce((Vector2)(num2 * vector3.normalized), ForceMode2D.Impulse);
					}
				}
				GameObject.Destroy(this.gameObject);

			}
			else if ( ChipType == Chips.Firearms )
			{
				List<Collider2D> colliders = new List<Collider2D>();

				ExplosionCreator.CreateFragmentationExplosion(new ExplosionCreator.ExplosionParameters()
				{
					Position                = transform.position,
					FragmentationRayCount   = 30u,
					BallisticShrapnelCount  = 5,
					CreateParticlesAndSound = true,
					FragmentForce           = 3,
					Range                   = 6
				});

				GameObject.Destroy(this.gameObject);

			}
			else if ( IsChip )
			{
				ModAPI.CreateParticleEffect( "IonExplosion", transform.position );
				GameObject.Destroy( this.gameObject );
			}
		}

		public void Use(ActivationPropagation activation)
		{
			if ( ChipType == Chips.AI )
			{
				audioSource.PlayOneShot(NpcMain.GetSound("change-team"), 2f);
				TeamId += 1;
				string teamColor = xxx.GetTeamColor(TeamId);
				MiscStrings[0] = teamColor;
				MiscStrings[1] = teamColor;
			}

		}


		void Start()
		{
			AddBoxTop();

			audioSource              = gameObject.AddComponent<AudioSource>();
			audioSource.spread       = 35f;
			audioSource.volume       = 1f;
			audioSource.minDistance  = 15f;
			audioSource.spatialBlend = 1f;
			audioSource.dopplerLevel = 0f;
		}

		void Awake()
		{

		}
		
		

		public IEnumerator IAnimateMiscChip()
		{
			ModAPI.CreateParticleEffect("Vapor", transform.position);

			NpcBehaviour xnpc = TargetPerson.gameObject.GetOrAddComponent<NpcBehaviour>();
			xnpc.DisableFlip = true;
			while ( Time.time < MiscTimers[1] )
			{
				transform.position = Vector2.Lerp(transform.position,TargetTrans.position, Time.fixedDeltaTime );
				Vector3 LastPos    = transform.position;
				Vector3 zpos       = Vector2.Lerp(transform.position,TargetTrans.position, Time.fixedDeltaTime );
				Vector3 diffpos    = LastPos - zpos;

				//transform.position = zpos;
				//if ( ChildChip && ChildChip.gameObject )
				//{
				//	ChildChip.MoveChild(diffpos);	
				//}
				yield return new WaitForFixedUpdate();

			}

			//if(gameObject.TryGetComponent<FixedJoint2D>( out FixedJoint2D joint2D)) {
			//	GameObject.Destroy(joint2D);

			//	gameObject.GetComponent<Renderer>().enabled = false;
			//	gameObject.transform.localScale = Vector3.zero;

			//	yield return new WaitForFixedUpdate();
			//}

			switch ( ChipType )
			{
				case Chips.Memory:
					xnpc.EnhancementMemory = true;
					audioSource.PlayOneShot( NpcMain.GetSound("memory"), 1f);
					break;

				case Chips.Firearms:
					xnpc.EnhancementFirearms = true;
					audioSource.PlayOneShot( NpcMain.GetSound("firearms"), 1f);
					break;

				case Chips.Karate:
					xnpc.EnhancementKarate = true;
					audioSource.PlayOneShot( NpcMain.GetSound("karate"), 1f);
					break;

				case Chips.Melee:
					xnpc.EnhancementMelee = true;
					audioSource.PlayOneShot( NpcMain.GetSound("melee"), 1f);
					break;

				case Chips.Troll:
					xnpc.EnhancementTroll = true;
					audioSource.PlayOneShot( NpcMain.GetSound("troll"), 1f);
					break;

				case Chips.Hero:
					xnpc.EnhancementHero = true;
					audioSource.PlayOneShot( NpcMain.GetSound("hero"), 1f);
					break;
			}

			for (int i = 0; ++i <=25;) { 
				ModAPI.CreateParticleEffect("Flash", transform.position);
				ModAPI.CreateParticleEffect("Flash", xnpc.Head.position);
			}

			while (audioSource.isPlaying) yield return new WaitForFixedUpdate();
			StopAllCoroutines();
			xnpc.DisableFlip = false;

			Destroy(this);
			if (gameObject) DestroyImmediate(gameObject);
		}

		public IEnumerator IAnimateAIChip()
		{
			Vector3 pos = Vector3.zero;

			yield return new WaitForFixedUpdate();

			TeamSelect.transform.position = gameObject.transform.position;
			TeamSelect.transform.rotation = gameObject.transform.rotation;
			TeamSelect.transform.SetParent(gameObject.transform);

			for(;;)
			{
				if ( ChipType == Chips.AI )
				{

					if (PB.charge > 1f ) { Damaged(); }
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

					yield return new WaitForEndOfFrame();
					yield return new WaitForFixedUpdate();

				}

				if ( BeenActivated )
				{
					SRS[1].sortingOrder = -1;
					if (!AlreadyAI && TargetPerson.IsTouchingFloor) { 
						RB["UpperBody"].AddForce(new Vector2(0,3) * 170f * Time.fixedDeltaTime);
						TargetPerson.OverridePoseIndex = (int)PoseState.Protective;
					}

					Vector3 LastPos    = transform.position;
					Vector3 zpos       = Vector2.Lerp(transform.position,TargetTrans.position, Time.fixedDeltaTime );
					Vector3 diffpos    = LastPos - zpos;

					transform.position = zpos;

					if ( ChildChip )	ChildChip.MoveChild(diffpos);

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
						ModAPI.CreateParticleEffect("Flash", transform.position);
					}

					if ( Time.time > MiscTimers[2] && !AlreadyAI ) TargetPerson.OverridePoseIndex = -1;

					if ( Time.time > MiscTimers[1] )
					{

						if (Time.time > MiscTimers[1] + 2f ) { 

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
								audioSource.PlayOneShot(NpcMain.GetSound("problem"), 1f);

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

								if ( TargetPerson.TryGetComponent<NpcBehaviour>( out NpcBehaviour xnpc ) )
								{
									NpcDeath reaper = xnpc.PBO.gameObject.GetOrAddComponent<NpcDeath>();
									
									reaper.Config(xnpc);
								
									GameObject.Destroy(xnpc.FH);
									GameObject.Destroy(xnpc.BH);
									GameObject.Destroy(xnpc);
								}

								yield return new WaitForSeconds(3);

								break;
							}

							foreach( LimbBehaviour limbx in TargetPerson.Limbs ) limbx.Broken = false;

							audioSource.PlayOneShot(NpcMain.GetSound("success"), 1f);
							
							yield return new WaitForSeconds(3);

							NpcBehaviour npc = TargetPerson.GetComponent<NpcBehaviour>();
							npc.enabled  = true;
							npc.TeamId   = TeamId;
							npc.AIMethod = AIMethods.AIChip;
							npc.DisableFlip = false;
							break;
						}
					}
				}
			}

			StopAllCoroutines();
			Destroy(this);
			DestroyImmediate(gameObject);

		}

		void OnJointBreak2D (Joint2D brokenJoint) {

			ConnectCoolDown = Time.time + 0.5f;

			ModAPI.CreateParticleEffect("Spark", transform.position);
			xxx.ToggleCollisions(brokenJoint.connectedBody.transform, transform, true);
			if (ChildChip)
			{
				ChildChip.ParentChip = null;
				ChildChip			 = null;
			}
		}

		public static bool IsAlsoChip( GameObject g )
		{
			if (g && g.TryGetComponent<NpcChip>( out NpcChip chip ) ) return (chip.IsChip);
			return false;
		}

		public void MoveChild(Vector3 pos)
		{
			transform.position -= pos;
			if (ChildChip) ChildChip.MoveChild(pos);
		}

		public IEnumerator EFixChildAlignment()
		{
			if (!ParentChip || !ParentChip.gameObject) yield break;
			yield return new WaitForFixedUpdate();
			SpriteRenderer sr2   = gameObject.GetComponent<SpriteRenderer>();
			SpriteRenderer sr	 = ParentChip.gameObject.GetComponent<SpriteRenderer>();

			float y  = (sr.bounds.max.y  - sr.bounds.min.y);
			float y2 = (sr2.bounds.max.y - sr2.bounds.min.y);

			sr2.sortingOrder = sr.sortingOrder - 1;

			transform.position = ParentChip.transform.position + new Vector3(0,(y + ((y2-y)/2))-0.022f);
			transform.rotation = ParentChip.transform.rotation;

			if (ChildChip) StartCoroutine( ChildChip.EFixChildAlignment() );
		}


		private void OnTriggerEnter2D( Collider2D coll=null )
		{
			if (coll == null) return;
			if (!coll || !coll.gameObject) return;
			if (!coll.TryGetComponent<NpcChip>(out _)) return;

			if ((ChipType == Chips.AI || IsChip) && IsAlsoChip(coll.gameObject) && coll.IsTouching(connector) && Time.time > ConnectCoolDown)
			{
				if (Mathf.Abs(transform.rotation.y) % 360 < 0.1f && 
					Mathf.Abs(coll.gameObject.transform.rotation.z - transform.rotation.z) < 0.1f &&
					Mathf.Abs(coll.gameObject.transform.position.x - transform.position.x) < 0.05f ) { 

					SpriteRenderer sr      = gameObject.GetComponent<SpriteRenderer>();
					SpriteRenderer sr2     = coll.gameObject.GetComponent<SpriteRenderer>();
					
					ChildChip              = coll.gameObject.GetComponent<NpcChip>();
					ChildChip.ParentChip   = this;

					ModAPI.CreateParticleEffect("Vapor", transform.position);

					//	Find if connected to an AI chip
					
					bool hasAI    = ChipType == Chips.AI;
					NpcChip xChip = this;

					while ( xChip.ParentChip ) { xChip = xChip.ParentChip; }
					
					if (xChip && xChip.ChipType == Chips.AI) hasAI = true;
					if (hasAI) xChip.audioSource.PlayOneShot(NpcMain.GetSound("chip_connect"), 1f);

					xxx.ToggleCollisions(coll.gameObject.transform, transform,false,false);

					float y  = (sr.bounds.max.y  - sr.bounds.min.y);
					float y2 = (sr2.bounds.max.y - sr2.bounds.min.y);

					coll.gameObject.transform.position = transform.position + new Vector3(0,(y + ((y2-y)/2))-0.022f);
					coll.gameObject.transform.rotation = transform.rotation;

					FixedJoint2D joint                 = gameObject.AddComponent<FixedJoint2D>();
					joint.connectedBody                = coll.gameObject.GetComponent<Rigidbody2D>();
					joint.autoConfigureConnectedAnchor = true;
					joint.breakForce                   = 1.5f;
					sr2.sortingOrder = sr.sortingOrder - 1;

					StartCoroutine(ChildChip.EFixChildAlignment());
				}
			}
		}

		public IEnumerator IFollowChain(Vector3 position)
		{
			if ( !ChildChip || ChildChip == null ) yield break;

			FixedJoint2D rJoint = gameObject.AddComponent<FixedJoint2D>() as FixedJoint2D;
			rJoint.transform.SetParent(gameObject.transform);
			rJoint.connectedBody = TargetPerson.Limbs[0].PhysicalBehaviour.rigidbody;
			rJoint.autoConfigureConnectedAnchor = true;
								
			if (ChildChip)	ChildChip.StartCoroutine(ChildChip.IFollowChain(position));

			Vector3 rDist = (position - transform.position);

			Vector3 dir = rDist.normalized;

			while ( dir != Vector3.zero )
			{
				transform.Translate(dir * Time.fixedDeltaTime * Time.fixedDeltaTime );
				yield return new WaitForFixedUpdate();
			}
			GameObject.DestroyImmediate(rJoint);
			GameObject.DestroyImmediate(this.gameObject);
			
		}

		private void OnTriggerEnter2D()
		{
			
		}

		private void OnCollisionEnter2D(Collision2D coll=null)
		{
			if (!coll.gameObject || coll == null || coll.collider == null || BeenActivated) return;

			if ( coll.gameObject && coll.transform && coll.transform.root && coll.transform.root.TryGetComponent<PersonBehaviour>( out PersonBehaviour person) )
			{
				if ( person.IsAlive() )
				{
					if (coll.gameObject.TryGetComponent<LimbBehaviour>(out LimbBehaviour lb))
					{ 
						if ( lb.RoughClassification == LimbBehaviour.BodyPart.Head )
						{
							TargetPerson = lb.Person;
							TargetTrans  = lb.transform;

							bool alreadySet = TargetPerson.gameObject.TryGetComponent<NpcBehaviour>(out NpcBehaviour npc);

							if (!alreadySet && ChipType != Chips.AI) return;

							PhysicalBehaviour pb     = gameObject.GetComponent<PhysicalBehaviour>();
							GripBehaviour[] AllGrips = UnityEngine.Object.FindObjectsOfType<GripBehaviour>();
							
							for ( int i = AllGrips.Length; --i >= 0; )
							{
								if(AllGrips[i].isHolding && AllGrips[i].CurrentlyHolding == pb) AllGrips[i].DropObject();
								if ( AllGrips[i].transform.root.TryGetComponent<NpcBehaviour>( out NpcBehaviour holdingNpc ) )
								{
									holdingNpc.CancelAction = true;
								}
							}

							if (ChipType == Chips.AI) {
								SRS[1].sortingOrder = -1;
								ModAPI.CreateParticleEffect("FuseBlown", transform.position);
							}

							gameObject.GetComponent<SpriteRenderer>().sortingOrder     = -1;
							gameObject.GetComponent<SpriteRenderer>().sortingLayerName = lb.PhysicalBehaviour.spriteRenderer.sortingLayerName;

							BeenActivated = true;
							transform.SetParent(lb.transform);

							//	Already has AI
							if ( ChipType == Chips.AI )
							{
								
								if (alreadySet) { 
									AlreadyAI = true;
									ModAPI.CreateParticleEffect("HugeZap", transform.position);
									person.OverridePoseIndex = (int)PoseState.WrithingInPain;
								}

								audioSource.PlayOneShot(NpcMain.GetSound("activating"), 1f);

								FixedJoint2D rJoint = gameObject.AddComponent<FixedJoint2D>() as FixedJoint2D;

								rJoint.transform.SetParent(gameObject.transform);
								rJoint.connectedBody = TargetPerson.Limbs[0].PhysicalBehaviour.rigidbody;
								rJoint.autoConfigureConnectedAnchor = true;
								
								if (ChildChip) { 
								
									ChildChip.transform.SetParent(transform);
									ChildChip.PB.MakeWeightless();
									ChildChip.IFollowChain( TargetPerson.Limbs[0].transform.position);

								}

							}

							//FixedJoint2D joint = gameObject.GetOrAddComponent<FixedJoint2D>();
							Vector3 pp = transform.position;

							//transform.position = TargetTrans.position;

							FixedJoint2D xspring = gameObject.GetOrAddComponent<FixedJoint2D>();
							//joint.anchor                       = lb.transform.InverseTransformPoint(transform.position);

							xspring.autoConfigureConnectedAnchor = true;
							xspring.connectedBody = lb.gameObject.GetOrAddComponent<Rigidbody2D>( );


							//FixedJoint.autoConfigureConnectedAnchor = false;
							//pb.rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

							gameObject.SetLayer(LayerMask.NameToLayer("Debris"));


							if ( ChipType == Chips.AI )
							{
								if (TeamId != 0 && MiscColors[0] != null) Lights[1].Color = Lights[0].Color = MiscColors[0];
								else Lights[1].Color = Lights[0].Color = Color.white;
								
								Lights[0].Brightness = Lights[1].Brightness = AlreadyAI ? 5.5f : 1.5f;
								Lights[0].Radius     = Lights[1].Radius     = 0.125f;
							}

							MiscTimers[1] = Time.time + xxx.rr(1f,3f);
							MiscTimers[2] = Time.time + xxx.rr(0.1f,1f);

							Rigidbody2D[] RBs   = TargetPerson.transform.GetComponentsInChildren<Rigidbody2D>();
							LimbBehaviour[] LBs = TargetPerson.transform.GetComponentsInChildren<LimbBehaviour>();

							RB.Clear();
							LB.Clear();

							foreach (Rigidbody2D rb in RBs) RB[rb.name]     = rb;
							foreach (LimbBehaviour lbx in LBs) LB[lbx.name] =  lbx;

							Facing = TargetPerson.transform.localScale.x < 0.0f ? -1f : 1f;

							if (alreadySet)
							{
								npc.DisableFlip = true;
								if (ChipType == Chips.AI) npc.StartCoroutine(npc.IGlitch());
								else StartCoroutine(IAnimateMiscChip());
							}  
							else {
								NpcBehaviour xnpc = TargetPerson.gameObject.GetOrAddComponent<NpcBehaviour>();
								xnpc.DisableFlip = true;
							}
						}
					}
				}
			}


			else
			{
				if ( coll.contacts[0].relativeVelocity.magnitude > 30f ) Damaged();
			}

		}




	}


}
