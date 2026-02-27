using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MossLib.Base;
using UnityEngine;

namespace MossLib.Example;

public class ModCommand : ModCommandBase
{
    private static ModCommand Instance { get; set; } = new();
    
    private static ModCommand _instance;
    
    public static void Initialize(ManualLogSource logger)
    { 
        if (_instance != null) return;
        _instance = new ModCommand();
        Instance = _instance;
        _instance.Initialize(logger, Plugin.Guid, Plugin.Name, Assembly.GetExecutingAssembly());
    }
    
    [HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
    public class ConsoleScriptRegisterAllCommandsPatcher
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Global
        public static void RegisterCustomCommands()
        {
            ConsoleScript.Commands.Add(new Command(
                "testhello", 
                "TEST HELLO",
                args =>
                {
                    Instance.CheckForWorld();
                    
                    var message = "Hello from MossLib!";
                    if (args.Length > 1)
                    {
                        message = $"Hello, {args[1]}!";
                    }
                    
                    PlayerCamera.main.DoAlert(message);
                    Instance.LogToConsole($"Greetings: {message}");
                    Instance.Logger.LogInfo($"Command executed: hello - {message}");
                },
                null,
                ("name", "可选的名称参数")
            ));
        }
    }
    
    [HarmonyPatch(typeof(ConsoleScript), "Awake")]
    public new class ConsoleScriptAwakePatcher
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Global
        public static void AddCustomLogCallback()
        {
            Application.logMessageReceived += Instance.ApplicationLogCallback;
        }
    }
}