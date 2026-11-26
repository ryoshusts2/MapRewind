using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes; // NGame 所在的命名空间，可能需要根据实际报错调整
using Godot;

namespace MapRewind;

public static class RewindManager
{
    // 字典：存历史快照，Key 用坐标字符串（比如 "Act0_X3_Y2"），Value 是数据包
    public static Dictionary<string, SerializableRun> Snapshots = new();

    /// <summary>
    /// 抓取当前游戏状态，存入字典
    /// </summary>
    public static void TakeSnapshot(string coordKey)
    {
        if (RunManager.Instance == null) return;

        try
        {
            // 调用原版的 ToSave 方法，直接打包当前状态
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

    /// <summary>
    /// 触发时光回溯（加载历史快照）
    /// </summary>
    public static async Task RewindTo(string coordKey)
    {
        if (!Snapshots.ContainsKey(coordKey))
        {
            GD.PrintErr($"[MapRewind] 找不到节点 {coordKey} 的快照！");
            return;
        }

        try
        {
            SerializableRun save = Snapshots[coordKey];
            
            // 1. 将数据包反序列化为游戏状态
            RunState runState = RunState.FromSerializable(save);
            
            // 2. 调用游戏引擎的 LoadRun 方法，刷新整个游戏场景
            // 注意：NGame.Instance 可能要等游戏完全加载后才能用
            await NGame.Instance.LoadRun(runState, save.PreFinishedRoom);
            
            GD.Print($"[MapRewind] 成功回溯到节点: {coordKey}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯失败: {ex.Message}\n{ex.StackTrace}");
        }
    }
}