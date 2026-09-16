using System.Linq;
using RimWorld;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// 开局确保 Super Earth 派系存在并固定为玩家盟友。
    /// canMakeRandomly=false 的派系世界不会自动生成，因此在这里显式创建并结盟，
    /// 供"战备呼叫"的盟友背景与未来文案/事件使用。
    /// </summary>
    public class WorldComponent_HelldiversFaction : WorldComponent
    {
        public WorldComponent_HelldiversFaction(World world) : base(world)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail("Helldivers_SuperEarth");
            if (def == null)
                return;

            Faction faction = Find.World.factionManager.AllFactions.FirstOrDefault(f => f.def == def);
            if (faction == null)
            {
                faction = FactionGenerator.NewGeneratedFaction(def);
                Find.World.factionManager.Add(faction);
            }

            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction != null && faction != null)
                faction.SetRelationDirect(playerFaction, FactionRelationKind.Ally);
        }
    }
}