using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ToughnessSkillMod;

public class StatPart_FinalBleedRate : StatPart
{
	public override void TransformValue(StatRequest req, ref float val)
	{
		if (!req.HasThing || !(req.Thing is Pawn pawn))
		{
			return;
		}
		float num = 1f;
		List<Hediff> list = pawn.health?.hediffSet?.hediffs;
		if (list != null)
		{
			foreach (Hediff item in list)
			{
				HediffStage hediffStage = item?.CurStage;
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

	public override string ExplanationPart(StatRequest req)
	{
		if (!req.HasThing || !(req.Thing is Pawn pawn))
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		float num = 1f;
		List<Hediff> list = pawn.health?.hediffSet?.hediffs;
		if (list != null)
		{
			foreach (Hediff item in list)
			{
				HediffStage hediffStage = item?.CurStage;
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
