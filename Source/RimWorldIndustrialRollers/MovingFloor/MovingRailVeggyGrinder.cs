using RimWorld;
using RimWorldIndustrialRollers;
using Verse;

namespace MovingFloor;

[StaticConstructorOnStartup]
internal class MovingRailVeggyGrinder : Building, IStoreSettingsParent, IRail
{
    public static Graphic[] graphic;

    private readonly string _graphicPathAdditionWoNumber = "_frame";

    private readonly int _updateEveryXTicks = 35;

    private CompPowerTrader _power;
    private RailLogicHelper _railLogicHelper;

    private int _stuckTicks;

    private int _tickCounter = 1;

    public StorageSettings storageSettings = new StorageSettings();

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
        var result = new IntVec3(thing.Position.ToVector3());
        result.z += Rotation.FacingCell.z;
        result.y += Rotation.FacingCell.y;
        result.x += Rotation.FacingCell.x;
        return result;
    }

    public IntVec3 GetNextPositionForDirection()
    {
        var result = new IntVec3(Position.ToVector3());
        result.z += Rotation.FacingCell.z;
        result.y += Rotation.FacingCell.y;
        result.x += Rotation.FacingCell.x;
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

    public bool StorageTabVisible => true;

    public StorageSettings GetStoreSettings()
    {
        return storageSettings;
    }

    public StorageSettings GetParentStoreSettings()
    {
        return def.building.fixedStorageSettings;
    }

    public override void SpawnSetup(Map map, bool respawnAfterLoad)
    {
        _railLogicHelper = new RailLogicHelper(this, map);
        base.SpawnSetup(map, respawnAfterLoad);
        _power = GetComp<CompPowerTrader>();
    }

    public override void PostMake()
    {
        base.PostMake();
        storageSettings = new StorageSettings(this);
        if (def.building.defaultStorageSettings != null)
        {
            storageSettings.CopyFrom(def.building.defaultStorageSettings);
        }
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

        var sourceCell = GetSourceCell();
        var thing = _railLogicHelper.map.thingGrid.ThingAt(Position, ThingCategory.Item);
        var thing2 = thing ?? _railLogicHelper.map.thingGrid.ThingAt(sourceCell, ThingCategory.Item);
        _railLogicHelper.SetCurrGraphicFrame(thing2 != null ? 1 : 0);
        _railLogicHelper.map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
        if (thing2 != null)
        {
            var nextPositionForDirection = GetNextPositionForDirection();
            var cellType = _railLogicHelper.NextCellContentType(nextPositionForDirection);
            Util.Log(thing2.Label + " " + thing2.ThingID);
            if (storageSettings.AllowedToAccept(thing2) && (cellType == CellType.Empty || cellType == CellType.Rail))
            {
                var thing3 = !def.defName.Equals("MovingRailMeatGrinder")
                    ? ThingMaker.MakeThing(ThingDef.Named("VeggyMix"))
                    : ThingMaker.MakeThing(ThingDef.Named("GroundMeat"));
                thing3.stackCount = thing2.stackCount;
                GenPlace.TryPlaceThing(thing3, nextPositionForDirection, _railLogicHelper.map, ThingPlaceMode.Direct);
                thing2.DeSpawn();
            }
        }

        _railLogicHelper.map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
        base.Tick();
    }

    public override void DrawExtraSelectionOverlays()
    {
        _railLogicHelper.DrawArrow();
        base.DrawExtraSelectionOverlays();
    }

    private IntVec3 GetSourceCell()
    {
        var result = new IntVec3(Position.ToVector3());
        result.z -= Rotation.FacingCell.z;
        result.y -= Rotation.FacingCell.y;
        result.x -= Rotation.FacingCell.x;
        return result;
    }

    public override void PrintForPowerGrid(SectionLayer layer)
    {
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref storageSettings, "storageSettings", this);
    }
}