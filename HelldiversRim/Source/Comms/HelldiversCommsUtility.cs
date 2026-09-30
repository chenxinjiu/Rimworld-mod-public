using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// 战备呼叫的统一入口（CallIn）。通讯终端 / 信标（便携版）/（未来）SOS2 飞船空投，
    /// 最终都通过这里落地：在指定落点生成蓝色光柱，并空投一艘载有炮塔的空投舱。
    /// 同时提供殖民地白银扣除工具（通讯终端的呼叫成本）。
    /// </summary>
    public static class HelldiversCommsUtility
    {
        /// <summary>在指定落点生成光柱并召唤载有炮塔的空投舱。</summary>
        public static void CallInTurret(IntVec3 cell, Map map, ThingDef turretDef, ThingDef pillarDef, int openDelay = 60)
        {
            if (pillarDef != null)
            {
                Thing pillar = ThingMaker.MakeThing(pillarDef);
                GenSpawn.Spawn(pillar, cell, map);
            }

            if (turretDef == null)
                return;

            Thing turret = ThingMaker.MakeThing(turretDef);

            ActiveTransporterInfo info = new ActiveTransporterInfo
            {
                SingleContainedThing = turret,
                openDelay = openDelay
            };

            DropPodUtility.MakeDropPodAt(cell, map, info);
        }

        /// <summary>从殖民地白银库存中扣除 amount；若派出方没有足够白银返回 false。</summary>
        public static bool TrySpendSilver(Map map, int amount)
        {
            if (map == null)
                return amount == 0;
            if (amount <= 0)
                return true;

            int remaining = amount;
            List<Thing> all = map.listerThings.AllThings;
            for (int i = 0; i < all.Count; i++)
            {
                if (remaining <= 0)
                    break;

                Thing t = all[i];
                if (t.Destroyed || t.def != ThingDefOf.Silver || t.Faction != Faction.OfPlayer)
                    continue;

                int take = Mathf.Min(remaining, t.stackCount);
                t.stackCount -= take;
                remaining -= take;

                if (t.stackCount <= 0)
                    t.Destroy();
            }

            return remaining <= 0;
        }

        /// <summary>
        /// 供通讯终端 / 便携呼叫器等共用的完整呼叫流程：扣白银 → 生成光柱 → 空投炮塔。
        /// silverCost 为 0 时视为免费；返回是否成功（白银不足时返回 false 且不召唤）。
        /// </summary>
        public static bool TrySpendSilverAndCallIn(Map map, IntVec3 cell, ThingDef turretDef, ThingDef pillarDef, int silverCost, int openDelay = 60)
        {
            if (silverCost > 0 && !TrySpendSilver(map, silverCost))
            {
                Messages.Message("Helldivers_SilverInsufficient".Translate().RawText, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            CallInTurret(cell, map, turretDef, pillarDef, openDelay);
            return true;
        }
    }
}