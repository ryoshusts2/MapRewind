using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Combat; // 引入战斗管理器
using Godot;

namespace MapRewind;

[HarmonyPatch(typeof(AbstractRoom), "Enter")]
public static class RewindPatches
{
    // 【核心修复】改成 Postfix，等房间加载完，坐标100%刷新的瞬间再抓快照！
    [HarmonyPostfix]
    public static void Postfix(AbstractRoom __instance)
    {
        try
        {
            if (RunManager.Instance == null) return;

            // 如果现在处于战斗中，也可以正常存（存下的是战斗刚开始的初始状态）
            SerializableRun snapshot = RunManager.Instance.ToSave(__instance);
            
            if (snapshot != null)
            {
                int actIndex = snapshot.CurrentActIndex;
                string roomType = __instance.GetType().Name.Replace("Room", "");
                string nodeName = $"第 {actIndex + 1} 层 - {roomType}";

                // 防止在同一个房间疯狂存（比如读档重载的时候）
                if (RewindManager.History.Count > 0)
                {
                    var last = RewindManager.History[^1];
                    if (last.NodeName == nodeName && last.ActIndex == actIndex) return;
                }

                RewindManager.History.Add(new RewindManager.SnapshotData
                {
                    NodeName = nodeName,
                    RunData = snapshot,
                    ActIndex = actIndex
                });

                GD.Print($"[MapRewind] 成功记录节点快照: {nodeName}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 记录快照失败: {ex.Message}");
        }
    }
}