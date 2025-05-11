using System;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using SmartAction.Patch;

namespace SmartAction.Utils;

public abstract class SpeedHealing
{
    public static void SpeedTicksByMovement(Player player, Item item)
    {
        var anim = (player.HandsController as Player.MedsController)?.FirearmsAnimator;
        if (anim == null)
            return;
        var movementContext = player.MovementContext;
        if (movementContext?.CurrentState == null)
            return;
        var movementState = movementContext.CurrentState.Name;

        var effect = PatchMedEffectHooks.CurrentHealingEffect;
        if (effect is null)
            return;

        var currentEffectState = effect.State;

        var hasChanged = true;
        if (PatchMedEffectHooks.EffectUpdateCache.TryGetValue(effect, out var last))
        {
            hasChanged = last.movementState != movementState || last.effectState != currentEffectState;
        }

        if (!hasChanged)
            return;

        SmartActionLogger.Log(
            $"[SpeedTicksByMovement] Movement/Effect state changed: {movementState}/{currentEffectState}");

        PatchMedEffectHooks.EffectUpdateCache[effect] = (movementState, currentEffectState);

        var type = effect.GetType();
        var float12Field = ReflectionUtils.FindField(type, "float_12");
        var workTimeField = ReflectionUtils.FindProperty(type, "WorkStateTime");

        var key = (PatchDoMedEffect.LastHealingItem, currentEffectState);
        if (!PatchMedEffectHooks.OriginalFloat12.TryGetValue(key, out var float12))
            return;
        if (!PatchMedEffectHooks.OriginalWorkTime.TryGetValue(key, out var workTime))
        {
            SmartActionLogger.Log($"[SpeedTicksByMovement] WorkTime not found for {item.Template._name} [State={currentEffectState}]");
        }

        var isStarted = currentEffectState == EEffectState.Started;

        switch (currentEffectState)
        {
            case EEffectState.Added or EEffectState.Started:
                SmartActionLogger.Log(
                    $"[SpeedTicksByMovement] Adjusting work time and animation speed for {currentEffectState}");
                AdjustHealingWorkTime(
                    movementState,
                    float12,
                    workTime,
                    effect,
                    float12Field,
                    workTimeField,
                    isStarted);
                AdjustHealingAnimationSpeed(movementState, anim);
                break;
            case EEffectState.Residued:
                SmartActionLogger.Log("[SpeedTicksByMovement] Adjusting animation speed for residue state");
                AdjustHealingAnimationSpeed(movementState, anim);
                break;
        }

        SmartActionLogger.Log(
            $"[ManualUpdate] ⚙️ Adjusting {item.Template._name} [State={currentEffectState}, Move={movementState}]");
    }

    private static void AdjustHealingAnimationSpeed(EPlayerState state, FirearmsAnimator anim)
    {
        switch (state)
        {
            case EPlayerState.None:
            case EPlayerState.Idle:
            case EPlayerState.ProneIdle:
            case EPlayerState.Transition:
                anim.SetAnimationSpeed(ConvertSpeedToFloat(Plugin.IdleSpeed.Value));
                SmartActionLogger.Log(
                    $"[ManualUpdate] 🎞️ animation speed = {ConvertSpeedToFloat(Plugin.IdleSpeed.Value)} (stationary)");
                break;
            case EPlayerState.Sprint:
                anim.SetAnimationSpeed(ConvertSpeedToFloat(Plugin.SprintSpeed.Value));
                SmartActionLogger.Log(
                    $"[ManualUpdate] 🎞️ animation speed = {ConvertSpeedToFloat(Plugin.SprintSpeed.Value)} (sprint)");
                break;
            case EPlayerState.ProneMove:
            case EPlayerState.Run:
            case EPlayerState.Jump:
            case EPlayerState.FallDown:
            case EPlayerState.BreachDoor:
            case EPlayerState.Loot:
            case EPlayerState.Pickup:
            case EPlayerState.Open:
            case EPlayerState.Close:
            case EPlayerState.Unlock:
            case EPlayerState.Sidestep:
            case EPlayerState.DoorInteraction:
            case EPlayerState.Approach:
            case EPlayerState.Prone2Stand:
            case EPlayerState.Transit2Prone:
            case EPlayerState.Plant:
            case EPlayerState.Stationary:
            case EPlayerState.Roll:
            case EPlayerState.JumpLanding:
            case EPlayerState.ClimbOver:
            case EPlayerState.ClimbUp:
            case EPlayerState.VaultingFallDown:
            case EPlayerState.VaultingLanding:
            case EPlayerState.BlindFire:
            case EPlayerState.IdleWeaponMounting:
            case EPlayerState.IdleZombieState:
            case EPlayerState.MoveZombieState:
            case EPlayerState.TurnZombieState:
            case EPlayerState.StartMoveZombieState:
            case EPlayerState.EndMoveZombieState:
            case EPlayerState.DoorInteractionZombieState:
            default:
                anim.SetAnimationSpeed(ConvertSpeedToFloat(Plugin.WalkSpeed.Value));
                SmartActionLogger.Log(
                    $"[ManualUpdate] 🎞️ animation speed  = {ConvertSpeedToFloat(Plugin.WalkSpeed.Value)} (défaut)");
                break;
        }
    }

    private static void AdjustHealingWorkTime(
        EPlayerState state,
        float float12,
        float workTime,
        IEffect effect,
        FieldInfo float12Field,
        PropertyInfo workTimeField,
        bool isStarted)
    {
        var newFloat12 = float12;
        var newWorkTime = workTime;
        var newLoop = 0f;

        switch (state)
        {
            case EPlayerState.None:
            case EPlayerState.Idle:
            case EPlayerState.ProneIdle:
            case EPlayerState.Transition:
                newFloat12 = float12 * ConvertSpeedToMultiTime(Plugin.IdleSpeed.Value);

                if (isStarted)
                {
                    newLoop = LoopTime.OriginalLoopTime * ConvertSpeedToMultiTime(Plugin.IdleSpeed.Value);
                    newWorkTime = workTime * ConvertSpeedToMultiTime(Plugin.IdleSpeed.Value);
                    SmartActionLogger.Log(
                        $"[AdjustHealingWorkTime] ⏱️ Idle → LoopTime: {newLoop:F2} | WorkTime: {newWorkTime:F2}");
                }

                SmartActionLogger.Log($"[AdjustHealingWorkTime] ⏱️ Idle → Float12: {newFloat12:F2}");
                break;

            case EPlayerState.Sprint:
                newFloat12 = float12 * ConvertSpeedToMultiTime(Plugin.SprintSpeed.Value);

                if (isStarted)
                {
                    newLoop = LoopTime.OriginalLoopTime * ConvertSpeedToMultiTime(Plugin.SprintSpeed.Value);
                    newWorkTime = workTime * ConvertSpeedToMultiTime(Plugin.SprintSpeed.Value);
                    SmartActionLogger.Log(
                        $"[AdjustHealingWorkTime] ⏱️ Sprint → LoopTime: {newLoop:F2} | WorkTime: {newWorkTime:F2}");
                }

                SmartActionLogger.Log($"[AdjustHealingWorkTime] ⏱️ Sprint → Float12: {newFloat12:F2}");
                break;

            default:
                if (isStarted)
                {
                    newLoop = LoopTime.OriginalLoopTime;
                    newWorkTime = workTime;
                    SmartActionLogger.Log(
                        $"[AdjustHealingWorkTime] ⏱️ Unchanged → WorkTime: {newFloat12:F2} | LoopTime: {newLoop:F2}");
                }

                break;
        }

        SetNewCalculateValue(
            float12Field,
            workTimeField,
            effect,
            newFloat12,
            newWorkTime,
            newLoop,
            isStarted
        );
    }

    private static void SetNewCalculateValue(
        FieldInfo float12Field,
        PropertyInfo workTimeField,
        IEffect effect,
        float newFloat12,
        float newWorkTime,
        float newLoop,
        bool isStarted)
    {
        if (float12Field != null)
        {
            try
            {
                float12Field.SetValue(effect, newFloat12);
                SmartActionLogger.Log($"[AdjustHealingWorkTime] ✅ Set float12 to {newFloat12:F2}");
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error(
                    $"[AdjustHealingWorkTime] ⚠️ Failed to set float12: {ex.Message}");
            }
        }

        if (isStarted && workTimeField != null)
        {
            try
            {
                workTimeField.SetValue(effect, newWorkTime);
                SmartActionLogger.Log($"[AdjustHealingWorkTime] ✅ Set work state time to {newWorkTime:F2}");
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error(
                    $"[AdjustHealingWorkTime] ⚠️ Failed to set effect newWorkTime: {ex.Message}");
            }
        }

        if (isStarted && newLoop > 0f)
        {
            try
            {
                LoopTime.SetLoopTime(newLoop);
                SmartActionLogger.Log($"[AdjustHealingWorkTime] ✅ Set loopTime to {newLoop:F2}");
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error(
                    $"[AdjustHealingWorkTime] ⚠️ Failed to set effect LoopTime: {ex.Message}");
            }
        }
    }

    private static float ConvertSpeedToFloat(int configSpeed)
    {
        return configSpeed / 10.0f;
    }

    private static float ConvertSpeedToMultiTime(int configValue)
    {
        return 10f / configValue;
    }
}