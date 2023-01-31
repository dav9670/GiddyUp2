using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace GiddyUp.Storage
{
    public class ExtendedDataStorage : WorldComponent, IExposable
    {
        Dictionary<int, ExtendedPawnData> _store = new Dictionary<int, ExtendedPawnData>();
        List<int> _idWorkingList;
        List<ExtendedPawnData> _extendedPawnDataWorkingList;
        public ExtendedDataStorage(World world) : base(world)
        {
        }
        public override void ExposeData()
        {
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

        // Return the associate extended data for a given Pawn, creating a new association
        // if required.
        public ExtendedPawnData GetExtendedDataFor(int pawnID)
        {
            if (_store.TryGetValue(pawnID, out ExtendedPawnData data))
            {
                return data;
            }

            var newExtendedData = new ExtendedPawnData(pawnID);

            _store[pawnID] = newExtendedData;
            return newExtendedData;
        }

        public void DeleteExtendedDataFor(Pawn pawn)
        {
            _store.Remove(pawn.thingIDNumber);
        }
    }
}
