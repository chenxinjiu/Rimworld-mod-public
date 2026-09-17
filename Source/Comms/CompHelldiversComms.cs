using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// 通讯终端的战备呼叫能力配置。XML 里通过
    /// &lt;comp Class="HelldiversRim.CompProperties_HelldiversComms"&gt; 指定：
    /// turretDef         要呼叫的炮塔 Def
    /// pillarDef          落点蓝色光柱 Def
    /// silverCost         每次呼叫花费的白银
    /// researchRequired   解锁此呼叫所需的研究（可为空/不写）
    /// </summary>
    public class CompProperties_HelldiversComms : CompProperties
    {
        public ThingDef turretDef;
        public ThingDef pillarDef;
        public int silverCost = 500;
        public ResearchProjectDef researchRequired;

        public CompProperties_HelldiversComms()
        {
            compClass = typeof(CompHelldiversComms);
        }
    }

    /// <summary>
    /// 挂在通讯终端建筑上：提供一个"呼叫战备"Gizmo，选择落点后扣除白银并空投炮塔。
    /// 所有 UI 文案走 Keyed 翻译（中英文双语）。
    /// </summary>
    public class CompHelldiversComms : ThingComp
    {
        public CompProperties_HelldiversComms Props => (CompProperties_HelldiversComms)props;

        private Map Map => parent.Map;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
                yield return g;

            if (Props.turretDef == null)
                yield break;

            Command_Action cmd = new Command_Action
            {
                defaultLabel = "Helldivers_CallInLabel".Translate(Props.turretDef.label).RawText,
                defaultDesc = (SOS2Compat.IsActive
                    ? "Helldivers_CommsDescOrbital".Translate(Props.turretDef.label)
                    : "Helldivers_CommsDescCost".Translate(Props.silverCost, Props.turretDef.label)).RawText,
                action = BeginCallIn
            };

            // 研究门槛
            bool researchOk = Props.researchRequired == null || Props.researchRequired.IsFinished;
            if (!researchOk)
                cmd.Disable("Helldivers_ResearchRequired".Translate(
                    Props.researchRequired?.label ?? "Helldivers_Unknown".Translate().RawText).RawText);

            // 电力门槛（若建筑配置了 CompPowerTrader）
            CompPowerTrader power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
                cmd.Disable("Helldivers_PowerRequired".Translate().RawText);

            yield return cmd;
        }

        /// <summary>打开地图落点瞄准。</summary>
        private void BeginCallIn()
        {
            TargetingParameters targetParams = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = false,
                canTargetBuildings = true,
                validator = (TargetInfo t) => t.Cell.InBounds(Map)
            };

            Find.Targeter.BeginTargeting(
                targetParams,
                OnTargetSelected);
        }

        private void OnTargetSelected(LocalTargetInfo target)
        {
            Map map = Map;
            if (map == null)
                return;

            // SOS2：装有 SOS2 且确认在轨飞船时走轨道空投。
            bool? ship = SOS2Compat.PlayerHasOrbitalShip();
            if (ship == true)
            {
                SOS2Compat.TryOrbitalDrop(target.Cell, map, Props.turretDef, Props.pillarDef);
                return;
            }

            if (!HelldiversCommsUtility.TrySpendSilver(map, Props.silverCost))
            {
                Messages.Message("Helldivers_SilverInsufficient".Translate().RawText, MessageTypeDefOf.RejectInput, false);
                return;
            }

            HelldiversCommsUtility.CallInTurret(target.Cell, map, Props.turretDef, Props.pillarDef);
        }
    }
}
