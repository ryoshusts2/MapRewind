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

    public static void CaptureSnapshot()
    {
        if (RunManager.Instance == null) return;
        try
        {
            var runState = RunManager.Instance.CurrentRunState;
            SerializableRun snapshot = RunManager.Instance.ToSave(null);
            
            if (snapshot != null && runState.CurrentMapCoord.HasValue)
            {
                var coord = runState.CurrentMapCoord.Value;
                int actIndex = snapshot.CurrentActIndex;
                string nodeName = $"第 {actIndex + 1} 层";

                // 防止重复存
                if (History.Count > 0)
                {
                    var last = History[^1];
                    if (last.Col == coord.col && last.Row == coord.row && last.ActIndex == actIndex) return;
                }

                History.Add(new SnapshotData
                {
                    NodeName = nodeName,
                    RunData = snapshot,
                    ActIndex = actIndex,
                    Col = coord.col,
                    Row = coord.row
                });
                GD.Print($"[MapRewind] 成功记录节点: {nodeName}, 坐标 ({coord.col}, {coord.row})");
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
        if (NGame.Instance == null) return;

        var data = History[index];
        if (data.RunData == null) return;

        try
        {
            GD.Print($"[MapRewind] 准备回溯到: {data.NodeName} ({data.Col}, {data.Row})");

            // 【核心修复】不在这里 QueueFree 任何 UI 节点。直接让原版场景树去接管清理！
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