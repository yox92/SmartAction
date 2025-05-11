using EFT;
using HarmonyLib;
using SmartAction.Patch;
using SmartAction.Utils;

namespace SmartAction.Boot;

public abstract class StopRaid
{
    [HarmonyPatch(typeof(ClientGameWorld), nameof(ClientGameWorld.OnDestroy))]
    private class PatchRaidEnd
    {
        private static void Postfix()
        {
            LoopTime.RestoreOriginalLoopTime();
            PatchMedEffectHooks.EffectUpdateCache.Clear();
            SmartActionLogger.Info("[OnDestroy] Raid off");
        }
    }

}