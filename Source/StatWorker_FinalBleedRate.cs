using RimWorld;
using Verse;

namespace ToughnessSkillMod
{
    /// <summary>
    /// <see cref="StatWorker"/> for the mod's <c>FinalBleedRate</c> display stat —
    /// restricts it to fleshy pawns (bleeding is meaningless for mechanoids).
    /// </summary>
    public class StatWorker_FinalBleedRate : StatWorker
    {
        /// <summary>Only flesh pawns bleed.</summary>
        /// <param name="req">Stat request being considered for display.</param>
        /// <returns>True when the request targets a flesh-and-blood pawn.</returns>
        public override bool ShouldShowFor(StatRequest req)
        {
            if (!req.HasThing || req.Thing is not Pawn pawn)
                return false;

            return pawn.RaceProps.IsFlesh;
        }
    }
}
