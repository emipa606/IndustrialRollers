using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldIndustrialRollers;
using UnityEngine;
using Verse;

namespace MovingFloor;

internal class RailLogicHelper
{
    private readonly Building _rail;

    private readonly int _tickTriesBeforeItemStuck = 6;

    public readonly Map map;
    private int _currGraphicFrame;

    private bool _didIssueErrorForZone;
    private bool _hasThing;

    private string _lastItemOnRail = string.Empty;

    private bool _movedSkipIteration;

    private string _previousThingId;

    public RailLogicHelper(Building rail, Map map)
    {
        _rail = rail;
        this.map = map;
        RemoveUnallowedComponents();
    }

    private void RemoveUnallowedComponents()
    {
        var enumerable = map.thingGrid.ThingsAt(_rail.Position);
        foreach (var item in enumerable)
        {
            if (item.def.category == ThingCategory.Building && (item.def.defName.Equals("PowerConduitInvisible") ||
                                                                item.def.defName.Equals("PowerConduit")))
            {
                item.DeSpawn();
            }
        }
    }

    private int GetTicksSinceItemStuck()
    {
        return ((IRail)_rail).GetTicksSinceItemStuck();
    }

    private void SetTicksSinceItemStuck(int ticks)
    {
        ((IRail)_rail).SetTicksSinceItemStuck(ticks);
    }

    private void AddStuckTick()
    {
        ((IRail)_rail).AddStuckTick();
    }

    public void DoTickerWork()
    {
        if (!((IRail)_rail).GetPowerComp().PowerOn)
        {
            return;
        }

        var thing = map.thingGrid.ThingAt(_rail.Position, ThingCategory.Item);
        BasicMoveItemLogic(thing);
    }

    private void BasicMoveItemLogic(Thing thing)
    {
        var noThingFound = _movedSkipIteration;
        if (!_hasThing)
        {
            noThingFound = true;
            _previousThingId = string.Empty;
        }

        _hasThing = thing != null;
        if (GetTicksSinceItemStuck() > 0 && _previousThingId.Equals(string.Empty) || _hasThing &&
            !_previousThingId.Equals(string.Empty) && !_previousThingId.Equals(thing?.ThingID))
        {
            SetTicksSinceItemStuck(0);
        }

        _currGraphicFrame = _hasThing ? 1 : 0;
        if (!_hasThing)
        {
            return;
        }

        _hasThing = true;
        _movedSkipIteration = false;
        _previousThingId = thing?.ThingID;
        if (noThingFound)
        {
            return;
        }

        var nextPositionForThing = GetNextPositionForThing(thing);
        Util.Log(thing?.ThingID + " next pos: " + nextPositionForThing);
        var cellType = NextCellContentType(nextPositionForThing);
        Util.Log("cellContentType" + cellType + " for item ");
        var zone = map.zoneManager.ZoneAt(nextPositionForThing);
        if (zone is Zone_Stockpile stockpile)
        {
            if (!stockpile.settings.AllowedToAccept(thing))
            {
                AddStuckTick();
                Util.Log("ticks since stuck " + GetTicksSinceItemStuck());
                if (GetTicksSinceItemStuck() < _tickTriesBeforeItemStuck)
                {
                    return;
                }

                thing.SetForbidden(false, false);
                SetTicksSinceItemStuck(0);
                Util.Log("Destination stockpile cant accept item");

                return;
            }

            if (!_lastItemOnRail.Equals(thing?.ThingID))
            {
                _didIssueErrorForZone = false;
            }

            _lastItemOnRail = thing?.ThingID;
            if (cellType != CellType.Rail)
            {
                if (thing != null && thing.def.stackLimit > 1)
                {
                    Util.Log("thing can stack got " + thing.stackCount + "/" + thing.def.stackLimit +
                             " (while carrying " + thing.ThingID + ") attempting to merge with existing items");
                    var similarItemsInStorage = GetSimilarItemsInStorage(stockpile, thing, false);
                    Util.Log("Found " + similarItemsInStorage.Count + " similar items in storage");
                    using (var enumerator = similarItemsInStorage.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                        {
                            var current = enumerator.Current;
                            current?.TryAbsorbStack(thing, true);
                            return;
                        }
                    }

                    Util.Log("After stacking, has " + thing.stackCount + "/" + thing.def.stackLimit + " left in stack");
                }

                var itemFreeSpotInZone = GetItemFreeSpotInZone(stockpile);
                if (itemFreeSpotInZone.HasValue && (itemFreeSpotInZone.Value.y != itemFreeSpotInZone.Value.x ||
                                                    itemFreeSpotInZone.Value.z != itemFreeSpotInZone.Value.y ||
                                                    itemFreeSpotInZone.Value.z != 0))
                {
                    thing?.DeSpawn();
                    Util.Log("call from stockpile detected got point: " + itemFreeSpotInZone);
                    MakeThingCopyAtPos(thing, itemFreeSpotInZone.Value);
                    thing.SetForbidden(false, false);
                    return;
                }

                Util.Log("Got to no free spots with " + _didIssueErrorForZone + " stuck tick counter: " +
                         GetTicksSinceItemStuck());
                Util.Log("got null from stokepile - no empty cells? (while carrying " + thing?.ThingID + ")");
                if (_didIssueErrorForZone)
                {
                    return;
                }

                thing.SetForbidden(false, false);
                _didIssueErrorForZone = true;
                if (GetTicksSinceItemStuck() < _tickTriesBeforeItemStuck)
                {
                }

                return;
            }
        }

        switch (cellType)
        {
            case CellType.Occupied:
                Util.Log("cell is blocked by a building");
                AddStuckTick();
                if (GetTicksSinceItemStuck() >= _tickTriesBeforeItemStuck)
                {
                    thing.SetForbidden(false, false);
                }

                break;
            case CellType.Item:
            {
                var thing2 = map.thingGrid.ThingAt(nextPositionForThing, ThingCategory.Item);
                Util.Log("cell is blocked by an item");
                Util.Log(thing2.ThingID);
                if (thing2.TryAbsorbStack(thing, true))
                {
                    break;
                }

                var thing3 = map.thingGrid.ThingAt(nextPositionForThing, ThingCategory.Building);
                if (thing3 == null || !IsBuildingBelt(thing3))
                {
                    AddStuckTick();
                    if (GetTicksSinceItemStuck() >= _tickTriesBeforeItemStuck && thing is { Destroyed: false })
                    {
                        thing.SetForbidden(false, false);
                    }
                }

                break;
            }
            case CellType.Rail:
            {
                var rail = (IRail)map.thingGrid.ThingsListAtFast(nextPositionForThing).FirstOrDefault(IsBuildingBelt);
                thing?.DeSpawn();
                if (rail != null)
                {
                    MakeThingCopyAtPos(thing, rail.GetProperPlacementForItem(thing, nextPositionForThing));
                    _movedSkipIteration = true;
                }

                break;
            }
            case CellType.Empty:
                thing.SetForbidden(false, false);
                break;
        }
    }

    public bool TryPlaceItemInStockpile(ref Thing thing, Zone_Stockpile zone)
    {
        if (!zone.settings.AllowedToAccept(thing))
        {
            AddStuckTick();
            Util.Log("ticks since stuck " + GetTicksSinceItemStuck());
            if (GetTicksSinceItemStuck() < _tickTriesBeforeItemStuck)
            {
                return false;
            }

            thing.SetForbidden(false, false);
            SetTicksSinceItemStuck(0);
            Util.Log("Destination stockpile cant accept item");
            return false;
        }

        if (!_lastItemOnRail.Equals(thing.ThingID))
        {
            _didIssueErrorForZone = false;
        }

        _lastItemOnRail = thing.ThingID;
        if (thing.def.stackLimit > 1)
        {
            Util.Log("thing can stack got " + thing.stackCount + "/" + thing.def.stackLimit + " (while carrying " +
                     thing.ThingID + ") attempting to merge with existing items");
            var similarItemsInStorage = GetSimilarItemsInStorage(zone, thing, false);
            Util.Log("Found " + similarItemsInStorage.Count + " similar items in storage");
            using var enumerator = similarItemsInStorage.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                current?.TryAbsorbStack(thing, true);
                return true;
            }
        }

        var itemFreeSpotInZone = GetItemFreeSpotInZone(zone);
        if (itemFreeSpotInZone.HasValue && (itemFreeSpotInZone.Value.y != itemFreeSpotInZone.Value.x ||
                                            itemFreeSpotInZone.Value.z != itemFreeSpotInZone.Value.y ||
                                            itemFreeSpotInZone.Value.z != 0))
        {
            thing.DeSpawn();
            Util.Log("call from stockpile detected got point: " + itemFreeSpotInZone);
            MakeThingCopyAtPos(thing, itemFreeSpotInZone.Value);
            thing.SetForbidden(false, false);
        }
        else
        {
            Util.Log("Got to no free spots with " + _didIssueErrorForZone + " stuck tick counter: " +
                     GetTicksSinceItemStuck());
            Util.Log("got null from stokepile - no empty cells? (while carrying " + thing.ThingID + ")");
            if (_didIssueErrorForZone)
            {
                return true;
            }

            thing.SetForbidden(false, false);
            _didIssueErrorForZone = true;
            return false;
        }

        return true;
    }

    public bool TryPlaceItemInStorageBuilding(ref Thing thing, Building_Storage building, bool force = false)
    {
        Util.Log("Trying to place item in storage building");
        if (!building.settings.AllowedToAccept(thing) && !force)
        {
            Util.Log("Not allowed to accept");
            AddStuckTick();
            Util.Log("ticks since stuck " + GetTicksSinceItemStuck());
            if (GetTicksSinceItemStuck() < _tickTriesBeforeItemStuck)
            {
                return false;
            }

            thing.SetForbidden(false, false);
            SetTicksSinceItemStuck(0);
            Util.Log("Destination storage cant accept item");
            return false;
        }

        if (!_lastItemOnRail.Equals(thing.ThingID))
        {
            _didIssueErrorForZone = false;
        }

        _lastItemOnRail = thing.ThingID;
        if (thing.def.stackLimit > 1)
        {
            Util.Log("thing can stack got " + thing.stackCount + "/" + thing.def.stackLimit + " (while carrying " +
                     thing.ThingID + ") attempting to merge with existing items");
            var similarItemsInSlotGroup = GetSimilarItemsInSlotGroup(building.slotGroup, thing, false);
            Util.Log("Found " + similarItemsInSlotGroup.Count + " similar items in storage");
            using var enumerator = similarItemsInSlotGroup.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                current?.TryAbsorbStack(thing, true);
                return true;
            }
        }

        var itemFreeSpotInSlotGroup = GetItemFreeSpotInSlotGroup(building.slotGroup);
        if (itemFreeSpotInSlotGroup.HasValue && (itemFreeSpotInSlotGroup.Value.y != itemFreeSpotInSlotGroup.Value.x ||
                                                 itemFreeSpotInSlotGroup.Value.z != itemFreeSpotInSlotGroup.Value.y ||
                                                 itemFreeSpotInSlotGroup.Value.z != 0))
        {
            thing.DeSpawn();
            Util.Log("cell from storage building detected got point: " + itemFreeSpotInSlotGroup);
            MakeThingCopyAtPos(thing, itemFreeSpotInSlotGroup.Value);
            thing.SetForbidden(false, false);
        }
        else
        {
            Util.Log("Got to no free spots with " + _didIssueErrorForZone + " stuck tick counter: " +
                     GetTicksSinceItemStuck());
            Util.Log("got null from stokepile - no empty cells? (while carrying " + thing.ThingID + ")");
            if (_didIssueErrorForZone)
            {
                return true;
            }

            thing.SetForbidden(false, false);
            _didIssueErrorForZone = true;
            return false;
        }

        return true;
    }

    private List<Thing> GetSimilarItemsInStorage(Zone zone, Thing item, bool allowFullyStacked)
    {
        var list = zone.AllContainedThings.Where(y => y.CanStackWith(item) && !y.Equals(item)).ToList();
        if (!allowFullyStacked)
        {
            list = list.Where(x => x.stackCount < x.def.stackLimit).ToList();
        }

        return list;
    }

    public List<Thing> GetSimilarItemsInSlotGroup(SlotGroup sg, Thing item, bool allowFullyStacked)
    {
        var list = sg.CellsList.Where(y =>
            map.thingGrid.ThingAt(y, ThingCategory.Item) != null &&
            map.thingGrid.ThingAt(y, ThingCategory.Item).CanStackWith(item) &&
            !map.thingGrid.ThingAt(y, ThingCategory.Item).Equals(item)).ToList();
        var list2 = new List<Thing>();
        foreach (var item2 in list)
        {
            var thing = map.thingGrid.ThingAt(item2, ThingCategory.Item);
            if (thing != null)
            {
                list2.Add(thing);
            }
        }

        if (!allowFullyStacked)
        {
            list2 = list2.Where(x => x.stackCount < x.def.stackLimit).ToList();
        }

        return list2;
    }

    public IntVec3? GetItemFreeSpotInSlotGroup(SlotGroup sg)
    {
        Util.Log("GetItemFreeSpotInSlotGroup");
        foreach (var cells in sg.CellsList)
        {
            Util.Log("Cell: " + cells);
            if (map.thingGrid.ThingAt(cells, ThingCategory.Item) == null)
            {
                Util.Log("Cell IS EMPTY");
                return cells;
            }

            Util.Log("Cell IS NOT EMPTY");
        }

        return null;
    }

    private IntVec3? GetItemFreeSpotInZone(Zone zone)
    {
        return zone.Cells.Find(x => !zone.AllContainedThings.Any(y =>
            (y.def.category.Equals(ThingCategory.Item) ||
             y.def.category.Equals(ThingCategory.Building) && IsStockpileBlockingBuilding(y.def)) &&
            y.Position.Equals(x)));
    }

    public void MakeThingCopyAtPos(Thing thing, IntVec3 pos)
    {
        if (pos.x == pos.y && pos.y == pos.z && pos.y == 0)
        {
            return;
        }

        Util.Log("Placing thing: " + thing.ThingID + " at pos: X:" + pos.x + " Y:" + pos.y + " Z:" + pos.z);
        GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Direct);
        thing.SetForbidden(true, false);
    }

    public bool IsStockpileBlockingBuilding(ThingDef buildingDef)
    {
        if (buildingDef.passability == Traversability.Standable ||
            buildingDef.passability.Equals(Traversability.Standable))
        {
            return false;
        }

        if (buildingDef.building is { isSittable: true })
        {
            return false;
        }

        return true;
    }

    public CellType NextCellContentType(IntVec3 pos)
    {
        var isEmpty = true;
        var thing = map.thingGrid.ThingAt(pos, ThingCategory.Item);
        if (thing is { Spawned: true, Destroyed: false })
        {
            return CellType.Item;
        }

        var list = (from x in map.thingGrid.ThingsListAtFast(pos)
            where x.def.category == ThingCategory.Building
            select x).ToList();
        if (list.Count == 0)
        {
            return CellType.Empty;
        }

        if (list.Any(x => IsStockpileBlockingBuilding(x.def)))
        {
            isEmpty = false;
        }

        if (list.Any(IsBuildingBelt))
        {
            return CellType.Rail;
        }

        if (isEmpty)
        {
            return CellType.Empty;
        }

        return CellType.Occupied;
    }

    public bool IsBuildingBelt(Thing cellBuilding)
    {
        if (cellBuilding.def.defName.Contains("MovingRail") && !cellBuilding.def.defName.Contains("Puller") &&
            !cellBuilding.def.defName.Contains("Pusher") && ((Building)cellBuilding).TransmitsPowerNow)
        {
            return true;
        }

        return false;
    }

    public Zone_Stockpile GetStockpileAt(IntVec3 rotationFacingCell)
    {
        var zone = map.zoneManager.ZoneAt(rotationFacingCell);
        return zone as Zone_Stockpile;
    }

    public Zone_Growing GetGrowpileAt(IntVec3 rotationFacingCell)
    {
        var zone = map.zoneManager.ZoneAt(rotationFacingCell);
        return zone as Zone_Growing;
    }

    private IntVec3 GetNextPositionForThing(Thing thing)
    {
        return ((IRail)_rail).GetNextPositionForThing(thing);
    }

    internal void UpdateGraphics()
    {
        var graphic = ((IRail)_rail).GetGraphic();
        if (graphic != null && graphic.Length != 0 && graphic[0] != null)
        {
            return;
        }

        var array = new Graphic[2];
        graphic = array;
        var startIndex = _rail.def.graphicData.texPath.ToLower()
            .LastIndexOf(((IRail)_rail).GetGraphicPath(), StringComparison.Ordinal);
        var text = _rail.def.graphicData.texPath.Remove(startIndex);
        for (var i = 0; i < 2; i++)
        {
            var path = text + ((IRail)_rail).GetGraphicPath() + i;
            graphic[i] = GraphicDatabase.Get<Graphic_Single>(path, _rail.def.graphic.Shader,
                _rail.def.graphic.drawSize, _rail.def.graphic.Color, _rail.def.graphic.ColorTwo);
        }

        ((IRail)_rail).SetGraphic(graphic);
    }

    public void SetCurrGraphicFrame(int frame)
    {
        _currGraphicFrame = frame;
    }

    public Graphic Graphic()
    {
        var graphic = ((IRail)_rail).GetGraphic();
        if (graphic?[0] != null)
        {
            return graphic[_currGraphicFrame];
        }

        ((IRail)_rail).UpdateGraphics();
        return graphic?[0] == null ? null : graphic[_currGraphicFrame];
    }

    public static void DrawArrowPointingAt(Vector3 from, Vector3 to, bool offscreenOnly = false)
    {
        var forward = (to - from).normalized * 1f;
        var position = from;
        position.x += 0.5f;
        position.z += 0.5f;
        position.y = AltitudeLayer.VisEffects.AltitudeFor();
        var rotation = Quaternion.LookRotation(forward);
        Graphics.DrawMesh(MeshPool.plane10, position, rotation, MovingRailBase.ArrowMatWhite, 0);
    }

    public void DrawArrow()
    {
        var to = ((IRail)_rail).GetNextPositionForDirection().ToVector3();
        var from = _rail.Position.ToVector3();
        DrawArrowPointingAt(from, to);
    }

    public int GetCurrGraphicFrame()
    {
        return _currGraphicFrame;
    }
}