using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using XRL.World;
using XRL.World.Parts;
using Skill = XRL.World.Parts.Skill;
using Effects = XRL.World.Effects;

namespace Klehrik_Blocking
{
    public class ShieldCheck
    {
        public static bool IsNotPassiveOnly(GameObject Shield)
        {
            if (Shield.GetPart<Klehrik_ShieldPassiveOnly>() != null)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch]
    public class ShieldSlamSkill
    {
        static MethodInfo s_targetCall = typeof(GameObject).GetMethod(nameof(GameObject.GetShieldWithHighestAV));
        static MethodInfo s_filterMethod = typeof(ShieldCheck).GetMethod(nameof(ShieldCheck.IsNotPassiveOnly));
        static ConstructorInfo s_delegateConstructor = AccessTools.Constructor(typeof(Func<GameObject, bool>), new[] { typeof(object), typeof(IntPtr) });

        static IEnumerable<MethodBase> TargetMethods()
        {
            // Patch skill use and Charge free slam
            yield return AccessTools.Method(typeof(Skill.Shield_Slam), nameof(Skill.Shield_Slam.PerformShieldSlam));
            yield return AccessTools.Method(typeof(Skill.Shield_Slam), nameof(Skill.Shield_Slam.FireEvent), new[] { typeof(Event) });
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions)
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, s_targetCall))
                .ThrowIfInvalid("Could not find call to GetShieldWithHighestAV")

                // Insert filter predicate as first argument (after first Ldnull)
                .Advance(-1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldftn, s_filterMethod))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Newobj, s_delegateConstructor));

            // DEBUG
            // string dump = string.Join("\n", codeMatcher.Instructions());
            // UnityEngine.Debug.Log("===== Patch Start =====\n" + dump + "\n===== Patch End =====");
            
            return codeMatcher.Instructions();
        }
    }

    [HarmonyPatch]
    public class ShieldWallSkill
    {
        static MethodInfo s_targetCall = typeof(GameObject).GetMethod(nameof(GameObject.GetShield));
        static MethodInfo s_filterMethod = typeof(ShieldCheck).GetMethod(nameof(ShieldCheck.IsNotPassiveOnly));
        static ConstructorInfo s_delegateConstructor = AccessTools.Constructor(typeof(Func<GameObject, bool>), new[] { typeof(object), typeof(IntPtr) });

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Skill.Shield_ShieldWall), nameof(Skill.Shield_ShieldWall.HandleEvent), new Type[] { typeof(CommandEvent) });
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions)
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, s_targetCall))
                .ThrowIfInvalid("Could not find call to GetShield")

                // Insert filter predicate as first argument (after first Ldnull)
                .Advance(-1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldftn, s_filterMethod))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Newobj, s_delegateConstructor));
            
            return codeMatcher.Instructions();
        }
    }

    [HarmonyPatch(typeof(Effects.ShieldWall))]
    public class ShieldWallEffect
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Effects.ShieldWall.HandleEvent), new Type[] { typeof(EndTurnEvent) })]
        static void Prefix(Effects.ShieldWall __instance, ref bool __result, EndTurnEvent E)
        {
            if (__instance.Object.GetShield(ShieldCheck.IsNotPassiveOnly) == null)
            {
                __instance.Object.RemoveEffect(__instance);
            }
        }
    }
}