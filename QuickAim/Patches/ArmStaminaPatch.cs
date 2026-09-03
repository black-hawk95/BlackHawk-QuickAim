using EFT;
using EFT.Animations;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace QuickAim.Patches
{
    /// <summary>
    /// Changes how fast arm stamina drains while aiming, based on your stance.
    ///
    /// Hooked on Player.LateUpdate, which runs EVERY FRAME while you are in a raid. That makes this
    /// the one place in the mod where cost matters, and the method is ordered accordingly: the
    /// cheapest and most selective checks come first, and the multiplier is only written when
    /// something has actually changed.
    /// </summary>
    internal class ArmStaminaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.LateUpdate));
        }

        // Last applied state, so the multiplier is written on change rather than every frame.
        private static bool _wasAiming;
        private static EPlayerPose _lastPose = EPlayerPose.Stand;
        private static bool _wasSupported;
        private static float _lastApplied = 1f;

        private const float PercentageDivisor = 100f;

        /// <summary>
        /// Clears the remembered state.
        ///
        /// Called when a raid ends, because the statics above would otherwise carry into the next
        /// one - and a stale "was aiming" would mean the first real aim looks like no change at
        /// all, so the multiplier never gets applied.
        /// </summary>
        public static void Reset()
        {
            _wasAiming = false;
            _lastPose = EPlayerPose.Stand;
            _wasSupported = false;
            _lastApplied = 1f;
        }

        [PatchPrefix]
        private static void Prefix(Player __instance)
        {
            // First, and cheapest. Also the multiplayer guard: in a Fika raid other players are
            // simulated on their own client, and writing to their stamina here would fight that.
            if (!__instance.IsYourPlayer) return;

            var animation = __instance.ProceduralWeaponAnimation;
            if (animation == null) return;

            var aiming = animation.IsAiming;

            // The common case by a wide margin: not aiming, and was not aiming last frame. Two
            // field reads and a return.
            if (!aiming && !_wasAiming) return;

            if (!Plugin.Enabled)
            {
                // Release the multiplier if the mod was holding one when it was switched off.
                if (_wasAiming) SetMultiplier(__instance, 1f);
                _wasAiming = aiming;
                return;
            }

            if (!aiming)
            {
                // Stopped aiming - hand stamina back to the game's own rate.
                SetMultiplier(__instance, 1f);
                _wasAiming = false;
                return;
            }

            var pose = __instance.Pose;
            var supported = IsSupported(animation);

            // Nothing has changed since the last frame, so there is nothing to write. This is the
            // common case while holding an aim, and it costs three comparisons.
            if (_wasAiming && pose == _lastPose && supported == _wasSupported) return;

            SetMultiplier(__instance, GetMultiplier(supported, pose));

            _wasAiming = true;
            _lastPose = pose;
            _wasSupported = supported;
        }

        /// <summary>
        /// True when the weapon is resting on something - mounted on a surface, or on a bipod.
        ///
        /// The original read a private _inMountedState field off MovementContext by reflection on
        /// every frame, and inferred bipod use from the player's movement state. Neither is needed:
        /// ProceduralWeaponAnimation exposes both as public properties.
        ///
        /// It is also the object this patch already has in hand - IsAiming is read from it a few
        /// lines above - so this costs two property reads and no lookup at all.
        /// </summary>
        private static bool IsSupported(ProceduralWeaponAnimation animation)
        {
            return animation.IsMountedState || animation.IsBipodUsed;
        }

        private static float GetMultiplier(bool supported, EPlayerPose pose)
        {
            if (supported) return Plugin.MountedDrain / PercentageDivisor;

            return pose switch
            {
                EPlayerPose.Prone => Plugin.ProneDrain / PercentageDivisor,
                EPlayerPose.Duck => Plugin.CrouchDrain / PercentageDivisor,
                _ => Plugin.StandingDrain / PercentageDivisor,
            };
        }

        /// <summary>
        /// Writes the multiplier, skipping the write when the value has not moved.
        ///
        /// Cheap insurance: another mod could be adjusting the same value, and writing an identical
        /// number every frame gains nothing while risking a fight over it.
        /// </summary>
        private static void SetMultiplier(Player player, float value)
        {
            if (Math.Abs(_lastApplied - value) < 0.001f) return;

            var physical = player.Physical;
            if (physical?.HandsStamina == null) return;

            physical.HandsStamina.Multiplier = value;
            _lastApplied = value;

            if (Plugin.LogDetails)
            {
                Plugin.LogSource?.LogInfo($"QuickAim: arm stamina drain set to {value:0.00}");
            }
        }
    }

    /// <summary>Clears the remembered state when a raid starts, so nothing carries over.</summary>
    internal class RaidStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        private static void Postfix() => ArmStaminaPatch.Reset();
    }
}
