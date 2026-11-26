using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace MapRewind;

[HarmonyPatch(typeof(NGame), "_Ready")]
public static class GlobalUIPatch
{
    [HarmonyPostfix]
    public static void Postfix(NGame __instance)
    {
        if (__instance.HasNode("MapRewindUI")) return;

        var canvas = new CanvasLayer();
        canvas.Name = "MapRewindUI";
        __instance.AddChild(canvas);

        var btn = new Button();
        // 【修改】中二台词！
        btn.Text = "春秋蝉大人！"; 
        btn.Position = new Vector2(20, 150); 
        btn.CustomMinimumSize = new Vector2(150, 50);
        canvas.AddChild(btn);

        btn.Pressed += () => ShowHistoryPopup(canvas);
        GD.Print("[MapRewind] 全局时光倒流按钮已注入！");
    }

    private static void ShowHistoryPopup(CanvasLayer canvas)
    {
        if (RewindManager.History.Count == 0) return;

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.8f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        
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

        btnCancel.Pressed += () => overlay.QueueFree();

        btnConfirm.Pressed += () => {
            var selected = itemList.GetSelectedItems();
            if (selected.Length == 0) return;
            int index = (int)itemList.GetItemMetadata(selected[0]);
            
            var confirmDialog = new ConfirmationDialog();
            confirmDialog.DialogText = $"确定要回到 [{RewindManager.History[index].NodeName}] 吗？\n当前进度将丢失！";
            overlay.AddChild(confirmDialog);
            confirmDialog.Confirmed += () => {
                // 先关掉弹窗，再执行回溯
                overlay.QueueFree();
                _ = RewindManager.RewindTo(index);
            };
            confirmDialog.PopupCentered();
        };

        overlay.AddChild(panel);
        canvas.AddChild(overlay);
    }
}