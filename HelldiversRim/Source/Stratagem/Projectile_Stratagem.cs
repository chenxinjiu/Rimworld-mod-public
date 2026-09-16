using RimWorld;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// 信标抛射物：飞到目标格后触发 CompStratagemBeacon.CallIn，召唤空投舱。
    /// </summary>
    public class Projectile_Stratagem : Projectile
    {
        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);

            Thing launcher = Launcher;
            if (launcher is ThingWithComps twc)
            {
                CompStratagemBeacon beacon = twc.TryGetComp<CompStratagemBeacon>();
                beacon?.CallIn(Position, Map);
            }
        }
    }
}