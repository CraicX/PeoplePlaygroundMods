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
    public static class NpcEvents
    {
        public static Dictionary<int, NpcBehaviour> NPCs = new Dictionary<int, NpcBehaviour>();
        public static Dictionary<int, Transform>    NPCT = new Dictionary<int,Transform>();
        public static Dictionary<int, EventInfo> EventLookup = new Dictionary<int, EventInfo>();

        public static Dictionary<EventIds, float> EventThrottle = new Dictionary<EventIds, float>();

        public static event System.EventHandler<UserSpawnEventArgs> OnItemSpawned;
        public static bool IsConfigured = false;


        public static void Subscribe( NpcBehaviour npc )
        {
            if ( !NPCs.ContainsKey( npc.NpcId ) )
            {
                NPCs.Add(npc.NpcId, npc );
                NPCT.Add(npc.NpcId,npc.transform.root);
            }
        }

        public static void Init()
        {
            SetupEvents();
        }


        // ────────────────────────────────────────────────────────────────────────────
        //   :::::: S E T U P    E V E N T S : :  :   :    :     :        :          :
        // ────────────────────────────────────────────────────────────────────────────
        //
        public static void SetupEvents()
        {
            if ( !IsConfigured )
            {
                IsConfigured = true;


                ModAPI.OnItemSelected += (sender, phys) => {
                    if ( phys.transform.root.TryGetComponent<NpcBehaviour>( out NpcBehaviour npc ) )
                    {
                        NpcGlobal.LastClicked = npc.NpcId;
                        npc.Selected(sender, phys);
                    }
                };


                ModAPI.OnDeath += (sender, life) => {
                    if ( life.transform.root.TryGetComponent<NpcBehaviour>( out NpcBehaviour npc ) )
                    {
                        if (!npc || !npc.PBO) return;
                        if ( npc.Memory.LastContact)
                        {
                            if (npc.Memory.LastContact.EnhancementTroll)
                            {
                                if ( Mathf.Abs(npc.Memory.LastContact.Head.position.x - npc.Head.position.x ) < 1.5f )
                                {
                                    npc.Memory.LastContact.SayRandom("death");
                                }
                            }
                        }
                        npc.Action.CurrentAction = "Dead";

                        if (NpcGlobal.AllNPC.Contains(npc)) NpcGlobal.AllNPC.Remove( npc );
                        npc.IsDead = true;
                        BroadcastRadius( npc, 10,EventIds.Death,5f);
                        
                    }
                    else if ( life.transform.root.TryGetComponent<PersonBehaviour>( out PersonBehaviour person ) )
                    {
                        if (NpcGlobal.AllPersons.Contains(person)) NpcGlobal.AllPersons.Remove( person );
                    }
                };


                ModAPI.OnItemSpawned += ( sender, args ) =>
                {

                    if ( args.Instance.TryGetComponent<PersonBehaviour>( out PersonBehaviour person ) )
                    {
                        if ( person.TryGetComponent<NpcBehaviour>( out NpcBehaviour npc ) ) NpcGlobal.AllNPC.Add( npc );
                        else NpcGlobal.AllPersons.Add( person );
                    }
                    else if ( args.Instance.TryGetComponent( out PhysicalBehaviour pb ) )
                    {
                        if ( xxx.CanHold( pb ) ) {
                            Props prop = pb.gameObject.GetOrAddComponent<Props>();
                            prop.Init(pb);
                            NpcGlobal.NewItems.Add( pb );

                        }

                    }


                    NpcGlobal.SpawnFrame = Time.frameCount;

                };

                ModAPI.OnItemRemoved += (sender, args) => {

                    if ( args.Instance.TryGetComponent<PersonBehaviour>( out PersonBehaviour person ) )
                    {
                        if ( person.TryGetComponent<NpcBehaviour>( out NpcBehaviour npc ) ) {
                            if (NpcGlobal.AllNPC.Contains(npc)) NpcGlobal.AllNPC.Remove( npc );
                        }
                        else if (NpcGlobal.AllPersons.Contains(person)) NpcGlobal.AllPersons.Remove( person );
                    }
                    else if ( args.Instance.TryGetComponent( out PhysicalBehaviour pb ) )
                    {
                        if (NpcGlobal.AllItems.Contains(pb)) NpcGlobal.AllItems.Remove(pb);
                    }
                
                };
            }
        }

        public static bool CanReportEvent( EventIds eventId )
        {
            if ( EventThrottle.TryGetValue( eventId, out float throttle ) )
            {
                if (Time.time > throttle ) return true;
            }

            return false;
        }

        public static EventInfo GetSetEventInfo( int uniqId )
        {
            if ( !EventLookup.ContainsKey( uniqId ) )
            { 
                EventInfo info = new EventInfo()
                {
                    Locations = new List<Vector2>(),
                    NPCs      = new List<NpcBehaviour>(),
                    PBs       = new List<PhysicalBehaviour>(),
                };
                EventLookup.Add( uniqId, info );
            }

            return EventLookup[uniqId];
        }
        
        public static void SetEventInfo( int UniqId, PhysicalBehaviour item )
        {
            EventInfo info = GetSetEventInfo( UniqId );
            
            EventLookup[UniqId].PBs.Add(item);        
        }

        public static void SetEventInfo( int UniqId, EventIds eventId )
        {
            EventInfo info = GetSetEventInfo( UniqId );
            info.EventId = eventId;

            EventLookup[UniqId] = info;

            EventThrottle[eventId] = Time.time + 5;

        }

        public static void BroadcastRadius( int uniqEventId, Vector3 point, float radius )
        {
            //ModAPI.Notify("Broadcasting EventId: " + uniqEventId + " : " + EventLookup[uniqEventId].EventId.ToString());

            foreach ( KeyValuePair<int, Transform> pair in NPCT )
            {
                if (!NPCs[pair.Key] || !NPCs[pair.Key].PBO) continue;
                //ModAPI.Notify("Checking NPC: " + NPCs[pair.Key].name);
                float dSqr = (point - pair.Value.position).sqrMagnitude;
       
                if ( dSqr < radius )
                {
                    NPCs[pair.Key].EventInfoIds.Add( uniqEventId );
                    //ModAPI.Notify("Notified NPC: " + NPCs[pair.Key].GetHashCode());
                }
            }
        }

        public static void BroadcastRadius( NpcBehaviour sender, float radius, EventIds eventId, float importance)
        {
            return;
            //if (!sender) return;
            //if ()
            //Vector3 pos = sender.transform.position;
            //NpcBehaviour myNpc;

            //foreach ( KeyValuePair<int, Transform> pair in NPCT )
            //{
            //    if (!NPCs[pair.Key] || !NPCs[pair.Key].PBO || !pair.Value) continue;

            //    float dSqr = (pos - pair.Value.position).sqrMagnitude;
       
            //    if ( dSqr < radius )
            //    {
            //        myNpc = NPCs[pair.Key];
            //        if (myNpc == sender) continue;

            //        Message(sender, myNpc, eventId, importance);
            //    }
            //}
        }

        public static void BroadcastEvent( EventInfo eventInfo, Vector3 point, float radius )
        {
            CleanOldEvents();

            foreach ( KeyValuePair<int, EventInfo> pair in EventLookup )
            {
                if (pair.Value.Sender == eventInfo.Sender && pair.Value.EventId == eventInfo.EventId) return;
            }

            if (eventInfo.Expires == 0) eventInfo.Expires = Time.time + 10f;

            int hash = eventInfo.GetHashCode();

            EventLookup[hash] = eventInfo;

            foreach ( KeyValuePair<int, Transform> pair in NPCT )
            {
                if (!NPCs[pair.Key] || !NPCs[pair.Key].PBO || !pair.Value) continue;
      
                float dSqr = (point - pair.Value.position).sqrMagnitude;
       
                if ( dSqr < radius )
                {
                    NPCs[pair.Key].EventInfoIds.Add( hash );
                }
            }
        }


        public static void CleanOldEvents()
        {
            List<int> oldIds = new List<int>();


            foreach ( KeyValuePair<int, EventInfo> pair in EventLookup )
            {
                if (Time.time > pair.Value.Expires) oldIds.Add(pair.Key);
            }

            foreach(int oldId in oldIds) EventLookup.Remove(oldId);

        }

        public static void Broadcast( NpcBehaviour sender, EventIds eventId, float importance)
        {
            NpcBehaviour[] npcs = AllNpcs(sender);

            foreach ( NpcBehaviour npc in npcs ) Message(sender, npc, eventId, importance);
        }


        public static void Message( NpcBehaviour sender, NpcBehaviour npc, EventIds eventId, float importance=1f )
        {
            switch ( eventId )
            {
                case EventIds.Birth:
                    if ( npc.Config.FriendlyTypes != null && npc.Config.FriendlyTypes.Contains(sender.Config.NpcType))
                    {
                        npc.Mojo.Feel("Annoyed",-1 * importance);
                    }
                    else if ( npc.Config.HostileTypes != null && npc.Config.HostileTypes.Contains( sender.Config.NpcType ) )
                    {
                        npc.Mojo.Feel("Annoyed",1 * importance);
                        npc.Mojo.Feel("Angry",1 * importance);
                    }
                    else if ( npc.Config.NpcType == sender.Config.NpcType )
                    {
                        npc.Mojo.Feel("Annoyed",-1);
                    }
                    break;

                case EventIds.Death:
                    if (npc.Config.FriendlyTypes != null && npc.Config.FriendlyTypes.Contains(sender.Config.NpcType))
                    {
                        npc.Mojo.Feel("Annoyed",1 * importance);
                        npc.Mojo.Feel("Angry",1 * importance);
                        npc.Mojo.Feel("Fear",1 * importance);
                    }
                    else if (npc.Config.HostileTypes != null &&  npc.Config.HostileTypes.Contains( sender.Config.NpcType ) )
                    {
                        npc.Mojo.Feel("Angry",-2 * importance);
                    }
                    else if ( npc.Config.NpcType == sender.Config.NpcType )
                    {
                        npc.Mojo.Feel("Annoyed",1);
                        npc.Mojo.Feel("Fear",2 * importance);
                    }
                    else
                    {
                        npc.Mojo.Feel("Fear",0.5f * importance);
                    }
                    break;

                case EventIds.Killed:
                    if ( npc.Config.FriendlyTypes != null && npc.Config.FriendlyTypes.Contains(sender.Config.NpcType))
                    {
                        npc.Mojo.Feel("Annoyed",-1 * importance);
                    }
                    else if ( npc.Config.HostileTypes != null && npc.Config.HostileTypes.Contains( sender.Config.NpcType ) )
                    {
                        npc.Mojo.Feel("Annoyed",1 * importance);
                        npc.Mojo.Feel("Angry",1 * importance);
                    }
                    else if ( npc.Config.NpcType == sender.Config.NpcType )
                    {
                        npc.Mojo.Feel("Annoyed",-1);
                    }
                    else
                    {
                        npc.Mojo.Feel("Annoyed",0.1f * importance);
                        npc.Mojo.Feel("Angry",0.1f * importance);
                    }
                    break;

                case EventIds.Gun:
                    int victimId       = sender.LastLog.NpcId;
                    string victimType  = sender.LastLog.NpcType;
                    float logImportance   = sender.LastLog.Importance;
                    
                    if ( npc.Config.FriendlyTypes != null && npc.Config.FriendlyTypes.Contains(victimType))
                    {
                        npc.Mojo.Feel("Angry",1 * logImportance);
                        npc.Mojo.Feel("Annoyed",1 * logImportance);
                        npc.Mojo.Feel("Fear",1 * logImportance);
                        npc.Mojo.Feel("Tired",-1 * logImportance);
                    }
                    else if ( npc.Config.HostileTypes != null && npc.Config.HostileTypes.Contains( victimType ) )
                    {
                        npc.Mojo.Feel("Annoyed",-1 * logImportance);
                        npc.Mojo.Feel("Angry",-1 * logImportance);
                    }
                    else if ( npc.Config.NpcType == victimType )
                    {
                        npc.Mojo.Feel("Angry",2 * logImportance);
                        npc.Mojo.Feel("Annoyed",2 * logImportance);
                        npc.Mojo.Feel("Fear",2 * logImportance);
                        npc.Mojo.Feel("Tired",-1 * logImportance);
                    }
                    break;

                //case EventIds.Medic:
                //    if (npc.TeamId > 0 && npc.TeamId != sender.TeamId) return;
                //    if ( npc.TeamId == sender.TeamId &&
                //        ( npc.PrimaryAction != NpcPrimaryActions.Fight || npc.PrimaryAction != NpcPrimaryActions.Defend ) )
                //    {
                        
                //    }
                //        break;


                case EventIds.Fire:
                    npc.Mojo.Feel("Fear", 1 * importance);

                    break;

            }
        }


        public static NpcBehaviour[] AllNpcs(NpcBehaviour filterOut=null)
        {
            List<NpcBehaviour> allNpcs = new List<NpcBehaviour>();

            allNpcs.AddRange(UnityEngine.Object.FindObjectsOfType<NpcBehaviour>());

            if (filterOut == null) return allNpcs.ToArray();
            
            allNpcs.RemoveAll(x => x == filterOut);

            return allNpcs.ToArray();
        }
    }
}
