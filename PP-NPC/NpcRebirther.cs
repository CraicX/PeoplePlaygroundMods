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
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

namespace PPnpc
{
	public class NpcRebirther : MonoBehaviour
	{
		public PhysicalBehaviour PB;
		public SpriteRenderer[] SRS;
		public bool _rebirtherActive = false;
		public int _teamId = 0;
		public Color[] MiscColors;
		public AudioSource audioSource;
		public AudioSource audioSource2;
		public float[] MiscFloats;
		public string[] MiscStrings;
		public bool[] MiscBools;
		public Coroutine xCoroutine, AnimateRoutine;
		public NpcGadget Expansion = null;
		public GameObject TeamSelect;
		public int SoulsRebirthed = 0;
		public int TimesShot      = 0;


		public Dictionary<int, int> Births  = new Dictionary<int, int>();

		public bool bigPowerOn = false;
		public TextMeshPro TV;
		public GameObject TVScreen;
		public GameObject bs;
		public SpriteRenderer bsr;
		public SpriteRenderer SR, TVSR;
		public RectTransform rectImage;

		void Start()
		{
			//PB          = gameObject.GetComponent<PhysicalBehaviour>();
			MiscBools   = new bool[] { false, false };
			MiscStrings = new string[] {"000000", "000000"};
			MiscColors  = new Color[] { Color.red, Color.green, Color.blue };
			MiscFloats  = new float[]{ Time.time, 0.01f };

			audioSource              = gameObject.AddComponent<AudioSource>();
			audioSource.spread       = 35f;
			audioSource.volume       = 1f;
			audioSource.minDistance  = 15f;
			audioSource.spatialBlend = 1f;
			audioSource.dopplerLevel = 0f;

			audioSource2              = gameObject.AddComponent<AudioSource>();
			audioSource2.spread       = 35f;
			audioSource2.volume       = 1f;
			audioSource2.minDistance  = 15f;
			audioSource2.spatialBlend = 1f;
			audioSource2.dopplerLevel = 0f;

			SR                           = gameObject.GetComponent<SpriteRenderer>();
			TVScreen                     = new GameObject("TVScreen", typeof(TextMeshPro));
			rectImage = TVScreen.GetComponent<RectTransform>();
			rectImage.localScale = Vector3.one;
			rectImage.transform.SetParent( transform, false );
			rectImage.transform.position = transform.position;
			rectImage.transform.rotation = transform.rotation;

			TV                         = TVScreen.gameObject.GetOrAddComponent<TextMeshPro>();
			TV.transform.SetParent(TVScreen.transform, false);
			TV.transform.position      = transform.position;
			TV.transform.rotation      = transform.rotation;
			TV.transform.localScale    = new Vector3(0.4f, 0.4f);
			TV.transform.localPosition = new Vector3(0f, 0.5f);
			TV.fontSize                = 2f;
			TV.horizontalAlignment     = HorizontalAlignmentOptions.Center;
			TV.verticalAlignment       = VerticalAlignmentOptions.Middle;
			TV.color                   = Color.blue;
			TV.text                    = "";

			TeamSelect.transform.SetParent(transform, false);
			TeamSelect.transform.localPosition = new Vector2(0.022f,0.06f);


		}

		


		public bool RebirtherActive
		{
			get { return _rebirtherActive; }
			set { 
				_rebirtherActive = value; 
			}
		}


		public void Use(ActivationPropagation activation)
		{
			if (xCoroutine != null) StopCoroutine(xCoroutine);

			if (PB.charge < 1f) xCoroutine = StartCoroutine(IMessage("<color=#D72F32><size=80%>No Power</size>", 6, 0.5f ));
			else
			{
				audioSource.PlayOneShot(NpcMain.GetSound("change-team"), 1f);
				TeamId += 1;
				string teamColor = xxx.GetTeamColor(TeamId);
				MiscStrings[0] = teamColor;
				MiscStrings[1] = teamColor;
			}
		}

		public IEnumerator IGiveRebirth( NpcBehaviour npc )
		{
			Vector3 dist;

			npc.RB["UpperBody"].velocity *= 0f;

			xxx.ToggleCollisions(npc.Head, transform);

			yield return new WaitForFixedUpdate();

			npc.RunLimbs(npc.LimbRecover);
			npc.RunLimbs(npc.LimbHeal);

			npc.LB["Foot"].PhysicalBehaviour.MakeWeightful();

			float timer = Time.time + 1.5f;

			while ( Time.time < timer )
			{
				dist = (transform.position - npc.Head.position);
				npc.RB["Head"].AddForce(dist.normalized * 10f * Time.fixedDeltaTime);
				npc.RB["UpperBody"].velocity *= 0.01f;
				yield return new WaitForFixedUpdate();
			}
			StartCoroutine(ISmokin());

			if( npc.RebirthLight ) {
				npc.RebirthLight.Brightness = 0;
				GameObject.Destroy(npc.RebirthLight.gameObject);
			}

			if (!Births.ContainsKey(npc.NpcId)) Births[npc.NpcId] = 0;
			Births[npc.NpcId] += 1;

			if (npc.Facing != npc.LimbFacing)
			{
				npc.Flip();
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
			}
			npc.ResetLimbs();
			if ( Births[npc.NpcId] > 2 && xxx.rr(1,3) == 2)
			{
				StartCoroutine(IMessage("Welcome back,\n" + npc.Config.Name + "!", 20));
			}
			else StartCoroutine(IMessage("You have been\nRebirthed!", 20));

			audioSource.enabled      = true;
			audioSource.PlayOneShot(NpcMain.GetSound("rebirth"), 1f);

			for (int i = 1; ++i < 50;) 
			{
				dist = (transform.position - npc.Head.position);
				npc.RB["UpperBody"].velocity *= 0f;
				npc.RunLimbs(npc.LimbAlphaDelta,0.05f);
				yield return new WaitForFixedUpdate();
				yield return new WaitForSeconds(0.05f);

				if (Time.frameCount % 1 == 0)
				{
					LimbBehaviour limb = npc.PBO.Limbs.PickRandom();
					ModAPI.CreateParticleEffect("Flash", limb.transform.position);
				}
			}

			
			npc.RunLimbs(npc.LimbGhost, false);
			npc.RunLimbs(npc.LimbAlphaFull,true);
			npc.RunLimbs(npc.LimbImmune, false);


			npc.Reborn();

			SoulsRebirthed++;


			if ( Births[npc.NpcId] > 2 && xxx.rr(1,3) == 2)
			{
				StartCoroutine(IMessage("You have died\n" + Births[npc.NpcId] + " times!"));
			}
			else StartCoroutine(IMessage("Better luck\nthis time!"));

			//	Determine rebirther upgrades
			NpcChip currentChip = Expansion.ChildChip;
			while ( currentChip )
			{
				audioSource.enabled      = true;
				switch ( currentChip.ChipType)
				{
					case Chips.Memory:
						if (!npc.EnhancementMemory) { 
							npc.EnhancementMemory = true;
							yield return new WaitForSeconds(0.5f);
							for (int i = 0; ++i <=55;) 
								ModAPI.CreateParticleEffect("Flash", npc.Head.position);
							audioSource.PlayOneShot( NpcMain.GetSound("memory"), 1f);
						}
						break;

					case Chips.Karate:
						if (!npc.EnhancementKarate) { 

							npc.EnhancementKarate = true;
							yield return new WaitForSeconds(0.5f);
							for (int i = 0; ++i <=55;) 
								ModAPI.CreateParticleEffect("Flash", npc.Head.position);
							audioSource.PlayOneShot( NpcMain.GetSound("karate"), 1f);
						}
						break;

					case Chips.Firearms:
						if (!npc.EnhancementFirearms) { 

							npc.EnhancementFirearms = true;
							yield return new WaitForSeconds(0.5f);
							for (int i = 0; ++i <=55;) 
								ModAPI.CreateParticleEffect("Flash", npc.Head.position);
							audioSource.PlayOneShot( NpcMain.GetSound("firearms"), 1f);
						}
						break;

					case Chips.Melee:
						if (!npc.EnhancementMelee) { 
							npc.EnhancementMelee = true;
							yield return new WaitForSeconds(0.5f);
							for (int i = 0; ++i <=55;) 
								ModAPI.CreateParticleEffect("Flash", npc.Head.position);
							audioSource.PlayOneShot( NpcMain.GetSound("melee"), 1f);
						}
						break;

					case Chips.Troll:
						if (!npc.EnhancementTroll) { 
							npc.EnhancementTroll = true;
							yield return new WaitForSeconds(0.5f);
							for (int i = 0; ++i <=55;) 
								ModAPI.CreateParticleEffect("Flash", npc.Head.position);
							audioSource.PlayOneShot( NpcMain.GetSound("troll"), 1f);
						}
						break;

					case Chips.Hero:
						if (!npc.EnhancementHero) { 
							npc.EnhancementHero= true;
							yield return new WaitForSeconds(0.5f);
							for (int i = 0; ++i <=55;) 
								ModAPI.CreateParticleEffect("Flash", npc.Head.position);
							audioSource.PlayOneShot( NpcMain.GetSound("hero"), 1f);
						}
						break;
				}

				currentChip = currentChip.ChildChip;
			}

		}


		public IEnumerator IMessage(string message, float seconds = 3f, float flashSpeed = 0f)
		{
			if ( flashSpeed > 0f )
            {
				seconds += Time.time;
				while ( Time.time < seconds )
                {
					TV.text = message;
					yield return new WaitForSeconds( flashSpeed );
					TV.text = "";
					yield return new WaitForSeconds( flashSpeed );
                }
            }
			else
            {
				TV.text = message;
			
				yield return new WaitForSeconds(seconds);
            }
			

			if (TV.text == message) TV.text = "";
		}

		

		
		public IEnumerator IImage( Sprite sprite, float seconds )
		{
			GameObject imgScreen			  = new GameObject() as GameObject;
			SpriteRenderer xsr				  = imgScreen.AddComponent<SpriteRenderer>();
			xsr.sprite						  = sprite;

			imgScreen.transform.SetParent(transform);
			imgScreen.transform.position      = transform.position;
			imgScreen.transform.rotation      = transform.rotation;
			imgScreen.transform.localPosition = new Vector2(0,0.5f);

			yield return new WaitForSeconds(seconds);
			GameObject.Destroy(imgScreen);
		}


		public IEnumerator ISmokin()
        {

			GameObject ParticlePrefab = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(
				"Prefabs/SmokeParticle"),
				transform.position + new Vector3(xxx.rr(-0.1f,0.1f),2f), transform.rotation, transform) as GameObject;

			ParticleSystem pSys = ParticlePrefab.GetComponent<ParticleSystem>();
			ParticlePrefab.AddComponent<Optout>();

			ParticleSystem.MainModule main   = pSys.main;
			ParticleSystem.ShapeModule shape = pSys.shape;

			shape.radiusSpeedMultiplier           = 50f;
			shape.radiusThickness                 = 0.1f;
			shape.spriteRenderer                  = SR;
			shape.radiusSpread                    = 5f;
			shape.radiusSpeed                     = 5f;
			shape.radiusMode                      = ParticleSystemShapeMultiModeValue.BurstSpread;//.Random;
			shape.shapeType                       = ParticleSystemShapeType.Hemisphere;
			shape.radius                          = 5;
			main.startColor                       = new Color(0, 0, 0);
			
			float timer = Time.time + xxx.rr(2f,3f);

			while ( Time.time < timer )
            {
				yield return new WaitForSeconds(xxx.rr(0.1f,0.2f));

				ParticleSystem.EmitParams pse         = new ParticleSystem.EmitParams()
				{
					startColor    = new Color(0.5f, xxx.rr(0.5f,1f), 0.5f),
					startSize     = 1f,
					startLifetime = 3f,
					position      = transform.position,
				};

				pse.position -= new Vector3(xxx.rr(-0.1f,0.1f), 1f);

				float c, x, y;
				for (int i = 0; ++i < 10;)
				{
					c                 = UnityEngine.Random.Range(0.1f,0.5f);
					pse.startColor    = new Color(xxx.rr(0.5f,1f), 0.1f, 0.1f, 0.5f);
					pse.startLifetime = UnityEngine.Random.Range(2,8);
					pse.startSize     = 3 ;// * 0.1f;
					x                 = UnityEngine.Random.Range(-1f,1f);
					y                 = UnityEngine.Random.Range(0.1f, 0.5f);
					pse.velocity      = new Vector2(x,y);
					pSys.Emit(pse, 2);

				}
			}
        }


		

		public IEnumerator IAnimateRebirther()
		{
			

			yield return new WaitForSeconds(2f);

			PB = gameObject.GetComponent<PhysicalBehaviour>();

			for(;;)
			{
				while ( PB.charge < 1f )
				{
					if ( MiscBools[0] == true )
					{
						if (audioSource.isPlaying) audioSource.Stop();
						yield return new WaitForEndOfFrame();
						audioSource.enabled      = true;
						audioSource.PlayOneShot(NpcMain.GetSound("rb_off"), 1.2f);
					}
					SRS[1].color = new Color(0.0f,0.0f,0.0f,0.0f);
					MiscBools[0] = MiscBools[1] = RebirtherActive = false;
					yield return new WaitForSeconds(2f);
				}

				if ( !MiscBools[0] )
				{
					RebirtherActive = false;
					if (xCoroutine != null) StopCoroutine(xCoroutine);
					StartCoroutine(IMessage("Booting up...", 4));
					TV.color = Color.cyan;
					yield return new WaitForSeconds(0.2f);
					audioSource.enabled      = true;
					audioSource.PlayOneShot(NpcMain.GetSound("rb_on", 3), 0.6f);
					

					yield return new WaitForSeconds(2f);
					TV.text = "";

					if ( !Expansion || !Expansion.MiscBools[0] )
					{
						if (audioSource.isPlaying) audioSource.Stop();
						yield return new WaitForEndOfFrame();
						ClearMsg();
						TV.text = "";
						audioSource.enabled      = true;
						audioSource.PlayOneShot(NpcMain.GetSound("rb_off"), 1.2f);
						MiscBools[0]    = MiscBools[1] = false;
						bs	            = new GameObject() as GameObject;
						bsr				= bs.AddComponent<SpriteRenderer>();
						bsr.sprite      = NpcMain.BlueScreenOfDeath;


						bs.transform.SetParent(transform);
						bs.transform.position      = transform.position;
						bs.transform.rotation      = transform.rotation;
						bs.transform.localPosition = new Vector2(0,0.5f);


						while ( PB.charge > 1f )
						{
							yield return new WaitForFixedUpdate();
						}

						GameObject.Destroy(bs);
					} else
					{
						while (audioSource.isPlaying) yield return new WaitForEndOfFrame();
						if ( !bigPowerOn )
						{
							bigPowerOn = true;
							audioSource.PlayOneShot(NpcMain.GetSound("rb_on", 2), 0.4f);
						}
						else
						{
							audioSource.PlayOneShot(NpcMain.GetSound("rb_on", 1), 0.6f);
						}

						if ( !MiscBools[0] ) { 
							SRS[1].color  = new Color(1.0f,1.0f,1.0f,0.1f);
							MiscFloats[1] = 0.01f;
				
							StartCoroutine(IMessage("<color=yellow><b><size=170%>Rebirther</size></b><br><size=90%><color=white>\"Giving dumb AI a 2nd <br>chance since 2022!\"</size>",10));
						}
						MiscBools[0]  = true;

					}
				}

				if ( MiscBools[0] )
				{
					if ( !Expansion || !Expansion.MiscBools[0] )
					{
						if (audioSource.isPlaying) audioSource.Stop();
						yield return new WaitForEndOfFrame();
						ClearMsg();
						TV.text = "";
						audioSource.PlayOneShot(NpcMain.GetSound("rb_off"), 1.2f);
						RebirtherActive = false;
						MiscBools[0]    = MiscBools[1] = false;
						bs	            = new GameObject() as GameObject;
						bsr				= bs.AddComponent<SpriteRenderer>();
						bsr.sprite      = NpcMain.BlueScreenOfDeath;
						bs.transform.SetParent(transform);
						bs.transform.position      = transform.position;
						bs.transform.rotation      = transform.rotation;
						bs.transform.localPosition = new Vector2(0,0.5f);
						SRS[1].color = new Color(0.0f,0.0f,0.0f,1f);

						while ( PB.charge > 1f )
						{
							yield return new WaitForFixedUpdate();
						}

						GameObject.Destroy(bs);
					} else
					{
						RebirtherActive = true;
						if (TeamId == 0) { 
							if ( Time.frameCount % 10 == 0 )
							{
								MiscFloats[1] = Mathf.Clamp(PB.charge * 0.5f, 25.1f, 100) * 0.01f;
							}
							SRS[1].color = Color.HSVToRGB( Mathf.PingPong((Time.time + MiscFloats[0]) * 0.1f, 1), MiscFloats[1] * 2, MiscFloats[1]);

						} else
						{
							SRS[1].color = Color.Lerp(MiscColors[0], MiscColors[1], Mathf.PingPong((Time.time + MiscFloats[0]) , 1));
						}
					}
				}

				yield return new WaitForFixedUpdate();
			}
		}

		public void ClearMsg()
        {
			TV.text = "";
        }

		public void Shot()
		{
			++TimesShot;
			if (RebirtherActive) StartCoroutine(IImage(NpcMain.RedScreen,2));
		}

		public int TeamId { 
			get { 
				return Mathf.Clamp(_teamId, 0, NpcGlobal.MaxTeamId); 
				} 
			set { 
				if (value > NpcGlobal.MaxTeamId) value = 0; 
				_teamId = value;
				if (_teamId > 0)
				{
					string teamColor = xxx.GetTeamColor(_teamId);


					string hexalpha = Mathf.RoundToInt(Mathf.Clamp(PB.charge * 2, 25, 255)).ToString("X2");
					if (ColorUtility.TryParseHtmlString(teamColor + hexalpha, out Color color))  MiscColors[0] = color;
					hexalpha = Mathf.RoundToInt(Mathf.Clamp(PB.charge , 15, 125)).ToString("X2");
					if (ColorUtility.TryParseHtmlString(teamColor + hexalpha, out Color color2)) MiscColors[1] = color2;

					xxx.CheckRebirthers();

				}
			} 
		}
	}
}
