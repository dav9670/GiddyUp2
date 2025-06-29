using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Jobs;

public class JobDriver_Mounted : JobDriver
{
    private static readonly Dictionary<JobDef, bool> AllowedJobs = new();
    
    public Pawn? Rider { get; private set; }

    
    private bool _isParking;
    public bool IsParking
    {
        get => _isParking;
        private set => _isParking = value;
    }

    private bool _isTrained;
    private bool _interrupted;
    private bool _isDespawning;
    private ExtendedPawnData _riderData;
    private Map _map;
    private IntVec3 _startingPoint, _dismountingAt, _riderOriginalDestination;
    private PathEndMode _originalPeMode = PathEndMode.Touch;
    private MountUtility.DismountLocationType _dismountLocationType = MountUtility.DismountLocationType.Auto;
    private int _parkingFailures;

    private enum DismountReason
    {
        False,
        Interrupted,
        BadState,
        LeftMap,
        NotSpawned,
        WrongMount,
        BadJob,
        ForbiddenAreaAndCannotPark,
        Parking,
        ParkingFailSafe
    };

    public static void BuildAllowedJobsCache(bool noMountedHunting)
    {
        var list = DefDatabase<JobDef>.defsList;
        for (var i = list.Count; i-- > 0;)
        {
            var def = list[i];
            if (def.GetModExtension<CanDoMounted>() is { } canDoMounted)
                AllowedJobs.Add(def, canDoMounted.checkTargets);
        }

        if (!noMountedHunting)
            AllowedJobs.AddDistinct(JobDefOf.Hunt, false);
    }

    public static void SetAllowedJob(JobDef jobDef, bool allow) => AllowedJobs.AddDistinct(jobDef, allow);

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        Rider = job.targetA.Thing as Pawn;
        _riderData = Rider.GetExtendedPawnData();
        _isTrained = pawn.training != null && pawn.training.HasLearned(TrainableDefOf.Obedience);
        _map = Map;
        _startingPoint = pawn.Position;
        yield return WaitForRider();
        yield return DelegateMovement();
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    private Toil WaitForRider() =>
        new()
        {
            defaultCompleteMode = ToilCompleteMode.Never,
            tickAction = delegate
            {
                //Rider just mounted up, finish toil
                if (_riderData.Mount == pawn)
                    ReadyForNextToil();

                //Something interrupted the rider, abort
                if (Current.gameInt.tickManager.ticksGameInt % 15 != 0) //Check 4 times per second
                    return;
                
                var curJobDef = Rider.CurJobDef;
                if (Rider == null || Rider.Dead || !Rider.Spawned || Rider.Downed || Rider.InMentalState ||
                    //Rider changed their mind
                    (curJobDef != ResourceBank.JobDefOf.Mount &&
                     curJobDef != JobDefOf.Vomit &&
                     curJobDef != JobDefOf.Wait_MaintainPosture &&
                     curJobDef != JobDefOf.Wait &&
                     _riderData.Mount == null) ||
                    //Rider is cheating on this mount and went with another
                    (Rider.CurJobDef == ResourceBank.JobDefOf.Mount &&
                     Rider.jobs.curDriver is JobDriver_Mount mountDriver && mountDriver.Mount != pawn))
                {
                    if (Settings.logging)
                        Log.Message("[Giddy-Up] " + pawn.Label + " is no longer waiting for " + Rider.Label);
                    _interrupted = true;
                    ReadyForNextToil();
                }
            }
        };

    private Toil DelegateMovement()
    {
        return new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Never,
            tickAction = delegate
            {
                _map ??= Map;
                
                if (CheckReason(RiderShouldDismount(_riderData), out var dismountReason))
                {
                    if (Settings.logging)
                        Log.Message("[Giddy-Up] " + pawn.Label + " dismounting for reason: " +
                                    dismountReason.ToString() + " (rider's job was: " +
                                    (Rider.CurJobDef?.ToString() ?? "NULL" + ")"));

                    //Check if something went wrong
                    if (dismountReason == DismountReason.ParkingFailSafe)
                        _interrupted = true;

                    ReadyForNextToil();
                    return;
                }

                pawn.Drawer.tweener = Rider.Drawer.tweener; //Could probably just be set once, but reloading could cause issues?
                pawn.Position = Rider.Position;
                pawn.Rotation = Rider.Rotation;
                if (_isTrained)
                    TryAttackEnemy(Rider);
            },
            finishActions = new List<Action>
            {
                delegate
                {
                    if (IsParking)
                        pawn.pather.StopDead();

                    //Check mount first. If it's null then they must have dismounted outside the driver's control
                    if (_riderData.Mount != null)
                        Rider.Dismount(
                            pawn,
                            _riderData,
                            false,
                            IsParking && pawn.Position.DistanceTo(_dismountingAt) < 5f ? _dismountingAt : default,
                            waitForRider: !_interrupted);
                    IsParking = false;

                    //Check if the mount was meant to despawn along with the rider. This is already handled in the RiderShouldDismount but some spaghetti code elsewhere could bypass it
                    //TODO: See if the two could be unified
                    if (!_isDespawning && Rider != null && !pawn.Faction.IsPlayer && !Rider.Spawned && pawn.Position.CloseToEdge(_map, ResourceBank.MapEdgeIgnore))
                    {
                        _isDespawning = true; //Avoid recurssive loop
                        pawn.ExitMap(false, CellRect.WholeMap(_map).GetClosestEdge(pawn.Position));
                    }
                }
            }
        };
    }

    private DismountReason RiderShouldDismount(ExtendedPawnData? riderData)
    {
        if (_interrupted || riderData == null || riderData.Mount == null || riderData.ID != Rider.thingIDNumber)
            return DismountReason.Interrupted;

        if (IsParking)
        {
            if ((_dismountLocationType == MountUtility.DismountLocationType.Auto &&
                 Rider.pather.nextCell == _dismountingAt) ||
                (_dismountLocationType != MountUtility.DismountLocationType.Auto &&
                 _dismountingAt.AdjacentTo8Way(Rider.pather.nextCell)))
            {
                Rider.pather.StartPath(_riderOriginalDestination, _originalPeMode); //Resume original work
                if (_startingPoint.DistanceTo(_dismountingAt) < 10f)
                    return DismountReason.ParkingFailSafe;
                else
                    return DismountReason.Parking;
            }

            if (Rider.pather.destination.Cell != _dismountingAt)
            {
                IsParking = false;
                if (_parkingFailures++ == 3)
                    return DismountReason.ParkingFailSafe; //Some sorta job is interferring with the parking, so just dismount.
            }
        }

        //Check remaining statements twice per second
        if (Current.gameInt.tickManager.ticksGameInt % 15 != 0)
            return DismountReason.False;

        //Check physical and mental health
        if (Rider.Downed || Rider.Dead || pawn.Downed || pawn.Dead ||
            pawn.HasAttachment(ThingDefOf.Fire) || Rider.HasAttachment(ThingDefOf.Fire) ||
            Rider.GetPosture() != PawnPosture.Standing ||
            pawn.InMentalState || (Rider.InMentalState && Rider.MentalState.def != MentalStateDefOf.PanicFlee) ||
            pawn.Faction != Rider.Faction //Quests can cause faction flips mid-mount
           )
            return DismountReason.BadState;

        //This will move the mount off the map, assuming their rider left the map as well
        if (!Rider.Spawned)
        {
            var riderIsColonist = Rider.IsColonist;
            if (riderIsColonist && Rider.GetCaravan() == null)
                return DismountReason.NotSpawned;
            
            pawn.ExitMap(riderIsColonist, CellRect.WholeMap(_map).GetClosestEdge(pawn.Position));
            return DismountReason.LeftMap;
        }

        var allowedJob = AllowedJobs.TryGetValue(Rider.CurJobDef, out var checkTargets);
        var riderDestination = Rider.pather.Destination.Cell;
        _map.GetGUAreas(out var areaNoMount, out var areaDropAnimal);

        if (!Rider.Drafted)
        {
            if (!IsParking && Settings.rideAndRollEnabled)
            {
                //Special checks for allowedJobs
                if (allowedJob && checkTargets && Rider.CurJob.targetA.Thing?.InteractionCell == riderDestination)
                    allowedJob = false;
                //If the mount's non-drafted rider is heading towards a forbidden area, they'll need to dismount
                if ((!allowedJob && Rider.Position.DistanceTo(riderDestination) < 25f) ||
                    !riderDestination.CanRideAt(areaNoMount))
                {
                    IsParking = true;
                    if (Rider.FindPlaceToDismount(areaDropAnimal, areaNoMount, riderDestination, out _dismountingAt, pawn,
                            out _dismountLocationType))
                    {
                        _riderOriginalDestination = riderDestination;
                        _originalPeMode = Rider.pather.peMode;
                        if (Settings.logging)
                            Log.Message("[Giddy-Up] " + Rider.Label + " wants to dismount at " +
                                        _dismountingAt.ToString() + " which is a " + _dismountLocationType.ToString());
                        Rider.pather.StartPath(_dismountingAt,
                            _dismountLocationType == MountUtility.DismountLocationType.Auto
                                ? PathEndMode.OnCell
                                : PathEndMode.Touch);
                    }
                    else
                    {
                        IsParking = false;
                        ExtendedDataStorage.Singleton.AddBadSpot(riderDestination);
                        return DismountReason.ForbiddenAreaAndCannotPark;
                    }
                }
            }
        }
        else
        {
            if (!allowedJob && Rider.Position.DistanceTo(Rider.pather.Destination.Cell) < ResourceBank.AutoHitchDistance)
                return DismountReason.BadJob;
            if (!pawn.Faction.def.isPlayer)
                return DismountReason.False;
        }

        if (!Settings.caravansEnabled)
            return DismountReason.False;
        
        var riderMindstateDef = Rider.mindState?.duty?.def;
        if (riderMindstateDef == DutyDefOf.TravelOrWait ||
            riderMindstateDef == DutyDefOf.TravelOrLeave ||
            riderMindstateDef == DutyDefOf.PrepareCaravan_GatherAnimals ||
            riderMindstateDef == DutyDefOf.PrepareCaravan_GatherDownedPawns)
            return riderData.ReservedMount == pawn ? DismountReason.False : DismountReason.WrongMount;

        if (Rider.Position.CloseToEdge(_map, ResourceBank.MapEdgeIgnore))
            return DismountReason.False; //Caravan just entered map and has not picked a job yet on this tick.

        return DismountReason.False;
    }

    private static bool CheckReason(DismountReason dismountReason, out DismountReason reason)
    {
        reason = dismountReason;
        return dismountReason != DismountReason.False;
    }

    private void TryAttackEnemy(Pawn rider)
    {
        Thing targetThing = null;
        var confirmedHostile = false;

        //The mount has something targeted but not the rider, so pass the target
        if (rider.TargetCurrentlyAimingAt != null)
        {
            targetThing = rider.TargetCurrentlyAimingAt.Thing;
        }
        //The rider is already trying to attack something
        else if (rider.CurJobDef == JobDefOf.AttackMelee && rider.CurJob.targetA.Thing.HostileTo(rider))
        {
            targetThing = rider.CurJob.targetA.Thing;
            confirmedHostile = true;
        }

        if (targetThing != null && (confirmedHostile || targetThing.HostileTo(rider)))
        {
            var modExt = pawn.def.GetModExtension<ResearchRestrictions>();
            if (modExt != null && modExt.researchProjectDefToAttack is { IsFinished: false })
                return;

            var verb = pawn.meleeVerbs?.TryGetMeleeVerb(targetThing);
            if (verb == null || !verb.CanHitTarget(targetThing))
                pawn.TryStartAttack(targetThing); //Try start ranged attack if possible
            else
                pawn.meleeVerbs?.TryMeleeAttack(targetThing);
        }
    }
    
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _isTrained, "isTrained");
        Scribe_Values.Look(ref _interrupted, "interrupted");
        Scribe_Values.Look(ref _isParking, "isParking");
        Scribe_Values.Look(ref _dismountingAt, "dismountingAt");
        Scribe_Values.Look(ref _dismountLocationType, "dismountLocationType");
        Scribe_Values.Look(ref _originalPeMode, "originalPeMode");
        Scribe_Values.Look(ref _riderOriginalDestination, "riderOriginalDestinaton");
    }
}