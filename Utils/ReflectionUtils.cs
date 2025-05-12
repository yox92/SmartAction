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
    private static readonly ConcurrentDictionary<(Type, string), FieldInfo> CachedFields = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> CachedProperties = new();
    private static readonly ConcurrentDictionary<(Type, string), MethodInfo> CachedMethods = new();
    private static readonly ConcurrentDictionary<(Type ParentType, string Name), Type> CachedNestedTypes = new();
    

    /// <summary>
    /// Get a field with caching
    /// </summary>
    public static FieldInfo GetOrCacheField(Type type, string fieldName)
    {
        return CachedFields.GetOrAdd((type, fieldName), key =>
        {
            var (targetType, name) = key;
            var fieldInfo =
                targetType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
            {
                SmartActionLogger.Warn($"[Reflection] Field '{name}' not found in type {targetType.FullName}");
            }

            return fieldInfo;
        });
    }

    /// <summary>
    /// Get a property with caching
    /// </summary>
    public static PropertyInfo GetOrCacheProperty(Type type, string propertyName)
    {
        return CachedProperties.GetOrAdd((type, propertyName), key =>
        {
            var (targetType, name) = key;
            var propertyInfo = targetType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null)
            {
                SmartActionLogger.Warn($"[Reflection] Property '{name}' not found in type {targetType.FullName}");
            }

            return propertyInfo;
        });
    }

    /// <summary>
    /// Get a method with caching
    /// </summary>
    public static MethodInfo GetOrCacheMethod(Type type, string methodName)
    {
        return CachedMethods.GetOrAdd((type, methodName), key =>
        {
            var (targetType, name) = key;
            var methodInfo =
                targetType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo == null)
            {
                SmartActionLogger.Warn($"[Reflection] Method '{name}' not found in type {targetType.FullName}");
            }

            return methodInfo;
        });
    }
    
    /// <summary>
    /// Get or cache a nested type
    /// </summary>

    public static Type GetOrCacheNestedType(Type parentType, string nestedTypeName)
    {
        return CachedNestedTypes.GetOrAdd((parentType, nestedTypeName), key =>
        {
            var (type, name) = key;
            var nestedType = AccessTools.Inner(type, name);
            if (nestedType == null)
            {
                SmartActionLogger.Warn($"[Reflection] Nested type '{name}' not found in type {type.FullName}");
            }
            return nestedType;
        });
    }


    /// <summary>
    /// MedEffect context
    /// </summary>
    public static (Player player, Item medItem, bool isValid) GetMedEffectContext(object instance, string hookName)
    {
        var medEffectType = GetOrCacheNestedType(typeof(ActiveHealthController), "MedEffect");
        if (instance.GetType() != medEffectType)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ Not MedEffect type");
            return (null, null, false);
        }

        var medItemProperty = GetOrCacheProperty(medEffectType, "MedItem");
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

        var healthControllerField = GetOrCacheField(medEffectType, "activeHealthController_0");
        if (healthControllerField?.GetValue(instance) is not ActiveHealthController healthController)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ Unable to get ActiveHealthController");
            return (null, medItem, false);
        }

        var playerField = GetOrCacheField(typeof(ActiveHealthController), "Player");
        if (playerField?.GetValue(healthController) is not Player player)
        {
            SmartActionLogger.Log($"[MedEffect.{hookName}] ⚠️ Unable to get Player");
            return (null, medItem, false);
        }

        if (!player.IsYourPlayer || !healthController.IsAlive)
            return (player, medItem, false);

        return (player, medItem, true);
    }

    /// <summary>
    /// Search for a field in a class hierarchy by inheritance
    /// </summary>
    public static FieldInfo FindField(Type type, string name)
    {
        while (type != null)
        {
            var field = GetOrCacheField(type, name);
            if (field != null)
            {
                return field;
            }

            type = type.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Search for a property in a class hierarchy by inheritance
    /// </summary>
    public static PropertyInfo FindProperty(Type type, string name)
    {
        while (type != null)
        {
            var property = GetOrCacheProperty(type, name);
            if (property != null)
            {
                return property;
            }

            type = type.BaseType;
        }

        return null;
    } 
}