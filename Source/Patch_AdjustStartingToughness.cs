using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ToughnessSkillMod;

[HarmonyPatch(typeof(PawnGenerator))]
[HarmonyPatch("GenerateSkills")]
public static class Patch_AdjustStartingToughness
{
    private static ThoughnessSkillSettings Settings =>
    LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

    private static readonly string[] PositiveToughnessKeywords = new string[15]
	{
		"soldier", "mercenary", "gladiator", "brawler", "miner", "scout", "gang", "marine", "tribal", "warrior",
		"hunter", "bodyguard", "wastelander", "leader", "war"
	};

	private static readonly string[] NegativeToughnessKeywords = new string[12]
	{
		"noble", "artist", "scholar", "scientist", "clerk", "engineer", "priest", "aristocrat", "diplomat", "actor",
		"academic", "researcher"
	};

	[HarmonyPostfix]
	public static void Postfix(Pawn pawn)
	{
		if (pawn == null || pawn.skills == null || !pawn.RaceProps.Humanlike)
		{
			return;
		}

        SkillRecord skill = pawn.skills.GetSkill(DefDatabase<SkillDef>.GetNamed("Toughness", errorOnFail: false));
		if (skill != null)
		{
			int baseToughness = Rand.RangeInclusive(1, 6);
            baseToughness = AdjustToughnessByBackstory(pawn, baseToughness);
			skill.levelInt = baseToughness;
			skill.xpSinceLastLevel = 0f;

            CompToughnessCache comp = pawn.TryGetComp<CompToughnessCache>();
            comp?.InitialiseComp(pawn, comp, skill);

            comp?.UpdateCache(pawn, true);

            if (Prefs.DevMode && (bool)Settings.debugMode)
            {
                Log.Message($"[Toughness] {pawn.Name} generated with Toughness {skill.levelInt}.");
            }
        }
	}



	public static int AdjustToughnessByBackstory(Pawn pawn, int baseToughness)
	{
		if (pawn.story != null)
		{
			string childhood = pawn.story.Childhood?.defName.ToLower() ?? "";
			string adulthood = pawn.story.Adulthood?.defName.ToLower() ?? "";
			string childhoodTitle = pawn.story.Childhood?.title.ToLower() ?? "";
			string adulthoodTitle = pawn.story.Adulthood?.title.ToLower() ?? "";
			if (PositiveToughnessKeywords.Any((string keyword) => childhood.Contains(keyword) || adulthood.Contains(keyword) || childhoodTitle.Contains(keyword) || adulthoodTitle.Contains(keyword)))
			{
				baseToughness += 2;
			}
			if (NegativeToughnessKeywords.Any((string keyword) => childhood.Contains(keyword) || adulthood.Contains(keyword) || childhoodTitle.Contains(keyword) || adulthoodTitle.Contains(keyword)))
			{
				baseToughness -= 2;
			}
		}
		return Math.Max(0, baseToughness);
	}
}
