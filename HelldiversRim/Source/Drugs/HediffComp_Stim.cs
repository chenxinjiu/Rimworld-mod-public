using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HelldiversRim
{
    // ========================================================
    // 立即效果：注入时触发
    //   1) 清除流血（BloodLoss）
    //   2) 恢复断肢（移除 Hediff_MissingPart）
    //   3) 清除所有伤口（Hediff_Injury）
    //   4) 重置依赖计时（severity → 0，没有依赖则添加）
    // ========================================================

    public class HediffCompProperties_StimEffect : HediffCompProperties
    {
        public HediffCompProperties_StimEffect()
        {
            compClass = typeof(HediffComp_StimEffect);
        }
    }

    public class HediffComp_StimEffect : HediffComp
    {
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);

            Pawn pawn = Pawn;
            if (pawn?.health == null) return;

            // 1) 清除流血
            var bloodLoss = pawn.health.hediffSet.hediffs
                .Where(h => h.def == HediffDefOf.BloodLoss)
                .ToList();
            foreach (var h in bloodLoss)
                pawn.health.RemoveHediff(h);

            // 2) 恢复断肢（移除 MissingPart）
            var missingParts = pawn.health.hediffSet.hediffs
                .Where(h => h is Hediff_MissingPart)
                .ToList();
            foreach (var h in missingParts)
                pawn.health.RemoveHediff(h);

            // 3) 清除所有伤口
            var injuries = pawn.health.hediffSet.hediffs
                .Where(h => h is Hediff_Injury)
                .ToList();
            foreach (var h in injuries)
                pawn.health.RemoveHediff(h);

            // 4) 重置依赖：没有就添加，有就清零
            Hediff addiction = pawn.health.hediffSet.hediffs
                .FirstOrDefault(h => h.def.defName == "Helldivers_StimAddiction");
            if (addiction == null)
            {
                HediffDef addictDef = DefDatabase<HediffDef>.GetNamedSilentFail("Helldivers_StimAddiction");
                if (addictDef != null)
                {
                    addiction = HediffMaker.MakeHediff(addictDef, pawn);
                    addiction.Severity = 0f;
                    pawn.health.AddHediff(addiction);
                }
            }
            else
            {
                addiction.Severity = 0f;
            }
        }
    }

    // ========================================================
    // 8 小时持续回血：每 tick 恢复 1% 最大血量
    //   高潮 hediff severity 从 1.0 降到 0（1 天）。
    //   severity > 0.667 = 前 1/3 天 = 前 8 小时 → 回血激活
    // ========================================================

    public class HediffCompProperties_StimRegen : HediffCompProperties
    {
        public HediffCompProperties_StimRegen()
        {
            compClass = typeof(HediffComp_StimRegen);
        }
    }

    public class HediffComp_StimRegen : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // 仅在前 8 小时（severity > 0.667）回血
            if (parent.Severity <= 0.667f) return;

            Pawn pawn = Pawn;
            if (pawn?.health == null) return;

            // 找到最严重的伤口并治疗
            var injuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(i => i.Severity > 0f)
                .ToList();

            if (injuries.Count > 0)
            {
                var worst = injuries.OrderByDescending(i => i.Severity).First();
                // 每 tick 恢复 ~0.02 严重度（约 1% 血量）
                worst.Severity -= 0.02f;
                if (worst.Severity <= 0f)
                    pawn.health.RemoveHediff(worst);
            }
        }
    }
}
