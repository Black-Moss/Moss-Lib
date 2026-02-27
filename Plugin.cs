using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MossLib.Locale;

namespace MossLib
{
    [BepInPlugin("blackmoss.mosslib", "Moss Lib", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
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
            _harmony.PatchAll();
            ModLocaleUtility.Initialize(Guid, Name, Logger);

            Logger.LogInfo($"Welcome to {Name}!");
        }
    }
}