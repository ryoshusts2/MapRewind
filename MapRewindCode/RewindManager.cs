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

        // 1. 挂载诗句 UI (绝对最高层级，防止被替换场景时误杀)
        var canvasLayer = new CanvasLayer();
        var overlay = new ColorRect();
        overlay.Color = new Color(0.05f, 0.02f, 0.02f, 1.0f); 
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        canvasLayer.AddChild(overlay);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.Center);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        
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
            label.AddThemeFontSizeOverride("font_size", 48); 
            label.AddThemeColorOverride("font_color", new Color(0.85f, 0.8f, 0.75f));
            label.Modulate = new Color(1, 1, 1, 0); 
            vbox.AddChild(label);
            labels.Add(label);
        }
        
        overlay.AddChild(vbox);
        
        var tree = Engine.GetMainLoop() as SceneTree;
        tree?.Root.AddChild(canvasLayer);

        var tween = NGame.Instance.CreateTween();
        for (int i = 0; i < labels.Count; i++)
        {
            tween.TweenInterval(0.5f);
            tween.TweenProperty(labels[i], "modulate:a", 1.0f, 0.8f);
        }

        // 强制刷新一帧，让文字浮现
        await NGame.Instance.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        try
        {
            GD.Print($"[MapRewind] 准备回溯到: {data.NodeName}");

            // 让诗句播一会儿
            await Task.Delay(3000);
            
            GD.Print("[MapRewind] 正在强行执行 LoadRun...");
            RunState runState = RunState.FromSerializable(data.RunData);
            
            if (runState != null && NGame.Instance != null)
            {
                // 【核心】不退回主菜单，直接原地强拆并重建！
                // 原版的 LoadRun 内部会执行 RootSceneContainer.SetCurrentScene，会自动把旧场景吃掉。
                // 中间那些 Handle is not initialized 报错会被我们的黑屏遮住。
                await NGame.Instance.LoadRun(runState, null);
                GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MapRewind] 回溯异常: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            // 等待新场景彻底生成
            await Task.Delay(1000);
            
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