using Verse;

namespace RimWorldIndustrialRollers;

internal class Settings : ModSettings
{
    public static string ButtonLocationString = "0";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref IndustrialRollersMod.HardcoreMode, "MovingRail.HardcoreMode", false);
        Scribe_Values.Look(ref IndustrialRollersMod.PullingDistance, "MovingRail.PullingDistance", 15f);
        Scribe_Values.Look(ref IndustrialRollersMod.UndergroundRollerDistance,
            "MovingRail.UndergroundRollerMaxDistance", MovingRailUndergroundOutput.MAX_BUILD_DISTANCE);
    }
}