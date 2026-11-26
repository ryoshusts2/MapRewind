using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MapRewind;

public static class Entry
{
    public const string ModId = "MapRewind";
    public static readonly string ResPath = $"res://{ModId}";
    public static readonly Logger Logger = new Logger(ModId);

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        try
        {
            var harmony = new Harmony(ModId);
            harmony.PatchAll(assembly);
            Logger.Info("MapRewind initialized.");
        }
        catch (System.Exception ex)
        {
            Logger.Error($"MapRewind Harmony patch failed: {ex.Message}");
        }
    }
}