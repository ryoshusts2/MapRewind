using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Godot;

namespace MapRewind;

[HarmonyPatch(typeof(NMapScreen), "_Ready")]
public static class RewindPatches
{
    [HarmonyPostfix]
    public static void Postfix(NMapScreen __instance)
    {
        // 延迟执行，等地图画面彻底加载完毕再抓快照
        __instance.CallDeferred("_DummyMethod"); // 占位，避免空方法报错
        Callable.From(() => CaptureSnapshot()).CallDeferred();
    }

    private static void CaptureSnapshot()
    {
        try
        {
            if (RunManager.Instance == null) return;

            SerializableRun snapshot = RunManager.Instance.ToSave(null); // 此时没有进入任何房间，传 null
            if (snapshot != null)
            {
                int actIndex = snapshot.CurrentActIndex;
                string nodeName = $"第 {actIndex + 1} 层 - 大地图选路";

                // 防止在一个地图重复存太多次
                if (RewindManager.History.Count > 0 && RewindManager.History[^1].NodeName == nodeName && RewindManager.History[^1].ActIndex == actIndex)
                {
                    // 如果刚存过，就不存了，直接返回
                    return; 
                }

                RewindManager.History.Add(new RewindManager.SnapshotData
                {
                    NodeName = nodeName,
                    RunData = snapshot,
                    ActIndex = actIndex
                });

                GD.Print($"[MapRewind] 成功记录大地图快照: {nodeName}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 记录快照失败: {ex.Message}");
        }
    }
}