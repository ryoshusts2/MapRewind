using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Combat; // 核心战斗命名空间
using Godot;

namespace MapRewind;

public static class RewindManager
{
    public class SnapshotData
    {
        public string NodeName = string.Empty;
        public SerializableRun? RunData;
        public int ActIndex;
    }

    public static List<SnapshotData> History = new();

    public static async Task RewindTo(int index)
    {
        if (index < 0 || index >= History.Count) return;
        if (NGame.Instance == null) return;

        var data = History[index];
        if (data.RunData == null) return;

        try
        {
            GD.Print($"[MapRewind] 准备回溯到: {data.NodeName}");

            // 【核心修复】如果正在战斗中，强行结束/打断当前战斗状态
            if (CombatManager.Instance != null && CombatManager.Instance.IsInProgress)
            {
                GD.Print("[MapRewind] 检测到战斗中回溯，正在强行结束当前战斗...");
                // 原版结束战斗的方法，强行让状态机退出
                CombatManager.Instance.IsInProgress = false; 
            }

            // 清理可能会卡住的 UI 节点 (比如顶部血条、战斗界面)
            var sceneTree = Engine.GetMainLoop() as SceneTree;
            if (sceneTree != null && sceneTree.Root != null)
            {
                var combatRoom = sceneTree.Root.FindChild("NCombatRoom", true, false);
                if (combatRoom != null)
                {
                    combatRoom.QueueFree(); // 暴力销毁战斗场景
                }
            }

            // 执行底层读档
            RunState runState = RunState.FromSerializable(data.RunData);
            await NGame.Instance.LoadRun(runState, data.RunData.PreFinishedRoom);
            GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯过程发生异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
}