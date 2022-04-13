using System;

namespace MovingFloor;

internal class Action_StorageBuilding
{
    private readonly MovingRailPuller _instance;
    public readonly Action action;

    public readonly string id;

    public readonly string label;

    public Action_StorageBuilding(string buildingThingId, string buildingLabel, MovingRailPuller delegatPuller)
    {
        id = buildingThingId;
        label = buildingLabel;
        _instance = delegatPuller;
        action = LabelSelected;
    }

    public Action GetAction()
    {
        return action;
    }

    private void LabelSelected()
    {
        _instance.NewZoneSelected(id, label, GetType());
    }
}