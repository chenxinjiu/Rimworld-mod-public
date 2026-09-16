using System.Linq;
using RimWorld;
using RimWorld.Planet;
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

        public override void WorldComponentUpdate()
        {
            base.WorldComponentUpdate();

            // 1.6 的 WorldComponent 已无 FinalizeInit，改用每帧调用的 WorldComponentUpdate；
            // 逻辑保证幂等（已存在则不再重复创建/结盟）。
            FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail("Helldivers_SuperEarth");
            if (def == null)
                return;

            Faction faction = Find.World.factionManager.AllFactions.FirstOrDefault(f => f.def == def);
            if (faction == null)
            {
                faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def));
                if (faction == null)
                    return;
                Find.World.factionManager.Add(faction);
            }

            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction != null
                && faction.RelationWith(playerFaction).kind != FactionRelationKind.Ally)
            {
                faction.SetRelationDirect(playerFaction, FactionRelationKind.Ally);
            }
        }
    }
}