using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using Godot;

namespace MapRewind;

// 拦截所有房间的基类：AbstractRoom 的 Enter 方法
[HarmonyPatch(typeof(AbstractRoom), "Enter")]
public static class RewindPatches
{
    // 因为原版方法是 async Task，且参数对不上，我们直接写无参 Prefix，Harmony 默认允许
    // 通过 __instance 拿到当前房间，只判断类型，不要参数
    [HarmonyPrefix]
    public static void Prefix(AbstractRoom __instance)
    {
        // 跳过正在加载存档/回溯恢复过程中的房间进入
        // 这里只拦截普通的探索进入
        string coordKey = $"Room_{System.DateTime.Now.Ticks}";
        
        // 记录快照
        RewindManager.TakeSnapshot(coordKey);
        
        GD.Print($"[MapRewind] 补丁触发，进入房间 {__instance.GetType().Name}，自动保存快照: {coordKey}");
    }
}