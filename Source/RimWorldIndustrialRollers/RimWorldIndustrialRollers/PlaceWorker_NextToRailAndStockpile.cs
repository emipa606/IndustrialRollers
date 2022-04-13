using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorldIndustrialRollers;

internal class PlaceWorker_NextToRailAndStockpile : PlaceWorker
{
    private List<IntVec3> GetFrontAndBack(IntVec3 loc, Rot4 rot)
    {
        var list = new List<IntVec3>();
        if ((int)Math.Floor(rot.AsAngle) == 0 || (int)Math.Floor(rot.AsAngle) == 180)
        {
            list.Add(loc + GenAdj.CardinalDirections[0]);
            list.Add(loc + GenAdj.CardinalDirections[2]);
        }
        else
        {
            list.Add(loc + GenAdj.CardinalDirections[1]);
            list.Add(loc + GenAdj.CardinalDirections[3]);
        }

        return list;
    }

    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        Thing thingToIgnore = null, Thing thing = null)
    {
        var num = 0;
        var northEast = true;
        var frontAndBack = GetFrontAndBack(loc, rot);
        foreach (var item in frontAndBack)
        {
            if (item.InBounds(map))
            {
                if (northEast && item.GetThingList(map).Any(x =>
                        x.def is { defName: { } } && x.def.defName.Contains("MovingRail") &&
                        !x.def.defName.Equals("MovingRailPusher") && !x.def.defName.Equals("MovingRailPuller")) ||
                    map.zoneManager.ZoneAt(item) != null && map.zoneManager.ZoneAt(item) is Zone_Stockpile ||
                    map.thingGrid.ThingAt(item, ThingCategory.Building) != null &&
                    map.thingGrid.ThingsAt(item).Any(x => x is Building_Storage))
                {
                    num++;
                }

                if (!northEast && (item.GetThingList(map).Any(x =>
                                       x.def is { defName: { } } && x.def.defName.Contains("MovingRail") &&
                                       !x.def.defName.Equals("MovingRailPusher") &&
                                       !x.def.defName.Equals("MovingRailPuller")) ||
                                   map.zoneManager.ZoneAt(item) != null &&
                                   map.zoneManager.ZoneAt(item) is Zone_Stockpile))
                {
                    num++;
                }
            }

            northEast = !northEast;
        }

        return num >= 2 ? true : "MovingRail_MustPlaceBetweenRailAndStockpile".Translate();
    }

    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        var frontAndBack = GetFrontAndBack(center, rot);
        if ((int)rot.AsAngle != 0 && (int)rot.AsAngle != 90)
        {
            (frontAndBack[1], frontAndBack[0]) = (frontAndBack[0], frontAndBack[1]);
        }

        GenDraw.DrawFieldEdges(new List<IntVec3> { frontAndBack[0] }, Color.white);
        GenDraw.DrawFieldEdges(new List<IntVec3> { frontAndBack[1] }, Color.cyan);
        base.DrawGhost(def, center, rot, ghostCol, thing);
    }
}