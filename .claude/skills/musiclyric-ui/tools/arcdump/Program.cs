// Prints the public API of TKCode.Arc types from the referenced package: arcdump <TypeName>... | arcdump --find <text>...
using System.Reflection;
var asms = Directory.GetFiles(AppContext.BaseDirectory, "TKCode.Arc*.dll").Select(Assembly.LoadFrom).ToArray();
var all = asms.Distinct().SelectMany(a => { try { return a.GetExportedTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; } }).ToList();
string N(System.Type t) => t.IsGenericType ? t.Name.Split('`')[0] + "<" + string.Join(",", t.GetGenericArguments().Select(N)) + ">" : t.Name;
if (args[0]=="--find") { foreach (var t in all.Where(t=>args.Skip(1).Any(k=>t!.FullName!.Contains(k)))) Console.WriteLine(t!.FullName); return; }
foreach (var name in args) {
  foreach (var t in all.Where(t => t!.Name == name)) {
    Console.WriteLine($"## {t!.FullName} : {t.BaseType?.Name}");
    if (t.IsEnum) { Console.WriteLine("  enum: " + string.Join(", ", Enum.GetNames(t))); continue; }
    const BindingFlags F = BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly;
    foreach (var p in t.GetProperties(F).Where(p => !p.Name.EndsWith("Property"))) Console.WriteLine($"  P {N(p.PropertyType)} {p.Name}{(p.CanWrite && p.SetMethod!.IsPublic ? "" : " (ro)")}");
    foreach (var e in t.GetEvents(F)) Console.WriteLine($"  E {N(e.EventHandlerType!)} {e.Name}");
    foreach (var m in t.GetMethods(F).Where(m => !m.IsSpecialName && m.DeclaringType == t)) Console.WriteLine($"  M {(m.IsStatic?"static ":"")}{N(m.ReturnType)} {m.Name}({string.Join(", ", m.GetParameters().Select(p => N(p.ParameterType)+" "+p.Name+(p.HasDefaultValue?"="+(p.DefaultValue??"null"):"")))})");
    foreach (var c in t.GetConstructors()) Console.WriteLine($"  ctor({string.Join(", ", c.GetParameters().Select(p => N(p.ParameterType)+" "+p.Name))})");
  }
}
