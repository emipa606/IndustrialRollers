using System.Collections.Generic;
using MovingFloor;
using RimWorld;
using Verse;

namespace RimWorldIndustrialRollers;

[StaticConstructorOnStartup]
public class MovingRailUndergroundInput : Building, IRail
{
    public static Graphic[] graphic;

    private readonly string _graphicPathAdditionWoNumber = "_frame";

    private readonly int _updateEveryXTicks = 35;

    private CompPowerTrader _power;

    private RailLogicHelper _railLogicHelper;

    private int _stuckTicks;

    private int _tickCounter = 1;

    public MovingRailUndergroundOutput OutputRoller;

    public int QueueSize;

    public List<Thing> ThingQueueThings;

    public List<int> ThingQueueTicksInQueue;

    public void UpdateGraphics()
    {
        _railLogicHelper.UpdateGraphics();
    }

    public bool IsCellBasedSkip()
    {
        return false;
    }

    public bool ShouldSkipForItemInPos(IntVec3 pos)
    {
        return false;
    }

    public IntVec3 GetProperPlacementForItem(Thing item, IntVec3 wantedPlace)
    {
        return wantedPlace;
    }

    public IntVec3 GetInteractionCell()
    {
        return Position;
    }

    public IntVec3[] GetInteractionCells()
    {
        return null;
    }

    public bool HasMultipleInteractionCells()
    {
        return false;
    }

    public void SetShouldSkipForItemInPos(IntVec3 pos, bool shouldSkip)
    {
    }

    public IntVec3 GetNextPositionForThing(Thing thing)
    {
        var result = default(IntVec3);
        result.z += (int)(thing.Position.ToVector3().z + Rotation.FacingCell.z);
        result.y += (int)(thing.Position.ToVector3().y + Rotation.FacingCell.y);
        result.x += (int)(thing.Position.ToVector3().x + Rotation.FacingCell.x);
        return result;
    }

    public IntVec3 GetNextPositionForDirection()
    {
        var result = default(IntVec3);
        result.z += (int)(Position.ToVector3().z + Rotation.FacingCell.z);
        result.y += (int)(Position.ToVector3().y + Rotation.FacingCell.y);
        result.x += (int)(Position.ToVector3().x + Rotation.FacingCell.x);
        return result;
    }

    public Graphic[] GetGraphic()
    {
        return graphic;
    }

    public void SetGraphic(Graphic[] graphics)
    {
        graphic = graphics;
    }

    public string GetGraphicPath()
    {
        return _graphicPathAdditionWoNumber;
    }

    public CompPowerTrader GetPowerComp()
    {
        return _power;
    }

    public void SetTicksSinceItemStuck(int ticks)
    {
        _stuckTicks = ticks;
    }

    public int GetTicksSinceItemStuck()
    {
        return _stuckTicks;
    }

    public void AddStuckTick()
    {
        _stuckTicks++;
    }

    public override void SpawnSetup(Map map, bool respawnAfterLoad)
    {
        Util.Log("Making Logic Helper");
        _railLogicHelper = new RailLogicHelper(this, map);
        base.SpawnSetup(map, respawnAfterLoad);
        _power = GetComp<CompPowerTrader>();
    }

    public MovingRailUndergroundOutput GetOutputRoller()
    {
        return OutputRoller;
    }

    public void SetOutputRoller(MovingRailUndergroundOutput outputRoller)
    {
        OutputRoller = outputRoller;
        QueueSize = 0;
        ThingQueueTicksInQueue = new List<int>();
        ThingQueueThings = new List<Thing>();
        if (OutputRoller != null)
        {
            QueueSize = (int)Position.DistanceTo(OutputRoller.Position);
            return;
        }

        QueueSize = 0;
        if (ThingQueueThings != null)
        {
            ThingQueueThings.ForEach(delegate(Thing x)
            {
                GenPlace.TryPlaceThing(x, Position, _railLogicHelper.map, ThingPlaceMode.Near);
            });
        }

        ThingQueueThings = new List<Thing>();
        ThingQueueTicksInQueue = new List<int>();
    }

    public override void PrintForPowerGrid(SectionLayer layer)
    {
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (OutputRoller != null)
        {
            OutputRoller.SetInputRoller(null);
            Util.Log("Input roller unassigned itself from output roller");
        }

        if (ThingQueueThings != null && ThingQueueTicksInQueue != null)
        {
            foreach (var thingQueueThing in ThingQueueThings)
            {
                GenPlace.TryPlaceThing(thingQueueThing, Position, _railLogicHelper.map, ThingPlaceMode.Near);
            }
        }

        ThingQueueThings = null;
        ThingQueueTicksInQueue = null;
        QueueSize = 0;
        base.Destroy(mode);
    }

    public override void DrawExtraSelectionOverlays()
    {
        _railLogicHelper.DrawArrow();
        base.DrawExtraSelectionOverlays();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        PreSave();
        Scribe_References.Look(ref OutputRoller, "OutputRoller");
        Scribe_Values.Look(ref QueueSize, "QueueSize");
        Scribe_Collections.Look(ref ThingQueueThings, "ThingQueueThings", LookMode.Deep);
        Scribe_Collections.Look(ref ThingQueueTicksInQueue, "ThingQueueTicksInQueue", LookMode.Value);
    }

    private void PreSave()
    {
    }

    public override void Tick()
    {
        if (_tickCounter >= _updateEveryXTicks)
        {
            DoTickerWork();
            _tickCounter = 0;
        }

        _tickCounter++;
        TickRare();
    }

    private void DoTickerWork()
    {
        if (!GetPowerComp().PowerOn)
        {
            return;
        }

        if (ThingQueueThings == null || ThingQueueTicksInQueue == null)
        {
            Util.Log("Couldnt load ThingQueue!: ThingQueueThings == null - " + (ThingQueueThings == null) +
                     " ThingQueueTicksInQueue == null - " + (ThingQueueTicksInQueue == null));
            return;
        }

        if (OutputRoller == null)
        {
            Util.Log("No output roller");
            return;
        }

        var thing = _railLogicHelper.map.thingGrid.ThingAt(Position, ThingCategory.Item);
        if (thing != null && ThingQueueThings.Count < QueueSize)
        {
            ThingQueueThings.Add(thing);
            ThingQueueTicksInQueue.Add(0);
            thing.DeSpawn();
        }

        var num = -1;
        var num2 = -1;
        for (var i = 0; i < ThingQueueTicksInQueue.Count; i++)
        {
            ThingQueueTicksInQueue[i] += 1;
            if (ThingQueueTicksInQueue[i] <= num2 || ThingQueueTicksInQueue[i] < QueueSize)
            {
                continue;
            }

            num2 = ThingQueueTicksInQueue[i];
            num = i;
        }

        if (num == -1 || !OutputRoller.CanAcceptItem())
        {
            return;
        }

        OutputRoller.TakeItem(ThingQueueThings[num]);
        ThingQueueTicksInQueue.RemoveAt(num);
        ThingQueueThings.RemoveAt(num);
    }

    public class ThingInQueue
    {
        public Thing Thing;

        public int TicksInQueue;
    }
}