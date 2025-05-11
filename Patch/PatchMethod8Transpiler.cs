using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using EFT;
using HarmonyLib;
using SmartAction.Utils;

namespace SmartAction.Patch
{
    [HarmonyPatch(typeof(Player.MedsController.Class1172), "method_8")]
    public static class PatchMethod8
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = instructions.ToList();
            var interceptorMethod = AccessTools.Method(typeof(SetActiveParamInterceptor), "SetBlockState");
            
            for (var i = 0; i < code.Count - 4; i++)
            {
                if (code[i].opcode != OpCodes.Ldarg_0 ||
                    code[i + 1].opcode != OpCodes.Ldfld || code[i + 1].operand is not FieldInfo field1 ||
                    field1.Name != "medsController_0" ||
                    code[i + 2].opcode != OpCodes.Ldfld || code[i + 2].operand is not FieldInfo field2 ||
                    field2.Name != "firearmsAnimator_0" ||
                    code[i + 3].opcode != OpCodes.Brfalse)
                    continue;

                for (var j = i + 4; j < code.Count; j++)
                {
                    if ((code[j].opcode != OpCodes.Callvirt &&
                         code[j].opcode != OpCodes.Call) ||
                        code[j].operand is not MethodInfo methodInfo ||
                        methodInfo.Name != "SetActiveParam" ||
                        methodInfo.DeclaringType != typeof(ObjectInHandsAnimator))
                        continue;

                    SmartActionLogger.Log($"[PatchMethod8Transpiler] ✅ SetActiveParam ID : {j}");

                    code[j].operand = interceptorMethod;
                    break;
                }
            }

            return code.AsEnumerable();
        }
        
    }
    
    public static class SetActiveParamInterceptor
    {
        public static bool BlockSetActiveParam = false;

        public static void SetBlockState(ObjectInHandsAnimator animator, bool active, bool resetLeftHand)
        {
            SmartActionLogger.Log($"[Interceptor] Intercept SetActiveParam with active={active}, resetLeftHand={resetLeftHand}");
            
            if (BlockSetActiveParam)
            {
                SmartActionLogger.Log($"[Interceptor] 🚫 Bloack Succes SetActiveParam");
                return;
            }
            
            animator.SetActiveParam(active, resetLeftHand);
        }
    }
}