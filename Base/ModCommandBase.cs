using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MossLib.Base;

public abstract class ModCommandBase
{
    private static readonly object Lock = new();
    private ManualLogSource _log;
    private bool _isInitialized;
    private System.Reflection.Assembly _pluginAssembly;
    
    private string _lastErr = "";

    protected void Initialize(ManualLogSource logger, string pluginGuid, string pluginName, System.Reflection.Assembly pluginAssembly, Harmony harmonyInstance = null)
    {
        if (_isInitialized)
        {
            logger.LogWarning("ModCommandBase has already been initialized");
            return;
        }

        lock (Lock)
        {
            if (_isInitialized) return;
            
            _log = logger;
            _pluginAssembly = pluginAssembly;
            
            var harmony = harmonyInstance ?? new Harmony($"{pluginGuid}.modcommand");
            harmony.PatchAll(GetType());
                
            _isInitialized = true;
        }
    }

    [HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
    public class ConsoleScriptPatcher
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Global
        public static void RegisterModCommands()
        {
        }
    }

    protected void LogToConsole(string text)
    {
        if (ConsoleScript.instance != null)
        {
            ConsoleScript.instance.ExecuteCommand($"log {text.Replace(" ", " ")}");
        }
    }

    public void ApplicationLogCallback(string condition, string stackTrace, LogType type)
    {
        if (_lastErr == condition)
            return;
            
        _lastErr = condition;

        switch (type)
        {
            case LogType.Error or LogType.Assert:
                LogToConsole($"<color=red>{condition}; {stackTrace}</color>");
                break;
            case LogType.Warning:
                LogToConsole($"<color=yellow>{condition}; {stackTrace}</color>");
                break;
            case LogType.Log:
                LogToConsole(condition);
                break;
            case LogType.Exception:
                LogToConsole($"<color=red>{condition}; {stackTrace}</color>");
                break;
        }
    }

    [HarmonyPatch(typeof(ConsoleScript), "Awake")]
    public class ConsoleScriptAwakePatcher
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Global
        public static void AddLogCallback()
        {
            // 具体的日志回调应该在子类中注册
        }
    }

    protected bool ParseBool(string s)
    {
        return !bool.TryParse(s, out var result) ? throw new Exception($"\"{s}\" is not a valid boolean value! (true/false)") : result;
    }
    
    protected void CheckForWorld()
    {
        if (!(bool) (UnityEngine.Object) PlayerCamera.main)
            throw new Exception("No world is loaded. Try starting a game?");
    }
    
    // 提供受保护的方法供子类使用
    protected ManualLogSource Logger => _log;
    
    protected System.Reflection.Assembly PluginAssembly => _pluginAssembly;
}