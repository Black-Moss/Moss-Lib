using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MossLib.Locale;

public abstract class ModLocaleBase
{
    private static readonly object _lock = new object();
    private static ManualLogSource _log;
    private static string _pluginGuid;
    private static string _pluginName;
    private static bool _isInitialized;
    
    private static readonly string LangDirectory = "Lang";
    private static JObject _currentLang = new();
    private static JObject _englishLang = new();
    
    protected static void Initialize(ManualLogSource logger, string pluginGuid, string pluginName, Harmony harmonyInstance = null)
    {
        if (_isInitialized)
        {
            logger.LogWarning("ModLocaleBase has already been initialized");
            return;
        }

        lock (_lock)
        {
            if (_isInitialized) return;
            
            _log = logger;
            _pluginGuid = pluginGuid;
            _pluginName = pluginName;
            
            var harmony = harmonyInstance ?? new Harmony($"{pluginGuid}.modlocale");
            harmony.PatchAll(typeof(ModLocaleBase));
                
            
            LoadLanguageFiles(); 
            _isInitialized = true;
            
        }
    }
    
    public static void ReloadLanguageFiles()
    {
        if (!_isInitialized)
        {
            _log?.LogWarning("ModLocaleBase is not initialized, cannot reload language files");
            return;
        }
        
        lock (_lock)
        {
            LoadLanguageFiles();
        }
    }

    private static void LoadLanguageFiles()
    {
        var currentLangName = PlayerPrefs.GetString("locale", "EN");
        var pluginPath = new DirectoryInfo(Path.Combine(Paths.PluginPath, _pluginName));
        var langDirectory = Path.Combine(pluginPath.FullName, LangDirectory);
        
        if (!Directory.Exists(langDirectory))
        {
            Directory.CreateDirectory(langDirectory);
            _log?.LogWarning($"Created language directory: {langDirectory}");
        }
        
        var langFilePath = Path.Combine(langDirectory, $"{currentLangName}.json");
        var englishFilePath = Path.Combine(langDirectory, "EN.json");
        
        try
        {
            LoadLanguageFile(langFilePath, ref _currentLang, currentLangName);
            LoadLanguageFile(englishFilePath, ref _englishLang, "EN");
            
            _log?.LogInfo($"Language files loaded successfully - Current language: {currentLangName}");
        }
        catch (JsonReaderException ex)
        {
            _log?.LogError($"Language file JSON format error: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            _log?.LogError($"Error occurred while loading language files: {ex.Message}");
        }
    }
    
    private static void LoadLanguageFile(string filePath, ref JObject target, string fileName)
    {
        if (File.Exists(filePath))
        {
            try
            {
                var jsonContent = File.ReadAllText(filePath);
                target = JObject.Parse(jsonContent);
                _log?.LogInfo($"Loaded language file: {fileName}.json");
            }
            catch (JsonReaderException ex)
            {
                _log?.LogError($"Failed to parse language file {fileName}.json: {ex.Message}");
                target = new JObject();
            }
        }
        else
        {
            _log?.LogWarning($"Language file not found: \"{fileName}.json\", will use English as fallback");
            target = new JObject();
        }
    }
    
    internal static string GetString(string key)
    {
        try
        {
            var currentValue = GetJsonValue(_currentLang, key);
            if (currentValue != null && currentValue.Type != JTokenType.Null)
            {
                return currentValue.ToString();
            }
            
            var englishValue = GetJsonValue(_englishLang, key);
            if (englishValue != null && englishValue.Type != JTokenType.Null)
            {
                _log?.LogWarning($"Translation key '{key}' not found in current language, using English fallback");
                return englishValue.ToString();
            }
            
            _log?.LogError($"Translation key '{key}' not found in both current language and English fallback");
            return $"[{key}]";
        }
        catch (System.Exception ex)
        {
            _log?.LogError($"Error getting localized string \"{key}\": {ex.Message}");
            return $"[{key}]";
        }
    }
    
    protected string[] GetStringArray(string key)
    {
        return TryGetValue<JArray>(key, out var jsonArray, _currentLang, _englishLang)
            ? jsonArray.Select(token => token.ToString()).ToArray()
            : new string[0];
    }
    
    internal static Dictionary<string, string> GetStringDictionary(string key)
    {
        return TryGetValue<JObject>(key, out var jsonObject, _currentLang, _englishLang)
            ? jsonObject.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>()
            : new Dictionary<string, string>();
    }
    
    protected string GetStringFormatted(string key, params object[] args)
    {
        var template = GetString(key);
        if (string.IsNullOrEmpty(template))
            return template;
            
        try
        {
            return string.Format(template, args);
        }
        catch (System.FormatException ex)
        {
            _log?.LogError($"String formatting failed Key: {key}, Error: {ex.Message}");
            return template;
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
            if (current == null || current.Type == JTokenType.Null) 
                return null;
            
            if (token.Contains('[') && token.Contains(']'))
            {
                var parts = token.Split('[', ']');
                var propertyName = parts[0];
                var indexStr = parts[1];
                
                if (!string.IsNullOrEmpty(propertyName))
                {
                    current = current[propertyName];
                    if (current == null || current.Type == JTokenType.Null) 
                        return null;
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
        if (result != null && result.Type != JTokenType.Null) 
            return true;
        
        result = GetJsonValue(englishLang, key) as T;
        if (result != null && result.Type != JTokenType.Null)
        {
            _log?.LogWarning($"Translation key '{key}' not found in current language, using English fallback");
            return true;
        }
        
        _log?.LogError($"Translation key '{key}' not found");
        result = null;
        return false;
    }
    
    protected bool HasKey(string key)
    {
        return GetJsonValue(_currentLang, key) != null || GetJsonValue(_englishLang, key) != null;
    }
    
    public static string CurrentLanguage => PlayerPrefs.GetString("locale", "EN");
}
