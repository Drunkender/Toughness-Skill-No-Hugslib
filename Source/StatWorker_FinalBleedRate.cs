using RimWorld;
using Verse;

namespace ToughnessSkillMod
{
    public class StatWorker_FinalBleedRate : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            if (!req.HasThing)
                return false;

            Pawn pawn = req.Thing as Pawn;
            if (pawn == null)
                return false;

            return pawn.RaceProps.IsFlesh;
        }
    }
}
