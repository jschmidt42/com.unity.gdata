using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public partial class G
{
    const string RootKey = "Root";

    private static int s_Time = int.MinValue;
    private static ConcurrentDictionary<int, string> s_NameLT;
    private static ConcurrentDictionary<GHandle, GValue> s_Values;
    private static ConcurrentDictionary<ulong, GHandle> s_Entities;
    private static ConcurrentDictionary<ulong, ConcurrentDictionary<int, ulong>> s_Children;
    private static ConcurrentQueue<GTransaction> s_Transaction;

    public static GHandle root { get; private set; }

    static G()
    {
        Reset();
    }

    public static int Time()
    {
        return Interlocked.Increment(ref s_Time);
    }

    public static bool LookupName(in int key, out string name)
    {
        return s_NameLT.TryGetValue(key, out name);
    }

    public static GHandle LookupEntity(in ulong key)
    {
        return s_Entities[key];
    }

    public static bool AddName(in int key, in string name)
    {
        if (s_NameLT.ContainsKey(key))
            return false;
        return Record(new GTransaction(GTransactionType.RecordName, key, name));
    }

    public static GHandle Create(in GHandle parent, in GKey name)
    {
        var child = new GHandle(parent, name);
        Record(new GTransaction(GTransactionType.CreateEntity, child, parent));
        return child;
    }

    public static GHandle Create(in string name)
    {
        return Create(root, name);
    }

    public static GHandle Create(in GHandle entity, in string name)
    {
        return Create(entity, new GKey(name));
    }

    public static GHandle Find(in string query, bool throwNotFound = false)
    {
        return Find(query, out _, throwNotFound);
    }

    public static GHandle Find(in string query, out int index, bool throwNotFound = false)
    {
        return Find(root, query, out index, throwNotFound);
    }

    public static GHandle Find(in GHandle entity, in GKey name)
    {
        return Find(entity, name, out _, throwNotFound: true);
    }

    public static GHandle Find(in GHandle entity, in GKey name, out int index, bool throwNotFound = false)
    {
        index = -1;
        if (!s_Children.TryGetValue(entity.key, out var children) || children == null)
            return GHandle.nil;
        
        foreach (var kvp in children)
        {
            if (!s_Entities.TryGetValue(kvp.Value, out var c))
                continue;
            if (c.name.Equals(name))
            {
                index = kvp.Key;
                return c;
            }
        }
        
        if (throwNotFound)
            throw new UnityException($"Failed to find {name} within {entity}");

        return GHandle.nil;
    }

    public static GHandle Find(in GHandle entity, in string query, bool throwNotFound = false)
    {
        return Find(entity, query, out _, throwNotFound);
    }

    public static GHandle Find(in GHandle entity, in string query, out int index, bool throwNotFound = false)
    {
        index = -1;
        GHandle match = entity;
        var tokens = query.Split(".");
        for (int i = 0; i < tokens.Length; ++i)
        {
            var t = tokens[i];
            var ap = t.LastIndexOf('[');
            var bp = t.LastIndexOf(']');
            string aIndex = null;
            if (bp > ap)
            { 
                aIndex = t.Substring(ap+1, bp-ap-1);
                t = t.Substring(0, ap);
            }

            var tk = new GKey(t.GetHashCode());
            match = Find(match, tk, out index, throwNotFound: false);
            if (!match.valid)
            {  
                if (throwNotFound)
                    throw new UnityException($"Failed to find {tk} ({query}) within {entity}");
                break;
            }

            if (aIndex != null)
            {
                if (!TryParse(aIndex, out int fieldIndex))
                    throw new UnityException($"Failed to parse index {aIndex} for {t} ({query}) within {entity}");

                if (!s_Children.TryGetValue(match.key, out var children) || children == null)
                { 
                    if (throwNotFound)
                        throw new UnityException($"{aIndex} for {t} ({query}) cannot be indexed within {match}");
                    return GHandle.nil;
                }

                if (!children.TryGetValue(fieldIndex, out var mk) || !s_Entities.TryGetValue(mk, out match))
                {
                    if (throwNotFound)
                        throw new UnityException($"{t}[{fieldIndex}] ({query}) not found in {match}");
                    return GHandle.nil;
                }
            }
        }

        return match;
    }

    internal static void ClearChildren(in GHandle entity)
    {
        Record(new GTransaction(GTransactionType.ClearChildren, entity));
    }

    internal static int IndexOfChild(in GHandle entity, in GHandle child)
    {
        if (!s_Children.TryGetValue(entity.key, out var children) || children == null)
            return -1;   
        return IndexOfChild(children, child.key);
    }

    static int IndexOfChild(in IEnumerable<KeyValuePair<int, ulong>> children, in ulong childKey)
    {
        foreach (var c in children)
        {
            if (childKey.Equals(c.Value))
                return c.Key;
        }
        return -1;
    }

    internal static bool ContainsChild(in GHandle entity, in GHandle child)
    {
        return IndexOfChild(entity, child) != -1;
    }

    internal static bool RemoveChild(in GHandle entity, in GHandle child)
    {
        return Record(new GTransaction(GTransactionType.RemoveChild, child, entity));
    }

    internal static IEnumerable<GHandle> EnumerateChildren(in GHandle entity)
    {
        if (!s_Children.TryGetValue(entity.key, out var children) || children == null)
            return Enumerable.Empty<GHandle>();
        return children.Values.Select(k => s_Entities[k]);
    }

    internal static IDictionary<int, ulong> GetChildren(in GHandle entity)
    {
        if (s_Children.TryGetValue(entity.key, out var children) && children != null)
            return children;
        return null;
    }

    internal static ConcurrentDictionary<int, ulong> GetOrCreateChildren(in GHandle entity)
    {
        return GetOrCreateChildren(entity.key);
    }

    internal static ConcurrentDictionary<int, ulong> GetOrCreateChildren(in ulong key)
    {
        if (s_Children.TryGetValue(key, out var children) && children != null)
            return children;

        lock (s_Children)
        {
            if (!s_Children.TryGetValue(key, out children))
            {
                children = new ConcurrentDictionary<int, ulong>();
                if (!s_Children.TryAdd(key, children))
                    throw new UnityException($"Failed to create children list for {s_Entities[key]}");
            }

            return children;
        }
    }

    public static GValue Get(in GHandle entity)
    {
        if (!entity.valid)
            return GValue.undefined;

        if (s_Values.TryGetValue(entity, out var value))
            return value;

        return GValue.undefined;
    }

    public static GValue Get(in GHandle entity, in string name)
    {
        var e = Find(entity, name);
        if (!e.valid)
            return GValue.undefined;

        return Get(e);
    }

    public static GHandle Set(in GHandle entity, in string text) => Set(entity, new GValue(text));
    public static GHandle Set(in GHandle entity, in double number) => Set(entity, new GValue(number));
    public static GHandle Set(in GHandle entity, in GHandle reference) => Set(entity, new GValue(reference));
    public static GHandle Set(in GHandle entity, in bool value) => Set(entity, new GValue(value));

    public static GHandle Set(in GHandle entity, in string name, in string value) => Set(GetOrCreate(entity, name), value);
    public static GHandle Set(in GHandle entity, in string name, in double number) => Set(GetOrCreate(entity, name), number);
    public static GHandle Set(in GHandle entity, in string name, in GHandle reference) => Set(GetOrCreate(entity, name), reference);
    public static GHandle Set(in GHandle entity, in string name, in bool value) => Set(GetOrCreate(entity, name), value);

    public static GHandle Set(in GHandle entity, in GKey name, in string text) => Set(GetOrCreate(entity, name), text);
    public static GHandle Set(in GHandle entity, in GKey name, in double number) => Set(GetOrCreate(entity, name), number);
    public static GHandle Set(in GHandle entity, in GKey name, in GHandle reference) => Set(GetOrCreate(entity, name), reference);
    public static GHandle Set(in GHandle entity, in GKey name, in bool value) => Set(GetOrCreate(entity, name), value);

    public static GHandle Set(in GHandle entity, in GValue value)
    {
        Record(new GTransaction(GTransactionType.SetValue, entity, value));
        return entity;
    }

    public static GHandle Set(in GHandle entity, in string name, in IEnumerable<GHandle> entities)
    {
        var g = GetOrCreate(entity, name);
        Record(new GTransaction(GTransactionType.ClearChildren, g));
        foreach (var e in entities)
            Record(new GTransaction(GTransactionType.AddChild, e, g));

        return g;
    }

    public static GHandle Add(in GHandle entity, in GHandle append)
    {
        Record(new GTransaction(GTransactionType.AddChild, append, entity));
        return entity;
    }

    private static GHandle GetOrCreate(in GHandle entity, in GKey name)
    {
        var e = Find(entity, name, out _, throwNotFound: false);
        if (e.valid)
            return e;
        return Create(entity, name);
    }

    internal static GHandle GetOrCreate(in GHandle entity, in string name)
    {
        var e = Find(entity, name);
        if (e.valid)
            return e;
        return Create(entity, name);
    }

    public static int GetEntityCount()
    {
        return s_Entities.Count;
    }

    public static int GetEntityCount(in GHandle entity)
    {
        return GetChildren(entity)?.Count ?? 0;
    }

    public static void Reset()
    {
        s_Time = Time();
        s_NameLT = new ConcurrentDictionary<int, string>();
        s_Entities = new ConcurrentDictionary<ulong, GHandle>();
        s_Children = new ConcurrentDictionary<ulong, ConcurrentDictionary<int, ulong>>();
        s_Values = new ConcurrentDictionary<GHandle, GValue>();
        s_Transaction = new ConcurrentQueue<GTransaction>();
        
        root = new GHandle(ulong.MaxValue, 0, RootKey);
        s_Entities[root.key] = root;
    }    

    public static IEnumerable<GHandle> EnumerateEntities()
    {
        foreach (var kvp in s_Entities)
            yield return kvp.Value;
    }

    public static string Path(in GHandle entity)
    {
        var current = entity;
        var path = current.name.str;
        while (current.parent != 0 && s_Entities.TryGetValue(current.parent, out var parent) && parent.valid)
        {
            current = parent;
            path = $"{parent.name.str}.{path}";
        }
        return path.Replace($"{RootKey}.", "");
    }

    static bool Record(in GTransaction transaction)
    {
        if (!Apply(transaction))
            return false;
        s_Transaction.Enqueue(transaction);
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= UpdateGSearch;
        UnityEditor.EditorApplication.delayCall += UpdateGSearch;
        #endif
        return true;
    }

    #if UNITY_EDITOR
    private static void UpdateGSearch()
    {
        var qs = Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>().FirstOrDefault() as UnityEditor.Search.ISearchView;
        if (qs != null)
            qs.Refresh(UnityEditor.Search.RefreshFlags.ItemsChanged);
    }
    #endif
}
