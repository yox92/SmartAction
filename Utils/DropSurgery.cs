using System;
using System.Collections;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using SmartAction.Patch;
using UnityEngine;

namespace SmartAction.Utils;

public abstract class DropSurgery
{
    private static bool _inputLocked = false;
    private static float _lastClickTime = 0f;
    private const float CancelThreshold = 0.2f;
    private static Coroutine _cancelRoutine;
    private const string CmsId = "5d02778e86f774203e7dedbe";
    private const string Surv12Id = "5d02797c86f774203f38e30a";

    public static void CanDropSurgery(Player player, Player.ItemHandsController hands, MongoID mongoID, Item item)
    {
        var isSurgeryItem = mongoID.Equals(CmsId) || mongoID.Equals(Surv12Id);
        if (isSurgeryItem)
        {
            var delta = Time.time - _lastClickTime;
            // 🛡️ 
            if (_inputLocked)
            {
                SmartActionLogger.Log("[CutAnimation-Mono] 🔒 Input verrouillé → annulation ignorée");
                return;
            }

            if (!Input.GetMouseButtonDown(1))
                return;
            _lastClickTime = Time.time;
            SmartActionLogger.Log($"[CutAnimation-Mono] 🖱️ Right click delta = {delta:F3}");

            if (delta >= CancelThreshold)
                return;
            // 🛡️ 
            _inputLocked = true;

            TryCancelHandsController(hands, item, player);
            SmartActionLogger.Log("[CutAnimation-Mono] ✂️ Double clic détecté → annulation tentative");
        }
        else
        {
            // 🛡️ 
            _inputLocked = false;
        }
    }

    private static void TryCancelHandsController(Player.ItemHandsController hands, Item item, Player player)
    {
        try
        {
            var effect = PatchMedEffectHooks.CurrentHealingEffect;
            if (effect is null)
            {
                SmartActionLogger.Log($"Effet non démarré ou null Skip. État actuel: {effect?.State}");
                return;
            }

            FastForward(hands);

            if (_cancelRoutine != null)
            {
                CoroutineRunner.Stop(_cancelRoutine);
                SmartActionLogger.Warn("[CutAnimation] 🔁 Ancienne coroutine annulée");
            }

            _cancelRoutine = CoroutineRunner.Run(MedsCancelSequence(item, player));
        }
        catch (Exception e)
        {
            SmartActionLogger.Error($"TryCancelHandsController exception: {e}");
            _inputLocked = false;
        }
    }

    private static IEnumerator MedsCancelSequence(Item item, Player player)
    {
        try
        {
            SmartActionLogger.Log("[CutAnimation] 1) Début de la séquence d'annulation de soin");

            yield return WaitForMedsOperationEndAndFinalize(player);
            SmartActionLogger.Log("[CutAnimation] 2) Fin d'attente de l'opération de soin");

            yield return WaitForFirearmReturnAndBoost(player);
            SmartActionLogger.Log("[CutAnimation] 3) Fin d'attente du retour de l'arme");

            yield return WaitForItemToBeDroppable(item, player);
            SmartActionLogger.Log("[CutAnimation] 4) Fin de l'attente du drop d'item");

            SmartActionLogger.Info("[CutAnimation] ✅ Toutes les coroutines sont terminées");
        }
        finally
        {
            // 🛡️ 
            _inputLocked = false;
            _cancelRoutine = null;
            SmartActionLogger.Info("[CutAnimation] ✅ Fin de la séquence, input débloqué");
        }
    }

    private static IEnumerator WaitForItemToBeDroppable(Item itemToDrop, Player player)
    {
        var timeout = 3f;
        SmartActionLogger.Info("[CutAnimation] 🎬 Début de WaitForItemToBeDroppable()");

        if (itemToDrop == null)
        {
            SmartActionLogger.Warn("[CutAnimation] ⛔ Item null, abandon de drop");
            yield break;
        }

        var itemId = itemToDrop.Id;
        SmartActionLogger.Log($"[CutAnimation] 🔍 Check de l'item {itemToDrop.LocalizedName()} [ID: {itemId}]");

        SmartActionLogger.Info($"[CutAnimation] ⏳ Attente que l'item devienne droppable (timeout: {timeout}s)");
        while (timeout > 0f)
        {
            if (player?.InventoryController == null)
            {
                SmartActionLogger.Warn("[CutAnimation] ⚠️ InventoryController indisponible");
                yield break;
            }

            SmartActionLogger.Log($"[CutAnimation] 🔄 Check Parent - Timeout restant: {timeout:F1}s");
            if (itemToDrop.Parent != null)
            {
                if (itemToDrop is MedsItemClass cmsOrSurv12)
                {
                    cmsOrSurv12.MedKitComponent.HpResource += 1f;
                }

                player.InventoryController.ThrowItem(itemToDrop, false, null);
                SmartActionLogger.Warn(
                    $"[CutAnimation] 💥 Item droppé : {itemToDrop.LocalizedName()} [Parent: {itemToDrop.Parent}]");
                yield break;
            }

            timeout -= Time.deltaTime;
            yield return null;
        }

        SmartActionLogger.Warn(
            $"[CutAnimation] ⏱️ Timeout : item jamais devenu droppable ({itemToDrop.LocalizedName()})");
    }

    private static IEnumerator WaitForMedsOperationEndAndFinalize(Player player)
    {
        var timeout = 3f;
        SmartActionLogger.Info("[CutAnimation] 🔄 Attente de fin d'opération Meds");

        while (timeout > 0f)
        {
            if (player?.HandsController is not Player.MedsController meds)
            {
                SmartActionLogger.Warn("[CutAnimation] ❌ Plus de MedsController actif");
                yield break;
            }

            var currentOp = meds.Class1172_0;
            if (currentOp == null)
            {
                SmartActionLogger.Info("[CutAnimation] ✅ currentOp nul → soin terminé");
                break;
            }

            var finished = currentOp.State.ToString() == "Finished";
            SmartActionLogger.Info($"[CutAnimation] 🔄 State: {currentOp.State}, Timeout: {timeout:F2}s");

            if (finished)
            {
                SmartActionLogger.Info("[CutAnimation] ✅ Soin terminé");
                break;
            }

            timeout -= Time.deltaTime;
            yield return null;
        }
    }

    private static IEnumerator WaitForFirearmReturnAndBoost(Player player)
    {
        var timeout = 1.5f;
        SmartActionLogger.Info("[CutAnimation] 🔄 Vérification retour Firearm");

        while (timeout > 0f)
        {
            if (player?.HandsController is Player.FirearmController firearm)
            {
                var currentOp = firearm.CurrentOperation;
                if (currentOp == null)
                {
                    SmartActionLogger.Warn(
                        "[CutAnimation] ❌ FirearmController détecté mais CurrentOperation est null");
                    break;
                }

                var opType = currentOp.GetType();
                SmartActionLogger.Info($"[CutAnimation] 🔫 Arme détectée, opération: {opType.FullName}");

                if (opType.FullName == "EFT.Player+FirearmController+GClass1824")
                {
                    var anim = firearm.FirearmsAnimator;
                    if (anim != null)
                    {
                        anim.SetAnimationSpeed(2f);
                        SmartActionLogger.Warn("[CutAnimation] ⚡ Accélération Draw (GClass1824) → x2 appliquée");
                        CoroutineRunner.Run(ResetAnimatorSpeedLater(anim));
                    }
                    else
                    {
                        SmartActionLogger.Warn(
                            "[CutAnimation] ⚠️ FirearmsAnimator est null malgré la détection du Draw");
                    }
                }
                else
                {
                    SmartActionLogger.Info("[CutAnimation] ℹ️ L'opération actuelle n'est pas le Draw (GClass1824)");
                }

                break;
            }

            timeout -= Time.deltaTime;
            yield return null;
        }

        _inputLocked = false;
        SmartActionLogger.Info("[CutAnimation] ✅ Fin du blocage input");
    }

    private static IEnumerator ResetAnimatorSpeedLater(FirearmsAnimator anim)
    {
        yield return new WaitForSeconds(1.0f);
        anim.SetAnimationSpeed(1.0f);
        SmartActionLogger.Log("[CutAnimation] 🔁 Vitesse d'animation réinitialisée à 1.0");
    }

    private static void FastForward(Player.ItemHandsController hands)
    {
        var handsType = hands.GetType();
        var currentOpProp = handsType.GetProperty("Class1172_0", BindingFlags.Instance | BindingFlags.Public);
        SmartActionLogger.Log(
            $"[CutAnimation] 📖 Type={handsType.Name}, Property={currentOpProp?.Name ?? "null"}");
        if (currentOpProp == null)
        {
            SmartActionLogger.Warn("[CutAnimation-Mono] ❌ Class1172_0 property non trouvée");
            return;
        }

        var currentOp = currentOpProp?.GetValue(hands);
        if (currentOp == null)
        {
            SmartActionLogger.Warn("[CutAnimation- Mono] ❌ Class1172_0 est null (aucun soin en cours ?)");
            return;
        }

        var fastForward = currentOp.GetType().GetMethod(
            "FastForward",
            BindingFlags.Public |
            BindingFlags.Instance
        );
        if (fastForward == null)
        {
            SmartActionLogger.Warn("[CutAnimation- Mono] ❌ FastForward non trouvé sur l’opération");
            return;
        }

        fastForward.Invoke(currentOp, null);
    }
}