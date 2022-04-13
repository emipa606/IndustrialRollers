using System;

namespace RimWorldIndustrialRollers;

internal class Action_Zone
{
    private readonly MovingRailGreenPuller _instance;
    public readonly Action action;

    public readonly string label;

    public Action_Zone(string lbl, MovingRailGreenPuller delegatePuller)
    {
        label = lbl;
        _instance = delegatePuller;
        action = LabelSelected;
    }

    public Action GetAction()
    {
        return action;
    }

    private void LabelSelected()
    {
        _instance.NewZoneSelected(label, label, GetType());
    }
}