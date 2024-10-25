using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using BepInEx;
using HarmonyLib;
using Projectiles;
using UnityEngine;
using AimAssist;
using Enemy;
using System;

namespace JusticeForPunchDash
{
    [BepInPlugin(pluginGuid, pluginName, pluginversion)]
    public class Mod : BaseUnityPlugin
    {
        public const string pluginGuid = "kestrel.iamyourbeast.justiceforpunchdash";
        public const string pluginName = "Justice For Punch Dash";
        public const string pluginversion = "1.1.2";

        public static bool successfulHit = false;

        public static ConfigEntry<bool> dontSaveTimes;

        public void Awake() {
            Logger.LogInfo("Hiiiiiiiiiiii :3");

            dontSaveTimes = Config.Bind("Options", "Stop Saving Times", true, "Stops times from being saved while the mod is active to avoid accidental overwriting");

            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll();
        }
    }

    //This patch is essentially just the important parts of the original demo Assembly-CSharp UseEquipment function reimplemented. this does mean some things will run twice but that shouldn't cause too many issues. i hope
    [HarmonyPatch(typeof(PlayerMeleeArmed), nameof(PlayerMeleeArmed.UseEquipment))]
    public class PatchMeleeEquipment
    {
        [HarmonyPostfix]
        public static void Postfix(ref WeaponPickup ___pickup) {

            bool flag = false;
            Vector3 pos = Vector3.zero;
            Vector3 goal = Vector3.zero;
            bool flag2 = true;

            if (___pickup && ___pickup.GetWeaponDetails().GetProjectilePrefab().GetComponent<SpherecastProjectile>().GetCollisionType() == SpherecastProjectile.CollisionType.AllTargets) {
                flag2 = false;
            }

            AimTarget highlightedTarget = GameManager.instance.player.GetAimManager().GetHighlightedTarget();

            if (highlightedTarget) {
                pos = highlightedTarget.GetCenter();
                Enemy.Enemy enemy = highlightedTarget.GetEnemy();
                if (enemy && flag2 && enemy is EnemyHuman) {
                    flag = true;
                    goal = enemy.transform.position + Vector3.Normalize(GameManager.instance.player.GetMovementScript().transform.position - enemy.transform.position) * 1.5f;
                }


                if (Mod.successfulHit) {
                    float duration = 0.125f;
                    GameManager.instance.player.GetLookScript().TriggerTimedLockOn(pos, duration, true);

                    if (flag) {
                        GameManager.instance.player.GetMovementScript().TriggerTimedLockOn(goal, duration, false);
                    }
                }
            }
        }
    }

    //We know that the previous patch will be run after UseEquipment has been called, so this is a good enough solution that avoids calling base class methods from within a patch (nasty)
    [HarmonyPatch(typeof(PlayerWeaponArmed), nameof(PlayerWeaponArmed.UseEquipment))]
    public class PatchWeaponEquipment
    {
        [HarmonyPostfix]
        public static void Postfix(bool __result) {
            Mod.successfulHit = __result;
        }
    }

    //Blocks saving of times with only two (2) horrible one-liners!!
    [HarmonyPatch(typeof(UILevelCompleteTimeScoreBar), nameof(UILevelCompleteTimeScoreBar.Initialize))]
    public class DontSaveTimes
    {
        [HarmonyPrefix]
        public static void Prefix(out float __state) {
            __state = GameManager.instance.progressManager.GetLevelData(GameManager.instance.levelController.GetInformationSetter().GetInformation()).GetBestTime();
        }

        [HarmonyPostfix]
        public static void Postfix(float __state) {
            if (Mod.dontSaveTimes.Value) GameManager.instance.progressManager.GetLevelData(GameManager.instance.levelController.GetInformationSetter().GetInformation()).SetNewBestTime(__state);
        }
    }


}