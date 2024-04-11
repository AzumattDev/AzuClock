﻿using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using UnityEngine;

namespace AzuClock;

public class Utilities
{
    internal static void WindowBuilder(int id)
    {
        AzuClockPlugin._timeRect =
            GUILayoutUtility.GetRect(new GUIContent(AzuClockPlugin.NewTimeString), AzuClockPlugin.Style);

        GUI.DragWindow(AzuClockPlugin._timeRect);

        if (AzuClockPlugin.ClockUseShadow.Value == AzuClockPlugin.Toggle.On)
            GUI.Label(
                new Rect(
                    AzuClockPlugin._timeRect.position + new Vector2(-AzuClockPlugin.ClockShadowOffset.Value,
                        AzuClockPlugin.ClockShadowOffset.Value),
                    AzuClockPlugin._timeRect.size), AzuClockPlugin.NewTimeString, AzuClockPlugin.Style2);
        GUI.Label(AzuClockPlugin._timeRect, AzuClockPlugin.NewTimeString, AzuClockPlugin.Style);
    }

    internal static void ApplyConfig()
    {
        string[]? split = AzuClockPlugin.ClockLocationString.Value.Split(',');
        AzuClockPlugin._clockPosition = new Vector2(
            split[0].Trim().EndsWith("%")
                ? float.Parse(split[0].Trim().Substring(0, split[0].Trim().Length - 1)) / 100f * Screen.width
                : float.Parse(split[0].Trim()),
            split[1].Trim().EndsWith("%")
                ? float.Parse(split[1].Trim().Substring(0, split[1].Trim().Length - 1)) / 100f * Screen.height
                : float.Parse(split[1].Trim()));

        AzuClockPlugin._windowRect = new Rect(AzuClockPlugin._clockPosition, new Vector2(1000, 100));

        if (AzuClockPlugin.ClockUseOSFont.Value == AzuClockPlugin.Toggle.On)
        {
            AzuClockPlugin._clockFont = Font.CreateDynamicFontFromOSFont(AzuClockPlugin.ClockFontName.Value,
                AzuClockPlugin.ClockFontSize.Value);
        }
        else
        {
            AzuClockPlugin.AzuClockLogger.LogDebug("Getting fonts");
            Font[]? fonts = Resources.FindObjectsOfTypeAll<Font>();
            foreach (Font? font in fonts)
                if (font.name == AzuClockPlugin.ClockFontName.Value)
                {
                    AzuClockPlugin._clockFont = font;
                    AzuClockPlugin.AzuClockLogger.LogDebug($"Got font {font.name}");
                    break;
                }
        }

        AzuClockPlugin.Style = new GUIStyle
        {
            richText = true,
            fontSize = AzuClockPlugin.ClockFontSize.Value,
            alignment = AzuClockPlugin.ClockTextAlignment.Value,
            font = AzuClockPlugin._clockFont
        };
        AzuClockPlugin.Style2 = new GUIStyle
        {
            richText = true,
            fontSize = AzuClockPlugin.ClockFontSize.Value,
            alignment = AzuClockPlugin.ClockTextAlignment.Value,
            font = AzuClockPlugin._clockFont
        };

        AzuClockPlugin._configApplied = true;
    }

    private static string GetCurrentTimeString(DateTime theTime, float fraction, int days)
    {
        string[]? fuzzyStringArray = AzuClockPlugin.ClockFuzzyStrings.Value.Split(',');

        int idx = Math.Min((int)(fuzzyStringArray.Length * fraction), fuzzyStringArray.Length - 1);

        if (AzuClockPlugin.ClockFormat.Value == "fuzzy")
            return string.Format(AzuClockPlugin.ClockString.Value, fuzzyStringArray[idx]);

        return string.Format(AzuClockPlugin.ClockString.Value, theTime.ToString(AzuClockPlugin.ClockFormat.Value), fuzzyStringArray[idx], days.ToString());
    }

    internal static string GetCurrentTimeString()
    {
        if (!EnvMan.instance)
            return "";
        float fraction = EnvMan.instance.m_smoothDayFraction;

        int hour = (int)(fraction * 24);
        int minute = (int)((fraction * 24 - hour) * 60);
        int second = (int)(((fraction * 24 - hour) * 60 - minute) * 60);

        DateTime now = DateTime.Now;
        DateTime theTime = new(now.Year, now.Month, now.Day, hour, minute, second);
        int days = EnvMan.instance.GetCurrentDay();
        return GetCurrentTimeString(theTime, fraction, days);
    }
    internal static string GetRealCurrentTimeString()
    {
        DateTime theTime = DateTime.Now;
        float fraction = (theTime.Hour * 60 * 60 + theTime.Minute * 60 + theTime.Second) / 24 * 60 * 60;

        return GetCurrentTimeString(theTime, fraction, 0);
    }
    
    public static bool IgnoreKeyPresses(bool extra = false)
    {
        if (!extra)
            return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() ||
                   Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() ||
                   Chat.instance?.HasFocus() == true;
        return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() ||
               Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() ||
               Chat.instance?.HasFocus() == true || StoreGui.IsVisible() || InventoryGui.IsVisible() ||
               Menu.IsVisible() || TextViewer.instance?.IsVisible() == true;
    }

    internal static void GenerateConfigs()
    {
        AzuClockPlugin.ShowingClock = AzuClockPlugin.context.config("1 - General", "Show Clock", AzuClockPlugin.Toggle.On, "Show the clock?");
        AzuClockPlugin.ToggleClockKey = AzuClockPlugin.context.config("1 - General", "Show Clock Key", new KeyboardShortcut(KeyCode.Home), new ConfigDescription("Key(s) used to toggle the clock display. use https://docs.unity3d.com/Manual/ConventionalGameInput.html", new AzuClockPlugin.AcceptableShortcuts()));
        AzuClockPlugin.ClockLocationString = AzuClockPlugin.context.config("1 - General", "Clock Location String", "48%,0%", "Location on the screen to show the clock (x,y) or (x%,y%)");
        AzuClockPlugin.RealTime = AzuClockPlugin.context.config("1 - General", "Show Realtime", AzuClockPlugin.Toggle.Off, "If on, it will show the real time instead of the game time");
        AzuClockPlugin.ShowClockOnChange = AzuClockPlugin.context.config("1 - General", "Show Clock On Change", AzuClockPlugin.Toggle.Off, "Only show the clock when the time changes?");
        AzuClockPlugin.ShowClockOnChangeFadeTime = AzuClockPlugin.context.config("1 - General", "Show Clock On Change Fade Time", 5f, "If only showing on change, length in seconds to show the clock before begining to fade");
        AzuClockPlugin.ShowClockOnChangeFadeLength = AzuClockPlugin.context.config("1 - General", "Show Clock On Change Fade Length", 1f, "How long fade should take in seconds");
        AzuClockPlugin.ClockUseOSFont = AzuClockPlugin.context.config("1 - General", "Clock Use OS Font", AzuClockPlugin.Toggle.Off, "Set to true to specify the name of a font from your OS; otherwise limited to fonts in the game resources");
        AzuClockPlugin.ClockUseShadow = AzuClockPlugin.context.config("1 - General", "Clock Use Shadow", AzuClockPlugin.Toggle.Off, "Add a shadow behind the text");
        AzuClockPlugin.ClockShadowOffset = AzuClockPlugin.context.config("1 - General", "Clock Shadow Offset", 2, "Shadow offset in pixels");
        AzuClockPlugin.ClockFontName = AzuClockPlugin.context.config("1 - General", "Clock Font Name", "AveriaSerifLibre-Bold", "Name of the font to use");
        AzuClockPlugin.ClockFontSize = AzuClockPlugin.context.config("1 - General", "Clock Font Size", 24, "Location on the screen in pixels to show the clock");
        AzuClockPlugin.ClockFontColor = AzuClockPlugin.context.config("1 - General", "Clock Font Color", Color.white, "Font color for the clock");
        AzuClockPlugin.ClockShadowColor = AzuClockPlugin.context.config("1 - General", "Clock Shadow Color", Color.black, "Color for the shadow");
        AzuClockPlugin.ToggleClockKeyOnPress = AzuClockPlugin.context.config("1 - General", "Show Clock Key On Press", AzuClockPlugin.Toggle.Off, "If true, limit clock display to when the hotkey is down");
        AzuClockPlugin.ClockFormat = AzuClockPlugin.context.config("1 - General", "Clock Format", "HH:mm", "Time format; set to 'fuzzy' for fuzzy time");
        AzuClockPlugin.ClockString = AzuClockPlugin.context.TextEntryConfig("Clock", "Clock String", "<b>{0}</b>", "Formatted clock string - {0} is replaced by the actual time string, {1} is replaced by the fuzzy string, {2} is replaced by the current day");
        AzuClockPlugin.ClockTextAlignment = AzuClockPlugin.context.config("1 - General", "Clock Text Alignment", TextAnchor.MiddleCenter, "Clock text alignment.");
        AzuClockPlugin.ClockFuzzyStrings = AzuClockPlugin.context.TextEntryConfig("Clock", "Clock Fuzzy Strings", "Midnight,Early Morning,Early Morning,Before Dawn,Before Dawn,Dawn,Dawn,Morning,Morning,Late Morning,Late Morning,Midday,Midday,Early Afternoon,Early Afternoon,Afternoon,Afternoon,Evening,Evening,Night,Night,Late Night,Late Night,Midnight", "Fuzzy time strings to split up the day into custom periods if ClockFormat is set to 'fuzzy'; comma-separated");

        AzuClockPlugin.NewTimeString = "";
        AzuClockPlugin.Style = new GUIStyle
        {
            richText = true,
            fontSize = AzuClockPlugin.ClockFontSize.Value,
            alignment = AzuClockPlugin.ClockTextAlignment.Value
        };
        AzuClockPlugin.Style2 = new GUIStyle
        {
            richText = true,
            fontSize = AzuClockPlugin.ClockFontSize.Value,
            alignment = AzuClockPlugin.ClockTextAlignment.Value
        };
    }

    internal static void AutoDoc()
    {
#if DEBUG

        // Store Regex to get all characters after a [
        Regex regex = new(@"\[(.*?)\]");

        // Strip using the regex above from Config[x].Description.Description
        string Strip(string x) => regex.Match(x).Groups[1].Value;
        StringBuilder sb = new();
        string lastSection = "";
        foreach (ConfigDefinition x in AzuClockPlugin.context.Config.Keys)
        {
            // skip first line
            if (x.Section != lastSection)
            {
                lastSection = x.Section;
                sb.Append($"{Environment.NewLine}`{x.Section}`{Environment.NewLine}");
            }

            sb.Append($"\n{x.Key} [{Strip(AzuClockPlugin.context.Config[x].Description.Description)}]" +
                      $"{Environment.NewLine}   * {AzuClockPlugin.context.Config[x].Description.Description.Replace("[Synced with Server]", "").Replace("[Not Synced with Server]", "")}" +
                      $"{Environment.NewLine}     * Default Value: {AzuClockPlugin.context.Config[x].GetSerializedValue()}{Environment.NewLine}");
        }

        File.WriteAllText(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                $"{AzuClockPlugin.ModName}_AutoDoc.md"), sb.ToString());
#endif
    }

    internal static void TextAreaDrawer(ConfigEntryBase entry)
    {
        GUILayout.ExpandHeight(true);
        GUILayout.ExpandWidth(true);
        entry.BoxedValue = GUILayout.TextArea((string)entry.BoxedValue, GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));
    }
}