using Verse;
using Verse.AI;
using UnityEngine;

//Note: Currently this class contains information specific for other mods (caravanMount, caravanRider, etc), which is of course not ideal for a core framework. Ideally it should be completely generic. However I have yet to come up with an
// way to do this properly without introducing a lot of extra work. So for now I'll just keep it as it is. 

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
		public Pawn Mount
		{
			 set
			 {
				if (value == null) 
				{
					//if (Prefs.DevMode) Log.Message("[Giddy-Up] pawn " + ID.ToString() + " no longer now mounted upon " + mount?.thingIDNumber.ToString() ?? "NULL");
					ExtendedDataStorage.isMounted.Remove(ID);
				}
				else
				{
					//if (Prefs.DevMode) Log.Message("[Giddy-Up] pawn " + ID.ToString() + " is now mounted upon " + value.thingIDNumber.ToString());
					ExtendedDataStorage.isMounted.Add(ID);
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

		public void Reset()
		{
			Mount = null;
		}       
	}
}