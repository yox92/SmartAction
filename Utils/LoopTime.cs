using System;
using System.Reflection;
using EFT.HealthSystem;
using HarmonyLib;

namespace SmartAction.Utils
{
    public abstract class LoopTime
    {
        public static float OriginalLoopTime { get; private set; }
        private static FieldInfo _loopTimeField;
        private static bool _isInitialized = false;
        private static object _medEffectInstance;

        public static void Initialize()
        {
            if (_isInitialized)
            {
                SmartActionLogger.Info("[LoopTime] Already initialized.");
                return;
            }

            SmartActionLogger.Info("[LoopTime] Initializing original LoopTime...");

            try
            {
                var gclass2823Property = AccessTools.Property(typeof(ActiveHealthController.GClass2813), "GClass2823_0");
                if (gclass2823Property == null)
                {
                    SmartActionLogger.Error($"❌ [LoopTime] Could not find property GClass2823_0");
                    return;
                }

                var gclass2823Instance = gclass2823Property.GetValue(null);
                if (gclass2823Instance == null)
                {
                    SmartActionLogger.Error($"❌ [LoopTime] Could not get instance of GClass2823_0");
                    return;
                }

                var medEffectField = AccessTools.Field(gclass2823Instance.GetType(), "MedEffect");
                if (medEffectField == null)
                {
                    SmartActionLogger.Error("❌ [LoopTime] Could not find MedEffect");
                    return;
                }

                _medEffectInstance = medEffectField.GetValue(gclass2823Instance);
                if (_medEffectInstance == null)
                {
                    SmartActionLogger.Error("❌ [LoopTime] Could not get MedEffect instance");
                    return;
                }

                _loopTimeField = ReflectionUtils.GetOrCacheField(_medEffectInstance.GetType(), "LoopTime");
                if (_loopTimeField == null)
                {
                    _loopTimeField = AccessTools.Field(_medEffectInstance.GetType(), "LoopTime");
                    if (_loopTimeField == null)
                    {
                        SmartActionLogger.Error("❌ [LoopTime] Could not find LoopTime");
                        return;
                    }
                }

                // Store original value
                OriginalLoopTime = (float)_loopTimeField.GetValue(_medEffectInstance);
                _isInitialized = true;

                SmartActionLogger.Info($"[LoopTime] ✅ Original LoopTime value: {OriginalLoopTime:F2}");
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error($"❌ [LoopTime] Error during initialization: {ex.Message}");
            }
        }

        public static void SetLoopTime(float newValue)
        {
            try
            {
                if (!_isInitialized)
                {
                    SmartActionLogger.Warn("[LoopTime] LoopTime is not initialized.");
                    return;
                }

                if (_loopTimeField == null || _medEffectInstance == null)
                {
                    SmartActionLogger.Error("❌ [LoopTime] LoopTimeField or MedEffect instance is null.");
                    return;
                }

                // Apply new loop time directement
                _loopTimeField.SetValue(_medEffectInstance, newValue);

                // Verification log
                var afterSet = (float)_loopTimeField.GetValue(_medEffectInstance);
                if (Math.Abs(afterSet - newValue) < 0.01f)
                {
                    SmartActionLogger.Log($"[LoopTime] ✅ LoopTime successfully modified: {afterSet:F2}");
                }
                else
                {
                    SmartActionLogger.Error(
                        $"[LoopTime] ❌ Failed to update LoopTime: current value {afterSet:F2}, expected {newValue:F2}");
                }
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error($"[LoopTime] ⚠️ Unable to update LoopTime: {ex.Message}");
                
                _isInitialized = false;
                try 
                {
                    Initialize();
                    if (_isInitialized)
                    {
                        if (_loopTimeField != null) _loopTimeField.SetValue(_medEffectInstance, newValue);
                        SmartActionLogger.Log($"[LoopTime] ✅ LoopTime successfully modified after reinitialization");
                    }
                }
                catch (Exception reinitEx)
                {
                    SmartActionLogger.Error($"[LoopTime] ⚠️ Reinit failed: {reinitEx.Message}");
                }
            }
        }

        /// <summary>
        /// Restores the original LoopTime value
        /// </summary>
        public static void RestoreOriginalLoopTime()
        {
            try
            {
                SetLoopTime(OriginalLoopTime);
                SmartActionLogger.Info("[LoopTime] 🔄 LoopTime restored to original value");
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error(
                    $"[LoopTime] ⚠️ Error while restoring LoopTime: {ex.Message}");
            }
        }
    }
}