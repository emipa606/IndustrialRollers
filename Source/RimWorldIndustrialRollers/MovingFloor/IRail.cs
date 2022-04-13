using RimWorld;
using Verse;

namespace MovingFloor;

internal interface IRail
{
    IntVec3 GetNextPositionForThing(Thing thing);

    IntVec3 GetNextPositionForDirection();

    Graphic[] GetGraphic();

    void SetGraphic(Graphic[] graphic);

    string GetGraphicPath();

    CompPowerTrader GetPowerComp();

    void SetTicksSinceItemStuck(int ticks);

    int GetTicksSinceItemStuck();

    void AddStuckTick();

    void UpdateGraphics();

    IntVec3 GetProperPlacementForItem(Thing item, IntVec3 wantedPlace);

    IntVec3 GetInteractionCell();

    IntVec3[] GetInteractionCells();

    bool HasMultipleInteractionCells();

    bool IsCellBasedSkip();

    bool ShouldSkipForItemInPos(IntVec3 pos);

    void SetShouldSkipForItemInPos(IntVec3 pos, bool shouldSkip);
}