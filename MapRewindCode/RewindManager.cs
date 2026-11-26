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
        if (data.RunData == null) return;

        try
        {
            GD.Print($"[MapRewind] 准备回溯到: {data.NodeName}");

            // 删掉 IsInProgress 判断，直接尝试强杀 UI 节点
            var sceneTree = Engine.GetMainLoop() as SceneTree;
            if (sceneTree != null && sceneTree.Root != null)
            {
                var combatRoom = sceneTree.Root.FindChild("NCombatRoom", true, false);
                if (combatRoom != null)
                {
                    combatRoom.QueueFree(); // 摧毁战斗场景
                }
            }

            // 原版读档，里面自带 SetCurrentScene，理论上会替换掉所有东西
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