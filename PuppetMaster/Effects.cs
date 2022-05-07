//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuppetMaster
{
	public class Effects
	{
		public static List<Collider2D> DoNotKill = new List<Collider2D>();
		public static int DoNotKillLayer         = 0;

		public static TrailRenderer Trail;

		public static TextMeshProUGUI Text;

		public static Canvas Canvas;

		public static TextMeshProUGUI speedometer;
		public static GameObject speedometerObj;
		public static Vector3 ScreenSize => Canvas.pixelRect.size;

		public static float TimeBurnout = 0f;


		public enum SpeedometerTypes
		{
			Off,
			Bike,
			Car,
			Hovercraft,
		}

		public static void Speedometer(SpeedometerTypes type, float speed)
		{
			if (speedometer == null)
			{
				if (Canvas == null) Canvas = Global.FindObjectOfType<Canvas>();

				speedometerObj = new GameObject("speedo");

				speedometerObj.transform.SetParent(Canvas.transform, false);

				Vector2 pos                       = new Vector2((ScreenSize.x / 2) + 100f,32f);
				speedometerObj.transform.position = pos;
				speedometer                       = speedometerObj.AddComponent<TextMeshProUGUI>();
				speedometer.fontSize              = 24;
				speedometer.alignment             = TextAlignmentOptions.TopLeft;
				speedometer.raycastTarget         = false;
				speedometer.text                  = "0";
				speedometer.color                 = new Color(0.8f,0.8f,0.8f,0.8f);
				speedometer.faceColor             = new Color(0.8f,0.8f,0.8f,0.8f);
				speedometer.outlineColor          = new Color(0.2f,0.2f,0.2f);
				speedometer.outlineWidth          = 0.1f;
				speedometer.alignment             = TextAlignmentOptions.Left;
				speedometer.fontStyle             = FontStyles.Bold;

				GameObject imageObj               = new GameObject("speedoImage");

				imageObj.transform.SetParent(speedometerObj.transform, false);

				imageObj.transform.position       = new Vector2(ScreenSize.x/2 - 40 , 32f);
				RectTransform rectImage           = imageObj.AddComponent<RectTransform>();
				rectImage.sizeDelta               = new Vector2(45,25);
				Image image                       = imageObj.AddComponent<Image>();
				image.sprite                      = JTMain.SpeedometerSprite;
			}


			if (type == SpeedometerTypes.Off)
			{
				speedometerObj.SetActive(false);

			} else 
			{
				if (speed > 0) speedometer.text = String.Format("{0}", Mathf.RoundToInt(speed));

				if ( Time.frameCount % 100 == 0 && !speedometerObj.activeSelf) speedometerObj.SetActive(true);
			}
		}



		public static TrailRenderer DoTrail(Rigidbody2D rbody, bool kill=false)
		{
			if (kill)
			{
				if (rbody.gameObject.TryGetComponent<TrailRenderer>(out TrailRenderer trailRenderer))
				{
					Util.Destroy(trailRenderer);
				}
				return (TrailRenderer)null;

			}

			Trail                      = rbody.gameObject.GetOrAddComponent<TrailRenderer>();
			//Trail.endColor             = Color.red;
			//Trail.startColor           = Color.white;
			Trail.startWidth           = 0.5f;
			Trail.endWidth             = 0.1f;
			Trail.generateLightingData = true;
			Trail.time                 = 0.5f;
			Trail.material             = Resources.Load<Material>("Materials/PhaseLink");

			return Trail;
		}

		public static void Burnout(PhysicalBehaviour phys, float timeStart=1)
		{
			float burnSec = Mathf.Clamp(Time.time - timeStart, 1f,10f);

			if (burnSec < 4.5f) phys.Sizzle(); 

			if (burnSec < 1.5f) return;


			GameObject ParticlePrefab = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(
				"Prefabs/SmokeParticle"),
				phys.transform.position, phys.transform.rotation, phys.transform) as GameObject;

			ParticleSystem pSys = ParticlePrefab.GetComponent<ParticleSystem>();
			ParticlePrefab.AddComponent<Optout>();

			ParticleSystem.MainModule main   = pSys.main;
			ParticleSystem.ShapeModule shape = pSys.shape;

			shape.radiusSpeedMultiplier           = 50f;
			shape.radiusThickness                 = 0.1f;
			shape.spriteRenderer                  = phys.transform.GetComponent<SpriteRenderer>();
			shape.radiusSpread                    = burnSec;
			shape.radiusSpeed                     = burnSec;
			shape.radiusMode                      = ParticleSystemShapeMultiModeValue.BurstSpread;//.Random;
			shape.shapeType                       = ParticleSystemShapeType.Hemisphere;
			shape.radius                          = burnSec;
			main.startColor                       = new Color(0, 0, 0);
			ParticleSystem.EmitParams pse         = new ParticleSystem.EmitParams()
			{
				startColor    = new Color(0, 0, 0),
				startSize     = 1f,
				startLifetime = 3f,
				position      = phys.transform.position,
			};

			pse.position -= new Vector3(0f, 0.1f);

			float c, x, y;
			for (int i = 0; ++i < 10;)
			{
				c                 = UnityEngine.Random.Range(0.1f,0.5f);
				pse.startColor    = new Color(c, c, c);
				pse.startLifetime = UnityEngine.Random.Range(2,8);
				pse.startSize     = burnSec ;// * 0.1f;
				x                 = UnityEngine.Random.Range(-1f,1f);
				y                 = UnityEngine.Random.Range(0f, 0.3f);
				pse.velocity      = new Vector2(x,y);
				pSys.Emit(pse, 2);

			}

		}


		public static void DoPulseExplosion(
			Vector3 position,
			float force,
			float range,
			bool breakObjects = true)
		{

			Vector2 point = (Vector2)position;
			double num1   = (double)range;
			int mask      = LayerMask.GetMask("Objects");

			foreach (Collider2D collider2D in Physics2D.OverlapCircleAll(point, (float)num1, mask))
			{
				if (DoNotKill.Contains(collider2D)) continue;
				if (DoNotKillLayer == mask) continue;

				if ((bool)(UnityEngine.Object)collider2D.attachedRigidbody)
				{
					Vector3 vector3 = collider2D.transform.position - position;
					float num2      = (float)((-(double)vector3.magnitude + (double)range) * ((double)force / (double)range));

					if (breakObjects && (double)UnityEngine.Random.Range(0, 10) > (double)force)
						collider2D.BroadcastMessage("Break", (object)(Vector2)(vector3 * num2 * -1f), SendMessageOptions.DontRequireReceiver);

					collider2D.attachedRigidbody.AddForce((Vector2)(num2 * vector3.normalized), ForceMode2D.Impulse);

					if ( collider2D.attachedRigidbody.TryGetComponent<LimbBehaviour>( out LimbBehaviour limb ) )
					{

						if (num2 > 1f)
						{
							if (!limb.IsConsideredAlive)
							{
								limb.Crush();
								continue;
							}
							if (num2 > 4f)
							{

								int baddaboom = UnityEngine.Random.Range(1,10);
								if (baddaboom == 1)  limb.Slice(); 
								else if (baddaboom == 2) limb.Crush();
								else if (baddaboom < 5) limb.BreakBone();
							}

							limb.IsLethalToBreak = false;
							limb.Person.AdrenalineLevel = 1f;
							limb.Person.ShockLevel      = 0f;
							limb.Person.PainLevel       = 0f;
							limb.Person.Consciousness   = 1f;
							limb.CirculationBehaviour.HealBleeding();

							limb.Health                                 = limb.InitialHealth;

							limb.Numbness                               = 0.0f;

							limb.CirculationBehaviour.IsPump            = limb.CirculationBehaviour.WasInitiallyPumping;
							limb.CirculationBehaviour.BloodFlow         = 1f;

							limb.BruiseCount                            = 
							limb.CirculationBehaviour.StabWoundCount    =
							limb.CirculationBehaviour.GunshotWoundCount = 0;

						}

					}

				}
			}
		}
	}

	public class DustClouds : MonoBehaviour
	{
		public float startX = 0f;

		public float speed = 1f;

		public float duration = 3f;

		public float timeStarted = 0f;

		public bool startClouds = false;

		public float cloudOffset = 0f;

		public float interval = 0;
		public Vector2 currentPos;
		public Vector2 startPos;

		public void Start()
		{
			timeStarted = Time.time;
			startPos    = Util.FindFloor(transform.position);
			startPos.x  = transform.position.x;
			startClouds = true;
		}

		public void ConfigDust( float _speed, float _duration )
		{
			speed    = _speed;
			duration = _duration;
			interval = speed * duration;
			Start();
		}

		public void FixedUpdate()
		{
			if (!startClouds) return;
			if (Time.time > timeStarted + duration * 0.7f) {
				Util.Destroy(this);
			}

			cloudOffset += interval * Time.deltaTime * 2;

			AddDust(1f); AddDust(-1f); 


		}

		public void AddDust(float direction)
		{
			currentPos = startPos;
			currentPos.x += cloudOffset * direction;

			GameObject ParticlePrefab = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(
				"Prefabs/SmokeParticle"),
				startPos, transform.rotation, transform) as GameObject;

			ParticleSystem pSys = ParticlePrefab.GetComponent<ParticleSystem>();
			ParticlePrefab.AddComponent<Optout>();

			ParticleSystem.MainModule main   = pSys.main;
			ParticleSystem.ShapeModule shape = pSys.shape;

			shape.radiusSpeedMultiplier           = 0.1f;
			shape.radiusThickness                 = 1f;
			shape.spriteRenderer                  = transform.GetComponent<SpriteRenderer>();
			shape.radiusSpread                    = 1f;
			shape.radiusSpeed                     = 1f;
			shape.radiusMode                      = ParticleSystemShapeMultiModeValue.BurstSpread;//.Random;
			shape.shapeType                       = ParticleSystemShapeType.Hemisphere;
			shape.radius                          = 1.5f;
			main.startColor                       = new Color(0, 0, 0);
			ParticleSystem.EmitParams pse         = new ParticleSystem.EmitParams()
			{
				startColor    = new Color(0, 0, 0),
				startSize     = 1.0f,
				startLifetime = 0.1f,
				position      = currentPos,
			};

			//pse.position -= new Vector3(0f, 0.1f);

			float c, x, y;
			for (int i = 0; ++i < 5;)
			{
				c                 = UnityEngine.Random.Range(0.1f,1.0f);
				pse.startColor    = new Color(c, c, c);
				pse.startLifetime = UnityEngine.Random.Range(0.5f,1);
				pse.startSize     = 0.5f ;// * 0.1f;
				x                 = UnityEngine.Random.Range(5.0f,15.5f) * direction;
				y                 = UnityEngine.Random.Range(-0.1f, 0f);
				pse.velocity      = new Vector2(x,y);
				pSys.Emit(pse, 1);

			}

		}
	}
}
