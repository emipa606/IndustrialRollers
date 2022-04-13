using System;
using System.Collections.Generic;
using MovingFloor;
using RimWorld;
using Verse;

namespace RimWorldIndustrialRollers;

[StaticConstructorOnStartup]
public class MovingRailUndergroundOutput : Building, IRail
{
    public static readonly int MAX_BUILD_DISTANCE = 6;

    public static Graphic[] graphic;

    private readonly string _graphicPathAdditionWoNumber = "_frame";

    private readonly int _updateEveryXTicks = 35;

    private CompPowerTrader _power;

    private RailLogicHelper _railLogicHelper;

    private int _stuckTicks;

    private int _tickCounter = 1;

    public MovingRailUndergroundInput InputRoller;

    public void UpdateGraphics()
    {
        _railLogicHelper.UpdateGraphics();
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

    public bool IsCellBasedSkip()
    {
        return false;
    }

    public bool ShouldSkipForItemInPos(IntVec3 pos)
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

    public void SetInputRoller(MovingRailUndergroundInput inputRoller)
    {
        InputRoller = inputRoller;
    }

    public override void SpawnSetup(Map map, bool respawnAfterLoad)
    {
        Util.Log("Making Logic Helper");
        _railLogicHelper = new RailLogicHelper(this, map);
        base.SpawnSetup(map, respawnAfterLoad);
        Util.Log("OUTPUT Spawned with data input roller: " + (InputRoller != null));
        if (InputRoller == null)
        {
            var possibleInputRollerTiles = GetPossibleInputRollerTiles();
            foreach (var item in possibleInputRollerTiles)
            {
                foreach (var item2 in map.thingGrid.ThingsAt(item))
                {
                    if (item2.def.category != ThingCategory.Building || item2.Rotation != Rotation ||
                        item2 is not MovingRailUndergroundInput movingRailUndergroundInput)
                    {
                        continue;
                    }

                    if (movingRailUndergroundInput.GetOutputRoller() != null &&
                        movingRailUndergroundInput.GetOutputRoller() != this)
                    {
                        continue;
                    }

                    movingRailUndergroundInput.SetOutputRoller(this);
                    SetInputRoller(movingRailUndergroundInput);
                    break;
                }
            }
        }

        _power = GetComp<CompPowerTrader>();
    }

    private List<IntVec3> GetPossibleInputRollerTiles()
    {
        var list = new List<IntVec3>();
        var num = 0;
        switch ((int)Math.Floor(Rotation.AsAngle))
        {
            case 0:
                num = 2;
                break;
            case 90:
                num = 3;
                break;
            case 180:
                num = 0;
                break;
            case 270:
                num = 1;
                break;
        }

        var intVec = Position + GenAdj.CardinalDirections[num];
        for (var i = 0; i <= IndustrialRollersMod.UndergroundRollerDistance; i++)
        {
            list.Add(new IntVec3(intVec.ToVector3()));
            intVec += GenAdj.CardinalDirections[num];
        }

        return list;
    }

    public StorageSettings GetStorageSettings()
    {
        return new StorageSettings();
    }

    public override void PrintForPowerGrid(SectionLayer layer)
    {
    }

    public void SetStorageSettings(StorageSettings settings)
    {
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (InputRoller != null)
        {
            InputRoller.SetOutputRoller(null);
            Util.Log("Output roller unassigned itself from input roller");
        }

        base.Destroy(mode);
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref InputRoller, "InputRoller");
        base.ExposeData();
    }

    public override void DrawExtraSelectionOverlays()
    {
        _railLogicHelper.DrawArrow();
        base.DrawExtraSelectionOverlays();
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
        _railLogicHelper.DoTickerWork();
    }

    public bool CanAcceptItem()
    {
        return _railLogicHelper.map.thingGrid.ThingAt(Position, ThingCategory.Item) == null;
    }

    public void TakeItem(Thing item)
    {
        if (item != null)
        {
            _railLogicHelper.MakeThingCopyAtPos(item, Position);
        }
    }
}