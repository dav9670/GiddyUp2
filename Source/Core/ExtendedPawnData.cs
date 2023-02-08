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
				if (Settings.logging) Log.Message("[Giddy-Up] pawn " + ID.ToString() + " no longer reserved to  " + reservedMount?.thingIDNumber.ToString() ?? "NULL");
				reservedMount = value;
			}
		}
		public Pawn Mount
		{
			set
			{
				if (value == null) 
				{
					if (Settings.logging) Log.Message("[Giddy-Up] pawn " + ID.ToString() + " no longer mounted upon " + mount?.thingIDNumber.ToString() ?? "NULL");
					ExtendedDataStorage.isMounted.Remove(ID);
					drawOffset = 0f;
				}
				else
				{
					if (Settings.logging) Log.Message("[Giddy-Up] pawn " + ID.ToString() + " is now mounted upon " + value.thingIDNumber.ToString());
					ExtendedDataStorage.isMounted.Add(ID);
					//Break ropes if there are any
					if (value.roping?.IsRoped ?? false) value.roping.BreakAllRopes();
					//Set the offset
					drawOffset = TextureUtility.FetchCache(value);
				}
				mount = value; 
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
		
		//TODO: improve and refactor this
		public void Reset()
		{
			Mount = null;
		}       
	}
}