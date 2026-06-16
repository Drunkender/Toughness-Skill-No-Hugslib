using System;
using HarmonyLib;
using Verse;

namespace ToughnessSkillMod;

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

            pawn.TryGetComp<CompToughnessCache>()?.NotifyDamageApplied(dinfo, result.totalDamageDealt, pawn);
		}
	}
}
