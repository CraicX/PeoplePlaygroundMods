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
using System.Linq;

namespace PPnpc
{
	public class NpcSmartBed : MonoBehaviour
	{
		public Rigidbody2D R;
		public PhysicalBehaviour P;

		public NpcGadget Expansion = null;

		public GameObject TeamSelect;

		public SpriteRenderer srts;

		public EdgeCollider2D edge;

		private EdgeCollider2D Mattress;

		public ContactFilter2D contactFilter;

		private bool _teamSwitch  = false;

		public Collider2D[] ECols = new Collider2D[24];		

		public SpriteRenderer sr;

		public GameObject Foot1, Foot2;
		

		void Start()
		{
			R = GetComponent<Rigidbody2D>();
			P = GetComponent<PhysicalBehaviour>();

			

			sr = P.spriteRenderer;

			//P.MakeWeightless();

			StartCoroutine(IAnimateBed());
		}

		public void SetupBed()
		{

			



		}

		//void Update()
		//{
		//	if ( Input.GetKeyDown( KeyCode.L ) )
		//	{
		//		//ModAPI.Notify(Global.main.MousePosition);
		//		Vector3 xpos = Global.main.MousePosition - transform.position;
				

		//	}

		//}

		IEnumerator IAnimateBed()
		{
			string[] AcceptedAttachments = { "Hover Thruster", "Motorised Wheel" };
			bool lastSetting = false;
			Color32 c;
			for (; ; )
			{
				if (!P) GameObject.Destroy(this);
				yield return new WaitForSeconds(0.1f);

				if ( Expansion && Expansion.MiscBools[0] && Expansion.ChildChip && Expansion.ChildChip.SRS[1])
				{
					lastSetting = true;
					if (!_teamSwitch)
					{
						srts.material  = ModAPI.FindMaterial("VeryBright");
						_teamSwitch    = true;
					}

					c = Expansion.ChildChip.SRS[1].material.color;
					c.a = 5;
					srts.color = c;
				}
				else { 
					if ( lastSetting )
					{
						lastSetting = false;
						FixedJoint2D[] Joints = Global.FindObjectsOfType<FixedJoint2D>();
						Rigidbody2D rb2;
						for (int i = Joints.Length; --i >=0; )
						{
							if ( Joints[i].connectedBody == R && AcceptedAttachments.Contains( Joints[i].attachedRigidbody.name ) )
							{
								rb2 = Joints[i].attachedRigidbody;

								GameObject.DestroyImmediate( (UnityEngine.Object)Joints[i] );

								rb2.AddForce(UnityEngine.Random.insideUnitCircle * 20,ForceMode2D.Impulse);

							}
						}
						Foot1 = Foot2 = null;
						P.MakeWeightful();


					}
					if (_teamSwitch) { 
						srts.color     = new Color(0.1f,0.1f,0.1f,1f);
						srts.material  = ModAPI.FindMaterial("Sprites-Default");
						_teamSwitch    = false;
					}
				}

				
			}
		}

		public IEnumerator IAttach( GameObject foot )
		{
			//if (foot.transform.root != foot.transform) foot = foot.transform.root.gameObject;
			//R.MoveRotation(0f);
			Rigidbody2D rb2 = foot.GetComponent<Rigidbody2D>();
			yield return new WaitForFixedUpdate();
			SpriteRenderer sr2 = foot.GetComponent<SpriteRenderer>();
			//rb2.MoveRotation(0f);
			yield return new WaitForFixedUpdate();
			R.bodyType = RigidbodyType2D.Static;
			rb2.bodyType = RigidbodyType2D.Static;
			
			float height = sr2.bounds.size.y;

			//R.MovePosition(new Vector2(0,R.position.y + height));
			yield return new WaitForFixedUpdate();

			foot.transform.SetParent(transform, false);

			foot.transform.position = transform.position;
			foot.transform.rotation = transform.rotation;
			yield return new WaitForFixedUpdate();
			if ( foot == Foot1 )
			{
				foot.transform.localPosition = new Vector2(-0.756f,-0.5f - (height / 2));

				FixedJoint2D joint                 = foot.AddComponent<FixedJoint2D>();
				joint.connectedBody                = R;
				joint.autoConfigureConnectedAnchor = true;

			}
			else
			{
				foot.transform.localPosition = new Vector2(1.388f,-0.5f - (height / 2));
				FixedJoint2D joint                 = foot.AddComponent<FixedJoint2D>();
				joint.connectedBody                = R;
				joint.autoConfigureConnectedAnchor = true;
			}
			yield return new WaitForFixedUpdate();
			R.bodyType = RigidbodyType2D.Dynamic;
			rb2.bodyType = RigidbodyType2D.Dynamic;
			yield return new WaitForFixedUpdate();
			R.velocity   *= 0f;
			rb2.velocity *= 0f;

			if (foot.name == "Motorised Wheel") {
				//R.mass = 2.0f;
				//R.gravityScale = 1.0f;
			} else
			{
				R.mass			= 0.01f;
				R.gravityScale	= 0;
			}

			yield return null;
			
		}

		private void OnCollisionEnter2D(Collision2D coll)
		{
			if ( !coll.gameObject || !Expansion.MiscBools[0] ) return;

			string[] AcceptedAttachments = { "Hover Thruster", "Motorised Wheel" };

			if ( AcceptedAttachments.Contains( coll.gameObject.name ) )
			{
				if (coll.gameObject.transform.position.y > transform.position.y - sr.bounds.size.y / 2 ) return;

				float xdist = coll.gameObject.transform.position.x - transform.position.x;
				if (xdist < 0 && !Foot1)
				{
					Foot1 = coll.gameObject;
					StartCoroutine(IAttach( Foot1 ));
				}
				else if (!Foot2 )
				{
					Foot2 = coll.gameObject;
					StartCoroutine(IAttach(  Foot2 ));
				}
			}


			
		}

		

	}

	
}