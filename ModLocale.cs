using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace BlackMossTemplate;

public class ModLocale
{
    private static readonly ManualLogSource Log = Plugin.Logger;
    internal static void Initialize()
    {
        new Harmony($"{Plugin.Guid}.modlocale").PatchAll(typeof(ModLocale));
        Log.LogInfo(GetString("log.test"));
    }
    
    private static readonly DirectoryInfo RootPath = new($"{Paths.PluginPath}//{Plugin.Name}");
    private static readonly Dictionary<string, string> CurrentLang = new();
    private static readonly Dictionary<string, string> EnglishLang = new(); // 英语备用字典
    
    static ModLocale()
    {
        var currentLangName = PlayerPrefs.GetString("locale");
        var langDirectory = $@"{RootPath.FullName}\Lang";
        
        if (!Directory.Exists(langDirectory))
        {
            Directory.CreateDirectory(langDirectory);
            Log.LogError("Language directory not found!");
            Log.LogInfo("Created Lang directory");
        }
        
        var langFilePath = $@"{langDirectory}\{currentLangName}.json";
        var englishFilePath = $@"{langDirectory}\EN.json"; // 英语文件路径
        
        try
        {
            // 加载当前语言文件
            if (File.Exists(langFilePath))
            {
                CurrentLang = JsonConvert.DeserializeObject<Dictionary<string, string>>
                    (File.ReadAllText(langFilePath));
                Log.LogInfo($"Loaded language file: {currentLangName}.json");
            }
            else
            {
                Log.LogWarning($"Language file not found: \"{currentLangName}.json\", will use English as fallback");
            }
            
            // 加载英语备用文件
            if (File.Exists(englishFilePath))
            {
                EnglishLang = JsonConvert.DeserializeObject<Dictionary<string, string>>
                    (File.ReadAllText(englishFilePath));
                Log.LogInfo("Loaded English fallback language file");
            }
            else
            {
                Log.LogError("English fallback language file not found!");
            }
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Error loading language files: {ex.Message}");
        }
    }

    internal static string GetString(string str)
    {
        try
        {
            if (CurrentLang.TryGetValue(str, out var currentLangValue) && !string.IsNullOrEmpty(currentLangValue))
            {
                return currentLangValue;
            }
            
            if (EnglishLang.TryGetValue(str, out var englishValue) && !string.IsNullOrEmpty(englishValue))
            {
                Log.LogWarning($"Translation key '{str}' not found in current language, using English fallback: '{englishValue}'");
                return englishValue;
            }
            
            Log.LogError($"Translation key '{str}' not found in both current language and English fallback");
            return str;
        } 
        catch (System.Exception ex)
        {
            Log.LogError($"Load locale string error: \"{str}\" - {ex.Message}");
            return str;
        }
    }
    
    [HarmonyPatch(typeof(Locale), "GetString")]
    public static class LocalePatch
    {
        [HarmonyPrefix]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedParameter.Local
        private static bool Prefix_Locale_GetString(string str, int type, ref string __result)
        {
            // 首先尝试从当前语言获取
            if (CurrentLang.TryGetValue(str, out var currentValue) && !string.IsNullOrEmpty(currentValue))
            {
                __result = currentValue;
                return false;
            }
            
            // 如果当前语言没有或为空，尝试从英语获取
            if (EnglishLang.TryGetValue(str, out var englishValue) && !string.IsNullOrEmpty(englishValue))
            {
                Log.LogWarning($"Translation key '{str}' not found in current language, using English fallback: '{englishValue}'");
                __result = englishValue;
                return false;
            }
            
            // 都找不到则不拦截，让原方法处理
            return true;
        }
    }
}