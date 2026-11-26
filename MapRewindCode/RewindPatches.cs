using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Godot;

namespace MapRewind;

[HarmonyPatch(typeof(AbstractRoom), "Enter")]
public static class RewindPatches
{
    // 【核心修复】改成 Postfix，此时坐标完美，且房间加载完毕
    [HarmonyPostfix]
    public static void Postfix(AbstractRoom __instance, IRunState runState)
    {
        if (__instance == null || runState == null) return;

        try
        {
            var mapCoord = runState.CurrentMapCoord;
            int col = mapCoord.HasValue ? mapCoord.Value.col : 0;
            int row = mapCoord.HasValue ? mapCoord.Value.row : 0;
            int actIndex = runState.CurrentActIndex;

            // 把当前房间 __instance 传进去！
            RewindManager.CaptureSnapshot(__instance, actIndex, col, row);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 抓取坐标失败: {ex.Message}");
        }
    }
}