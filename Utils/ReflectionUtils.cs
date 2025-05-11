using System;
using System.Collections.Concurrent;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SmartAction.Patch;

namespace SmartAction.Utils;

public abstract class ReflectionUtils
{
    private static readonly ConcurrentDictionary<string, Delegate> FieldCache =
        new ConcurrentDictionary<string, Delegate>();

    public static (Player player, Item medItem, bool isValid) GetMedEffectContext(object instance, string hookName)
    {
        var medEffectType = AccessTools.Inner(typeof(ActiveHealthController), "MedEffect");
        if (instance.GetType() != medEffectType)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ Not MedEffect type");
            return (null, null, false);
        }

        var medItemProperty = medEffectType.GetProperty("MedItem");
        if (medItemProperty?.GetValue(instance) is not Item medItem)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ No MedItem found");
            return (null, null, false);
        }

        if (PatchDoMedEffect.LastHealingItem != medItem)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ Not the last healing item");
            return (null, medItem, false);
        }

        var healthControllerField = AccessTools.Field(medEffectType, "activeHealthController_0");
        if (healthControllerField?.GetValue(instance) is not ActiveHealthController healthController)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ Unable to get ActiveHealthController");
            return (null, medItem, false);
        }

        var playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");
        if (playerField?.GetValue(healthController) is not Player player)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ Unable to get Player");
            return (null, medItem, false);
        }

        if (!player.IsYourPlayer || !healthController.IsAlive)
            return (player, medItem, false);


        return (player, medItem, true);
    }
    
    private static Type GetNestedType(Type parentType, string nestedTypeName)
    {
        return AccessTools.Inner(parentType, nestedTypeName);
    }
    
    public static MethodBase GetNestedMethod(Type parentType, string nestedTypeName, string methodName)
    {
        var nestedType = GetNestedType(parentType, nestedTypeName);
        return AccessTools.Method(nestedType, methodName);
    }
    
    
    public static FieldInfo FindLoopTimeField(IEffect effect)
    {
        if (effect is ActiveHealthController.GClass2813 gclassEffect)
        {
            SmartActionLogger.Log($"[INFO] Effect cast successful: {gclassEffect.GetType().Name}");
        }
        else
        {
            SmartActionLogger.Log($"[ERROR] Effect {effect.GetType().Name} is not GClass2813");
        }
        
        var gclass2823Property = AccessTools.Property(typeof(ActiveHealthController.GClass2813), "GClass2823_0");
        if (gclass2823Property == null)
        {
            SmartActionLogger.Log($"[MedEffect.Added] ⚠️ Unable to find GClass2823_0");
            return null;
        }

        var gclass2823Instance = gclass2823Property.GetValue(null);
        if (gclass2823Instance == null)
        {
            SmartActionLogger.Log($"[MedEffect.Added] ⚠️ Unable to get GClass2823_0 instance");
            return null;
        }

        var medEffectField = AccessTools.Field(gclass2823Instance.GetType(), "MedEffect");
        if (medEffectField == null)
        {
            SmartActionLogger.Log($"[SpeedTicksByMovement] ⚠️ Unable to find MedEffect");
            return null;
        }

        var medEffectInstance = medEffectField.GetValue(gclass2823Instance);
        if (medEffectInstance == null)
        {
            SmartActionLogger.Log($"[SpeedTicksByMovement] ⚠️ Unable to get MedEffect instance");
            return null;
        }

        var loopTimeField = AccessTools.Field(medEffectInstance.GetType(), "LoopTime");
        if (loopTimeField == null)
        {
            SmartActionLogger.Log($"[SpeedTicksByMovement] ⚠️ Unable to find LoopTime");
            return null;
        }
        return loopTimeField;
    }
    
    public static FieldInfo FindField(Type type, string name)
    {
        while (type != null)
        {
            var field = type.GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null) return field;
            type = type.BaseType;
        }

        return null;
    }

    public static PropertyInfo FindProperty(Type type, string name)
    {
        while (type != null)
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (property != null) return property;
            type = type.BaseType;
        }

        return null;
    }

}