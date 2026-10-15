using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// Unattended smoke-test harness, activated by passing <c>-toughness-test</c> on the
/// RimWorld command line. Exercises the mod's real code paths on a live map (pawn
/// generation → damage → XP → stat reads → settings/card IMGUI) then quits the process
/// with exit code 0 on success or 1 on any assertion failure.
/// </summary>
/// <remarks>
/// Launch recipe (see vault tooling notes): <c>RimWorld.exe -savedatafolder=&lt;tmp&gt;
/// -quicktest -NoWindow -console -NoCrashDialog -toughness-test</c>. <c>-NoWindow</c> is
/// required — the IMGUI loop still runs so <see cref="GameComponentOnGUI"/> fires, unlike
/// <c>-headless</c> which creates no ImGuiHost. With no flag the component returns from
/// every callback immediately, so normal play pays nothing.
/// </remarks>
public class ToughnessSmokeTest : GameComponent
{
    /// <summary>Cached once: whether <c>-toughness-test</c> was passed on the command line.</summary>
    private static readonly bool enabled = GenCommandLine.CommandLineArgPassed("toughness-test");

    /// <summary>Test stages; the harness advances Idle → LogicDone → done/quit.</summary>
    private enum Stage { Idle, LogicDone }

    private Stage stage = Stage.Idle;

    /// <summary>Colonist picked for the damage/XP test; reused by the UI pass for the card draw.</summary>
    private Pawn? testPawn;

    /// <summary>Unused ctor arg — the game instantiates components with the <see cref="Game"/>.</summary>
    public ToughnessSmokeTest(Game game) { }

    /// <summary>Runs the non-UI assertions once a map with pawns exists.</summary>
    public override void GameComponentTick()
    {
        if (!enabled || stage != Stage.Idle)
        {
            return;
        }
        if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null)
        {
            return;
        }

        stage = Stage.LogicDone;
        try
        {
            RunLogicTests();
        }
        catch (Exception e) when (!e.IsFatal())
        {
            Fail($"unhandled exception: {e}");
        }
    }

    /// <summary>
    /// Draws the settings window and a character card into an offscreen clip rect during a
    /// real repaint — full IMGUI code-path coverage with zero on-screen output — then exits.
    /// </summary>
    public override void GameComponentOnGUI()
    {
        // Only repaint events produce real widget draws; Layout/etc. would hit the
        // interactive paths against a half-initialised GUI state.
        if (!enabled || stage != Stage.LogicDone || Event.current == null || Event.current.type != EventType.Repaint)
        {
            return;
        }
        stage = Stage.Idle;   // Block re-entry even if Quit is deferred a frame.
        try
        {
            GUI.BeginClip(new Rect(-2000f, -2000f, 900f, 700f));
            try
            {
                Rect content = new Rect(0f, 0f, 900f, 700f);
                Mod mod = LoadedModManager.GetMod<ToughnessSkillMod>();
                mod.DoSettingsWindowContents(content);
                if (testPawn != null)
                {
                    CharacterCardUtility.DrawCharacterCard(content, testPawn);
                }
            }
            finally
            {
                GUI.EndClip();
            }
            Log.Message("[Toughness] smoke test PASS");
            Application.Quit(0);
        }
        catch (Exception e) when (!e.IsFatal())
        {
            Fail($"UI pass failed: {e}");
        }
    }

    /// <summary>
    /// Asserts the patch wiring end-to-end: pawn generation assigns Toughness (postfix
    /// ran iff the comp got initialised), a damage event grants XP, and both stats read
    /// finite values through the mod's StatParts.
    /// </summary>
    private void RunLogicTests()
    {
        SkillDef? toughnessDef = DefDatabase<SkillDef>.GetNamed("Toughness", errorOnFail: false);
        if (toughnessDef == null)
        {
            Fail("Toughness SkillDef not loaded");
            return;
        }

        // 1) Generation: fixed adult so backstory/trait rolls are deterministic-class —
        //    children legitimately roll different init (and can have zero traits).
        Pawn scratch = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist, null, PawnGenerationContext.NonPlayer,
            forceGenerateNewPawn: true, fixedBiologicalAge: 30f, forceNoGear: true));
        try
        {
            SkillRecord? skill = scratch.skills?.GetSkill(toughnessDef);
            CompToughnessCache? comp = scratch.TryGetComp<CompToughnessCache>();
            if (skill == null)
            {
                Fail("scratch pawn has no Toughness SkillRecord");
                return;
            }
            if (comp == null || !comp.compintitialised)
            {
                Fail("GenerateSkills postfix did not initialise CompToughnessCache");
                return;
            }
            if (skill.levelInt < 0 || skill.levelInt > 8)
            {
                Fail($"Toughness level {skill.levelInt} outside expected 0..8 (1d6 ±2 backstory, clamped)");
                return;
            }
        }
        finally
        {
            // Never DiscardGeneratedPawn outside a generation call — in 1.7 it pops an
            // empty stack (InvalidOperationException). Manual world-pawn cleanup instead.
            if (Find.WorldPawns.Contains(scratch))
            {
                Find.WorldPawns.RemovePawn(scratch);
            }
            Find.WorldPawns.PassToWorld(scratch, PawnDiscardDecideMode.Discard);
        }

        // 2) Damage → XP: a spawned player colonist exercises the full
        //    DamageWorker_AddInjury → ApplyDamageToPart → NotifyDamageApplied chain.
        testPawn = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive.FirstOrDefault(
            (Pawn p) => p.IsColonist && p.Spawned && p.RaceProps.Humanlike && p.skills != null);
        if (testPawn == null)
        {
            Fail("no spawned colonist found on quicktest map");
            return;
        }
        SkillRecord? colonistSkill = testPawn.skills.GetSkill(toughnessDef);
        if (colonistSkill == null)
        {
            Fail("colonist has no Toughness SkillRecord");
            return;
        }
        float xpBefore = colonistSkill.xpSinceLastLevel;
        int levelBefore = colonistSkill.levelInt;
        testPawn.TakeDamage(new DamageInfo(DamageDefOf.Cut, 15f));
        if (colonistSkill.xpSinceLastLevel <= xpBefore && colonistSkill.levelInt <= levelBefore)
        {
            Fail("damage event granted no Toughness XP (ApplyDamageToPart postfix not applying?)");
            return;
        }

        // 3) Stat reads: both stat parts must produce finite numbers, not NaN/∞.
        StatDef? incoming = DefDatabase<StatDef>.GetNamed("IncomingDamageFactor", errorOnFail: false);
        StatDef? bleed = DefDatabase<StatDef>.GetNamed("FinalBleedRate", errorOnFail: false);
        if (incoming == null || bleed == null)
        {
            Fail("stat defs missing");
            return;
        }
        float incomingVal = testPawn.GetStatValue(incoming);
        float bleedVal = testPawn.GetStatValue(bleed);
        if (!Finite(incomingVal))
        {
            Fail($"IncomingDamageFactor read non-finite: {incomingVal}");
            return;
        }
        if (!Finite(bleedVal))
        {
            Fail($"FinalBleedRate read non-finite: {bleedVal}");
        }
    }

    /// <summary>NaN/∞ guard — StatPart maths must never produce either.</summary>
    private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);

    /// <summary>Logs the failure and quits with a nonzero exit code for the outer watchdog.</summary>
    private static void Fail(string what)
    {
        Log.Error($"[Toughness] smoke test FAIL: {what}");
        Application.Quit(1);
    }
}
