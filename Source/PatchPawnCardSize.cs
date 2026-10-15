using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace ToughnessSkillMod;

/// <summary>
/// Harmony postfix on <see cref="CharacterCardUtility.PawnCardSize"/> that grows the
/// character card by one row so the added Toughness skill fits in the bio tab.
/// </summary>
/// <remarks>
/// Applied by <c>Harmony.PatchAll()</c> in the mod constructor — this class is a pure
/// attribute patch, no manual wiring. In 1.7 <c>PawnCardSize</c> returns
/// <c>BasePawnCardSize</c> (480×455) plus 20px per extra quest-inspect line, so the
/// <c>&gt;= 392f</c> threshold is effectively always true; it is kept as a guard
/// against any future card variant that renders shorter.
/// </remarks>
[HarmonyPatch(typeof(CharacterCardUtility), nameof(CharacterCardUtility.PawnCardSize))]
public static class PatchPawnCardSize
{
	/// <summary>Adds 24px (one skill row) to the card height for standard-size cards.</summary>
	/// <param name="__result">The card size vanilla computed; widened in place.</param>
	public static void Postfix(ref Vector2 __result)
	{
		if (__result.y >= 392f)
		{
			__result.y += 24f;
		}
	}
}
