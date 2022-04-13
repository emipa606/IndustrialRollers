using System.Linq;
using RimWorld;
using RimWorldIndustrialRollers;
using Verse;

namespace MovingFloor;

[StaticConstructorOnStartup]
public class MovingRailSplitter : Building, IRail
{
    public static Graphic[] graphic;

    private readonly string _graphicPathAdditionWoNumber = "_frame";

    private readonly int _updateEveryXTicks = 35;

    private bool _hasThing;

    private CompPowerTrader _power;
    private RailLogicHelper _railLogicHelper;

    private bool[] _shouldSkip;

    private bool _side;

    private int _stuckTicks;

    private int _tickCounter = 1;

    public override Graphic Graphic
    {
        get
        {
            var localGraphic = _railLogicHelper.Graphic();
            if (localGraphic != null)
            {
                return localGraphic;
            }

            return base.Graphic;
        }
    }

    public void UpdateGraphics()
    {
        _railLogicHelper.UpdateGraphics();
    }

    public bool IsCellBasedSkip()
    {
        return true;
    }

    public bool ShouldSkipForItemInPos(IntVec3 pos)
    {
        if (pos == Position)
        {
            return _shouldSkip[0];
        }

        return _shouldSkip[1];
    }

    public void SetShouldSkipForItemInPos(IntVec3 pos, bool shouldSkip)
    {
        if (pos == Position)
        {
            _shouldSkip[0] = shouldSkip;
        }
        else
        {
            _shouldSkip[1] = shouldSkip;
        }
    }

    public IntVec3 GetInteractionCell()
    {
        return Position;
    }

    public IntVec3[] GetInteractionCells()
    {
        return new[]
        {
            Position,
            GetOtherSidePos()
        };
    }

    public bool HasMultipleInteractionCells()
    {
        return true;
    }

    public IntVec3 GetProperPlacementForItem(Thing item, IntVec3 wantedPlace)
    {
        var intVec = wantedPlace;
        var intVec2 = Position;
        if (_side && wantedPlace == Position)
        {
            intVec = GetOtherSidePos();
        }
        else if (!_side && wantedPlace != Position)
        {
            intVec = Position;
            intVec2 = GetOtherSidePos();
        }

        if (_railLogicHelper.map.thingGrid.ThingAt(intVec, ThingCategory.Item) != null)
        {
            intVec = intVec2;
        }

        _side = !_side;
        return intVec;
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
        _shouldSkip = new bool[2];
        _power = GetComp<CompPowerTrader>();
    }

    public StorageSettings GetStorageSettings()
    {
        return new StorageSettings();
    }

    public void SetStorageSettings(StorageSettings settings)
    {
    }

    public override void PrintForPowerGrid(SectionLayer layer)
    {
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
        var array = new[]
        {
            _railLogicHelper.map.thingGrid.ThingAt(Position, ThingCategory.Item),
            _railLogicHelper.map.thingGrid.ThingAt(GetOtherSidePos(), ThingCategory.Item)
        };
        if (!GetPowerComp().PowerOn)
        {
            return;
        }

        foreach (var thing in array)
        {
            _hasThing = thing != null;
            _railLogicHelper.map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
            if (!_hasThing)
            {
                continue;
            }

            _hasThing = true;
            var nextPositionForThing = GetNextPositionForThing(thing);
            Util.Log(thing?.ThingID + " next pos: " + nextPositionForThing);
            var cellType = _railLogicHelper.NextCellContentType(nextPositionForThing);
            Util.Log("cellContentType" + cellType + " for item ");
            switch (cellType)
            {
                case CellType.Occupied:
                    Util.Log("cell is blocked by a building");
                    AddStuckTick();
                    thing.SetForbidden(false, false);
                    break;
                case CellType.Item:
                {
                    var thing2 = _railLogicHelper.map.thingGrid.ThingAt(nextPositionForThing, ThingCategory.Item);
                    Util.Log("cell is blocked by an item");
                    Util.Log(thing2.ThingID);
                    _railLogicHelper.map.thingGrid.ThingAt(nextPositionForThing, ThingCategory.Item)
                        .TryAbsorbStack(thing, true);
                    AddStuckTick();
                    if (GetTicksSinceItemStuck() >= 6 && thing is { Destroyed: false })
                    {
                        thing.SetForbidden(false, false);
                    }

                    break;
                }
                case CellType.Rail:
                {
                    thing?.DeSpawn();
                    var rail = (IRail)_railLogicHelper.map.thingGrid.ThingsListAtFast(nextPositionForThing)
                        .FirstOrDefault(_railLogicHelper.IsBuildingBelt);
                    if (rail != null)
                    {
                        _railLogicHelper.MakeThingCopyAtPos(thing,
                            rail.GetProperPlacementForItem(thing, nextPositionForThing));
                        _railLogicHelper.map.mapDrawer.MapMeshDirty(nextPositionForThing, MapMeshFlag.Things, true,
                            false);
                    }

                    break;
                }
                case CellType.Empty:
                    thing.SetForbidden(false, false);
                    break;
            }
        }
    }

    private IntVec3 GetOtherSidePos()
    {
        var result = new IntVec3(Position.ToVector3());
        switch (Rotation.AsInt)
        {
            case 0:
                result.x++;
                break;
            case 1:
                result.z--;
                break;
            case 2:
                result.x--;
                break;
            case 3:
                result.z++;
                break;
        }

        return result;
    }
}