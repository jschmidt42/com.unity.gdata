using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public readonly struct GHandle : IEquatable<GHandle>, IEnumerable<GHandle>, ICollection<GHandle>
{
    public readonly GKey name;
    public readonly ulong key;
    public readonly ulong parent;

    public static GHandle nil = new GHandle(0);

    public bool valid => key != 0;
    public GValue value => G.Get(this);
    public static implicit operator GValue(in GHandle e) => G.Get(e);

    public GHandle this[int index]
    {
        get
        {
            var children = G.GetChildren(this);
            if (children == null)
                throw new UnityException($"Entity has no children");

            if (children.TryGetValue(index, out var h))
                return G.LookupEntity(h);
            return nil;
        }

        set
        {
            var children = G.GetOrCreateChildren(this);
            // TODO: Log transaction (SetChild)
            children[index] = value.key;
        }
    }

    public GHandle this[in string name]
    {
        get => G.GetOrCreate(this, name);
    }

    public int Count => G.GetEntityCount(this);
    bool ICollection<GHandle>.IsReadOnly => false;

    internal GHandle(in ulong key)
    {
        parent = 0UL;
        this.key = key;
        name = default;
    }

    public GHandle(in string name)
        : this(nil, name)
    {
    }

    public GHandle(in GHandle parent, in GKey name)
    {
        this.name = name;
        this.parent = parent.key;
        key = Combine(G.Time(), name.GetHashCode());
    }

    public GHandle(in GHandle parent, in string name) 
        : this(parent, new GKey(name))
    {
    }

    public GHandle(in ulong key, in ulong parent, in string name = null)
    {
        this.key = key;
        this.parent = parent;

        if (string.IsNullOrEmpty(name))
        {
            Decombine(key, out int pn, out _);
            this.name = new GKey(pn);
        }
        else
        {
            this.name = new GKey(name);
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is ulong key)
            return this.key.Equals(key);
        if (obj is not GHandle h)
            throw new ArgumentException("Invalid type", nameof(obj));
        return Equals(h);
    }

    public bool Equals(GHandle other)
    {
        return key.Equals(other.key);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(key);
    }

    public override string ToString()
    {
        if (key == 0)
            return "nil";

        string s = name.str;
        if (parent == 0)
            return $"{s} [{key}]";

        var pe = G.LookupEntity(parent);
        if (pe.valid)
            return $"{pe.name.str}.{s} [{key}]";
        
        return key.ToString();
    }

    static ulong Combine(in int hi, in int lo)
    {
        uint ulo = (uint)lo;
        ulong uhi = (uint)hi;
        return uhi << 32 | ulo;
    }

    static void Decombine(in ulong c, out int lo, out int hi)
    {
        lo = (int)(c & 0xFFFFFFFFUL);
        hi = (int)(c >> 32);
    }

    public IEnumerator<GHandle> GetEnumerator()
    {
        return G.EnumerateChildren(this).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<GHandle>.Add(GHandle child) => G.Add(this, child);
    void ICollection<GHandle>.Clear() => G.ClearChildren(this);
    bool ICollection<GHandle>.Contains(GHandle child) => G.ContainsChild(this, child);
    bool ICollection<GHandle>.Remove(GHandle child) => G.RemoveChild(this, child);
    void ICollection<GHandle>.CopyTo(GHandle[] array, int arrayIndex)
    {
        foreach (var c in G.EnumerateChildren(this))
            array[arrayIndex++] = c;
    }
}
