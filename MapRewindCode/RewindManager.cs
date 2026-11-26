using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using Godot;

namespace MapRewind
{
    public static class RewindManager
    {
        public static Dictionary<string, SerializableRun> Snapshots = new Dictionary<string, SerializableRun>();

        public static void TakeSnapshot(string coordKey)
        {
            if (RunManager.Instance == null) return;
            
            try
            {
                SerializableRun snapshot = RunManager.Instance.ToSave(null);
                if (snapshot != null)
                {
                    Snapshots[coordKey] = snapshot;
                    GD.Print("[MapRewind] 成功记录节点快照: " + coordKey);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr("[MapRewind] 记录快照失败: " + ex.Message);
            }
        }

        public static async Task RewindTo(string coordKey)
        {
            if (!Snapshots.ContainsKey(coordKey)) return;
            
            if (NGame.Instance == null)
            {
                GD.PrintErr("[MapRewind] NGame.Instance 为空，无法加载");
                return;
            }

            try
            {
                SerializableRun save = Snapshots[coordKey];
                RunState runState = RunState.FromSerializable(save);
                await NGame.Instance.LoadRun(runState, save.PreFinishedRoom);
                GD.Print("[MapRewind] 成功回溯到节点: " + coordKey);
            }
            catch (Exception ex)
            {
                GD.PrintErr("[MapRewind] 回溯失败: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}