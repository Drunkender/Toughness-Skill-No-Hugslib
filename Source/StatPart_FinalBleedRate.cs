using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// <see cref="StatPart"/> feeding the mod's <c>FinalBleedRate</c> stat: multiplies the
/// value by every active hediff <c>totalBleedFactor</c> (coagulators etc.) and then by
/// the pawn's cached Toughness bleed factor.
/// </summary>
public class StatPart_FinalBleedRate : StatPart
{
    /// <summary>Applies hediff + Toughness bleed multipliers to the running stat value.</summary>
    /// <param name="req">Stat request; ignored for non-pawn things.</param>
    /// <param name="val">Stat value to transform.</param>
	public override void TransformValue(StatRequest req, ref float val)
	{
		if (!req.HasThing || req.Thing is not Pawn pawn)
		{
			return;
		}
		float num = 1f;
		List<Hediff>? list = pawn.health?.hediffSet?.hediffs;
		if (list != null)
		{
			foreach (Hediff item in list)
			{
				HediffStage? hediffStage = item.CurStage;
				if (hediffStage != null && hediffStage.totalBleedFactor != 1f)
				{
					num *= hediffStage.totalBleedFactor;
				}
			}
		}
		CompToughnessCache compToughnessCache = pawn.TryGetComp<CompToughnessCache>();
		if (compToughnessCache != null)
		{
			compToughnessCache.UpdateCache(pawn, false);
            num *= compToughnessCache.cachedBleedFactor;
		}
		val *= num;
	}

    /// <summary>Per-factor breakdown shown in the stat's tooltip; null when nothing applies.</summary>
    /// <param name="req">Stat request; ignored for non-pawn things.</param>
    /// <returns>Explanation text, or null when the combined factor is 1.</returns>
	public override string? ExplanationPart(StatRequest req)
	{
		if (!req.HasThing || req.Thing is not Pawn pawn)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		float num = 1f;
		List<Hediff>? list = pawn.health?.hediffSet?.hediffs;
		if (list != null)
		{
			foreach (Hediff item in list)
			{
				HediffStage? hediffStage = item.CurStage;
				if (hediffStage != null && hediffStage.totalBleedFactor != 1f)
				{
					num *= hediffStage.totalBleedFactor;
					stringBuilder.AppendLine((item.def?.label ?? item.Label) + ": x" + hediffStage.totalBleedFactor.ToStringPercent());
				}
			}
		}
		CompToughnessCache compToughnessCache = pawn.TryGetComp<CompToughnessCache>();
		if (compToughnessCache != null)
		{
			float cachedBleedFactor = compToughnessCache.cachedBleedFactor;
			if (cachedBleedFactor != 1f)
			{
				stringBuilder.AppendLine("Toughness skill: x" + cachedBleedFactor.ToStringPercent());
				num *= cachedBleedFactor;
			}
		}
		if (num != 1f)
		{
			stringBuilder.AppendLine("Final: x" + num.ToStringPercent());
		}
		if (num == 1f)
		{
			return null;
		}
		return stringBuilder.ToString().Trim();
	}
}
