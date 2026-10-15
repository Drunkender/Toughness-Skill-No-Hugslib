using HarmonyLib;
using RimWorld;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// Mod entry point: applies all Harmony patches, owns the settings lifecycle, and exposes
/// the faction-classification helpers (<see cref="IsColonist"/> etc.) used by every patch
/// to gate Toughness per pawn category.
/// </summary>
public class ToughnessSkillMod : Mod
{
    /// <summary>
    /// Loaded settings instance. Initialised to a default instance (not null) so early
    /// callers during boot never observe a null — <see cref="Mod.GetSettings{T}"/>
    /// replaces it with the user's saved values immediately after. Internal rather than
    /// public: the mutable-static pattern is this assembly's convention only (CA2211).
    /// </summary>
    internal static ThoughnessSkillSettings settings = new ThoughnessSkillSettings();
    private static ThoughnessSkillSettings defaultSettings = new ThoughnessSkillSettings();

    private string baseXpBuffer = "";
    private string breakpointBuffer = "";
    private string slopeEarlyBuffer = "";
    private string maxReductionBuffer = "";

    /// <summary>Mod constructor: applies Harmony patches and loads settings.</summary>
    /// <param name="content">This mod's content pack.</param>
    public ToughnessSkillMod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("tyinplop.ToughnessSkillMod");
        harmony.PatchAll();
        settings = GetSettings<ThoughnessSkillSettings>();

        // Seed the text-field buffers from loaded settings on first open — they're
        // instance state (the settings window is a fresh Mod each session otherwise).
        if (string.IsNullOrEmpty(baseXpBuffer))
        {
            baseXpBuffer = settings.baseXp.ToString(CultureInfo.InvariantCulture);
        }
        if (string.IsNullOrEmpty(breakpointBuffer))
        {
            breakpointBuffer = settings.breakpoint.ToString(CultureInfo.InvariantCulture);
        }
        if (string.IsNullOrEmpty(slopeEarlyBuffer))
        {
            slopeEarlyBuffer = settings.slopeEarly.ToString(CultureInfo.InvariantCulture);
        }
        if (string.IsNullOrEmpty(maxReductionBuffer))
        {
            maxReductionBuffer = settings.maxReduction.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Display name in the mod settings list.</summary>
    public override string SettingsCategory()
    {
        return "Thoughness Skill";
    }

    /// <summary>Draws the settings window: XP tuning, faction toggles, reset button.</summary>
    /// <param name="inRect">Content area provided by the settings dialog.</param>
    public override void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);

        float labelPct = 0.60f;
        float spacing = 0.75f;

        // --- Base XP: single-line label + numeric text field with tooltip ---
        Rect row = listingStandard.GetRect(Text.LineHeight);
        Rect labelRect = new Rect(row.x, row.y, row.width * labelPct, row.height);
        Rect fieldRect = new Rect(row.x + labelRect.width + spacing, row.y, row.width - labelRect.width - spacing, row.height);

        Widgets.Label(labelRect, "Base XP per damage");
        TooltipHandler.TipRegion(labelRect, "Tweaks XP gain for the Toughness skill.");
        Widgets.TextFieldNumeric(fieldRect, ref settings.baseXp, ref baseXpBuffer);

        listingStandard.Gap();

        // Debug mode
        listingStandard.CheckboxLabeled("DebugMode", ref settings.debugMode, "Extra logging in the console when Dev Mode is on.");
        listingStandard.Gap();

        // --- Breakpoint level: single-line label + manual ± control with editable number ---
        Rect row2 = listingStandard.GetRect(Text.LineHeight);
        Rect labelRect2 = new Rect(row2.x, row2.y, row2.width * labelPct, row2.height);
        Rect fieldRect2 = new Rect(row2.x + labelRect2.width + spacing, row2.y, row2.width - labelRect2.width - spacing, row2.height);

        Widgets.Label(labelRect2, "Breakpoint level");
        TooltipHandler.TipRegion(labelRect2, "Levels ≤ breakpoint use the first slope; above it, the second part.");

        // Manual compact IntAdjuster: minus button, editable value, plus button
        float btnWidth = 28f;
        float innerSpacing = 4f;
        Rect minusRect = new Rect(fieldRect2.x, fieldRect2.y, btnWidth, fieldRect2.height);
        Rect plusRect = new Rect(fieldRect2.xMax - btnWidth, fieldRect2.y, btnWidth, fieldRect2.height);
        Rect valueRect = new Rect(minusRect.xMax + innerSpacing, fieldRect2.y, fieldRect2.width - (btnWidth * 2f) - (innerSpacing * 2f), fieldRect2.height);

        if (Widgets.ButtonText(minusRect, "−"))
        {
            settings.breakpoint = Mathf.Max(1, settings.breakpoint - 1);
            breakpointBuffer = settings.breakpoint.ToString(CultureInfo.InvariantCulture);
        }

        if (Widgets.ButtonText(plusRect, "+"))
        {
            settings.breakpoint = settings.breakpoint + 1;
            breakpointBuffer = settings.breakpoint.ToString(CultureInfo.InvariantCulture);
        }
        Widgets.TextFieldNumeric(valueRect, ref settings.breakpoint, ref breakpointBuffer, 1f, 999f);

        listingStandard.Gap();

        // --- Early-level slope: single-line label + numeric text field with tooltip ---
        Rect row3 = listingStandard.GetRect(Text.LineHeight);
        Rect labelRect3 = new Rect(row3.x, row3.y, row3.width * labelPct, row3.height);
        Rect fieldRect3 = new Rect(row3.x + labelRect3.width + spacing, row3.y, row3.width - labelRect3.width - spacing, row3.height);

        Widgets.Label(labelRect3, "Early-level slope (+% per level)");
        TooltipHandler.TipRegion(labelRect3, "Damage multiplier change per level below the breakpoint.");
        Widgets.TextFieldNumeric(fieldRect3, ref settings.slopeEarly, ref slopeEarlyBuffer);

        listingStandard.Gap();

        // --- Max reduction at level: single-line label + numeric text field with tooltip ---
        Rect row4 = listingStandard.GetRect(Text.LineHeight);
        Rect labelRect4 = new Rect(row4.x, row4.y, row4.width * labelPct, row4.height);
        Rect fieldRect4 = new Rect(row4.x + labelRect4.width + spacing, row4.y, row4.width - labelRect4.width - spacing, row4.height);

        Widgets.Label(labelRect4, "Max reduction at level 20");
        TooltipHandler.TipRegion(labelRect4, "Fraction of damage blocked at level 20 (0.50 = 50%).");
        Widgets.TextFieldNumeric(fieldRect4, ref settings.maxReduction, ref maxReductionBuffer);

        listingStandard.Gap();

        listingStandard.CheckboxLabeled("Allow Toughness for Colonists", ref settings.patchColonists, "Allows the skill on Colonists");

        listingStandard.Gap();

        listingStandard.CheckboxLabeled("Allow Toughness for friendly Pawns", ref settings.patchFriendlyPawns, "Allows the skill on friendly Pawns");

        listingStandard.Gap();

        listingStandard.CheckboxLabeled("Allow Toughness for hostile Pawns", ref settings.patchHostilePawns, "Allows the skill on hostile Pawns");

        listingStandard.Gap();

        // Centered Reset to defaults button
        Rect buttonRow = listingStandard.GetRect(Text.LineHeight);
        string resetLabel = "Reset to defaults";
        Vector2 labelSize = Text.CalcSize(resetLabel);
        float padding = 20f;
        float buttonWidth = Mathf.Max(150f, labelSize.x + padding);
        Rect buttonRect = new Rect(buttonRow.x + (buttonRow.width - buttonWidth) / 2f, buttonRow.y, buttonWidth, buttonRow.height);
        if (Widgets.ButtonText(buttonRect, resetLabel))
        {
            settings.baseXp = defaultSettings.baseXp;
            settings.debugMode = defaultSettings.debugMode;
            settings.breakpoint = defaultSettings.breakpoint;
            settings.slopeEarly = defaultSettings.slopeEarly;
            settings.maxReduction = defaultSettings.maxReduction;
            settings.patchColonists = defaultSettings.patchColonists;
            settings.patchFriendlyPawns = defaultSettings.patchFriendlyPawns;
            settings.patchHostilePawns = defaultSettings.patchHostilePawns;

            baseXpBuffer = settings.baseXp.ToString(CultureInfo.InvariantCulture);
            breakpointBuffer = settings.breakpoint.ToString(CultureInfo.InvariantCulture);
            slopeEarlyBuffer = settings.slopeEarly.ToString(CultureInfo.InvariantCulture);
            maxReductionBuffer = settings.maxReduction.ToString(CultureInfo.InvariantCulture);

            WriteSettings();
        }

        listingStandard.End();
        base.DoSettingsWindowContents(inRect);
    }

    /// <summary>
    /// Parses the text buffers into settings on close, then triggers a cache refresh so
    /// curve/toggle edits apply without a reload.
    /// </summary>
    public override void WriteSettings()
    {
        settings.baseXp = ParseOrDefault(baseXpBuffer, defaultSettings.baseXp, settings.baseXp, out baseXpBuffer);
        settings.breakpoint = (int)ParseOrDefault(breakpointBuffer, defaultSettings.breakpoint, settings.breakpoint, out breakpointBuffer);
        settings.slopeEarly = ParseOrDefault(slopeEarlyBuffer, defaultSettings.slopeEarly, settings.slopeEarly, out slopeEarlyBuffer);
        settings.maxReduction = ParseOrDefault(maxReductionBuffer, defaultSettings.maxReduction, settings.maxReduction, out maxReductionBuffer);

        base.WriteSettings();
        // Deferred until the settings save finishes mutating state — refreshing caches
        // mid-write can read half-updated values.
        LongEventHandler.ExecuteWhenFinished(RefreshCaches);
    }

    /// <summary>
    /// Parses a settings buffer to float, falling back to <paramref name="defaultValue"/>
    /// on empty input and <paramref name="currentValue"/> on unparseable input.
    /// </summary>
    /// <param name="buffer">Raw text the user typed.</param>
    /// <param name="defaultValue">Value used when the buffer is blank.</param>
    /// <param name="currentValue">Last known-good value, used when parsing fails.</param>
    /// <param name="newBuffer">Normalised buffer text to write back to the field.</param>
    /// <returns>The parsed or fallback value.</returns>
    private static float ParseOrDefault(string buffer, float defaultValue, float currentValue, out string newBuffer)
    {
        bool printDebugMessage = Prefs.DevMode && settings.debugMode;
        if (string.IsNullOrWhiteSpace(buffer))
        {
            newBuffer = defaultValue.ToString(CultureInfo.InvariantCulture);
            if (printDebugMessage)
            {
                Log.Message($"[ToughnessSkillMod] Empty input, using default {defaultValue}");
            }
            return defaultValue;
        }
        if (float.TryParse(buffer, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
        {
            newBuffer = parsed.ToString(CultureInfo.InvariantCulture);
            if (printDebugMessage)
            {
                Log.Message($"[ToughnessSkillMod] Parsed '{buffer}' as {parsed}");
            }
            return parsed;
        }
        newBuffer = currentValue.ToString(CultureInfo.InvariantCulture);
        if (printDebugMessage)
        {
            Log.Message($"[ToughnessSkillMod] Invalid input '{buffer}', reverting to last valid value {currentValue}");
        }
        return currentValue;
    }

    /// <summary>
    /// Forces every living humanlike pawn's <see cref="CompToughnessCache"/> to re-evaluate
    /// level and disable state. Called after settings writes and once at game start.
    /// </summary>
    public static void RefreshCaches()
    {
        foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive.Where(p => p != null && p.RaceProps?.Humanlike == true))
        {
            pawn.TryGetComp<CompToughnessCache>()?.UpdateCache(pawn, true);
        }
    }

    /// <summary>True for player-faction pawns.</summary>
    public static bool IsColonist(Pawn pawn)
    {
        return pawn.Faction != null && pawn.Faction.IsPlayer;
    }

    /// <summary>True for non-player pawns whose faction isn't hostile to the player.</summary>
    public static bool IsFriendlyPawn(Pawn pawn)
    {
        return pawn.Faction != null && !pawn.Faction.HostileTo(Faction.OfPlayer) && !pawn.Faction.IsPlayer;
    }

    /// <summary>True for non-player pawns whose faction is hostile to the player.</summary>
    public static bool IsHostilePawn(Pawn pawn)
    {
        return pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer) && !pawn.Faction.IsPlayer;
    }

    /// <summary>True for factionless pawns; gated by the friendly-pawns toggle (wild spawns share that class).</summary>
    public static bool IsIndependentPawn(Pawn pawn)
    {
        return pawn.Faction == null;
    }
}
