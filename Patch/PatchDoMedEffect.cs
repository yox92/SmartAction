using System.Collections;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SmartAction.Utils;
using UnityEngine;

namespace SmartAction.Patch
{
    [HarmonyPatch(typeof(ActiveHealthController), "DoMedEffect")]
    public static class PatchDoMedEffect
    {
        private static Coroutine _monitorCoroutine;
        private static float _monitorStartTime;
        public static Item LastHealingItem;

        [HarmonyPrefix]
        public static void Prefix(
            ActiveHealthController __instance,
            Item item,
            EBodyPart bodyPart,
            float? amount = null)
        {
            SmartActionLogger.Log($"[DoMedEffect] Prefix");
            if (__instance == null)
                return;
            // 🔐
            if (__instance.Player == null)
                return;
            // 🔐
            if (!__instance.Player.IsYourPlayer)
                return;
            // 🔐
            if (!__instance.IsAlive)
                return;
            // 🔐
            if (item == null)
                return;
            var medsItem = item as MedsItemClass;
            var foodDrinkItem = item as FoodDrinkItemClass;
            // 🔐
            if (medsItem == null && foodDrinkItem == null)
                return;
            LastHealingItem = item;
            PatchMedEffectHooks.CurrentHealingEffect = null;
            PatchMedEffectHooks.EffectUpdateCache.Clear();
            SmartActionLogger.Log($"[Prefix-DoMedEffect] 🧹🧹🧹 Clear cache");
        }

        [HarmonyPostfix]
        public static void Postfix(
            ActiveHealthController __instance,
            Item item,
            EBodyPart bodyPart,
            float? amount = null)
        {
            SmartActionLogger.Log($"[DoMedEffect] Postfix");
            if (__instance == null)
                return;
            var player = __instance.Player;
            if (player == null)
                return;
            // 🔐
            if (!__instance.Player.IsYourPlayer)
                return;
            // 🔐
            if (!__instance.IsAlive)
                return;
            // 🔐
            if (item is not MedsItemClass medsItem)
                return;
            // 🔐
            if (LastHealingItem != item)
                return;
            // 🔐
            if (bodyPart == EBodyPart.Common)
                return;
            var hpResourceRate = medsItem.MedKitComponent?.HpResourceRate;
            // 🔐
            if (hpResourceRate is null or <= 0f)
            {
                SmartActionLogger.Log("[DoMedEffect] ⚠️ HpResourceRate is null or <= 0");
                return;
            }
            else
            {
                SmartActionLogger.Log($"[DoMedEffect] ℹ️ HpResourceRate = {hpResourceRate}");
            }

            Start(__instance.Player, bodyPart);
        }

        private static void Start(Player player, EBodyPart body)
        {
            if (_monitorCoroutine != null)
            {
                CoroutineRunner.Stop(_monitorCoroutine);
                _monitorCoroutine = null;
            }

            _monitorStartTime = Time.realtimeSinceStartup;
            _monitorCoroutine = CoroutineRunner.Run(MonitorHealing(player, body));
        }

        private static IEnumerator MonitorHealing(Player player, EBodyPart body)
        {
            const float durationLimit = 60f;

            while (Time.realtimeSinceStartup - _monitorStartTime < durationLimit)
            {
                yield return new WaitForSeconds(0.1f);

                var effect = PatchMedEffectHooks.CurrentHealingEffect;
                if (effect is not { State: EEffectState.Started })
                {
                    continue;
                }

                var isHpFull = player.HealthController.GetBodyPartHealth(body, true).AtMaximum;
                if (!isHpFull)
                {
                    continue;
                }

                SmartActionLogger.Log($"[DoMedEffect] ==> Residue");
                TryAdvanceEffectToEnd(effect, player);
                break;
            }

            SmartActionLogger.Log(
                $"[DoMedEffect] monitoring end - Durée totale: {Time.realtimeSinceStartup - _monitorStartTime}s");
            _monitorCoroutine = null;
        }

        private static void TryAdvanceEffectToEnd(IEffect effect, Player player)
        {
            var type = effect.GetType();
            while (type != null)
            {
                var workTimeField = ReflectionUtils.FindField(type, "float_12");
                if (workTimeField != null)
                {
                    workTimeField.SetValue(effect, 0f);
                    SmartActionLogger.Log($"[DoMedEffect] float_12 forcé à 0");
                    break;
                }

                type = type.BaseType;
            }
        }
    }
    
    
}