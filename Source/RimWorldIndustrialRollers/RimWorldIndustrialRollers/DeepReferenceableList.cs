using System.Collections;
using System.Collections.Generic;
using Verse;

namespace RimWorldIndustrialRollers;

public abstract class DeepReferenceableList<T> : IEnumerable<T>, IExposable where T : ILoadReferenceable
{
    protected List<T> referenceableList;

    public DeepReferenceableList()
    {
        referenceableList = new List<T>();
    }

    public int Count => referenceableList.Count;

    public T this[int index]
    {
        get => referenceableList[index];
        set => referenceableList[index] = value;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)referenceableList).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)referenceableList).GetEnumerator();
    }

    public virtual void ExposeData()
    {
        Scribe_Collections.Look(ref referenceableList, "loadReferenceIDs", LookMode.Reference);
    }

    public IEnumerable<T> Concat(IEnumerable<T> other)
    {
        using (var enumerator = GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        foreach (var item in other)
        {
            yield return item;
        }
    }

    public void Add(T t)
    {
        referenceableList.Add(t);
    }

    public bool Remove(T t)
    {
        return referenceableList.Remove(t);
    }

    public void RemoveAt(int index)
    {
        referenceableList.RemoveAt(index);
    }

    public static implicit operator List<T>(DeepReferenceableList<T> dpl)
    {
        return dpl.referenceableList;
    }
}