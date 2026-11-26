using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Godot;

namespace MapRewind;

[HarmonyPatch(typeof(AbstractRoom), "Enter")]
public static class RewindPatches
{
    // 用 IRunState 接收参数，直接拿到玩家坐标
    [HarmonyPrefix]
    public static void Prefix(IRunState runState)
    {
        if (runState == null) return;

        try
        {
            // 拿到当前坐标
            var mapCoord = runState.CurrentMapCoord;
            int col = mapCoord.HasValue ? mapCoord.Value.col : 0;
            int row = mapCoord.HasValue ? mapCoord.Value.row : 0;
            int actIndex = runState.CurrentActIndex;

            // 存快照
            RewindManager.CaptureSnapshot(actIndex, col, row);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 抓取坐标失败: {ex.Message}");
        }
    }
}