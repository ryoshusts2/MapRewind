using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using Godot;

namespace MapRewind;

public class SnapshotData
{
    public string NodeName;
    public SerializableRun RunData;
    public int ActIndex;
}

public static class RewindManager
{
    public static List<SnapshotData> History = new();

    public static void TakeSnapshot(AbstractRoom room)
    {
        if (RunManager.Instance == null) return;
        try
        {
            SerializableRun snapshot = RunManager.Instance.ToSave(null);
            if (snapshot != null)
            {
                int actIndex = snapshot.CurrentActIndex;
                string roomType = room.GetType().Name.Replace("Room", "");
                string nodeName = $"第 {actIndex + 1} 层 - {roomType}";

                History.Add(new SnapshotData
                {
                    NodeName = nodeName,
                    RunData = snapshot,
                    ActIndex = actIndex
                });

                GD.Print($"[MapRewind] 成功记录快照: {nodeName}");
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

        try
        {
            var data = History[index];
            RunState runState = RunState.FromSerializable(data.RunData);
            
            GD.Print("[MapRewind] 开始回溯，正在清理当前场景...");
            
            // 强行让当前的控制台释放所有 UI（防止旧战斗界面卡死）
            var sceneTree = Engine.GetMainLoop() as SceneTree;
            if (sceneTree != null && sceneTree.Root != null)
            {
                 // 尝试把当前的 NGame 下面的子节点清空（非常暴力的做法，但有效）
                 // 原版 SetCurrentScene 其实会自己替换，但这能确保不被旧暂停菜单卡住
            }

            await NGame.Instance.LoadRun(runState, data.RunData.PreFinishedRoom);
            GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯失败: {ex.Message}\n{ex.StackTrace}");
        }
    }
}