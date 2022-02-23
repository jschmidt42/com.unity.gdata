using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

[Flags]
public enum GJSONFormatting
{
    None = 0,
    RootBraces = 1 << 0,
    EscapeBraces = 1 << 1
}

public partial class G
{
    public static string ToJSON(GJSONFormatting formatting = GJSONFormatting.None)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");

        // Entities
        sb.AppendLine("\t\"entities\": {");
        bool first = true;
        foreach (var e in s_Entities)
        {
            if (first)
                first = false;
            else
                sb.AppendLine(",");
            sb.Append($"\t\t\"{e.Key}\": {{ \"{e.Value.name.str}\": {e.Value.name.id}, \"parent\": {e.Value.parent}, \"crc32\": {e.Key.GetHashCode()} }}");
        }
        sb.AppendLine($"\n\t}}");

        // Values
        sb.AppendLine("\t\"values\": {");
        first = true;
        foreach (var e in s_Values)
        {
            if (first)
                first = false;
            else
                sb.AppendLine(",");
            sb.Append($"\t\t\"{e.Key.key}\": {{ \"{e.Key.name.str}\": {e.Value.ToString(JSON: true)}, \"parent\": {e.Key.parent}, \"type\": \"{e.Value.type}\" }}");
        }
        sb.AppendLine("\n\t}");

        // String table
        sb.AppendLine("\t\"strings\": {");
        first = true;
        foreach (var e in s_NameLT)
        {
            if (first)
                first = false;
            else
                sb.AppendLine(",");
            sb.Append($"\t\t\"{e.Key}\": \"{e.Value}\"");
        }
        sb.AppendLine("\n\t}");

        // Hierarchy
        sb.Append("\t\"root\": ");
        var visited = new HashSet<ulong>();
        ToChildrenJSON(sb, root, "\t\t", visited);

        sb.Append("\n}");

        if ((formatting & GJSONFormatting.EscapeBraces) == GJSONFormatting.EscapeBraces)
            return sb.ToString().Replace("{", "{{").Replace("}", "}}");
        return sb.ToString();
    }

    public static string ToJSON(in GHandle entity, GJSONFormatting formatting = GJSONFormatting.None)
    {
        var sb = new StringBuilder();
        var rootIndent = "\t";
        if ((formatting & GJSONFormatting.RootBraces) == GJSONFormatting.RootBraces)
            sb.AppendLine("{");
        else
            rootIndent = string.Empty;

        // Hierarchy
        sb.Append($"{rootIndent}\"{entity.name.str}\": ");
        ToChildrenJSON(sb, entity, $"{rootIndent}\t", new HashSet<ulong>());

        if ((formatting & GJSONFormatting.RootBraces) == GJSONFormatting.RootBraces)
            sb.Append("\n}");

        if ((formatting & GJSONFormatting.EscapeBraces) == GJSONFormatting.EscapeBraces)
            return sb.ToString().Replace("{", "{{").Replace("}", "}}");
        return sb.ToString();
    }

    internal static string EscapeJSON(in string text, in bool JSON)
    {
        if (!JSON)
            return text;
        return "\"" + text + "\"";
    }

    static void ToChildrenJSON(StringBuilder sb, in GHandle e, in string indent, ISet<ulong> visited)
    {
        if (visited.Contains(e.key))
        {
            sb.Append($"{e.key}");
            return;
        }

        visited.Add(e.key);
        if (s_Children.TryGetValue(e.key, out var children) && children != null && children.Count != 0)
        {
            sb.AppendLine("{");
            sb.Append($"{indent}\"key\": {e.key}");
            foreach (var c in children)
            {
                var ce = s_Entities[c.Value];
                sb.AppendLine(",");
                sb.Append($"{indent}\"{ce.name.str}\": ");
                ToChildrenJSON(sb, ce, indent + "\t", visited);
            }
            sb.Append($"\n{indent[0..^1]}}}");
        }
        else if (s_Values.TryGetValue(e, out var value))
        {
            sb.Append(value.ToString(JSON: true));
        }
        else
        {
            sb.Append(e.key);
        }
    }

    static bool TryParse<T>(string expression, out T result)
    {
        expression = expression.Replace(',', '.');
        expression = expression.TrimEnd('f');
        expression = expression.ToLowerInvariant();

        bool success = false;
        result = default;
        if (typeof(T) == typeof(float))
        {
            if (expression == "pi")
            {
                success = true;
                result = (T)(object)(float)Math.PI;
            }
            else
            {
                success = float.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                result = (T)(object)temp;
            }
        }
        else if (typeof(T) == typeof(int))
        {
            success = int.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
            result = (T)(object)temp;
        }
        else if (typeof(T) == typeof(uint))
        {
            success = uint.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
            result = (T)(object)temp;
        }
        else if (typeof(T) == typeof(double))
        {
            if (expression == "pi")
            {
                success = true;
                result = (T)(object)Math.PI;
            }
            else
            {
                success = double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                result = (T)(object)temp;
            }
        }
        else if (typeof(T) == typeof(long))
        {
            success = long.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
            result = (T)(object)temp;
        }
        else if (typeof(T) == typeof(ulong))
        {
            success = ulong.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
            result = (T)(object)temp;
        }
        return success;
    }
}
