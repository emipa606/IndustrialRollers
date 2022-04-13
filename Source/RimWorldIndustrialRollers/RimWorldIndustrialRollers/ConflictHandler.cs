using System.Collections.Generic;
using Verse;

namespace RimWorldIndustrialRollers;

internal class ConflictHandler
{
    private static ConflictHandler _instance;

    private readonly List<string> _mods = new List<string>();

    private ConflictHandler()
    {
        PopulateModList();
    }

    private void PopulateModList()
    {
        foreach (var allInstalledMod in ModLister.AllInstalledMods)
        {
            if (allInstalledMod.Active)
            {
                Util.Log(allInstalledMod.Name);
            }
        }
    }

    public List<string> GetModList()
    {
        return _mods;
    }

    public static ConflictHandler GetInstance()
    {
        if (_instance == null)
        {
            _instance = new ConflictHandler();
        }

        return _instance;
    }

    public static bool HasRimFridge()
    {
        return GetInstance().GetModList().Any(x => x.Contains(ModNames.RimFridge));
    }
}