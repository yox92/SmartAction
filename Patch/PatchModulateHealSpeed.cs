using System.Collections.Generic;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SmartAction.Utils;

namespace SmartAction.Patch
{
    [HarmonyPatch(typeof(Player), nameof(Player.ManualUpdate))]
    public class PatchModulateHealSpeed
    {
        public static Dictionary<IEffect, (EPlayerState movementState, EEffectState effectState)> EffectUpdateCache =
            new();

        public static void Postfix(Player __instance, float deltaTime)
        {
            // 🔐
            if (!__instance.IsYourPlayer)
                return;
            // 🔐
            var isAlive = __instance.ActiveHealthController?.IsAlive;
            if (isAlive == null)
                return;
            // 🔐
            if (!isAlive.Value)
                return;
            // 🔐
            if (__instance.HandsController is not Player.ItemHandsController hands)
                return;
            // 🔐
            if (hands.Item is not { } rawItem)
                return;
            // 🔐
            var medsItem = rawItem as MedsItemClass;
            var foodDrinkItem = rawItem as FoodDrinkItemClass;
            if (medsItem == null && foodDrinkItem == null)
                return;
            var id = rawItem.TemplateId;
            var stringId = id.ToString();
            // 🔐
            if (string.IsNullOrWhiteSpace(stringId))
                return;
            // 🔐
            if (stringId is not { Length: 24 })
                return;
            if (rawItem != PatchDoMedEffect.LastHealingItem)
                return;

            DropSurgery.CanDropSurgery(__instance, hands, id, medsItem);
            SpeedHealing.SpeedTicksByMovement(__instance, rawItem);
        }
    }
}