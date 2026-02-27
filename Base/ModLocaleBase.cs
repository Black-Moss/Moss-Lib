using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MossLib.Base;

public abstract class ModLocaleBase
{
    private static readonly object Lock = new();
    private ManualLogSource _log;
    private bool _isInitialized;
    private System.Reflection.Assembly _pluginAssembly;

    private const string LangDirectory = "Lang";
    private JObject _currentLang = new();
    private JObject _englishLang = new();
    
    // 移除静态 Initialize 方法，改为实例方法
    protected void Initialize(ManualLogSource logger, string pluginGuid, string pluginName, System.Reflection.Assembly pluginAssembly, Harmony harmonyInstance = null)
    {
        if (_isInitialized)
        {
            logger.LogWarning("ModLocaleBase has already been initialized");
            return;
        }

        lock (Lock)
        {
            if (_isInitialized) return;
            
            _log = logger;
            _pluginAssembly = pluginAssembly;
            
            var harmony = harmonyInstance ?? new Harmony($"{pluginGuid}.modlocale");
            harmony.PatchAll(GetType());
                
            LoadLanguageFiles(); 
            _isInitialized = true;
        }
    }
    
    [System.Obsolete("Use instance Initialize method instead")]
    protected static void Initialize(ManualLogSource logger, string pluginGuid, string pluginName, Harmony harmonyInstance = null)
    {
        logger.LogError("Static Initialize method is deprecated. Please use instance method with plugin assembly parameter.");
    }

    private void LoadLanguageFiles()
    {
        var currentLangName = PlayerPrefs.GetString("locale", "EN");
    
        // 直接使用实例字段，不再需要 null 检查
        var pluginAssembly = _pluginAssembly;
        var pluginDirectory = Path.GetDirectoryName(pluginAssembly.Location);
    
        if (string.IsNullOrEmpty(pluginDirectory))
        {
            _log?.LogError("Failed to get plugin directory");
            return;
        }
    
        var langDirectory = Path.Combine(pluginDirectory, LangDirectory);
        
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

    private void LoadLanguageFile(string filePath, ref JObject target, string fileName)
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

    protected string GetString(string key)
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
    
    protected string GetStringOnDictionary(string dictionary, string key)
    {
        Dictionary<string, string> stringDictionary = GetStringDictionary(dictionary);
        return stringDictionary[key];
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
            ? jsonObject.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>()
            : new Dictionary<string, string>();
    }

    protected string GetStringFormatted(string key, params object[] args)
    {
        var template = GetString(key);
        if (string.IsNullOrEmpty(template) || (template.StartsWith("[") && template.EndsWith("]")))
        {
            return template;
        }
            
        try
        {
            return string.Format(template, args);
        }
        catch (System.FormatException ex)
        {
            _log?.LogError($"String formatting failed Key: {key}, Template: {template}, Error: {ex.Message}");
            return template;
        }
    }

    // 新增重载方法，专门支持int和float类型
    protected string GetStringFormatted(string key, int arg)
    {
        return GetStringFormatted(key, (object)arg);
    }

    protected string GetStringFormatted(string key, float arg)
    {
        return GetStringFormatted(key, (object)arg);
    }

    protected string GetStringFormatted(string key, int arg1, int arg2)
    {
        return GetStringFormatted(key, (object)arg1, (object)arg2);
    }

    protected string GetStringFormatted(string key, float arg1, float arg2)
    {
        return GetStringFormatted(key, (object)arg1, (object)arg2);
    }

    protected string GetStringFormatted(string key, int arg1, float arg2)
    {
        return GetStringFormatted(key, (object)arg1, (object)arg2);
    }

    protected string GetStringFormatted(string key, float arg1, int arg2)
    {
        return GetStringFormatted(key, (object)arg1, (object)arg2);
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

    private bool TryGetValue<T>(string key, out T result, JObject currentLang, JObject englishLang) where T : JToken
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
}
