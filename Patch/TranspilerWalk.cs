using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using EFT;
using HarmonyLib;
using SmartAction.Utils;

namespace SmartAction.Patch
{
    [HarmonyPatch(typeof(MovementContext), nameof(MovementContext.CanWalk), MethodType.Getter)]
    public static class CanWalkTranspilerPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
        var methodCheck = AccessTools.Method(typeof(MovementContext), nameof(MovementContext.PhysicalConditionIs));

        for (var i = 0; i < code.Count - 5; i++)
        {
            var loadThis      = code[i];
            var loadValue     = code[i + 1];
            var callCondition = code[i + 2];
            var branchFalse   = code[i + 3];
            var pushFalse     = code[i + 4];
            var returnFalse   = code[i + 5];

            var match =
                loadThis.opcode == OpCodes.Ldarg_0 &&
                (loadValue.opcode == OpCodes.Ldc_I4 || loadValue.opcode == OpCodes.Ldc_I4_S) &&
                callCondition.Calls(methodCheck) &&
                (branchFalse.opcode == OpCodes.Brfalse || branchFalse.opcode == OpCodes.Brfalse_S) &&
                pushFalse.opcode == OpCodes.Ldc_I4_0 &&
                returnFalse.opcode == OpCodes.Ret;

            if (!match)
                continue;

            var condition = (EPhysicalCondition)Convert.ToInt32(loadValue.operand);
            if (condition != EPhysicalCondition.HealingLegs)
                continue;

            var hasLabels = false;
            for (var j = 0; j <= 5; j++)
            {
                if (code[i + j].labels.Count <= 0) 
                    continue;
                SmartActionLogger.Warn($"[CanWalkTranspiler] code[{i + j}] porte {code[i + j].labels.Count} label(s), bloc ignoré.");
                hasLabels = true;
                break;
            }

            if (hasLabels)
                continue;

            code[i]     = new CodeInstruction(OpCodes.Ldc_I4_1);
            code[i + 1] = new CodeInstruction(OpCodes.Ret);
            for (var j = 2; j <= 5; j++)
                code[i + j] = new CodeInstruction(OpCodes.Nop);
            SmartActionLogger.Info($"[CanWalkTranspiler] 🛑 Bloc {condition} neutralise");
        }

        return code;
    }
        
    }
}
// .method public hidebysig virtual newslot specialname instance bool
// get_CanWalk() cil managed
// {
//     .maxstack 8
//
//     // [2581 7 - 2581 115]
//     IL_0000: ldarg.0      // this
//     IL_0001: ldc.i4       256 // 0x00000100
//     IL_0006: call         instance bool EFT.MovementContext::PhysicalConditionIs(valuetype EFT.EPhysicalCondition)
//     IL_000b: brfalse.s    IL_000f
//
//     IL_000d: ldc.i4.0
//     IL_000e: ret
//     IL_000f: ldarg.0      // this
//     IL_0010: ldfld        class EFT.ObstacleCollision.IObstacleCollisionFacade EFT.MovementContext::_obstacleCollisionFacade
//     IL_0015: callvirt     instance bool EFT.ObstacleCollision.IObstacleCollisionFacade::CanMove()
//     IL_001a: brtrue.s     IL_001e
//     IL_001c: ldc.i4.0
//     IL_001d: ret
//     IL_001e: ldc.i4.1
//     IL_001f: ret

 // end of method MovementContext::get_CanWalk


