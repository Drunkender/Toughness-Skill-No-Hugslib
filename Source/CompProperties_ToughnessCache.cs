using Verse;

namespace ToughnessSkillMod;

/// <summary>
/// <see cref="CompProperties"/> binding <see cref="CompToughnessCache"/> — injected into
/// the <c>Human</c> ThingDef's comp list by <c>Patches/Comp_ToughnessCache.xml</c>.
/// </summary>
public class CompProperties_ToughnessCache : CompProperties
{
    /// <summary>Points the def loader at <see cref="CompToughnessCache"/>.</summary>
	public CompProperties_ToughnessCache()
	{
		compClass = typeof(CompToughnessCache);
	}
}
