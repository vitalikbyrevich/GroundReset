using BepInEx;
using BepInEx.Configuration;
using CodeMonkey.Utils;
using HarmonyLib;
using ServerSync;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS8632
namespace GroundReset
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        #region values
        internal const string ModName = "GroundReset", ModVersion = "1.0.1", ModGUID = "com.Frogger." + ModName;
        private static readonly Harmony harmony = new(ModGUID);
        public static Plugin _self;
        #endregion
        #region ConfigSettings
        static string ConfigFileName = "com.Frogger.BossDespawn.cfg";
        DateTime LastConfigChange;
        public static readonly ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = _self.Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
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
        internal static ConfigEntry<float> timePassedConfig;
        internal static ConfigEntry<bool> debugConfig;
        internal static float timeInMinutes;
        internal static float timePassed;
        internal static bool debug;
        #endregion
        internal static Action onTimer;

        internal static DateTime lastReset;



        private void Awake()
        {
            _self = this;

            #region config
            Config.SaveOnConfigSet = false;

            timeInMinutesConfig = config("General", "TheTriggerTime", 4320f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 312480)));
            timePassedConfig = config("DO NOT TOUCH", "time has passed since the last trigger", 0f, description: new ConfigDescription("", null, new ConfigurationManagerAttributes() { Browsable = false }));
            debugConfig = config("Debug", "Debug", false, "");

            SetupWatcherOnConfigFile();
            Config.ConfigReloaded += (_, _) => { UpdateConfiguration(); };
            Config.SaveOnConfigSet = true;
            Config.Save();
            #endregion
            onTimer += () =>
            {
                lastReset = DateTime.Now;
                FunctionTimer.Create(onTimer, timeInMinutes * 60, "JF_GroundReset", true, true);
                Debug("onTimer");
            };

            harmony.PatchAll();
        }

        private void Update()
        {
            if(debug && Input.GetKeyDown(KeyCode.P))
            {
                FunctionTimer.StopAllTimersWithName("JF_GroundReset");
                onTimer?.Invoke();
            }
        }

        #region tools
        public static void Debug(string msg, bool debugInAnyWay = false)
        {
            if(debug || debugInAnyWay) _self.DebugPrivate(msg);
        }
        private void DebugPrivate(string msg)
        {
            Logger.LogInfo(msg);
        }
        public void DebugError(string msg)
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
            if((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0)
            {
                return;
            }
            LastConfigChange = DateTime.Now;
            try
            {
                Config.Reload();
                Debug("Reloading Config...");
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
                if(timeInMinutes != timeInMinutesConfig.Value)
                {
                    FunctionTimer.Create(onTimer, timeInMinutes * 60, "JF_GroundReset", true, true);
                }
                timeInMinutes = timeInMinutesConfig.Value;

                timePassed = timePassedConfig.Value;
                debug = debugConfig.Value;
            });

            Task.WaitAll();
            Debug("Configuration Received");
        }
        #endregion
    }
}