public static class GExtensions
{
    public static GHandle Create(this GHandle h, in string name)
    {
        return G.Create(h, name);
    }

    public static GHandle Add(this GHandle h, in GHandle child)
    {
        G.Add(h, child);
        return child;
    }

    public static GHandle Set(this GHandle h, in GKey name, in string text) { G.Set(h, name, text); return h; }
    public static GHandle Set(this GHandle h, in GKey name, in double number) { G.Set(h, name, number); return h; }
    public static GHandle Set(this GHandle h, in GKey name, in GHandle handle) { G.Set(h, name, handle); return h; }
    public static GHandle Set(this GHandle h, in GKey name, in bool value) { G.Set(h, name, value); return h; }

    public static GHandle Set(this GHandle h, in string name, in string text) { G.Set(h, name, text); return h; }
    public static GHandle Set(this GHandle h, in string name, in double number) { G.Set(h, name, number); return h; }
    public static GHandle Set(this GHandle h, in string name, in GHandle handle) { G.Set(h, name, handle); return h; }
    public static GHandle Set(this GHandle h, in string name, in bool value) { G.Set(h, name, value); return h; }

    public static GHandle Set(this GHandle h, in string text) => G.Set(h, text);
    public static GHandle Set(this GHandle h, in double number) => G.Set(h, number);
    public static GHandle Set(this GHandle h, in GHandle handle) => G.Set(h, handle);
    public static GHandle Set(this GHandle h, in bool value) => G.Set(h, value);

    public static GValue Get(this GHandle h) => G.Get(h);
    public static GValue Get(this GHandle h, in string name) => G.Get(h, name);

    public static GHandle Find(this GHandle h, in string query)
    {
        return G.Find(h, query);
    }

    public static GHandle Find(this GHandle h, in GKey name)
    {
        return G.Find(h, name);
    }

    public static double ToNumber(this GHandle h)
    {
        return G.Get(h).number;
    }

    public static GValue ToValue(this GHandle h)
    {
        return G.Get(h);
    }

    public static string ToJSON(this GHandle h, in GJSONFormatting formatting = GJSONFormatting.None)
    {
        return G.ToJSON(h, formatting);
    }
}
