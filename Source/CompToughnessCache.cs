using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// Per-pawn <see cref="ThingComp"/> (injected onto <c>Human</c> via
/// <c>Patches/Comp_ToughnessCache.xml</c>) that caches the Toughness-derived damage and
/// bleed multipliers so the stat parts don't recompute the curve on every stat read.
/// </summary>
/// <remarks>
/// Invariant: <see cref="cachedDamageFactor"/>/<see cref="cachedBleedFactor"/> reflect
/// <see cref="lastLevel"/>; <see cref="UpdateCache"/> re-derives them whenever the skill
/// level or the faction-disable verdict changes. All fields are scribed so caches
/// survive save/load without a recompute pass.
/// </remarks>
public class CompToughnessCache : ThingComp
{
	/// <summary>Skill level the cached factors were computed for; -1 = never computed.</summary>
	private int lastLevel = -1;

	/// <summary>Multiplier applied to <c>IncomingDamageFactor</c> (lower = tougher).</summary>
	public float cachedDamageFactor = 1f;

	/// <summary>Multiplier applied to the mod's <c>FinalBleedRate</c> stat (lower = tougher).</summary>
	public float cachedBleedFactor = 1f;

    /// <summary>True when the Toughness skill is disabled for this pawn (faction toggles).</summary>
    public bool skillDisabled;

    /// <summary>True once <see cref="InitialiseComp"/> has run; gates the disabled-verdict patch.</summary>
    public bool compintitialised;

	/// <summary>
	/// Legacy one-shot flag from the single-trait build (<c>Tough</c> → +1). Still scribed
	/// under its original key so old saves fold that already-applied +1 into
	/// <see cref="traitLevelsApplied"/> instead of double-counting or losing it.
	/// </summary>
	private bool legacyToughTraitApplied;

	/// <summary>
	/// Net trait-derived level delta already folded into <see cref="SkillRecord.levelInt"/>.
	/// Compared against the live trait set on every <see cref="UpdateCache"/> call so trait
	/// additions/removals (dev tools, other mods) re-balance instead of being missed.
	/// </summary>
	private int traitLevelsApplied;

    private static ThoughnessSkillSettings Settings =>
        LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

    /// <summary>
    /// Recomputes the cached factors when the skill level changed, and optionally when the
    /// pawn's skill-disable state may have changed (settings write, faction flip).
    /// </summary>
    /// <param name="pawn">The owning pawn.</param>
    /// <param name="skillDisablesChanged">
    /// True to force a <see cref="SkillRecord.Notify_SkillDisablesChanged"/> re-evaluation —
    /// pass false on stat-read paths (called per stat computation; recalc is expensive).
    /// </param>
	public void UpdateCache(Pawn pawn, bool skillDisablesChanged)
	{
		SkillRecord? skillRecord = pawn.skills?.GetSkill(DefDatabase<SkillDef>.GetNamed("Toughness", errorOnFail: false));
        // GetSkill can return null (skill def absent or pawn has no record) — the 1.6 code
        // dereferenced skillRecord before checking, a latent NRE. Bail out instead.
        if (skillRecord == null)
		{
			return;
		}
        skillRecord.levelInt = AdjustToughnessByTrait(pawn, skillRecord.levelInt);

        if (skillDisablesChanged)
        {
			skillRecord.Notify_SkillDisablesChanged();
            if (skillRecord.TotallyDisabled)
			{
				skillDisabled = true;
				CalculateCachedFactors(pawn, skillRecord);
			}
			else
			{
				skillDisabled = false;
				lastLevel = -1;
			}
		}

        if (skillRecord.levelInt != lastLevel)
		{
            CalculateCachedFactors(pawn, skillRecord);
		}
	}

    /// <summary>
    /// Awards Toughness XP for damage taken, scaled by wound count and severity.
    /// Called from the <see cref="Patch_GainToughnessXP"/> postfix on
    /// <see cref="DamageWorker_AddInjury.ApplyDamageToPart"/>.
    /// </summary>
    /// <param name="dinfo">The damage event (def used for the damage-type bonus).</param>
    /// <param name="totalDamageDealt">Post-mitigation damage actually dealt.</param>
    /// <param name="pawn">The injured pawn.</param>
	public static void NotifyDamageApplied(DamageInfo dinfo, float totalDamageDealt, Pawn pawn)
	{
        if (pawn?.skills == null || totalDamageDealt <= 0f)
		{
			return;
		}
		SkillRecord skill = pawn.skills.GetSkill(DefDatabase<SkillDef>.GetNamed("Toughness", errorOnFail: false));
		if (skill == null || skill.TotallyDisabled)
		{
			return;
		}
		float amount = dinfo.Amount;
		float num = totalDamageDealt * Settings.baseXp;
		float num2 = 1f;
		int num3 = 0;
		float num4 = 0f;
		foreach (Hediff_Injury item in pawn.health?.hediffSet?.hediffs.OfType<Hediff_Injury>() ?? Enumerable.Empty<Hediff_Injury>())
		{
			// Only untended, non-trivial wounds count toward the XP multiplier —
            // tended/scarred injuries shouldn't keep feeding toughness growth.
			if (!item.IsTended() && !(item.Severity <= 0.01f))
			{
				num3++;
				num4 += item.Severity;
			}
		}
        // +5% XP per live wound — being hurt repeatedly is what trains toughness.
		if (num3 > 0)
		{
			num2 += 0.05f * num3;
		}
        // Severity tiers: >5 total severity → ×1.2, >10 → ×1.5 (multiplicative stacking).
		if (num4 > 5f)
		{
			num2 *= 1.2f;
		}
		if (num4 > 10f)
		{
			num2 *= 1.5f;
		}
        // Damage-type bonuses: lingering damage (burns/frostbite) teaches more than
        // instant trauma (bullets/cuts), which still beats everything else.
		if (dinfo.Def == DamageDefOf.Burn || dinfo.Def == DamageDefOf.Frostbite)
		{
			num2 *= 1.25f;
		}
		else if (dinfo.Def == DamageDefOf.Bullet || dinfo.Def == DamageDefOf.Cut)
		{
			num2 *= 1.1f;
		}
		float num5 = num * num2;
		skill.Learn(num5);
		if (Prefs.DevMode && Settings.debugMode)
		{
			Log.Message($"[Toughness] {pawn.NameShortColored} took {amount:0.##} raw damage, reduced to {totalDamageDealt:0.##}. XP granted: {num5:0.##} (x{num2:0.##}). Base XP: {Settings.baseXp:0.##}");
		}
	}

    /// <summary>
    /// Awards a flat Toughness XP bonus (600, scaled by the live-wound multipliers) when the
    /// pawn is downed by qualifying violent damage. Called from the
    /// <see cref="Patch_GainToughnessXP_OnDowned"/> postfix on <see cref="Pawn_HealthTracker.MakeDowned"/>.
    /// </summary>
    /// <param name="dinfo">The downing damage event, or null for non-damage downs.</param>
    /// <param name="hediff">The hediff that downed the pawn; illness hediffs are excluded.</param>
    /// <param name="pawn">The downed pawn.</param>
	public static void NotifyDowned(DamageInfo? dinfo, Hediff hediff, Pawn pawn)
	{
		if (pawn?.skills == null || pawn.Dead)
		{
			return;
		}
		SkillRecord skill = pawn.skills.GetSkill(DefDatabase<SkillDef>.GetNamed("Toughness", errorOnFail: false));
		if (skill == null || skill.TotallyDisabled)
		{
			return;
		}
		bool flag = false;
		if (dinfo.HasValue)
		{
			DamageDef def = dinfo.Value.Def;
			if (def == DamageDefOf.Cut || def == DamageDefOf.Blunt || def == DamageDefOf.Stab || def == DamageDefOf.Bullet || def == DamageDefOf.Bomb)
			{
				flag = true;
			}
		}
        // A down with no qualifying DamageDef still counts if the pawn has live wounds —
        // covers e.g. pain shock from accumulated injuries where dinfo was indirect.
		if (!flag && pawn.health?.hediffSet?.hediffs != null)
		{
			foreach (Hediff_Injury item in pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>())
			{
				if (!item.IsTended() && !(item.Severity <= 0.01f))
				{
					flag = true;
					break;
				}
			}
		}
        // Non-violent downs (food poisoning, infection, plague) award nothing —
        // toughness is about taking hits, not being sick.
		if (!flag || hediff.def == HediffDefOf.FoodPoisoning || hediff.def == HediffDefOf.WoundInfection || hediff.def == HediffDefOf.Plague)
		{
			return;
		}
		float num = 600f;   // Flat XP bonus for surviving being downed.
		float num2 = 1f;
		int num3 = 0;
		float num4 = 0f;
		if (pawn.health?.hediffSet?.hediffs != null)
		{
			foreach (Hediff_Injury item2 in pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>())
			{
				if (!item2.IsTended() && !(item2.Severity <= 0.01f))
				{
					num3++;
					num4 += item2.Severity;
				}
			}
		}
		if (num3 > 0)
		{
			num2 += 0.05f * num3;
		}
		if (num4 > 5f)
		{
			num2 *= 1.2f;
		}
		if (num4 > 10f)
		{
			num2 *= 1.5f;
		}
		float num5 = num * num2;
		skill.Learn(num5);
		if (Prefs.DevMode && Settings.debugMode)
		{
			Log.Message($"[Toughness] {pawn.Name} downed in combat! Awarding {num5} XP.");
		}
	}

    /// <summary>
    /// Derives both cached factors from the piecewise damage curve: below
    /// <c>breakpoint</c> the pawn takes MORE damage (early slope), above it the factor
    /// eases toward <c>1 - maxReduction</c> at level 20.
    /// </summary>
	private void CalculateCachedFactors(Pawn pawn, SkillRecord skillRecord)
	{
        lastLevel = skillRecord.levelInt;
        if (skillRecord.TotallyDisabled)
		{
			cachedBleedFactor = 1f;
			cachedDamageFactor = 1f;
            return;
        }

        if (lastLevel <= Settings.breakpoint)
        {
            cachedDamageFactor = 1f + Settings.slopeEarly * (Settings.breakpoint - lastLevel);
        }
        else
        {
            float num = Settings.maxReduction / (20f - Settings.breakpoint);
            cachedDamageFactor = 1f - num * (lastLevel - Settings.breakpoint);
        }
        // Bleed curve uses the same shape but its early branch is keyed to level 5,
        // not the configurable breakpoint — low-level pawns bleed slightly faster.
        if (lastLevel <= 5)
        {
            cachedBleedFactor = 1f + Settings.slopeEarly * (Settings.breakpoint - lastLevel);
            return;
        }
        float num2 = Settings.maxReduction / (20f - Settings.breakpoint);
        cachedBleedFactor = 1f - num2 * (lastLevel - Settings.breakpoint);
    }

    /// <summary>
    /// One-time per-pawn setup: marks the comp initialised and pins disabled-state caches.
    /// Called from <see cref="Patch_AdjustStartingToughness"/> (generation) and
    /// <see cref="ToughnessGameComponent"/> (existing save load).
    /// </summary>
    /// <param name="pawn">The owning pawn (logging only).</param>
    /// <param name="skill">The pawn's Toughness record, for the initial disabled check.</param>
	public void InitialiseComp(Pawn pawn, SkillRecord skill)
	{
        if (!compintitialised)
        {
            compintitialised = true;

            if (skill.TotallyDisabled)
            {
                cachedBleedFactor = 1f;
                cachedDamageFactor = 1f;
                skillDisabled = true;
            }

			if (Prefs.DevMode && Settings.debugMode)
			{
				Log.Message($"[Toughness] Initialised toughness cache for {pawn.NameShortColored}. Skill disabled: {skillDisabled}. Cached damage factor: {cachedDamageFactor:0.##}. Cached bleed factor: {cachedBleedFactor:0.##}, Level: {skill.levelInt}");
            }
        }
    }

    /// <summary>
    /// Folds trait-derived level deltas into the pawn's Toughness level.
    /// </summary>
    /// <remarks>
    /// Recomputes the desired delta from the pawn's live trait set on every call and
    /// applies only the difference vs <see cref="traitLevelsApplied"/>, making it
    /// idempotent on stat-read paths and self-correcting when traits change post-spawn.
    /// Only the amount that actually moved the level is recorded (the result is clamped
    /// to 0–20), so removing a trait whose delta was partially clamped restores exactly
    /// the pre-clamp level.
    /// </remarks>
    /// <param name="pawn">Pawn whose traits are inspected.</param>
    /// <param name="baseToughness">Current level to adjust.</param>
    /// <returns>Level including the outstanding trait delta, clamped to 0–20.</returns>
    public int AdjustToughnessByTrait(Pawn pawn, int baseToughness)
    {
        if (pawn.story?.traits == null)
        {
            return Math.Max(0, baseToughness);
        }

        int desiredDelta = 0;
        foreach (Trait trait in pawn.story.traits.allTraits)
        {
            desiredDelta += TraitLevelDelta(trait);
        }

        // Pre-multi-trait saves scribed only a bool for the old +1 Tough bonus —
        // count it as already applied so those pawns gain just the remaining +1.
        int appliedDelta = traitLevelsApplied + (legacyToughTraitApplied ? 1 : 0);
        int target = Math.Clamp(baseToughness + desiredDelta - appliedDelta, 0, 20);
        traitLevelsApplied += target - baseToughness;
        return target;
    }

    /// <summary>
    /// Toughness level delta for a single trait, matched by defName so Biotech/Anomaly
    /// traits resolve without hard DLC references — absent DLCs simply never produce them.
    /// </summary>
    /// <param name="trait">The trait instance; <see cref="Trait.Degree"/> matters for spectrum traits.</param>
    /// <returns>Signed level delta; 0 for traits with no bearing on bodily durability.</returns>
    private static int TraitLevelDelta(Trait trait)
    {
        switch (trait.def.defName)
        {
            case "Tough": return 2;        // Thick skin, dense flesh — vanilla's own durability axis.
            case "Delicate": return -2;    // Biotech: fragile skin and bones; Tough's exclusion-tag opposite.
            case "Wimp": return -2;        // Cowardly; a little pain immobilizes them.
            case "Masochist": return 2;    // Enjoys being hurt — built tolerance. Vanilla opposite of Wimp.
            case "Brawler": return 2;      // Fights up close, eats hits constantly.
            case "Bloodlust": return 1;    // Violent life, inured to blood and injury.
            case "Ascetic": return 1;      // Forsakes physical comfort — hardship-hardened.
            case "BodyMastery": return 3;  // Anomaly dev trait (commonality 0): body runs on unknown mechanism.
            // Immunity is a spectrum: super-immune (degree 1) vs sickly (degree -1).
            case "Immunity": return trait.Degree > 0 ? 1 : -2;
            default: return 0;
        }
    }

    /// <summary>Persists the cache fields and trait-application state through save/load.</summary>
    public override void PostExposeData()
    {
		base.PostExposeData();

        Scribe_Values.Look(ref lastLevel, "lastLevel", -1);
        Scribe_Values.Look(ref cachedDamageFactor, "cachedDamageFactor", 1f);
        Scribe_Values.Look(ref cachedBleedFactor, "cachedBleedFactor", 1f);

        Scribe_Values.Look(ref skillDisabled, "skillDisabled", false);
        Scribe_Values.Look(ref compintitialised, "compintitialised", false);
		Scribe_Values.Look(ref legacyToughTraitApplied, "toughTraitAdjusted", false);
		Scribe_Values.Look(ref traitLevelsApplied, "traitLevelsApplied", 0);
    }

}
