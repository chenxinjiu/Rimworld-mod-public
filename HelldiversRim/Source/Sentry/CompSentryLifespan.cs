using RimWorld;
using Verse;

namespace HelldiversRim
{
    /// <summary>
    /// 一次性哨戒炮的寿命管理配置。XML 里通过
    /// &lt;comp Class="HelldiversRim.CompProperties_SentryLifespan"&gt; 指定：
    /// shots           初始内部弹药（每发一炮减一）
    /// explodeRadius   打空自毁的爆炸半径
    /// explodeDamage   自毁爆炸的基础伤害
    /// scrapDef        分解出的废料（用哪个 ThingDef，例如 Steel）
    /// scrapCount      废料数量
    /// </summary>
    public class CompProperties_SentryLifespan : CompProperties
    {
        public int shots = 18;
        public float explodeRadius = 2.8f;
        public int explodeDamage = 30;
        public ThingDef scrapDef;
        public int scrapCount = 60;

        public CompProperties_SentryLifespan()
        {
            compClass = typeof(CompSentryLifespan);
        }
    }

    /// <summary>
    /// 附着在哨戒炮塔上，配合 Verb_SentryMortar 使用：
    /// 每开一炮扣一发内部弹药，弹尽后自毁（爆炸）并分解为废料。
    /// 全部为实例状态（无跨 tick 静态缓存），对 RimThreaded / 多线程性能优化 mod 友好。
    /// </summary>
    public class CompSentryLifespan : ThingComp
    {
        public CompProperties_SentryLifespan Props => (CompProperties_SentryLifespan)props;

        private int shellsRemaining = -1;
        private bool done;

        /// <summary>由 Verb_SentryMortar 每发射一发调用（仅非 CE 路径会走到这里）。</summary>
        public void ShotFired()
        {
            if (done)
                return;

            if (shellsRemaining < 0)
                shellsRemaining = Props.shots;

            shellsRemaining--;
            if (shellsRemaining <= 0)
                SelfDestruct();
        }

        /// <summary>
        /// CE 路径：CE 下开火由 CE 弹药系统驱动，射击动词不会触发，因此改为轮询
        /// CE 弹药余量，弹尽即自毁。非 CE 时此分支直接跳过。
        /// </summary>
        public override void CompTick()
        {
            if (done || !CECompat.IsActive)
                return;

            // 节流：每隔 ~0.25s 才查一次，避免每 tick 反射。
            if ((Find.TickManager.TicksGame & 15) != 0)
                return;

            int? ammo = CECompat.AmmoRemaining(parent);
            if (ammo.HasValue && ammo.Value <= 0)
                SelfDestruct();
        }

        private void SelfDestruct()
        {
            done = true;

            if (parent.Map != null)
            {
                GenExplosion.DoExplosion(
                    parent.Position,
                    parent.Map,
                    Props.explodeRadius,
                    DamageDefOf.Bomb,
                    null,
                    Props.explodeDamage);

                if (Props.scrapDef != null)
                {
                    Thing scrap = ThingMaker.MakeThing(Props.scrapDef);
                    scrap.stackCount = Props.scrapCount;
                    GenSpawn.Spawn(scrap, parent.Position, parent.Map);
                }
            }

            parent.Destroy(DestroyMode.KillFinalize);
        }
    }
}