using RimWorld;
using RimWorldIndustrialRollers;
using UnityEngine;
using Verse;

namespace MovingFloor;

[StaticConstructorOnStartup]
public class MovingRailBase : Building, IRail
{
    public static readonly Material ArrowMatWhite =
        MaterialPool.MatFrom("UI/Overlays/Arrow", ShaderDatabase.CutoutFlying, Color.white);

    public static Graphic[] graphic;

    private readonly string _graphicPathAdditionWoNumber = "_frame";

    private readonly int _updateEveryXTicks = 35;

    private CompPowerTrader _power;
    private RailLogicHelper _railLogicHelper;

    private int _stuckTicks;

    private int _tickCounter = 1;

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

    public override void SpawnSetup(Map map, bool respawnAfterLoad)
    {
        Util.Log("Making Logic Helper");
        _railLogicHelper = new RailLogicHelper(this, map);
        base.SpawnSetup(map, respawnAfterLoad);
        _power = GetComp<CompPowerTrader>();
    }

    public override void PrintForPowerGrid(SectionLayer layer)
    {
    }

    public StorageSettings GetStorageSettings()
    {
        return new StorageSettings();
    }

    public void SetStorageSettings(StorageSettings settings)
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
        _railLogicHelper.DoTickerWork();
    }
}