using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using Godot;

namespace MapRewind;

public static class RewindManager
{
    public class SnapshotData
    {
        public string NodeName = string.Empty;
        public SerializableRun? RunData;
        public int ActIndex;
        public int Col;
        public int Row;
    }

    public static List<SnapshotData> History = new();

    public static void CaptureSnapshot(int actIndex, int col, int row)
    {
        if (RunManager.Instance == null) return;
        try
        {
            SerializableRun snapshot = RunManager.Instance.ToSave(null);
            if (snapshot != null)
            {
                string nodeName = $"第 {actIndex + 1} 层 - 坐标({col}, {row})";

                if (History.Count > 0)
                {
                    var last = History[^1];
                    if (last.Col == col && last.Row == row && last.ActIndex == actIndex) return;
                }

                History.Add(new SnapshotData
                {
                    NodeName = nodeName,
                    RunData = snapshot,
                    ActIndex = actIndex,
                    Col = col,
                    Row = row
                });
                GD.Print($"[MapRewind] 成功记录: {nodeName}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 记录快照失败: {ex.Message}");
        }
    }

    public static void RewindTo(int index)
    {
        if (index < 0 || index >= History.Count) return;
        var data = History[index];
        if (data.RunData == null) return;

        GD.Print($"[MapRewind] 准备回溯到: {data.NodeName}");

        // 【核心修复】在清理场景前，先等待一帧，防止 UI 节点锁死
        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree != null)
        {
            // 延迟执行 DoRewind
            tree.ProcessFrame += () => DoRewind(data);
        }
    }

    private static async void DoRewind(SnapshotData data)
    {
        try
        {
            if (data.RunData == null || NGame.Instance == null) return;

            // 强行销毁当前关卡残留的根节点
            var sceneTree = Engine.GetMainLoop() as SceneTree;
            if (sceneTree != null && sceneTree.Root != null)
            {
                var combatRoom = sceneTree.Root.FindChild("NCombatRoom", true, false);
                if (combatRoom != null) combatRoom.QueueFree();

                var mapScreen = sceneTree.Root.FindChild("NMapScreen", true, false);
                if (mapScreen != null) mapScreen.QueueFree();
            }

            RunState runState = RunState.FromSerializable(data.RunData);
            
            // 执行原版读档
            await NGame.Instance.LoadRun(runState, data.RunData.PreFinishedRoom);
            
            GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
}