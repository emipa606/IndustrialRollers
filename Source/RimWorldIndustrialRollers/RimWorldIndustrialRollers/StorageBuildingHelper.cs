using System.Linq;
using RimWorld;
using Verse;

namespace RimWorldIndustrialRollers;

internal class StorageBuildingHelper : I_IR_StorageInterface
{
    public enum ZoneType
    {
        Stockpile,
        BuildingStorage,
        None
    }

    private readonly Building_Storage _sbZone;

    private readonly Zone_Stockpile _spZone;

    private readonly string _zoneId;

    private readonly ZoneType _zoneType;

    public StorageBuildingHelper(ZoneType zoneType, string currentZoneAccessName, Map map)
    {
        _zoneType = zoneType;
        _zoneId = currentZoneAccessName;
        if (zoneType == ZoneType.Stockpile)
        {
            _spZone = (Zone_Stockpile)map.zoneManager.AllZones.FirstOrDefault(x => x.label == _zoneId);
        }

        if (zoneType == ZoneType.BuildingStorage)
        {
            _sbZone = map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>()
                .FirstOrDefault(x => x.ThingID.Equals(_zoneId));
        }
    }

    public bool IsValid
    {
        get
        {
            if (_zoneType == ZoneType.Stockpile)
            {
                return _spZone != null;
            }

            if (_zoneType == ZoneType.BuildingStorage)
            {
                return _sbZone != null;
            }

            return false;
        }
    }

    public StorageSettings GetStoreSettings()
    {
        if (_zoneType == ZoneType.Stockpile)
        {
            return _spZone.settings;
        }

        if (_zoneType == ZoneType.BuildingStorage)
        {
            return _sbZone.settings;
        }

        return null;
    }

    public SlotGroup GetSlotGroup()
    {
        if (_zoneType == ZoneType.Stockpile)
        {
            return _spZone.slotGroup;
        }

        if (_zoneType == ZoneType.BuildingStorage)
        {
            return _sbZone.slotGroup;
        }

        return null;
    }
}