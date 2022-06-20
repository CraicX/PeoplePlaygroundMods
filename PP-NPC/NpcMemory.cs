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
using System.Linq;
using UnityEngine;

namespace PPnpc
{
    public class NpcMemory
    {
        public NpcBehaviour NPC;

        public Dictionary<int, KnownNpc> KnownNpcs = new Dictionary<int, KnownNpc>();
        
        private NpcBehaviour _lastContact = null;
        private float lcTimer = 0f;
        public NpcBehaviour LastContact
        {
            get { return (Time.time - lcTimer > 60) ? null : _lastContact; }
            set { 
                _lastContact = value; 
                lcTimer      = Time.time;
                }
        }

        public List<int> NoFidget = new List<int>();

        public NpcMemory( NpcBehaviour npc )
        {
            NPC = npc;
        }

        public class KnownNpc
        {
            public int NpcId;
            public float Friendship = 0;
            public Dictionary<string, int> Stats = new Dictionary<string, int>();
            
            public int GetStat( string statName ) => Stats.ContainsKey( statName ) ? Stats[statName] : 0;

            public int AddStat( string statName, int alphaVal=1 )
            {
                if ( !Stats.ContainsKey( statName ) ) Stats[ statName ] = alphaVal;
                else Stats[ statName ] += alphaVal;

                return Stats[ statName ];
            }

            public int SetStat( string statName, int val )
            {
                Stats[statName] = val;
                return Stats[ statName ];
            }

            public KnownNpc( int _npcId ) => NpcId = _npcId;

        }

        public void Init( NpcBehaviour npc )
        {
            NPC = npc;
        }

        public int GetNpcStat( int npcId, string statName )
        {
            if ( KnownNpcs.TryGetValue( npcId, out KnownNpc knpc ) )
            {
                return knpc.GetStat( statName );
            }

            return 0;
        }

        public int AddNpcStat( int npcId, string statName, int alphaVal=1 )
        {
            CheckNpcMemory(npcId);
            return KnownNpcs[npcId].AddStat( statName, alphaVal );
        }


        public int SetNpcStat( int npcId, string statName, int val )
        {
            CheckNpcMemory(npcId);
            return KnownNpcs[npcId].SetStat( statName, val );
        }


        public void CheckNpcMemory( int npcId )
        {
            if (!KnownNpcs.ContainsKey(npcId))
            {
                KnownNpc knpc = new KnownNpc(npcId);
                KnownNpcs.Add( npcId, knpc );
            }
        }

    }
}