using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Godot;

namespace MapRewind;

[HarmonyPatch(typeof(AbstractRoom), "Enter")]
public static class RewindPatches
{
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

            // 【核心修复】只存纯地图状态，绝不存半成品房间（传 null）
            RewindManager.CaptureSnapshot(actIndex, col, row);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 抓取坐标失败: {ex.Message}");
        }
    }
}