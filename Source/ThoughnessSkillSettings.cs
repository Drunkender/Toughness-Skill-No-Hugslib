using Verse;

namespace ToughnessSkillMod;

public class ThoughnessSkillSettings : ModSettings
{
    public float baseXp = 50;
    public bool debugMode = false;
    public int breakpoint = 5;
    public float slopeEarly = 0.075f;
    public float maxReduction = 0.5f;
    public bool patchColonists = true;
    public bool patchFriendlyPawns = true;
    public bool patchHostilePawns = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref baseXp, "baseXp", 50);
        Scribe_Values.Look(ref debugMode, "debugMode", false);
        Scribe_Values.Look(ref breakpoint, "breakpoint", 5);
        Scribe_Values.Look(ref slopeEarly, "slopeEarly", 0.075f);
        Scribe_Values.Look(ref maxReduction, "maxReduction", 0.5f);
        Scribe_Values.Look(ref patchColonists, "patchColonists", true);
        Scribe_Values.Look(ref patchFriendlyPawns, "patchFriendlyPawns", true);
        Scribe_Values.Look(ref patchHostilePawns, "patchHostilePawns", true);
    }
}

