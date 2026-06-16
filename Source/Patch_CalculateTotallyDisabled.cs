using HarmonyLib;
using RimWorld;
using System.Runtime;
using Verse;

namespace ToughnessSkillMod
{
    [HarmonyPatch(typeof(SkillRecord))]
    [HarmonyPatch("CalculateTotallyDisabled")]
    public static class Patch_CalculateTotallyDisabled
    {
        private static ThoughnessSkillSettings Settings =>
        LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

        public static void Postfix(SkillRecord __instance, ref bool __result)
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                __result = false;
                return;
            }

            Pawn pawn = __instance.Pawn;
            if (__instance.def == DefDatabase<SkillDef>.GetNamed("Toughness", errorOnFail: false))
            {
                CompToughnessCache comp = pawn.TryGetComp<CompToughnessCache>();
                if (comp != null)
                {
                    if (!comp.compintitialised)
                    {
                        __result = false;
                        return;
                    }
                }

                if (!Settings.patchColonists && ToughnessSkillMod.IsColonist(pawn))
                {
                    __result = true;
                    return;
                }
                if (!Settings.patchFriendlyPawns && ToughnessSkillMod.IsFriendlyPawn(pawn))
                {
                    __result = true;
                    return;
                }
                if (!Settings.patchHostilePawns && ToughnessSkillMod.IsHostilePawn(pawn))
                {
                    __result = true;
                    return;
                }
                if (!Settings.patchFriendlyPawns && ToughnessSkillMod.IsIndependentPawn(pawn))
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}