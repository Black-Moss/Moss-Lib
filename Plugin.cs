using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MossLib.Example;

namespace MossLib
{
    [BepInPlugin(Guid, Name, "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        // ReSharper disable once MemberCanBePrivate.Global
        internal new static ManualLogSource Logger;
        internal const string Guid = "blackmoss.mosslib";
        internal const string Name = "Moss Lib";
        private readonly Harmony _harmony = new(Guid);
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public static Plugin Instance { get; private set; } = null!;

        public void Awake()
        {
            Logger = base.Logger;
            Instance = this;
            
            ModLocale.Initialize(Logger);
            ModCommand.Initialize(Logger);
            
            _harmony.PatchAll();
            
            Logger.LogInfo(ModLocale.GetFormat("log.welcome"));
        }
    }
}