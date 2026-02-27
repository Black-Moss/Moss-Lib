using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using MossLib.Base;

namespace MossLib.Example;

public class ModLocale : ModLocaleBase
{
    // ReSharper disable once UnusedMember.Global
    private static ModLocale Instance { get; set; } = new();
    
    private static ModLocale _instance;
        
    public static void Initialize(ManualLogSource logger)
    {
        if (_instance != null) return;
        _instance = new ModLocale();
        Instance = _instance;
        _instance.Initialize(logger, Plugin.Guid, Plugin.Name, Assembly.GetExecutingAssembly());
    }
        
    public static string Get(string key) => Instance.GetString(key);
        
    public static string GetFormat(string key, params object[] args) => 
        Instance.GetStringFormatted(key, args);
            
    public static bool ContainsKey(string key) => Instance.HasKey(key);
        
    public static string GetOnDictionary(string dictionary, string key) => 
        Instance.GetStringOnDictionary(dictionary, key);
    
    public static string[] GetArray(string key) => Instance.GetStringArray(key);
        
    public static Dictionary<string, string> GetDictionary(string key) => 
        Instance.GetStringDictionary(key);
}