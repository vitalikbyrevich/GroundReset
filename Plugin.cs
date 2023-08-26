﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using CodeMonkey.Utils;
using HarmonyLib;
using ServerSync;
using UnityEngine.SceneManagement;

namespace GroundReset;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    #region values

    internal const string ModName = "Frogger.GroundReset", ModVersion = "1.1.0", ModGUID = "com." + ModName;
    public static Plugin _self;
    internal static Action onTimer;
    internal static DateTime lastReset = DateTime.MinValue;
    internal static FunctionTimer timer;

    #endregion

    #region ConfigSettings

    static string ConfigFileName = "com.Frogger.GroundReset.cfg";
    DateTime LastConfigChange;

    public static readonly ConfigSync configSync = new(ModName)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigEntry<T> configEntry = _self.Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
    {
        setter(config.Value);
        config.SettingChanged += (_, _) => setter(config.Value);
    }

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    #endregion

    #region values

    internal static ConfigEntry<float> timeInMinutesConfig;
    internal static ConfigEntry<float> timePassedInMinutesConfig;
    internal static ConfigEntry<float> fuckingBugDistanceConfig;
    internal static ConfigEntry<float> savedTimeUpdateIntervalConfig;
    internal static float timeInMinutes = -1;
    internal static float timePassedInMinutes;
    internal static float fuckingBugDistance;
    internal static float savedTimeUpdateInterval;

    #endregion


    private void Awake()
    {
        _self = this;

        #region config

        Config.SaveOnConfigSet = false;

        _ = configSync.AddLockingConfigEntry(config("General", "Lock Configuration", true, ""));
        timeInMinutesConfig = config("General", "TheTriggerTime", 4320f, "");
        fuckingBugDistanceConfig = config("General", "Fucking Bug Distance", 115f, "");
        savedTimeUpdateIntervalConfig = config("General", "SavedTime Update Interval (seconds)", 120f, "");
        timePassedInMinutesConfig = config("DO NOT TOUCH", "time has passed since the last trigger", 0f,
            description: new ConfigDescription("", null,
                new ConfigurationManagerAttributes() { Browsable = false }));

        SetupWatcherOnConfigFile();
        Config.ConfigReloaded += (_, _) => { UpdateConfiguration(); };
        Config.SettingChanged += (_, _) => { UpdateConfiguration(); };

        Config.SaveOnConfigSet = true;
        Config.Save();

        #endregion

        onTimer += () => { ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ResetTerrain"); };

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGUID);
    }

    public void RPC_ResetTerrain(long _)
    {
        lastReset = DateTime.Now;
        FunctionTimer.Create(onTimer, timeInMinutes * 60, "JF_GroundReset", true, true);
        timePassedInMinutes = 0;
        Config.Reload();
        Log($"Подготовка к сбросу территории {DateTime.Now}");
        if (!ZNet.m_isServer)
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", new object[]
            {
                (int)MessageHud.MessageType.TopLeft,
                "<color=yellow><size=30>Подготовка к восстановлению ландшафта</size></color>"
            });
    }

    #region tools

    public static void Log(string msg) => _self.DebugPrivate(msg);

    public static void DebugError(object msg)
    {
        _self.DebugErrorPrivate(msg);
    }

    private void DebugPrivate(string msg)
    {
        Logger.LogInfo(msg);
    }

    private void DebugErrorPrivate(object msg)
    {
        Logger.LogError($"{msg} Write to the developer and moderator if this happens often.");
    }

    #endregion

    #region Config

    public void SetupWatcherOnConfigFile()
    {
        FileSystemWatcher fileSystemWatcherOnConfig = new(Paths.ConfigPath, ConfigFileName);
        fileSystemWatcherOnConfig.Changed += ConfigChanged;
        fileSystemWatcherOnConfig.IncludeSubdirectories = true;
        fileSystemWatcherOnConfig.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        fileSystemWatcherOnConfig.EnableRaisingEvents = true;
    }

    private void ConfigChanged(object sender, FileSystemEventArgs e)
    {
        if ((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0)
        {
            return;
        }

        LastConfigChange = DateTime.Now;
        try
        {
            Config.Reload();
            Log("Reloading Config...");
        }
        catch
        {
            DebugError("Can't reload Config");
        }
    }

    private void UpdateConfiguration()
    {
        Task task = null;
        task = Task.Run(() =>
        {
            if (timeInMinutes != -1 && timeInMinutes != timeInMinutesConfig.Value &&
                SceneManager.GetActiveScene().name == "main")
            {
                FunctionTimer.StopAllTimersWithName("JF_GroundReset");
                FunctionTimer.Create(onTimer, timeInMinutes * 60, "JF_GroundReset", true, true);
            }

            timeInMinutes = timeInMinutesConfig.Value;
            timePassedInMinutes = timePassedInMinutesConfig.Value;
            fuckingBugDistance = fuckingBugDistanceConfig.Value;
            savedTimeUpdateInterval = savedTimeUpdateIntervalConfig.Value;
        });

        Task.WaitAll();
        Log("Configuration Received");
    }

    #endregion
}
