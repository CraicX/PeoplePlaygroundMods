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
using System.Collections.Generic;
using System;
using UnityEngine;

namespace PPnpc
{
	public class NpcMojo
	{
		NpcBehaviour NPC;

		public Dictionary<string, float> Stats    = new Dictionary<string, float>();
		public Dictionary<string, float> Feelings = new Dictionary<string, float>();
		public Dictionary<string, float> Traits   = new Dictionary<string, float>();

		static string[] s_Stats    = {"Health", "Strength", "Speed", "Energy"};
		static string[] s_Feelings = {"Mojo", "Bored", "Fear", "Angry", "Annoyed", "Hungry", "Bathroom", "Lonely", "Tired", "Chicken"};
		static string[] s_Traits   = {"Shy", "Mean", "Friendly", "Playful", "Annoying", "Brave"};
		
		public float Health
		{
			get
			{
				float blood = 0.1f;
				for (int i = NPC.PBO.Limbs.Length; --i >= 0;) blood += NPC.PBO.Limbs[i].CirculationBehaviour.TotalLiquidAmount;
				return (float)Math.Round(blood / NPC.PBO.Limbs.Length,1);
			}
		}
		public float GetRand => UnityEngine.Random.Range(20,60);
		public void SetFeeling( string feeling, float value ) => Feelings[feeling] = Mathf.Clamp(value, 0, 100);


		public NpcMojo(NpcBehaviour npc)
		{
			NPC = npc;

			foreach (string s in s_Stats)    Stats.Add(s, GetRand);
			foreach (string s in s_Feelings) Feelings.Add(s, GetRand);
			foreach (string s in s_Traits)   Traits.Add(s, GetRand);

			Feelings["Tired"]	= Feelings["Chicken"] = 0f;
			Feelings["Mojo"]		= NPC.PBO.AverageHealth;
		}

		public void ClampAll()
		{
			string[] keys = new string[Feelings.Count];
			Feelings.Keys.CopyTo(keys,0);
			foreach ( string s in keys )
				Feelings[s] = Mathf.Clamp(Feelings[s], 0, 100);
		}

		

		public void Feel( string feeling, float value )
		{
			//Feelings[feeling] += value;
			float adjValue = value;

			switch ( feeling )
			{
				case "Angry":
					Feelings["Annoyed"] += value * 0.2f;
					adjValue *= (Traits["Mean"]/100);
					break;

				case "Annoyed":
					adjValue *= -(Traits["Friendly"]/100);
					adjValue *= -(Traits["Playful"]/100);
					break;

				case "Bored":
					adjValue *= (Traits["Playful"]/100);
					break;

				case "Lonely":
					Feelings["Bored"] += value * 0.5f;
					adjValue *= -(Traits["Shy"]/100);
					break;

				case "Hungry":
					if (value < 0) Feelings["Bathroom"] += (value * -0.5f);
					break;

				case "Fear":
					adjValue *= -(Traits["Brave"]/100);
					break;
			}

			if (!Feelings.ContainsKey(feeling))
			{
				ModAPI.Notify(":Missing Feeling: " + feeling);
				return;
			}
			Feelings[feeling] += adjValue;

			ClampAll();
		}

		public void ShowStats()
		{
			return;
			////if (NPC.PBO.IsAlive()) { FunFacts.ShowFunFacts(NPC.NpcId); return; }
			//if (NPC.PBO) Feelings["Mojo"] = NPC.PBO.AverageHealth;
			//else Feelings["Mojo"] = 0;

			//ModAPI.Notify("");
			//string xtraInfo = "";
			//if (NPC.PrimaryAction == NpcPrimaryActions.Scavenge) {
			//	if (NPC.MyTargets.item) xtraInfo = xxx.NGlow(NPC.MyTargets.item.name);
			//}
			//else if (NPC.PrimaryAction == NpcPrimaryActions.Fight) {
			//	if (NPC.MyTargets.enemy) xtraInfo = xxx.NGlow(NPC.MyTargets.enemy.name);
			//}
			
			
			//int bshot      = FunFacts.Get(NPC.NpcId, "BeenShot");
			//int bstab      = FunFacts.Get(NPC.NpcId, "BeenStabbed");
			//int ShotsFired = FunFacts.Get(NPC.NpcId, "ShotsFired");

			//ModAPI.Notify("<size=150%><align=center><color=#581312>[<color=#841D1C>[ <color=#9BE9EA>" + NPC.Config.Name + "<color=#841D1C> ]<color=#581312>]</color></size>");
			//ModAPI.Notify("<size=110%><align=center><b><color=#C72C2A>Threat </color> <color=#9594E3>--==<color=#9BE9EA>" + (Math.Round(NPC.ThreatLevel,1)) + "<color=#9594E3>==-- <color=#C72C2A> Level</b></color>");
			
			//int pnum        = 1;
			//string myOutput = "";
			//int v           = 1;
			//foreach ( KeyValuePair<string, float> pair in Feelings )
			//{
			//	float val = Mathf.Round(pair.Value);

			//	if (++pnum % 2 == 0) {
			//		myOutput += "<voffset="+ (v * 1.5f) + "em><align=left><pos=10%><size=90%><color=#C72C2A>" 
			//				+ pair.Key + ": <color=#2CC72A><b><pos=40%>" 
			//				+ val + "</b></color><color=#C72C2A>";
			//	}
			//	else {
			//		myOutput += "<pos=60%>" + pair.Key + ": <color=#2CC72A><b><pos=85%>" + val + "</b></color></align></voffset>";
			//		v++;
			//	}
			//}
			
			
			//int health      = Mathf.Clamp(Mathf.RoundToInt((NPC.MyBlood - 0.5f) * 100),1,50);
			//if (!NPC.PBO.IsAlive()) health = 1;
			//string color = "#CC0000";

			//if (health > 20) color = "#CC0000";
			//else if (health > 10) color = "#990000";
			//else color = "#440000";

		
			//ModAPI.Notify("<align=center><voffset=1em><color=#636297><i>( <color=#9796B9>" + NPC.PrimaryAction.ToString() + " <color=#636297>)</i></voffset></align>");
			//ModAPI.Notify("");
			//ModAPI.Notify(myOutput);
			//ModAPI.Notify("");
			//ModAPI.Notify("<voffset=1.5em><align=center><color=" + color + ">" + (new string('/', Mathf.RoundToInt(health)) ) + "</align></voffset>");
			//ModAPI.Notify("<voffset=2em><align=center><size=90%><color=#24B124># Times Shot: <b><color=#E7E789>" 
			//	+ bshot + "</b><space=2em> <color=#24B124>Shot Others: <b><color=#E7E789>" + ShotsFired );

		}







	}

}
