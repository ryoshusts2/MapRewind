using System;
using System.Reflection;
using HarmonyLib;
using Godot;

namespace MapRewind;

public static class Entry
{
    public const string ModId = "MapRewind";
    public static readonly string ResPath = $"res://{ModId}";

    // 这个类不需要任何方法了，RitsuLib 会自动帮我们扫补丁
}