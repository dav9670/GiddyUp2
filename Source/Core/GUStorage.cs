using RimWorld;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
    static class StorageUtility
    {
        //Needs to be its own class as an extension
        public static ExtendedPawnData GetGUData(this Pawn pawn)
        {
            //return ExtendedDataStorage.GUComp[pawn.thingIDNumber];
            var _store = ExtendedDataStorage.GUComp._store;
            int pawnID = pawn?.thingIDNumber ?? -1;
            if (_store.TryGetValue(pawnID, out ExtendedPawnData data))
            {
                return data;
            }
            else if (pawnID == -1) Log.Warning("[Giddy-Up] Invalid pawnID sent.");

            var newExtendedData = new ExtendedPawnData(pawn);

            _store[pawnID] = newExtendedData;
            return newExtendedData;
        }
        public static bool IsMounted(this Thing pawn)
        {
            return ExtendedDataStorage.isMounted.Contains(pawn.thingIDNumber);
        }

        //Equiv version of the vanilla GetLabeled method, but it avoids iterating the list twice
        public static void UpdateAreaCache(this Map map, bool reset = false)
        {
            if (reset)
            {
                if (Settings.logging) Log.Message("[Giddy-Up] Registering area for map ID " + map.uniqueID.ToString());
                ExtendedDataStorage.GUComp.areaNoMount.Add(map.uniqueID, null);
			    ExtendedDataStorage.GUComp.areaDropAnimal.Add(map.uniqueID, null);
            }
            var list = map.areaManager.areas;
            var length = list.Count;
            Area areaNoMount = null;
            Area areaDropAnimal = null;
            for (int i = 0; i < length; i++)
            {
                var area = list[i];
                var label = area.Label;
                if (label == ResourceBank.AreaNoMount) areaNoMount = area;
                else if (label == ResourceBank.AreaDropMount) areaDropAnimal = area;
            }
            ExtendedDataStorage.GUComp.areaNoMount[map.uniqueID] = areaNoMount;
			ExtendedDataStorage.GUComp.areaDropAnimal[map.uniqueID] = areaDropAnimal;
        }
        public static bool GetGUAreas(this Map map, out Area areaNoMount, out Area areaDropAnimal)
        {
            ExtendedDataStorage.GUComp.areaNoMount.TryGetValue(map.uniqueID, out areaNoMount);
			ExtendedDataStorage.GUComp.areaDropAnimal.TryGetValue(map.uniqueID, out areaDropAnimal);

            return areaNoMount != null;
        }
        public static bool CanRideAt(this IntVec3 cell, Area noRideArea)
        {
            if (noRideArea == null) return true;
            return !noRideArea.innerGrid[noRideArea.Map.cellIndices.CellToIndex(cell)];
        }
    }
    public class ExtendedDataStorage : WorldComponent, IExposable
    {
        public static ExtendedDataStorage GUComp; //Singleton instance creaed on world init
        public static HashSet<int> isMounted = new HashSet<int>(); //This just serves as a cached logic gate
        public static HashSet<Thing> nofleeingAnimals;
        public Dictionary<int, ExtendedPawnData> _store = new Dictionary<int, ExtendedPawnData>(); //Pawn xData sorted via their thingID
        public Dictionary<int, Area> areaNoMount = new Dictionary<int, Area>(), areaDropAnimal = new Dictionary<int, Area>();
        public ExtendedDataStorage(World world) : base(world) {}

        public override void FinalizeInit()
        {
            GUComp = this;
            ExtendedDataStorage.isMounted = new HashSet<int>();

            LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.GUC_Animal_Handling, OpportunityType.GoodToKnow);
            //Remove alert
            if (!Settings.rideAndRollEnabled)
            {
                try { Find.Alerts.AllAlerts.RemoveAll(x => x.GetType() == typeof(GiddyUpRideAndRoll.Alert_NoDropAnimal)); }
                catch (System.Exception) { Log.Warning("[Giddy-Up] Failed to remove Alert_NoDropAnimal instance."); }
            }

            //BM
            if (Settings.battleMountsEnabled)
            {
                LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.BM_Mounting, OpportunityType.GoodToKnow);
                LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.BM_Enemy_Mounting, OpportunityType.GoodToKnow);
            }
        }
        public override void ExposeData()
        {
            List<ExtendedPawnData> _extendedPawnDataWorkingList = new List<ExtendedPawnData>();
            List<int> _idWorkingList = new List<int>();
            
            base.ExposeData();
            Scribe_Collections.Look(ref _store, "store", LookMode.Value, LookMode.Deep, ref _idWorkingList, ref _extendedPawnDataWorkingList);
            
            //Validate data
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var workingList = new List<int>();
                foreach (var (key, value) in _store) if (value == null) workingList.Add(key);
                _store.RemoveAll(x => workingList.Contains(x.Key));
            }
        }
        
        //Only used for sanity check and cleanup
        public bool ReverseLookup(int ID, out ExtendedPawnData pawnData)
        {
            foreach (var (key, value) in _store)
            {
                if (value.reservedBy?.thingIDNumber == ID)
                {
                    pawnData = value;
                    return true;
                }
            }
            pawnData = new ExtendedPawnData(null);
            Log.Warning("[Giddy-Up] A reverse lookup request failed to find anything. Returning fallback instance...");
            return false;
        }
    }
    public class ExtendedPawnData : IExposable
	{
		public Pawn pawn;
        public int ID;
		public Pawn mount;
		public Pawn reservedMount, reservedBy; //Used by the rider and mount respectively. This creates a short-term association, like for example of a rider hops off for a few moments.
		public bool selectedForCaravan = false;
		public float drawOffset;
        public Automount automount = Automount.Anyone; public enum Automount { False, Anyone, Colonists, Slaves };
		
		public ExtendedPawnData() { }
		public ExtendedPawnData(Pawn pawn)
		{
			this.pawn = pawn;
            this.ID = pawn?.thingIDNumber ?? -1;
		}
		public Pawn ReservedMount
		{
			set
			{
				if (Settings.logging && value == null) Log.Message("[Giddy-Up] pawn " + ID.ToString() + " no longer reserved to  " + (reservedMount?.thingIDNumber.ToString() ?? "NULL"));
				else if (Settings.logging && value != null) Log.Message("[Giddy-Up] pawn " + ID.ToString() + " now reserved to  " + (value?.thingIDNumber.ToString() ?? "NULL"));
				reservedMount = value;
			}
		}
		public Pawn ReservedBy
		{
			set
			{
				if (Settings.logging && value == null) Log.Message("[Giddy-Up] animal " + ID.ToString() + " no longer reserved to  " + (reservedBy?.thingIDNumber.ToString() ?? "NULL"));
				else if (Settings.logging && value != null) Log.Message("[Giddy-Up] animal " + ID.ToString() + " now reserved to  " + (value?.thingIDNumber.ToString() ?? "NULL"));
				reservedBy = value;
			}
		}
		public void ExposeData()
		{
			Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref mount, "mount");
			Scribe_References.Look(ref reservedBy, "reservedBy");
			Scribe_References.Look(ref reservedMount, "reservedMount");
			
			Scribe_Values.Look(ref ID, "ID");
			Scribe_Values.Look(ref automount, "automount", Automount.Anyone);
			Scribe_Values.Look(ref drawOffset, "drawOffset");
			
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (mount != null) ExtendedDataStorage.isMounted.Add(ID);
                if (Settings.disableSlavePawnColumn && automount == Automount.Slaves) automount = Automount.False;

                //Remove any invalid entries that somehow slipped into the store.
                if (ID == 0) ExtendedDataStorage.GUComp._store.Remove(0);
                else if (ID == -1) ExtendedDataStorage.GUComp._store.Remove(-1);
                else if (pawn == null || pawn.Dead)
                {
                    if (Settings.logging) Log.Message("[Giddy-Up] clean up... removed ID: " + ID.ToString());
                    ExtendedDataStorage.GUComp._store.Remove(ID);
                }
			}
		}
	}
}