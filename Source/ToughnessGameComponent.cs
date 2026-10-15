using RimWorld;
using System.Linq;
using Verse;

namespace ToughnessSkillMod
{
    /// <summary>
    /// One-shot <see cref="GameComponent"/> that rolls starting Toughness for humanlike
    /// pawns on the first tick after a map exists — the backfill path for saves that
    /// predate the mod (generated pawns get theirs from <see cref="Patch_AdjustStartingToughness"/>).
    /// </summary>
    public class ToughnessGameComponent : GameComponent
    {
        /// <summary>Persisted so the backfill runs once per save, not once per session.</summary>
        private bool initialized;

        private static ThoughnessSkillSettings Settings =>
        LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

        /// <summary>Unused ctor arg — the game instantiates components with the <see cref="Game"/>.</summary>
        public ToughnessGameComponent(Game game) { }

        /// <summary>Runs the backfill on the first tick where any map is loaded.</summary>
        public override void GameComponentTick()
        {
            if (initialized) return;

            if (Find.Maps != null && Find.Maps.Any())
            {
                initialized = true;
                InitializeToughness();
            }
        }

        /// <summary>
        /// Assigns a 1–6 starting level (backstory-adjusted) to every living humanlike
        /// pawn that doesn't already have one, then initialises their comp cache.
        /// </summary>
        private static void InitializeToughness()
        {
            var pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive.Where((Pawn p) => p.RaceProps.Humanlike);
            foreach (var pawn in pawns)
            {
                SkillRecord? skill = pawn.skills?.GetSkill(DefDatabase<SkillDef>.GetNamed("Toughness", false));
                if (skill != null && skill.levelInt <= 0 && !skill.TotallyDisabled)
                {
                    int baseToughness = Rand.RangeInclusive(1, 6);
                    skill.levelInt = Patch_AdjustStartingToughness.AdjustToughnessByBackstory(pawn, baseToughness);
                    skill.xpSinceLastLevel = 0f;

                    CompToughnessCache comp = pawn.TryGetComp<CompToughnessCache>();
                    comp?.InitialiseComp(pawn, skill);

                    if (Prefs.DevMode && Settings.debugMode)
                    {
                        Log.Message($"[Toughness] Init {pawn.LabelShortCap} → Toughness {skill.levelInt}");
                    }
                }
            }
        }

        /// <summary>Persists <see cref="initialized"/> so backfill doesn't re-roll loaded saves.</summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref initialized, "initialized", false);
        }
    }
}
