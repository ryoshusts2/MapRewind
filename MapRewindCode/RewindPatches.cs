using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map; // 引入地图界面的命名空间
using MegaCrit.Sts2.Core.Runs;
using Godot;

namespace MapRewind;

// 拦截大地图界面的打开
[HarmonyPatch(typeof(NMapScreen), "_Ready")]
public static class RewindPatches
{
    [HarmonyPostfix]
    public static void Postfix(NMapScreen __instance)
    {
        try
        {
            if (RunManager.Instance == null) return;

            // 当玩家站在大地图上时，抓取安全快照
            SerializableRun snapshot = RunManager.Instance.ToSave(null);
            if (snapshot != null)
            {
                // 获取层数
                int actIndex = snapshot.CurrentActIndex;
                string nodeName = $"第 {actIndex + 1} 层 - 大地图";

                RewindManager.History.Add(new RewindManager.SnapshotData
                {
                    NodeName = nodeName,
                    RunData = snapshot,
                    ActIndex = actIndex
                });

                GD.Print($"[MapRewind] 在大地图安全记录快照: {nodeName}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 记录大地图快照失败: {ex.Message}");
        }
    }
}