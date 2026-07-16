using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace EffectExclusion
{
    [VideoEffect(nameof(Texts.EffectExclusionEffectName), [VideoEffectCategories.Composition], [nameof(Texts.TagExclusion), nameof(Texts.TagGroupControl), nameof(Texts.TagEffectItem)], IsAviUtlSupported = false, ResourceType = typeof(Texts))]
    public sealed class EffectExclusionEffect : VideoEffectBase
    {
        static EffectExclusionEffect()
        {
            EffectExclusionPipeline.Initialize();
        }

        public EffectExclusionEffect()
        {
            EffectExclusionUpdateNotifier.EnsureCheckedOnce();
        }

        public override string Label => Texts.EffectExclusionEffectName;

        [Display(GroupName = nameof(Texts.EffectExclusionEffectName), Name = nameof(Texts.Targets), Description = nameof(Texts.TargetsDescription), ResourceType = typeof(Texts))]
        [TextEditor(AcceptsReturn = true)]
        public string Targets
        {
            get => _targets;
            set
            {
                if (Set(ref _targets, value))
                    _targetRemarks = null;
            }
        }
        private string _targets = string.Empty;

        private string[]? _targetRemarks;

        internal bool Matches(string? remark)
        {
            var targetRemarks = _targetRemarks ??= ParseTargets(_targets);
            if (targetRemarks.Length == 0)
                return true;
            var trimmedRemark = remark?.Trim();
            if (string.IsNullOrEmpty(trimmedRemark))
                return false;
            foreach (var targetRemark in targetRemarks)
            {
                if (string.Equals(targetRemark, trimmedRemark, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static string[] ParseTargets(string targets)
        {
            return targets
                .Split('\n')
                .Select(static x => x.Trim())
                .Where(static x => x.Length > 0)
                .ToArray();
        }

        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription) => [];

        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
            => new EffectExclusionEffectProcessor(devices);

        protected override IEnumerable<IAnimatable> GetAnimatables() => [];
    }
}
