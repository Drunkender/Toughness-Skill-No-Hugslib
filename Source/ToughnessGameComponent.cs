using RimWorld;
using System.Linq;
using Verse;

namespace ToughnessSkillMod
{
    public class ToughnessGameComponent : GameComponent
    {
        private bool initialized;

        private static ThoughnessSkillSettings Settings =>
LoadedModManager.GetMod<ToughnessSkillMod>()?.GetSettings<ThoughnessSkillSettings>() ?? ToughnessSkillMod.settings;

        public ToughnessGameComponent(Game game) { }

        public override void GameComponentTick()
        {
            if (initialized) return;

            if (Find.Maps != null && Find.Maps.Any())
            {
                initialized = true;
                InitializeToughness();
            }
        }

        private void InitializeToughness()
        {
            var pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive.Where((Pawn p) => p.RaceProps.Humanlike);
            foreach (var pawn in pawns)
            {
                SkillRecord skill = pawn.skills?.GetSkill(DefDatabase<SkillDef>.GetNamed("Toughness", false));
                if (skill != null && skill.levelInt <= 0 && !skill.TotallyDisabled)
                {
                    int baseToughness = Rand.RangeInclusive(1, 6);
                    skill.levelInt = Patch_AdjustStartingToughness.AdjustToughnessByBackstory(pawn, baseToughness);
                    skill.xpSinceLastLevel = 0f;

                    CompToughnessCache comp = pawn.TryGetComp<CompToughnessCache>();
                    comp?.InitialiseComp(pawn, comp, skill);

                    if (Prefs.DevMode && Settings.debugMode)
                    {
                        Log.Message($"[Toughness] Init {pawn.LabelShortCap} → Toughness {skill.levelInt}");
                    }
                }
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref initialized, "initialized", false);
        }
    }
}
