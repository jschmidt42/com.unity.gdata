using System;
using System.Collections.Generic;
using UnityEditor.Search;

static class GSearch
{
    const string providerId = "g";
    static QueryEngine<GHandle> s_QueryEngine;

    [SearchItemProvider]
    internal static SearchProvider CreateProvider()
    {
        return new SearchProvider(providerId, "G", FetchGs)
        {
            fetchLabel = FetchLabel,
            fetchDescription = FetchDescription
        };
    }

    [SearchSelector("gname", provider: providerId)] internal static object SelectName(SearchItem item) => ((GHandle)item.data).name.str;
    [SearchSelector("gkey", provider: providerId)] internal static object SelectId(SearchItem item) => ((GHandle)item.data).key;
    [SearchSelector("gparent", provider: providerId)] internal static object SelectParent(SearchItem item) => ((GHandle)item.data).parent;
    [SearchSelector("gpath", provider: providerId)] internal static object SelectPath(SearchItem item) => G.Path((GHandle)item.data);
    [SearchSelector("gvalue", provider: providerId)] internal static object SelectValue(SearchItem item) => ((GHandle)item.data).value;

    static string FetchLabel(SearchItem item, SearchContext context)
    {
        var e = (GHandle)item.data;
        return $"{G.Path(e)} [{e.key}]";
    }

    static string FetchDescription(SearchItem item, SearchContext context)
    {
        var e = (GHandle)item.data;
        var v = G.Get(e);

        if ((item.options & SearchItemOptions.FullDescription) == SearchItemOptions.FullDescription)
            return $"{G.Path(e)}\n<b>Key: </b><i>{e.key}</i>\n<b>Value: </b>{FetchValueDescription(e, v)}";

        if ((item.options & SearchItemOptions.Compacted) == SearchItemOptions.Compacted)
            return $"{G.Path(e)} [<i>{e.key}</i>] => {FetchValueDescription(e, v)}";

        return FetchValueDescription(e, v);
    }

    static string FetchValueDescription(in GHandle e, in GValue v)
    {
        if (v.valid)
            return $"{v} ({G.GetEntityCount(e)} children)";
        return $"Nil ({G.GetEntityCount(e)} children)";
    }

    static IEnumerable<SearchItem> FetchGs(SearchContext context, SearchProvider provider)
    {
        if (string.IsNullOrEmpty(context.searchQuery))
            yield break;

        var qe = GetQueryEngine();
        var query = qe.Parse(context.searchQuery, useFastYieldingQueryHandler: true);

        if (!query.valid)
        {
            foreach (var err in query.errors)
                context.AddSearchQueryError(new SearchQueryError(err, context, provider));
            yield break;
        }

        foreach (var e in query.Apply(G.EnumerateEntities()))
        {
            if (e.valid)
                yield return provider.CreateItem(context, e.key.ToString(), e.name.id, null, null, null, e);
            else
                yield return null;
        }
    }

    static QueryEngine<GHandle> GetQueryEngine()
    {
        if (s_QueryEngine == null)
        { 
            s_QueryEngine = new QueryEngine<GHandle>();
            s_QueryEngine.SetSearchDataCallback(EnumerateWords, StringComparison.OrdinalIgnoreCase);
            s_QueryEngine.AddFilter("id", e => e.key);
            s_QueryEngine.AddFilter("parent", e => e.parent);
            s_QueryEngine.AddFilter("name", e => e.name.str, StringComparison.OrdinalIgnoreCase);
            s_QueryEngine.AddFilter("value", e => G.Get(e).ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return s_QueryEngine;
    }

    static IEnumerable<string> EnumerateWords(GHandle e)
    {
        yield return "*";
        yield return e.key.ToString();
        if (!string.IsNullOrEmpty(e.name.str))
            yield return e.name.str;
    }
}
