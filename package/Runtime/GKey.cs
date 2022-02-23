using System;

[Serializable]
public readonly struct GKey : IEquatable<GKey>
{
    public readonly int id;
    public string str => G.LookupName(id, out var name) ? string.Intern(name) : "-";

    public GKey(in int key)
    {
        id = key;
    }

    public GKey(in string name)
        : this(Hash(name))
    {
        G.AddName(id, name);
    }

    public bool Equals(GKey other)
    {
        return id.Equals(other.id);
    }

    public override bool Equals(object obj)
    {
        if (obj is GKey h)
            return Equals(h);
        return false;
    }

    public override int GetHashCode()
    {
        return id;
    }

    public override string ToString()
    {
        if (G.LookupName(id, out var name))
            return $"{name} ({id})";
        return id.ToString();
    }

    public static int Hash(in string name)
    {
        return name.GetHashCode();
    }
}
