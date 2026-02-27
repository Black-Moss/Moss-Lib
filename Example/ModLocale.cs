using BepInEx.Logging;
using MossLib.Locale;

namespace MossLib.Example;

public class ModLocale : ModLocaleBase
{
    // ReSharper disable once MemberCanBePrivate.Global
    internal static ModLocale Instance { get; private set; } = new();
    
    public static void Initialize(ManualLogSource logger)
    {
        Instance ??= new ModLocale();

        ModLocaleBase.Initialize(
            logger, 
            Plugin.Guid, 
            Plugin.Name
        );
    }
}