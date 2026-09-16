using System.Linq;
using System.Reflection;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// CE（Combat Extended）软依赖适配层。
    /// 通过 ModsConfig.IsActive 判断 CE 是否安装，读 CE 弹药余量时用反射，避免
    /// 编译期硬引用 CE 程序集——这样 dlld 在有没有 CE 的机器上都能正常加载。
    /// </summary>
    public static class CECompat
    {
        /// <summary>CE 的 packageId。</summary>
        public const string CEPackageId = "CETeam.CombatExtended";

        private static readonly bool? active;

        static CECompat()
        {
            active = ModsConfig.IsActive(CEPackageId);
        }

        public static bool IsActive => active == true;

        /// <summary>
        /// 读取指定炮塔上 CE 弹药余量；读不到时返回 null（安全降级，不触发自毁）。
        /// CE 里弹药字段名因版本而异，这里对若干候选名做反射探测并告警一次。
        /// </summary>
        public static int? AmmoRemaining(Thing parent)
        {
            if (!IsActive)
                return null;

            ThingComp ceComp = parent.AllComps.FirstOrDefault(c =>
                c.GetType().Namespace != null && c.GetType().Namespace.StartsWith("CombatExtended"));
            if (ceComp == null)
                return null;

            System.Type t = ceComp.GetType();
            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.IgnoreCase;

            foreach (string candidate in new[] { "AmmoRemaining", "MagAmmoCount", "AmmoCount", "RemainingAmmo" })
            {
                PropertyInfo pi = t.GetProperty(candidate, flags);
                if (pi != null && pi.PropertyType == typeof(int))
                    return (int)pi.GetValue(ceComp);

                FieldInfo fi = t.GetField(candidate, flags);
                if (fi != null && fi.FieldType == typeof(int))
                    return (int)fi.GetValue(ceComp);
            }

            Log.WarningOnce(
                "HelldiversRim: 无法读取 CE 弹药余量来触发哨戒自毁，请检查 CE 版本并核对候选字段名。",
                "Helldivers_CEAmmoLookup".GetHashCode());
            return null;
        }
    }
}