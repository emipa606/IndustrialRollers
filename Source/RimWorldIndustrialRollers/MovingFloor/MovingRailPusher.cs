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
internal class MovingRailPusher : Building, IStoreSettingsParent, IRail
{
    public static Graphic[] graphic;

    private readonly string _graphicPathAdditionWoNumber = "_frame";

    private readonly int _updateEveryXTicks = 10;
    private CompPowerTrader _power;

    private RailLogicHelper _railLogicHelper;

    private int _stuckTicks;

    private int _tickCounter;

    public StorageSettings storageSettings = new StorageSettings();

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

    public override IEnumerable<Gizmo> GetGizmos()
    {
        IList<Gizmo> list = new List<Gizmo>();
        var command_Action = new Command_Action
        {
            icon = ContentFinder<Texture2D>.Get("UI/Commands/CopySettings"),
            defaultDesc = "CommandCopyZoneSettingsDesc".Translate(),
            defaultLabel = "CommandCopyZoneSettingsLabel".Translate(),
            activateSound = SoundDef.Named("Click"),
            action = CopySettingsPressed,
            groupKey = 887767552
        };
        list.Add(command_Action);
        var command_Action2 = new Command_Action
        {
            icon = ContentFinder<Texture2D>.Get("UI/Commands/PasteSettings"),
            defaultDesc = "CommandPasteZoneSettingsDesc".Translate(),
            defaultLabel = "CommandPasteZoneSettingsLabel".Translate(),
            activateSound = SoundDef.Named("Click"),
            action = PasteSettingsPressed,
            groupKey = 887767553
        };
        list.Add(command_Action2);
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

    public override void PrintForPowerGrid(SectionLayer layer)
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
            _railLogicHelper.SetCurrGraphicFrame(0);
            return;
        }

        var sourceCell = GetSourceCell();
        var thing = _railLogicHelper.map.thingGrid.ThingAt(Position, ThingCategory.Item);
        var thing2 = thing ?? _railLogicHelper.map.thingGrid.ThingAt(sourceCell, ThingCategory.Item);
        if (_railLogicHelper.GetCurrGraphicFrame() != 1)
        {
            _railLogicHelper.SetCurrGraphicFrame(1);
        }

        _railLogicHelper.map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
        if (thing2 != null)
        {
            var destinationCell = GetDestinationCell();
            var cellType = _railLogicHelper.NextCellContentType(destinationCell);
            if (storageSettings.AllowedToAccept(thing2))
            {
                _railLogicHelper.SetCurrGraphicFrame(2);
                var stockpileAt = _railLogicHelper.GetStockpileAt(destinationCell);
                var thing3 = _railLogicHelper.map.thingGrid.ThingsAt(destinationCell)
                    .FirstOrDefault(x => x is Building_Storage);
                var thing4 = _railLogicHelper.map.thingGrid.ThingsAt(destinationCell)
                    .FirstOrDefault(x => x.TryGetComp<CompRefuelable>() != null);
                if (stockpileAt?.GetStoreSettings().AllowedToAccept(thing2) ?? false)
                {
                    _railLogicHelper.TryPlaceItemInStockpile(ref thing2, stockpileAt);
                }
                else if (thing3 != null)
                {
                    Util.Log("trying to place item in storage building " + thing2.ThingID);
                    _railLogicHelper.TryPlaceItemInStorageBuilding(ref thing2, (Building_Storage)thing3);
                }
                else if (thing4 != null)
                {
                    Util.Log("trying to refuel " + thing2.ThingID);
                    var compRefuelable = thing4.TryGetComp<CompRefuelable>();
                    if (compRefuelable.IsFull || !compRefuelable.Props.fuelFilter.AllowedThingDefs.Contains(thing2.def))
                    {
                        return;
                    }

                    var num = compRefuelable.TargetFuelLevel - compRefuelable.Fuel;
                    if (num is > 0f and < 1f)
                    {
                        num = 1f;
                    }

                    if (num >= thing2.stackCount)
                    {
                        compRefuelable.Refuel(new List<Thing> { thing2 });
                    }
                    else
                    {
                        var item = thing2.SplitOff((int)Math.Ceiling(num));
                        compRefuelable.Refuel(new List<Thing> { item });
                    }
                }
                else if (cellType == CellType.Empty || cellType == CellType.Rail)
                {
                    thing2.DeSpawn();
                    GenPlace.TryPlaceThing(thing2, destinationCell, _railLogicHelper.map, ThingPlaceMode.Direct);
                }
            }
        }

        _railLogicHelper.map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
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
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref storageSettings, "storageSettings", this);
    }

    private IntVec3 GetSourceCell()
    {
        var result = new IntVec3(Position.ToVector3());
        result.z -= Rotation.FacingCell.z;
        result.y -= Rotation.FacingCell.y;
        result.x -= Rotation.FacingCell.x;
        return result;
    }

    private IntVec3 GetDestinationCell()
    {
        var result = new IntVec3(Position.ToVector3());
        result.z += Rotation.FacingCell.z;
        result.y += Rotation.FacingCell.y;
        result.x += Rotation.FacingCell.x;
        return result;
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