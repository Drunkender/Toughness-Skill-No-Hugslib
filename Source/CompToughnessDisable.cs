using RimWorld;
using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// One-shot <see cref="GameComponent"/> (auto-instantiated by the game for every
/// non-abstract subclass) that refreshes all toughness caches once the world is fully
/// loaded — catches pawns that existed before the mod's comp/setting state settled.
/// </summary>
/// <remarks>
/// Named ToughnessInitComponent despite living in CompToughnessDisable.cs — a rename is
/// safe internally but the file name stays for history. Overlaps with
/// <see cref="ToughnessGameComponent"/> on purpose: that one rolls starting levels on
/// pre-mod saves, this one only re-syncs caches.
/// </remarks>
public class ToughnessInitComponent : GameComponent
{
    private bool initialized;

    /// <summary>Unused ctor arg — the game instantiates components with the <see cref="Game"/>.</summary>
    public ToughnessInitComponent(Game game) { }

    /// <summary>Fires the refresh exactly once, after a map + player faction exist.</summary>
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
