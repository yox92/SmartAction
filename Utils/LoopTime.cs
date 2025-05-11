using System;
using System.Reflection;
using EFT.HealthSystem;
using HarmonyLib;

namespace SmartAction.Utils
{
    public abstract class LoopTime
    {
        public static float OriginalLoopTime { get; private set; }
        private static FieldInfo LoopTimeField;
        private static bool _isInitialized = false;

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
                // Get LoopTime field by reflection
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
                    SmartActionLogger.Error("[LoopTime] Could not find MedEffect");
                    return;
                }

                var medEffectInstance = medEffectField.GetValue(gclass2823Instance);
                if (medEffectInstance == null)
                {
                    SmartActionLogger.Error("[LoopTime] Could not get MedEffect instance");
                    return;
                }

                LoopTimeField = AccessTools.Field(medEffectInstance.GetType(), "LoopTime");
                if (LoopTimeField == null)
                {
                    SmartActionLogger.Error("[LoopTime] Could not find LoopTime");
                    return;
                }

                // Store original value
                OriginalLoopTime = (float)LoopTimeField.GetValue(medEffectInstance);
                _isInitialized = true;

                SmartActionLogger.Info($"[LoopTime] ✅ Original LoopTime value: {OriginalLoopTime:F2}");
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error($"[LoopTime] ⚠️ Error during initialization: {ex.Message}");
            }
        }

        public static void SetLoopTime(float newValue)
        {
            try
            {
                if (!_isInitialized)
                {
                    SmartActionLogger.Error("[LoopTime] ⚠️ LoopTime is not initialized.");
                    return;
                }

                if (LoopTimeField == null)
                {
                    SmartActionLogger.Error("[LoopTime] ⚠️ LoopTimeField is null.");
                    return;
                }

                // Re-get instance to ensure no desync
                var gclass2823Property = AccessTools.Property(typeof(ActiveHealthController.GClass2813), "GClass2823_0");
                var gclass2823Instance = gclass2823Property.GetValue(null);

                if (gclass2823Instance == null)
                {
                    SmartActionLogger.Error("[LoopTime] ⚠️ Could not get instance of GClass2823_0");
                    return;
                }

                var medEffectField = AccessTools.Field(gclass2823Instance.GetType(), "MedEffect");
                if (medEffectField == null)
                {
                    SmartActionLogger.Error("[LoopTime] ⚠️ Could not find MedEffect");
                    return;
                }

                var medEffectInstance = medEffectField.GetValue(gclass2823Instance);
                if (medEffectInstance == null)
                {
                    SmartActionLogger.Error("[LoopTime] ⚠️ Could not get MedEffect instance");
                    return;
                }

                // Apply new loop time
                LoopTimeField.SetValue(medEffectInstance, newValue);

                // Verification log
                var afterSet = (float)LoopTimeField.GetValue(medEffectInstance);
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
                SmartActionLogger.Info("[EffectTimeManager] 🔄 LoopTime restored to original value");
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error(
                    $"[EffectTimeManager] ⚠️ Error while restoring LoopTime: {ex.Message}");
            }
        }
    }
}