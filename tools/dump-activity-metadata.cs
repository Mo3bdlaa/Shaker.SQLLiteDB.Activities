using System.Activities;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

var asm = typeof(Shaker.SQLLiteDB.Activities.Core.SQLiteConnectionHandle).Assembly;
var list = new List<object>();
foreach (var t in asm.GetExportedTypes().Where(x => typeof(Activity).IsAssignableFrom(x) && !x.IsAbstract).OrderBy(x => x.FullName))
{
    var props = new List<object>();
    var seen = new HashSet<string>();
    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        if (p.GetSetMethod() == null) continue;
        if (p.Name is "DisplayName" or "Implementation" or "CacheId" or "Id" or "Body") continue;
        if (!seen.Add(p.Name)) continue;
        var pt = p.PropertyType;
        string kind = "plain", inner;
        if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(InArgument<>)) { kind = "in"; inner = Name(pt.GetGenericArguments()[0]); }
        else if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(OutArgument<>)) { kind = "out"; inner = Name(pt.GetGenericArguments()[0]); }
        else if (typeof(Argument).IsAssignableFrom(pt) || typeof(Activity).IsAssignableFrom(pt)) continue;
        else inner = Name(pt);
        props.Add(new
        {
            name = p.Name,
            kind,
            type = inner,
            isEnum = (kind == "plain") && pt.IsEnum,
            isBool = (kind == "plain" ? pt == typeof(bool) : inner == "bool"),
            category = p.GetCustomAttribute<CategoryAttribute>()?.Category ?? "",
            display = p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? "",
            tooltip = p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
            required = p.GetCustomAttributes().Any(a => a.GetType().Name == "RequiredArgumentAttribute"),
        });
    }
    list.Add(new
    {
        name = t.Name,
        ns = t.Namespace,
        display = t.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName,
        group = t.GetCustomAttribute<CategoryAttribute>()?.Category,
        props,
    });
}
File.WriteAllText(args[0], JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"{list.Count} activities");

static string Name(Type t)
{
    if (t == typeof(string)) return "string";
    if (t == typeof(int)) return "int";
    if (t == typeof(long)) return "long";
    if (t == typeof(bool)) return "bool";
    if (t == typeof(double)) return "double";
    if (t == typeof(object)) return "object";
    if (!t.IsGenericType) return t.FullName.Replace('+', '.');
    return t.GetGenericTypeDefinition().FullName.Split('`')[0].Replace('+', '.') + "<" + string.Join(", ", t.GetGenericArguments().Select(Name)) + ">";
}
