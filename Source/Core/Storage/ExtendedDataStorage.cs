using System.Collections.Generic;
using GiddyUpRideAndRoll;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp;

public class ExtendedDataStorage(World world) : WorldComponent(world)
{
    public static ExtendedDataStorage Singleton { get; private set; } = null!; //Singleton instance created on world init
    public static HashSet<int> isMounted = []; //This just serves as a cached logic gate
    public static HashSet<Thing>? noFleeingAnimals;
    
    private Dictionary<int, ExtendedPawnData> _extendedPawnDataStore = new(); //Pawn xData sorted via their thingID
    public IReadOnlyDictionary<int, ExtendedPawnData> ExtendedPawnDataStore => _extendedPawnDataStore;
    
    private readonly Dictionary<int, Area?> _areaNoMount = new();
    public IReadOnlyDictionary<int, Area?> AreaNoMount => _areaNoMount;
    
    private readonly Dictionary<int, Area?> _areaDropAnimal = new();
    public IReadOnlyDictionary<int, Area?> AreaDropAnimal => _areaDropAnimal;
    
    private HashSet<IntVec3> _badSpots = [];
    public IReadOnlyCollection<IntVec3> BadSpots => _badSpots;

    public void UpdateAreaCache(Map map, bool reset = false)
    {
        if (reset)
        {
            if (Settings.logging)
                Log.Message("[Giddy-Up] Registering area for map ID " + map.uniqueID.ToString());
            _areaNoMount.Add(map.uniqueID, null);
            _areaDropAnimal.Add(map.uniqueID, null);
        }

        var list = map.areaManager.areas;
        var length = list.Count;
        Area? areaNoMount = null;
        Area? areaDropAnimal = null;
        for (var i = 0; i < length; i++)
        {
            var area = list[i];
            var label = area.Label;
            switch (label)
            {
                case ResourceBank.AreaNoMount:
                    areaNoMount = area;
                    break;
                case ResourceBank.AreaDropMount:
                    areaDropAnimal = area;
                    break;
            }
        }

        _areaNoMount[map.uniqueID] = areaNoMount;
        _areaDropAnimal[map.uniqueID] = areaDropAnimal;
    }
    
    public ExtendedPawnData GetExtendedPawnData(Pawn? pawn)
    {
        var pawnID = pawn?.thingIDNumber ?? -1;
        
        if (_extendedPawnDataStore.TryGetValue(pawnID, out var data))
            return data;
        
        if (pawnID == -1)
            Log.Warning("[Giddy-Up] Invalid pawnID sent.");

        var newExtendedData = new ExtendedPawnData(pawn);

        _extendedPawnDataStore[pawnID] = newExtendedData;
        return newExtendedData;
    }
    
    public ExtendedPawnData? GetExtendedPawnDataReverseLookup(int pawnId)
    {
        foreach (var (_, value) in _extendedPawnDataStore)
            if (value.ReservedBy?.thingIDNumber == pawnId)
                return value;

        return null;
    }

    public void AddBadSpot(IntVec3 spot)
    {
        _badSpots.Add(spot);
    }
    
    public bool UpdateBadSpot(Pawn pawn, Area? areaDropAnimal, Area? areaNoMount, IntVec3 firstTarget, Pawn? bestAnimal)
    {
        if (!pawn.FindPlaceToDismount(areaDropAnimal, areaNoMount, firstTarget, out _, bestAnimal, out _))
            return false;
        
        _badSpots.Remove(firstTarget);
        return true;
    }

    public void CleanupExtendedPawnDataFromStore(ExtendedPawnData pawnData)
    {
        if (pawnData.ID is 0 or -1)
        {
            _extendedPawnDataStore.Remove(pawnData.ID);
        }
        else if (pawnData.Pawn == null || pawnData.Pawn.Dead)
        {
            if (Settings.logging)
                Log.Message("[Giddy-Up] clean up... removed ID: " + pawnData.ID);
            _extendedPawnDataStore.Remove(pawnData.ID);
        }
    }
    
    public override void FinalizeInit(bool fromLoad)
    {
        Singleton = this;
        isMounted = [];

        LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.GUC_Animal_Handling, OpportunityType.GoodToKnow);
        //Remove alert
        if (!Settings.rideAndRollEnabled)
            try
            {
                Find.Alerts.AllAlerts.RemoveAll(x => x.GetType() == typeof(Alert_NoDropAnimal));
            }
            catch
            {
                Log.Warning("[Giddy-Up] Failed to remove Alert_NoDropAnimal instance.");
            }

        if (Settings.battleMountsEnabled)
        {
            LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.BM_Mounting, OpportunityType.GoodToKnow);
            LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.BM_Enemy_Mounting, OpportunityType.GoodToKnow);
        }
    }

    public override void ExposeData()
    {
        var extendedPawnDataWorkingList = new List<ExtendedPawnData>();
        var idWorkingList = new List<int>();

        base.ExposeData();
        Scribe_Collections.Look(ref _extendedPawnDataStore, "store", LookMode.Value, LookMode.Deep, ref idWorkingList, ref extendedPawnDataWorkingList);
        Scribe_Collections.Look(ref _badSpots, "badSpots", LookMode.Value);

        //Validate data
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            var workingList = new List<int>();
            foreach (var (key, value) in _extendedPawnDataStore)
                if (value == null)
                    workingList.Add(key);
            _extendedPawnDataStore.RemoveAll(x => workingList.Contains(x.Key));
        }
        else if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            _extendedPawnDataStore ??= new Dictionary<int, ExtendedPawnData>();
            _badSpots ??= [];
        }
    }
}