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
using System.Linq;
using UnityEngine;

namespace PPnpc
{
    [SkipSerialisation]
    public class Props : MonoBehaviour
    {
        public PhysicalBehaviour P;

        public int objId;

        public float size = 0;
        public float angleOffset  = 0f;
        public float angleHold    = 0f;
        public float angleAim     = 95f;

        public bool holdToSide    = false;
        public bool isWeapon      = false;
        public bool isEnergySword = false;
        public bool isFlashlight  = false;
        public bool isShiv        = false;
        public bool isSyringe     = false;
        public bool isMedic       = false;
        public bool isChainSaw    = false;
        public bool isAutomatic   = false;
        public bool isRocket      = false;
        public bool isGrenade     = false;
        
        public bool protectFromFire;
        
        public bool canFightFire  = false;
        public bool canSlice      = false;
        public bool canStab       = false;
        public bool canAim        = false;
        public bool canShoot      = false;
        public bool canStrike     = false;
        public bool canExplode    = false;
        
        public bool lockWeights   = false;

        //public bool xtraLarge;
        public bool needsTwoHands = false;
        public bool xtraLarge     = false;

        public AimStyles CurrentAimStyle = AimStyles.Standard;

        public Dictionary<AimStyles, int> aimStyles = new Dictionary<AimStyles, int>();

        public Dictionary<string, bool> Traits = new Dictionary<string, bool>()
        {
            {"extinguisher", false },
            {"flashlight", false },
            {"knife", false },
            {"club", false },
            {"sword", false },
            {"firework", false },
            {"grenade", false },
            {"explosive", false },
            {"gun", false },
            {"rocket", false },
        };

        
        public AimStyles AimStyle()
        {
            if (aimStyles.Count == 0) { CurrentAimStyle = AimStyles.Standard; return CurrentAimStyle; }
            if (aimStyles.Count == 1) { CurrentAimStyle =  aimStyles.Keys.First(); return CurrentAimStyle; }
            int maxRand = 0;

            foreach ( KeyValuePair<AimStyles, int> pair in aimStyles )
            {
                maxRand += pair.Value;
            }

            int idx = xxx.rr(0,maxRand);

            maxRand = 0;

            foreach ( KeyValuePair<AimStyles, int> pair in aimStyles )
            {
                maxRand += pair.Value;
                if (idx <= maxRand) { CurrentAimStyle = pair.Key; return CurrentAimStyle; }
            }

            CurrentAimStyle = aimStyles.Keys.First();
            return CurrentAimStyle;
        }

        public void AddStyle( AimStyles aimStyle, int weight, bool clearStyles=false, bool lockStyles=false )
        {
            if (lockWeights) return;
            if (clearStyles) aimStyles.Clear();

            if (aimStyles.TryGetValue(aimStyle, out _)) aimStyles[aimStyle] += weight;
            else aimStyles[aimStyle] = weight;
            
            if (lockStyles) lockWeights = true;
        }

        public void Init(PhysicalBehaviour pb)
        {
            P = pb;

            objId = P.name.GetHashCode();

            const float SmallShivLength     = 1.25f;
            const float NeedsTwoHandsLength = 1.30f;

            string itemName = P.name.ToLower();

            canFightFire = canSlice = canStab = canAim = canShoot = canStrike = false;
            isWeapon = isEnergySword = isFlashlight = isShiv = isSyringe = isMedic = isChainSaw = isAutomatic = false;

            angleHold   = 0f;
            angleOffset = 0f;

            protectFromFire = false;

            needsTwoHands = false;


            //  Loop through items components and check for behaviour classes that identify if its auto or manual

            string AutoFire         = @"ACCELERATORGUNBEHAVIOUR,FLAMETHROWERBEHAVIOUR,PHYSICSGUNBEHAVIOUR,MINIGUNBEHAVIOUR,TEMPERATURERAYGUNBEHAVIOUR";
            string ManualFire       = @"ARCHELIXCASTERBEHAVIOUR,BEAMFORMERBEHAVIOUR,ROCKETLAUNCHERBEHAVIOUR,GENERICSCIFIWEAPON40BEHAVIOUR,
                                        LIGHTNINGGUNBEHAVIOUR,PULSEDRUMBEHAVIOUR,HEALTHGUNBEHAVIOUR,FREEZEGUNBEHAVIOUR,SINGLEFLOODLIGHTBEHAVIOUR";
            string FireProtection   = @"ENERGYSWORDBEHAVIOUR,FLAMETHROWERBEHAVIOUR";
            string EnergySword      = @"ENERGYSWORDBEHAVIOUR";

            string Rockets          = @"ROCKETLAUNCHERBEHAVIOUR,";

            MonoBehaviour[] Components = P.GetComponents<MonoBehaviour>();

            if (Components.Length > 0)
            {
                for (int i = Components.Length; --i >= 0;)
                {
                    string compo = Components[i].GetType().ToString().ToUpper();

                    if (EnergySword.Contains(compo)) isEnergySword      = true;
                    if (FireProtection.Contains(compo)) protectFromFire = true;
                    
                    if (AutoFire.Contains(compo))
                    {
                        canShoot    = true;
                        isAutomatic = true;
                    }

                    if (ManualFire.Contains(compo))
                    {
                        canShoot    = true;
                        isAutomatic = false;
                    }

                    if ( Rockets.Contains( compo ) )
                    {
                        ModAPI.Notify("Rocket Launcher");
                        isRocket         = true;
                        Traits["Rocket"] = true;
                        AddStyle(AimStyles.Rockets,2,true,true);
                    }
                }
            }

            CanShoot[] ShootComponents = P.GetComponents<CanShoot>();

            if ( ShootComponents.Length > 0 )
            {
                canShoot    = true;
            }

            //  @Todo: need to return modified values to their OG after they're dropped

            //  Determine if we can do auto firing

            if (P.TryGetComponent(out FirearmBehaviour FBH))
            {
                canShoot                = true;
                isAutomatic             = FBH.Automatic;
                FBH.Cartridge.Recoil    = 0.1f;
                Traits["gun"]        = true;

                AddStyle(AimStyles.Standard,5);
                AddStyle(AimStyles.Crouched,3);
                AddStyle(AimStyles.Proned,1);
            }
            else if (P.TryGetComponent(out ProjectileLauncherBehaviour PLB))
            {
                canShoot             = true;
                isAutomatic          = PLB.IsAutomatic;
                PLB.recoilMultiplier = 0.1f;

                AddStyle(AimStyles.Standard,5);
                AddStyle(AimStyles.Crouched,5);
                AddStyle(AimStyles.Proned,2);
            }
            else if (P.TryGetComponent(out BlasterBehaviour BB))
            {
                canShoot             = true;
                isAutomatic          = BB.Automatic;
                BB.Recoil            = 0.1f;
                Traits["gun"]     = true;

                AddStyle(AimStyles.Standard,1, true);
                AddStyle(AimStyles.Crouched,4);
                AddStyle(AimStyles.Proned,2,false,true);

            }
            else if (P.TryGetComponent(out BeamformerBehaviour BB2))
            {
                BB2.RecoilForce               = 0.1f;
                Traits["rocket"]              = true;
                
                AddStyle(AimStyles.Rockets,2,true,true);
            }
            else if (P.TryGetComponent(out GenericScifiWeapon40Behaviour GWB))
            {
                GWB.RecoilForce     = 0.1f;
            }
            //else if (P.TryGetComponent(out AcceleratorGunBehaviour AB))
            //{
            //    AB.RecoilIntensity = 0.1f;
            //    canShoot           = true;
            //    isAutomatic        = true;
            //}
            //else if (P.TryGetComponent<ArchelixCasterBehaviour>(out _))
            //{
            //    canShoot    = true;
            //    isAutomatic = false;
            //}
            else if (P.TryGetComponent<SingleFloodlightBehaviour>(out _))
            {
                isFlashlight = true;
                angleHold    = 95f;
                angleAim     = 180f;
                canAim       = true;
                Traits["flashlight"]        = true;
            } 

            if ( P.name.ToLower().Contains( "grenade launch" ) )
            {
                canFightFire = false;
                canShoot     = true;
                Traits["rocket"]        = true;
                AddStyle(AimStyles.Rockets,2,true,true);

            }
            
            if ( P.name.ToLower().Contains( "extinguisher" ) )
            {
                canFightFire = true;
                canShoot     = false;
                Traits["extinguisher"]        = true;
                AddStyle(AimStyles.Spray,2,true,true);

            }

            size             = P.ObjectArea;

            xtraLarge   = size > 1.2f;
            
            //  Check for clubs/bats
            if (!canShoot)
            {
                canStab         = (P.Properties.Sharp && P.StabCausesWound) || isSyringe;
                canStrike       = true;
                
                canSlice        = P.TryGetComponent<SharpOnAllSidesBehaviour>(out _);

                if (canStab) {
                    if (P.ObjectArea < SmallShivLength) {
                        isShiv = true;
                        Traits["knife"]        = true;
                    } else
                    {
                        if (canStab) Traits["sword"] = true;
                    }
                }


            } else
            {
                canAim      = true;
                if (size > NeedsTwoHandsLength) needsTwoHands = true;
            }

            if (itemName.Contains("syringe"))
            {
                angleOffset = -95f;
                isSyringe   = true;
                isShiv      = true;
                Traits["syringe"]        = true;

                //InvSFX = (AudioClip)null;
            }
            else if (itemName.Contains("crystal"))
            {
                angleOffset = -95f;
                //InvSFX = (AudioClip)null;
            }
            else if (itemName.Contains("bulb"))
            {
                isShiv      = true;
                angleOffset = 180f;
                //InvSFX = (AudioClip)null;
            }
            else if (itemName.Contains("stick"))
            {
                canStrike    = true;
                angleOffset  = 180f;
                Traits["club"]        = true;
            }
            else if (itemName.Contains("rod"))
            {
                canStrike   = true;
                angleOffset = 180f;
                Traits["club"]        = true;
            }
            else if (itemName.Contains("bolt"))
            {
                isShiv      = true;
                angleOffset = -90f;
            }

            if (P.TryGetComponent<ChainsawBehaviour>(out _))
            {
                isChainSaw  = true;
                canStab     = false;
                canSlice    = false;
                canStrike   = false;
            }
        }
    }
}
