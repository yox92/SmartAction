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
                SmartActionLogger.Info("[LoopTime] Déjà initialisé.");
                return;
            }

            SmartActionLogger.Info("[LoopTime] Initialisation de LoopTime d'origine...");

            try
            {
                // Récupération du champ LoopTime par réflexion
                var gclass2823Property = AccessTools.Property(typeof(ActiveHealthController.GClass2813), "GClass2823_0");
                if (gclass2823Property == null)
                {
                    SmartActionLogger.Error($"❌ [LoopTime] Impossible de trouver la propriété GClass2823_0");
                    return;
                }

                var gclass2823Instance = gclass2823Property.GetValue(null);
                if (gclass2823Instance == null)
                {
                    SmartActionLogger.Error($"❌ [LoopTime] Impossible de récupérer l'instance de GClass2823_0");
                    return;
                }

                var medEffectField = AccessTools.Field(gclass2823Instance.GetType(), "MedEffect");
                if (medEffectField == null)
                {
                    SmartActionLogger.Error("[LoopTime] Impossible de trouver MedEffect");
                    return;
                }

                var medEffectInstance = medEffectField.GetValue(gclass2823Instance);
                if (medEffectInstance == null)
                {
                    SmartActionLogger.Error("[LoopTime] Impossible de récupérer l'instance de MedEffect");
                    return;
                }

                LoopTimeField = AccessTools.Field(medEffectInstance.GetType(), "LoopTime");
                if (LoopTimeField == null)
                {
                    SmartActionLogger.Error("[LoopTime] Impossible de trouver LoopTime");
                    return;
                }

                // On stocke la valeur d'origine
                OriginalLoopTime = (float)LoopTimeField.GetValue(medEffectInstance);
                _isInitialized = true;

                SmartActionLogger.Info($"[LoopTime] ✅ Valeur d'origine LoopTime : {OriginalLoopTime:F2}");
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error($"[LoopTime] ⚠️ Erreur lors de l'initialisation : {ex.Message}");
            }
        }

        public static void SetLoopTime(float newValue)
        {
            try
            {
                if (!_isInitialized)
                {
                    SmartActionLogger.Error("[LoopTime] ⚠️ LoopTime n'est pas initialisé.");
                    return;
                }

                if (LoopTimeField == null)
                {
                    SmartActionLogger.Error("[LoopTime] ⚠️ LoopTimeField est null.");
                    return;
                }

                // Re-récupérer l'instance pour s'assurer qu'on n'a pas été désynchronisé
                var gclass2823Property = AccessTools.Property(typeof(ActiveHealthController.GClass2813), "GClass2823_0");
                var gclass2823Instance = gclass2823Property.GetValue(null);

                if (gclass2823Instance == null)
                {
                    SmartActionLogger.Error("[LoopTime] ⚠️ Impossible de récupérer l'instance de GClass2823_0");
                    return;
                }

                var medEffectField = AccessTools.Field(gclass2823Instance.GetType(), "MedEffect");
                if (medEffectField == null)
                {
                    SmartActionLogger.Error("[LoopTime] ⚠️ Impossible de trouver MedEffect");
                    return;
                }

                var medEffectInstance = medEffectField.GetValue(gclass2823Instance);
                if (medEffectInstance == null)
                {
                    SmartActionLogger.Error("[LoopTime] ⚠️ Impossible de récupérer l'instance de MedEffect");
                    return;
                }

                // Appliquer le nouveau temps de boucle
                LoopTimeField.SetValue(medEffectInstance, newValue);

                // Log de vérification
                var afterSet = (float)LoopTimeField.GetValue(medEffectInstance);
                if (Math.Abs(afterSet - newValue) < 0.01f)
                {
                    SmartActionLogger.Log($"[LoopTime] ✅ LoopTime modifié avec succès : {afterSet:F2}");
                }
                else
                {
                    SmartActionLogger.Error(
                        $"[LoopTime] ❌ Échec de la mise à jour LoopTime : valeur actuelle {afterSet:F2}, attendue {newValue:F2}");
                }
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error($"[LoopTime] ⚠️ Impossible de mettre à jour LoopTime : {ex.Message}");
            }
        }

        /// <summary>
        /// Restaure la valeur d'origine de LoopTime
        /// </summary>
        public static void RestoreOriginalLoopTime()
        {
            try
            {
                SetLoopTime(OriginalLoopTime);
                SmartActionLogger.Info("[EffectTimeManager] 🔄 LoopTime restauré à sa valeur d'origine");
            }
            catch (Exception ex)
            {
                SmartActionLogger.Error(
                    $"[EffectTimeManager] ⚠️ Erreur lors de la restauration de LoopTime : {ex.Message}");
            }
        }
    }
}