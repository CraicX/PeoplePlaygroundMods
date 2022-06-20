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
	public class NpcPose
	{
		public NpcBehaviour NPC;
		public int poseID        = -1;
		public string MovesList  = "";
		public bool doReset      = false;
		public bool autoStart    = false;
		public bool comboMove    = false;

		public readonly RagdollPose Ragdoll;

		public Dictionary<string, LimbMove> moves = new Dictionary<string, LimbMove>();

		public string MoveHash => "NPC" + MovesList.GetHashCode();

		public struct LimbMove
		{
			public string limbName;
			public float angle;
			public float rigid;
			public float force;
	   }

		//
		// ─── NEW MOVESET ────────────────────────────────────────────────────────────────
		//
		public NpcPose(NpcBehaviour npc, string movesList, bool autoStart=true, RagdollPose presetPose=null)
		{
			NPC       = npc;
			MovesList = movesList;
			Ragdoll   = new RagdollPose()
			{
				Name                     = MoveHash,
				Rigidity                 = 4,
				ShouldStandUpright       = true,
				DragInfluence            = 1,
				UprightForceMultiplier   = 1,
				ShouldStumble            = false,
				State                    = PoseState.Rest,
				AnimationSpeedMultiplier = 1,
				Angles                   = new List<RagdollPose.LimbPose>(),
			};

			if (presetPose != null)
			{
				Ragdoll      = presetPose.ShallowClone();
				Ragdoll.Name = MoveHash;
				comboMove    = true;
			}

			if (autoStart) Import();
		}


		public string[] GetPoseData(string movesList)
		{
			if (movesList.Contains(",")) return MovesList.Trim().Split(',');

			if (MoveData.ContainsKey(movesList)) return MoveData[movesList].Split(',');

			return null;

		}

		//
		// ─── IMPORT MOVESET ────────────────────────────────────────────────────────────────
		//
		public void Import()
		{
			string[] mvItem   = GetPoseData(MovesList);
			bool enableArmAim = true;

			for (int i = mvItem.Length; --i >= 0;)
			{
				string[] parts  = mvItem[i].Trim().Split(':');
				if (parts.Length < 2) continue;

				//  Skip commented out limbs
				if ("#/".Contains("" + parts[0][0])) continue;

				if (parts[0].Contains("Arm")) enableArmAim = false;

				LimbMove move   = new LimbMove()
				{
					limbName    = parts[0],
					angle       = 0f,
					force       = 4f,
					rigid       = 0f,
				};

				if (float.TryParse(parts[1], out float angle)) move.angle = angle;

				if (parts.Length > 2) { 
					for (int n = parts.Length; --n >= 2;)
					{
						string item = parts[n].Trim();

						if (item.StartsWith("f")      && float.TryParse(parts[n].Substring(1), out float force)) move.force = force;
						else if (item.StartsWith("r") && float.TryParse(parts[n].Substring(1), out float rigid)) move.rigid = rigid;
					}
				}

				moves.Add(parts[0], move);
			}

			RagdollPose.LimbPose limbPose;

			bool includeMove;

			foreach (LimbBehaviour limb in NPC.PBO.Limbs)
			{
				includeMove = false;

				if (limb.HasJoint)
				{
					if (moves.ContainsKey(limb.name))
					{
						includeMove = true;
					}
				}

				if (includeMove)
				{
					limbPose = new RagdollPose.LimbPose(limb, moves[limb.name].angle)
					{
						Name                 = MoveHash,
						Limb                 = limb,
						Animated             = false,
						PoseRigidityModifier = moves[limb.name].rigid,
						StartAngle           = 0f,
						EndAngle             = 0f,
						AnimationDuration    = 0f,
						RandomInfluence      = 0f,
						RandomSpeed          = 0f,
						TimeOffset           = 0f,
						AnimationCurve       = new AnimationCurve(),
					};
				}
				else if(!comboMove)
				{
					limbPose = new RagdollPose.LimbPose(limb,0f)
					{
						Name                 = MoveHash,
						Limb                 = limb,
						Animated             = false,
						PoseRigidityModifier = 0f,
						Angle                = 0f,
						StartAngle           = 0f,
						EndAngle             = 0f,
						AnimationDuration    = 0f,
						RandomInfluence      = 0f,
						RandomSpeed          = 0f,
						TimeOffset           = 0f,
						AnimationCurve       = new AnimationCurve(),
					};
				} else continue;

				if (comboMove)
				{
					for (int i = Ragdoll.Angles.Count; --i >= 0;)
					{
						if (Ragdoll.Angles[i].Limb == limb)
						{
							Ragdoll.Angles[i] = limbPose;
						}
					}
				} else 
				{
					Ragdoll.Angles.Add(limbPose);
				}
			}

			Ragdoll.ConstructDictionary();

			for (int pnum = NPC.PBO.Poses.Count; --pnum >= 0;)
			{
				if (NPC.PBO.Poses[pnum].Name == MoveHash)
				{
					poseID = pnum;

					NPC.PBO.Poses[pnum] = Ragdoll;

					return;
				}
			}

			NPC.PBO.Poses.Add(Ragdoll);

			poseID = NPC.PBO.Poses.Count -1;
			if ( enableArmAim )
			{
				NPC.PBO.Poses[poseID].AngleDictionary[NPC.BH.LB]       = NPC.BH.PoseL;
				NPC.PBO.Poses[poseID].AngleDictionary[NPC.BH.uArmL]    = NPC.BH.PoseU;
				NPC.PBO.Poses[poseID].AngleDictionary[NPC.FH.LB]       = NPC.FH.PoseL;
				NPC.PBO.Poses[poseID].AngleDictionary[NPC.FH.uArmL]    = NPC.FH.PoseU;
			}

		}


		public bool RunMove()
		{
			NPC.PBO.OverridePoseIndex = poseID;
			return true;
		}


		//
		// ─── COMBINE MOVESET ────────────────────────────────────────────────────────────────
		//
		public void CombineMove()
		{
			//  Only pose applicable limbs (continue previous actions)
			foreach (KeyValuePair<string, LimbMove> pair in moves)
			{
				NPC.LB[pair.Key].InfluenceMotorSpeed(Mathf.DeltaAngle(
					NPC.LB[pair.Key].Joint.jointAngle, pair.Value.angle * NPC.PBO.AngleOffset) * pair.Value.force);

			}

		}



		public static void Clear(PersonBehaviour pb)
		{
			if (!pb) return;
			foreach ( LimbBehaviour limb in pb.Limbs )
			{
				if (!limb) continue;
				limb.Health                = limb.InitialHealth;
				limb.Numbness              = 0.0f;
				limb.HealBone();
				limb.CirculationBehaviour.BleedingRate  *= 0.5f;
				limb.PhysicalBehaviour.BurnProgress     *= 0.5f;
				limb.SkinMaterialHandler.AcidProgress   *= 0.5f;
				limb.SkinMaterialHandler.RottenProgress *= 0.5f;
			}
			pb.OverridePoseIndex = -1;
		}

		public void ClearMove()
		{
			foreach (string limbName in moves.Keys)
			{
				if (NPC.LB?[limbName])
				{
					NPC.LB[limbName].Health                = NPC.LB[limbName].InitialHealth;
					NPC.LB[limbName].Numbness              = 0.0f;
					NPC.LB[limbName].HealBone();

					NPC.LB[limbName].CirculationBehaviour.BleedingRate  *= 0.5f;
					NPC.LB[limbName].PhysicalBehaviour.BurnProgress     *= 0.5f;
					NPC.LB[limbName].SkinMaterialHandler.AcidProgress   *= 0.5f;
					NPC.LB[limbName].SkinMaterialHandler.RottenProgress *= 0.5f;
				}
			}

			NPC.PBO.OverridePoseIndex = -1;
		}


		public static RagdollPose.LimbPose FreeLimbPose( LimbBehaviour limb, bool setPose=false )
		{
			RagdollPose.LimbPose limbPose = new RagdollPose.LimbPose(limb,0f)
			{
				Name                 = "clear",
				Limb                 = limb,
				Animated             = false,
				PoseRigidityModifier = 0f,
				Angle                = 0f,
				StartAngle           = 0f,
				EndAngle             = 0f,
				AnimationDuration    = 0f,
				RandomInfluence      = 0f,
				RandomSpeed          = 0f,
				TimeOffset           = 0f,
				AnimationCurve       = new AnimationCurve(),
			};

			if (setPose) { 

				limb.IsActiveInCurrentPose = false;
				limb.Person.ActivePose.AngleDictionary[limb] = limbPose;
			}

			return limbPose;

		}

		public static Dictionary<string, NpcPose> SetCommonPoses(NpcBehaviour npcx)
		{
			Dictionary<string, NpcPose> CommonPoses = new Dictionary<string, NpcPose>();

			CommonPoses["crouch"]	= new NpcPose(npcx, "crouch", false);
			CommonPoses["crouch"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["crouch"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["crouch"].Ragdoll.Rigidity                 = 1.7f;
			CommonPoses["crouch"].Ragdoll.AnimationSpeedMultiplier = 2.5f;
			CommonPoses["crouch"].Ragdoll.UprightForceMultiplier   = 1.4f;
			CommonPoses["crouch"].Import();

			CommonPoses["takecover"]	= new NpcPose(npcx, "takecover", false);
			CommonPoses["takecover"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["takecover"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["takecover"].Ragdoll.Rigidity                 = 1.7f;
			CommonPoses["takecover"].Ragdoll.AnimationSpeedMultiplier = 2.5f;
			CommonPoses["takecover"].Ragdoll.UprightForceMultiplier   = 1.4f;
			CommonPoses["takecover"].Import();

			CommonPoses["survive"]	= new NpcPose(npcx, "survive", false);
			CommonPoses["survive"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["survive"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["survive"].Ragdoll.Rigidity                 = 1.7f;
			CommonPoses["survive"].Ragdoll.AnimationSpeedMultiplier = 2.5f;
			CommonPoses["survive"].Ragdoll.UprightForceMultiplier   = 1.4f;
			CommonPoses["survive"].Import();

			//	Build Prone Pose
			CommonPoses["prone"]	= new NpcPose(npcx, "prone", false);
			CommonPoses["prone"].Ragdoll.ShouldStandUpright       = false;
			CommonPoses["prone"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["prone"].Ragdoll.Rigidity                 = 2.3f;
			CommonPoses["prone"].Ragdoll.AnimationSpeedMultiplier = 2.5f;
			CommonPoses["prone"].Ragdoll.UprightForceMultiplier   = 0f;
			CommonPoses["prone"].Import();

			CommonPoses["dive"]	= new NpcPose(npcx, "dive", false);
			CommonPoses["dive"].Ragdoll.ShouldStandUpright       = false;
			CommonPoses["dive"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["dive"].Ragdoll.Rigidity                 = 2.3f;
			CommonPoses["dive"].Ragdoll.AnimationSpeedMultiplier = 2.5f;
			CommonPoses["dive"].Ragdoll.UprightForceMultiplier   = 0f;
			CommonPoses["dive"].Import();

			//	Build Fight Poses
			CommonPoses["frontkick"]	= new NpcPose(npcx, "frontkick", false);
			CommonPoses["frontkick"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["frontkick"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["frontkick"].Ragdoll.Rigidity                 = 3.3f;
			CommonPoses["frontkick"].Ragdoll.AnimationSpeedMultiplier = 4.5f;
			CommonPoses["frontkick"].Ragdoll.UprightForceMultiplier   = 4f;
			CommonPoses["frontkick"].Import();

			CommonPoses["soccerkick"]	= new NpcPose(npcx, "soccerkick", false);
			CommonPoses["soccerkick"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["soccerkick"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["soccerkick"].Ragdoll.Rigidity                 = 4.3f;
			CommonPoses["soccerkick"].Ragdoll.AnimationSpeedMultiplier = 5.5f;
			CommonPoses["soccerkick"].Ragdoll.UprightForceMultiplier   = 4f;
			CommonPoses["soccerkick"].Import();

			CommonPoses["hawaiianpunch"]	= new NpcPose(npcx, "hawaiianpunch", false);
			CommonPoses["hawaiianpunch"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["hawaiianpunch"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["hawaiianpunch"].Ragdoll.Rigidity                 = 3.3f;
			CommonPoses["hawaiianpunch"].Ragdoll.AnimationSpeedMultiplier = 4.5f;
			CommonPoses["hawaiianpunch"].Ragdoll.UprightForceMultiplier   = 4f;
			CommonPoses["hawaiianpunch"].Import();

			CommonPoses["tackle"]	= new NpcPose(npcx, "tackle", false);
			CommonPoses["tackle"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["tackle"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["tackle"].Ragdoll.Rigidity                 = 3.3f;
			CommonPoses["tackle"].Ragdoll.AnimationSpeedMultiplier = 4.5f;
			CommonPoses["tackle"].Ragdoll.UprightForceMultiplier   = 4f;
			CommonPoses["tackle"].Import();

			CommonPoses["low_club_1"]	= new NpcPose(npcx, "low_club_1", false);
			CommonPoses["low_club_1"].Ragdoll.ShouldStandUpright       = false;
			CommonPoses["low_club_1"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["low_club_1"].Ragdoll.Rigidity                 = 3.3f;
			CommonPoses["low_club_1"].Ragdoll.AnimationSpeedMultiplier = 4.5f;
			CommonPoses["low_club_1"].Ragdoll.UprightForceMultiplier   = 0f;
			CommonPoses["low_club_1"].Import();

			CommonPoses["bat_idle_fh"]	= new NpcPose(npcx, "bat_idle_fh", false);
			CommonPoses["bat_idle_fh"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["bat_idle_fh"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["bat_idle_fh"].Ragdoll.Rigidity                 = 3.3f;
			CommonPoses["bat_idle_fh"].Ragdoll.AnimationSpeedMultiplier = 4.5f;
			CommonPoses["bat_idle_fh"].Ragdoll.UprightForceMultiplier   = 4f;
			CommonPoses["bat_idle_fh"].Import();

			CommonPoses["bat_idle_bh"]	= new NpcPose(npcx, "bat_idle_bh", false);
			CommonPoses["bat_idle_bh"].Ragdoll.ShouldStandUpright       = true;
			CommonPoses["bat_idle_bh"].Ragdoll.State                    = PoseState.Rest;
			CommonPoses["bat_idle_bh"].Ragdoll.Rigidity                 = 3.3f;
			CommonPoses["bat_idle_bh"].Ragdoll.AnimationSpeedMultiplier = 4.5f;
			CommonPoses["bat_idle_bh"].Ragdoll.UprightForceMultiplier   = 4f;
			CommonPoses["bat_idle_bh"].Import();
			
			return CommonPoses;
		}

		//
		// ─── POSE DATA ────────────────────────────────────────────────────────────────
		//
		private static readonly Dictionary<string, string> MoveData = new Dictionary<string, string>()
		{
			{ "tackle_1",
			  @"Foot:-6.215636,
				FootFront:-29.81085,
				Head:10.4053,
				LowerArm:0.003941019,
				LowerArmFront:-2.142316,
				LowerBody:3.087017,
				LowerLeg:40.1882,
				LowerLegFront:61.68584,
				UpperArm:-118.3024,
				UpperArmFront:-135.5224,
				UpperBody:1.436911,
				UpperLeg:-88.27985,
				UpperLegFront:-55.96539"
			},
			{ "tackle_2",
			  @"Foot:-31.87887,
				FootFront:-30.66121,
				Head:1.867224,
				LowerArm:0.006939472,
				LowerArmFront:2.139721,
				LowerBody:-1.041672,
				LowerLeg:6.53615,
				LowerLegFront:-2.044385,
				UpperArm:-169.4037,
				UpperArmFront:-175.3632,
				UpperBody:-0.3288326,
				UpperLeg:-0.5435874,
				UpperLegFront:5.117028"
			},
			{ "survive",
			  @"Foot:39.82808,
				FootFront:70.95604,
				#Head:-0.1240534,
				LowerArm:-109.9698,
				LowerArmFront:-103.8699,
				LowerBody:2.468101,
				LowerLeg:90.3269,
				LowerLegFront:98.48474,
				UpperArm:-50.53148,
				UpperArmFront:-48.30542,
				UpperBody:1.461895,
				UpperLeg:4.079845,
				UpperLegFront:-21.50286"
			},
			{ "low_club_1",
			  @"Foot:-31.31203,
				FootFront:22.38507,
				Head:-6.193285,
				LowerArm:-80.29018,
				LowerArmFront:-78.25074,
				LowerBody:-5.160613,
				LowerLeg:106.5819,
				LowerLegFront:103.0128,
				UpperArm:-109.4485,
				UpperArmFront:-111.5559,
				UpperBody:0.5681385,
				UpperLeg:-93.13656,
				UpperLegFront:-19.42678"
			},
			{ "club_overhead_1",
			  @"Head:0.001169457,
				LowerArm:-119.7841,
				LowerArmFront:-120.7134,
				LowerBody:-4.973232E-05,
				UpperArm:-28.28773,
				UpperArmFront:-23.47642,
				UpperBody:0.0001993561,
				UpperLeg:-0.003408905,
				UpperLegFront:0.001104143"
			},
			{ "club_overhead_2",
				@"Foot:-20.41524,
				FootFront:-28.86352,
				Head:0.72461,
				LowerArm:-69.05737,
				LowerArmFront:-34.10315,
				LowerBody:-0.2727596,
				LowerLeg:4.03408E-05,
				LowerLegFront:-0.2099087,
				UpperArm:128.1161,
				UpperArmFront:112.4894,
				UpperBody:-0.6717041,
				UpperLeg:-2.774807,
				UpperLegFront:0.07094885"
			},
			{ "club_overhead_3",
				@"Foot:13.73309,
				FootFront:-2.066161,
				Head:-2.159185,
				LowerArm:-73.09756,
				LowerArmFront:-36.23891,
				LowerBody:-0.8006212,
				LowerLeg:30.96106,
				LowerLegFront:1.730145,
				UpperArm:149.1487,
				UpperArmFront:131.6175,
				UpperBody:1.558321,
				UpperLeg:-7.039429,
				UpperLegFront:42.63924"
			},
			{ "club_overhead_4",
				@"Foot:13.73311,
				FootFront:-2.802847,
				Head:-2.673387,
				LowerArm:-50.32974,
				LowerArmFront:-34.17508,
				LowerBody:-7.136448,
				LowerLeg:29.9012,
				LowerLegFront:2.509524,
				UpperArm:-132.847,
				UpperArmFront:214.0704,
				UpperBody:0.3321589,
				UpperLeg:-44.59186,
				UpperLegFront:3.984259"
			},
			{ "club_overhead_5",
				@"Foot:1.93022,
				FootFront:50.10918,
				Head:-0.2331758,
				LowerArm:-23.43349,
				LowerArmFront:-14.85086,
				LowerBody:0.08582474,
				LowerLeg:-1.701906,
				LowerLegFront:61.93295,
				UpperArm:-68.15904,
				UpperArmFront:-68.40199,
				UpperBody:0.2320249,
				UpperLeg:-47.8306,
				UpperLegFront:9.311983"
			},
			{ "frontkick",
			  @"Foot:-32.00002,
				FootFront:15.70725
				Head:0.8086697,
				LowerArm:-42.47679,
				LowerArmFront:-87.99306,
				LowerBody:-31.11087,
				LowerLeg:112.2291,
				LowerLegFront:1.255953,
				UpperArm:-76.79327,
				UpperArmFront:-30.96368,
				UpperBody:-0.823617,
				UpperLeg:-113.3026,
				UpperLegFront:22.14269"
			},
			{ "soccerkick",
			  @"Foot:12.9847,
				FootFront:-30.00206,
				Head:-0.3353831,
				LowerArm:-100.6996,
				LowerArmFront:-62.46478,
				LowerBody:-2.406393,
				LowerLeg:-0.5384836,
				LowerLegFront:83.46684,
				UpperArm:-7.903792,
				UpperArmFront:-8.554337,
				UpperBody:0.2728253,
				UpperLeg:-9.999465,
				UpperLegFront:34.59338"
			},
			{ "hawaiianpunch",
			  @"Foot:21.77727,
				FootFront:-29.28115,
				Head:-0.07867994,
				LowerArm:-1.553318,
				LowerArmFront:-121.8119,
				LowerBody:-2.683145,
				LowerLeg:0.154031,
				LowerLegFront:21.24045,
				UpperArm:0.6001145,
				UpperArmFront:75.92109,
				UpperBody:0.01944299,
				UpperLeg:-13.48313,
				UpperLegFront:16.26041"
			},
			{ "tackle",
			  @"Foot:-32.00005,
				FootFront:51.81009,
				Head:-1.022326,
				LowerArm:-80.15282,
				LowerArmFront:-53.29562,
				LowerBody:-0.3018542,
				LowerLeg:105.4025,
				LowerLegFront:40.24598,
				UpperArm:3.129313,
				UpperArmFront:-17.13989,
				UpperBody:0.7884856,
				UpperLeg:-44.01986,
				UpperLegFront:21.55813"
			},
			{ "takecover",
			  @"Foot:-31.25526,
				FootFront:-19.08509,
				Head:-18.10908,
				LowerArm:0.001591434,
				LowerArmFront:-67.28458,
				LowerBody:-11.21474,
				LowerLeg:127.8911,
				LowerLegFront:128.04,
				UpperArm:-0.006167661,
				UpperArmFront:-12.03789,
				UpperBody:3.531075,
				UpperLeg:-40.74041,
				UpperLegFront:-101.2017"
			},
			{ "throw_1",
			  @"UpperArmFront:53.87996,
				UpperArm:0.1090901,
				LowerLegFront:-0.01318056,
				LowerArmFront:-36.86349,
				LowerArm:-0.4033567,
				UpperLegFront:3.002661,
				UpperLeg:-0.003749454,
				LowerBody:-0.05592484,
				LowerLeg:-0.007670409"
			},
			{ "throw_2",
			  @"LowerArm:0.1230614,
				UpperArm:0.117004,
				LowerLeg:0.001100474,
				LowerLegFront:0.001414623,
				UpperArmFront:174.287,
				LowerArmFront:86.93764,
				UpperLeg:0.007332622,
				UpperLegFront:0.00374265"
			},
			{ "crouch",
			  @"Head:-0.0002471675,
				UpperLegFront:-75.54669,
				UpperLeg:-0.03494752,
				LowerLeg:106.0683,
				LowerLegFront:78.07973,
				Foot:-24.21083,
				FootFront:0.4032795"
			},
			{ "dive",
			  @"UpperArm:-136.7963,
				LowerBody:-11.74754,
				UpperArmFront:-136.7963,
				UpperBody:-8.516563,
				LowerArm:-34.3288,
				UpperLeg:-74.00098,
				UpperLegFront:-67.9553,
				LowerArmFront:-33.82881,
				LowerLegFront:63.65865,
				LowerLeg:60.26822"
			},
			{ "backflip_1",
			  @"Foot:-34.77201,
				FootFront:-27.44459,
				LowerArm:-30.00827,
				LowerArmFront:-38.24268,
				LowerLeg:95.33749,
				LowerLegFront:100.3498,
				UpperArm:-157.7551,
				UpperArmFront:-149.6151,
				UpperLeg:-28.73837,
				UpperLegFront:-27.9621"
			},
			{ "backflip_2",
			  @"Foot:51.61726,
				FootFront:50.23609,
				Head:0.8721468,
				LowerArm:-30.56421,
				LowerArmFront:-22.44059,
				LowerBody:-1.73008,
				LowerLeg:39.59584,
				LowerLegFront:52.36701,
				UpperArm:155.1894,
				UpperArmFront:-196.8672,
				UpperBody:-0.8053715,
				UpperLeg:0.4376444,
				UpperLegFront:-0.03437976"
			},
			{ "robot1",
			  @"LowerArm:74.26377,
				LowerArmFront:100.959,
				UpperArm:-83.66212,
				UpperArmFront:75.15743"  
			},
			{ "robot2",
			  @"LowerArm:92.8947,
				LowerArmFront:95.35808,
				UpperArm:72.13412,
				UpperArmFront:-103.7387"  
			},
			{ "robotbody",@"LowerBody:32.04889"},
			{ "troll_1",
			  @"LowerArm:-18.83585,
				UpperArm:-63.27104"
			},
			{ "troll_2",
			  @"Foot:3.734164,
				FootFront:3.620136,
				Head:0.0003287029,
				LowerArm:-0.5349259,
				LowerArmFront:-70.07084,
				LowerBody:-0.03299237,
				LowerLeg:-0.008127072,
				LowerLegFront:0.02881059,
				UpperArm:-0.2548779,
				UpperArmFront:-92.90836,
				UpperBody:-0.002587788,
				UpperLeg:-0.151941,
				UpperLegFront:0.05902223"
			},
			{ "troll_3",
			  @"Foot:0.3850704,
				FootFront:0.5392938,
				Head:0.05808863,
				LowerArm:0.007814003,
				LowerArmFront:-50.8674,
				LowerBody:0.04533052,
				LowerLeg:2.134434E-06,
				LowerLegFront:0.04618782,
				UpperArm:0.01312784,
				UpperArmFront:-12.73461,
				UpperBody:-0.0165782,
				UpperLeg:-0.06074648,
				UpperLegFront:0.04300543"
			},
			{"troll_4",
			 @"Foot:1.009492,
				FootFront:0.06646708,
				Head:-0.01468245,
				LowerArm:-33.36053,
				LowerArmFront:-24.0003,
				LowerBody:-0.4193774,
				LowerLeg:0.1760253,
				LowerLegFront:-0.1480205,
				UpperArm:-112.8961,
				UpperArmFront:-122.5443,
				UpperBody:-0.01868927,
				UpperLeg:-1.746746,
				UpperLegFront:0.6715606"
			},
			{"troll_5",
			 @"Foot:2.283299,
				FootFront:0.2654704,
				Head:0.01646529,
				LowerArm:-9.168829,
				LowerArmFront:-5.576501,
				LowerBody:-0.7163552,
				LowerLeg:-0.001325057,
				LowerLegFront:-0.3955542,
				UpperArm:-39.52893,
				UpperArmFront:-42.08421,
				UpperBody:-0.0622329,
				UpperLeg:-3.137375,
				UpperLegFront:1.292902" 
			},
			{"troll_6",
			  @"Head:-0.1732245,
				LowerArm:-95.86164,
				LowerArmFront:-0.1081008,
				UpperArm:-79.75221,
				UpperArmFront:-0.6198328,
				UpperBody:0.0137623"
			},
			{ "troll_7",
			  @"Head:-0.0003253945,
				LowerArm:-88.82897,
				LowerArmFront:-121.2977,
				UpperArm:-87.12891,
				UpperArmFront:-59.76366,
				UpperBody:-0.07936541"
			},
			{ "troll_8", 
			  @"Head:-0.0003253945,
				LowerArm:-88.25494,
				LowerArmFront:-40.55567,
				UpperArm:-77.61653,
				UpperArmFront:-80.22157,
				UpperBody:0.02199534"
			},
			{ "troll_9", 
			  @"Head:-0.0003253945,
				LowerArm:-88.25494,
				LowerArmFront:-40.55567,
				UpperArm:-77.61653,
				UpperArmFront:-80.22157,
				UpperBody:0.02199534"
			},
			{ "troll_10",
			  @"Head:-5.037264E-05,
				LowerArmFront:-107.8224,
				UpperArm:-0.002627061,
				UpperArmFront:-103.7351,
				UpperBody:-4.738444E-05" 
			},
			{ "troll_11",
			  @"Head:0.07887588,
				LowerArm:-112.0027,
				LowerArmFront:-108.589,
				LowerBody:0.2058012,
				UpperArm:-88.86582,
				UpperArmFront:-61.66507,
				UpperBody:0.03061045" 
			},
			{ "troll_12",
			  @"Head:16.79458,
				LowerArm:-108.3576,
				LowerArmFront:-110.3306,
				LowerBody:1.263406,
				UpperArm:-57.23454,
				UpperArmFront:-62.34679,
				UpperBody:4.632922" 
			},
			{ "jump",
			  @"LowerArm:-95.09151,
				LowerArmFront:-85.78412,
				UpperArm:-376.2525,
				UpperArmFront:-369.4041,
				UpperLegFront:-111.5341,
				UpperLeg:-112.034,
				LowerLeg:133.9088,
				LowerLegFront:134.5014"
			},
			{ "bat_idle_fh",
			  @"LowerArm:-58.24822,
				UpperArm:-33.09913,
				LowerArmFront:-99.12464,
				UpperArmFront:59.33511"
			},
			{ "bat_idle_bh",
			  @"LowerArmFront:-58.24822,
				UpperArmFront:-33.09913,
				LowerArm:-99.12464,
				UpperArm:59.33511"
			},

			/*
			 * #Head:0.06648272,
				#LowerArm:-43.82706,
				LowerArmFront:-99.12464,
				LowerBody:-0.05570553,
				LowerLeg:-0.00465392,
				LowerLegFront:26.49835,
				LowerArm-88.24822,
				UpperArm-38.09913				
				#UpperArm:-32.36065,
				UpperArmFront:59.33511,
				UpperBody:-0.07466507,
				UpperLeg:-0.06083713,
				UpperLegFront:0.01067004"
			 * 
			 */
			{ "jump_spin",
			  @"LowerBody:4.073051,
				UpperBody:1.87907,
				#UpperArm:-77.37722,
				#UpperArmFront:-82.49118,
				LowerLeg:98.53986,
				Foot:52.00001,
				LowerLegFront:105.6584,
				FootFront:50.32719,
				#LowerArmFront:-122.3479,
				#LowerArm:-118.9032,
				UpperLeg:-110.6554,
				UpperLegFront:-112"
			},
			{ "prone",
			  @"Head:-6.500257, 
				LowerBody:17.68937,
				UpperArmFront:-18.54307,
				UpperArm:-16.35537,
				LowerLegFront:9.899184,
				LowerLeg:11.33566,
				Foot:0.02769642, 
				FootFront:0.04737419,
				UpperLegFront:16.45826,
				UpperLeg:15.87339,
				LowerArmFront:-103.8956,
				UpperBody:-17.44694,
				LowerArm:-102.0114"
			},
			{ "medic",
			  @"Foot:11.16336,
				FootFront:51.42083,
				Head:0.02700998,
				LowerArm:-76.75396,
				LowerArmFront:-78.2864,
				LowerBody:13.43545,
				LowerLeg:137.2816,
				LowerLegFront:128.2527,
				UpperArm:-5.436793,
				UpperArmFront:-0.4983989,
				UpperBody:0.04292091,
				UpperLeg:-95.10425,
				UpperLegFront:-96.133"
			},
			{ "jumpsword_1",
			  @"Foot:-30.16558,
				FootFront:37.30453,
				#Head:-0.004139095,
				LowerArm:-72.21709,
				LowerArmFront:-85.00078,
				LowerBody:0.5152695,
				LowerLeg:-248.4472,
				LowerLegFront:-257.0258,
				UpperArm:-100.6666,
				UpperArmFront:-92.74561,
				UpperBody:0.00395468,
				UpperLeg:46.62198,
				UpperLegFront:46.22927"
			},
			{ "jumpsword_2",
			  @"Foot:-31.99515,
				FootFront:50.46266,
				#Head:0.008920227,
				LowerArm:-102.5917,
				LowerArmFront:-110.0015,
				LowerBody:42.94868,
				LowerLeg:-0.1923655,
				LowerLegFront:13.52737,
				UpperArm:387.0154,
				UpperArmFront:382.127,
				UpperBody:-5.140564,
				UpperLeg:-94.6385,
				UpperLegFront:45.05071"
			},
			{ "spearhold",
			  @"Foot:-30.16564,
				FootFront:37.3046,
				Head:0.0003141887,
				LowerArm:-430.0918,
				LowerArmFront:-443.1978,
				LowerBody:-3.46585,
				LowerLeg:-249.7297,
				LowerLegFront:-258.3062,
				UpperArm:-89.24717,
				UpperArmFront:-81.00411,
				UpperBody:-17.57518,
				UpperLeg:47.90446,
				UpperLegFront:47.50976"
			},
			{ "spear_1",
			  @"Foot:0.3362804,
				FootFront:-0.4490192,
				Head:-0.0003978585,
				LowerArm:-0.0007299231,
				LowerArmFront:-121.741,
				LowerBody:-0.004964107,
				LowerLeg:-0.002408175,
				LowerLegFront:-0.000129987,
				UpperArm:-0.01419425,
				UpperArmFront:-7.900223,
				UpperBody:0.001294588,
				UpperLeg:-0.00224964,
				UpperLegFront:1.56886"
			},
			{ "spear_2",
			  @"Foot:-0.2711532,
				FootFront:0.3674452,
				Head:0.04528368,
				LowerArm:-1.056267,
				LowerArmFront:-64.89429,
				LowerBody:-0.07540518,
				LowerLeg:0.1310087,
				LowerLegFront:-0.1006831,
				UpperArm:-11.41162,
				UpperArmFront:117.8358,
				UpperBody:-0.01748134,
				UpperLeg:0.3105718,
				UpperLegFront:-0.4763209"  
			},
			{ "spear_3",
			  @"Foot:-30.59307,
				FootFront:-19.17831,
				Head:-0.01713011,
				LowerArm:-1.355939,
				LowerArmFront:-64.62589,
				LowerBody:-17.71288,
				LowerLeg.29605,
				LowerLegFront:67.17188,
				UpperArm:-96.83207,
				UpperArmFront:31.8473,
				UpperBody:-0.1174417,
				UpperLeg:0.1126486,
				UpperLegFront:-54.36752"  
			},
			{ "groundpound_1",
			  @"Foot:51.70714,
				FootFront:-31.99269,
				#Head:-0.07597391,
				LowerArm:-36.66118,
				LowerArmFront:-19.87204,
				LowerBody:-3.997653,
				LowerLeg:146.4712,
				LowerLegFront:150.5883,
				UpperArm:-308.9179,
				UpperArmFront:38.9634,
				UpperBody:0.06812089,
				UpperLeg:22.58445,
				UpperLegFront:25.51438"
			},
			{ "groundpound_2",
			  @"Foot:51.99865,
				FootFront:-30.64739,
				LowerArm:277.8946,
				LowerArmFront:-0.2756784,
				LowerBody:0.7950341,
				LowerLeg:137.9989,
				LowerLegFront:88.16898,
				UpperArm:-211.8853,
				UpperArmFront:-73.02646,
				UpperBody:-2.796829,
				UpperLeg:-104.0163,
				UpperLegFront:-21.80765"
			},
			{ "groundpound_2x",
			  @"Foot:-30.07862,
				FootFront:-8.974759,
				#Head:-0.2506458,
				LowerArm:-86.63663,
				LowerArmFront5.9857,
				LowerBody:-10.36328,
				LowerLeg:78.862,
				LowerLegFront:83.92636,
				UpperArm:-91.01971,
				UpperArmFront:-132.8579,
				UpperBody:0.63482,
				UpperLeg:-78.86197,
				UpperLegFront:0.1315119"
			},
			{ "groundpound_3",
			  @"Foot:6.143962,
				FootFront:51.7829,
				#Head:14.52312,
				LowerArm:-21.96695,
				LowerArmFront:-42.34736,
				#LowerBody:-13.23629,
				LowerLeg:79.28992,
				LowerLegFront:73.83433,
				UpperArm:-41.73515,
				UpperArmFront:-43.72218,
				#UpperBody:13.89472,
				UpperLeg:-110.0218,
				UpperLegFront:-3.436492"
			},
			{ "chainsaw",
			  @"LowerArm:-28.53935,
				LowerArmFront:-76.81657,
				UpperArm:-47.884,
				UpperArmFront:-3.689704"
			},
			{ "bicycle_1",
			  @"#Foot:-16.25298,
				#FootFront:7.71407,
				Head:-0.003797585,
				LowerArm:-0.04865827,
				LowerArmFront:-30.08054,
				LowerBody:-2.096376,
				UpperArm:-66.23631,
				UpperArmFront:-52.03539,
				UpperBody:0.001038189,
				UpperLeg:-97.02609:r4,
				UpperLegFront:-70.46504:r4,
				LowerLeg:93.72361:r4,
				LowerLegFront:67.38856:r4"
			},
			{ "bicycle_2",
			  @"#Foot:-16.25306,
				#FootFront:7.715409,
				Head:-8.253792,
				LowerArm:0.4919102,
				LowerArmFront:-30.06942,
				LowerBody:-1.764511,
				UpperArm:-66.15874,
				UpperArmFront:-51.97304,
				UpperBody:-0.2303413,
				UpperLegFront:-97.02609:r4,
				UpperLeg:-70.46504:r4,
				LowerLegFront:93.72361:r4,
				LowerLeg:67.38856:r4"
			},
			{ "car_1",
			  @"Foot:-30.3219,
				FootFront:16.76529,
				Head:0.03925481,
				LowerArm:0.1478599,
				LowerArmFront:0.5158705,
				LowerBody:0.01901354,
				LowerLeg:80.20628,
				LowerLegFront:0.00769677,
				UpperArm:719.9979,
				UpperArmFront:654.4448,
				UpperBody:-0.005143133,
				UpperLeg:-73.78301,
				UpperLegFront:6.370961"
			},
			{ "car_2",
			  @"Foot:0.0008742642,
				FootFront:0.0003278491,
				Head:0.01568211,
				LowerArm:-76.72209,
				LowerArmFront:-84.46452,
				LowerBody:-0.614116,
				LowerLeg:53.09472,
				LowerLegFront:58.28015,
				UpperArm:-41.6382,
				UpperArmFront:-24.33673,
				UpperBody:-0.01426143,
				UpperLeg:-87.6869,
				UpperLegFront:-91.46274"
			},
			{ "hovercar",
			  @"Foot:0.01568211,
				FootFront:0.01139276,
				Head:-0.007868378,
				LowerArm:-25.04409,
				LowerArmFront:-24.96836,
				LowerBody:0.04311216,
				LowerLeg:89.98813,
				LowerLegFront:89.9802,
				UpperArm:-36.20858,
				UpperArmFront:-15.03177,
				UpperBody:-4.112211,
				UpperLeg:-89.98132,
				UpperLegFront:-89.98193"
			},
			{ "swing",
			  @"Foot:41.42316,
				FootFront:25.18564,
				LowerArm:1.846992,
				LowerArmFront:1.97305,
				UpperArm:-489.6964,
				UpperArmFront:-130.1055,
				LowerLeg:15.31137,
				LowerLegFront:25.25853,
				UpperLeg:-67.0476,
				UpperLegFront:-71.30318"
			},
			{ "swing_f",
			  @"LowerArm:1.846992,
				LowerArmFront:1.97305,
				UpperArm:-489.6964,
				UpperArmFront:-130.1055,
				LowerLeg:93.72361,
				LowerLegFront:93.72361,
				UpperLeg:-67.0476,
				UpperLegFront:-71.30318"
			},
			{ "swing_b",
			  @"LowerArm:4.924457,
				LowerArmFront:1.527872,
				UpperArm:186.2132,
				UpperArmFront:192.5992,
				LowerLeg:93.72361,
				LowerLegFront:93.72361,
				UpperLeg:-2.232871,
				UpperLegFront:-0.6828669"
			},
			{ "bflip_1",
			  @"Foot:-0.04034764,
				FootFront:0.005662227,
				Head:-0.03662347,
				LowerArm:-24.87376,
				LowerArmFront:-28.54758,
				LowerBody:16.99994,
				LowerLeg:89.94771,
				LowerLegFront:89.99181,
				UpperArm:-107.6021,
				UpperArmFront:-111.3755,
				UpperBody:-16.28723,
				UpperLeg:-89.99418,
				UpperLegFront:-90.00147"
			},
			{ "bflip_2",
			  @"Foot:12.9756,
				FootFront:12.94864,
				Head:0.006085698,
				LowerArm:-27.29536,
				LowerArmFront:-23.44413,
				LowerBody:16.67098,
				LowerLeg:61.49474,
				LowerLegFront:67.16005,
				UpperArm:-131.2479,
				UpperArmFront:-134.4315,
				UpperBody:-16.62746,
				UpperLeg:-47.98672,
				UpperLegFront:-50.66712"
			},
			{ "bflip_3",
			  @"Foot:-0.0006352076,
				FootFront:-0.0005600755,
				Head:-0.006707246,
				LowerArm:-13.68577,
				LowerArmFront:-12.73648,
				LowerBody:16.69749,
				LowerLeg:89.99817,
				LowerLegFront:89.99821,
				UpperArm:158.7476,
				UpperArmFront:154.9339,
				UpperBody:-16.49267,
				UpperLeg:-89.99746,
				UpperLegFront:-89.99746"
			}
		};
	}
}
