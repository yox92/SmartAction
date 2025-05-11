using EFT;
using HarmonyLib;
using SmartAction.Utils;

namespace SmartAction.Boot;

public abstract class StartRaid
{
    [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
    public static class PatchGameWorldOnGameStarted
    {
        private static void Postfix()
        {
            SmartActionLogger.Info("[OnGameStarted] Start raid !");
            LoopTime.Initialize();
        }
    }}