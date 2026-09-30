using System;
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

            // 1-3) 清除流血 / 断肢 / 所有伤口
            RemoveHediffs(pawn,
                h => h.def == HediffDefOf.BloodLoss,
                h => h is Hediff_MissingPart,
                h => h is Hediff_Injury);

            // 4) 重置依赖：没有就添加，有就清零
            const string addictionDefName = "Helldivers_StimAddiction";
            Hediff addiction = pawn.health.hediffSet.hediffs
                .FirstOrDefault(h => h.def.defName == addictionDefName);
            if (addiction == null)
            {
                HediffDef addictDef = DefDatabase<HediffDef>.GetNamedSilentFail(addictionDefName);
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

        private static void RemoveHediffs(Pawn pawn, params Func<Hediff, bool>[] predicates)
        {
            var toRemove = pawn.health.hediffSet.hediffs
                .Where(h => predicates.Any(p => p(h)))
                .ToList();
            foreach (var h in toRemove)
                pawn.health.RemoveHediff(h);
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
            Hediff_Injury worst = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(i => i.Severity > 0f)
                .OrderByDescending(i => i.Severity)
                .FirstOrDefault();
            if (worst == null) return;

            // 每 tick 恢复 ~0.02 严重度（约 1% 血量）
            worst.Severity -= 0.02f;
            if (worst.Severity <= 0f)
                pawn.health.RemoveHediff(worst);
        }
    }
}
