using System;
using System.Reflection;
using Microsoft.Extensions.AI;

namespace InspectTypes
{
    class Program
    {
        static void Main(string[] args)
        {
            InspectInterface();
            InspectMetadata();
        }

        static void InspectInterface()
        {
            var iType = typeof(IChatClient);
            Console.WriteLine("=== IChatClient Interface ===");
            Console.WriteLine($"Namespace: {iType.Namespace}");
            Console.WriteLine($"FullName: {iType.FullName}");

            Console.WriteLine("\nProperties:");
            foreach (var prop in iType.GetProperties())
            {
                Console.WriteLine($"  {prop.Name}: {prop.PropertyType.Name}");
            }

            Console.WriteLine("\nMethods (Public Instance):");
            foreach (var method in iType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase))
            {
                if (!method.IsSpecialName)
                {
                    var @params = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Console.WriteLine($"  {method.ReturnType.Name} {method.Name}({@params})");
                }
            }
        }

        static void InspectMetadata()
        {
            try
            {
                var type = Type.GetType("Microsoft.Extensions.AI.ChatClientMetadata, Microsoft.Extensions.AI.Abstractions");
                if (type != null)
                {
                    Console.WriteLine("\n=== ChatClientMetadata Class ===");
                    Console.WriteLine($"Type: {type.FullName}");

                    Console.WriteLine("\nProperties:");
                    foreach (var prop in type.GetProperties())
                    {
                        Console.WriteLine($"  {prop.Name}: {prop.PropertyType.Name} (Can Read: {prop.CanRead}, Can Write: {prop.CanWrite})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inspecting ChatClientMetadata: {ex.Message}");
            }
        }
    }
}
