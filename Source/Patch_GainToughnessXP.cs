using System;
using HarmonyLib;
using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// Harmony postfix on <see cref="DamageWorker_AddInjury.ApplyDamageToPart"/> that feeds
/// every successful injury through <see cref="CompToughnessCache.NotifyDamageApplied"/>
/// for XP gain. Signature-pinned so a vanilla signature change fails loudly at patch time.
/// </summary>
[HarmonyPatch(typeof(DamageWorker_AddInjury))]
[HarmonyPatch("ApplyDamageToPart")]
[HarmonyPatch(new Type[]
{
	typeof(DamageInfo),
	typeof(Pawn),
	typeof(DamageWorker.DamageResult)
})]
public static class Patch_GainToughnessXP
{
    private static ThoughnessSkillSettings Settings =>
    LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

    /// <summary>Awards Toughness XP after vanilla applies the injury.</summary>
    /// <param name="dinfo">The damage event being applied.</param>
    /// <param name="pawn">The pawn taking the hit.</param>
    /// <param name="result">Vanilla's damage result; <c>totalDamageDealt</c> drives XP.</param>
    [HarmonyPostfix]
	public static void Postfix(DamageInfo dinfo, Pawn pawn, DamageWorker.DamageResult result)
	{
        if (pawn?.skills != null && !(result.totalDamageDealt <= 0f))
		{
			if (!Settings.patchColonists && ToughnessSkillMod.IsColonist(pawn)) {
				return;
            }
			if (!Settings.patchFriendlyPawns && ToughnessSkillMod.IsFriendlyPawn(pawn)) {
				return;
            }
			if (!Settings.patchHostilePawns && ToughnessSkillMod.IsHostilePawn(pawn))
            {
				return;
            }
			if (!Settings.patchFriendlyPawns && ToughnessSkillMod.IsIndependentPawn(pawn))
            {
                return;
            }

            // Fatal exceptions must propagate (IsFatal filter); everything else is
            // deduped to one log line instead of spamming per-hit inside the damage loop.
            try
            {
                if (pawn.TryGetComp<CompToughnessCache>() != null)
                {
                    CompToughnessCache.NotifyDamageApplied(dinfo, result.totalDamageDealt, pawn);
                }
            }
            catch (Exception e) when (!e.IsFatal())
            {
                Log.ErrorOnce($"[Toughness] NotifyDamageApplied failed: {e}", "ToughnessDamageXP".GetHashCode(StringComparison.Ordinal));
            }
		}
	}
}
