using RimWorld;
using Verse;

namespace ToughnessSkillMod;
public class ToughnessInitComponent : GameComponent
{
    private bool initialized = false;

    public ToughnessInitComponent(Game game) { }

    public override void GameComponentTick()
    {
        if (initialized)
            return;

        if (Current.ProgramState != ProgramState.Playing)
            return;

        if (Faction.OfPlayer == null)
            return;

        if (Find.CurrentMap == null)
            return;

        initialized = true;
        ToughnessSkillMod.RefreshCaches();
    }
}
