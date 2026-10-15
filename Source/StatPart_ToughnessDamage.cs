using RimWorld;
using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// <see cref="StatPart"/> injected into vanilla's <c>IncomingDamageFactor</c> stat by
/// <c>Patches/IncomingDamageFactor.xml</c>; multiplies incoming damage by the pawn's
/// cached Toughness damage factor.
/// </summary>
public class StatPart_ToughnessDamage : StatPart
{
    /// <summary>Applies the cached Toughness damage multiplier.</summary>
    /// <param name="req">Stat request; ignored for non-pawn things.</param>
    /// <param name="val">Stat value to transform.</param>
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

    /// <summary>One-line explanation for the stat tooltip.</summary>
    /// <param name="req">Stat request; ignored for non-pawn things or pawns without the skill.</param>
    /// <returns>Explanation text, or null when the pawn lacks a Toughness record.</returns>
	public override string? ExplanationPart(StatRequest req)
	{
		if (req.Thing is not Pawn pawn || pawn.skills == null)
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
