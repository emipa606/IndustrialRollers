using System.Collections.Generic;
using Verse;

namespace RimWorldIndustrialRollers;

public sealed class DeepThingList : DeepReferenceableList<Thing>
{
    public DeepThingList()
    {
        referenceableList = new List<Thing>();
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref referenceableList, "pawns", LookMode.Reference);
    }

    public static implicit operator DeepThingList(List<Thing> list)
    {
        return new DeepThingList
        {
            referenceableList = list
        };
    }
}