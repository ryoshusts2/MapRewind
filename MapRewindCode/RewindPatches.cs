using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using Godot;

namespace MapRewind;

[HarmonyPatch(typeof(AbstractRoom), "Enter")]
public static class RewindPatches
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        // 每次进入新房间，呼叫重写后的抓取逻辑
        RewindManager.CaptureSnapshot();
    }
}