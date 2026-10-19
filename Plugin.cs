using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SPTLeftStanceWallFix
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.m1starkot.leftstancewallfix";
        public const string PluginName = "SPTLeftStanceWallFix";
        public const string PluginVersion = "1.0.0";

        public static ManualLogSource LogSource { get; private set; }
        public static ConfigEntry<bool> ModEnabled { get; private set; }

        private void Awake()
        {
            LogSource = Logger;

            ModEnabled = Config.Bind(
                "Main",
                "Enabled",
                true,
                "Enable or disable Left Stance Wall Fix (prevents resetting left shoulder stance when colliding with walls)"
            );

            try
            {
                new DisableLeftStanceAnimPatch().Enable();

                if (DisableLeftStanceFromHandsPatch.FindMethod() != null)
                {
                    new DisableLeftStanceFromHandsPatch().Enable();
                }

                LogSource.LogInfo($"{PluginName} v{PluginVersion} by m1starkot (for SPT 4.1.6) loaded successfully!");
            }
            catch (Exception ex)
            {
                LogSource.LogError($"Failed to initialize {PluginName}: {ex}");
            }
        }
    }

    // Main Patch: Prevents resetting left shoulder stance from body/wall collision actions in LeftStanceController
    public class DisableLeftStanceAnimPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type controllerType = typeof(MovementContext).GetProperty("LeftStanceController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.PropertyType
                                  ?? AccessTools.TypeByName("LeftStanceController")
                                  ?? typeof(LeftStanceController);

            return AccessTools.Method(controllerType, "DisableLeftStanceAnimFromBodyAction");
        }

        [PatchPrefix]
        public static bool PatchPrefix()
        {
            return !Plugin.ModEnabled.Value;
        }
    }

    // Secondary Patch (enabled if method exists): Prevents resetting left shoulder stance from hands actions
    public class DisableLeftStanceFromHandsPatch : ModulePatch
    {
        public static MethodBase FindMethod()
        {
            Type controllerType = typeof(MovementContext).GetProperty("LeftStanceController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.PropertyType
                                  ?? AccessTools.TypeByName("LeftStanceController")
                                  ?? typeof(LeftStanceController);

            return AccessTools.Method(controllerType, "DisableLeftStanceAnimFromHandsAction");
        }

        protected override MethodBase GetTargetMethod()
        {
            return FindMethod();
        }

        [PatchPrefix]
        public static bool PatchPrefix()
        {
            return !Plugin.ModEnabled.Value;
        }
    }
}
