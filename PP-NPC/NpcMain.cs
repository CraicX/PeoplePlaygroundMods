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
using System.Linq;
using System.Collections.Generic;

namespace PPnpc
{
    public class NpcMain
    {
        static bool doSetup = true;
        public static Sprite StatsBG, BlueScreenOfDeath, RedScreen, ChatSpriteBG;
        public static bool DEBUG_MODE    = true;
        public static bool DEBUG_DRAW    = false;
        public static bool DEBUG_LOGGING = false;

        public static Sprite[] eSprites, iSprites, uSprites, ChipEFX;

        public static Dictionary<string, AudioClip[]> SoundBank = new Dictionary<string, AudioClip[]>();

        public static void AddSound( string sfxId, int fileCount=0 )
        {
            bool addCount = fileCount > 0;

            if (!addCount) fileCount = 1;

            AudioClip[] bank = new AudioClip[fileCount];

            string suffix;

            for ( int i = -1; ++i < fileCount; )
            {
                suffix  = addCount ? "_" + (i+1) : "";

                bank[i] = ModAPI.LoadSound("sounds/" + sfxId + suffix + ".mp3");
            }

            SoundBank[ sfxId] = bank;

        }

        public static AudioClip GetSound( string sfxId, int soundPos=-1 )
        {
            if (!SoundBank.ContainsKey(sfxId)) return null;

            if (soundPos <= 0 || SoundBank[sfxId].Length < soundPos) return SoundBank[sfxId].PickRandom();

            return SoundBank[sfxId][soundPos - 1];
        }

        public static void Main()
        {
            if (doSetup)
            {
                //  Create category button for PP NPC
                Category category    = ScriptableObject.CreateInstance<Category>();
                category.name        = "NPC";
                category.Description = "Adds 50 percent less intelligence to characters in People Playground";
                category.Icon        = ModAPI.LoadSprite("img/category-alt.png");

                CatalogBehaviour.Main.Catalog.Categories = CatalogBehaviour.Main.Catalog.Categories.Append(category).ToArray();
                doSetup = false;
            }

            if (DEBUG_MODE) ModAPI.Register<NpcDebug>();


            NpcEvents.IsConfigured = false;

            float scale = 1.0f;

            eSprites = new Sprite[]
            {
                ModAPI.LoadSprite("img/icons/e-aim.png", scale, true),
                ModAPI.LoadSprite("img/icons/e-heal.png", scale, true),
                ModAPI.LoadSprite("img/icons/e-processing.png", scale, true),
                ModAPI.LoadSprite("img/icons/e-strength.png", scale, true),
                ModAPI.LoadSprite("img/icons/e-vision.png", scale, true),
            };

            iSprites = new Sprite[]
            {
                ModAPI.LoadSprite("img/icons/i-binocs.png", scale,true),
                ModAPI.LoadSprite("img/icons/i-died.png", scale,true),
                ModAPI.LoadSprite("img/icons/i-group.png", scale,true),
                ModAPI.LoadSprite("img/icons/i-medic.png", scale,true),
                ModAPI.LoadSprite("img/icons/i-tag.png", scale,true),
            };

            uSprites = new Sprite[]
            {
                ModAPI.LoadSprite("img/icons/u-memory.png", scale, true),
                ModAPI.LoadSprite("img/icons/u-firearms.png", scale, true),
                ModAPI.LoadSprite("img/icons/u-karate.png", scale, true),
                ModAPI.LoadSprite("img/icons/u-melee.png", scale, true),
                ModAPI.LoadSprite("img/icons/u-troll.png", scale, true),
                ModAPI.LoadSprite("img/icons/u-hero.png", scale, true),
            };

            ChipEFX = new Sprite[]
            {
                ModAPI.LoadSprite("img/hero-chip-efx.png", 2.5f, true),

            };


            StatsBG              = ModAPI.LoadSprite("img/hover-stats-panel.png");
            BlueScreenOfDeath    = ModAPI.LoadSprite("img/bluescreen-of-death.png", 4.6f);
            RedScreen			 = ModAPI.LoadSprite("img/angry-screen.png", 4.6f);
            ChatSpriteBG		 = ModAPI.LoadSprite("img/chat-bubble.png", 4.6f);


            Dictionary<string, Sprite> HeroAbilities = new Dictionary<string, Sprite>()
			{
                {"herobody", ModAPI.LoadSprite("img/hero/herobody.png", 5.1f)},
                {"foot", ModAPI.LoadSprite("img/hero/foot.png", 5.1f)},
                {"footfront", ModAPI.LoadSprite("img/hero/footfront.png", 5.1f)},
                {"lowerleg", ModAPI.LoadSprite("img/hero/lowerleg.png", 5.1f)},
                {"lowerlegfront", ModAPI.LoadSprite("img/hero/lowerlegfront.png", 5.1f)},
                {"upperleg", ModAPI.LoadSprite("img/hero/upperleg.png", 5.1f)},
                {"upperlegfront", ModAPI.LoadSprite("img/hero/upperlegfront.png", 5.1f)},
                {"upperarm", ModAPI.LoadSprite("img/hero/upperarm.png", 5.1f)},
                {"lowerarm", ModAPI.LoadSprite("img/hero/lowerarm.png", 5.1f)},
                {"lowerarmfront", ModAPI.LoadSprite("img/hero/lowerarmfront.png", 5.1f)},
                {"upperarmfront", ModAPI.LoadSprite("img/hero/upperarmfront.png", 5.1f)},
                {"lowerbody", ModAPI.LoadSprite("img/hero/lowerbody.png", 5.1f)},
                {"middlebody", ModAPI.LoadSprite("img/hero/middlebody.png", 5.1f)},
                {"upperbody", ModAPI.LoadSprite("img/hero/upperbody.png", 5.1f)},
                {"head", ModAPI.LoadSprite("img/hero/head.png", 5.1f)},
			};


            AddSound("jukebox", 7);
            AddSound("k", 23);
            AddSound("rk", 3);
            AddSound("memory", 2);
            AddSound("karate", 4);
            AddSound("firearms", 5);
            AddSound("troll", 2);
            AddSound("hero", 2);
            AddSound("melee", 2);
            AddSound("no_power");
            AddSound("switch",4);
            AddSound("rebirth",3);
            AddSound("rb_on",3);
            AddSound("rb_off",5);
            AddSound("whistle",2);
            AddSound("change-team");
            AddSound("activating");
            AddSound("success");
            AddSound("chip_connect");
            

            ModAPI.RegisterLiquid(AISyringe.AISerum.ID, new AISyringe.AISerum());

            // ????????????????????????????????????????????????????????????
            //   :::::: AI MICROCHIP
            // ????????????????????????????????????????????????????????????
            //
            ModAPI.Register(new Modification()
            {
                NameOverride          = "AI Microchip",
                NameToOrderByOverride = "a01",
                DescriptionOverride   = @"<color=#0095FF>A cybernetic implant for any <b>human</b> or <b>android</b><br><color=#55B8FF>NZT-48 chipset<br><color=#FF9500>This is <u>not</u> the real <b>NZT-48</b> chip, it is a replica created by <b>Charlie Gordon</b> from refurbished <b>MEOWZT-48</b> chips. His modifications have reclassified the chip as 'not just for cats'.",
                OriginalItem          = ModAPI.FindSpawnable("Plastic Barrel"),
                CategoryOverride      = ModAPI.FindCategory("NPC"),
                ThumbnailOverride     = ModAPI.LoadSprite("img/ai-chip-thumb.png",2.5f),

                AfterSpawn          = (Instance) =>
                {
                    SpriteRenderer sr = Instance.GetComponent<SpriteRenderer>();

                    sr.sprite             = ModAPI.LoadSprite("img/ai-chip.png", 2.5f);
                    sr.sortingOrder = -1;
                    
                    PhysicalBehaviour pb    = Instance.GetComponent<PhysicalBehaviour>();
                    pb.InitialMass          = 0.002f;
                    pb.TrueInitialMass      = 0.002f;
                    pb.Disintegratable      = true;
                    pb.HoldingPositions		= new Vector3[] { new Vector3(0, 0.1f, 0) };
                    pb.ForceNoCharge        = false;
                    pb.ChargeBurns          = true;


                    Instance.FixColliders();

                    NpcChip chip			= Instance.GetOrAddComponent<NpcChip>();

                    chip.PB			        = pb;
                    chip.ChipType           = Chips.AI;

                    //	Add fekn lights
                    chip.Lights = new LightSprite[]
                    {
                        ModAPI.CreateLight(chip.transform, 
                               Color.Lerp(new Color32(0,0,50,255), new Color32(0,0,255,255),Time.fixedDeltaTime * 100),
                               0.225f, 1.5f),

                        ModAPI.CreateLight(chip.transform, 
                          Color.Lerp(new Color32(50,0,0,255), new Color32(255,0,0,255),Time.fixedDeltaTime * 100),
                          0.225f,1.5f),
                    };

                    chip.Lights[0].transform.SetParent(chip.gameObject.transform);
                    chip.Lights[1].transform.SetParent(chip.gameObject.transform);

                    chip.MiscFloats       = new float[]{ Time.time };
                    chip.MiscStrings      = new string[]{"000000", "000000"};
                    chip.MiscColors       = new Color[] { Color.red, Color.green, Color.blue };
                    
                    chip.TeamSelect		  = new GameObject("TeamSelect", typeof(SpriteRenderer));
                    chip.TeamSelect.transform.SetParent(chip.gameObject.transform, false);
                    chip.TeamSelect.transform.localPosition = Vector3.zero;
                    chip.TeamSelect.transform.rotation = chip.gameObject.transform.rotation;

                    chip.SRS              = new SpriteRenderer[] {sr, chip.TeamSelect.GetComponent<SpriteRenderer>() };
                    chip.SRS[1].sprite    = ModAPI.LoadSprite("img/ai-chip-team.png", 2.5f);
                    chip.SRS[1].enabled   = true;
                    
                    chip.TeamSelect.transform.parent = null;

                    chip.AnimateRoutine   = chip.StartCoroutine(chip.IAnimateAIChip());

                }
            });
            

            CreateChip(
                Chips.Memory, "64K Memory", "memory-chip", 2,
                "Adds 64K of RAM allowing AI to recognize people and things from their past.<br><br><color=yellow>WARNING!<br><color=red> May cause AI to remember things they wish they could forget.</color>"
            );

            CreateChip(
                Chips.Firearms, "Firearms Chip Upgrade", "gun-chip", 3,
                "<color=#62FF63>This was a collaberation between <br><b>Gunnery Sergeant Hartman</b> & <b>Private Pyle</b>.<br><color=#41AA42>The chip implants an artificial medulla oblongata (AMO) allowing AI to kill without remorse yet show mercy to anyone that's weak sauce."
            );

            CreateChip(
                Chips.Karate, "Karate Chip Upgrade", "karate-chip", 4,
                "The official chip used by the <b><color=#C2BA7F>YMCA Tae Bo Club</b>.<br><color=#FF6263>Over 10,000 hours of MoCap data analyzed featuring <color=#FF9697>Michael Dudikoff</color> performing all 3 of <b><color=#62FF63>Chuck Norris'</color></b> legendary kicks.<br><color=#8D82FA>(tight jeans required to reach full potential)"
            );

            CreateChip(
                Chips.Melee, "Melee Chip Upgrade", "melee-chip", 5,
                "<color=#FF9500>Developed by the <b><color=##F6A00>Droogs</color></b> to glitch the effects of <color=#8D82FA>aversion therapy</color>.<br><br><color=#FFAD55>Infected AI seen prowling the streets looking to duff up any bloke with <color=red><b>ultra-violence</b></color> and <color=red><b>gusto</b>."
            );

            CreateChip(
                Chips.Troll, "Troll Chip Upgrade", "troll-chip", 6,
                "<color=#7CB342>Gives AI the ability to express themselves and communicate."
            );

            CreateChip(
                Chips.Hero, "Hero Chip Upgrade", "hero-chip", 6,
                "<color=#FF0000>Lets AI be aware of limbs that have been given super powers"
            );


            // ????????????????????????????????????????????????????????????
            //   :::::: PEG BOARD
            // ????????????????????????????????????????????????????????????
            //
            ModAPI.Register(
            new Modification()
            {
                OriginalItem          = ModAPI.FindSpawnable("Plank"),
                NameToOrderByOverride = "c02",
                NameOverride          = "Pegboard",
                DescriptionOverride   = @"<color=#86FA28><b>This thing sucks!</b>  <i><color=#A6591A>(but only when its powered)</i><br><br><color=#86FA28>Great for any base to display weapons that <br>""Fell off a truck"".  Keeps AI armed, and retrieves lost weapons with the power of <color=#AEFB6F><b>Magneto</b></color> and <color=#AEFB6F><b>Hoover</b>",
                CategoryOverride      = ModAPI.FindCategory("NPC"),
                ThumbnailOverride     = ModAPI.LoadSprite("img/pegboard_thumb.png"),
                AfterSpawn            = (Instance) =>
                {
                    SpriteRenderer sr       = Instance.GetOrAddComponent<SpriteRenderer>();
                    sr.sprite               = ModAPI.LoadSprite("img/pegboard.png", 2f);
                    sr.sortingLayerName     = "Background";
                    sr.sortingOrder         = -10;

                    PhysicalBehaviour pb    = Instance.GetComponent<PhysicalBehaviour>();
                    pb.InitialMass          = 0.002f;
                    pb.TrueInitialMass      = 0.002f;
                    pb.Disintegratable      = true;
                    pb.ForceNoCharge        = true;
                    pb.ChargeBurns          = false;

                    

                    NpcArsenal arse			= Instance.GetOrAddComponent<NpcArsenal>();

                    GameObject expansion              = ModAPI.CreatePhysicalObject("Expansion", ModAPI.LoadSprite("img/expansion-slot.png", 0.9f));
                    expansion.transform.SetParent(Instance.transform);
                    expansion.transform.position      = Instance.transform.position;
                    expansion.transform.rotation      = Instance.transform.rotation;
                    expansion.transform.localPosition = new Vector2(0.8f,2.18f);
                    
                    NpcGadget expGadget               = expansion.AddComponent<NpcGadget>();
                    expGadget.Gadget                  = Gadgets.Expansion;
                    

                    FixedJoint2D joint                 = expansion.AddComponent<FixedJoint2D>();
                    joint.autoConfigureConnectedAnchor = true;
                    joint.connectedBody                = Instance.GetComponent<Rigidbody2D>();

                    arse.TeamSelect		  = new GameObject("TeamSelect", typeof(SpriteRenderer));
                    arse.TeamSelect.transform.SetParent(arse.gameObject.transform, false);
                    arse.TeamSelect.transform.localPosition = new Vector2(0.011f,2.15f);
                    arse.TeamSelect.transform.rotation = arse.gameObject.transform.rotation;

                    arse.OnOffLight       = new GameObject("OnOff", typeof(SpriteRenderer));
                    arse.OnOffLight.transform.SetParent(arse.gameObject.transform, false);
                    arse.OnOffLight.transform.localPosition = new Vector2(-0.917f,2.15f);
                    arse.OnOffLight.transform.rotation = arse.gameObject.transform.rotation;

                    arse.Expansion = expGadget;

                    arse.SRS              = new SpriteRenderer[] {
                        sr, 
                        arse.TeamSelect.GetComponent<SpriteRenderer>(), 
                        arse.OnOffLight.GetComponent<SpriteRenderer>() 
                    };


                    arse.SRS[1].sprite    = ModAPI.LoadSprite("img/pegboard-team.png", 2f);
                    arse.SRS[1].enabled   = true;
                    arse.SRS[1].color	  = new Color(0.4f,0.4f,0.4f,1f);

                    arse.SRS[2].sprite    = ModAPI.LoadSprite("img/pegboard-onoff.png", 2f);
                    arse.SRS[2].color	  = new Color(0.4f,0.4f,0.4f,1f);
                    arse.SRS[2].enabled   = true;

                    Instance.FixColliders();
                }
            });

            // ????????????????????????????????????????????????????????????
            //   :::::: SYRINGE
            // ????????????????????????????????????????????????????????????
            //
            ModAPI.Register(
            new Modification()
            {
                OriginalItem          = ModAPI.FindSpawnable("Life Syringe"),
                NameToOrderByOverride = "b01",
                NameOverride          = "AI Syringe",
                DescriptionOverride   = "Contains a mild version of AI Serum. It can be improved.",
                CategoryOverride      = ModAPI.FindCategory("NPC"),
                ThumbnailOverride     = ModAPI.LoadSprite("img/ai-syringe-thumb.png"),
                AfterSpawn            = (Instance) =>
                {
                    Instance.GetOrAddComponent<AISyringe>();
                }
            });


            // ????????????????????????????????????????????????????????????
            //   :::::: SIGNS
            // ????????????????????????????????????????????????????????????
            //
            ModAPI.Register(
            new Modification()
            {
                OriginalItem          = ModAPI.FindSpawnable("Metal Pole"),
                NameToOrderByOverride = "d01",
                NameOverride          = "No Fighting Sign",
                DescriptionOverride   = "Designate areas where fighting is prohibited",
                CategoryOverride      = ModAPI.FindCategory("NPC"),
                ThumbnailOverride     = ModAPI.LoadSprite("img/no-fighting-thumb.png", 3.5f),
                AfterSpawn            = (Instance) =>
                {
                    SpriteRenderer sr	    = Instance.GetComponent<SpriteRenderer>();
                    sr.sprite               = ModAPI.LoadSprite("img/no-fighting.png", 2.0f);
                    sr.sortingLayerName     = "Background";
                    sr.sortingOrder         = -10;
                    PhysicalBehaviour pb    = Instance.GetComponent<PhysicalBehaviour>();

                    GameObject arrow = new GameObject("Arrow", typeof(SpriteRenderer));

                    SpriteRenderer arsr = arrow.GetComponent<SpriteRenderer>();
                    arsr.sprite = ModAPI.LoadSprite("img/arrow.png", 4.0f);
                    arrow.transform.SetParent(Instance.transform, false);

                    if ( Instance.transform.localScale.x < 0f )
					{
                        Instance.transform.localScale = Vector3.one;
                        arrow.transform.localScale    = new Vector3(-1f,1f,1f);
					}

                    Instance.FixColliders();
                    
                    NpcGadget gadget  = arrow.AddComponent<NpcGadget>();
                    if (arrow.transform.localScale.x < 0f) gadget.SignLeft = true;
                    gadget.Gadget     = Gadgets.NoFightSign;
                    gadget.SetupSign();

                }
            });


            ModAPI.Register(
            new Modification()
            {
                OriginalItem          = ModAPI.FindSpawnable("Metal Pole"),
                NameToOrderByOverride = "d02",
                NameOverride          = "No Guns Sign",
                DescriptionOverride   = "Designate areas where Guns are prohibited",
                CategoryOverride      = ModAPI.FindCategory("NPC"),
                ThumbnailOverride     = ModAPI.LoadSprite("img/no-guns-thumb.png", 3.5f),
                AfterSpawn            = (Instance) =>
                {
                    SpriteRenderer sr	    = Instance.GetComponent<SpriteRenderer>();
                    sr.sprite               = ModAPI.LoadSprite("img/no-guns.png", 4.0f);
                    sr.sortingLayerName     = "Background";
                    sr.sortingOrder         = -10;
                    PhysicalBehaviour pb    = Instance.GetComponent<PhysicalBehaviour>();

                    GameObject arrow = new GameObject("Arrow", typeof(SpriteRenderer));

                    SpriteRenderer arsr = arrow.GetComponent<SpriteRenderer>();
                    arsr.sprite = ModAPI.LoadSprite("img/arrow.png", 4.0f);
                    arrow.transform.SetParent(Instance.transform, false);

                    if ( Instance.transform.localScale.x < 0f )
					{
                        
                        Instance.transform.localScale = Vector3.one;
                        arrow.transform.localScale    = new Vector3(-1f,1f,1f);
					}

                    Instance.FixColliders();
                    
                    NpcGadget gadget               = arrow.AddComponent<NpcGadget>();
                    gadget.Gadget                  = Gadgets.NoFightSign;

                    if (arrow.transform.localScale.x < 0f) gadget.SignLeft = true;

                    gadget.SetupSign();

                }
            });



            // ????????????????????????????????????????????????????????????
            //   :::::: REBIRTHER
            // ????????????????????????????????????????????????????????????
            //
            ModAPI.Register(new Modification()
            {
                NameOverride                = "Rebirther",
                NameToOrderByOverride       = "c01",
                DescriptionOverride         = "A cheap unreliable respawner. Use correct power. <color=yellow>WARNING!<color=orange> AI should use at own risk. For novelty purposes only.",
                OriginalItem                = ModAPI.FindSpawnable("Small I-Beam"),
                CategoryOverride            = ModAPI.FindCategory("NPC"),
                ThumbnailOverride           = ModAPI.LoadSprite("img/rebirther-thumb.png",2.5f),

                AfterSpawn					= (Instance) =>
                {
                    SpriteRenderer sr	    = Instance.GetComponent<SpriteRenderer>();
                    sr.sprite               = ModAPI.LoadSprite("img/rebirther.png", 0.9f);
                    sr.sortingLayerName     = "Background";
                    sr.sortingOrder		    = -1;


                    PhysicalBehaviour pb    = Instance.GetComponent<PhysicalBehaviour>();
                    pb.InitialMass          = 15.2f;
                    pb.TrueInitialMass      = 15.2f;
                    pb.Disintegratable      = true;
                    pb.ForceNoCharge        = true;
                    pb.ChargeBurns          = false;

                    NpcRebirther rebirther  = Instance.AddComponent<NpcRebirther>();


                    GameObject expansion              = ModAPI.CreatePhysicalObject("Expansion", ModAPI.LoadSprite("img/expansion-slot.png", 0.9f));
                    expansion.transform.parent        = Instance.transform;
                    expansion.transform.position      = Instance.transform.position;
                    expansion.transform.rotation      = Instance.transform.rotation;
                    expansion.transform.localPosition = new Vector2(0.6f,1.54f);

                    NpcGadget expGadget               = expansion.AddComponent<NpcGadget>();
                    expGadget.Gadget                  = Gadgets.Expansion;

                    rebirther.Expansion				   = expGadget;

                    FixedJoint2D joint                 = expansion.AddComponent<FixedJoint2D>();
                    joint.autoConfigureConnectedAnchor = true;
                    joint.connectedBody                = Instance.GetComponent<Rigidbody2D>();

                    SpriteRenderer sr3                 = expansion.GetOrAddComponent<SpriteRenderer>();



                    rebirther.TeamSelect   = new GameObject("TeamSelect");
                    rebirther.TeamSelect.transform.SetParent(Instance.transform, false);
                    rebirther.TeamSelect.transform.localPosition = new Vector2(0.022f,0.06f);


                    SpriteRenderer sr2      = rebirther.TeamSelect.GetOrAddComponent<SpriteRenderer>();
                    sr2.sprite              = ModAPI.LoadSprite("img/rebirther-team.png", 0.95f);
                    sr2.enabled             = true;
                    sr2.sortingOrder        = 1;
                    sr2.sortingLayerName    = "Background";
                    sr2.color               = new Color(0.0f,0.0f,0.0f,0.0f);
                    
                    rebirther.TeamSelect.SetActive(true);
                    rebirther.TeamSelect.transform.parent = null;

                    rebirther.SRS = new SpriteRenderer[] {sr,sr2,sr3};
                    rebirther.StartCoroutine(rebirther.IAnimateRebirther());

                    Instance.FixColliders();

                }
            });
        }

        public static void CreateChip( Chips _chipType, string _name, string _imgName, int position, string _desc  )
        {
            ModAPI.Register(new Modification()
            {
                NameOverride          = _name,
                NameToOrderByOverride = "a0" + position,
                DescriptionOverride   = _desc,
                OriginalItem          = ModAPI.FindSpawnable("Plastic Barrel"),
                CategoryOverride      = ModAPI.FindCategory("NPC"),
                ThumbnailOverride     = ModAPI.LoadSprite("img/" + _imgName + "-thumb.png",2.5f),

                AfterSpawn            = (Instance) =>
                {
                    SpriteRenderer sr = Instance.GetOrAddComponent<SpriteRenderer>();

                    sr.sprite               = ModAPI.LoadSprite("img/" + _imgName + ".png", 2.5f);
                    sr.sortingOrder         = -1;

                    PhysicalBehaviour pb    = Instance.GetComponent<PhysicalBehaviour>();
                    pb.InitialMass          = 0.002f;
                    pb.TrueInitialMass      = 0.002f;
                    pb.Disintegratable      = true;
                    pb.HoldingPositions		= new Vector3[] { new Vector3(0, 0.1f, 0) };


                    NpcChip chip			= Instance.GetOrAddComponent<NpcChip>();
                    chip.PB			        = pb;

                    chip.Init(_chipType);
                    Instance.FixColliders();

                }
            });
        }
    }

    [SkipSerialisation]
    public class NpcDebug : MonoBehaviour
    {
        static bool AllSpawn    = false;
        static bool AllSpawnSet = false;
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8)) {
                AllSpawn = !AllSpawn;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    ModAPI.Notify("NPC DEBUG: All NPC Spawn");
                    if ( !AllSpawnSet )
                    {
                        AllSpawnSet = true;
                        ModAPI.OnItemSpawned += ( sender, args ) =>
                        {
                            if ( AllSpawn && args.Instance.TryGetComponent<PersonBehaviour>( out PersonBehaviour person ) )
                            {
                                NpcBehaviour npc             = person.gameObject.GetOrAddComponent<NpcBehaviour>();
                                npc.enabled                  = true;
                                npc.EnhancementFirearms      = true;
                                npc.EnhancementKarate        = true;
                                npc.EnhancementMelee         = true;
                                npc.EnhancementMemory        = true;
                                npc.EnhancementTroll         = true;
                            }
                        };
                    }
                }
                else {
                    NpcMain.DEBUG_DRAW = !NpcMain.DEBUG_DRAW;
                    if (NpcMain.DEBUG_DRAW) ModAPI.Notify("NPC DEBUG: View Line of sight");
                }
            }
        }

        void Start()
        {
            NpcMain.DEBUG_LOGGING = false;
            AllSpawnSet           = AllSpawn = false;
        }
    }
}
