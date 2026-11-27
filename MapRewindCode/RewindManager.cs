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
                GD.Print($"[MapRewind] 成功记录地图快照: {nodeName}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 记录快照失败: {ex.Message}");
        }
    }

    public static async Task RewindTo(int index)
    {
        if (index < 0 || index >= History.Count) return;
        var data = History[index];
        if (data.RunData == null || NGame.Instance == null) return;

        try
        {
            GD.Print($"[MapRewind] 准备回溯到: {data.NodeName}");

            // ================= 核心修复 =================
            // 1. 找到当前游戏的主场景（NRun）并彻底销毁它！
            var sceneTree = Engine.GetMainLoop() as SceneTree;
            if (sceneTree != null && sceneTree.Root != null)
            {
                // 原版的主场景节点通常叫 "NRun"
                var currentRunScene = sceneTree.Root.GetNodeOrNull("NRun"); 
                if (currentRunScene != null)
                {
                    GD.Print("[MapRewind] 检测到当前有跑团场景，正在强行拆除...");
                    currentRunScene.QueueFree();
                }
            }

            // 2. 等待 200 毫秒，给引擎充足的帧数去释放旧内存和单例
            await Task.Delay(200);

            // 3. 现在场景树是干净的，再让原版去加载新的场景
            RunState runState = RunState.FromSerializable(data.RunData);
            if (runState == null) return;

            await NGame.Instance.LoadRun(runState, null);
            
            GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
}