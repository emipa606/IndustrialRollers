using System;
using System.Collections.Generic;
using System.Linq;
using MovingFloor;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorldIndustrialRollers;

[StaticConstructorOnStartup]
internal class MovingRailGreenPuller : Building, IRail
{
    public static Graphic[] graphic;

    private readonly string _graphicPathAdditionWoNumber = "_frame";

    private readonly int _updateEveryXTicks = 35;

    private bool _isWorking;
    private CompPowerTrader _power;

    private RailLogicHelper _railLogicHelper;

    private int _stuckTicks;

    private int _tickCounter;

    private string currentZoneAccessName;

    private string currentZoneDisplayName;

    public string DestinationStockPile;

    public bool doForever;

    private bool showZone = true;

    private bool stockPileSelected = true;

    public StorageSettings storageSettings = new StorageSettings();

    private StorageBuildingHelper.ZoneType zoneType;

    public bool StorageTabVisible => true;

    public override Graphic Graphic
    {
        get
        {
            if (_railLogicHelper == null)
            {
                return base.Graphic;
            }

            var localGraphic = _railLogicHelper.Graphic();
            if (localGraphic != null)
            {
                return localGraphic;
            }

            return base.Graphic;
        }
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
        return GetDestinationCell();
    }

    public IntVec3 GetNextPositionForDirection()
    {
        return GetDestinationCell();
    }

    public void UpdateGraphics()
    {
        var array = GetGraphic();
        if (array != null && array.Length != 0 && array[0] != null)
        {
            return;
        }

        var array2 = new Graphic[3];
        array = array2;
        var startIndex = def.graphicData.texPath.ToLower().LastIndexOf(GetGraphicPath(), StringComparison.Ordinal);
        var text = def.graphicData.texPath.Remove(startIndex);
        for (var i = 0; i < 3; i++)
        {
            var path = text + GetGraphicPath() + i;
            array[i] = GraphicDatabase.Get<Graphic_Single>(path, def.graphic.Shader, def.graphic.drawSize,
                def.graphic.Color, def.graphic.ColorTwo);
        }

        SetGraphic(array);
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

    public IntVec3 GetProperPlacementForItem(Thing item, IntVec3 wantedPlace)
    {
        return wantedPlace;
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
        _railLogicHelper = new RailLogicHelper(this, map);
        base.SpawnSetup(map, respawnAfterLoad);
        _power = GetComp<CompPowerTrader>();
    }

    public override void PrintForPowerGrid(SectionLayer layer)
    {
    }

    public void NewZoneSelected(string label, string displayName, Type actionType)
    {
        zoneType = StorageBuildingHelper.ZoneType.None;
        doForever = false;
        if (actionType == typeof(Action_Zone))
        {
            Util.Log("New stockpile selected : " + label);
            if (label.Equals("RimworldMovingRail_HOLD"))
            {
                ResetZone();
            }
            else if (label.Equals("RimworldMovingRail_DO_FOREVER"))
            {
                ResetZone();
                doForever = true;
            }
            else if (!label.NullOrEmpty())
            {
                currentZoneAccessName = currentZoneDisplayName = label;
                zoneType = StorageBuildingHelper.ZoneType.Stockpile;
            }
            else
            {
                ResetZone();
            }
        }
        else if (actionType == typeof(Action_StorageBuilding))
        {
            Util.Log("New storage building selected : " + label);
            currentZoneAccessName = label;
            currentZoneDisplayName = displayName;
            zoneType = StorageBuildingHelper.ZoneType.BuildingStorage;
        }
    }

    private void ResetZone()
    {
        currentZoneDisplayName = string.Empty;
        currentZoneAccessName = string.Empty;
        zoneType = StorageBuildingHelper.ZoneType.None;
    }

    private void SelectStockpilePressed()
    {
        stockPileSelected = !stockPileSelected;
        var list = new List<FloatMenuOption>
        {
            new FloatMenuOption("MovingRail_HOLD".Translate(),
                new Action_Zone("RimworldMovingRail_HOLD", this).GetAction()),
            new FloatMenuOption("MovingRail_Do_Forever".Translate(),
                new Action_Zone("RimworldMovingRail_DO_FOREVER", this).GetAction())
        };
        var list2 = _railLogicHelper.map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>().ToList();
        var allZones = _railLogicHelper.map.zoneManager.AllZones;
        foreach (var item in allZones)
        {
            if (!item.label.Equals(string.Empty) && item is Zone_Growing)
            {
                list.Add(new FloatMenuOption(item.label, new Action_Zone(item.label, this).GetAction()));
            }
        }

        foreach (var item2 in list2)
        {
            if (!item2.Label.Equals(string.Empty))
            {
                list.Add(new FloatMenuOption(item2.Label,
                    new Action_StorageBuilding(item2.ThingID, item2.Label, this).GetAction()));
            }
            else if (!item2.def.label.Equals(string.Empty))
            {
                list.Add(new FloatMenuOption(item2.def.label,
                    new Action_StorageBuilding(item2.ThingID, item2.def.label, this).GetAction()));
            }
        }

        var window = new FloatMenu(list);
        Find.WindowStack.Add(window);
    }

    public bool isActive()
    {
        return true;
    }

    public ThingFilter SetAllowAllForThingFilter(ThingFilter thingFilter)
    {
        foreach (var allThingCategoryNode in ThingCategoryNodeDatabase.allThingCategoryNodes)
        {
            Util.Log("loading cat: " + allThingCategoryNode.catDef);
            thingFilter.SetAllow(allThingCategoryNode.catDef, true);
        }

        return thingFilter;
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
        var growpileAt = _railLogicHelper.GetGrowpileAt(GetSourceCell());
        if (growpileAt != null)
        {
            _power.powerOutputInt = (float)Math.Min(0.0 - (growpileAt.cells.Count * 2.5), -60.0);
        }
        else
        {
            _power.powerOutputInt = -60f;
        }

        if (!GetPowerComp().PowerOn)
        {
            _railLogicHelper.SetCurrGraphicFrame(0);
            return;
        }

        if (growpileAt == null)
        {
            _isWorking = false;
            return;
        }

        ThingDef growingThing = null;
        try
        {
            growingThing = growpileAt.GetPlantDefToGrow().plant.harvestedThingDef;
        }
        catch (Exception)
        {
            // ignored
        }

        if (growingThing == null || _isWorking)
        {
            return;
        }

        _isWorking = true;
        if (_railLogicHelper.GetCurrGraphicFrame() != 1)
        {
            _railLogicHelper.SetCurrGraphicFrame(1);
        }

        var thing = growpileAt.AllContainedThings.FirstOrDefault(x => x.def.defName.Equals(growingThing.defName));
        if (thing != null)
        {
            var destinationCell = GetDestinationCell();
            var cellType = _railLogicHelper.NextCellContentType(destinationCell);
            Util.Log(thing.Label + " " + thing.ThingID);
            _railLogicHelper.SetCurrGraphicFrame(2);
            if (cellType == CellType.Rail)
            {
                thing.DeSpawn();
                GenPlace.TryPlaceThing(thing, destinationCell, _railLogicHelper.map, ThingPlaceMode.Direct);
            }
            else if (_railLogicHelper.map.zoneManager.ZoneAt(destinationCell) != null &&
                     _railLogicHelper.map.zoneManager.ZoneAt(destinationCell) is Zone_Stockpile)
            {
                _railLogicHelper.TryPlaceItemInStockpile(ref thing,
                    _railLogicHelper.map.zoneManager.ZoneAt(destinationCell) as Zone_Stockpile);
            }

            _railLogicHelper.map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
            if (cellType == CellType.Rail)
            {
                thing.SetForbidden(true, false);
            }
        }

        _isWorking = false;
        base.Tick();
    }

    public override void DrawExtraSelectionOverlays()
    {
        var list = new List<IntVec3>
        {
            GetSourceCell(),
            GetDestinationCell()
        };
        GenDraw.DrawFieldEdges(list, Color.cyan);
        var selectedZoneLoc = GetSelectedZoneLoc();
        if (selectedZoneLoc.HasValue)
        {
            GenDraw.DrawLineBetween(Position.ToVector3(), selectedZoneLoc.Value.ToVector3());
        }
    }

    private IntVec3? GetSelectedZoneLoc()
    {
        IntVec3? result = null;
        switch (zoneType)
        {
            case StorageBuildingHelper.ZoneType.Stockpile:
            {
                var zone_Growing =
                    (Zone_Growing)_railLogicHelper.map.zoneManager.AllZones.FirstOrDefault(x =>
                        x.label == currentZoneAccessName);
                if (zone_Growing != null)
                {
                    result = zone_Growing.Cells.FirstOrDefault();
                }

                break;
            }
            case StorageBuildingHelper.ZoneType.BuildingStorage:
            {
                var building_Storage =
                    (Building_Storage)_railLogicHelper.map.listerThings.AllThings.FirstOrDefault(x =>
                        x.ThingID == currentZoneAccessName);
                if (building_Storage != null)
                {
                    result = building_Storage.Position.ToVector3().ToIntVec3();
                }

                break;
            }
        }

        return result;
    }

    public StorageSettings GetStoreSettings()
    {
        return storageSettings;
    }

    private IntVec3 GetSourceCell()
    {
        var result = new IntVec3(Position.ToVector3());
        result.z += Rotation.FacingCell.z;
        result.y += Rotation.FacingCell.y;
        result.x += Rotation.FacingCell.x;
        return result;
    }

    private IntVec3 GetDestinationCell()
    {
        var result = new IntVec3(Position.ToVector3());
        result.z -= Rotation.FacingCell.z;
        result.y -= Rotation.FacingCell.y;
        result.x -= Rotation.FacingCell.x;
        return result;
    }

    public StorageSettings GetParentStoreSettings()
    {
        return def.building.fixedStorageSettings;
    }

    public StorageSettings GetStorageSettings()
    {
        return GetStoreSettings();
    }

    public void SetStorageSettings(StorageSettings settings)
    {
        storageSettings = settings;
    }
}