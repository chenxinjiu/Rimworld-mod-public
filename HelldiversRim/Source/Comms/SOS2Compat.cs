using RimWorld;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// SOS2（Save Our Ship 2）软依赖适配层。与 CECompat 同思路：运行时用 ModsConfig
    /// 判断是否安装，避免编译期硬引用 SOS2 程序集——dll 在有没有 SOS2 的机器上都能加载。
    ///
    /// 设计目标（来自 ARCHITECTURE.md Phase 3）：
    /// 装了 SOS2 时，玩家若拥有一艘在轨飞船，可从飞船向星球空投炮台；否则/未装 SOS2
    /// 则回落到通讯终端呼叫。
    ///
    /// ⚠️ SOS2 的"玩家在轨飞船"具体类型/成员会随版本变化，PlayerHasOrbitalShip 目前是
    /// 待补的判定点（留 TODO）。实机测试后按 SOS2 实际 API 补上即可。
    /// </summary>
    public static class SOS2Compat
    {
        /// <summary>SOS2 的 packageId（实测时确认）。</summary>
        public const string PackageId = "kentington.SaveOurShip2";

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
        /// 玩家阵营是否拥有一艘在轨飞船（SOS2）。
        /// 判定点：定位 SOS2 中"玩家在轨飞船/舰队"的组件或字段。
        /// 未实现时返回 null（安全降级 → 呼叫回落到通讯终端路径，不崩）。
        /// </summary>
        public static bool? PlayerHasOrbitalShip()
        {
            if (!IsActive)
                return null;

            // TODO(SOS2 API)：SOS2 中玩家在轨飞船通常由 SOS2 的
            //   worldComponents 或飞船舰队逻辑托管。确认实际类型/字段后在此返回 true/false。
            // 当前默认 false，保证先走通讯终端路径、不触发 SOS2 专有代码。
            return false;
        }
    }
}