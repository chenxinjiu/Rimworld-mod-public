using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// 配置项：持续型便携呼叫器的呼叫目标。XML 里通过
    /// &lt;comp Class="HelldiversRim.CompProperties_StratagemCaller"&gt; 指定：
    ///   mortarSentryDef  要呼叫的炮塔 Def
    ///   podOpenDelay      空投舱开舱延迟
    ///   beaconPillarDef   落点蓝色光柱 Def
    ///   silverCost        每次呼叫花费的白银（默认 0 = 免费）
    /// </summary>
    public class CompProperties_StratagemCaller : CompProperties
    {
        public ThingDef mortarSentryDef;
        public int podOpenDelay = 60;
        public ThingDef beaconPillarDef;
        public int silverCost = 0;

        public CompProperties_StratagemCaller()
        {
            compClass = typeof(CompStratagemCaller);
        }
    }

    /// <summary>
    /// 持续型便携呼叫器（副手装备）。携带者（玩家殖民者）会获得一个"呼叫战备"Gizmo，
    /// 选点后不消耗地呼叫一座哨戒炮塔。装备占副手槽（equipmentType=Secondary），
    /// 不影响主武器。呼叫统一走 HelldiversCommsUtility.CallInTurret。
    /// </summary>
    public class CompStratagemCaller : ThingComp
    {
        public CompProperties_StratagemCaller Props => (CompProperties_StratagemCaller)props;

        public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
                yield return g;

            // 只有被玩家携带的使用员身上才显示呼叫 Gizmo。
            Pawn pawn = parent.ParentHolder as Pawn;
            if (pawn == null || !pawn.RaceProps.Humanlike || pawn.Faction != Faction.OfPlayer)
                yield break;
            if (Props.mortarSentryDef == null)
                yield break;

            Command_Action cmd = new Command_Action
            {
                defaultLabel = "呼叫战备：" + Props.mortarSentryDef.label,
                defaultDesc = "不消耗地呼叫一座 " + Props.mortarSentryDef.label + " 空投到指定落点。",
                action = BeginCallIn
            };

            yield return cmd;
        }

        private void BeginCallIn()
        {
            Map map = parent.Map;
            if (map == null)
                return;

            TargetingParameters targetParams = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = false,
                canTargetBuildings = true,
                validator = (TargetInfo t) => t.Cell.InBounds(map)
            };

            Find.Targeter.BeginTargeting(targetParams, OnTargetSelected);
        }

        private void OnTargetSelected(LocalTargetInfo target)
        {
            Map map = parent.Map;
            if (map == null)
                return;

            if (Props.silverCost > 0 && !HelldiversCommsUtility.TrySpendSilver(map, Props.silverCost))
            {
                Messages.Message("白银不足，无法呼叫战备。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            HelldiversCommsUtility.CallInTurret(
                target.Cell,
                map,
                Props.mortarSentryDef,
                Props.beaconPillarDef,
                Props.podOpenDelay);
        }
    }
}
