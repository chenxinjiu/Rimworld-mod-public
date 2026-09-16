using RimWorld;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// 配置项：信标调用信使在 XML 里通过 &lt;comp Class="HelldiversRim.CompProperties_StratagemBeacon"&gt;
    /// 指定要空投的炮塔 Def（mortarSentryDef）与空投舱开舱延迟（podOpenDelay）。
    /// </summary>
    public class CompProperties_StratagemBeacon : CompProperties
    {
        public ThingDef mortarSentryDef;
        public int podOpenDelay = 465;

        public CompProperties_StratagemBeacon()
        {
            compClass = typeof(CompStratagemBeacon);
        }
    }

    /// <summary>
    /// 附着在"战略配备信标"武器上。当空投物落地后由 Projectile_Stratagem 调用 CallIn，
    /// 在目标格顶点掉一艘空投舱（drop pod），舱内装有哨戒炮塔。
    /// </summary>
    public class CompStratagemBeacon : ThingComp
    {
        public CompProperties_StratagemBeacon Props => (CompProperties_StratagemBeacon)props;

        /// <summary>在指定地图格上召唤空投舱，内含哨戒炮塔。</summary>
        public void CallIn(IntVec3 cell, Map map)
        {
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
    }
}