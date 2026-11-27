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
    
    // 让玩家把诗看完，同时给引擎 3 秒时间，让旧场景的动画、特效自然停滞
    await Task.Delay(3000);

    GD.Print("[MapRewind] 正在执行原版 LoadRun...");
    
    // 【核心修复】在加载前，强行把当前正在渲染的旧界面隐藏，极大减轻显存瞬间崩溃的压力
    var rootScene = NGame.Instance.RootSceneContainer;
    if (rootScene != null && rootScene.CurrentScene != null)
    {
        rootScene.CurrentScene.Visible = false;
    }

    // 给引擎一帧的时间去隐藏渲染
    await NGame.Instance.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

    RunState runState = RunState.FromSerializable(data.RunData);
    if (runState != null)
    {
        await NGame.Instance.LoadRun(runState, null);
        GD.Print($"[MapRewind] 成功回溯到: {data.NodeName}");
    }
}