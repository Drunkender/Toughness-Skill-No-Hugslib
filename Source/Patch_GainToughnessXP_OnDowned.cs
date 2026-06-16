using HarmonyLib;
using System.Runtime;
using Verse;

namespace ToughnessSkillMod;

[HarmonyPatch(typeof(Pawn_HealthTracker))]
[HarmonyPatch("MakeDowned")]
public static class Patch_GainToughnessXP_OnDowned
{
    private static ThoughnessSkillSettings Settings =>
LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

    [HarmonyPostfix]
	public static void Postfix(Pawn_HealthTracker __instance, DamageInfo? dinfo, Hediff hediff)
	{
        Pawn value = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

        if (!Settings.patchColonists && ToughnessSkillMod.IsColonist(value)) {
            return;
        }
        if (!Settings.patchFriendlyPawns && ToughnessSkillMod.IsFriendlyPawn(value))
        {
            return;
        }
        if (!Settings.patchHostilePawns && ToughnessSkillMod.IsHostilePawn(value))
        {
            return;
        }
        if (!Settings.patchFriendlyPawns && ToughnessSkillMod.IsIndependentPawn(value))
        {
            return;
        }

        
		value?.TryGetComp<CompToughnessCache>()?.NotifyDowned(dinfo, hediff, value);
	}
}
