using System;
using System.Reflection;
using System.Collections.Generic;
using AnimationEventSystem;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SmartAction.Utils;

namespace SmartAction.Devs
{
//     [HarmonyPatch(typeof(WeaponAnimationSpeedControllerClass), "SetActive")]
//     public class PatchSetActive
//     {
//         [HarmonyPrefix]
//         public static void Prefix_SetActive(IAnimator animator, bool active)
//         {
//             SmartActionLogger.Log($"[WeaponAnimationSpeedControllerClass] 🔄 SetActive → Animator: {animator} | Active: {active}");
//         }
//     }
//     
//     [HarmonyPatch(typeof(ActiveHealthController), "add_EffectRemovedEvent")]
//     public static class PatchAddEffectRemovedEvent
//     {
//         [HarmonyPrefix]
//         public static void Prefix(ActiveHealthController __instance, Action<IEffect> value)
//         {
//             SmartActionLogger.Log($"[EffectRemovedEvent] 🔗 Abonnement détecté : {value.Method.Name}");
//         }
//     }
//
//     [HarmonyPatch(typeof(ActiveHealthController), "remove_EffectRemovedEvent")]
//     public static class PatchEffectRemovedEventRemove
//     {
//         [HarmonyPrefix]
//         public static void Prefix(ActiveHealthController __instance, Action<IEffect> value)
//         {
//             SmartActionLogger.Log($"[EffectRemovedEvent] ❌ Désabonnement détecté : {value.Method.Name}");
//         }
//     }
//
//     [HarmonyPatch(typeof(Player.MedsController.Class1172))]
//     public class PatchClass1172
//     {
//         [HarmonyPatch("method_8")]
//         [HarmonyPrefix]
//         public static void Prefix_Method8(Player.MedsController.Class1172 __instance, IEffect effect)
//         {
//             SmartActionLogger.Log("[Class1172] 🔄 method_8 appelé !");
//
//             if (effect != null)
//             {
//                 var effectType = effect.GetType().Name;
//                 SmartActionLogger.Log($"[Class1172] 🔍 Type de l'effet : {effectType}");
//             }
//
//             var firearmsAnimatorField = typeof(Player.MedsController)
//                 .GetField(
//                     "firearmsAnimator_0",
//                     System.Reflection.BindingFlags.Instance |
//                     System.Reflection.BindingFlags.NonPublic);
//
//             if (firearmsAnimatorField == null)
//                 return;
//             var medsControllerField = typeof(Player.MedsController.Class1172)
//                 .GetField(
//                     "medsController_0",
//                     System.Reflection.BindingFlags.Instance |
//                     System.Reflection.BindingFlags.NonPublic);
//
//             if (medsControllerField == null)
//                 return;
//
//             var medsController = medsControllerField.GetValue(__instance);
//             if (medsController == null)
//                 return;
//
//             if (firearmsAnimatorField.GetValue(medsController) is FirearmsAnimator { Animator: not null } firearmsAnimator)
//             {
//                 SmartActionLogger.Log($"[Class1172] 🔄 method_8 → Animator State Active: {firearmsAnimator.Animator.enabled}");
//             }
//             
//             SmartActionLogger.Log("[PatchMethod8] ✅ Execution allow method_8.");
//         }
//
//         [HarmonyPatch("HideWeapon")]
//         [HarmonyPrefix]
//         public static void Prefix_HideWeapon(Player.MedsController.Class1172 __instance)
//         {
//             SmartActionLogger.Log("[Class1172::HideWeapon] 🔍 HideWeapon appelé");
//         }
//
//         [HarmonyPatch("HideWeaponComplete")]
//         [HarmonyPrefix]
//         public static void Prefix_HideWeaponComplete(Player.MedsController.Class1172 __instance)
//         {
//             SmartActionLogger.Log("[Class1172::HideWeaponComplete] 🔍 HideWeapon appelé");
//         }
//     }
//
//
//     [HarmonyPatch]
//     public class PatchMethod5
//     {
//         private static MethodBase TargetMethod()
//         {
//             var playerType = typeof(Player);
//             var medsControllerType = playerType.GetNestedType("MedsController", BindingFlags.NonPublic | BindingFlags.Public);
//             if (medsControllerType == null)
//             {
//                 SmartActionLogger.Warn("[Patch] ❌ Player.MedsController introuvable");
//                 return null;
//             }
//
//             var class1172Type =
//                 medsControllerType.GetNestedType("Class1172", BindingFlags.NonPublic | BindingFlags.Public);
//             if (class1172Type == null)
//             {
//                 SmartActionLogger.Warn("[Patch] ❌ Class1172 introuvable dans MedsController");
//                 return null;
//             }
//
//             var method = class1172Type.GetMethod("method_5",
//                 BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
//             if (method != null)
//                 return method;
//             SmartActionLogger.Warn("[Patch] ❌ method_5 introuvable dans Class1172");
//             return null;
//         }
//
//         [HarmonyPostfix]
//         private static void Postfix(object __instance)
//         {
//             var stateProp = __instance.GetType().GetProperty("State", BindingFlags.Instance | BindingFlags.Public);
//             var state = stateProp?.GetValue(__instance);
//
//             var queueField = __instance.GetType()
//                 .GetField("queue_0", BindingFlags.Instance | BindingFlags.NonPublic);
//             var queue = queueField?.GetValue(__instance) as Queue<EBodyPart>;
//
//             var floatField = __instance.GetType()
//                 .GetField("float_0", BindingFlags.Instance | BindingFlags.NonPublic);
//             var amount = (float?)floatField?.GetValue(__instance);
//
//             SmartActionLogger.Log(
//                 $"[method_5] 🛠️ Relance de soin Etat de la queue : {queue?.Count} | Montant : {amount}");
//
//             var medsControllerField = __instance.GetType()
//                 .GetField("medsController_0", BindingFlags.Instance | BindingFlags.NonPublic);
//             var medsController = medsControllerField?.GetValue(__instance);
//
//             var playerField = medsController?.GetType().GetField("_player",
//                 BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//             var player = playerField?.GetValue(medsController) as Player;
//
//             if (player?.HandsController is Player.ItemHandsController handsController)
//             {
//                 var animator = handsController.FirearmsAnimator.Animator;
//
//                 if (animator != null)
//                 {
//                     for (var i = 0; i < animator.layerCount; i++)
//                     {
//                         var stateInfo = animator.GetCurrentAnimatorStateInfo(i);
//                         var animationName = animator.GetStateName(stateInfo);
//                         SmartActionLogger.Log(
//                             $"[method_5] 🎞️ Animation détectée - Layer[{i}]: {animationName} | Loop: {stateInfo.loop}");
//                     }
//                 }
//                 else
//                 {
//                     SmartActionLogger.Log("[method_5] ❌ Animator introuvable dans FirearmsAnimator");
//                 }
//             }
//
//             SmartActionLogger.Log($"[method_5] ✅ Fin de method_5 | State={state} | MedsController={medsController}");
//         }
//     }
//
//     [HarmonyPatch]
//     public class PatchWeaponAnimationSpeedController
//     {
//         [HarmonyPatch(typeof(WeaponAnimationSpeedControllerClass), "SetUseLeftHand")]
//         [HarmonyPrefix]
//         public static void Prefix_SetUseLeftHand(IAnimator animator, bool useLeftHand)
//         {
//             SmartActionLogger.Log(
//                 $"[WeaponAnimationSpeedControllerClass] 🔄 SetUseLeftHand → Animator: {animator} | UseLeftHand: {useLeftHand}");
//         }
//
//         [HarmonyPatch(typeof(WeaponAnimationSpeedControllerClass), "SetInventory")]
//         [HarmonyPrefix]
//         public static void Prefix_SetInventory(IAnimator animator, bool inventory)
//         {
//             SmartActionLogger.Log(
//                 $"[WeaponAnimationSpeedControllerClass] 🔄 SetInventory → Animator: {animator} | Inventory: {inventory}");
//         }
//
//         [HarmonyPatch(typeof(WeaponAnimationSpeedControllerClass), "SetQuickFire")]
//         [HarmonyPrefix]
//         public static void Prefix_SetQuickFire(IAnimator animator, bool quickFire)
//         {
//             SmartActionLogger.Log(
//                 $"[WeaponAnimationSpeedControllerClass] 🔄 SetQuickFire → Animator: {animator} | QuickFire: {quickFire}");
//         }
//     }
//
//     [HarmonyPatch(typeof(ActiveHealthController.GClass2813))]
//     public class PatchGClass2813
//     {
//         [HarmonyPatch("method_0")]
//         [HarmonyPrefix]
//         public static void Prefix_Method0(ActiveHealthController.GClass2813 __instance)
//         {
//             SmartActionLogger.Log("[GClass2813::method_0] 🔍 method_0 appelé !");
//         }
//     }
//
//     [HarmonyPatch(typeof(AnimationEventsContainer))]
//     public class PatchAnimationEventsContainer
//     {
//         [HarmonyPatch("method_3")]
//         [HarmonyPrefix]
//         public static void Prefix_OnWeapOut()
//         {
//             SmartActionLogger.Log("[IEventsConsumer] 🔍 method_3 appelé  AnimationEventsContainer");
//         }
//     }
//
//     [HarmonyPatch(typeof(Player.BaseAnimationOperationClass), "set_State")]
//     public class PatchBaseAnimationOperationClassState
//     {
//         [HarmonyPrefix]
//         public static void Prefix(ref Player.EOperationState value)
//         {
//             SmartActionLogger.Log($"[Patch]  Changement d'état détecté: {value}");
//
//             if (value == Player.EOperationState.Finished)
//             {
//                 SmartActionLogger.Warn($" [Patch] Transition vers Finished détectée !");
//             }
//         }
//     }
//
//     [HarmonyPatch(typeof(ObjectInHandsAnimator), "AnimatorEventHandler")]
//     public class PatchAnimatorEventHandler
//     {
//         static bool Prefix(int functionNameHash, AnimationEventSystem.AnimationEventParameter parameter)
//         {
//             SmartActionLogger.Log(
//                 $"[Patch_AnimatorEventHandler]  Event détecté  Hash: {functionNameHash} | Paramètre: {parameter}");
//
//             return true; // Sinon, l'event continue normalement
//         }
//     }
//     
//
//     [HarmonyPatch(typeof(ActiveHealthController.GClass2813))]
//     public class PatchDoMedEffectResidue
//     {
//         [HarmonyPatch("ForceResidue")]
//         [HarmonyPrefix]
//         public static void Prefix_ForceResidue(ActiveHealthController.GClass2813 __instance)
//         {
//             SmartActionLogger.Log("[DoMedEffect]  ForceResidue appelé");
//
//             // Log des informations de l'effet
//             SmartActionLogger.Log(
//                 $"[DoMedEffect] 🔍 Effet en résidu : {__instance.GetType().Name} | BodyPart: {__instance.BodyPart}");
//         }
//     }
//
//     [HarmonyPatch(typeof(ObjectInHandsAnimator))]
//     public class PatchObjectInHandsAnimator
//     {
//         [HarmonyPatch("SetActiveParam")]
//         [HarmonyPrefix]
//         public static void Prefix_SetActiveParam(ObjectInHandsAnimator __instance, bool active)
//         {
//             SmartActionLogger.Log($"[ObjectInHandsAnimator]  SetActiveParam appel Active: {active}");
//         }
//
//         [HarmonyPatch("SetInventory")]
//         [HarmonyPrefix]
//         public static void Prefix_SetInventory(ObjectInHandsAnimator __instance, bool open)
//         {
//             SmartActionLogger.Log($"[ObjectInHandsAnimator]  SetInventory appel Open: {open}");
//         }
//
//         [HarmonyPatch("ResetLeftHand")]
//         [HarmonyPrefix]
//         public static void Prefix_ResetLeftHand(ObjectInHandsAnimator __instance)
//         {
//             SmartActionLogger.Log("[ObjectInHandsAnimator]  ResetLeftHand appelé !");
//         }
//     }
// }
    // [HarmonyPatch(typeof(ActiveHealthController.GClass2813), "ManualUpdate")]
    // public static class PatchTrackHealingAccumulation
    // {
    //     [HarmonyPrefix]
    //     public static void Prefix(ActiveHealthController.GClass2813 __instance, float deltaTime)
    //     {
    //         if (__instance == null)
    //             return;
    //
    //         // Récupération des champs avec les vrais noms
    //         var float_6 = AccessTools.Field(typeof(ActiveHealthController.GClass2813), "float_6");
    //         var float_12 = AccessTools.Field(typeof(ActiveHealthController.GClass2813), "float_12");
    //         var float_0 = AccessTools.Field(typeof(ActiveHealthController.GClass2813), "float_0");
    //         var float_4 = AccessTools.Field(typeof(ActiveHealthController.GClass2813), "float_4");
    //         var float_15 = AccessTools.Field(typeof(ActiveHealthController.GClass2813), "float_15");
    //         var float_16 = AccessTools.Field(typeof(ActiveHealthController.GClass2813), "float_16");
    //         var float_17 = AccessTools.Field(typeof(ActiveHealthController.GClass2813), "float_17");
    //         var float_18 = AccessTools.Field(typeof(ActiveHealthController.GClass2813), "float_18");
    //
    //         // Lecture des valeurs
    //         var Single_0 = (float)float_6.GetValue(__instance);
    //         var totalTime = (float)float_12.GetValue(__instance);
    //         var tickValue = (float)float_0.GetValue(__instance);
    //         var workStateTime = (float)float_4.GetValue(__instance);
    //         var energyAmount = (float)float_15.GetValue(__instance);
    //         var hydrationAmount = (float)float_16.GetValue(__instance);
    //         var otherValue1 = (float)float_17.GetValue(__instance);
    //         var otherValue2 = (float)float_18.GetValue(__instance);
    //
    //         // 🔍 Log détaillé à chaque tick
    //         SmartActionLogger.Log(
    //             $"[ManualUpdate] ➡️ Single_0 = {Single_0:F2} | float_12 = {totalTime:F2} | float_0 = {tickValue:F2} | float_4 = {workStateTime:F2} 💧 float_15 = {energyAmount:F2} | float_16 = {hydrationAmount:F2} | float_17 = {otherValue1:F2} | float_18 = {otherValue2:F2}");
    //
    //     }
    // }
    
    // [HarmonyPatch(typeof(ActiveHealthController), "ChangeHealth")]
    // public static class PatchChangeHealth
    // {
    //     [HarmonyPrefix]
    //     public static void Prefix(ActiveHealthController __instance, EBodyPart bodyPart, float value,
    //         DamageInfoStruct damageInfo)
    //     {
    //         SmartActionLogger.Log($"[ChangeHealth] 🩹 BodyPart: {bodyPart}, Value: {value:F2}, Reason: {damageInfo}");
    //     }
    // }
    //
    // [HarmonyPatch(typeof(ActiveHealthController), "ChangeEnergy")]
    // public static class PatchChangeEnergy
    // {
    //     [HarmonyPrefix]
    //     public static void Prefix(ActiveHealthController __instance, float value)
    //     {
    //         SmartActionLogger.Log($"[ChangeEnergy] ⚡ Energy Changed: {value:F2}");
    //     }
    // }
    //
    // [HarmonyPatch(typeof(ActiveHealthController), "ChangeHydration")]
    // public static class PatchChangeHydration
    // {
    //     [HarmonyPrefix]
    //     public static void Prefix(ActiveHealthController __instance, float value)
    //     {
    //         SmartActionLogger.Log($"[ChangeHydration] 💧 Hydration Changed: {value:F2}");
    //     }
    // }
}
// [HarmonyPatch(typeof(Player.MedsController.Class1172), "Start")]
// public static class PatchMedController
// {
//     private static readonly Dictionary<EBodyPart, int> PredictedCycles = new();
//
//     [HarmonyPrefix]
//     public static void Prefix(Player.MedsController.Class1172 __instance, GStruct353<EBodyPart> bodyParts,
//         float amount, Action callback)
//     {
//         if (__instance == null)
//             return;
//         SmartActionLogger.Log("[PatchCheckLocalPlayer] Start ! ");
//     }
// }