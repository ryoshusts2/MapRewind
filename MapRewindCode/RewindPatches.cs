using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using Godot;

namespace MapRewind;

[HarmonyPatch(typeof(AbstractRoom), "Enter")]
public static class RewindPatches
{
    [HarmonyPrefix]
    public static void Prefix(AbstractRoom __instance)
    {
        // 记录快照
        RewindManager.TakeSnapshot(__instance);
    }
}