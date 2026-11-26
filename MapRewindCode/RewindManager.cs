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
        public string NodeName;
        public SerializableRun RunData;
        public int ActIndex;
    }

    public static List<SnapshotData> History = new();

    public static async Task RewindTo(int index)
    {
        if (index < 0 || index >= History.Count) return;
        if (NGame.Instance == null) return;

        try
        {
            var data = History[index];
            GD.Print($"[MapRewind] 准备回溯到: {data.NodeName}");
            
            // 从存档数据反序列化
            RunState runState = RunState.FromSerializable(data.RunData);
            if (runState == null) {
                GD.PrintErr("[MapRewind] 回溯失败：序列化数据为空！");
                return;
            }

            // 执行底层加载（原版重武器）
            await NGame.Instance.LoadRun(runState, data.RunData.PreFinishedRoom);
            GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯过程发生异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
}