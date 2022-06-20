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
		public List<NpcBehaviour> NpcQueue = new List<NpcBehaviour>();
		public GameObject TextObj;
		//public Canvas Canvas = null;
		public SpriteRenderer[] SRS;
		public bool WorkingQueue = false;
		public Coroutine QueueRoutine = null;
		private bool _rebirtherActive = false;
		public SpawnableAsset asset;
		public GameObject RebirtherLight;
		public SingleFloodlightBehaviour[] Lightsx;  
		private int _teamId = 0;
		public Color[] MiscColors;
		private AudioSource audioSource;
		private AudioSource audioSource2;
		public float[] MiscFloats;
		public string[] MiscStrings;
		public bool[] MiscBools;
		public Coroutine xCoroutine, AnimateRoutine;
		public NpcGadget Expansion = null;
		public GameObject TeamSelect;
		public int SoulsRebirthed = 0;
		public int TimesShot      = 0;

		public bool AlreadyStarted = false;
		public bool ExitQueue = false;

		public Dictionary<int, int> Births  = new Dictionary<int, int>();

		private bool bigPowerOn = false;
		public TextMeshPro TV;
		public SpriteRenderer SR, TVSR;
		public RectTransform rectImage;

		void Awake()
		{
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
		}

		public void SetupRebirther()
		{
			PB          = gameObject.GetComponent<PhysicalBehaviour>();
			MiscBools   = new bool[] { false, false };
			MiscStrings = new string[] {"000000", "000000"};
			MiscColors  = new Color[] { Color.red, Color.green, Color.blue };
			MiscFloats  = new float[]{ Time.time, 0.01f };

			//GameObject TV = neew GameObject("TV") as GameObject;

			//TV.transform.position     = transform.position + new Vector3(0,0.5f);;
			//TV.transform.rotation     = new Quaternion(0,0,0,0);
			//TV.transform.localScale   = Vector3.one;

			//gameObject.SetLayer(11);
			SR                           = gameObject.GetComponent<SpriteRenderer>();
			GameObject TVScreen          = new GameObject("TVScreen", typeof(TextMeshPro));
			//TVSR                         = TVScreen.gameObject.GetComponent<SpriteRenderer>();
			if ( TVScreen.gameObject.TryGetComponent<RectTransform>( out RectTransform rectImage ) )
            {
                rectImage.localScale = Vector3.one;
                rectImage.transform.SetParent( transform, false );
                rectImage.transform.position = transform.position;
                rectImage.transform.rotation = transform.rotation;
                //rectImage.sizeDelta = new Vector2( 13.5f, 9.5f );
			}
			
				//rectImage.transform.localPosition  = new Vector3(0, 0.5f);
			//Canvas = TVScreen.gameObject.AddComponent<Canvas>() as Canvas;
			//Canvas.transform.SetParent( TVScreen.transform, false );
			//Canvas.transform.localScale = Vector3.one;
			//Canvas.transform.position = TVScreen.gameObject.transform.position;
			//Canvas.transform.rotation = TVScreen.gameObject.transform.rotation;
			//Canvas.transform.localPosition = new Vector3(0,0.5f);
			
			TV   = TVScreen.gameObject.GetOrAddComponent<TextMeshPro>();
			TV.transform.SetParent(TVScreen.transform, false);
			TV.transform.position      = transform.position;
			TV.transform.rotation      = transform.rotation;
			TV.transform.localScale    = new Vector3(0.4f, 0.4f);
			//TV.transform.localScale    = new Vector3(0.05f, 0.05f, 0.0f);
			//TV.bounds                = rectImage.GetWorldSpaceBounds();//.bounds = new Bounds(0,0ize = new Vector3(2.1f, 2.1f);
			TV.transform.localPosition = new Vector3(0f, 0.5f);
			TV.fontSize                = 2f;
			TV.horizontalAlignment     = HorizontalAlignmentOptions.Center;
			TV.verticalAlignment       = VerticalAlignmentOptions.Middle;
			TV.color                   = Color.blue;
			TV.text                    = "";
			//TV.outlineWidth          = 0.0f;
			//TV.fontStyle             = FontStyles.Normal;
			//TV.isOverlay             = false;
			//TV.lineSpacing           = 25.0f;

			//
			//

			NpcQueue = new List<NpcBehaviour>();




			//asset       = ModAPI.FindSpawnable("Red cathode light");
	//         instance	= UnityEngine.Object.Instantiate<GameObject>(asset.Prefab, transform.position, Quaternion.identity) as GameObject;

			//pb	= instance.GetComponent<PhysicalBehaviour>();
	//         pb.SpawnSpawnParticles                    = false;
			//pb.Disintegratable                        = false;
	//         pb.SimulateTemperature                    = false;
	//         pb.StabCausesWound                        = false;
	//         pb.Properties.Brittleness                 = 0.0001f;
	//         pb.Properties.BurningTemperatureThreshold = float.MaxValue;
	//         pb.Properties.Burnrate                    = 0.00001f;
	//         pb.Properties.Conducting                  = false;
	//         pb.Properties.Flammability                = 0.0000001f;
	//         pb.Properties.BulletSpeedAbsorptionPower  = float.MaxValue;

	//         instance.layer = 2;

	//         instance.transform.SetParent(gameObject.transform, false);
	//         instance.transform.position        = transform.position;
			//instance.transform.localPosition   = new Vector3(0,-1.5f);

			////instance.transform.localRotation	= new Quaternion(0,45,0,0);

			////instance.layer = LayerMask.NameToLayer("Debris"); 
			////pb.gameObject.layer = instance.layer;

			//FixedJoint2D joint2                 = instance.gameObject.AddComponent<FixedJoint2D>();
			//joint2.connectedBody                = PB.rigidbody;
			//joint2.autoConfigureConnectedAnchor = true;


			////instance.transform.localPosition = spotLight.Offset;
	//         //instance.transform.localRotation = Quaternion.Euler(0,0, spotLight.Rotation);

	//         if (instance.TryGetComponent<SpriteRenderer>(out SpriteRenderer srr2))
	//         {
	//             Texture2D tex     = new Texture2D(0, 0);
	//             Sprite mySprite   = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.1f, 0.1f), 0.1f);
	//             srr2.sprite       = mySprite;
	//         }

	//         if (instance.TryGetComponent<GlowtubeBehaviour>(out GlowtubeBehaviour tube))
	//         {
	//             tube.Activated         = true;
	//             tube.LightSprite.color = Color.red;
			//	tube.GasColour         = Color.blue;
			//	tube.GasEffectRadius   = 5f;

	//             if (light.TryGetComponent<DamagableMachineryBehaviour>(out DamagableMachineryBehaviour dmb))
	//             {
	//                 GameObject.Destroy(dmb);
	//             }

			//}

			//GlowtubeBehaviour

			AnimateRoutine = StartCoroutine(IAnimateRebirther());
			StartCoroutine(ISouls());
			StartCoroutine(IProcessQueue());

		}



		public bool RebirtherActive
		{
			get { return _rebirtherActive; }
			set { 
				if ( value != _rebirtherActive )
				{
					if (!value )
					{
						if (QueueRoutine != null) {
							StopCoroutine(QueueRoutine);
							ExitQueue = true;
						}
						foreach ( NpcBehaviour npc in NpcQueue )
						{
							if ( npc )
							{
								if (npc.RebirthLight) GameObject.Destroy(npc.RebirthLight.gameObject);
								npc.RunLimbs(npc.LimbAlphaFull,true);
								npc.RunLimbs(npc.LimbGhost, false);
								npc.RunLimbs(npc.LimbImmune, false);
								npc.RunLimbs(npc.LimbIchi);
								if (npc.PBO) { 
									NpcDeath reaper = npc.PBO.gameObject.AddComponent<NpcDeath>();
									reaper.Config(npc);
								} else
								{
									GameObject.Destroy(npc.gameObject);
								}
							}
						}
						NpcQueue.Clear();
					} else
					{
						_rebirtherActive = value; 
					}
				}
				_rebirtherActive = value; 

			}
		}


		public void Use(ActivationPropagation activation)
		{
			if (xCoroutine != null) StopCoroutine(xCoroutine);

			if (PB.charge < 1f) xCoroutine = StartCoroutine(IMessage("<color=#D72F32><size=70%>No Power</size>", 6, 0.5f ));
			else
			{
				audioSource.PlayOneShot(NpcMain.GetSound("change-team"), 1f);
				TeamId += 1;
				string teamColor = xxx.GetTeamColor(TeamId);
				MiscStrings[0] = teamColor;
				MiscStrings[1] = teamColor;
			}
		}



		IEnumerator IProcessQueue()
		{
			//asset = ModAPI.FindSpawnable("Flashlight");

	//         GameObject instance    = UnityEngine.Object.Instantiate<GameObject>(asset.Prefab, transform.position, Quaternion.identity) as GameObject;
	//         PhysicalBehaviour pb   = instance.GetComponent<PhysicalBehaviour>();

	//         pb.SpawnSpawnParticles                    = false;
			//pb.Disintegratable                        = false;
	//         pb.SimulateTemperature                    = false;
	//         pb.StabCausesWound                        = false;
	//         pb.Properties.Brittleness                 = 0.0001f;
	//         pb.Properties.BurningTemperatureThreshold = float.MaxValue;
	//         pb.Properties.Burnrate                    = 0.00001f;
	//         pb.Properties.Conducting                  = false;
	//         pb.Properties.Flammability                = 0.0000001f;
	//         pb.Properties.BulletSpeedAbsorptionPower  = float.MaxValue;

	//         instance.layer = 2;

	//         instance.transform.SetParent(gameObject.transform, false);
	//         instance.transform.position        = transform.position;
			//instance.transform.localPosition   = new Vector3(0,1.5f);
			////instance.transform.localScale = new Vector3(10f,1f);

			////instance.layer = LayerMask.NameToLayer("Debris"); 
			////pb.gameObject.layer = instance.layer;

			//FixedJoint2D joint                 = instance.gameObject.AddComponent<FixedJoint2D>();
			//joint.connectedBody                = PB.rigidbody;
			//joint.autoConfigureConnectedAnchor = true;

	//         Lightsx = new SingleFloodlightBehaviour[2];

			////instance.transform.localPosition = spotLight.Offset;
	//         //instance.transform.localRotation = Quaternion.Euler(0,0, spotLight.Rotation);

	//         if (instance.TryGetComponent<SpriteRenderer>(out SpriteRenderer srr))
	//         {
	//             Texture2D tex    = new Texture2D(0, 0);
	//             Sprite mySprite  = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.1f, 0.1f), 0.1f);
	//             srr.sprite       = mySprite;
	//         }


			//Lightsx[0] = instance.GetComponent<SingleFloodlightBehaviour>();
			//Lightsx[0].transform.localScale = new Vector3(10f, 0.5f);
	//         Lightsx[0].Highlights[0].Intensity = 0f;
	//         Lightsx[0].Highlights[0].Colour = Color.red;
	//         Lightsx[0].Highlights[0].Initialise();
			//Lightsx[0].Activated = true;

	//         if (Lightsx[0].TryGetComponent<DamagableMachineryBehaviour>(out DamagableMachineryBehaviour dmb))
	//         {
	//             GameObject.Destroy(dmb);
	//         }

			float cpuSpeedStart = 10;
			float cpuSpeed, ww, sqr;
			Vector2 dist;
			NpcBehaviour npc = null;
			while ( true )
			{
				ww = 1;
				cpuSpeed = Mathf.Clamp(cpuSpeedStart - (PB.charge * 0.1f), 1, 10);
				yield return new WaitForSeconds(cpuSpeed);

				if ( !RebirtherActive  || NpcQueue.Count <= 0 ) continue;
				//Lightsx[0].Highlights[0].Colour = Color.red;
				//Lightsx[0].transform.localScale = new Vector3(, 0.5f);

				if ( !NpcQueue[0]  ) continue;
				npc = NpcQueue[0];

				NpcQueue.RemoveAt(0);

				if (!npc) continue;

				npc.InRebirth = false;
				if (npc.RebirthLight) { 
					npc.RebirthLight.Brightness *= 2;
					npc.RebirthLight.Color = Color.cyan;
				}

				while (npc && npc.PBO && npc.Head)
				{
					dist = (transform.position - npc.Head.position);
					sqr  = dist.sqrMagnitude;
					if (npc.RB["UpperBody"]) npc.RB["UpperBody"].AddForce(dist.normalized * Mathf.Clamp(PB.charge * 2, 5, 30) * Time.fixedDeltaTime);
					if (npc.RB["LowerBody"]) npc.RB["LowerBody"].AddForce(dist.normalized * Mathf.Clamp(PB.charge * 2, 5, 30) * Time.fixedDeltaTime);
					if (npc.RB["MiddleBody"]) npc.RB["MiddleBody"].AddForce(dist.normalized * Mathf.Clamp(PB.charge * 2, 5, 30) * Time.fixedDeltaTime);
					if ( sqr < 2000f )
					{
						//Lightsx[0].Highlights[0].Intensity = 1f;
						//Lightsx[0].transform.localScale = new Vector3(sqr/200f, 0.5f);
					}
					if ( sqr < 300f && npc.RB["Head"].velocity.sqrMagnitude > 10) npc.RB["UpperBody"].velocity *= 0.5f;
					if ( sqr < 5f )
					{
						if (npc.RB["UpperBody"])	npc.RB["UpperBody"].velocity *= 0.1f;
					}
					if (sqr < 3f) {
						if (npc.RB["MiddleBody"])   npc.RB["MiddleBody"].velocity *= 0f; 
						break;
					}

					yield return new WaitForFixedUpdate();
					if (ExitQueue)
					{
						npc.RunLimbs(npc.LimbIchi);
						npc.Death(true);
						continue;
					}
				}

				if (!npc || !npc.PBO) continue;

				npc.RB["UpperBody"].velocity *= 0f;

				xxx.ToggleCollisions(npc.Head, transform);

				yield return new WaitForFixedUpdate();

				npc.RunLimbs(npc.LimbRecover);
				npc.RunLimbs(npc.LimbHeal);

				npc.LB["Foot"].PhysicalBehaviour.MakeWeightful();
				//npc.LB["FootFront"].PhysicalBehaviour.MakeWeightful();

				float timer = Time.time + 1.5f;
				//Lightsx[0].Highlights[0].Colour = Color.green;
				while ( Time.time < timer )
				{
					ww += 0.05f;
					//Lightsx[0].transform.localScale = new Vector3(Mathf.Clamp(ww,1,10), 0.5f);
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


				if ( Births[npc.NpcId] > 2 && xxx.rr(1,3) == 2)
				{
					StartCoroutine(IMessage("Welcome back,\n" + npc.Config.Name + "!", 20));
				}
				else StartCoroutine(IMessage("You have been\nRebirthed!", 20));


				audioSource.PlayOneShot(NpcMain.GetSound("rebirth"), 1f);

				for (int i = 1; ++i < 50;) 
				{
					dist = (transform.position - npc.Head.position);
					//npc.RB["Head"].AddForce(dist.normalized * 3f * Time.fixedDeltaTime);
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
					//currentGadget = currentGadget.ChildGadget;

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
								npc.EnhancementTroll = true;
								yield return new WaitForSeconds(0.5f);
								for (int i = 0; ++i <=55;) 
									ModAPI.CreateParticleEffect("Flash", npc.Head.position);
								audioSource.PlayOneShot( NpcMain.GetSound("hero"), 1f);
							}
							break;
					}

					currentChip = currentChip.ChildChip;
				}


				//Lightsx[0].Highlights[0].Intensity = 0f;
				//Lightsx[0].Highlights[0].Colour = Color.red;

				if (ExitQueue)
				{
					npc.RunLimbs(npc.LimbIchi);
					npc.Death(true);
					continue;
				}

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


		public IEnumerator ISouls()
		{
			Color c;
			float pushTime = 0;
			for(;;) { 

				if (NpcQueue.Count > 0) {
					c = Color.HSVToRGB( Mathf.PingPong((Time.time + MiscFloats[0]) * 0.5f, 1), 1, 1);
					for(int i = NpcQueue.Count; --i >= 0;) {
						if ( !NpcQueue[i] || !NpcQueue[i].RebirthLight || !NpcQueue[i].RebirthLight.gameObject) continue;
						NpcQueue[i].RebirthLight.Color = c;
					}

					if ( Time.time > pushTime) 
					{
						pushTime = Time.time + xxx.rr(1f,3f);

						NpcBehaviour npc = NpcQueue.PickRandom();

						if (npc) npc.RB["UpperBody"].AddForce( UnityEngine.Random.insideUnitCircle * xxx.rr( 1, Mathf.Clamp(PB.charge / 3, 0.1f, 10f) ), ForceMode2D.Impulse );
					}

				}

				yield return new WaitForSeconds(0.1f);
			}
		}

		public IEnumerator IAnimateRebirther()
		{
			GameObject bs;
			SpriteRenderer bsr;

			yield return new WaitForFixedUpdate();

			TeamSelect.transform.SetParent(gameObject.transform, false);
			TeamSelect.transform.localPosition = new Vector2(0.022f,0.06f);


			for(;;)
			{
				while ( PB.charge < 1f )
				{
					if ( MiscBools[0] == true )
					{
						if (audioSource.isPlaying) audioSource.Stop();
						yield return new WaitForEndOfFrame();
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
					audioSource.PlayOneShot(NpcMain.GetSound("rb_on", 3), 0.6f);
					

					yield return new WaitForSeconds(2f);
					TV.text = "";

					if ( !Expansion || !Expansion.MiscBools[0] )
					{
						if (audioSource.isPlaying) audioSource.Stop();
						yield return new WaitForEndOfFrame();
						ClearMsg();
						TV.text = "";
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

							//Lights[1].Color = Color.HSVToRGB( Mathf.PingPong((Time.time + MiscFloats[0]), 1), 1, 1);
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
