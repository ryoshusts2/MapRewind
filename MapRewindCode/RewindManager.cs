using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
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

        // 1. 创建黑底和绝对居中的诗句
        var canvasLayer = new CanvasLayer();
        var overlay = new ColorRect();
        overlay.Color = new Color(0.05f, 0.02f, 0.02f, 1.0f); 
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        canvasLayer.AddChild(overlay);

        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(centerContainer);

        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        centerContainer.AddChild(vbox);

        string[] poems = {
            "落魄谷中寒风吹，春秋蝉鸣少年归。",
            "荡魂山处石人泪，定仙游走魔向北。",
            "逆流河上万仙退，爱情不敌坚持泪。",
            "宿命天成命中败，仙尊悔而我不悔。"
        };

        var labels = new List<Label>();
        foreach (var line in poems)
        {
            var label = new Label();
            label.Text = line;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 64); // 字体加大到 64
            label.AddThemeColorOverride("font_color", new Color(0.85f, 0.8f, 0.75f));
            label.Modulate = new Color(1, 1, 1, 0); 
            vbox.AddChild(label);
            labels.Add(label);
        }
        
        // 挂载到 Godot 的最底层根节点，防止被强拆
        var tree = Engine.GetMainLoop() as SceneTree;
        tree?.Root.AddChild(canvasLayer);

        var tween = NGame.Instance.CreateTween();
        for (int i = 0; i < labels.Count; i++)
        {
            tween.TweenInterval(0.6f);
            tween.TweenProperty(labels[i], "modulate:a", 1.0f, 0.8f);
        }

        await NGame.Instance.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        try
        {
            GD.Print($"[MapRewind] 准备回溯到: {data.NodeName}");
            
            // 让玩家把诗看完，给底层 3 秒时间
            await Task.Delay(3000);

            GD.Print("[MapRewind] 正在强制清理旧场景...");
            // 【核心突破】彻底把旧的战斗/地图界面从底层拔除
            var rootScene = NGame.Instance.RootSceneContainer;
            if (rootScene != null && rootScene.CurrentScene != null)
            {
                var currentScene = rootScene.CurrentScene;
                if (GodotObject.IsInstanceValid(currentScene))
                {
                    // 两个连招：移除并释放
                    currentScene.GetParent().RemoveChild(currentScene);
                    currentScene.QueueFree();
                }
            }

            // 等待一帧，确保 Godot 引擎清理完毕
            await NGame.Instance.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            GD.Print("[MapRewind] 正在生成新场景...");
            RunState runState = RunState.FromSerializable(data.RunData);
            
            if (runState != null)
            {
                // 【核心突破】不调原版的 LoadRun，直接复刻它的源码逻辑
                var newRunNode = NRun.Create(runState);
                if (newRunNode != null)
                {
                    rootScene?.SetCurrentScene(newRunNode);
                    await RunManager.Instance.GenerateMap();
                    
                    // 把玩家放到地图的当前坐标，而不是刚才半死不活的房间
                    await RunManager.Instance.LoadIntoLatestMapCoord(null);
                    
                    GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
                }
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯异常: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            await Task.Delay(1500);
            
            if (GodotObject.IsInstanceValid(overlay))
            {
                var fadeOutTween = overlay.CreateTween();
                fadeOutTween.TweenProperty(overlay, "color:a", 0.0f, 1.5f);
                await overlay.ToSignal(fadeOutTween, Tween.SignalName.Finished);
            }
            
            if (GodotObject.IsInstanceValid(canvasLayer))
            {
                canvasLayer.QueueFree();
            }
        }
    }
}