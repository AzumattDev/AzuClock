using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace AzuClock
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class AzuClockPlugin : BaseUnityPlugin

    {
        internal const string ModName = "AzuClock";
        internal const string ModVersion = "1.0.4";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static readonly int windowId = 434344;
        internal static AzuClockPlugin context = null!;

        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource AzuClockLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        public enum Toggle
        {
            Off,
            On
        }

        private void Awake()
        {
            context = this;
            Utilities.GenerateConfigs();

            Utilities.AutoDoc();
            _harmony.PatchAll();
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void Update()
        {
            if (Utilities.IgnoreKeyPresses() || ToggleClockKeyOnPress.Value == Toggle.On || !ToggleClockKey.Value.IsDown())
                return;
            var show = ShowingClock.Value;
            ShowingClock.Value = show == Toggle.Off ? Toggle.On : Toggle.Off;
            Config.Save();
        }

        private void OnGUI()
        {
            if (ShowingClock.Value == Toggle.On && _configApplied && Player.m_localPlayer && Hud.instance)
            {
                float alpha = 1f;
                if(AzuClockPlugin.RealTime.Value == Toggle.On) {NewTimeString = Utilities.GetRealCurrentTimeString();}
                else
                {
                    NewTimeString = Utilities.GetCurrentTimeString();
                }
                if (ShowClockOnChange.Value == Toggle.On)
                {
                    if (NewTimeString == _lastTimeString)
                    {
                        _shownTime = 0;

                        if (ToggleClockKeyOnPress.Value == Toggle.Off || !ToggleClockKey.Value.IsPressed())
                            return;
                    }

                    if (_shownTime > ShowClockOnChangeFadeTime.Value)
                    {
                        if (_shownTime > ShowClockOnChangeFadeTime.Value + ShowClockOnChangeFadeLength.Value)
                        {
                            _shownTime = 0;
                            _lastTimeString = NewTimeString;
                            if (ToggleClockKeyOnPress.Value == Toggle.Off || !ToggleClockKey.Value.IsPressed())
                                return;
                        }

                        alpha = (ShowClockOnChangeFadeLength.Value + ShowClockOnChangeFadeTime.Value - _shownTime) / ShowClockOnChangeFadeLength.Value;
                    }

                    _shownTime += Time.deltaTime;
                }

                Style.normal.textColor = new Color(ClockFontColor.Value.r, ClockFontColor.Value.g, ClockFontColor.Value.b, ClockFontColor.Value.a * alpha);
                Style2.normal.textColor = new Color(ClockShadowColor.Value.r, ClockShadowColor.Value.g, ClockShadowColor.Value.b, ClockShadowColor.Value.a * alpha);
                if ((ToggleClockKeyOnPress.Value == Toggle.Off && ShowingClock.Value == Toggle.On || ToggleClockKeyOnPress.Value == Toggle.On && (ShowClockOnChange.Value == Toggle.On || ToggleClockKey.Value.IsPressed())) && Hud.instance.IsVisible())
                {
                    GUI.backgroundColor = Color.clear;
                    _windowRect = GUILayout.Window(windowId, new Rect(_windowRect.position, _timeRect.size), Utilities.WindowBuilder, "");
                }
            }

            if (!Input.GetKey(KeyCode.Mouse0) && (_windowRect.x != _clockPosition.x || _windowRect.y != _clockPosition.y))
            {
                _clockPosition = new Vector2(_windowRect.x, _windowRect.y);
                ClockLocationString.Value = $"{_windowRect.x},{_windowRect.y}";
                Config.Save();
            }
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                AzuClockLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                AzuClockLogger.LogError($"There was an issue loading your {ConfigFileName}");
                AzuClockLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        public static ConfigEntry<Toggle> ShowingClock = null!;
        internal static ConfigEntry<Toggle> RealTime = null!;
        internal static ConfigEntry<Toggle> ShowClockOnChange = null!;
        internal static ConfigEntry<float> ShowClockOnChangeFadeTime = null!;
        internal static ConfigEntry<float> ShowClockOnChangeFadeLength = null!;
        internal static ConfigEntry<Toggle> ToggleClockKeyOnPress = null!;
        internal static ConfigEntry<Toggle> ClockUseOSFont = null!;
        internal static ConfigEntry<Toggle> ClockUseShadow = null!;
        internal static ConfigEntry<Color> ClockFontColor = null!;
        internal static ConfigEntry<Color> ClockShadowColor = null!;
        internal static ConfigEntry<int> ClockShadowOffset = null!;
        internal static ConfigEntry<string> ClockLocationString = null!;
        internal static ConfigEntry<int> ClockFontSize = null!;
        internal static ConfigEntry<KeyboardShortcut> ToggleClockKey = null!;
        internal static ConfigEntry<string> ClockFontName = null!;
        internal static ConfigEntry<string> ClockFormat = null!;
        internal static ConfigEntry<string> ClockString = null!;
        internal static ConfigEntry<TextAnchor> ClockTextAlignment = null!;
        internal static ConfigEntry<string> ClockFuzzyStrings = null!;

        internal static Font _clockFont = null!;
        internal static GUIStyle Style = null!;
        internal static GUIStyle Style2 = null!;
        internal static bool _configApplied;
        internal static Vector2 _clockPosition;
        internal static float _shownTime;
        internal static string _lastTimeString = "";
        internal static Rect _windowRect;
        internal static Rect _timeRect;
        internal static string NewTimeString = "";
        
        internal ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);
            return configEntry;
        }

        internal ConfigEntry<T> config<T>(string group, string name, T value, string description)
        {
            return Config.Bind(group, name, value, new ConfigDescription(description));
        }
        
        internal ConfigEntry<T> TextEntryConfig<T>(string group, string name, T value, string desc)
        {
            ConfigurationManagerAttributes attributes = new()
            {
                CustomDrawer = Utilities.TextAreaDrawer
            };
            return Config.Bind(group, name, value, new ConfigDescription(desc, null, attributes));
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        internal class AcceptableShortcuts : AcceptableValueBase // Used for KeyboardShortcut Configs 
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() => "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }

        #endregion
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class ZNetSceneAwakePatch
    {
        private static void Postfix()
        {
            if (AzuClockPlugin.ShowingClock.Value == AzuClockPlugin.Toggle.Off)
                return;

            Utilities.ApplyConfig();
        }
    }
}