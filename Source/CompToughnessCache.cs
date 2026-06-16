using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace ToughnessSkillMod;

public class CompToughnessCache : ThingComp
{
	private int lastLevel = -1;

	public float cachedDamageFactor = 1f;

	public float cachedBleedFactor = 1f;

    public bool skillDisabled = false;
    public bool compintitialised = false;
	public bool toughTraitAdjusted = false;

    private static ThoughnessSkillSettings Settings =>
        LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

	public void UpdateCache(Pawn pawn, bool skillDisablesChanged)
	{
		SkillRecord skillRecord = pawn.skills?.GetSkill(DefDatabase<SkillDef>.GetNamed("Toughness", errorOnFail: false));
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

        if (skillRecord != null && skillRecord.levelInt != lastLevel)
		{
            CalculateCachedFactors(pawn, skillRecord);
		} 
	}

	public void NotifyDamageApplied(DamageInfo dinfo, float totalDamageDealt, Pawn pawn)
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
		float num = totalDamageDealt * (float)Settings.baseXp;
		float num2 = 1f;
		int num3 = 0;
		float num4 = 0f;
		foreach (Hediff_Injury item in pawn.health?.hediffSet?.hediffs.OfType<Hediff_Injury>() ?? Enumerable.Empty<Hediff_Injury>())
		{
			if (!item.IsTended() && !(item.Severity <= 0.01f))
			{
				num3++;
				num4 += item.Severity;
			}
		}
		if (num3 > 0)
		{
			num2 += 0.05f * (float)num3;
		}
		if (num4 > 5f)
		{
			num2 *= 1.2f;
		}
		if (num4 > 10f)
		{
			num2 *= 1.5f;
		}
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
		if (Prefs.DevMode && (bool)Settings.debugMode)
		{
			Log.Message($"[Toughness] {pawn.NameShortColored} took {amount:0.##} raw damage, reduced to {totalDamageDealt:0.##}. XP granted: {num5:0.##} (x{num2:0.##}). Base XP: {Settings.baseXp:0.##}");
		}
	}

	public void NotifyDowned(DamageInfo? dinfo, Hediff hediff, Pawn pawn)
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
		if (!flag || hediff.def == HediffDefOf.FoodPoisoning || hediff.def == HediffDefOf.WoundInfection || hediff.def == HediffDefOf.Plague)
		{
			return;
		}
		float num = 600f;
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
			num2 += 0.05f * (float)num3;
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
		if (Prefs.DevMode && (bool)Settings.debugMode)
		{
			Log.Message($"[Toughness] {pawn.Name} downed in combat! Awarding {num5} XP.");
		}
	}

	private void CalculateCachedFactors(Pawn pawn, SkillRecord skillRecord)
	{
        lastLevel = skillRecord.levelInt;
        if (skillRecord.TotallyDisabled)
		{
			cachedBleedFactor = 1f;
			cachedDamageFactor = 1f;
            return;
        }

        if (lastLevel <= (int)Settings.breakpoint)
        {
            cachedDamageFactor = 1f + (float)Settings.slopeEarly * (float)((int)Settings.breakpoint - lastLevel);
        }
        else
        {
            float num = (float)Settings.maxReduction / (20f - (float)(int)Settings.breakpoint);
            cachedDamageFactor = 1f - num * (float)(lastLevel - (int)Settings.breakpoint);
        }
        if (lastLevel <= 5)
        {
            cachedBleedFactor = 1f + (float)Settings.slopeEarly * (float)((int)Settings.breakpoint - lastLevel);
            return;
        }
        float num2 = (float)Settings.maxReduction / (20f - (float)(int)Settings.breakpoint);
        cachedBleedFactor = 1f - num2 * (float)(lastLevel - (int)Settings.breakpoint);
    }

	public void InitialiseComp(Pawn pawn, CompToughnessCache comp, SkillRecord skill)
	{
        if (!comp.compintitialised)
        {
            comp.compintitialised = true;

            if (skill.TotallyDisabled)
            {
                comp.cachedBleedFactor = 1f;
                comp.cachedDamageFactor = 1f;
                comp.skillDisabled = true;
            }

			if (Prefs.DevMode && (bool)Settings.debugMode)
			{
				Log.Message($"[Toughness] Initialised toughness cache for {pawn.NameShortColored}. Skill disabled: {comp.skillDisabled}. Cached damage factor: {comp.cachedDamageFactor:0.##}. Cached bleed factor: {comp.cachedBleedFactor:0.##}, Level: {skill.levelInt}");
            }
        }
    }

    public int AdjustToughnessByTrait(Pawn pawn, int baseToughness)
    {
        if (pawn.story != null && pawn.story.traits != null)
        {
            TraitDef thoughTrait = TraitDef.Named("Tough");
            if (thoughTrait != null && pawn.story.traits.HasTrait(thoughTrait))
            {
				bool traitFound = false;
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
					if (trait.def == thoughTrait)
					{
						traitFound = true;
                    }
                }
				if (!traitFound)
				{
                    toughTraitAdjusted = false;
                }
				if (traitFound && !toughTraitAdjusted)
				{
                    toughTraitAdjusted = true;
                    baseToughness += 1;
                }
            }
        }
        return Math.Max(0, baseToughness);
    }

    public override void PostExposeData()
    {
		base.PostExposeData();

        Scribe_Values.Look(ref lastLevel, "lastLevel", -1);
        Scribe_Values.Look(ref cachedDamageFactor, "cachedDamageFactor", 1f);
        Scribe_Values.Look(ref cachedBleedFactor, "cachedBleedFactor", 1f);

        Scribe_Values.Look(ref skillDisabled, "skillDisabled", false);
        Scribe_Values.Look(ref compintitialised, "compintitialised", false);
		Scribe_Values.Look(ref toughTraitAdjusted, "toughTraitAdjusted", false);
    }

}
