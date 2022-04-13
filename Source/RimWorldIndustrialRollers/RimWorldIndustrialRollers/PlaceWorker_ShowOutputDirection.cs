using System;
using System.Collections.Generic;
using MovingFloor;
using UnityEngine;
using Verse;

namespace RimWorldIndustrialRollers;

internal class PlaceWorker_ShowOutputDirection : PlaceWorker
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

    private List<IntVec3> GetFrontRightAndLeft(IntVec3 loc, Rot4 rot)
    {
        var list = new List<IntVec3>();
        if ((int)Math.Floor(rot.AsAngle) == 0 || (int)Math.Floor(rot.AsAngle) == 180)
        {
            list.Add(loc + GenAdj.CardinalDirections[1]);
            list.Add(loc + GenAdj.CardinalDirections[3]);
        }
        else
        {
            list.Add(loc + GenAdj.CardinalDirections[0]);
            list.Add(loc + GenAdj.CardinalDirections[2]);
        }

        return list;
    }

    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        var frontAndBack = GetFrontAndBack(center, rot);
        var frontRightAndLeft = GetFrontRightAndLeft(center, rot);
        if ((int)rot.AsAngle == 0 || (int)rot.AsAngle == 270)
        {
            (frontRightAndLeft[1], frontRightAndLeft[0]) = (frontRightAndLeft[0], frontRightAndLeft[1]);
        }

        if (def.defName.Contains("Left"))
        {
            RailLogicHelper.DrawArrowPointingAt(center.ToVector3(), frontRightAndLeft[0].ToVector3());
        }
        else if (def.defName.Contains("Right"))
        {
            RailLogicHelper.DrawArrowPointingAt(center.ToVector3(), frontRightAndLeft[1].ToVector3());
        }
        else if (def.defName.Contains("Splitter"))
        {
            if ((int)rot.AsAngle != 0 && (int)rot.AsAngle != 90)
            {
                (frontAndBack[1], frontAndBack[0]) = (frontAndBack[0], frontAndBack[1]);
            }

            var vector = new Vector3(1f, 0f, 0f);
            switch ((int)rot.AsAngle)
            {
                case 90:
                    vector = new Vector3(0f, 0f, -1f);
                    break;
                case 180:
                    vector = new Vector3(-1f, 0f, 0f);
                    break;
                case 270:
                    vector = new Vector3(0f, 0f, 1f);
                    break;
            }

            RailLogicHelper.DrawArrowPointingAt(center.ToVector3(), frontAndBack[0].ToVector3());
            RailLogicHelper.DrawArrowPointingAt(center.ToVector3() + vector, frontAndBack[0].ToVector3() + vector);
        }
        else
        {
            if ((int)rot.AsAngle != 0 && (int)rot.AsAngle != 90)
            {
                (frontAndBack[1], frontAndBack[0]) = (frontAndBack[0], frontAndBack[1]);
            }

            RailLogicHelper.DrawArrowPointingAt(center.ToVector3(), frontAndBack[0].ToVector3());
        }

        base.DrawGhost(def, center, rot, ghostCol, thing);
    }
}