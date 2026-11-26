using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Godot;

namespace MapRewind;

// 拦截所有房间的基类：AbstractRoom 的 Enter 方法
[HarmonyPatch(typeof(AbstractRoom), "Enter")]
public static class RewindPatches
{
    [HarmonyPrefix]
    public static void Prefix(AbstractRoom __instance, RunState state, bool isRestoringRoomStackBase)
    {
        // isRestoringRoomStackBase 为 true 代表这是读档/回溯恢复的过程，防止死循环
        if (!isRestoringRoomStackBase)
        {
            string coordKey = $"Room_{System.DateTime.Now.Ticks}";
            
            // 记录快照
            RewindManager.TakeSnapshot(coordKey);
            
            GD.Print($"[MapRewind] 补丁触发，进入房间 {__instance.GetType().Name}，自动保存快照: {coordKey}");
        }
    }
}