using RimWorld;
using Verse;

namespace ToughnessSkillMod;

public class StatPart_ToughnessDamage : StatPart
{
	public override void TransformValue(StatRequest req, ref float val)
	{
		if (req.HasThing && req.Thing is Pawn pawn)
		{
			CompToughnessCache compToughnessCache = pawn.TryGetComp<CompToughnessCache>();
			if (compToughnessCache != null)
			{
                compToughnessCache.UpdateCache(pawn, false);
                val *= compToughnessCache.cachedDamageFactor;
			}
		}
	}

	public override string ExplanationPart(StatRequest req)
	{
		if (!req.HasThing)
		{
			return null;
		}
		Pawn pawn = req.Thing as Pawn;
		if (pawn?.skills == null)
		{
			return null;
		}
		if (pawn.skills.GetSkill(DefDatabase<SkillDef>.GetNamed("Toughness", errorOnFail: false)) == null)
		{
			return null;
		}
		CompToughnessCache compToughnessCache = pawn.TryGetComp<CompToughnessCache>();
		compToughnessCache?.UpdateCache(pawn, false);
		float f = compToughnessCache?.cachedDamageFactor ?? 1f;
		return "Toughness skill: x" + f.ToStringPercent() + " damage taken";
	}
}
