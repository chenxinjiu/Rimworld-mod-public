using RimWorld;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// 配置项：信标调用信使在 XML 里通过 &lt;comp Class="HelldiversRim.CompProperties_StratagemBeacon"&gt;
    /// 指定要空投的炮塔 Def（mortarSentryDef）、空投舱开舱延迟（podOpenDelay），
    /// 以及落点信标光柱特效（beaconPillarDef）。
    /// </summary>
    public class CompProperties_StratagemBeacon : CompProperties
    {
        public ThingDef mortarSentryDef;
        public int podOpenDelay = 60;
        public ThingDef beaconPillarDef;

        public CompProperties_StratagemBeacon()
        {
            compClass = typeof(CompStratagemBeacon);
        }
    }

    /// <summary>
    /// 附着在"战略配备信标"武器上。当空投物落地后由 Projectile_Stratagem 调用 CallIn，
    /// 在目标格点掉一艘空投舱（drop pod），舱内装有哨戒炮塔；同时在该格生成一根
    /// 蓝色光柱（信标动画），标示此次空投落点。
    /// </summary>
    public class CompStratagemBeacon : ThingComp
    {
        public CompProperties_StratagemBeacon Props => (CompProperties_StratagemBeacon)props;

        /// <summary>在指定地图格上生成落点光柱，并召唤载有哨戒炮塔的空投舱。</summary>
        public void CallIn(IntVec3 cell, Map map)
        {
            SpawnPillar(cell, map);

            ThingDef mortarDef = Props.mortarSentryDef;
            if (mortarDef == null)
            {
                Log.Warning("HelldiversRim: stratagem beacon has no mortarSentryDef set.");
                return;
            }

            Thing turret = ThingMaker.MakeThing(mortarDef);

            ActiveDropPodInfo info = new ActiveDropPodInfo
            {
                SingleContainedThing = turret,
                openDelay = Props.podOpenDelay
            };

            DropPodUtility.MakeDropPodAt(cell, map, info);
        }

        /// <summary>在落点生成一根蓝色光柱（Mote）。Mote 会自动淡入/淡出并自毁。</summary>
        private void SpawnPillar(IntVec3 cell, Map map)
        {
            if (Props.beaconPillarDef == null)
                return;

            Thing pillar = ThingMaker.MakeThing(Props.beaconPillarDef);
            GenSpawn.Spawn(pillar, cell, map);
        }
    }
}