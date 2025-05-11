using System.Reflection;
using EFT;
using HarmonyLib;
using SmartAction.Utils;

namespace SmartAction.Patch
{
    [HarmonyPatch(typeof(Player.MedsController.Class1172))]
    public static class PatchEndOfCycle
    {
        [HarmonyPostfix]
        [HarmonyPatch("method_9")]
        public static void Postfix_EndOfCycle(Player.MedsController.Class1172 __instance)
        {
            var medsControllerField = __instance
                .GetType()
                .GetField(
                    "medsController_0", 
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
            var medsController = medsControllerField?.GetValue(__instance);

            var playerField = medsController?
                .GetType()
                .GetField(
                    "_player",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            
            var player = playerField?.GetValue(medsController) as Player;
            if (!player)
                return;
            if (!player.IsYourPlayer && !player.ActiveHealthController?.IsAlive == true)
                return;
            if (((Player.AbstractHandsController)medsController).Item is not MedsItemClass medsItem)
                return;
            if (medsItem != PatchDoMedEffect.LastHealingItem)
                return;
            SetActiveParamInterceptor.BlockSetActiveParam = false;
            SmartActionLogger.Log("[method_9] 🚀 End healing cycle! 🔓 reset SetActiveParam method_8");
        }
    }
}