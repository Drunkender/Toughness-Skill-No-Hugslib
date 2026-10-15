using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// Harmony postfix on <see cref="PawnGenerator"/>'s private <c>GenerateSkills</c> that
/// rolls a starting Toughness level (1–6, adjusted by backstory keywords) for every
/// generated humanlike pawn.
/// </summary>
/// <remarks>
/// The 1.7 signature is <c>GenerateSkills(Pawn pawn, PawnGenerationRequest request)</c>;
/// the postfix binds <paramref name="pawn"/> by name and ignores the extra parameter.
/// </remarks>
[HarmonyPatch(typeof(PawnGenerator))]
[HarmonyPatch("GenerateSkills")]
public static class Patch_AdjustStartingToughness
{
    private static ThoughnessSkillSettings Settings =>
    LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

    /// <summary>Backstory keywords that add +2 Toughness (martial/outdoor upbringing).</summary>
    private static readonly string[] PositiveToughnessKeywords = new string[15]
	{
		"soldier", "mercenary", "gladiator", "brawler", "miner", "scout", "gang", "marine", "tribal", "warrior",
		"hunter", "bodyguard", "wastelander", "leader", "war"
	};

    /// <summary>Backstory keywords that subtract 2 Toughness (sheltered/intellectual upbringing).</summary>
	private static readonly string[] NegativeToughnessKeywords = new string[12]
	{
		"noble", "artist", "scholar", "scientist", "clerk", "engineer", "priest", "aristocrat", "diplomat", "actor",
		"academic", "researcher"
	};

    /// <summary>Rolls and assigns the starting Toughness level after vanilla skill generation.</summary>
    /// <param name="pawn">The freshly generated pawn (before it enters the world).</param>
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
            comp?.InitialiseComp(pawn, skill);

            comp?.UpdateCache(pawn, true);

            if (Prefs.DevMode && Settings.debugMode)
            {
                Log.Message($"[Toughness] {pawn.Name} generated with Toughness {skill.levelInt}.");
            }
        }
	}

    /// <summary>
    /// Applies the ±2 backstory adjustment by matching <see cref="PositiveToughnessKeywords"/>/
    /// <see cref="NegativeToughnessKeywords"/> against childhood and adulthood def names and titles.
    /// </summary>
    /// <param name="pawn">Pawn whose backstories are inspected.</param>
    /// <param name="baseToughness">The 1–6 roll to adjust.</param>
    /// <returns>Adjusted level, clamped to a minimum of 0.</returns>
	public static int AdjustToughnessByBackstory(Pawn pawn, int baseToughness)
	{
		if (pawn.story != null)
		{
			string childhood = pawn.story.Childhood?.defName ?? "";
			string adulthood = pawn.story.Adulthood?.defName ?? "";
			string childhoodTitle = pawn.story.Childhood?.title ?? "";
			string adulthoodTitle = pawn.story.Adulthood?.title ?? "";
            // OrdinalIgnoreCase keeps this allocation-free and culture-stable; the previous
            // ToLower()+Contains(keyword) approach triggered CA1307/CA1308 under latest-all.
			if (PositiveToughnessKeywords.Any((string keyword) => ContainsKeyword(childhood, keyword) || ContainsKeyword(adulthood, keyword) || ContainsKeyword(childhoodTitle, keyword) || ContainsKeyword(adulthoodTitle, keyword)))
			{
				baseToughness += 2;
			}
			if (NegativeToughnessKeywords.Any((string keyword) => ContainsKeyword(childhood, keyword) || ContainsKeyword(adulthood, keyword) || ContainsKeyword(childhoodTitle, keyword) || ContainsKeyword(adulthoodTitle, keyword)))
			{
				baseToughness -= 2;
			}
		}
		return Math.Max(0, baseToughness);
	}

    /// <summary>Case-insensitive substring test used for backstory keyword matching.</summary>
    private static bool ContainsKeyword(string text, string keyword) =>
        text.Contains(keyword, StringComparison.OrdinalIgnoreCase);
}
