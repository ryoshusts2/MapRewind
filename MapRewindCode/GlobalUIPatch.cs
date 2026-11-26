using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using System.Linq;

namespace MapRewind;

[HarmonyPatch(typeof(NGame), "_Ready")]
public static class GlobalUIPatch
{
    private static CanvasLayer? _canvas;
    private static Control? _currentOverlay;

    [HarmonyPostfix]
    public static void Postfix(NGame __instance)
    {
        if (__instance.HasNode("MapRewindUI")) return;

        _canvas = new CanvasLayer();
        _canvas.Name = "MapRewindUI";
        __instance.AddChild(_canvas);

        var btn = new Button();
        btn.Text = "春秋蝉大人！"; 
        btn.Position = new Vector2(20, 150); 
        btn.CustomMinimumSize = new Vector2(150, 50);
        _canvas.AddChild(btn);

        btn.Pressed += ShowHistoryMap; 
        GD.Print("[MapRewind] 全局按钮已注入！");
    }

    private static void ShowHistoryMap()
    {
        if (RewindManager.History.Count == 0) return;

        if (_currentOverlay != null && GodotObject.IsInstanceValid(_currentOverlay))
            _currentOverlay.QueueFree();

        _currentOverlay = new ColorRect();
        ((ColorRect)_currentOverlay).Color = new Color(0, 0, 0, 0.9f);
        _currentOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(800, 600); 
        
        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        var title = new Label();
        title.Text = "春秋蝉大人，你还要我等到什么时候！";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var grid = new GridContainer();
        grid.Columns = 7; 
        grid.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(grid);

        var currentActIndex = RewindManager.History[0].ActIndex;
        var actSnapshots = RewindManager.History.Where(h => h.ActIndex == currentActIndex)
            .OrderBy(h => h.Row).ThenBy(h => h.Col).ToList();

        int maxRow = actSnapshots.Count > 0 ? actSnapshots.Max(h => h.Row) : 0;
        
        for (int r = 0; r <= maxRow; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                var snap = actSnapshots.FirstOrDefault(h => h.Row == r && h.Col == c);
                if (snap != null)
                {
                    var nodeBtn = new Button();
                    nodeBtn.Text = $"{r}-{c}"; 
                    nodeBtn.CustomMinimumSize = new Vector2(80, 80);
                    nodeBtn.Modulate = new Color(1, 0.8f, 0.2f); 
                    
                    var snapIndex = RewindManager.History.IndexOf(snap);
                    nodeBtn.Pressed += () => ShowConfirmDialog(snapIndex);
                    grid.AddChild(nodeBtn);
                }
                else
                {
                    var empty = new Control();
                    empty.CustomMinimumSize = new Vector2(80, 80);
                    grid.AddChild(empty);
                }
            }
        }

        var btnClose = new Button() { Text = "关闭地图" };
        btnClose.Pressed += () => _currentOverlay?.QueueFree();
        vbox.AddChild(btnClose);

        _currentOverlay.AddChild(panel);
        _canvas?.AddChild(_currentOverlay);
    }

    private static void ShowConfirmDialog(int index)
    {
        var confirmDialog = new ConfirmationDialog();
        confirmDialog.DialogText = $"确定要回到 [{RewindManager.History[index].NodeName}] 吗？";
        _currentOverlay?.AddChild(confirmDialog);
        confirmDialog.Confirmed += () => {
            // 【核心修复】必须先用 QueueFree 销毁我们当前的 UI，防止它在读档时崩溃！
            _currentOverlay?.QueueFree();
            RewindManager.RewindTo(index);
        };
        confirmDialog.PopupCentered();
    }
}