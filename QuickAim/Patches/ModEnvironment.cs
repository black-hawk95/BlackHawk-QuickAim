using Comfort.Common;
using EFT;
using EFT.Hideout;
using System;
using System.Linq;

namespace QuickAim.Patches
{
    /// <summary>
    /// Cheap guards, checked before the mod does anything.
    /// </summary>
    internal static class ModEnvironment
    {
        /// <summary>
        /// True on a Fika headless client - a server-side client with no player of its own.
        ///
        /// Nothing here applies: there is no one aiming. Detected by looking for Fika's headless
        /// plugin rather than referencing Fika directly, so this builds and runs fine without Fika
        /// installed.
        /// </summary>
        public static bool IsHeadlessClient
        {
            get
            {
                if (_isHeadless.HasValue) return _isHeadless.Value;

                _isHeadless = BepInEx.Bootstrap.Chainloader.PluginInfos.Keys
                    .Any(guid => guid.IndexOf("headless", StringComparison.OrdinalIgnoreCase) >= 0);

                return _isHeadless.Value;
            }
        }

        private static bool? _isHeadless;

        /// <summary>True when the player is in a raid rather than the hideout.</summary>
        public static bool IsInRaid =>
            Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is not HideoutGameWorld;
    }
}
