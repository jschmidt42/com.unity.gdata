using System;

public enum GType : byte
{
    Undefined = 0,
    Number = 1,
    Text = 2,
    Handle = 3,
    Boolean = 4
}

[Serializable]
public readonly struct GValue : IEquatable<GValue>
{
    public readonly GType type;
    public readonly double number;
    
    public ulong u0 => (ulong)BitConverter.DoubleToInt64Bits(number);
    public readonly ulong u1;

    public bool valid => type != GType.Undefined;

    public static GValue undefined = default;

    public GValue(in string text)
    {
        var k = new GKey(text);
        type = GType.Text;
        number = (double)k.id;
        u1 = (ulong)k.id;
    }

    public GValue(in double number)
    {
        type = GType.Number;
        this.number = number;
        u1 = default;
    }

    public GValue(in GHandle handle)
    {
        type = GType.Handle;
        number = BitConverter.Int64BitsToDouble((long)handle.key);
        u1 = handle.parent;
    }

    public GValue(in bool value)
    {
        type = GType.Boolean;
        u1 = value ? 1UL: 0UL;
        number = value ? 1d : 0;
    }

    public bool Equals(GValue other)
    {
        return type.Equals(other.type) && number.Equals(other.number) && u1.Equals(other.u1);
    }

    public override bool Equals(object obj)
    {
        if (obj is GValue h)
            return Equals(h);
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(type, number, u1);
    }

    public override string ToString()
    {
        return ToString(JSON: false);
    }

    public static implicit operator bool(in GValue v) => v.number != 0;
    public static implicit operator string(in GValue v) => v.ToString(JSON: false);
    public static implicit operator int(in GValue v) => Convert.ToInt32(v.number);
    public static implicit operator uint(in GValue v) => Convert.ToUInt32(v.number);
    public static implicit operator long(in GValue v) => (long)v.u0;
    public static implicit operator ulong(in GValue v) => v.u0;
    public static implicit operator float(in GValue v) => Convert.ToSingle(v.number);
    public static implicit operator double(in GValue v) => v.number;
    public static implicit operator GHandle(in GValue v) => v.ToHandle();

    public GHandle ToHandle()
    {
        return new GHandle(u0, u1);
    }

    public string ToString(bool JSON)
    {
        if (type == GType.Undefined)
            return "null";
        if (type == GType.Number)
            return number.ToString();
        if (type == GType.Boolean)
            return u1 == 0 ? "false" : "true";
        if (type == GType.Text && G.LookupName((int)number, out var text))
            return G.EscapeJSON(text, JSON);
        if (type == GType.Handle)
        { 
            if (JSON)
                return u0.ToString();
            return ToHandle().ToString();
        }
        return G.EscapeJSON(string.Empty, JSON);
    }
}
