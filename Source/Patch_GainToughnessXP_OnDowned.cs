using HarmonyLib;
using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// Harmony postfix on <see cref="Pawn_HealthTracker.MakeDowned"/> that awards a lump of
/// Toughness XP when a pawn is downed by qualifying damage (see
/// <see cref="CompToughnessCache.NotifyDowned"/> for the eligibility rules).
/// </summary>
/// <remarks>
/// <paramref name="___pawn"/> is Harmony's field-injection syntax for the tracker's
/// private <c>pawn</c> field — Harmony 3 type-checks it at patch time, so this is both
/// faster and safer than the old <c>Traverse</c> reflection it replaced.
/// </remarks>
[HarmonyPatch(typeof(Pawn_HealthTracker))]
[HarmonyPatch("MakeDowned")]
public static class Patch_GainToughnessXP_OnDowned
{
	private static ThoughnessSkillSettings Settings =>
		LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

	/// <summary>Awards downed XP after vanilla finishes the state transition.</summary>
	/// <param name="___pawn">The pawn that owns this health tracker (injected from the private field).</param>
	/// <param name="dinfo">The damage that caused the down, or null for non-damage downs.</param>
	/// <param name="hediff">The hediff that caused the down, if any.</param>
	[HarmonyPostfix]
	public static void Postfix(Pawn ___pawn, DamageInfo? dinfo, Hediff hediff)
	{
		// Faction gates mirror the damage-XP patch: each toggle controls one pawn class.
		if (!Settings.patchColonists && ToughnessSkillMod.IsColonist(___pawn)) {
			return;
		}
		if (!Settings.patchFriendlyPawns && ToughnessSkillMod.IsFriendlyPawn(___pawn))
		{
			return;
		}
		if (!Settings.patchHostilePawns && ToughnessSkillMod.IsHostilePawn(___pawn))
		{
			return;
		}
		if (!Settings.patchFriendlyPawns && ToughnessSkillMod.IsIndependentPawn(___pawn))
		{
			return;
		}

		// Comp presence is the gate: only Human pawns carry CompToughnessCache.
		if (___pawn?.TryGetComp<CompToughnessCache>() != null)
		{
			CompToughnessCache.NotifyDowned(dinfo, hediff, ___pawn);
		}
	}
}
