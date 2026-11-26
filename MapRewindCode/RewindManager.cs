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

    // 去掉 room 参数
    public static void CaptureSnapshot(int actIndex, int col, int row)
    {
        if (RunManager.Instance == null) return;
        try
        {
            // 【核心修复】传 null。因为坐标已经更新，这样读档时会直接回到这个节点前的地图状态！
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

    public static async Task RewindTo(int index)
    {
        if (index < 0 || index >= History.Count) return;
        var data = History[index];
        if (data.RunData == null || NGame.Instance == null) return;

        try
        {
            GD.Print($"[MapRewind] 准备回溯到: {data.NodeName}");

            RunState runState = RunState.FromSerializable(data.RunData);
            if (runState == null) 
            {
                GD.PrintErr("[MapRewind] 回溯失败：反序列化 RunState 为空！");
                return;
            }

            // preFinishedRoom 传 null 表示回到地图界面
            await NGame.Instance.LoadRun(runState, null);
            
            GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
}