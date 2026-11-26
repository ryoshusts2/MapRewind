using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Godot;

namespace MapRewind;

[HarmonyPatch(typeof(AbstractRoom), "Enter")]
public static class RewindPatches
{
    [HarmonyPrefix]
    public static void Prefix(AbstractRoom __instance)
    {
        try
        {
            if (RunManager.Instance == null) return;

            // 【核心修复】把当前房间传进去，保留房间信息
            SerializableRun snapshot = RunManager.Instance.ToSave(__instance);
            
            if (snapshot != null)
            {
                int actIndex = snapshot.CurrentActIndex;
                string roomType = __instance.GetType().Name.Replace("Room", "");
                string nodeName = $"第 {actIndex + 1} 层 - {roomType}";

                RewindManager.History.Add(new RewindManager.SnapshotData
                {
                    NodeName = nodeName,
                    RunData = snapshot,
                    ActIndex = actIndex
                });

                GD.Print($"[MapRewind] 成功记录快照: {nodeName}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 记录快照失败: {ex.Message}");
        }
    }
}