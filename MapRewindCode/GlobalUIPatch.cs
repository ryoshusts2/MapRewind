using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace MapRewind;

[HarmonyPatch(typeof(NGame), "_Ready")]
public static class GlobalUIPatch
{
    // 加上 ? 允许为空，消除警告
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

        btn.Pressed += ShowHistoryPopup;
        GD.Print("[MapRewind] 全局时光倒流按钮已注入！");
    }

    private static void ShowHistoryPopup()
    {
        if (RewindManager.History.Count == 0) return;

        if (_currentOverlay != null && GodotObject.IsInstanceValid(_currentOverlay))
        {
            _currentOverlay.QueueFree();
        }

        _currentOverlay = new ColorRect();
        (_currentOverlay as ColorRect)!.Color = new Color(0, 0, 0, 0.8f); // 加 ! 告诉编译器肯定不为空
        _currentOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(600, 400);
        
        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        var title = new Label();
        title.Text = "春秋蝉大人，你还要我等到什么时候！";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var itemList = new ItemList();
        itemList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        for (int i = 0; i < RewindManager.History.Count; i++)
        {
            itemList.AddItem(RewindManager.History[i].NodeName, null, true);
            itemList.SetItemMetadata(i, i);
        }
        vbox.AddChild(itemList);

        var hbox = new HBoxContainer();
        var btnConfirm = new Button() { Text = "回溯" };
        var btnCancel = new Button() { Text = "取消" };
        hbox.AddChild(btnConfirm);
        hbox.AddChild(btnCancel);
        vbox.AddChild(hbox);

        // 加上 ? 防止空引用
        btnCancel.Pressed += () => _currentOverlay?.QueueFree();

        btnConfirm.Pressed += () => {
            var selected = itemList.GetSelectedItems();
            if (selected.Length == 0) return;
            int index = (int)itemList.GetItemMetadata(selected[0]);
            
            var confirmDialog = new ConfirmationDialog();
            confirmDialog.DialogText = $"确定要回到 [{RewindManager.History[index].NodeName}] 吗？\n当前进度将丢失！";
            _currentOverlay?.AddChild(confirmDialog); // 加上 ?
            confirmDialog.Confirmed += () => {
                _currentOverlay?.QueueFree();
                _ = RewindManager.RewindTo(index);
            };
            confirmDialog.PopupCentered();
        };

        _currentOverlay.AddChild(panel);
        _canvas?.AddChild(_currentOverlay);
    }
}