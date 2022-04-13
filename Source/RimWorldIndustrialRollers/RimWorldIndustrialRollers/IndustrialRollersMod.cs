using UnityEngine;
using Verse;

namespace RimWorldIndustrialRollers;

internal class IndustrialRollersMod : Mod
{
    public static bool HardcoreMode;

    public static float PullingDistance = 15f;

    public static int UndergroundRollerDistance = MovingRailUndergroundOutput.MAX_BUILD_DISTANCE;

    public IndustrialRollersMod(ModContentPack content)
        : base(content)
    {
        GetSettings<Settings>();
    }

    public override string SettingsCategory()
    {
        return "MovingRail_ModName".Translate();
    }

    public override void DoSettingsWindowContents(Rect rect)
    {
        var rect2 = new Rect(320f, 0f, 280f, 20f);
        Widgets.CheckboxLabeled(rect2, "MovingRail_HardCoreMode".Translate(), ref HardcoreMode);
        if (HardcoreMode)
        {
            rect2 = new Rect(320f, 35f, 280f, 35f);
            PullingDistance = Mathf.Floor(Widgets.HorizontalSlider(rect2, PullingDistance, 1f, 100f, false,
                "Custom pulling distance: " + PullingDistance));
            IndustrialRollersWorldComp.UpdatePullingDistance(PullingDistance);
        }

        rect2 = new Rect(320f, 70f, 280f, 35f);
        UndergroundRollerDistance = (int)Mathf.Floor(Widgets.HorizontalSlider(rect2, UndergroundRollerDistance, 1f,
            100f, false, "Custom underground roller max distance: " + UndergroundRollerDistance));
        IndustrialRollersWorldComp.UpdateMaxUndergroundRollerDistance(UndergroundRollerDistance);
        IndustrialRollersWorldComp.UpdateHardcoreMode(HardcoreMode);
    }
}