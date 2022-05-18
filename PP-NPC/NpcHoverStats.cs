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
		private NpcBehaviour _npc = null;
		public TextMeshProUGUI Text;
		public Canvas Canvas;
		public TextMeshProUGUI HoverStats;
		public GameObject AttachedObj = null;
		public SpriteRenderer SR = null;
		public Vector3 position;
		public Vector3 Offset = new Vector3(0, 2.1f);
		public RectTransform rectImage2 = null;
		public RectTransform rectImage = null;

		private bool ZoomingIn         = true;
		private Vector2 RSize          = new Vector2(2.5f, 3.5f);
		public static StringBuilder sb = new StringBuilder();
		public bool StartFade          = false;
		private bool okInit = false;


		public NpcBehaviour NPC { 
			get { return _npc; }
			set { 
				_npc = value; 
				AttachedObj = value.Head.gameObject;
				okInit = true;

			}
		}

		public Vector3 ScreenSize => Canvas.pixelRect.size;

		private bool _show = false;

		

		void Start()
		{
			Offset = new Vector3(0, 2.1f);
		}

		void FixedUpdate()
		{
			//if (Time.frameCount % 100 == 0 && (!AttachedObj || !_npc || !_npc.PBO ) )
   //         {
			//	//if (HSObj) UnityEngine.Object.Destroy(HSObj);
			//	UnityEngine.Object.Destroy(this);
			//	return;
   //         }

			
			//if ( ZoomingIn && rectImage2 )
			//{


			//	rectImage2.sizeDelta = Vector2.Lerp(rectImage2.sizeDelta, RSize,Time.fixedDeltaTime * 5f);

			//	if ((rectImage2.sizeDelta - RSize).magnitude < 0.1f) {
			//		ZoomingIn = false;
			//	}
			//} else
            //{
				if ( okInit && _show && AttachedObj )
				{
					transform.position = Vector3.Slerp(transform.position,AttachedObj.transform.position + Offset,  Time.fixedDeltaTime);
				}
            //}
		}

		public void ToggleStats( NpcBehaviour _npc = null )
		{
			if (_npc != null) NPC = _npc;
			_show = !_show;
			_npc.ShowingStats = _show;
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

			sb.Clear();

			sb.AppendFormat(@"<font-weight=900><br><size=150%><align=center><color=#9BE9EA>{0}</color></size><line-height=165%>", _npc.Config.Name);
			sb.AppendFormat(@"<br><size=120%><align=center><b><color=#C72C2A>Threat </color> <color=#9594E3>--==<color=#9BE9EA> {0} <color=#9594E3>==-- <color=#C72C2A> Level</b></color></align></size><br><line-height=100%>", (Math.Round(NPC.ThreatLevel,1)));

			int pnum = 1;

			foreach ( KeyValuePair<string, float> pair in _npc.Mojo.Feelings )
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

			foreach ( KeyValuePair<string, float> pair in _npc.Mojo.Stats )
			{
				float val = Mathf.Round(pair.Value);

				if (++pnum % 2 == 0) {
					sb.AppendFormat(@"<br><align=left><pos=10%><color=#55B16D>{0}: <color=#CB0B1E><b><pos=43%>{1}</b></color>", pair.Key, val); 
				} else
				{
					sb.AppendFormat(@"<pos=55%><color=#55B16D>{0}: <color=#CB0B1E><b><pos=85%>{1}</b></color>", pair.Key, val); 
				}
			}

			string ActionString = _npc.PrimaryAction.ToString();

			if ("Wander Thinking Wait".Contains(ActionString)) ActionString = "";

			if (_npc.ActivityMessage.time > Time.time) ActionString = _npc.ActivityMessage.msg;

			if (ActionString != "") sb.AppendFormat(@"<br><br><size=120%><align=center><color=#636297><i>( <color=#A2C4CB>{0}<color=#636297> )</i></align>", ActionString);

			HoverStats.text = sb.ToString();

			NPC.ShowingStats = true;

		}

		public void HideStats()
		{
			//NpcHoverStats.Hide(this);
			sb.Clear();
			AllHoverStats.Remove(NPC.NpcId);

			Destroy( gameObject );
			Destroy( this );
		}



		public void DisplayStats()
		{
			if (!CheckNPC()) return;

			//HSObj                = new GameObject("NpcHoverStats") as GameObject;
			transform.position     = AttachedObj.transform.position + Offset;
			transform.rotation     = new Quaternion(0,0,0,0);
			transform.localScale   = Vector3.one;

			gameObject.SetLayer(11);
			SR = gameObject.GetComponent<SpriteRenderer>();

			rectImage = gameObject.GetComponent<RectTransform>();
			rectImage.sizeDelta     = new Vector2(2.5f,3.5f);
			
			Canvas = gameObject.GetComponent<Canvas>() as Canvas;

			GameObject HSimg = new GameObject("HoverBGImg") as GameObject;
			HSimg.SetLayer(11);
			HSimg.transform.SetParent(transform);
			HSimg.transform.position  = Canvas.transform.position;
			rectImage2  = HSimg.AddComponent<RectTransform>();
			rectImage2.sizeDelta      = RSize;
			Image image               = HSimg.AddComponent<Image>();
			image.sprite              = NpcMain.StatsBG;
			image.CrossFadeAlpha(0.5f,0,true);

			HoverStats = gameObject.GetComponent<TextMeshProUGUI>();
			HoverStats.fontSize              = 0.12f;
			
			HoverStats.text                  = "";
			HoverStats.outlineWidth          = 0.0f;
			HoverStats.fontStyle             = FontStyles.UpperCase | FontStyles.Bold;
			HoverStats.isOverlay             = true;
			HoverStats.lineSpacing           = 25.0f;

			ShowText();
			_show = true;
			NPC.ShowingStats = _show;

			StartCoroutine(IValidate());


		}

		public IEnumerator IValidate()
        {
			for (; ; )
            {
				if (!_npc || !_npc.PBO)
                {
					HideStats();
					yield return new WaitForFixedUpdate();
					Destroy(this);
                }
				
				//ShowText();

				yield return new WaitForSeconds(5);
            }
        }

		public static Dictionary<int, NpcHoverStats> AllHoverStats = new Dictionary<int, NpcHoverStats>();
		
		public static bool HasBeenInit = false;

		public static Coroutine vRout;

		public static void Show( NpcBehaviour npc )
        {
			if (AllHoverStats.TryGetValue(npc.NpcId,out NpcHoverStats hoverStats))
            {
				hoverStats.DisplayStats();
            }
			else
            {
				NpcHoverStats npcHoverStats = CreateHoverStats(npc);
				AllHoverStats[npc.NpcId]    = npcHoverStats;
				npcHoverStats.DisplayStats();
            }
        }

		public static void Hide( NpcBehaviour npc )
        {
			if (AllHoverStats.TryGetValue(npc.NpcId,out NpcHoverStats hoverStats)) Hide(hoverStats);
        }

		public static void Hide( int NpcId )
        {
			if (AllHoverStats.TryGetValue(NpcId,out NpcHoverStats hoverStats)) Hide(hoverStats);
        }

		public static void Hide( NpcHoverStats hs )
        {
			foreach ( KeyValuePair<int, NpcHoverStats> pair in AllHoverStats )
            {
				if (pair.Value == hs)
                {
					if (hs) hs.HideStats();
					return;
                }
            }
        }

		public static void Toggle( NpcBehaviour npc )
        {

			if (AllHoverStats.TryGetValue(npc.NpcId,out NpcHoverStats hs)) Hide(hs);
			else Show(npc);
        }

		public static NpcHoverStats CreateHoverStats( NpcBehaviour myNpc )
        {
			GameObject MyGo	= new GameObject("HoverStats", 
				 typeof(SpriteRenderer), 
				 typeof(NpcHoverStats), 
				 typeof(RectTransform), 
				 typeof(Canvas), 
				 typeof(TextMeshProUGUI), 
				 typeof(Optout)) as GameObject;
			NpcHoverStats xHoverStats = MyGo.AddComponent<NpcHoverStats>();
			xHoverStats.NPC           = myNpc;
			return xHoverStats;
        }

		

	}


}
