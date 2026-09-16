using RimWorld;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// 哨戒炮的发射动词。每次真正发射一发，就从炮塔上的 CompSentryLifespan 扣除一发内部弹药，
    /// 弹尽后由 compt 触发自毁分解。caster 在炮塔语境下就是炮塔建筑本身。
    /// </summary>
    public class Verb_SentryMortar : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            bool fired = base.TryCastShot();
            if (fired && caster is ThingWithComps twc)
            {
                twc.TryGetComp<CompSentryLifespan>()?.ShotFired();
            }
            return fired;
        }
    }
}