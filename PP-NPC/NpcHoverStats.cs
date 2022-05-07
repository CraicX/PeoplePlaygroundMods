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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace PPnpc
{
	public class NpcHoverStats : MonoBehaviour
	{
		private NpcBehaviour _npc;
		public TextMeshProUGUI Text;
		public Canvas Canvas;
		public TextMeshProUGUI HoverStats;
		public GameObject HSObj;
		public GameObject AttachedObj;
		public SpriteRenderer SR;
		public Vector3 position;
		public Vector3 Offset;
		public RectTransform rectImage2;
		public RectTransform rectImage;
		private bool ZoomingIn = true;
		private Vector2 RSize = new Vector2(2.5f, 3.5f);
		public static StringBuilder sb = new StringBuilder();


		public NpcBehaviour NPC { 
			get { return _npc; }
			set { 
				_npc = value; 
				AttachedObj = value.Head.gameObject;
			}
		}

		public Vector3 ScreenSize => Canvas.pixelRect.size;

		private bool _show = false;

		public bool Show
		{
			get { return _show; }
			set { _show = value; }
		}

		void Start()
		{
			Offset = new Vector3(0, 2.1f);
		}

		void FixedUpdate()
		{
			if ( _show )
			{
				HSObj.transform.position = Vector3.Slerp(HSObj.transform.position,AttachedObj.transform.position + Offset,  Time.fixedDeltaTime);
			}
			if ( ZoomingIn )
			{
				rectImage2.sizeDelta = Vector2.Lerp(rectImage2.sizeDelta, RSize,Time.fixedDeltaTime * 5f);

				if ((rectImage2.sizeDelta - RSize).magnitude < 0.1f) {
					ZoomingIn = false;
					ShowText();
				}
			}
		}

		public void ToggleStats( NpcBehaviour _npc = null )
		{
			if (_npc != null) NPC = _npc;
			_show = !_show;
			NPC.ShowingStats = _show;
			if (_show) DisplayStats();
			else HideStats();
		}

		public bool CheckNPC()
		{
			if (NPC) return true;

			if ( transform.parent.root.TryGetComponent<NpcBehaviour>( out NpcBehaviour _npc ) )
			{
				NPC = _npc;
				return true;
			}

			return false;
		}


		

		public void ShowText()
		{
			if (ZoomingIn) return;

			sb.Clear();

			sb.AppendFormat(@"<font-weight=900><br><size=150%><align=center><color=#9BE9EA>{0}</color></size><line-height=165%>", NPC.name);
			sb.AppendFormat(@"<br><size=120%><align=center><b><color=#C72C2A>Threat </color> <color=#9594E3>--==<color=#9BE9EA> {0} <color=#9594E3>==-- <color=#C72C2A> Level</b></color></align></size><br><line-height=100%>", (Math.Round(NPC.ThreatLevel,1)));

			int pnum = 1;

			foreach ( KeyValuePair<string, float> pair in NPC.Mojo.Feelings )
			{
				float val = Mathf.Round(pair.Value);

				if (++pnum % 2 == 0) {
					sb.AppendFormat(@"<br><align=left><pos=10%><color=#55B16D>{0}: <color=#2CC72A><b><pos=43%>{1}</b></color>", pair.Key, val); 
				} else
				{
					sb.AppendFormat(@"<pos=55%><color=#55B16D>{0}: <color=#2CC72A><b><pos=85%>{1}</b></color>", pair.Key, val); 
				}
			}

			sb.Append("<br>");

			pnum = 1;

			foreach ( KeyValuePair<string, float> pair in NPC.Mojo.Stats )
			{
				float val = Mathf.Round(pair.Value);

				if (++pnum % 2 == 0) {
					sb.AppendFormat(@"<br><align=left><pos=10%><color=#55B16D>{0}: <color=#CB0B1E><b><pos=43%>{1}</b></color>", pair.Key, val); 
				} else
				{
					sb.AppendFormat(@"<pos=55%><color=#55B16D>{0}: <color=#CB0B1E><b><pos=85%>{1}</b></color>", pair.Key, val); 
				}
			}

			sb.AppendFormat(@"<br><br><size=120%><align=center><color=#636297><i>( <color=#A2C4CB>{0}<color=#636297> )</i></align>", NPC.PrimaryAction.ToString());

			HoverStats.text = sb.ToString();

		}

		public void HideStats()
		{
			if (HSObj)
			{
				Destroy( HSObj );
				ZoomingIn = true;
				sb.Clear();
			}
		}

		public void DisplayStats()
		{
			if (!CheckNPC()) return;

			//if (HSObj == null) Canvas = Global.FindObjectOfType<Canvas>();
			
			HSObj = new GameObject("NpcHoverStats") as GameObject;
			HSObj.transform.SetParent(AttachedObj.transform, false);
			//HSObj.transform.parent     = AttachedObj.transform;
			HSObj.transform.position     = AttachedObj.transform.position + Offset;
			HSObj.transform.parent       = null;
			HSObj.transform.rotation     = new Quaternion(0,0,0,0);
			HSObj.transform.localScale = Vector3.one;

			//	2.5f,3.5f
			HSObj.SetLayer(11);
			rectImage = HSObj.AddComponent<RectTransform>();
			rectImage.sizeDelta     = new Vector2(2.5f,3.5f);
			
			Canvas = HSObj.AddComponent<Canvas>() as Canvas;

			GameObject HSimg = new GameObject("HoverBGImg") as GameObject;
			HSimg.SetLayer(11);
			HSimg.transform.SetParent(HSObj.transform);
			HSimg.transform.position  = Canvas.transform.position;
			rectImage2  = HSimg.AddComponent<RectTransform>();
			rectImage2.sizeDelta      = new Vector2(0.1f, 0.1f);
			Image image               = HSimg.AddComponent<Image>();
			image.sprite              = NpcMain.StatsBG;
			image.CrossFadeAlpha(0.5f,0,true);

			HoverStats = HSObj.AddComponent<TextMeshProUGUI>();
			HoverStats.fontSize              = 0.12f;
			
			HoverStats.text                  = "";
			//HoverStats.color                 = Color.white;
			//HoverStats.faceColor             = new Color(0.8f,0.8f,0.8f,1f);
			//HoverStats.outlineColor          = new Color(0.2f,0.2f,0.2f,0.0f);
			HoverStats.outlineWidth          = 0.0f;
			//HoverStats.alignment             = TextAlignmentOptions.Left;
			HoverStats.fontStyle             = FontStyles.UpperCase | FontStyles.Bold;
			HoverStats.isOverlay = true;
			HoverStats.lineSpacing = 25.0f;

			ShowText();

			NPC.ShowingStats = _show;

		}

	}
}
