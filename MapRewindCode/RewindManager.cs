using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
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

    public static void CaptureSnapshot(AbstractRoom room, int actIndex, int col, int row)
    {
        if (RunManager.Instance == null) return;
        try
        {
            // 把 room 传进去，这样 PreFinishedRoom 就有值了
            SerializableRun snapshot = RunManager.Instance.ToSave(room);
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

            // 【核心修复】等待一帧，让旧战斗界面彻底销毁释放，防止引擎场景树报错
            await ToSignal(Engine.GetMainLoop(), "process_frame");
            await Task.Delay(100);

            RunState runState = RunState.FromSerializable(data.RunData);
            if (runState == null) 
            {
                GD.PrintErr("[MapRewind] 回溯失败：反序列化 RunState 为空！");
                return;
            }

            // 此时 PreFinishedRoom 有值了，原版就不会在空指针里迷路了
            await NGame.Instance.LoadRun(runState, data.RunData.PreFinishedRoom);
            
            GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
}