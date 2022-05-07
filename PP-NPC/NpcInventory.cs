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
using UnityEngine;

namespace PPnpc
{
    class Inventory
    {
        public NpcBehaviour NPC;

        public float StorageSize        = 4.0f;

        public bool HasGun              = false;
        public bool HasShiv             = false;
        public bool HasSword            = false;
        public bool HasClub             = false;
        public bool HasMedic            = false;
        public bool HasFireExtingisher  = false; 

        public Dictionary<int, GameObject> Items = new Dictionary<int, GameObject>();
        public List<NpcTool> Tools      = new List<NpcTool>();

        public List<int> ItemHash       = new List<int>();

        public int GetId(NpcTool item)              => item.P ? item.P.GetHashCode() : 0;
        public int GetId(PhysicalBehaviour item)    => item ? item.GetHashCode() : 0;

        public NpcHand Holding(string fieldName)
        {
            foreach ( NpcHand hand in NPC.Hands )
            {
                if ( hand.Tool.props.Traits.ContainsKey(fieldName) && hand.Tool.props.Traits[fieldName]) return hand;
            }

            return null;
        }


        public bool CanFit( NpcTool tool )
        {
            if (!tool.P) return false;

            return (tool.P.ObjectArea <= StorageSize);
        }
        
        public bool Store( NpcTool tool )
        {
            int itemId = GetId(tool);

            if ( ItemHash.Contains( itemId ) )
            {
                tool.XDestroy();
            }

            return true;
        }


    }

    


}