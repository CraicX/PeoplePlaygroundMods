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
		public Dictionary<string, int> Perks      = new Dictionary<string, int>();

		static string[] s_Stats    = {"Health", "Strength", "Speed", "Energy", "Vision", "Brains", "Aiming"};
		static string[] s_Feelings = {"Bored", "Fear", "Angry", "Tired", "Chicken"};
		static string[] s_Traits   = {"Shy", "Mean", "Friendly", "Playful", "Annoying", "Brave"};
		static string[] s_Perks    = {"Heal"};
		
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
			foreach (string s in s_Perks)    Perks.Add(s, 0);

			Feelings["Tired"]	= Feelings["Chicken"] = 0f;
			
			Stats["Vision"]		= 35f;
			Stats["Brains"]		= 5f;
			Stats["Aiming"]	    = 50f;


			

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
					adjValue *= (Traits["Mean"]/100);
					break;

				case "Bored":
					adjValue *= (Traits["Playful"]/100);
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
	}
}
