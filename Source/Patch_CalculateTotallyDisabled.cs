using HarmonyLib;
using RimWorld;
using Verse;

namespace ToughnessSkillMod
{
    /// <summary>
    /// Harmony postfix on <see cref="SkillRecord"/>'s private <c>CalculateTotallyDisabled</c>
    /// that enforces the mod's per-faction toggles: a pawn whose class is disabled in
    /// settings reports the Toughness skill as fully disabled.
    /// </summary>
    /// <remarks>
    /// The result is forced to <c>false</c> outside <see cref="ProgramState.Playing"/> —
    /// during world-gen/menus there is no faction context to evaluate and the "comp not
    /// yet initialised" path would otherwise wedge generated pawns into a disabled state.
    /// </remarks>
    [HarmonyPatch(typeof(SkillRecord))]
    [HarmonyPatch("CalculateTotallyDisabled")]
    public static class Patch_CalculateTotallyDisabled
    {
        private static ThoughnessSkillSettings Settings =>
        LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

        /// <summary>Adjusts the disabled verdict for the Toughness skill only.</summary>
        /// <param name="__instance">The skill record being evaluated.</param>
        /// <param name="__result">Vanilla's verdict; overwritten only for Toughness.</param>
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
