using Verse;
using Verse.AI;
using UnityEngine;

//Note: Currently this class contains information specific for other mods (caravanMount, caravanRider, etc), which is of course not ideal for a core framework. Ideally it should be completely generic. However I have yet to come up with an
// way to do this properly without introducing a lot of extra work. So for now I'll just keep it as it is. 

namespace GiddyUp.Storage
{
    public class ExtendedPawnData : IExposable
    {
        public int ID;
        public Pawn mount, caravanMount, caravanRider, ownedBy, owning;
        public bool selectedForCaravan = false;
        public float drawOffset;
        
        //used in Giddy-up Ride and Roll
        public Job targetJob = null;
        public bool mountableByAnyone = true, mountableByMaster, wasRidingToJob;

        public ExtendedPawnData() { }
        public ExtendedPawnData(int ID)
        {
            this.ID = ID;
        }
        public Pawn Mount
        {
             get { return mount; }
             set
             {
                //Log.Message("[Giddy-Up] pawn " + ID.ToString() + " is now mounted.");
                if (value == null) Setup.isMounted.Remove(ID);
                else Setup.isMounted.Add(ID);
                mount = value; 
             }
        }
        public void ExposeData()
        {
            Scribe_References.Look(ref mount, "mount", false);
            Scribe_References.Look(ref caravanMount, "caravanMount", false);
            Scribe_References.Look(ref caravanRider, "caravanRider", false);
            Scribe_References.Look(ref ownedBy, "ownedBy", false);
            Scribe_References.Look(ref owning, "owning", false);
            Scribe_References.Look(ref targetJob, "targetJob");

            Scribe_Values.Look(ref ID, "ID");
            Scribe_Values.Look(ref selectedForCaravan, "selectedForCaravan", false);
            Scribe_Values.Look(ref mountableByAnyone, "mountableByAnyone", true);
            Scribe_Values.Look(ref mountableByMaster, "mountableByMaster", true);
            Scribe_Values.Look(ref wasRidingToJob, "wasRidingToJob", false);
            Scribe_Values.Look(ref drawOffset, "drawOffset", 0);
            
        }

        public void Reset()
        {
            Mount = null;
        }
        
    }
}
