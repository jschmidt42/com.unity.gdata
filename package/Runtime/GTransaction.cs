using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// TODO: Log transaction
public enum GTransactionType : byte
{
    Invalid = 0,

    // String table transactions
    RecordName,

    // CRUD Entity transactions
    CreateEntity,
    AddChild,
    RemoveChild,
    ClearChildren,
    
    // Value transactions
    SetValue
}

[Serializable]
public readonly struct GTransaction : IEquatable<GTransaction>, IComparable<GTransaction>
{
    public static GTransaction invalid = default;

    public readonly int timestamp;
    public readonly GTransactionType type;

    // Entity
    public readonly ulong u0; // key
    public readonly ulong u1; // parent

    // GValue or entity parent
    public readonly ulong u2; // Type
    public readonly ulong u3; // value lo / parent key
    public readonly ulong u4; // value hi / parent-parent

    // Entity or key name
    public readonly string text;

    public GHandle handle => new GHandle(u0, u1, text);
    public GHandle parent => new GHandle(u3, u4);
    public bool valid => type != GTransactionType.Invalid;

    private GTransaction(in GTransactionType type)
    {
        timestamp = G.Time();
        this.type = type;
        u0 = u1 = u2 = u3 = u4 = default;
        text = null;
    }

    public GTransaction(in GTransactionType type, in int key, in string text)
        : this(type)
    {
        u0 = (ulong)key;
        this.text = string.Intern(text);
    }

    public GTransaction(in GTransactionType type, in GHandle entity)
        : this(type, entity.name.id, entity.name.str)
    {
        u0 = entity.key;
        u1 = entity.parent;
    }

    public GTransaction(in GTransactionType setValue, in GHandle entity, in GValue value)
        : this(setValue, entity)
    {
        u2 = (ulong)value.type;
        u3 = value.u0;
        u4 = value.u1;
        if (value.type == GType.Text)
            text = value.ToString();
    }

    public GTransaction(in GTransactionType type, in GHandle entity, in GHandle parent)
        : this(type, entity)
    {
        u3 = parent.key;
        u4 = parent.parent;
    }

    public GTransaction(BinaryReader r)
    {
        timestamp = r.ReadInt32();
        type = (GTransactionType)r.ReadByte();
        u0 = r.ReadUInt64();
        u1 = r.ReadUInt64();
        u2 = r.ReadUInt64();
        u3 = r.ReadUInt64();
        u4 = r.ReadUInt64();

        if (r.ReadBoolean())
            text = new GKey(r.ReadString()).str;
        else
            text = null;
    }

    public void Write(BinaryWriter w)
    {
        w.Write(timestamp);
        w.Write((byte)type);
        w.Write(u0);
        w.Write(u1);
        w.Write(u2);
        w.Write(u3);
        w.Write(u4);

        var hasText = !string.IsNullOrEmpty(text);
        w.Write(hasText);
        if (hasText)
            w.Write(text ?? string.Empty);
    }

    public override string ToString()
    {
        return $"{timestamp} > {type} > {u0} > {(GType)u2}";
    }

    public override int GetHashCode()
    {
        return timestamp;
    }

    public override bool Equals(object obj)
    {
        return Equals((GTransaction)obj);
    }

    public bool Equals(GTransaction other)
    {
        return timestamp.Equals(other.timestamp);
    }

    public int CompareTo(GTransaction other)
    {
        return timestamp.CompareTo(other.timestamp);
    }

    public GValue ToValue()
    {
        var type = (GType)u2;
        switch (type)
        {
            case GType.Undefined: return GValue.undefined;
            case GType.Number: return new GValue(BitConverter.Int64BitsToDouble((long)u3));
            case GType.Text: return new GValue(text);
            case GType.Handle: return new GValue(new GHandle(u3, u4, text));
            case GType.Boolean: return new GValue(u3 != 0);
        }

        throw new NotSupportedException("Invalid value type");
    }
}

public partial class G
{
    const string SerializeVersion = "_G1";

    public static IEnumerable<GTransaction> Diff()
    {
        return s_Transaction;
    }

    public static void Apply(IEnumerable<GTransaction> transactions)
    {
        foreach (var t in transactions)
            Apply(t);
    }

    static bool Apply(in GTransaction transaction)
    {
        if (transaction.type == GTransactionType.RecordName)
        {
            return s_NameLT.TryAdd((int)transaction.u0, transaction.text);
        }
        else if (transaction.type == GTransactionType.CreateEntity)
        {
            var entity = transaction.handle;

            if (!s_Entities.TryGetValue(entity.parent, out var parent))
                throw new UnityException($"Failed to find parent entity {entity.parent} with {entity}");

            if (!s_Entities.TryAdd(entity.key, entity))
                throw new UnityException($"Failed to bind {entity} ({entity.GetHashCode()}) to parent {parent}");

            if (parent.valid)
            {
                var children = GetOrCreateChildren(parent);
                while (!children.TryAdd(children.Count, entity.key))
                    ;
            }

            return true;
        }
        else if (transaction.type == GTransactionType.SetValue)
        {
            var entity = transaction.handle;
            s_Values[entity] = transaction.ToValue();
            return true;
        }
        else if (transaction.type == GTransactionType.AddChild)
        {
            var children = GetOrCreateChildren(transaction.parent.key);
            var child = transaction.handle;
            while (!children.TryAdd(children.Count, child.key))
                ;
            return true;
        }
        else if (transaction.type == GTransactionType.ClearChildren)
        {
            var entity = transaction.handle;
            if (!s_Children.TryGetValue(entity.key, out var children) || children == null)
                return false;
            children.Clear();
            return true;
        }
        else if (transaction.type == GTransactionType.RemoveChild)
        {
            if (!s_Children.TryGetValue(transaction.parent.key, out var children) || children == null)
                return false;
            var r = IndexOfChild(children, transaction.handle.key);
            if (r == -1)
                return false;
            return children.TryRemove(r, out _);
        }

        throw new NotImplementedException($"Cannot apply transaction {transaction}");
    }

    public static bool Serialize(Stream stream, IEnumerable<GTransaction> transactions)
    {
        using (var w = new BinaryWriter(stream))
        {
            w.Write(SerializeVersion);
            foreach (var t in transactions)
                t.Write(w);
        }

        return true;
    }

    public static IEnumerable<GTransaction> Deserialize(Stream stream)
    {
        using var r = new BinaryReader(stream);
        if (!string.Equals(r.ReadString(), SerializeVersion, StringComparison.Ordinal))
            yield break;

        while (stream.Position < stream.Length)
            yield return new GTransaction(r);
    }
}
