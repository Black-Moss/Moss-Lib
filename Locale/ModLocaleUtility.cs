using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MossLib.Locale;

public static class ModLocaleUtility
{
    private static readonly Dictionary<string, JObject> LoadedLanguages = new();
    private static readonly Dictionary<string, ManualLogSource> Loggers = new();
    
    public static void Initialize(string pluginGuid, string pluginName, ManualLogSource logger, string langDirectory = "Lang")
    {
        if (!Loggers.ContainsKey(pluginGuid))
        {
            Loggers[pluginGuid] = logger;
        }
        
        LoadLanguageFiles(pluginGuid, pluginName, langDirectory);
    }

    private static void LoadLanguageFiles(string pluginGuid, string pluginName, string langDirectory)
    {
        var currentLangName = PlayerPrefs.GetString("locale", "EN");
        var pluginPath = new DirectoryInfo(Path.Combine(Paths.PluginPath, pluginName));
        var langDirPath = Path.Combine(pluginPath.FullName, langDirectory);
        
        if (!Directory.Exists(langDirPath))
        {
            Directory.CreateDirectory(langDirPath);
            Loggers[pluginGuid]?.LogWarning($"Language directory created: {langDirPath}");
        }
        
        var langFilePath = Path.Combine(langDirPath, $"{currentLangName}.json");
        var englishFilePath = Path.Combine(langDirPath, "EN.json");
        
        try
        {
            LoadLanguageFile(pluginGuid, langFilePath, currentLangName);
            LoadLanguageFile(pluginGuid, englishFilePath, "EN");
        }
        catch (JsonReaderException ex)
        {
            Loggers[pluginGuid]?.LogError($"Invalid JSON format in language file: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Loggers[pluginGuid]?.LogError($"Error loading language files: {ex.Message}");
        }
    }

    private static void LoadLanguageFile(string pluginGuid, string filePath, string fileName)
    {
        if (!LoadedLanguages.ContainsKey(pluginGuid))
        {
            LoadedLanguages[pluginGuid] = new JObject();
        }
        
        if (File.Exists(filePath))
        {
            var jsonContent = File.ReadAllText(filePath);
            var langData = JObject.Parse(jsonContent);
            LoadedLanguages[pluginGuid][fileName] = langData;
            Loggers[pluginGuid]?.LogInfo($"Loaded language file: {fileName}.json for {pluginGuid}");
        }
        else
        {
            Loggers[pluginGuid]?.LogWarning($"Language file not found: \"{fileName}.json\"");
        }
    }
    
    public static string GetString(string pluginGuid, string key)
    {
        if (!LoadedLanguages.ContainsKey(pluginGuid) || !Loggers.ContainsKey(pluginGuid))
        {
            return key;
        }
        
        try
        {
            var currentLangName = PlayerPrefs.GetString("locale", "EN");
            var currentLang = LoadedLanguages[pluginGuid][currentLangName] as JObject;
            var englishLang = LoadedLanguages[pluginGuid]["EN"] as JObject;
            
            var currentValue = GetJsonValue(currentLang, key);
            if (currentValue != null)
            {
                return currentValue.ToString();
            }
            
            var englishValue = GetJsonValue(englishLang, key);
            if (englishValue != null)
            {
                Loggers[pluginGuid].LogWarning($"Translation key '{key}' not found in current language, using English fallback");
                return englishValue.ToString();
            }
            
            Loggers[pluginGuid].LogError($"Translation key '{key}' not found in both current language and English fallback");
            return key;
        }
        catch (System.Exception ex)
        {
            Loggers[pluginGuid]?.LogError($"Load locale string error: \"{key}\" - {ex.Message}");
            return key;
        }
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
                
                if (int.TryParse(indexStr, out var index) && current.Type == JTokenType.Array)
                {
                    var array = (JArray)current;
                    if (index >= 0 && index < array.Count)
                        current = array[index];
                    else
                        return null;
                }
            }
            else
            {
                current = current[token];
            }
        }
        
        return current;
    }
}
