using EFT;
using HarmonyLib;

namespace SmartAction.Patch
{
    public static class PatchSprint
    {
        [HarmonyPatch(typeof(MovementContext), nameof(MovementContext.CanSprint), MethodType.Getter)]
        public static class PatchCanSprintRewrite
        {
            [HarmonyPrefix]
            public static bool Prefix(MovementContext __instance, ref bool __result)
            {
                var result =
                    !__instance.PhysicalConditionIs(EPhysicalCondition.SprintDisabled) &&
                    (
                        __instance.PhysicalConditionIs(EPhysicalCondition.OnPainkillers) ||
                        (
                            !__instance.PhysicalConditionIs(EPhysicalCondition.RightLegDamaged) &&
                         !__instance.PhysicalConditionIs(EPhysicalCondition.LeftLegDamaged)
                            )
                    );

                __result = result;
                return false; 
            }
        }
    }
}

// 
// get_CanSprint() cil managed
// {
//     .maxstack 2
//
//     // [2565 7 - 2565 388]
//     IL_0000: ldarg.0      // this
//     IL_0001: ldc.i4       1024 // 0x00000400
//     IL_0006: call         instance bool EFT.MovementContext::PhysicalConditionIs(valuetype EFT.EPhysicalCondition)
//     IL_000b: brfalse.s    IL_000f
//
//     IL_000d: ldc.i4.0
//     IL_000e: ret
//     IL_000f: ldarg.0      // this
//     IL_0010: ldc.i4       128 // 0x00000080
//     IL_0015: call         instance bool EFT.MovementContext::PhysicalConditionIs(valuetype EFT.EPhysicalCondition)
//     IL_001a: brfalse.s    IL_001e
//     IL_001c: ldc.i4.0
//     IL_001d: ret
//     IL_001e: ldarg.0      // this
//     IL_001f: ldc.i4       256 // 0x00000100
//     IL_0024: call         instance bool EFT.MovementContext::PhysicalConditionIs(valuetype EFT.EPhysicalCondition)
//     IL_0029: brfalse.s    IL_002d
//     IL_002b: ldc.i4.0
//     IL_002c: ret
//     IL_002d: ldarg.0      // this
//     IL_002e: ldc.i4.1
//     IL_002f: call         instance bool EFT.MovementContext::PhysicalConditionIs(valuetype EFT.EPhysicalCondition)
//     IL_0034: brfalse.s    IL_0038
//     IL_0036: ldc.i4.1
//     IL_0037: ret
//     IL_0038: ldarg.0      // this
//     IL_0039: ldc.i4.4
//     IL_003a: call         instance bool EFT.MovementContext::PhysicalConditionIs(valuetype EFT.EPhysicalCondition)
//     IL_003f: brfalse.s    IL_0043
//     IL_0041: ldc.i4.0
//     IL_0042: ret
//     IL_0043: ldarg.0      // this
//     IL_0044: ldc.i4.2
//     IL_0045: call         instance bool EFT.MovementContext::PhysicalConditionIs(valuetype EFT.EPhysicalCondition)
//     IL_004a: brfalse.s    IL_004e
//     IL_004c: ldc.i4.0
//     IL_004d: ret
//     IL_004e: ldc.i4.1
//     IL_004f: ret

