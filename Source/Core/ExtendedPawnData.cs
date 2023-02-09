using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
	public class ExtendedPawnData : IExposable
	{
		public int ID;
		public Pawn mount;
		public Pawn reservedMount, reservedBy; //Used by the rider and mount respectively. This creates a short-term association, like for example of a rider hops off for a few moments.
		public bool selectedForCaravan = false;
		public float drawOffset;
		public bool automount = true;
		
		public ExtendedPawnData() { }
		public ExtendedPawnData(int ID)
		{
			this.ID = ID;
		}
		public Pawn ReserveMount
		{
			set
			{
				if (Settings.logging && value == null) Log.Message("[Giddy-Up] pawn " + ID.ToString() + " no longer reserved to  " + (reservedMount?.thingIDNumber.ToString() ?? "NULL"));
				else if (Settings.logging && value != null) Log.Message("[Giddy-Up] pawn " + ID.ToString() + " now reserved to  " + (value?.thingIDNumber.ToString() ?? "NULL"));
				reservedMount = value;
			}
		}
		public void ExposeData()
		{
			Scribe_References.Look(ref mount, "mount");
			Scribe_References.Look(ref reservedBy, "reservedBy");
			Scribe_References.Look(ref reservedMount, "reservedMount");
			
			Scribe_Values.Look(ref ID, "ID");
			Scribe_Values.Look(ref automount, "automount", true);
			Scribe_Values.Look(ref drawOffset, "drawOffset");
			
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (mount != null) ExtendedDataStorage.isMounted.Add(ID);
			}
		}
	}
}