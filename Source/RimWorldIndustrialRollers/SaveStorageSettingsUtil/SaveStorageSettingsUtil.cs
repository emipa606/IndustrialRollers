using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SaveStorageSettingsUtil;

public static class SaveStorageSettingsUtil
{
    private static Assembly saveStateAssembly;

    private static bool initialized;

    public static bool Exists
    {
        get
        {
            if (initialized)
            {
                return saveStateAssembly != null;
            }

            foreach (var runningMod in LoadedModManager.RunningMods)
            {
                foreach (var loadedAssembly in runningMod.assemblies.loadedAssemblies)
                {
                    if (!loadedAssembly.GetName().Name.Equals("SaveStorageSettings") ||
                        loadedAssembly.GetType("SaveStorageSettings.GizmoUtil") == null)
                    {
                        continue;
                    }

                    initialized = true;
                    saveStateAssembly = loadedAssembly;
                    break;
                }

                if (initialized)
                {
                    break;
                }
            }

            initialized = true;

            return saveStateAssembly != null;
        }
    }

    public static IEnumerable<Gizmo> AddSaveLoadGizmos(IEnumerable<Gizmo> gizmos, SaveTypeEnum saveTypeEnum,
        ThingFilter thingFilter, int groupKey = 987767552)
    {
        return AddSaveLoadGizmos(gizmos, saveTypeEnum.ToString(), thingFilter);
    }

    public static List<Gizmo> AddSaveLoadGizmos(List<Gizmo> gizmos, SaveTypeEnum saveTypeEnum, ThingFilter thingFilter,
        int groupKey = 987767552)
    {
        return AddSaveLoadGizmos(gizmos, saveTypeEnum.ToString(), thingFilter);
    }

    public static IEnumerable<Gizmo> AddSaveLoadGizmos(IEnumerable<Gizmo> gizmos, string storageTypeName,
        ThingFilter thingFilter, int groupKey = 987767552)
    {
        var gizmos2 = gizmos == null ? new List<Gizmo>(2) : new List<Gizmo>(gizmos);
        return AddSaveLoadGizmos(gizmos2, storageTypeName, thingFilter);
    }

    public static List<Gizmo> AddSaveLoadGizmos(List<Gizmo> gizmos, string storageTypeName, ThingFilter thingFilter,
        int groupKey = 987767552)
    {
        try
        {
            if (Exists)
            {
                saveStateAssembly.GetType("SaveStorageSettings.GizmoUtil")
                    .GetMethod("AddSaveLoadGizmos", BindingFlags.Static | BindingFlags.Public)
                    ?.Invoke(null,
                        new object[] { gizmos, storageTypeName, thingFilter, groupKey });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex.GetType().Name + " " + ex.Message + "\n" + ex.StackTrace);
        }

        return gizmos;
    }
}