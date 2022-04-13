using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorldIndustrialRollers;

internal class PlaceWorker_NextToUndergroundRoller : PlaceWorker
{
    private List<IntVec3> GetBackTiles(IntVec3 loc, Rot4 rot)
    {
        var list = new List<IntVec3>();
        var num = 0;
        switch ((int)Math.Floor(rot.AsAngle))
        {
            case 0:
                num = 2;
                break;
            case 90:
                num = 3;
                break;
            case 180:
                num = 0;
                break;
            case 270:
                num = 1;
                break;
        }

        var intVec = loc + GenAdj.CardinalDirections[num];
        for (var i = 0; i <= IndustrialRollersMod.UndergroundRollerDistance; i++)
        {
            list.Add(new IntVec3(intVec.ToVector3()));
            intVec += GenAdj.CardinalDirections[num];
        }

        return list;
    }

    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        Thing thingToIgnore = null, Thing thing = null)
    {
        var backTiles = GetBackTiles(loc, rot);
        foreach (var item in backTiles)
        {
            if (!item.InBounds(map))
            {
                continue;
            }

            foreach (var item2 in map.thingGrid.ThingsAt(item))
            {
                if (item2.def.category != ThingCategory.Building || !(item2.Rotation == rot) ||
                    !(item2 is MovingRailUndergroundInput input) ||
                    input.GetOutputRoller() != null)
                {
                    continue;
                }

                return true;
            }
        }

        return "MovingRail_MustPlaceNextToUndergroundInput".Translate();
    }

    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        var backTiles = GetBackTiles(center, rot);
        GenDraw.DrawFieldEdges(backTiles, Color.white);
        base.DrawGhost(def, center, rot, ghostCol, thing);
    }
}