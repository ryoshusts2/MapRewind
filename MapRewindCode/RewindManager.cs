using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves.Migrations.SerializableRuns; // <--- 改了这里
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using Godot;

namespace MapRewind;

public static class RewindManager
{
    public static Dictionary<string, SerializableRun> Snapshots = new();

    public static void TakeSnapshot(string coordKey)
    {
        if (RunManager.Instance == null) return;
        try
        {
            SerializableRun snapshot = RunManager.Instance.ToSave(null);
            if (snapshot != null)
            {
                Snapshots[coordKey] = snapshot;
                GD.Print($"[MapRewind] 成功记录节点快照: {coordKey}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 记录快照失败: {ex.Message}");
        }
    }

    public static async Task RewindTo(string coordKey)
    {
        if (!Snapshots.ContainsKey(coordKey)) return;
        try
        {
            SerializableRun save = Snapshots[coordKey];
            RunState runState = RunState.FromSerializable(save);
            await NGame.Instance.LoadRun(runState, save.PreFinishedRoom);
            GD.Print($"[MapRewind] 成功回溯到节点: {coordKey}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯失败: {ex.Message}\n{ex.StackTrace}");
        }
    }
}