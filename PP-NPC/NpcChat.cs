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
using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;


namespace PPnpc
{
	class NpcChat : MonoBehaviour
	{
		public TextMeshPro ChatMessage;
		public GameObject ChatBubble;
		public GameObject ChatBG;
		public NpcBehaviour NPC;
		public RectTransform rect;
		public SpriteRenderer SR;
		
		public bool IsActive      = false;
		public Vector3 Offset     = new Vector2(1f,1f);
		private bool _lastFlipped = true;
		public string Message     = "";
		public float Timer        = 1f;
		

		public bool IsFlipped => (NPC.Facing < 0.0f);

		void FixedUpdate()
		{
			if (!IsActive) return;
			
			if (IsFlipped != _lastFlipped) { 

				if ( IsFlipped )
				{
				   // ChatMessage.horizontalAlignment = HorizontalAlignmentOptions.Left;
					Offset = new Vector3(1f,1.5f);
				}
				else
				{
					//ChatMessage.horizontalAlignment = HorizontalAlignmentOptions.Right;
					Offset = new Vector3(-1f,1.3f);
				}

				_lastFlipped = IsFlipped;

			}
			if ( !NPC || !NPC.Head )
			{
				IsActive = false;
				Quiet();
				return;
			}
		   
			ChatBubble.transform.position = Vector3.Slerp(ChatBubble.transform.position,NPC.Head.position + Offset, Time.fixedDeltaTime);
		}

		void Start()
		{
			ChatBubble                      = new GameObject("chatbubble", typeof(TextMeshPro)) as GameObject;
		   if ( IsFlipped )
			{
				// ChatMessage.horizontalAlignment = HorizontalAlignmentOptions.Left;
				Offset = new Vector3(1f,1f);
			}
			else
			{
				//ChatMessage.horizontalAlignment = HorizontalAlignmentOptions.Right;
				Offset = new Vector3(-1f,1f);
			}

			ChatBubble.transform.position   = transform.position + Offset;
			ChatBubble.transform.rotation   = new Quaternion(0,0,0,0);

			if ( ChatBubble.gameObject.TryGetComponent<RectTransform>( out RectTransform rectImage ) )
			{
				rectImage.localScale = new Vector3(1,1);
				rectImage.transform.position = transform.position + Offset;
				rectImage.transform.rotation = new Quaternion(0,0,0,0);
			}

			GameObject ChatBG = new GameObject("ChatBG", typeof(SpriteRenderer)) as GameObject;
			
			SR = ChatBG.GetComponent<SpriteRenderer>();


		}

		public void Say( string text, float seconds, NpcBehaviour npc )
		{
			NPC     = npc;
			Message = text;
			Timer   = seconds;
			StartCoroutine(ISetup());

		}


		IEnumerator ISetup()
		{
			yield return new WaitForSeconds(0.1f);
			ChatBubble.transform.position       = NPC.Head.position + Offset;
			ChatMessage                         = ChatBubble.gameObject.GetOrAddComponent<TextMeshPro>();
			ChatMessage.autoSizeTextContainer   = true;
			ChatMessage.alignment               = TextAlignmentOptions.Left;
			ChatMessage.enableAutoSizing        = false;
			ChatMessage.fontSize                = 2f;
			ChatMessage.color                   = Color.white;
			ChatMessage.extraPadding = true;
			ChatMessage.text                    = Message;
			
			yield return new WaitForFixedUpdate();
			SR.transform.SetParent(ChatBubble.gameObject.transform, false);
			
			Color c     = Color.black;
			c.a         = 0.5f;
			SR.drawMode = SpriteDrawMode.Sliced;
			SR.sprite   = NpcMain.ChatSpriteBG;
			SR.size     = ChatMessage.textBounds.size * 1.1f;
			SR.color    = c;
			IsActive    = true;
			

			yield return new WaitForSeconds(Timer);

			Quiet();
		}
		
		
		

		public void Quiet()
		{
			IsActive         = false;
			ChatMessage.text = "";
			UnityEngine.Object.Destroy(ChatBubble);
			UnityEngine.Object.Destroy(ChatMessage.gameObject);
			UnityEngine.Object.Destroy(this);

		}
	}
}