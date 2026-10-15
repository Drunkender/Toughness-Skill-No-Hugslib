using HarmonyLib;
using RimWorld;
using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// Harmony postfix on <see cref="SkillUI"/>'s private <c>GetSkillDescription</c> that
/// appends the pawn's current damage/bleed multipliers to the Toughness skill tooltip.
/// </summary>
/// <remarks>
/// The target is <c>private static</c> in 1.7 — attribute patching handles privates fine.
/// Faction toggles show a neutral x1.00 rather than hiding the line, so the tooltip stays
/// honest about what the skill would do if enabled.
/// </remarks>
[HarmonyPatch(typeof(SkillUI), "GetSkillDescription")]
public static class Patch_ToughnessSkillTooltip
{
    private static ThoughnessSkillSettings Settings =>
    LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

    /// <summary>Appends the current effect summary to the Toughness skill's tooltip.</summary>
    /// <param name="sk">The skill record being described.</param>
    /// <param name="__result">Vanilla's tooltip text; appended in place.</param>
    [HarmonyPostfix]
	public static void Postfix(SkillRecord sk, ref string __result)
	{
		if (!(sk.def.defName != "Toughness") && sk.Pawn != null)
		{
            Pawn pawn = sk.Pawn;

            // Mirrors CompToughnessCache.CalculateCachedFactors: same piecewise curve so
            // the tooltip reads exactly what the stat parts apply.
            int level = sk.Level;
			float num;
			if (level <= Settings.breakpoint)
			{
				num = 1f + Settings.slopeEarly * (Settings.breakpoint - level);
			}
			else
			{
				float num2 = Settings.maxReduction / (20f - Settings.breakpoint);
				num = 1f - num2 * (level - Settings.breakpoint);
			}
			float num3 = num;

            if (!Settings.patchColonists && ToughnessSkillMod.IsColonist(pawn))
            {
                num3 = 1f;
                num = 1f;
            }
            if (!Settings.patchFriendlyPawns && ToughnessSkillMod.IsFriendlyPawn(pawn))
            {
                num3 = 1f;
                num = 1f;
            }
            if (!Settings.patchHostilePawns && ToughnessSkillMod.IsHostilePawn(pawn))
            {
                num3 = 1f;
                num = 1f;
            }
            if (!Settings.patchFriendlyPawns && ToughnessSkillMod.IsIndependentPawn(pawn))
            {
                num3 = 1f;
                num = 1f;
            }
            __result = __result + "\n\n<b>Current Effects:</b>\n" + $"• Incoming Damage: x{num:0.00}\n" + $"• Bleed Rate: x{num3:0.00}";
		}
	}
}
