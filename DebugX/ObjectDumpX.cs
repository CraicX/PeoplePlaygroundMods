using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

namespace ObjectDumpX
{
	public class ObjectDumpX
	{

		public static int    LevelsDeep             = 4;
		public static string CurrentBehaviour       = "";
		public static string[] SkipNestedBehaviours = {
			"PhysicalBehaviour", 
			"SkinMaterialHandler", 
			"ConnectedNodeBehaviour", 
			"CirculationBehaviour",
			"PersonBehaviour",
			"LimbBehaviour",
			"bulletHolePoints",
		};

		public static void OnLoad()
		{
			BindKeys();
		}

		public static void Main()
		{
			ModAPI.Register<ObjectDumpXBehaviour>();
		}

		public static bool KeyCheck(string _name)
		{
			if (DialogBox.IsAnyDialogboxOpen || Global.ActiveUiBlock) return false;

			return (InputSystem.Down(_name));
		}

		private static void BindKeys()
		{
			string[] KeySchematic =
			{
				"dumpObjects  > Dump selected Objects       > F3",
				"dumpAll      > Dump everything in scene    > F4"
			};

			string inputKey;
			InputAction action;

			foreach (string keyMapping in KeySchematic)
			{
				string[] KeyParts = keyMapping.Split('>');

				inputKey = "Dump-" + KeyParts[0].Trim();

				if (!InputSystem.Has(inputKey))
				{
					ModAPI.RegisterInput("[Dump] " + KeyParts[1].Trim(), inputKey, (KeyCode)Enum.Parse(typeof(KeyCode), KeyParts[2].Trim()));

					if (KeyParts.Length == 4)
					{
						InputAction actionCreate = new InputAction(
							(KeyCode)Enum.Parse(typeof(KeyCode), KeyParts[2].Trim()),
							(KeyCode)Enum.Parse(typeof(KeyCode), KeyParts[3].Trim())
						);


						InputSystem.Actions.Add(inputKey, actionCreate);
					}
				}
				else
				{
					action = InputSystem.Actions[inputKey];
					ModAPI.RegisterInput("[Dump] " + KeyParts[1].Trim(), inputKey, action.Key);

					InputAction actionNew  = InputSystem.Actions[inputKey];
					actionNew.SecondaryKey = action.SecondaryKey;
				}
			}
		}
	}


	public class ObjectDumpXBehaviour : MonoBehaviour
	{

		public void Update()
		{

			if (Input.GetKey(KeyCode.None)) return;     // dont bother checking the others
			else if (ObjectDumpX.KeyCheck("Dump-dumpObjects"))  DumpSelectedObjects();
			else if (ObjectDumpX.KeyCheck("Dump-dumpAll"))      DumpAllObjects();
		}

		// - - - - - - - - - - - - - - - - - - -
		//
		//  FN: GET SELECTED SHTUFF
		//
		public static List<PhysicalBehaviour> GetSelected(bool bothTypes = false)
		{
			List<PhysicalBehaviour> PBL = new List<PhysicalBehaviour>();

			if (bothTypes || SelectionController.Main.SelectedObjects.Count == 0)
			{
				foreach (PhysicalBehaviour PB in Global.main.PhysicalObjectsInWorld)
				{
					if (!PB.Selectable || PB.colliders.Length == 0) continue;

					foreach (Collider2D collider in PB.colliders)
					{
						if (!collider) continue;

						if (collider.OverlapPoint((Vector2)Global.main.MousePosition))
						{
							if (PB.transform.root.TryGetComponent<PhysicalBehaviour>(out PhysicalBehaviour component))
							{
								if (component.Selectable) PBL.Add(component);
							}
							else
							{
								PBL.Add(PB);
							}
						}
					}
				}
			}

			if (bothTypes || PBL.Count == 0) PBL.AddRange(SelectionController.Main.SelectedObjects);

			return PBL;

		}

		static List<string> vectorList;
		static string logOutput;

		public static void DumpTransforms(Transform t, bool useFilter = true)
		{
			ModAPI.Notify("Dumping Transform Tree: <color=yellow>" + t.root.name + "</color>");

			string divider  = "\n" + new string('-', 84) + "\n";
			logOutput       = divider + "DUMP TRANSFORM TREE: " + t.root.name + divider;
			vectorList      = new List<string>();

			DumpSingleT(t.root, "", useFilter);

			logOutput += divider;

			foreach (string item in vectorList) logOutput += item + "\n";

			Debug.Log(logOutput + divider);
		}

		public static void DumpSingleT(Transform t, string prev_path, bool useFilter = true)
		{
			if (useFilter)
			{
				string[] filterList = { "particle", "outline", "decal" };

				foreach (string filterWord in filterList)
					if (t.name.ToLower().Contains(filterWord)) return;
			}

			prev_path += t.name;
			logOutput += prev_path + "\n";

			vectorList.Add(t.name + " -> pos: " + t.position    + " [" + t.localPosition    + "]");
			vectorList.Add(t.name + " -> ang: " + t.eulerAngles + " [" + t.localEulerAngles + "]");

			for (int i = 0; i < t.childCount; ++i)
				DumpSingleT(t.GetChild(i), prev_path + " -> ", useFilter);
		}



		public static void DumpObject(PhysicalBehaviour PBO)
		{

			string pName = PBO.name;
			string tName;

			GameObject g = PBO.gameObject;

			string hr = new string('-', 84);

			string LogData = "\n\n\n" + hr + "\n  :::::: " + pName + "\n" + hr + "\n\n";

			MonoBehaviour[] components = g.GetComponentsInParent<MonoBehaviour>();

			foreach (MonoBehaviour component in components)
			{
				tName    = component.GetType().ToString();

				ObjectDumpX.CurrentBehaviour = tName;

				LogData += Head(tName, pName);

				LogData += ObjectDumper.Dump(component);

				LogData += Tail(tName, pName);

			}

			Debug.Log( LogData );

			ModAPI.Notify("Dumping <color=yellow>" + pName + "</color>");

		}

		private static string Head(string tName, string pName)
		{
			string textIn  = tName + " [" + pName + "] ";

			int lineLength = 80 - textIn.Length;

			string textOut = "--- " + textIn + new string('-', lineLength);
			textOut += "\n";

			return textOut;

		}

		private static string Tail(string tName, string pName)
		{

			int lineLength = 79 - tName.Length;

			string textOut = new string('-', lineLength);
			textOut += " " + tName + " ---\n\n";

			return textOut;

		}

		public static void DumpObjects(List<PhysicalBehaviour> PBS)
		{
			foreach (PhysicalBehaviour PB in PBS)
			{
				DumpObject(PB);
			}
		}

		public static void DumpSelectedObjects()
		{
			if (Input.GetKey(KeyCode.LeftShift))
			{
				RequestLevels();
			} 
			else if (Input.GetKey(KeyCode.LeftControl))
			{
				List<PhysicalBehaviour> pblist = GetSelected();

				if (pblist.Count > 0) DumpTransforms(pblist[0].transform);
			}
			else
			{
				DumpObjects(GetSelected());
			}

		}

		public static void RequestLevels()
		{
			DialogBox dialog = null;

			dialog = DialogBoxManager.TextEntry(
				"Set how many levels deep to go",
				ObjectDumpX.LevelsDeep.ToString(),
				new DialogButton("Set", true, new UnityAction[1] { (UnityAction)(() => ApplyLevels(dialog)) }),
				new DialogButton("Cancel", true, (UnityAction[])Array.Empty<UnityAction>()));
		}

		public static void ApplyLevels(DialogBox d)
		{
			string levelsDeep = d.EnteredText;

			if (levelsDeep == "") levelsDeep = ObjectDumpX.LevelsDeep.ToString();

			if (int.TryParse(levelsDeep, out int iDeep))
			{
				ObjectDumpX.LevelsDeep = iDeep;

				ModAPI.Notify("Dumps will now go <color=yellow>" + ObjectDumpX.LevelsDeep + "</color> levels deep.");
			}
			else
			{
				ModAPI.Notify("Try a number 1-10");
			}
		}

		public static void DumpAllObjects()
		{

			if (Input.GetKey(KeyCode.LeftShift))
			{
				DumpPose(GetSelected());
			}
			else
			{
				DumpObjects(Global.main.PhysicalObjectsInWorld);
			}

		}

		public static void DumpPose(List<PhysicalBehaviour> PBS)
		{
			Dictionary<int, RagdollPose> Rags = new Dictionary<int, RagdollPose>();

			Dictionary<string, RagdollPose.LimbPose> Poses = new Dictionary<string, RagdollPose.LimbPose>();  

			foreach (PhysicalBehaviour PB in PBS)
			{
				PersonBehaviour person = PB.gameObject.GetComponentInParent<PersonBehaviour>();

				if (!person) continue; 

				int PHash = person.GetHashCode();   

				if (!Rags.ContainsKey(PHash) ) {

					Rags[PHash] = new RagdollPose() {
						Angles = new List<RagdollPose.LimbPose>()
					};

					//foreach (RagdollPose pose in person.Poses)
					//{
					//    foreach (RagdollPose.LimbPose lp in pose.Angles)
					//    {
					//        RagdollPose.LimbPose nlp = new RagdollPose.LimbPose();
					//        nlp = lp.ShallowClone();

					//        nlp.Limb = null;

					//        if (!Poses.ContainsKey(lp.Name)) Poses.Add(lp.Name, lp);
					//    }
					//}
				}

				if (PB.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb))
				{
					Rags[PHash].Angles.Add(new RagdollPose.LimbPose(limb, limb.HasJoint ? limb.Joint.jointAngle : 0f));
				}

			}

			if (Rags.Count < 1)
			{
				ModAPI.Notify("<color=red>Pose Failed:</color> No people in selection");
				return;
			}

			//string PoseString = JsonConvert.SerializeObject(Poses, Formatting.Indented);

			string LogData = "";
			string hr = new string('-', 84);

			//string LogData = "\n\n\n" + hr + "\n  :::::: " + "LINKED POSES" + "\n" + hr + "\n\n";

			//LogData += PoseString;


			LogData += "\n\n\n" + hr + "\n  :::::: " + "RAGDOLL POSES" + "\n" + hr + "\n\n";

			foreach (KeyValuePair<int, RagdollPose> Rag in Rags)
			{
				Rag.Value.ConstructDictionary();
				LogData += Head(Rag.Key.ToString(), "RagdollPose");
				LogData += "\n{'LimbAngles':{";
				foreach (RagdollPose.LimbPose limbPose in Rags[Rag.Key].Angles)
				{
					LogData += "'" + limbPose.Name + "': '" + limbPose.Angle + "', ";
				}
				LogData = LogData.Substring(0, LogData.Length - 2);
				LogData += "}}\n\n";
				LogData += Tail(Rag.Key.ToString(), "RagdollPose");
			}

			Debug.Log(LogData);

			ModAPI.Notify("Dumped Poses for <color=yellow>" + Rags.Count + " </color>selected");

		}

	}

	public class ObjectDumper
	{
		private int _level;
		private readonly int _indentSize;
		private readonly StringBuilder _stringBuilder;
		private readonly List<int> _hashListOfFoundElements;

		private ObjectDumper(int indentSize)
		{
			_indentSize              = indentSize;
			_stringBuilder           = new StringBuilder();
			_hashListOfFoundElements = new List<int>();
		}

		public static string Dump(object element)
		{
			return Dump(element, 2);
		}

		public static string Dump(object element, int indentSize)
		{
			ObjectDumper instance = new ObjectDumper(indentSize);
			return instance.DumpElement(element);
		}

		private string DumpElement(object element)
		{
			if (element == null) return "";

			if (element == null || element is ValueType || element is string)
			{
				Write(FormatValue(element));
			}
			else
			{
				Type objectType = element.GetType();

				if (_level > 0 && ObjectDumpX.SkipNestedBehaviours.Any(objectType.FullName.Contains))
				{
					//_level--;
					return "";
				}

				if( _level > 0 && !objectType.FullName.Contains("UnityEngine.Transform")) Write("{{{0}}}", objectType.FullName);

				if (!typeof(IEnumerable).IsAssignableFrom(objectType))
				{
					_hashListOfFoundElements.Add(element.GetHashCode());
					_level++;
				}

				if (element is IEnumerable enumerableElement)
				{
					foreach (object item in enumerableElement)
					{
						if (item is IEnumerable && !(item is string))
						{
							_level++;
							DumpElement(item);
							_level--;
						}
						else
						{
							if (!AlreadyTouched(item)) DumpElement(item);
						}
					}
				}
				else
				{
					MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
					foreach (MemberInfo memberInfo in members)
					{
						switch (memberInfo.Name)
						{
							case "minVolume":
							case "maxVolume":
							case "rolloffFactor":
							case "bulletHolePoints":
								continue;
						}


						FieldInfo fieldInfo = memberInfo as FieldInfo;
						PropertyInfo propertyInfo = memberInfo as PropertyInfo;

						if (fieldInfo == null && propertyInfo == null)
							continue;

						try
						{

							Type type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
							object value = fieldInfo != null
											? fieldInfo.GetValue(element)
											: propertyInfo.GetValue(element, null);


							if (type.IsValueType || type == typeof(string))
							{
								Write("{0}: {1}", memberInfo.Name, FormatValue(value));
							}
							else
							{
								if (memberInfo.Name != "gameObject")
								{
									bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
									Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");

									bool alreadyTouched = !isEnumerable && AlreadyTouched(value);
									_level++;
									if (!alreadyTouched && _level < ObjectDumpX.LevelsDeep) DumpElement(value);
									_level--;
								}
							}
						}
						catch { }
					}
				}

				if (!typeof(IEnumerable).IsAssignableFrom(objectType))
				{
					_level--;
				}
			}

			return _stringBuilder.ToString();
		}

		private bool AlreadyTouched(object value)
		{
			if (value == null)
				return false;

			int hash = value.GetHashCode();
			for (int i = 0; i < _hashListOfFoundElements.Count; i++)
			{
				if (_hashListOfFoundElements[i] == hash)
					return true;
			}
			return false;
		}

		private void Write(string value, params object[] args)
		{
			string space = new string(' ', _level * _indentSize);

			if (args != null)
				value = string.Format(value, args);

			_stringBuilder.AppendLine(space + value);
		}

		private string FormatValue(object o)
		{
			if (o == null)
				return ("null");

			if (o is DateTime time)
				return (time.ToShortDateString());

			if (o is string)
				return string.Format("\"{0}\"", o);

			if (o is char @char && @char == '\0')
				return string.Empty;

			if (o is ValueType)
				return (o.ToString());

			if (o is IEnumerable)
				return ("...");

			return ("{ }");
		}
	}

}
