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

        // ================= 春秋蝉 · 逆流河 =================
        var canvasLayer = new CanvasLayer();
        // 带一点深红/墨色的背景，比纯黑更有宿命感
        var overlay = new ColorRect();
        overlay.Color = new Color(0.05f, 0.02f, 0.02f, 0.95f); 
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        canvasLayer.AddChild(overlay);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.Center);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        
        var poetry = new Label();
        poetry.Text = "落魄谷中寒风吹，春秋蝉鸣少年归。\n荡魂山处石人泪，定仙游走魔向北。\n逆流河上万仙退，爱情不敌坚持泪。\n宿命天成命中败，仙尊悔而我不悔。";
        poetry.HorizontalAlignment = HorizontalAlignment.Center;
        poetry.AddThemeFontSizeOverride("font_size", 32); // 字体放大
        poetry.AddThemeColorOverride("font_color", new Color(0.85f, 0.8f, 0.75f)); // 微微泛黄的复古色
        
        vbox.AddChild(poetry);
        overlay.AddChild(vbox);
        
        NGame.Instance.AddChild(canvasLayer); 
        
        // 让文字有一个非常缓慢的淡入效果
        vbox.Modulate = new Color(1, 1, 1, 0);
        var tween = NGame.Instance.CreateTween();
        tween.TweenProperty(vbox, "modulate:a", 1.0f, 1.5f); 
        
        // 强制刷新一帧，确保黑屏和诗句立刻盖上去
        await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);

        try
        {
            GD.Print($"[MapRewind] 准备回溯到: {data.NodeName}");

            // 1. 彻底退回主菜单（此时屏幕上全是诗，玩家看不到底层清理报错）
            await NGame.Instance.ReturnToMainMenu();
            
            // 2. 等待 1 秒，给引擎足够的时间去释放旧场景的内存
            await Task.Delay(1000);

            // 3. 加载目标存档
            RunState runState = RunState.FromSerializable(data.RunData);
            if (runState != null)
            {
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
            // 4. 停顿 3 秒，让玩家读完诗，然后慢慢淡出遮罩
            await Task.Delay(3000);
            var fadeOutTween = NGame.Instance.CreateTween();
            fadeOutTween.TweenProperty(vbox, "modulate:a", 0.0f, 1.0f);
            fadeOutTween.TweenProperty(overlay, "color:a", 0.0f, 1.0f);
            await NGame.Instance.ToSignal(fadeOutTween, Tween.SignalName.Finished);
            
            // 5. 彻底移除 UI
            canvasLayer.QueueFree();
        }
    }
}