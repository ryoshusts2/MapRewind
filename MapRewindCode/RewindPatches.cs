using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using Godot;

namespace MapRewind;

// 彻底放弃进房间存快照，改成每次回到大地图界面时存
[HarmonyPatch(typeof(NMapScreen), "_Ready")]
public static class RewindPatches
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        try
        {
            if (RunManager.Instance == null) return;
            
            // 延迟执行，等大地图UI完全加载完
            Callable.From(() => {
                var runState = RunManager.Instance.DebugOnlyGetState();
                if (runState == null) return;

                var mapCoord = runState.CurrentMapCoord;
                int col = mapCoord.HasValue ? mapCoord.Value.col : 0;
                int row = mapCoord.HasValue ? mapCoord.Value.row : 0;
                int actIndex = runState.CurrentActIndex;

                // 此时传 null，保存的是纯纯的、无战斗的干净地图状态
                RewindManager.CaptureSnapshot(actIndex, col, row);
            }).CallDeferred();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 抓取地图失败: {ex.Message}");
        }
    }
}