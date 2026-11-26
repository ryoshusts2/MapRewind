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
    }

    public static List<SnapshotData> History = new();

    public static async Task RewindTo(int index)
    {
        if (index < 0 || index >= History.Count) return;
        if (NGame.Instance == null) return;

        var data = History[index];
        if (data.RunData == null)
        {
            GD.PrintErr("[MapRewind] 回溯失败：快照数据为空！");
            return;
        }

        try
        {
            GD.Print($"[MapRewind] 准备回溯到: {data.NodeName}");
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