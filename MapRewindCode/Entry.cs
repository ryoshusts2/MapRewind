using System;
using System.Reflection;
using HarmonyLib;
using Godot;
using STS2RitsuLib;

namespace MapRewind;

public static class Entry
{
    public const string ModId = "MapRewind";
    public static readonly string ResPath = $"res://{ModId}";

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        GD.Print("[MapRewind] Initializing...");

        try
        {
            // 传 null 即可
            RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, null);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MapRewind] EnsureGodotScriptsRegistered failed: {ex.Message}");
        }

        try
        {
            var harmony = new Harmony(ModId);
            harmony.PatchAll(assembly);
            GD.Print("[MapRewind] Harmony patches applied.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MapRewind] Harmony patch failed: {ex.Message}\n{ex.StackTrace}");
        }

        GD.Print("[MapRewind] initialized.");
    }
}