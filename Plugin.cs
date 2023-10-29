using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using InkboundModEnabler.Util;
using System.Reflection;

namespace InkyBubbleTweaks
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [CosmeticPlugin]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "CBF6F9BE-204D-48C4-B1E4-A4C68A778A85";
        public const string PLUGIN_NAME = "Inky Bubble Tweaks";
        public const string PLUGIN_VERSION = "1.0.0";
        public static ManualLogSource log;

        public static Harmony HarmonyInstance => new Harmony(PLUGIN_GUID);

        private void Awake()
        {
            // Plugin startup logic
            log = Logger;
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
        }


    }
}