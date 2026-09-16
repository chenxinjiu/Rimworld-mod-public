using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// SOS2（Save Our Ship 2）软依赖适配层。与 CECompat 同思路：运行时用 ModsConfig
    /// 判断是否安装，避免编译期硬引用 SOS2 程序集——dll 在有没有 SOS2 的机器上都能加载。
    ///
    /// 设计目标：装了 SOS2 时，玩家若拥有一艘在轨飞船，可从飞船（大概率靠"轨道空投"）
    /// 向星球呼叫炮台；未装 SOS2 或无在轨飞船时，回落通讯终端 / 便携呼叫器路径。
    ///
    /// 检测"在轨飞船"无需反射 SOS2 私有类型：SOS2 会把玩家发射进轨道的飞船注册为一个
    /// 世界对象（WorldObjectDef defName=ShipOrbiting，类 SaveOurShip2.WorldObjectOrbitingShip，
    /// canBePlayerHome=true）。这里只用 RimWorld 标准 API 遍历世界对象 + 玩家派系判断，
    /// 对 SOS2 版本相对稳健。
    /// </summary>
    public static class SOS2Compat
    {
        /// <summary>SOS2 的 packageId。</summary>
        public const string PackageId = "kentington.saveourship2";

        /// <summary>SOS2 中"在轨飞船"的世界对象 defName（见 Defs/WorldObjectDefs/WorldObjects.xml）。</summary>
        private const string OrbitalShipDefName = "ShipOrbiting";

        private static readonly bool? active;

        static SOS2Compat()
        {
            active = ModsConfig.IsActive(PackageId);
        }

        public static bool IsActive => active == true;

        /// <summary>从在轨飞船向落点空投炮台（当前实现复用统一呼叫入口）。</summary>
        public static bool TryOrbitalDrop(IntVec3 cell, Map map, ThingDef turretDef, ThingDef pillarDef, int openDelay = 60)
        {
            if (!IsActive)
                return false;

            HelldiversCommsUtility.CallInTurret(cell, map, turretDef, pillarDef, openDelay);
            return true;
        }

        /// <summary>
        /// 玩家阵营是否拥有一艘在轨飞船（SOS2）。三态：
        ///   null → 未安装 SOS2（主调方应走"无轨道支援"的回落路径）；
        ///   true → 已确认有玩家在轨飞船（可走轨道呼叫）；
        ///   false→ 装了 SOS2 但没有玩家在轨飞船。
        /// </summary>
        public static bool? PlayerHasOrbitalShip()
        {
            if (!IsActive)
                return null;

            List<WorldObject> objs = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < objs.Count; i++)
            {
                WorldObject wo = objs[i];
                if (wo == null || wo.def == null || wo.Faction == null)
                    continue;

                if (string.Equals(wo.def.defName, OrbitalShipDefName, StringComparison.Ordinal)
                    && wo.Faction == Faction.OfPlayer)
                {
                    return true;
                }
            }

            return false;
        }
    }
}