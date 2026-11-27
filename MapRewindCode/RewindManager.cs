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

        // 1. 创建全屏黑底和正中心的诗句
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
            label.AddThemeFontSizeOverride("font_size", 64); 
            label.AddThemeColorOverride("font_color", new Color(0.85f, 0.8f, 0.75f));
            label.Modulate = new Color(1, 1, 1, 0); 
            vbox.AddChild(label);
            labels.Add(label);
        }
        
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
            
            // 让玩家把诗看完，同时给引擎 3.5 秒时间，让旧场景的动画、特效、UI协程自然停滞
            await Task.Delay(3500);

            GD.Print("[MapRewind] 正在执行原版 LoadRun...");
            RunState runState = RunState.FromSerializable(data.RunData);
            
            if (runState != null)
            {
                // 【核心修复】不要自己去拆场景。让原版自己处理。
                // 就算原版因为旧战斗残留报了空指针错误，协程被打断，只要新场景生成出来就行。
                try { await NGame.Instance.LoadRun(runState, null); } catch { }
                
                // 【补漏】如果原版被打断了，新场景没有生成，我们强杀旧的，手动重新生成！
                var rootScene = NGame.Instance.RootSceneContainer;
                if (rootScene != null)
                {
                    // 等待一帧检查场景是否已经被替换
                    await NGame.Instance.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    if (rootScene.CurrentScene == null || !rootScene.CurrentScene.Name.ToString().Contains("Run"))
                    {
                        GD.Print("[MapRewind] 原版加载被打断，正在强制手动重建场景...");
                        var newRunNode = NRun.Create(runState);
                        if (newRunNode != null)
                        {
                            // 直接强行替换，不用 RemoveChild，让引擎自己去吞掉旧场景
                            rootScene.SetCurrentScene(newRunNode);
                            await RunManager.Instance.GenerateMap();
                            await RunManager.Instance.LoadIntoLatestMapCoord(null);
                        }
                    }
                }
                GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯异常: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            // 给新地图一点时间渲染出来
            await Task.Delay(2000);
            
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