using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Combat;
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
            // 传 null，记录最纯粹的地图节点状态
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
                GD.Print($"[MapRewind] 成功记录节点: {nodeName}");
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

            // 【核心修复】如果正在战斗中，强行解除战斗状态，防止底层读档时跟战斗UI打架
            if (CombatManager.Instance != null && CombatManager.Instance.IsInProgress)
            {
                GD.Print("[MapRewind] 强行中断当前战斗状态...");
                // 直接调用底层原版的脱战方法（结束战斗），如果没有，就用反射强杀
                var combatManager = CombatManager.Instance;
                var prop = combatManager.GetType().GetProperty("IsInProgress");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(combatManager, false);
                }
            }

            // 延迟 300 毫秒让引擎彻底释放旧的战斗UI
            await Task.Delay(300);

            RunState runState = RunState.FromSerializable(data.RunData);
            if (runState == null) return;

            // 传 null，安全加载地图节点
            await NGame.Instance.LoadRun(runState, null);
            
            GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
}