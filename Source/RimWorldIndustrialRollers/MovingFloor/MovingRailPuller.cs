using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldIndustrialRollers;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MovingFloor;

[StaticConstructorOnStartup]
internal class MovingRailPuller : Building, IStoreSettingsParent, IRail
{
    public static Graphic[] graphic;

    private readonly string _graphicPathAdditionWoNumber = "_frame";

    private readonly int _updateEveryXTicks = 35;

    private int _currentHour = -1;

    private int _hourCheckTickRate = 1000;

    private bool _isWorking;
    private CompPowerTrader _power;

    private RailLogicHelper _railLogicHelper;

    private int _stuckTicks;

    private int _tickCounter;

    private int _ticksSinceLastHourCheck;

    private string currentZoneAccessName;

    private string currentZoneDisplayName;

    public string DestinationStockPile;

    public bool doForever = true;

    public int EveryHours = 1;

    public int LastOperationAtHour = -999;

    public int MaxPullsPerTime;

    public int PullsThisTimeFrame;

    private bool showZone = true;

    private bool stockPileSelected = true;

    public StorageSettings storageSettings = new StorageSettings();

    private StorageBuildingHelper.ZoneType zoneType;

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

    public IntVec3 GetProperPlacementForItem(Thing item, IntVec3 wantedPlace)
    {
        return wantedPlace;
    }

    public Graphic[] GetGraphic()
    {
        return graphic;
    }

    public void SetGraphic(Graphic[] graphicData)
    {
        graphic = graphicData;
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
        _currentHour = GenLocalDate.HourInteger(_railLogicHelper.map);
        _hourCheckTickRate = Util.RandomBetween(200, 600);
        base.SpawnSetup(map, respawnAfterLoad);
        _power = GetComp<CompPowerTrader>();
    }

    public void UpdatePullsPerHour(int everyHours, int pullsPerHour)
    {
        MaxPullsPerTime = pullsPerHour;
        EveryHours = everyHours;
        PullsThisTimeFrame = 0;
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

    public override IEnumerable<Gizmo> GetGizmos()
    {
        IList<Gizmo> list = new List<Gizmo>();
        var command_Action = new Command_Action
        {
            icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile"),
            defaultDesc = "MovingRail_Do_until_zone_is_full".Translate()
        };
        if (!currentZoneDisplayName.NullOrEmpty())
        {
            command_Action.defaultLabel = currentZoneDisplayName + " " + "MovingRail_selected".Translate();
        }
        else if (doForever)
        {
            command_Action.defaultLabel =
                "MovingRail_No_Zone".Translate() + " - " + "MovingRail_Do_Forever".Translate();
        }
        else
        {
            command_Action.defaultLabel = "MovingRail_No_Zone".Translate() + " - " + "MovingRail_ON_HOLD".Translate();
        }

        command_Action.activateSound = SoundDef.Named("Click");
        command_Action.action = SelectStockpilePressed;
        command_Action.groupKey = 887767541;
        list.Add(command_Action);
        var command_Action2 = new Command_Action
        {
            icon = ContentFinder<Texture2D>.Get("UI/Commands/SettingsMeter"),
            defaultDesc = "MovingRail_Configure_Pulling_Intervals".Translate(),
            defaultLabel = "MovingRail_Pulling_Settings".Translate(),
            activateSound = SoundDef.Named("Click"),
            action = PullIntervalSettingsPressed,
            groupKey = 887767542
        };
        list.Add(command_Action2);
        var command_Action3 = new Command_Action
        {
            icon = ContentFinder<Texture2D>.Get("UI/Commands/CopySettings"),
            defaultDesc = "CommandCopyZoneSettingsDesc".Translate(),
            defaultLabel = "CommandCopyZoneSettingsLabel".Translate(),
            activateSound = SoundDef.Named("Click"),
            action = CopySettingsPressed,
            groupKey = 887767543
        };
        list.Add(command_Action3);
        var command_Action4 = new Command_Action
        {
            icon = ContentFinder<Texture2D>.Get("UI/Commands/PasteSettings"),
            defaultDesc = "CommandPasteZoneSettingsDesc".Translate(),
            defaultLabel = "CommandPasteZoneSettingsLabel".Translate(),
            activateSound = SoundDef.Named("Click"),
            action = PasteSettingsPressed,
            groupKey = 887767544
        };
        list.Add(command_Action4);
        var gizmos = base.GetGizmos();
        gizmos = SaveStorageSettingsUtil.SaveStorageSettingsUtil.AddSaveLoadGizmos(gizmos,
            "IndustrialRollers_StorageSettings", storageSettings.filter);
        return gizmos == null ? list.AsEnumerable() : list.AsEnumerable().Concat(gizmos);
    }

    private void PasteSettingsPressed()
    {
        if (!StorageSettingsClipboard.HasCopiedSettings)
        {
            return;
        }

        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        StorageSettingsClipboard.PasteInto(storageSettings);
    }

    private void CopySettingsPressed()
    {
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        StorageSettingsClipboard.Copy(storageSettings);
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
            if (!item.label.Equals(string.Empty) && item is Zone_Stockpile)
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

    public void PullIntervalSettingsPressed()
    {
        Find.WindowStack.Add(new Dialog_PullerMaxPullConfig(EveryHours, MaxPullsPerTime, this));
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
        if (_ticksSinceLastHourCheck > _hourCheckTickRate)
        {
            var num = GenLocalDate.HourInteger(_railLogicHelper.map);
            if (IsPastTimeFrame(_currentHour, num))
            {
                PullsThisTimeFrame = 0;
            }

            UpdateCurrentHour(num);
        }

        if (_tickCounter >= _updateEveryXTicks)
        {
            DoTickerWork();
            _tickCounter = 0;
        }

        _ticksSinceLastHourCheck++;
        _tickCounter++;
        TickRare();
    }

    private bool IsPastTimeFrame(int prevHour, int curHour)
    {
        if (prevHour == curHour)
        {
            return false;
        }

        if (EveryHours == 1)
        {
            return true;
        }

        if (EveryHours == 24)
        {
            return LastOperationAtHour == curHour;
        }

        var num = LastOperationAtHour + EveryHours;
        if (num > 23)
        {
            num -= 24;
        }

        return num == curHour;
    }

    public override void PrintForPowerGrid(SectionLayer layer)
    {
    }

    private void UpdateCurrentHour(int hour)
    {
        _currentHour = hour;

        _ticksSinceLastHourCheck = 0;
    }

    private void DoTickerWork()
    {
        var stockpileAt = _railLogicHelper.GetStockpileAt(GetSourceCell());
        if (stockpileAt != null)
        {
            _power.powerOutputInt = (float)Math.Min(0.0 - (stockpileAt.cells.Count * 4.5), -60.0);
        }
        else
        {
            _power.powerOutputInt = -60f;
        }

        var building_Storage = _railLogicHelper.map.thingGrid.ThingAt<Building_Storage>(GetSourceCell());
        if (!GetPowerComp().PowerOn)
        {
            if (_railLogicHelper.GetCurrGraphicFrame() != 0)
            {
                _railLogicHelper.SetCurrGraphicFrame(0);
            }
        }
        else
        {
            if (_isWorking)
            {
                return;
            }

            if (currentZoneAccessName == string.Empty && !doForever)
            {
                _isWorking = false;
                if (_railLogicHelper.GetCurrGraphicFrame() != 0)
                {
                    _railLogicHelper.SetCurrGraphicFrame(0);
                }

                return;
            }

            if (_railLogicHelper.GetCurrGraphicFrame() != 1)
            {
                _railLogicHelper.SetCurrGraphicFrame(1);
            }

            if (!ShouldPullThisTimeFrame())
            {
                return;
            }

            _isWorking = true;
            Thing thing;
            var thing2 = _railLogicHelper.map.thingGrid.ThingAt(GetSourceCell(), ThingCategory.Item);
            var storageBuildingHelper =
                new StorageBuildingHelper(zoneType, currentZoneAccessName, _railLogicHelper.map);
            if (!storageBuildingHelper.IsValid && !doForever)
            {
                _isWorking = false;
                return;
            }

            if (thing2 != null && storageSettings.AllowedToAccept(thing2) && (doForever ||
                                                                              storageBuildingHelper
                                                                                  .GetStoreSettings() != null &&
                                                                              storageBuildingHelper.GetStoreSettings()
                                                                                  .AllowedToAccept(thing2) &&
                                                                              (_railLogicHelper
                                                                                   .GetItemFreeSpotInSlotGroup(
                                                                                       storageBuildingHelper
                                                                                           .GetSlotGroup()).HasValue ||
                                                                               _railLogicHelper
                                                                                   .GetSimilarItemsInSlotGroup(
                                                                                       storageBuildingHelper
                                                                                           .GetSlotGroup(), thing2,
                                                                                       false).Count > 0)))
            {
                thing = thing2;
            }
            else
            {
                if (stockpileAt == null && building_Storage == null)
                {
                    _isWorking = false;
                    return;
                }

                thing = GetNextItemFromThingsList(
                    stockpileAt != null ? stockpileAt.AllContainedThings : building_Storage.slotGroup.HeldThings,
                    storageBuildingHelper);
            }

            if (thing != null)
            {
                Util.Log("Thing: " + thing);
                Util.Log("Category: " + thing.def.category);
                Util.Log("In valid storage(thing.IsInValidStorage): " + thing.IsInValidStorage());
                var destinationCell = GetDestinationCell();
                var cellType = _railLogicHelper.NextCellContentType(destinationCell);
                if (storageSettings.AllowedToAccept(thing))
                {
                    _railLogicHelper.SetCurrGraphicFrame(2);
                    if (cellType == CellType.Rail)
                    {
                        LogPullActionThisTimeFrame();
                        thing.DeSpawn();
                        GenPlace.TryPlaceThing(thing, destinationCell, _railLogicHelper.map, ThingPlaceMode.Direct);
                    }
                    else if (_railLogicHelper.map.zoneManager.ZoneAt(destinationCell) != null &&
                             _railLogicHelper.map.zoneManager.ZoneAt(destinationCell) is Zone_Stockpile)
                    {
                        LogPullActionThisTimeFrame();
                        _railLogicHelper.TryPlaceItemInStockpile(ref thing,
                            _railLogicHelper.map.zoneManager.ZoneAt(destinationCell) as Zone_Stockpile);
                    }

                    if (cellType == CellType.Rail)
                    {
                        thing.SetForbidden(true, false);
                    }
                }
            }

            _isWorking = false;
        }
    }

    private Thing GetNextItemFromStockpileStorage(Zone_Stockpile sourceStockpile, StorageBuildingHelper destination)
    {
        if (IndustrialRollersMod.HardcoreMode)
        {
            return sourceStockpile.AllContainedThings.FirstOrDefault(x =>
                x.def.category.Equals(ThingCategory.Item) &&
                Vector3.Distance(x.Position.ToVector3(), Position.ToVector3()) <=
                IndustrialRollersMod.PullingDistance && storageSettings.AllowedToAccept(x) && (doForever ||
                    destination.GetStoreSettings() != null && destination.GetStoreSettings().AllowedToAccept(x) &&
                    (_railLogicHelper.GetItemFreeSpotInSlotGroup(destination.GetSlotGroup()).HasValue ||
                     _railLogicHelper.GetSimilarItemsInSlotGroup(destination.GetSlotGroup(), x, false).Count > 0)));
        }

        return sourceStockpile.AllContainedThings.FirstOrDefault(x =>
            x.def.category.Equals(ThingCategory.Item) && storageSettings.AllowedToAccept(x) && (doForever ||
                destination.GetStoreSettings() != null && destination.GetStoreSettings().AllowedToAccept(x) &&
                (_railLogicHelper.GetItemFreeSpotInSlotGroup(destination.GetSlotGroup()).HasValue || _railLogicHelper
                    .GetSimilarItemsInSlotGroup(destination.GetSlotGroup(), x, false).Count > 0)));
    }

    private Thing GetNextItemFromThingsList(IEnumerable<Thing> things, StorageBuildingHelper destination)
    {
        if (IndustrialRollersMod.HardcoreMode)
        {
            return things.FirstOrDefault(x =>
                x.def.category.Equals(ThingCategory.Item) &&
                Vector3.Distance(x.Position.ToVector3(), Position.ToVector3()) <=
                IndustrialRollersMod.PullingDistance && storageSettings.AllowedToAccept(x) && (doForever ||
                    destination.GetStoreSettings() != null && destination.GetStoreSettings().AllowedToAccept(x) &&
                    (_railLogicHelper.GetItemFreeSpotInSlotGroup(destination.GetSlotGroup()).HasValue ||
                     _railLogicHelper.GetSimilarItemsInSlotGroup(destination.GetSlotGroup(), x, false).Count > 0)));
        }

        return things.FirstOrDefault(x =>
            x.def.category.Equals(ThingCategory.Item) && storageSettings.AllowedToAccept(x) && (doForever ||
                destination.GetStoreSettings() != null && destination.GetStoreSettings().AllowedToAccept(x) &&
                (_railLogicHelper.GetItemFreeSpotInSlotGroup(destination.GetSlotGroup()).HasValue || _railLogicHelper
                    .GetSimilarItemsInSlotGroup(destination.GetSlotGroup(), x, false).Count > 0)));
    }

    private void LogPullActionThisTimeFrame()
    {
        if (MaxPullsPerTime <= 0)
        {
            return;
        }

        PullsThisTimeFrame++;
        LastOperationAtHour = _currentHour;
    }

    private bool ShouldPullThisTimeFrame()
    {
        if (EveryHours != 1)
        {
        }

        return MaxPullsPerTime == 0 || PullsThisTimeFrame < MaxPullsPerTime;
    }

    public override void DrawExtraSelectionOverlays()
    {
        if (IndustrialRollersMod.HardcoreMode)
        {
            GenDraw.DrawRadiusRing(Position, IndustrialRollersMod.PullingDistance);
        }

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
                var zone_Stockpile =
                    (Zone_Stockpile)_railLogicHelper.map.zoneManager.AllZones.FirstOrDefault(x =>
                        x.label == currentZoneAccessName);
                if (zone_Stockpile != null)
                {
                    result = zone_Stockpile.Cells.FirstOrDefault();
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

    public override void ExposeData()
    {
        base.ExposeData();
        if (currentZoneAccessName == null)
        {
            currentZoneAccessName = string.Empty;
        }

        if (currentZoneDisplayName == null)
        {
            currentZoneDisplayName = string.Empty;
        }

        Scribe_Deep.Look(ref storageSettings, "storageSettings", this);
        Scribe_Values.Look(ref doForever, "doForever");
        Scribe_Values.Look(ref showZone, "showZone");
        Scribe_Values.Look(ref MaxPullsPerTime, "MaxPullsPerTime");
        Scribe_Values.Look(ref PullsThisTimeFrame, "PullsThisTimeFrame");
        Scribe_Values.Look(ref EveryHours, "EveryHours", 1);
        Scribe_Values.Look(ref LastOperationAtHour, "LastOperationAtHour", -999);
        Scribe_Values.Look(ref currentZoneAccessName, "currentZoneAccessName", string.Empty);
        Scribe_Values.Look(ref currentZoneDisplayName, "currentZoneDisplayName", string.Empty);
        Scribe_Values.Look(ref zoneType, "zoneType", StorageBuildingHelper.ZoneType.None);
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
}