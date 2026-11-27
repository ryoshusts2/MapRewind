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

    public static void CaptureSnapshot(int actIndex, int col, int row)
    {
        if (RunManager.Instance == null) return;
        try
        {
            // 使用传 null 的纯净地图存档
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

            // ================= 融合思路：先彻底清场，再重开 =================
            // 1. 获取当前游戏的主场景树根节点（RootSceneContainer）
            var rootScene = NGame.Instance.RootSceneContainer;
            if (rootScene != null && rootScene.CurrentScene != null)
            {
                GD.Print("[MapRewind] 正在强行销毁当前游戏主场景...");
                var current = rootScene.CurrentScene;
                // 直接把当前跑团节点移出场景树，切断它所有的后台运行逻辑
                current.GetParent().RemoveChild(current);
                current.QueueFree();
            }

            // 2. 等待 200 毫秒，让 Godot 引擎彻底清理废旧的垃圾内存和渲染资源
            await Task.Delay(200);

            // 3. 反序列化数据
            RunState runState = RunState.FromSerializable(data.RunData);
            if (runState == null) return;

            // 4. 调用原版读档（它会全新创建一个跑团界面，并且状态与原节点完全一致）
            await NGame.Instance.LoadRun(runState, null);

            GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
}