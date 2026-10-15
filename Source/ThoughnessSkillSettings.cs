using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// Persisted mod settings: XP gain tuning, the piecewise damage-reduction curve, and the
/// per-faction-class toggles that disable Toughness on colonists/friendly/hostile pawns.
/// </summary>
/// <remarks>
/// Class name keeps the historical "Thoughness" typo — settings are serialized under the
/// mod's settings file and the name is referenced across the codebase; renaming buys
/// nothing and risks churn.
/// </remarks>
public class ThoughnessSkillSettings : ModSettings
{
    /// <summary>XP granted per point of post-mitigation damage taken.</summary>
    public float baseXp = 50;

    /// <summary>Extra console logging when Dev Mode is on.</summary>
    public bool debugMode;

    /// <summary>Levels at or below this use the early (damage-increasing) slope.</summary>
    public int breakpoint = 5;

    /// <summary>Damage-factor change per level below <see cref="breakpoint"/>.</summary>
    public float slopeEarly = 0.075f;

    /// <summary>Fraction of damage blocked at level 20 (0.50 = 50%).</summary>
    public float maxReduction = 0.5f;

    /// <summary>Whether colonists may benefit from Toughness.</summary>
    public bool patchColonists = true;

    /// <summary>Whether non-hostile, non-player pawns (and factionless) may benefit.</summary>
    public bool patchFriendlyPawns = true;

    /// <summary>Whether pawns hostile to the player may benefit.</summary>
    public bool patchHostilePawns = true;

    /// <summary>Scribes every setting under stable keys shared with the 1.6 build.</summary>
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
