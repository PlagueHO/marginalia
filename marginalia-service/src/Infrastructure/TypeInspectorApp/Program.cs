using System;
using System.Reflection;
using Microsoft.Extensions.AI;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   Extension Methods & Additional APIs                         ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

        ShowChatClientExtensions();
    }

    static void ShowChatClientExtensions()
    {
        Console.WriteLine("\n▶ ChatClientExtensions (static methods)");

        var extensionsType = typeof(ChatClientExtensions);
        var methods = extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        Console.WriteLine($"\n  Total Methods: {methods.Length}");
        Console.WriteLine("\n  GetService-related methods:");

        foreach (var m in methods.Where(x => x.Name.Contains("GetService")))
        {
            var @params = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name}"));
            if (@params.Length > 70) @params = @params.Substring(0, 70) + "...";
            Console.WriteLine($"    • {m.ReturnType.Name} {m.Name}({@params})");
        }

        Console.WriteLine("\n  GetResponse-related methods:");
        foreach (var m in methods.Where(x => x.Name.Contains("GetResponse")))
        {
            var @params = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name}"));
            if (@params.Length > 70) @params = @params.Substring(0, 70) + "...";
            Console.WriteLine($"    • {m.ReturnType.Name} {m.Name}({@params})");
        }
    }
}
