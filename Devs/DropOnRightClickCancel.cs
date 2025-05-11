// using System.Collections;
// using System.Collections.Generic;
// using System.Reflection;
// using EFT;
// using EFT.GameTriggers;
// using EFT.InventoryLogic;
// using HarmonyLib;
// using TimeStretch.Utils;
// using UnityEngine;
//
// namespace QuickDropCancelMod
// {
//     public class DropOnRightClickCancel : MonoBehaviour
//     {
//         private Player _player;
//         private float _ressource;
//         private Animator _animator;
//         private float _lastClickTime;
//         private const float CancelThreshold = 0.3f;
//
//
//         private Coroutine _waitEnOperationEnd;
//         private Coroutine _waitFirearmReturnAndBoost;
//         private Coroutine _waitForItemDroppableCoroutine;
//         private Coroutine _waitResetSpeed;
//
//         private readonly HashSet<string> _droppedItemIds = [];
//         private bool _inputLocked;
//         private bool _updateRun;
//
//         public void Initialize(Player player, float ressource)
//         {
//             _ressource = ressource;
//             _player = player;
//             _droppedItemIds.Clear();
//             SmartActionLogger.Info($"[CutAnimation] initialized with player : {_player}");
//             TryResolveAnimator();
//         }
//
//         public bool IsBusy()
//         {
//             return _waitEnOperationEnd != null;
//         }
//
//         private void TryResolveAnimator()
//         {
//             var handler = _player?.GetComponentInChildren<HandlerAnimator>();
//             if (handler != null)
//             {
//                 _animator = AccessTools.Field(typeof(HandlerAnimator), "_target").GetValue(handler) as Animator;
//                 if (_animator != null)
//                     SmartActionLogger.Info($"[CutAnimation] ✅ Animator resolved in Initialize {_animator}");
//             }
//             else
//             {
//                 SmartActionLogger.Info("[CutAnimation] handle null");
//             }
//         }
//
//         private void Update()
//         {
//             if (!_updateRun)
//             {
//                 _updateRun = true;
//                 SmartActionLogger.Info("[CutAnimation- Mono] Update run");
//             }
//
//             if (_inputLocked) return;
//
//             if (!Input.GetMouseButtonDown(1))
//                 return;
//
//             var delta = Time.time - _lastClickTime;
//             _lastClickTime = Time.time;
//             SmartActionLogger.Log($"[CutAnimation- Mono] 🖱️ Right click delta = {delta}");
//
//             if (!(delta < CancelThreshold))
//                 return;
//             SmartActionLogger.Log($"[CutAnimation- Mono] 🖱️ GOOOO");
//             _inputLocked = true;
//             TryCancelHandsController();
//         }
//
//         private void TryCancelHandsController()
//         {
//             var hands = _player?.HandsController;
//             if (!hands)
//             {
//                 SmartActionLogger.Warn("[CutAnimation- Mono] ❌ HandsController nul");
//                 _inputLocked = false;
//                 return;
//             }
//
//             if (hands.GetType().Name == "MedsController")
//             {
//                 var currentOpProp =
//                     hands.GetType().GetProperty("Class1172_0", BindingFlags.Instance | BindingFlags.Public);
//                 var currentOp = currentOpProp?.GetValue(hands);
//
//                 if (currentOp == null)
//                 {
//                     SmartActionLogger.Warn("[CutAnimation- Mono] ❌ Class1172_0 est null (aucun soin en cours ?)");
//                     _inputLocked = false;
//                     return;
//                 }
//
//                 SmartActionLogger.Log($"[CutAnimation- Mono] 💉 Opération de soin détectée : {currentOp.GetType().FullName}");
//
//                 var fastForward = currentOp.GetType().GetMethod(
//                     "FastForward",
//                     BindingFlags.Public |
//                     BindingFlags.Instance
//                 );
//                 if (fastForward == null)
//                 {
//                     SmartActionLogger.Warn("[CutAnimation- Mono] ❌ FastForward non trouvé sur l’opération");
//                     _inputLocked = false;
//                     return;
//                 }
//
//                 var item = hands.Item;
//                 if (item == null)
//                 {
//                     SmartActionLogger.Warn("[CutAnimation- Mono] ❌ item on hand non trouvé");
//                     _inputLocked = false;
//                     return;
//                 }
//
//                 fastForward.Invoke(currentOp, null);
//                 SmartActionLogger.Warn("[CutAnimation - Mono] ⚡ FastForward() exécuté immédiatement pour couper le soin");
//                 StartCoroutine(MedsCancelSequence(item));
//             }
//             else
//             {
//                 _inputLocked = false;
//                 SmartActionLogger.Warn("[CutAnimation- Mono] ❌ Class1172_0 est null (aucun soin en cours ?)");
//             }
//         }
//
//         private IEnumerator MedsCancelSequence(Item item)
//         {
//             SmartActionLogger.Log("[CutAnimation] 1) Début de la séquence d'annulation de soin");
//
//             _waitEnOperationEnd = StartCoroutine(WaitForMedsOperationEndAndFinalize());
//             yield return _waitEnOperationEnd;
//             SmartActionLogger.Log("[CutAnimation] 2) Fin d'attente de l'opération de soin");
//
//             _waitFirearmReturnAndBoost = StartCoroutine(WaitForFirearmReturnAndBoost());
//             yield return _waitFirearmReturnAndBoost;
//             SmartActionLogger.Log("[CutAnimation] 3) Fin d'attente du retour de l'arme");
//
//             if (Mathf.Approximately(_ressource, 1f))
//                 yield break;
//             SmartActionLogger.Log("[CutAnimation] 4) Démarrage de l'attente du drop d'item");
//             _waitForItemDroppableCoroutine = StartCoroutine(WaitForItemToBeDroppable(item));
//             yield return _waitForItemDroppableCoroutine;
//             SmartActionLogger.Log("[CutAnimation] 5) Fin de l'attente du drop d'item");
//
//             yield return _waitResetSpeed;
//             yield return new WaitForSeconds(0.2f);
//             SmartActionLogger.Info(
//                 "[CutAnimation] ✅ Toutes les coroutines sont terminées, destruction de DropOnRightClickCancel");
//             Destroy(this);
//         }
//
//         private IEnumerator WaitForItemToBeDroppable(Item itemToDrop)
//         {
//             var timeout = 3f;
//             SmartActionLogger.Info("[CutAnimation] 🎬 Début de WaitForItemToBeDroppable()");
//
//             if (itemToDrop == null)
//             {
//                 SmartActionLogger.Warn("[CutAnimation] ⛔ Item null, abandon de drop");
//                 yield break;
//             }
//
//             var itemId = itemToDrop.Id;
//             SmartActionLogger.Log($"[CutAnimation] 🔍 Check de l'item {itemToDrop.LocalizedName()} [ID: {itemId}]");
//
//             if (_droppedItemIds.Contains(itemId))
//             {
//                 SmartActionLogger.Warn("[CutAnimation] ⛔ Item déjà droppé, on ignore : " + itemToDrop.LocalizedName());
//                 yield break;
//             }
//
//             SmartActionLogger.Info($"[CutAnimation] ⏳ Attente que l'item devienne droppable (timeout: {timeout}s)");
//             while (timeout > 0f)
//             {
//                 if (_player?.InventoryController == null)
//                 {
//                     SmartActionLogger.Warn("[CutAnimation] ⚠️ InventoryController indisponible");
//                     yield break;
//                 }
//
//                 SmartActionLogger.Log($"[CutAnimation] 🔄 Check Parent - Timeout restant: {timeout:F1}s");
//                 if (itemToDrop.Parent != null)
//                 {
//                     _droppedItemIds.Add(itemId);
//                     _player.InventoryController.ThrowItem(itemToDrop, false, null);
//                     SmartActionLogger.Warn(
//                         $"[CutAnimation] 💥 Item droppé : {itemToDrop.LocalizedName()} [Parent: {itemToDrop.Parent}]");
//                     yield break;
//                 }
//
//                 timeout -= Time.deltaTime;
//                 yield return null;
//             }
//
//             SmartActionLogger.Warn(
//                 $"[CutAnimation] ⏱️ Timeout : item jamais devenu droppable ({itemToDrop.LocalizedName()})");
//         }
//
//         private IEnumerator WaitForMedsOperationEndAndFinalize()
//         {
//             var timeout = 3f;
//             SmartActionLogger.Info("[CutAnimation] 🔄 Attente de fin d'opération Meds");
//
//             while (timeout > 0f)
//             {
//                 if (_player?.HandsController is not Player.MedsController meds)
//                 {
//                     SmartActionLogger.Warn("[CutAnimation] ❌ Plus de MedsController actif");
//                     yield break;
//                 }
//
//                 var currentOp = meds.Class1172_0;
//                 if (currentOp == null)
//                 {
//                     SmartActionLogger.Info("[CutAnimation] ✅ currentOp nul → soin terminé");
//                     break;
//                 }
//
//                 var finished = currentOp.State.ToString() == "Finished";
//                 SmartActionLogger.Info($"[CutAnimation] 🔄 State: {currentOp.State}, Timeout: {timeout:F2}s");
//
//                 if (finished)
//                 {
//                     SmartActionLogger.Info("[CutAnimation] ✅ Soin terminé");
//                     break;
//                 }
//
//                 timeout -= Time.deltaTime;
//                 yield return null;
//             }
//         }
//
//         private IEnumerator WaitForFirearmReturnAndBoost()
//         {
//             var timeout = 1.5f;
//             SmartActionLogger.Info("[CutAnimation] 🔄 Vérification retour Firearm");
//
//             while (timeout > 0f)
//             {
//                 if (_player?.HandsController is Player.FirearmController firearm)
//                 {
//                     var currentOp = firearm.CurrentOperation;
//                     if (currentOp == null)
//                     {
//                         SmartActionLogger.Warn("[CutAnimation] ❌ FirearmController détecté mais CurrentOperation est null");
//                         break;
//                     }
//
//                     var opType = currentOp.GetType();
//                     SmartActionLogger.Info($"[CutAnimation] 🔫 Arme détectée, opération: {opType.FullName}");
//
//                     if (opType.FullName == "EFT.Player+FirearmController+GClass1824")
//                     {
//                         var anim = firearm.FirearmsAnimator;
//                         if (anim != null)
//                         {
//                             anim.SetAnimationSpeed(2f);
//                             SmartActionLogger.Warn("[CutAnimation] ⚡ Accélération Draw (GClass1824) → x2 appliquée");
//                             _waitResetSpeed = StartCoroutine(ResetAnimatorSpeedLater(anim));
//                         }
//                         else
//                         {
//                             SmartActionLogger.Warn("[CutAnimation] ⚠️ FirearmsAnimator est null malgré la détection du Draw");
//                         }
//
//                         break;
//                     }
//                     else
//                     {
//                         SmartActionLogger.Info("[CutAnimation] ℹ️ L'opération actuelle n'est pas le Draw (GClass1824)");
//                     }
//
//                     break;
//                 }

                // timeout -= Time.deltaTime;
                // yield return null;
            // }

//             _inputLocked = false;
//             SmartActionLogger.Info("[CutAnimation] ✅ Fin du blocage input");
//         }
//
//
//         private static IEnumerator ResetAnimatorSpeedLater(FirearmsAnimator anim)
//         {
//             yield return new WaitForSeconds(0.5f);
//             anim.SetAnimationSpeed(1.0f);
//             SmartActionLogger.Log("[CutAnimation] 🔁 Vitesse d'animation réinitialisée à 1.0");
//         }
//
//         public IEnumerator WaitForAllCoroutinesAndSelfDestruct()
//         {
//             SmartActionLogger.Info("[CutAnimation] ⏳ Attente de fin de toutes les coroutines avant destruction...");
//
//             while (_waitEnOperationEnd != null || _waitForItemDroppableCoroutine != null)
//                 yield return null;
//
//             SmartActionLogger.Info("[CutAnimation] ✅ Toutes coroutines terminées, destruction de DropOnRightClickCancel");
//             Destroy(this);
//         }
//     }
// }

//     public static class HookCategories
//     {
//         
//         [HarmonyPatch(typeof(Player), nameof(Player.ManualUpdate))]
//         public class PatchModulateHealSpeed
//         {
//             private static readonly Dictionary<IEffect, EEffectState> PreviousStates = new();
//
//             public static void Postfix(Player __instance, float deltaTime) 
//             {
//                 if (!__instance.IsYourPlayer)
//                     return;
//
//                 var healthController = __instance.ActiveHealthController;
//                 if (healthController == null)
//                     return;
//
//                 if (__instance.HandsController is not Player.MedsController { FirearmsAnimator: { } anim })
//                     return;
//
//                 foreach (var effect in healthController.GetAllEffects())
//                 {
//                     if (!effect.GetType().Name.Contains("MedEffect"))
//                         continue;
//
//                     var state = effect.State;
//                     PreviousStates.TryGetValue(effect, out var prevState);
//
//                     if (state == prevState) 
//                         continue;
//                     PreviousStates[effect] = state;
//
//                     if (state != EEffectState.Added) 
//                         continue;
//                     var velocity = __instance.MovementContext?.CharacterController?.velocity;
//                     if (velocity == null)
//                     {
//                         SmartActionLogger.Log("[MedEffect RegularUpdate] ❌ velocity null");
//                         return;
//                     }
//                 
//                     var isMoving = !IsApproximatelyZero(velocity.Value);
//                     var shouldAccelerate = !isMoving;
//
//                     var speed = shouldAccelerate ? 2f : 1f;
//                     anim.SetAnimationSpeed(speed);
//
//                     SmartActionLogger.Log($"[Player.ManualUpdate] État=Added → AnimationSpeed set to {speed:F2} (isMoving={shouldAccelerate})");
//                 }
//             }
//         }
//         
//         [HarmonyPatch]
//         public static class PatchMedEffectAccelerate
//         {
//             public static MethodBase TargetMethod()
//             {
//                 return AccessTools.Method("EFT.HealthSystem.ActiveHealthController+MedEffect:RegularUpdate");
//             }
//
//             // Statique car on ne travaille pas par instance
//             private static float _originalWorkStateTime = -1f;
//             private static bool _isAccelerated = false;
//
//             static void Prefix(object __instance, float deltaTime)
//             {
//                 if (__instance == null)
//                     return;
//
//                 var type = __instance.GetType();
//
//                 var healthControllerProp = AccessTools.Property(type.BaseType, "HealthController");
//                 if (healthControllerProp?.GetValue(__instance) is not ActiveHealthController healthController)
//                 {
//                     SmartActionLogger.Log("[MedEffect RegularUpdate] ❌ HealthController introuvable ou cast échoué");
//                     return;
//                 }
//
//                 var player = healthController.Player;
//                 if (player == null || !player.IsYourPlayer)
//                     return;
//                 
//                 var velocity = player.MovementContext?.CharacterController?.velocity;
//                 if (velocity == null)
//                 {
//                     SmartActionLogger.Log("[MedEffect RegularUpdate] ❌ velocity null");
//                     return;
//                 }
//                 
//                 var isMoving = !IsApproximatelyZero(velocity.Value);
//                 var shouldAccelerate = !isMoving;
//
//                 if (player.HandsController is Player.MedsController medsController)
//                 {
//                     var anim = medsController.FirearmsAnimator;
//                     if (anim != null)
//                     {
//                         var newSpeed = shouldAccelerate ? 2.0f : 1.0f;
//                         anim.SetAnimationSpeed(newSpeed);
//                         SmartActionLogger.Log($"[MedEffect RegularUpdate] 🎞️ Animation speed set to {newSpeed:F2}");
//                     }
//                 }
//                 
//                
//                 var elapsedTimeProp = FindProperty(type, "WholeTime");
//                 var workStateTimeField = FindField(type, "float_12");
//                 var healAmountField = FindField(type, "float_16");
//
//                 if (elapsedTimeProp == null || workStateTimeField == null || healAmountField == null)
//                 {
//                     SmartActionLogger.Log("[MedEffect RegularUpdate] ❌ Champs critiques manquants");
//                     return;
//                 }
//
//                 var elapsedTime = (float)(elapsedTimeProp.GetValue(__instance) ?? -1f);
//                 var currentWorkStateTime = (float)(workStateTimeField.GetValue(__instance) ?? -1f);
//                 var currentHealAmount = (float)(healAmountField.GetValue(__instance) ?? 0f);
//
//                 SmartActionLogger.Log(
//                     $"[MedEffect RegularUpdate] deltaTime={deltaTime:F3} | isMoving={isMoving} | " +
//                     $"elapsedTime={elapsedTime:F2} | workStateTime={currentWorkStateTime:F2} | " +
//                     $"healAmount={currentHealAmount:F2} | velocity={velocity}"
//                 );
//                 SmartActionLogger.Log($"[DEBUG] _originalWorkStateTime={_originalWorkStateTime:F2} |" +
//                                 $" currentWorkStateTime={currentWorkStateTime:F2} |" +
//                                 
//                                 $" elapsedTime={elapsedTime:F2}");
//                 
//                 if (_originalWorkStateTime < 0f)
//                 {
//                     _originalWorkStateTime = currentWorkStateTime;
//                     _isAccelerated = false;
//                     SmartActionLogger.Log("[MedEffect RegularUpdate] 📌 Valeurs originales sauvegardées");
//                 }
//                 
//                 if (_isAccelerated != shouldAccelerate)
//                 {
//                     float ratio = shouldAccelerate ? 0.5f : 1.0f;
//                     float newWorkStateTime = _originalWorkStateTime * ratio;
//
//                     workStateTimeField.SetValue(__instance, newWorkStateTime);
//
//                     _isAccelerated = shouldAccelerate;
//
//                     SmartActionLogger.Log(
//                         $"[MedEffect RegularUpdate] 🌀 {(shouldAccelerate ? "Accélération" : "Restauration")} appliquée → ratio={ratio:F2}");
//                 }
//                 
//             }
//             
//             [HarmonyPatch]
//             public static class PatchMedEffectResidue
//             {
//                 public static MethodBase TargetMethod()
//                 {
//                     return AccessTools.Method("EFT.HealthSystem.ActiveHealthController+MedEffect:Residue");
//                 }
//                 static void Prefix(object __instance)
//                 {
//                     _originalWorkStateTime = -1f;
//                     _isAccelerated = false;
//
//                     try
//                     {
//                         var type = __instance.GetType();
//                         var healthControllerProp = AccessTools.Property(type.BaseType, "HealthController");
//                         if (healthControllerProp?.GetValue(__instance) is not ActiveHealthController healthController)
//                         {
//                             SmartActionLogger.Log("[MedEffect Residue] ❌ HealthController introuvable ou cast échoué");
//                             return;
//                         }
//
//                         var player = healthController.Player;
//                         if (player == null || !player.IsYourPlayer)
//                             return;
//
//                         if (player?.HandsController is Player.MedsController { FirearmsAnimator: { } anim })
//                         {
//                             anim.SetAnimationSpeed(1.0f);
//                             SmartActionLogger.Log("[MedEffect Residue] 🧹 Animation speed reset to 1.0f");
//                         }
//
//                         SmartActionLogger.Log("[MedEffect Residue] ✅ Reset complet appliqué");
//                     }
//                     catch (Exception ex)
//                     {
//                         SmartActionLogger.Log($"[MedEffect Residue] ❌ Erreur lors du reset : {ex}");
//                     }
//                     
//                 }
//             }
//
//             
//             
//             private static FieldInfo FindField(Type type, string name)
//             {
//                 while (type != null)
//                 {
//                     var field = type.GetField(name,
//                         BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
//                     if (field != null) return field;
//                     type = type.BaseType;
//                 }
//
//                 return null;
//             }
//
//             private static PropertyInfo FindProperty(Type type, string name)
//             {
//                 while (type != null)
//                 {
//                     var prop = type.GetProperty(name,
//                         BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
//                     if (prop != null) return prop;
//                     type = type.BaseType;
//                 }
//
//                 return null;
//             }
//         }
//
//         [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.ChangeHealth))]
//         public static class PatchApplyChange
//         {
//             public static void Postfix(
//                 ActiveHealthController __instance,
//                 EBodyPart bodyPart,
//                 float value,
//                 DamageInfoStruct damageInfo)
//             {
//                 if (__instance == null)
//                     return;
//                 var source = damageInfo.DamageType.ToString();
//                 var sourceId = damageInfo.SourceId?.ToString() ?? "null";
//                 var isHealing = value > 0f;
//                 var prefix = isHealing ? "💚 HEAL" : "💥 DAMAGE";
//
//                 var partName = Enum.GetName(typeof(EBodyPart), bodyPart);
//                 var player = __instance?.Player;
//                 var isLocal = player?.IsYourPlayer ?? false;
//                 var localTag = isLocal ? "[LOCAL]" : "[REMOTE]";
//
//                 SmartActionLogger.Log(
//                     $"[ActiveHealthController.ChangeHealth] {prefix} {localTag} → {partName} | {(isHealing ? "+" : "")}{value:F2} HP " +
//                     $" (Type: {source}, ID: {sourceId})"
//                 );
//             }
//         }
//         
//         private static bool IsApproximatelyZero(Vector3 vec)
//         {
//             const float epsilon = 0.01f;
//             return vec.sqrMagnitude < epsilon * epsilon;
//         }
//     }
// }
//
//
// // public static void Postfix(Player __instance, float deltaTime)
// // {
// //     if (!__instance.IsYourPlayer)
// //     {
// //         SmartActionLogger.Log("[HEAL SPEED] Not local player, skipping");
// //         return;
// //     }
// //    
// //
// //     _tickCooldown -= deltaTime;
// //     if (_tickCooldown > 0f)
// //         return;
// //
// //     _tickCooldown = TickInterval;
// //
// //     var healthController = __instance.ActiveHealthController;
// //     if (healthController == null)
// //     {
// //         SmartActionLogger.Log("[HEAL SPEED] No health controller found");
// //         return;
// //     }
// //
// //     var medEffect = healthController
// //         .GetAllEffects()
// //         .FirstOrDefault(e => e.GetType().Name.Contains("MedEffect") &&
// //                              e.State == EEffectState.Started);
// //
// //     if (medEffect == null)
// //     {
// //         SmartActionLogger.Log("[HEAL SPEED] No active med effect found");
// //         return;
// //     }
// //
// //     SmartActionLogger.Log($"[HEAL SPEED] Found med effect: {medEffect.GetType().Name}");
// //     
// //
// //     var clampedSpeed = __instance.MovementContext.ClampedSpeed;
// //     var speedFactor = clampedSpeed > 0.1f ? MultiplierWhenMoving : 1f;
// //     SmartActionLogger.Log($"[HEAL SPEED] 🔍 Med effect state: {medEffect.State}, Current speed: {clampedSpeed:F2}");
// //
// //     // 🔁 Sync animation
// //     if (__instance.HandsController is Player.MedsController medsController)
// //     {
// //         medsController.FirearmsAnimator?.SetAnimationSpeed(speedFactor);
// //         SmartActionLogger.Log($"[HEAL SPEED] 🎬 Animation speed set to {speedFactor:F2}x");
// //     }
// //
// //     var effect = medEffect as IEffect;
// //     if (!_healingTimers.TryGetValue(effect, out float elapsed))
// //         elapsed = 0f;
// //
// //     elapsed += deltaTime * speedFactor;
// //
// //     if (elapsed >= TickRate)
// //     {
// //         float over = elapsed - TickRate;
// //         elapsed = over;
// //
// //         var method = medEffect.GetType().GetMethod("AddWorkTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
// //         if (method != null)
// //         {
// //             method.Invoke(medEffect, new object[] { TickRate, false });
// //             SmartActionLogger.Log($"[HEAL SPEED] ⏩ AddWorkTime({TickRate:F2}) [Speed: {clampedSpeed:F2}]");
// //         }
// //         else
// //         {
// //             SmartActionLogger.Warn("[HEAL SPEED] ❌ Méthode AddWorkTime introuvable");
// //         }
// //     }
// //
// //     _healingTimers[effect] = elapsed;
// //     SmartActionLogger.Log($"[HEAL SPEED] ⏱️ Elapsed: {elapsed:F2}s (TickRate: {TickRate:F2})");
// // }
// // }
// // }
// // }
//
// // [HarmonyPatch(typeof(Player), "set_HandsController")]
// // public static class PatchPlayerHandsController
// // {
// //     [HarmonyPostfix]
// //     public static void Postfix(Player __instance)
// //     {
// // // 🔐
// // if (__instance == null || !__instance.IsYourPlayer)
// // {
// //     SmartActionLogger.Log("🛑 Skip non-local or null player");
// //     return;
// // }
// //
// // // 🔐
// // if (!__instance.HealthController.IsAlive)
// //     return;
// // // 🔐
// // var hands = __instance.HandsController;
// // if (hands == null)
// //     return;
// // // 🔐
// // if (AccessTools.Field(hands.GetType(), "item_0")?.GetValue(hands) is not Item item)
// //     return;
// // // 🔐
// // if (item.TemplateId.ToString().Length != 24)
// //     return;
// //
// // var handsTypeName = hands.GetType().Name;
// // var id = item.TemplateId.ToString();
// // SmartActionLogger.Warn($"[CutAnimation] 🔎 id: {id}");
// // MedsItemClass medItem = null;
// // var isValidMeds = handsTypeName == "MedsController" && item is MedsItemClass med &&
// //                   (medItem = med) != null;
// // MedKitItemClass medKit = null;
// // var isValidMedkit = item is MedKitItemClass medKitItem && (medKit = medKitItem) != null;
// // var isMonoWay = !string.IsNullOrEmpty(id) &&
// //                 (id.Equals("5d02778e86f774203e7dedbe") || id.Equals("5d02797c86f774203f38e30a"));
// //
// // SmartActionLogger.Warn(
// //     $"[CutAnimation] 🔎 HandsController type: {handsTypeName}, Item type: {item.GetType().FullName}, Template: {item.TemplateId} → {id}");
//
// // if (isValidMedkit)
// // {
// //     if (_playerMovementCoroutine != null)
// //     {
// //         __instance.StopCoroutine(_playerMovementCoroutine);
// //         _playerMovementCoroutine = null;
// //     }
// //
// //     
// //     _playerMovementCoroutine =
// //         __instance.StartCoroutine(MonitorPlayerMovementAndAdjustAnimSpeed(__instance, medKit));
// // }
// // else
// // {
// //     SmartActionLogger.Warn($"[CutAnimation] not MedKitItemClass");
// // }
//
//
// // // 🎯 Gestion du Mono uniquement pour CMS / Surv12
// // var mono = __instance.GetComponent<DropOnRightClickCancel>();
// // if (!isValidMeds || !isMonoWay) 
// //     return;
// //
// // if (mono == null)
// // {
// //     mono = __instance.gameObject.AddComponent<DropOnRightClickCancel>();
// //     SmartActionLogger.Info("[CutAnimation] 🎯 New DropOnRightClickCancel");
// // }
// // var ressource = medItem.MedKitComponent.HpResource;
// // mono.Initialize(__instance, ressource);
// // SmartActionLogger.Info($"[CutAnimation] ✅ Init sur MedsController : {__instance}");
// // }
// // }
// // }
// // }
//
// // private IEnumerator SwitchWeaponWhenHandsFree(Item weapon)
// // {
// //     float timeout = 2f;
// //     while (_player.HandsController != null && timeout > 0f)
// //     {
// //         timeout -= Time.deltaTime;
// //         yield return null;
// //     }
// //
// //     _player.SetItemInHands(weapon, result =>
// //     {
// //         if (!result.Succeed)
// //         {
// //             SmartActionLogger.Warn("[CutAnimation] ⚠️ SetItemInHands échoué, fallback TryProceed");
// //             _player.TryProceed(weapon, null);
// //         }
// //     });
// // }
// //         
//
// // --- Firearms
// // if (hands is Player.FirearmController firearm)
// // {
// //     var currentOp = firearm?.CurrentOperation;
// //     SmartActionLogger.Log($"[CutAnimation] 🔍 FirearmController.CurrentOperation = {currentOp?.GetType().FullName}");
// //
// //     if (currentOp?.GetType().FullName == "EFT.Player+FirearmController+GClass1824")
// //     {
// //         firearm.FirearmsAnimator.SetAnimationSpeed(4f); // vitesse x3.5 (à ajuster selon ton ressenti)
// //         SmartActionLogger.Warn("[CutAnimation] ⚡ Accélération de Draw (GClass1824)");
// //
// //         // Reset après 0.5s
// //         StartCoroutine(ResetAnimatorSpeedLater(firearm.FirearmsAnimator));
// //     }
// //     else
// //     {
// //         firearm.CurrentOperation.FastForward();
// //         SmartActionLogger.Warn("[CutAnimation] 🔫 FirearmController → FastForward() exécuté");
// //     }
// //
// //     return;
// // }
// // --- Meds (CMS, Surv12, etc.)
//
//
// // if (hands.GetType().Name == "MedsController")
// // {
// //     var currentOpProp = hands.GetType().GetProperty("Class1172_0", BindingFlags.Public | BindingFlags.Instance);
// //     var currentOp = currentOpProp?.GetValue(hands);
// //     var fastForward = currentOp?.GetType().GetMethod("FastForward", BindingFlags.Public | BindingFlags.Instance);
// //     if (fastForward != null)
// //     {
// //         fastForward.Invoke(currentOp, null);
// //         SmartActionLogger.Warn("[CutAnimation] 💉 MedsController → FastForward() exécuté avec succès");
// //
// //         // 🔁 Tentative de drop de l’item utilisé (CMS, Surv12, etc.)
// //         var item = hands.Item;
// //         var inventory = _player.InventoryController;
// //
// //         if (item != null && inventory != null)
// //         {
// //             SmartActionLogger.Warn($"[CutAnimation] 🧻 Tentative de drop de : {item.LocalizedName()}");
// //             inventory.ThrowItem(item, callback: null);
// //         }
// //         else
// //         {
// //             SmartActionLogger.Warn($"[CutAnimation] ❌ Impossible de drop l’item (item ou inventory null)");
// //         }
// //     }
// //     else
// //     {
// //         SmartActionLogger.Warn("[CutAnimation] ⚠️ MedsController détecté mais FastForward() introuvable");
// //     }
// //     return;
// // }