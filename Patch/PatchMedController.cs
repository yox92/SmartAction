using System.Collections.Generic;
using System.Reflection;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SmartAction.Utils;

namespace SmartAction.Patch;

public static class PatchMedEffectHooks
{
    public static readonly Dictionary<(Item item, EEffectState state), float> OriginalFloat12 = new();
    public static readonly Dictionary<(Item item, EEffectState state), float> OriginalWorkTime = new();
    public static readonly Dictionary<
        IEffect,
        (EPlayerState movementState, EEffectState effectState)
    > EffectUpdateCache = new();

    public static IEffect CurrentHealingEffect;

    [HarmonyPatch]
    public class PatchMedEffectAdded
    {
        private static MethodBase TargetMethod()
        {
            var medEffectType = ReflectionUtils.GetOrCacheNestedType(typeof(ActiveHealthController), "MedEffect");
            var method = ReflectionUtils.GetOrCacheMethod(medEffectType, "Added");
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            SmartActionLogger.Log("[MedEffect.Added] Postfix Added !");

            var (player, medItem, isValid) = ReflectionUtils.GetMedEffectContext(__instance, "Added");
            if (!isValid)
                return;

            var float12Field = ReflectionUtils.GetOrCacheField(__instance.GetType(), "float_12");

            if (__instance is not IEffect effect)
            {
                SmartActionLogger.Log($"[MedEffect.Added] Instance is not IEffect");
                return;
            }

            EffectUpdateCache.Remove(effect);
            EffectUpdateCache[effect] = (EPlayerState.None, EEffectState.None);
            CurrentHealingEffect = effect;
            LoopTime.RestoreOriginalLoopTime();

            var key = (medItem, effect.State);

            if (float12Field?.GetValue(__instance) is not (float workTime and > 0f and < 20f))
            {
                SmartActionLogger.Log($"[MedEffect.Added] Invalid work time field");
            }
            else
            {
                OriginalFloat12[key] = workTime;
                SmartActionLogger.Log($"[MedEffect.Added] save 💾" +
                                      $" WorkTime = {workTime:F2} for " +
                                      $"{medItem.Template._name} and {effect.State.ToString()}");
            }
        }

        [HarmonyPatch]
        public class PatchMedEffectStarted
        {
            private static MethodBase TargetMethod()
            {
                var medEffectType = ReflectionUtils.GetOrCacheNestedType(typeof(ActiveHealthController), "MedEffect");
                var method = ReflectionUtils.GetOrCacheMethod(medEffectType, "Started");
                return method;
            }

            [HarmonyPostfix]
            private static void Postfix(object __instance)
            {
                var (player, medItem, isValid) = ReflectionUtils.GetMedEffectContext(__instance, "Started");
                if (!isValid)
                    return;

                if (__instance is not IEffect effect)
                {
                    SmartActionLogger.Log($"[MedEffect.Started] Instance is not IEffect");
                    return;
                }

                SetActiveParamInterceptor.BlockSetActiveParam = false;
                SmartActionLogger.Log($"[MedEffect.Started] Postfix Started ! 🔓🔓🔓 SetActiveParam method8 free");

                EffectUpdateCache.Remove(effect);
                EffectUpdateCache[effect] = (EPlayerState.None, EEffectState.None);
                CurrentHealingEffect = effect;

                var key = (medItem, effect.State);

                var float12Field = ReflectionUtils.GetOrCacheField(__instance.GetType(), "float_12");
                var workStateTimeProperty = ReflectionUtils.GetOrCacheProperty(typeof(ActiveHealthController.GClass2813), "WorkStateTime");

                if (float12Field?.GetValue(__instance)
                        is not (float float12 and > 0f and < 20f) ||
                    workStateTimeProperty?.GetValue(__instance)
                        is not (float workStateTime and > 0f and < 20f))
                {
                    SmartActionLogger.Log($"[MedEffect.Started] Invalid float12 or work time field");
                }
                else
                {
                    OriginalFloat12[key] = float12;
                    OriginalWorkTime[key] = workStateTime;
                    SmartActionLogger.Log($"[MedEffect.Started] save 💾" +
                                          $" Float12 = {float12:F2}, WorkTime = {workStateTime:F2} for " +
                                          $"{medItem.Template._name} and {effect.State.ToString()}");
                }
            }

            [HarmonyPatch]
            public class PatchMedEffectResidue
            {
                private static MethodBase TargetMethod()
                {
                    var medEffectType =
                        ReflectionUtils.GetOrCacheNestedType(typeof(ActiveHealthController), "MedEffect");
                    var method = ReflectionUtils.GetOrCacheMethod(medEffectType, "Residue");
                    return method;
                }

                [HarmonyPostfix]
                private static void Postfix(object __instance)
                {
                    SmartActionLogger.Log("[MedEffect.Residue] Postfix Residue !");

                    var (player, medItem, isValid) = ReflectionUtils.GetMedEffectContext(__instance, "Residue");
                    if (!isValid)
                        return;

                    if (medItem != PatchDoMedEffect.LastHealingItem)
                        return;

                    SmartActionLogger.Log($"[MedEffect.Residue][Transpiler] 🔏🔏🔏 stop SetActiveParam method8");
                    SetActiveParamInterceptor.BlockSetActiveParam = true;

                    if (__instance is not IEffect effect)
                    {
                        SmartActionLogger.Log($"[MedEffect.Residue] Instance is not IEffect");
                        return;
                    }

                    EffectUpdateCache.Remove(effect);
                    EffectUpdateCache[effect] = (EPlayerState.None, EEffectState.None);
                    CurrentHealingEffect = effect;
                }
            }

            // [HarmonyPatch]
            // public class PatchMedEffectRemoved
            // {
            //     private static MethodBase TargetMethod()
            //     {
            //         return ReflectionUtils.GetOrCacheMethod(
            //             ReflectionUtils.GetOrCacheNestedType(typeof(ActiveHealthController), "MedEffect"),
            //             "Removed");
            //     }
            //
            //     [HarmonyPrefix]
            //     private static void Prefix(object __instance)
            //     {
            //         var (player, medItem, isValid) = ReflectionUtils.GetMedEffectContext(__instance, "Removed");
            //         if (!isValid)
            //             return;
            //
            //         SmartActionLogger.Log($"[MedEffect.Removed] Removed");
            //     }
            // }
        }
    }
}