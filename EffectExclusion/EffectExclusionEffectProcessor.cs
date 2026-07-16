using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Player.Video.Effects;

namespace EffectExclusion
{
    internal sealed class EffectExclusionEffectProcessor(IGraphicsDevicesAndContext devices) : VideoEffectProcessorBase(devices)
    {
        public override DrawDescription Update(EffectDescription effectDescription)
            => effectDescription.DrawDescription;

        protected override ID2D1Image? CreateEffect(IGraphicsDevicesAndContext devices)
            => null;

        protected override void setInput(ID2D1Image? input)
        {
        }

        protected override void ClearEffectChain()
        {
        }
    }
}
