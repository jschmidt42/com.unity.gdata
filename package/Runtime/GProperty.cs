using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct GPropertyInt
{
    public readonly GHandle data;
    public GPropertyInt(in GHandle data, string name)
    {
        Debug.Assert(data.valid);
        this.data = G.GetOrCreate(data, name);
    }

    public GPropertyInt(in GHandle data)
    {
        Debug.Assert(data.valid);
        this.data = data;
    }

    public static implicit operator int(in GPropertyInt p) => p.Get();
    
    public int Get()
    {
        return data.Get();
    }

    public void Set(int value)
    {
        data.Set(value);
    }

    public override string ToString()
    {
        return data.ToString();
    }
}

public readonly struct GPropertyNumber
{
    public readonly GHandle data;

    public GPropertyNumber(in GHandle data, string name)
    {
        Debug.Assert(data.valid);
        this.data = G.GetOrCreate(data, name);
    }

    public GPropertyNumber(GHandle data)
    {
        Debug.Assert(data.valid);
        this.data = data;
    }

    public static implicit operator double(in GPropertyNumber p) => p.Get();

    public double Get()
    {
        return data.Get();
    }

    public void Set(double value)
    {
        data.Set(value);
    }

    public override string ToString()
    {
        return data.ToString();
    }
}

public readonly struct GPropertyBool
{
    public readonly GHandle data;

    public GPropertyBool(in GHandle data, string name)
    {
        Debug.Assert(data.valid);
        this.data = G.GetOrCreate(data, name);
    }

    public GPropertyBool(GHandle data)
    {
        Debug.Assert(data.valid);
        this.data = data;
    }
    public static implicit operator bool(in GPropertyBool p) => p.Get();
    
    public bool Get()
    {
        return data.Get();
    }

    public void Set(bool value)
    {
        data.Set(value);
    }

    public override string ToString()
    {
        return data.ToString();
    }
}

public readonly struct GPropertyString
{
    public readonly GHandle data;

    public GPropertyString(in GHandle data, string name)
    {
        Debug.Assert(data.valid);
        this.data = G.GetOrCreate(data, name);
    }

    public GPropertyString(GHandle data)
    {
        Debug.Assert(data.valid);
        this.data = data;
    }
    
    public static implicit operator string(in GPropertyString p) => p.Get();
    
    public string Get()
    {
        return data.Get();
    }

    public void Set(string value)
    {
        data.Set(value);
    }

    public override string ToString()
    {
        return Get();
    }
}
