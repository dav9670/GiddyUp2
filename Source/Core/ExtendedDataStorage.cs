using RimWorld;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
    public class ExtendedDataStorage : WorldComponent, IExposable
    {
        public static ExtendedDataStorage GUComp; //Singleton instance creaed on world init
        public static HashSet<int> isMounted = new HashSet<int>(); //This just serves as a cached logic gate
        Dictionary<int, ExtendedPawnData> _store = new Dictionary<int, ExtendedPawnData>(); //Pawn xData sorted via their thingID
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
                catch (System.Exception) { Log.Warning("[Giddy-up] Failed to remove Alert_NoDropAnimal instance."); }
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
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var workingList = new List<int>();
                foreach (var item in _store)
                {
                    if (item.Value == null || item.Value.ID == 0) workingList.Add(item.Key);
                }
                _store.RemoveAll(x => workingList.Contains(x.Key));
            }
        }
        public ExtendedPawnData this[int pawnID]
        {
            get
            {
                if (_store.TryGetValue(pawnID, out ExtendedPawnData data))
                {
                    return data;
                }

                var newExtendedData = new ExtendedPawnData(pawnID);

                _store[pawnID] = newExtendedData;
                return newExtendedData;
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
            pawnData = null;
            return false;
        }
    }
}