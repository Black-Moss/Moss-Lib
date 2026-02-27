using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MossLib.Locale;

public abstract class ModLocaleBase
{
    private static ManualLogSource Log { get; set; }
    
    protected abstract string PluginGuid { get; }
    
    protected abstract string PluginName { get; }

    private static string LangDirectory => "Lang";
    
    private JObject _currentLang = new();
    
    private JObject _englishLang = new();
    
    protected void Initialize(ManualLogSource logger)
    {
        Log = logger;
        
        var harmony = new Harmony($"{PluginGuid}.modlocale");
        harmony.PatchAll(GetType());
        
        LoadLanguageFiles();
    }

    private void LoadLanguageFiles()
    {
        var currentLangName = PlayerPrefs.GetString("locale", "EN");
        var pluginPath = new DirectoryInfo(Path.Combine(Paths.PluginPath, PluginName));
        var langDirectory = Path.Combine(pluginPath.FullName, LangDirectory);
        
        if (!Directory.Exists(langDirectory))
        {
            Directory.CreateDirectory(langDirectory);
            Log.LogWarning($"Language directory created: {langDirectory}");
        }
        
        var langFilePath = Path.Combine(langDirectory, $"{currentLangName}.json");
        var englishFilePath = Path.Combine(langDirectory, "EN.json");
        
        try
        {
            LoadLanguageFile(langFilePath, ref _currentLang, currentLangName);
            LoadLanguageFile(englishFilePath, ref _englishLang, "EN");
        }
        catch (JsonReaderException ex)
        {
            Log.LogError($"Invalid JSON format in language file: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Error loading language files: {ex.Message}");
        }
    }
    
    private static void LoadLanguageFile(string filePath, ref JObject target, string fileName)
    {
        if (File.Exists(filePath))
        {
            var jsonContent = File.ReadAllText(filePath);
            target = JObject.Parse(jsonContent);
            Log.LogInfo($"Loaded language file: {fileName}.json");
        }
        else
        {
            Log.LogWarning($"Language file not found: \"{fileName}.json\", will use English as fallback");
        }
    }
    
    protected string GetString(string key)
    {
        try
        {
            var currentValue = GetJsonValue(_currentLang, key);
            if (currentValue != null)
            {
                return currentValue.ToString();
            }
            
            var englishValue = GetJsonValue(_englishLang, key);
            if (englishValue != null)
            {
                Log.LogWarning($"Translation key '{key}' not found in current language, using English fallback");
                return englishValue.ToString();
            }
            
            Log.LogError($"Translation key '{key}' not found in both current language and English fallback");
            return key;
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Load locale string error: \"{key}\" - {ex.Message}");
            return key;
        }
    }
    
    protected string[] GetStringArray(string key)
    {
        return TryGetValue<JArray>(key, out var jsonArray, _currentLang, _englishLang)
            ? jsonArray.Select(token => token.ToString()).ToArray()
            : [];
    }
    
    protected Dictionary<string, string> GetStringDictionary(string key)
    {
        return TryGetValue<JObject>(key, out var jsonObject, _currentLang, _englishLang)
            ? jsonObject.ToObject<Dictionary<string, string>>()
            : new Dictionary<string, string>();
    }
    
    private static JToken GetJsonValue(JObject jsonObject, string path)
    {
        if (jsonObject == null || string.IsNullOrEmpty(path))
            return null;
            
        if (jsonObject.TryGetValue(path, out var directValue))
            return directValue;
            
        var tokens = path.Split('.');
        JToken current = jsonObject;
        
        foreach (var token in tokens)
        {
            if (current == null) return null;
            
            if (token.Contains('[') && token.Contains(']'))
            {
                var parts = token.Split('[', ']');
                var propertyName = parts[0];
                var indexStr = parts[1];
                
                if (!string.IsNullOrEmpty(propertyName))
                {
                    current = current[propertyName];
                    if (current == null) return null;
                }

                if (!int.TryParse(indexStr, out var index) || current.Type != JTokenType.Array) continue;
                var array = (JArray)current;
                if (index >= 0 && index < array.Count)
                    current = array[index];
                else
                    return null;
            }
            else
            {
                current = current[token];
            }
        }
        
        return current;
    }
    
    private static bool TryGetValue<T>(string key, out T result, JObject currentLang, JObject englishLang) where T : JToken
    {
        result = GetJsonValue(currentLang, key) as T;
        if (result != null) return true;
        
        result = GetJsonValue(englishLang, key) as T;
        if (result != null)
        {
            Log?.LogWarning($"Translation key '{key}' not found in current language, using English fallback");
            return true;
        }
        
        Log?.LogError($"Translation key '{key}' not found");
        return false;
    }
}
