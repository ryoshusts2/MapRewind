using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;

namespace MapRewind;

// 我们直接拦截游戏的全局初始化类 NGame
[HarmonyPatch(typeof(NGame), "_Ready")]
public static class GlobalUIPatch
{
    [HarmonyPostfix]
    public static void Postfix(NGame __instance)
    {
        // 防止重复加载
        if (__instance.HasNode("MapRewindUI")) return;

        // 创建一个 UI 画布层，保证悬浮在所有画面之上
        var canvas = new CanvasLayer();
        canvas.Name = "MapRewindUI";
        __instance.AddChild(canvas);

        // 创建一个悬浮按钮
        var btn = new Button();
        btn.Text = "时光倒流";
        btn.Position = new Vector2(20, 150); // 放在左上角偏下的位置
        btn.CustomMinimumSize = new Vector2(120, 50);
        canvas.AddChild(btn);

        btn.Pressed += () => ShowHistoryPopup(canvas);
        GD.Print("[MapRewind] 全局时光倒流按钮已注入！");
    }

    private static void ShowHistoryPopup(CanvasLayer canvas)
    {
        if (RewindManager.History.Count == 0) return;

        // 半透明背景
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.8f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        
        // 居中面板
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(600, 400);
        
        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        var title = new Label();
        title.Text = "选择你要回溯的历史节点";
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

        btnCancel.Pressed += () => overlay.QueueFree();

        btnConfirm.Pressed += () => {
            var selected = itemList.GetSelectedItems();
            if (selected.Length == 0) return;
            int index = (int)itemList.GetItemMetadata(selected[0].Index);
            
            // 二次确认
            var confirmDialog = new ConfirmationDialog();
            confirmDialog.DialogText = $"确定要回到 [{RewindManager.History[index].NodeName}] 吗？\n当前进度将丢失！";
            overlay.AddChild(confirmDialog);
            confirmDialog.Confirmed += () => {
                overlay.QueueFree();
                _ = RewindManager.RewindTo(index);
            };
            confirmDialog.PopupCentered();
        };

        overlay.AddChild(panel);
        canvas.AddChild(overlay);
    }
}