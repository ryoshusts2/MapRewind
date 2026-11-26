using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using Godot;

namespace MapRewind;

// 拦截暂停菜单的初始化
[HarmonyPatch(typeof(NPauseMenu), "_Ready")]
public static class PauseMenuPatch
{
    [HarmonyPostfix]
    public static void Postfix(NPauseMenu __instance)
    {
        // 防止重复添加
        if (__instance.HasNode("MapRewindButton")) return;

        Button rewindBtn = new Button();
        rewindBtn.Name = "MapRewindButton";
        rewindBtn.Text = "时光回溯 (Map Rewind)";
        rewindBtn.CustomMinimumSize = new Vector2(250, 50);
        
        // 这里用绝对位置占个位，一般原版菜单在屏幕左侧
        rewindBtn.Position = new Vector2(50, 300); 

        // 点击打开自定义弹窗
        rewindBtn.Pressed += () => {
            ShowHistoryPopup(__instance);
        };

        __instance.AddChild(rewindBtn);
        GD.Print("[MapRewind] 成功将按钮注入暂停菜单！");
    }

    private static void ShowHistoryPopup(NPauseMenu parent)
    {
        if (RewindManager.History.Count == 0) return;

        // 创建一个覆盖全屏的面板
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.8f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(600, 400);
        
        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        // 标题
        var title = new Label();
        title.Text = "选择你要回溯的历史节点";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        // 历史列表
        var itemList = new ItemList();
        itemList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        for (int i = 0; i < RewindManager.History.Count; i++)
        {
            itemList.AddItem(RewindManager.History[i].NodeName, null, true);
            itemList.SetItemMetadata(i, i);
        }
        vbox.AddChild(itemList);

        // 按钮横排
        var hbox = new HBoxContainer();
        var btnConfirm = new Button() { Text = "回溯" };
        var btnCancel = new Button() { Text = "取消" };
        hbox.AddChild(btnConfirm);
        hbox.AddChild(btnCancel);
        vbox.AddChild(hbox);

        // 取消事件
        btnCancel.Pressed += () => overlay.QueueFree();

        // 确认事件（二次确认）
        btnConfirm.Pressed += () => {
            var selected = itemList.GetSelectedItems();
            if (selected.Length == 0) return;
            int index = (int)itemList.GetItemMetadata(selected[0].Index);
            
            // 再次确认
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
        parent.AddChild(overlay);
    }
}