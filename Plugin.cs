using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.Animations;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SPTLeftStanceWallFix
{
    [BepInPlugin("com.pein.leftstancewallfix", "SPTLeftStanceWallFix", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static ConfigEntry<bool> ModEnabled;

        private void Awake()
        {
            LogSource = Logger;

            ModEnabled = Config.Bind(
                "Main",
                "Enabled",
                true,
                "Enable or disable Left Stance Wall Fix (prevents resetting left shoulder stance when colliding with walls)"
            );

            new DisableLeftStanceAnimPatch().Enable();
            new SetLeftStanceForcePatch().Enable();

            LogSource.LogInfo("SPTLeftStanceWallFix 1.0.0 (for SPT 4.1.6) loaded successfully!");
        }
    }

    public class DisableLeftStanceAnimPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type controllerType = typeof(MovementContext).GetProperty("LeftStanceController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.PropertyType
                                  ?? AccessTools.TypeByName("LeftStanceController")
                                  ?? typeof(LeftStanceController);

            return AccessTools.Method(controllerType, "DisableLeftStanceAnimFromBodyAction")
                   ?? AccessTools.Method(controllerType, "DisableLeftStanceAnimFromHandsAction");
        }

        [PatchPrefix]
        public static bool PatchPrefix()
        {
            if (!Plugin.ModEnabled.Value)
            {
                return true;
            }
            return false;
        }
    }

    public class SetLeftStanceForcePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ProceduralWeaponAnimation), "SetLeftStanceForce");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ProceduralWeaponAnimation __instance, ref bool force)
        {
            if (!Plugin.ModEnabled.Value)
            {
                return true;
            }

            if (!force && __instance.OverlapValue > 0f)
            {
                return false;
            }

            return true;
        }
    }
}
