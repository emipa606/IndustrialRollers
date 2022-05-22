using System;
using System.Collections.Generic;
using MovingFloor;
using RimWorld;
using UnityEngine;
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

    private Graphic currentGraphic;

    public MovingRailUndergroundInput InputRoller;

    private int outputDirection;

    public override Graphic Graphic
    {
        get
        {
            if (currentGraphic == null)
            {
                updateCurrentGraphics();
            }

            return currentGraphic;
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
        switch (outputDirection)
        {
            case 0: // Straight
                result.z += Rotation.FacingCell.z;
                result.y += Rotation.FacingCell.y;
                result.x += Rotation.FacingCell.x;
                break;
            case 1: // Left
                switch ((int)Math.Floor(Rotation.AsAngle))
                {
                    case 0:
                        result.x--;
                        break;
                    case 90:
                        result.z++;
                        break;
                    case 180:
                        result.x++;
                        break;
                    case 270:
                        result.z--;
                        break;
                }

                break;
            case 2: // Right
                switch ((int)Math.Floor(Rotation.AsAngle))
                {
                    case 0:
                        result.x++;
                        break;
                    case 90:
                        result.z--;
                        break;
                    case 180:
                        result.x--;
                        break;
                    case 270:
                        result.z++;
                        break;
                }

                break;
        }

        return result;
    }

    public IntVec3 GetNextPositionForDirection()
    {
        var result = new IntVec3(Position.ToVector3());
        switch (outputDirection)
        {
            case 0: // Straight
                result.z += Rotation.FacingCell.z;
                result.y += Rotation.FacingCell.y;
                result.x += Rotation.FacingCell.x;
                break;
            case 1: // Left
                switch ((int)Math.Floor(Rotation.AsAngle))
                {
                    case 0:
                        result.x--;
                        break;
                    case 90:
                        result.z++;
                        break;
                    case 180:
                        result.x++;
                        break;
                    case 270:
                        result.z--;
                        break;
                }

                break;
            case 2: // Right
                switch ((int)Math.Floor(Rotation.AsAngle))
                {
                    case 0:
                        result.x++;
                        break;
                    case 90:
                        result.z--;
                        break;
                    case 180:
                        result.x--;
                        break;
                    case 270:
                        result.z++;
                        break;
                }

                break;
        }

        return result;
    }

    public Graphic[] GetGraphic()
    {
        return graphic;
    }

    public void SetGraphic(Graphic[] graphics)
    {
        switch (outputDirection)
        {
            case 1: // Left
                for (var index = 0; index < graphics.Length; index++)
                {
                    graphics[index] = GraphicDatabase.Get<Graphic_Single>(graphics[index].path.Replace("_out", "_left"),
                        graphics[index].Shader,
                        graphics[index].drawSize, graphics[index].Color, graphics[index].ColorTwo);
                }

                break;
            case 2: // Right
                for (var index = 0; index < graphics.Length; index++)
                {
                    graphics[index] = GraphicDatabase.Get<Graphic_Single>(
                        graphics[index].path.Replace("_out", "_right"), graphics[index].Shader,
                        graphics[index].drawSize, graphics[index].Color, graphics[index].ColorTwo);
                }

                break;
        }

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

    private void updateCurrentGraphics()
    {
        var path = def.graphicData.texPath;
        switch (outputDirection)
        {
            case 1: // Left
                path = path.Replace("_out", "_left");
                break;
            case 2: // Right
                path = path.Replace("_out", "_right");
                break;
        }

        currentGraphic = GraphicDatabase.Get<Graphic_Single>(path, def.graphic.Shader,
            def.graphic.drawSize, def.graphic.Color, def.graphic.ColorTwo);
        Map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things);
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

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        var command_Action = new Command_Action();

        switch (outputDirection)
        {
            case 0: // Straight
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/ArrowStraight");
                command_Action.defaultDesc = "MovingRail_ExitStraight".Translate();
                break;
            case 1: // Left
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/ArrowLeft");
                command_Action.defaultDesc = "MovingRail_ExitLeft".Translate();
                break;
            case 2: // Right
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/ArrowRight");
                command_Action.defaultDesc = "MovingRail_ExitRight".Translate();
                break;
        }

        command_Action.action = delegate
        {
            outputDirection++;
            if (outputDirection > 2)
            {
                outputDirection = 0;
            }

            UpdateGraphics();
            updateCurrentGraphics();
        };

        yield return command_Action;
    }

    public override void ExposeData()
    {
        //Scribe_References.Look(ref InputRoller, "InputRoller");
        Scribe_Values.Look(ref outputDirection, "outputDirection", 0, true);
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