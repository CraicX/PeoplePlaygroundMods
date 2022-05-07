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
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace PPnpc
{
    public class NpcMain
    {
        static bool doSetup = true;
        public static AudioClip AudioSuccess;
        public static Sprite StatsBG;

        public static void OnLoad()
        {
        }

        public static void Main()
        {
            if (doSetup)
            {
                StatsBG      = ModAPI.LoadSprite("img/hover-stats-panel.png");
                AudioSuccess = ModAPI.LoadSound("sounds/success.mp3");
                doSetup      = false;
                //  Create category button for PP NPC
                Category category    = ScriptableObject.CreateInstance<Category>();
                category.name        = "NPC";
                category.Description = "Adds 50 percent less intelligence to characters in People Playground";
                category.Icon        = ModAPI.LoadSprite("img/category-alt.png");

                CatalogBehaviour.Main.Catalog.Categories = CatalogBehaviour.Main.Catalog.Categories.Append(category).ToArray();
            }

            ModAPI.RegisterLiquid(AISyringe.AISerum.ID, new AISyringe.AISerum());

            ModAPI.Register(new Modification()
            {
                NameOverride        = "AI Microchip",
                DescriptionOverride = "A basic cybernetic implant for any ordinary NPC",
                OriginalItem        = ModAPI.FindSpawnable("Plastic Barrel"),
                CategoryOverride    = ModAPI.FindCategory("NPC"),
                ThumbnailOverride   = ModAPI.LoadSprite("img/ai-chip-thumb.png",2.5f),
                
                AfterSpawn          = (Instance) =>
                {
                    SpriteRenderer sr = Instance.GetComponent<SpriteRenderer>();

                    sr.sprite             = ModAPI.LoadSprite("img/ai-chip.png", 2.5f);
                    
                    GameObject TeamSelect = new GameObject("TeamSelect");
                    TeamSelect.transform.SetParent(Instance.transform, false);


                    SpriteRenderer sr2 = TeamSelect.GetOrAddComponent<SpriteRenderer>();
                    sr2.sprite            = ModAPI.LoadSprite("img/ai-chip-team.png", 2.5f);
                    sr2.enabled = true;
                    sr2.sortingOrder = 1;
                    //sr2.sortingLayerID = SortingLayer.NameToID("Top");
                    //TeamSelect.SetLayer(LayerMask.NameToLayer("Objects"));
                    TeamSelect.SetActive(true);
                    PhysicalBehaviour pb            = Instance.GetComponent<PhysicalBehaviour>();
                    pb.InitialMass                  = 0.002f;
                    pb.TrueInitialMass              = 0.002f;
                    pb.Disintegratable              = true;

                    Instance.FixColliders();

                    NpcGadget gadget = Instance.AddComponent<NpcGadget>();

                    gadget.AudioClips = new AudioClip[4];
                    gadget.AudioClips[0] = ModAPI.LoadSound("sounds/change-team.mp3");
                    gadget.AudioClips[1] = ModAPI.LoadSound("sounds/activating.mp3");
                    gadget.AudioClips[2] = ModAPI.LoadSound("sounds/success.mp3");
                    gadget.AudioClips[3] = ModAPI.LoadSound("sounds/problem.mp3");

                    gadget.Init(Gadgets.AIChip);

                }
            });


            ModAPI.Register(
            new Modification()
            {
                OriginalItem = ModAPI.FindSpawnable("Life Syringe"),
                NameOverride = "AI Syringe",
                DescriptionOverride = "Contains a mild version of AI Serum. It can be improved.",
                CategoryOverride = ModAPI.FindCategory("NPC"),
                ThumbnailOverride = ModAPI.LoadSprite("img/ai-syringe-thumb.png"),
                AfterSpawn = (Instance) =>
                {
                    Instance.GetOrAddComponent<AISyringe>();
                }
            });
        }
    }
}
