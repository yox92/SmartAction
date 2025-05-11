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
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ Aucun MedItem trouvé");
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
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ Impossible de récupérer ActiveHealthController");
            return (null, medItem, false);
        }

        var playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");
        if (playerField?.GetValue(healthController) is not Player player)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ Impossible de récupérer Player");
            return (null, medItem, false);
        }

        if (!player.IsYourPlayer)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] 🔐 Joueur non local - ignoré");
            return (player, medItem, false);
        }

        if (!healthController.IsAlive)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] 🔐 Joueur mort - ignoré");
            return (player, medItem, false);
        }

        return (player, medItem, true);
    }

    /// <summary>
    /// Récupère un type imbriqué (inner class) à partir d'un type parent.
    /// </summary>
    /// <param name="parentType">Le type parent contenant la classe imbriquée.</param>
    /// <param name="nestedTypeName">Le nom de la classe imbriquée.</param>
    /// <returns>Le Type de la classe imbriquée.</returns>
    private static Type GetNestedType(Type parentType, string nestedTypeName)
    {
        return AccessTools.Inner(parentType, nestedTypeName);
    }

    /// <summary>
    /// Récupère une méthode statique ou d'instance depuis un type parent et un nom de méthode.
    /// </summary>
    /// <param name="parentType">Le type parent contenant la méthode.</param>
    /// <param name="nestedTypeName">Le nom de la classe imbriquée.</param>
    /// <param name="methodName">Le nom de la méthode à récupérer.</param>
    /// <returns>La méthode sous forme de MethodBase.</returns>
    public static MethodBase GetNestedMethod(Type parentType, string nestedTypeName, string methodName)
    {
        var nestedType = GetNestedType(parentType, nestedTypeName);
        return AccessTools.Method(nestedType, methodName);
    }
    
    
    public static FieldInfo FindLoopTimeField(IEffect effect)
    {
        if (effect is ActiveHealthController.GClass2813 gclassEffect)
        {
            SmartActionLogger.Log($"[INFO] Effet casté avec succès : {gclassEffect.GetType().Name}");
        }
        else
        {
            SmartActionLogger.Log($"[ERROR] L'effet {effect.GetType().Name} n'est pas un GClass2813");
        }
        
        var gclass2823Property = AccessTools.Property(typeof(ActiveHealthController.GClass2813), "GClass2823_0");
        if (gclass2823Property == null)
        {
            SmartActionLogger.Log($"[MedEffect.Added] ⚠️ Impossible de trouver GClass2823_0");
            return null;
        }

        var gclass2823Instance = gclass2823Property.GetValue(null);
        if (gclass2823Instance == null)
        {
            SmartActionLogger.Log($"[MedEffect.Added] ⚠️ Impossible de récupérer l'instance de GClass2823_0");
            return null;
        }

        var medEffectField = AccessTools.Field(gclass2823Instance.GetType(), "MedEffect");
        if (medEffectField == null)
        {
            SmartActionLogger.Log($"[SpeedTicksByMovement] ⚠️ Impossible de trouver MedEffect");
            return null;
        }

        var medEffectInstance = medEffectField.GetValue(gclass2823Instance);
        if (medEffectInstance == null)
        {
            SmartActionLogger.Log($"[SpeedTicksByMovement] ⚠️ Impossible de récupérer l'instance de MedEffect");
            return null;
        }

        var loopTimeField = AccessTools.Field(medEffectInstance.GetType(), "LoopTime");
        if (loopTimeField == null)
        {
            SmartActionLogger.Log($"[SpeedTicksByMovement] ⚠️ Impossible de trouver LoopTime");
            return null;
        }
        SmartActionLogger.Log($"[DEBUG] Type de LoopTime: {loopTimeField.GetType()}");
        SmartActionLogger.Log($"[DEBUG] Valeur par défaut: {loopTimeField.GetValue(medEffectInstance)}");
        SmartActionLogger.Log($"[DEBUG] Attributs: {loopTimeField.Attributes}");
        SmartActionLogger.Log($"[DEBUG] Type déclaré: {loopTimeField.DeclaringType?.Name}");
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