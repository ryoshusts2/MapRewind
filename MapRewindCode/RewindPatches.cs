using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves; // <--- 核心修复：补上这个
using Godot;

namespace MapRewind;

[HarmonyPatch(typeof(NMapScreen), "_Ready")]
public static class RewindPatches
{
    [HarmonyPostfix]
    public static void Postfix(NMapScreen __instance)
    {
        try
        {
            if (RunManager.Instance == null) return;

            SerializableRun snapshot = RunManager.Instance.ToSave(null);
            if (snapshot != null)
            {
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