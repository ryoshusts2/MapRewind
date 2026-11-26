using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Godot;

namespace MapRewind;

[HarmonyPatch(typeof(RunManager), "OnRoomEntered")]
public static class RewindPatches
{
    [HarmonyPrefix]
    public static void Prefix(AbstractRoom room)
    {
        // 每次进新房间，强行抓取一次快照！
        // 用当前时间戳作为坐标Key，确保唯一
        string coordKey = $"Room_{System.DateTime.Now.Ticks}";
        
        RewindManager.TakeSnapshot(coordKey);
        GD.Print($"[MapRewind] 补丁触发，进入房间，自动保存快照: {coordKey}");
    }
}