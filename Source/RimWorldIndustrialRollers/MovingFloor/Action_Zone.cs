using System;

namespace MovingFloor;

internal class Action_Zone
{
    private readonly MovingRailPuller _instance;
    public readonly Action action;

    public readonly string label;

    public Action_Zone(string lbl, MovingRailPuller delegatPuller)
    {
        label = lbl;
        _instance = delegatPuller;
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