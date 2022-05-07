//     ___                        _                    _     
//    / _ \_   _ _ __  _ __   ___| |_  /\/\   __ _ ___| |_ ___ _ __ 
//   / /_)/ | | | '_ \| '_ \ / _ \ __|/    \ / _` / __| __/ _ \ '__|
//  / ___/| |_| | |_) | |_) |  __/ |_/ /\/\ \ (_| \__ \ ||  __/ |  
//  \/     \__,_| .__/| .__/ \___|\__\/    \/\__,_|___/\__\___|_|  
//              |_|   |_|    Feel free to use and modify any code
//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetMaster
{
	[SkipSerialisation]
	public class Inventory : MonoBehaviour
	{
		public Puppet Puppet;
		public Thing Clone;

		private InventoryStates _inventoryState = InventoryStates.Idle;
		public ItemTypes AllowedTypes           = ItemTypes.All;

		public int numberKeyPressed = 0;

		private int _inventoryPos   = 0;
		private int _lastHash       = 0;
		private float _endlessLoop1 = 0f;

		private float heldKeyTime   = 0f;
		private bool disableKeyTime = false;

		public PM3 LastQResult;



		public InventoryStates State
		{
			get { return _inventoryState; }
			set { 
				_inventoryState = value; 

				//ModAPI.Notify("Inventory: " + value);
			}

		}


		private readonly List<Thing> MyInventory = new List<Thing>();


		public int LastPos = 0;

		private bool _inventoryWatch = false;

		public bool bypassAutoInventory = false;

		public PuppetHand FH => Puppet.PG.FH;
		public PuppetHand BH => Puppet.PG.BH;
		public PuppetHand[] BothHands => Puppet.PG.BothHands;

		public bool IsHolding(PuppetHand hand) => (hand.IsHolding  && hand.GB.isHolding && hand.Thing != null && hand.Thing.P != null);

		public bool InventoryWatch
		{
			get { return _inventoryWatch; }
			set { 
				_inventoryWatch = value; 

				if (value) {
					KB.DisableNumberKeys();
					numberKeyPressed    = 0;
					bypassAutoInventory = false;
					heldKeyTime         = Time.time;
				}
				else {
					if ( !disableKeyTime && Time.time > heldKeyTime + 3f )
					{
						ClearHand(FH); 
						ClearHand(BH);
						bypassAutoInventory = true;
					}
					KB.EnableNumberKeys();
					if (!bypassAutoInventory) StartCoroutine(ITriggerInventory());
				}
			}
		}

		public int CurrentPos
		{
			get { 
				if (_inventoryPos < -1 || _inventoryPos >= MyInventory.Count) _inventoryPos = -1;
				return _inventoryPos; 
			}
			set { 
				if (value >= MyInventory.Count) _inventoryPos = -1;
				else if (value < -1) _inventoryPos = -1;
				else _inventoryPos = value;
			}
		}
		public int NextPos
		{
			get { CurrentPos = CurrentPos + 1; return CurrentPos; }
		}

		private void Update()
		{
			if (!Puppet.IsActive) return;

			if (!KB.Inventory && InventoryWatch) InventoryWatch = false;

			if (InventoryWatch) CheckKeyCombos();

		}

		private void CheckKeyCombos()
		{
			int _numberKey = KB.CheckNumberKey();

			if (_numberKey > 0 && _numberKey < 4)
			{
				if (_numberKey == 1 || _numberKey == 2) Puppet.PG.ForcedHolding = ForcedHoldingPositions.OneHand;
				else if(_numberKey == 3)                Puppet.PG.ForcedHolding = ForcedHoldingPositions.TwoHand;

				bypassAutoInventory = true;
				numberKeyPressed    = _numberKey;
				StartCoroutine(ITriggerInventory());
			}

			if (KB.MouseDown)
			{
				PhysicalBehaviour clickedPB = Util.GetClickedItem();

				if (clickedPB != null && PuppetMaster.CanHoldItem(clickedPB))
				{
					bypassAutoInventory = true;
					heldKeyTime         = Time.time;
					Thing thing         = clickedPB.gameObject.GetOrAddComponent<Thing>();

					if (thing != null) StartCoroutine(IStoreThing(thing));
				}
			}

			if (KB.MouseUp)
			{
				PersonBehaviour clickedPerson = Util.GetClickedPerson();
				if (clickedPerson != null && clickedPerson != Puppet.PBO)
				{
					if ( clickedPerson.TryGetComponent<Puppet>( out Puppet oldPuppet ) )
					{ 
						TransferInventory( oldPuppet );
						bypassAutoInventory = true;
						heldKeyTime         = Time.time;
					}
				}
			}

			if ( Input.GetKeyDown( KeyCode.Backspace ) || Input.GetKeyDown( KeyCode.Delete ) )
			{
				ClearInventory();
				bypassAutoInventory = true;
			}

			if ( !disableKeyTime && Time.time > heldKeyTime + 1.5f )
			{
				if (Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Alpha3) || KB.Shift)
				{
					if (FH.IsHolding) { ClearHand(FH); bypassAutoInventory = true;}
					heldKeyTime = Time.time;
				}

				if ( Input.GetKey( KeyCode.Alpha2 ) || Input.GetKey(KeyCode.Alpha3) || KB.Control )
				{
					if (BH.IsHolding) { ClearHand(BH); bypassAutoInventory = true;}
					heldKeyTime = Time.time;
				}
			}

			if (!disableKeyTime && Time.time > heldKeyTime + 2f )
			{
				ClearHand(FH);
				ClearHand(BH);
				heldKeyTime = Time.time;
				bypassAutoInventory = true;
			}
		}


		public void TransferInventory( Puppet oldPuppet )
		{
			int beforeCount = MyInventory.Count;

			foreach (Thing othing in oldPuppet.Inventory.MyInventory)
			{
				if (!MyInventory.Contains(othing)) MyInventory.Add(othing);
			}

			int afterCount = MyInventory.Count;

			Util.Notify("Transferred <color=yellow>[" + afterCount + "] new items </color> from past puppet", VerboseLevels.Minimal);

			return;
		}


		public bool InInventory( Thing thing )
		{
			return Locate(thing) >= 0;
		}


		public int Locate( Thing thing )
		{
			if (MyInventory.Count == 0) return -1;
			if (thing == null) return -1;
			for ( int i = MyInventory.Count; --i >= 0; )
			{
				if ( MyInventory[i] != null && MyInventory[i].P != null && 
					MyInventory[i].P.name == thing.P.name )
				{
					return i;
				}
			}

			return -1;
		}


		public void ValidateInventory()
		{
			for ( int i = MyInventory.Count; --i >= 0; )
			{
				if (MyInventory[i] == null || MyInventory[i].P == null) {
					Util.Notify("Removing invalid inventory at #<color=red>" + i + "</color>", VerboseLevels.Full);
					MyInventory.RemoveAt(i);
				}
			}
		}

		public void PutAwayItems(ItemTypes type=ItemTypes.All)
		{
			foreach ( PuppetHand hand in Puppet.PG.BothHands )
			{
				if (hand.GB.isHolding && hand.Thing != null) {

					if (type != ItemTypes.All) {
						if (type == ItemTypes.VehicleSafe)
						{
							if (hand.Thing.canStab)  return;
							if (hand.Thing.canShoot) return;
						}
					}
					StartCoroutine(IStoreThing(hand.Thing));
				}
			}
		}


		//
		// ─── CLEAR INVENTORY ────────────────────────────────────────────────────────────────
		//
		public void ClearInventory()
		{
			Thing _thing;
			int itemCount = 0;

			foreach ( PuppetHand hand in BothHands )
			{
				if ( IsHolding(hand))
				{
					_thing = hand.Thing;
					Puppet.PG.Drop(hand);
					_thing.Destroy();
					itemCount++;
				}
			}

			foreach ( Thing thing in MyInventory )
			{
				itemCount++;
				if (thing != null ) thing.Destroy();
			}

			MyInventory.Clear();
			CurrentPos       = 0;
			LastPos          = 0;

			Util.Notify("Cleared (<color=yellow>" + itemCount + "</color>) Items from Inventory!", VerboseLevels.Minimal);

		}

		public PM3 ForceDrop( Thing thing )
		{
			if (thing == null || thing.P == null) return PM3.Success;

			int thingHash = thing.P.GetHashCode();

			if (thingHash != _lastHash)
			{
				_endlessLoop1 = Time.time + 2f;
				_lastHash     = thingHash;
			}

			if (Time.time > _endlessLoop1) return PM3.Timeout;

			PM3 dropped = PM3.Success;

			if (FH.GB.CurrentlyHolding == thing.P) {
				dropped = PM3.Fail;
				FH.GB.DropObject();
			}

			if (BH.GB.CurrentlyHolding == thing.P)
			{
				dropped = PM3.Fail;
				BH.GB.DropObject();
			}

			if (FH.Thing == thing)
			{
				FH.IsHolding = false;
				FH.Thing     = null;
			}

			if ( BH.Thing == thing )
			{
				BH.IsHolding = false;
				BH.Thing     = null;
			}

			return dropped;
		}

		public void ClearHand( PuppetHand hand )
		{
			if ( hand.IsHolding && hand.Thing != null )
			{
				StartCoroutine(IStoreThing(hand.Thing));
			}
			else hand.IsHolding = false;
		}

		public PM3 QFree( float waitingTime )
		{
			if (State == InventoryStates.Idle) { LastQResult = PM3.Success; return PM3.Success; }

			if (waitingTime > Time.time) { LastQResult = PM3.Timeout; return PM3.Timeout; }

			LastQResult = PM3.Fail; return PM3.Fail;
		}


		IEnumerator ITriggerInventory()
		{
			float IeTimeout = Time.time + 2f;

			if (Puppet.PG.PauseHold) yield break;

			FH.LB.Broken = FH.uArmL.Broken = BH.LB.Broken = BH.uArmL.Broken = false;

			//  Overrides
			if (KB.Control && KB.Shift) numberKeyPressed = 3;
			else if (KB.Control)        numberKeyPressed = 2;
			else if (KB.Shift)          numberKeyPressed = 1;

			if ( numberKeyPressed <= 0 )
			{
					Puppet.PG.ForcedHolding = ForcedHoldingPositions.Default;

				if ( IsHolding( BH ) && BH.HoldStyle == HoldStyle.Dual )
					numberKeyPressed = 3;
				else 
					numberKeyPressed = 1;

			}

			if ( numberKeyPressed == 1 )
			{
				//  Front Hand

				bool getItem = true;

				if ( IsHolding( FH ) )
				{
					if (!InInventory(FH.Thing)) getItem = false;

					StartCoroutine(IStoreThing(FH.Thing));
					while (State != InventoryStates.Idle) yield return new WaitForEndOfFrame();
					if (LastQResult != PM3.Success) yield break;
				}

				if (getItem) {
					StartCoroutine(ISelectThing(FH));
					while (State != InventoryStates.Idle) yield return new WaitForEndOfFrame();
				}

				yield break;

			}

			if ( numberKeyPressed == 2 )
			{
				//  Back Hand

				bool getItem = true;

				if ( IsHolding( BH ) )
				{
					if (!InInventory(BH.Thing)) getItem = false;

					StartCoroutine(IStoreThing(BH.Thing));
					while (State != InventoryStates.Idle) yield return new WaitForEndOfFrame();
					if (LastQResult != PM3.Success) yield break;
				}

				if (getItem) {
					StartCoroutine(ISelectThing(BH));
					while (State != InventoryStates.Idle) yield return new WaitForEndOfFrame();
				}

				yield break;

			}

			if ( numberKeyPressed == 3 )
			{
				//  Both Hands
				Puppet.PG.ForcedHolding = ForcedHoldingPositions.TwoHand;

				bool getItem = true;

				if ( IsHolding( FH ) )
				{
					if (!InInventory(FH.Thing)) getItem = false;
					StartCoroutine(IStoreThing(FH.Thing));
					while (State != InventoryStates.Idle) yield return new WaitForEndOfFrame();
					if (LastQResult != PM3.Success) yield break;
				}

				if ( BH.Validate() )
				{
					if (!InInventory(BH.Thing)) getItem = false;
					StartCoroutine(IStoreThing(BH.Thing));
					while (State != InventoryStates.Idle) yield return new WaitForEndOfFrame();
					if (BH.Validate()) Puppet.PG.FixPosition(BH);
					if (LastQResult != PM3.Success) yield break;
				}

				if (getItem) {
					StartCoroutine(ISelectThing(FH));
					while (State != InventoryStates.Idle) yield return new WaitForEndOfFrame();
					if (LastQResult != PM3.Success) yield break;

					if ( FH.IsHolding && FH.Thing != null && FH.Thing.P != null )
					{
						FH.HoldStyle = HoldStyle.Dual;
						StartCoroutine(Puppet.PG.IDualGrip(BH));
					}
				}

				yield break;

			}

		}


		//
		// ─── E SELECT THING ────────────────────────────────────────────────────────────────
		//
		IEnumerator ISelectThing( PuppetHand hand, int invPos=-1 )
		{
			Debug.Log(hand.HandId + ") Select Inventory ");
			float IeTimeout = Time.time + 2f;

			PM3 qResult;
			do
			{
				qResult = QFree(IeTimeout);

				if (qResult == PM3.Timeout) yield break;

				yield return new WaitForEndOfFrame();
			}
			while (qResult == PM3.Fail);

			if (MyInventory.Count == 0) yield break;

			State = InventoryStates.SelectingItem;

			Thing thing = (Thing)null;

			if (invPos == -1) invPos = NextPos;

			if ( invPos >= MyInventory.Count || invPos < 0 ) { State = InventoryStates.Idle; yield break; }
			if ( MyInventory[invPos] == null )
			{
				Util.Notify("Inventory item no longer valid at #" + invPos, VerboseLevels.Full);
				ValidateInventory();
				State = InventoryStates.Idle;
				yield break;
			}

			thing = MyInventory[invPos];

			LastPos = invPos;

			if (thing.P == null)
			{
				MyInventory.RemoveAt(invPos);
				State = InventoryStates.Idle;
				yield break;
			}

			if (AllowedTypes != ItemTypes.All)
			{
				//  Restrict certain types from being held ATM
				if (AllowedTypes == ItemTypes.VehicleSafe)
				{
					if (thing.isChainSaw) thing = (Thing)null;
					State = InventoryStates.Idle;
					yield break;
				}
			}

			GameObject instance = UnityEngine.Object.Instantiate(
				thing.G, thing.G.transform.position, Quaternion.identity) as GameObject;

			instance.name       = thing.Name;

			Clone               = instance.GetOrAddComponent<Thing>();
			Clone.isPersistant  = false;
			Clone.R.bodyType    = RigidbodyType2D.Dynamic;
			Clone.G.layer       = 9;
			Clone.Name          = instance.name;

			if (Puppet.IsInVehicle) Garage.NoCollisionsWithHeldItem(Clone); 

			yield return new WaitForFixedUpdate();

			Puppet.PG.Hold( Clone, hand );

			State = InventoryStates.Idle;
		}

		//
		// ─── E STORE THING ────────────────────────────────────────────────────────────────
		//
		IEnumerator IStoreThing( Thing thing )
		{
			float IeTimeout = Time.time + 2f;

			if (thing == null || thing.P == null) yield break;

			PM3 qResult;
			do
			{
				qResult = QFree(IeTimeout);

				if (qResult == PM3.Timeout) yield break;

				yield return new WaitForEndOfFrame();
			}
			while (qResult == PM3.Fail);

			State = InventoryStates.StoringItems;

			PM3 dropResult;

			Puppet.PG.Drop(thing);
			yield return new WaitForFixedUpdate();

			do
			{
				dropResult = ForceDrop( thing );
				if (dropResult == PM3.Timeout) {

					Util.Notify("<color=red>DROP TIMEOUT: <color=orange>" + thing.P.name + "</color>", VerboseLevels.Full);
					yield break;

				}
				yield return new WaitForEndOfFrame();

			} while ( dropResult == PM3.Fail ); 



			if (InInventory(thing)) {

				Thing storedThing = MyInventory[Locate(thing)];

				if (storedThing.P.GetHashCode() != thing.P.GetHashCode())
				{
					thing.Destroy();
					yield return new WaitForFixedUpdate();

					State = InventoryStates.Idle;

					yield break;
				}
			}

			thing.G.layer              = 10;
			thing.G.transform.position = (new Vector2(-100, -100));
			thing.R.position           = (new Vector2(1, 1));
			thing.MakePersistant();

			yield return new WaitForFixedUpdate();

			thing.R.bodyType           = RigidbodyType2D.Static;

			if (!InInventory(thing)) {
				MyInventory.Add(thing);
				Util.Notify(thing.Name + " <color=orange>stored in position #:</color> " + (MyInventory.Count), VerboseLevels.Minimal);
			}

			State = InventoryStates.Idle;

		}
	}
}