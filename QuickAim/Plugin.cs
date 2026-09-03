using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using QuickAim.Patches;
using System;

namespace QuickAim
{
    [BepInPlugin("com.blackhawk.quickaim", "BlackHawk-QuickAim", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private const string SectionDrain = "1. Aim stamina";

        public static ManualLogSource LogSource;

        private static ConfigEntry<bool> _enabled;
        private static ConfigEntry<float> _standingDrain;
        private static ConfigEntry<float> _crouchDrain;
        private static ConfigEntry<float> _proneDrain;
        private static ConfigEntry<float> _mountedDrain;
        private static ConfigEntry<bool> _logDetails;

        public static bool Enabled => _enabled?.Value ?? true;
        public static float StandingDrain => _standingDrain?.Value ?? 100f;
        public static float CrouchDrain => _crouchDrain?.Value ?? 60f;
        public static float ProneDrain => _proneDrain?.Value ?? 30f;
        public static float MountedDrain => _mountedDrain?.Value ?? 30f;
        public static bool LogDetails => _logDetails?.Value ?? false;

        private void Awake()
        {
            LogSource = Logger;

            // A Fika headless client has no player aiming at anything.
            if (ModEnvironment.IsHeadlessClient)
            {
                Logger.LogInfo("QuickAim: headless client detected, plugin disabled.");
                return;
            }

            try
            {
                BindSettings();
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    $"QuickAim: settings could not be created, so defaults are in use. Reason: {ex.Message}");
            }

            // Each patch is enabled separately, so one failure cannot silently take out the other.
            TryEnable("arm stamina", new ArmStaminaPatch());
            TryEnable("raid start reset", new RaidStartPatch());

            Logger.LogInfo("QuickAim: startup complete.");
        }

        private void BindSettings()
        {
            _enabled = Config.Bind(SectionDrain, "Enabled", true,
                new ConfigDescription(
                    "Turn the whole mod on or off.\n\n" +
                    "With it off, aim stamina drains at the game's normal rate.",
                    null, new ConfigurationManagerAttributes { Order = 100 }));

            _standingDrain = Config.Bind(SectionDrain, "Standing", 100f,
                new ConfigDescription(
                    "How fast aim stamina drains while standing.\n\n" +
                    "100 is the game's normal rate. Lower means you can hold your aim longer.",
                    new AcceptableValueRange<float>(10f, 200f),
                    new ConfigurationManagerAttributes { Order = 99 }));

            _crouchDrain = Config.Bind(SectionDrain, "Crouching", 60f,
                new ConfigDescription(
                    "How fast aim stamina drains while crouched.\n\n" +
                    "Lower than standing by default - you can brace your elbow on your knee.",
                    new AcceptableValueRange<float>(10f, 200f),
                    new ConfigurationManagerAttributes { Order = 98 }));

            _proneDrain = Config.Bind(SectionDrain, "Prone", 30f,
                new ConfigDescription(
                    "How fast aim stamina drains while lying down.\n\n" +
                    "Low by default - the gun is resting on the ground.",
                    new AcceptableValueRange<float>(10f, 200f),
                    new ConfigurationManagerAttributes { Order = 97 }));

            _mountedDrain = Config.Bind(SectionDrain, "Mounted or on a bipod", 30f,
                new ConfigDescription(
                    "How fast aim stamina drains when the weapon is resting on something - mounted " +
                    "on a surface, or on a deployed bipod.\n\n" +
                    "Takes priority over your stance, since the weapon is supported either way.",
                    new AcceptableValueRange<float>(10f, 200f),
                    new ConfigurationManagerAttributes { Order = 96 }));

            _logDetails = Config.Bind(SectionDrain, "Log details to console", false,
                new ConfigDescription(
                    "Write each drain rate change to the BepInEx log. Only needed for reporting a bug.",
                    null, new ConfigurationManagerAttributes { Order = 90 }));
        }

        private void TryEnable(string description, SPT.Reflection.Patching.ModulePatch patch)
        {
            try
            {
                patch.Enable();
                Logger.LogInfo($"QuickAim: {description} enabled.");
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    $"QuickAim: could not enable {description}, so that part is off. Reason: {ex.Message}");
            }
        }
    }
}
