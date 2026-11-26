public static async Task RewindTo(string coordKey)
{
    if (!Snapshots.ContainsKey(coordKey)) return;
    
    // 加上这个判空
    if (NGame.Instance == null)
    {
        GD.PrintErr("[MapRewind] NGame.Instance 为空，无法加载");
        return;
    }

    try
    {
        SerializableRun save = Snapshots[coordKey];
        RunState runState = RunState.FromSerializable(save);
        await NGame.Instance.LoadRun(runState, save.PreFinishedRoom);
        GD.Print($"[MapRewind] 成功回溯到节点: {coordKey}");
    }
    catch (System.Exception ex)
    {
        GD.PrintErr($"[MapRewind] 回溯失败: {ex.Message}\n{ex.StackTrace}");
    }
}