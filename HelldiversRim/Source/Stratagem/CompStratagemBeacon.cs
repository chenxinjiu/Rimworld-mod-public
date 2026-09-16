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
    /// 附着在"战略配备信标"武器上（便携版）。当信标落地后由 Projectile_Stratagem 调用 CallIn，
    /// 统一经由 HelldiversCommsUtility.CallInTurret 在目标格生成蓝色光柱并空投哨戒炮塔。
    /// </summary>
    public class CompStratagemBeacon : ThingComp
    {
        public CompProperties_StratagemBeacon Props => (CompProperties_StratagemBeacon)props;

        /// <summary>在指定地图格上生成落点光柱，并召唤载有哨戒炮塔的空投舱。</summary>
        public void CallIn(IntVec3 cell, Map map)
        {
            HelldiversCommsUtility.CallInTurret(
                cell,
                map,
                Props.mortarSentryDef,
                Props.beaconPillarDef,
                Props.podOpenDelay);
        }
    }
}