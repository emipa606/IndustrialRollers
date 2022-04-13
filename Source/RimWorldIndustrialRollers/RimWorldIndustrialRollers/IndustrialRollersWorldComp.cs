using RimWorld.Planet;

namespace RimWorldIndustrialRollers;

internal class IndustrialRollersWorldComp : WorldComponent
{
    private static bool oldIsHardcore = IndustrialRollersMod.HardcoreMode;

    private static IndustrialRollersWorldComp Instance;

    public IndustrialRollersWorldComp(World world)
        : base(world)
    {
        Instance = this;
    }

    public static bool IsHardcore { get; private set; } = IndustrialRollersMod.HardcoreMode;

    public static float PullingDistance { get; private set; } = IndustrialRollersMod.PullingDistance;

    public static float MaxUndergroundRollerDistance { get; private set; } =
        MovingRailUndergroundOutput.MAX_BUILD_DISTANCE;

    public static bool IsNewGame { get; set; }

    internal static void UpdateHardcoreMode(bool hardcoreMode)
    {
        if (Instance == null)
        {
            return;
        }

        oldIsHardcore = IsHardcore;
        IsHardcore = hardcoreMode;
    }

    internal static void UpdatePullingDistance(float newPullingDistance)
    {
        if (Instance != null)
        {
            PullingDistance = newPullingDistance;
        }
    }

    internal static void UpdateMaxUndergroundRollerDistance(int newUndergroundRollerDistance)
    {
        if (Instance != null)
        {
            MaxUndergroundRollerDistance = newUndergroundRollerDistance;
        }
    }
}