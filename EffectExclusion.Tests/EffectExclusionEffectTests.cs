namespace EffectExclusion.Tests;

public sealed class EffectExclusionEffectTests
{
    [Fact]
    public void Targets_DefaultsToEmpty()
    {
        var effect = new EffectExclusionEffect();

        Assert.Equal(string.Empty, effect.Targets);
    }

    [Fact]
    public void IsEnabled_DefaultsToTrue()
    {
        var effect = new EffectExclusionEffect();

        Assert.True(effect.IsEnabled);
    }

    [Fact]
    public void CreateExoVideoFilters_ReturnsEmpty()
    {
        var effect = new EffectExclusionEffect();

        Assert.Empty(effect.CreateExoVideoFilters(0, null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("グループA")]
    public void Matches_EmptyTargets_MatchesAnyRemark(string? remark)
    {
        var effect = new EffectExclusionEffect();

        Assert.True(effect.Matches(remark));
    }

    [Fact]
    public void Matches_WhitespaceOnlyTargets_MatchesAnyRemark()
    {
        var effect = new EffectExclusionEffect { Targets = " \r\n\r\n  " };

        Assert.True(effect.Matches("グループA"));
    }

    [Fact]
    public void Matches_ExactRemark_ReturnsTrue()
    {
        var effect = new EffectExclusionEffect { Targets = "グループA" };

        Assert.True(effect.Matches("グループA"));
    }

    [Fact]
    public void Matches_DifferentRemark_ReturnsFalse()
    {
        var effect = new EffectExclusionEffect { Targets = "グループA" };

        Assert.False(effect.Matches("グループB"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Matches_TargetsSpecifiedButRemarkEmpty_ReturnsFalse(string? remark)
    {
        var effect = new EffectExclusionEffect { Targets = "グループA" };

        Assert.False(effect.Matches(remark));
    }

    [Fact]
    public void Matches_TrimsTargetLinesAndRemark()
    {
        var effect = new EffectExclusionEffect { Targets = "  グループA \r\n" };

        Assert.True(effect.Matches(" グループA\r\n"));
    }

    [Fact]
    public void Matches_MultipleLines_MatchesEachEntry()
    {
        var effect = new EffectExclusionEffect { Targets = "グループA\r\n\r\nエフェクトB" };

        Assert.True(effect.Matches("グループA"));
        Assert.True(effect.Matches("エフェクトB"));
        Assert.False(effect.Matches("グループC"));
    }

    [Fact]
    public void Matches_ReflectsUpdatedTargets()
    {
        var effect = new EffectExclusionEffect { Targets = "グループA" };
        Assert.True(effect.Matches("グループA"));

        effect.Targets = "グループB";

        Assert.False(effect.Matches("グループA"));
        Assert.True(effect.Matches("グループB"));
    }
}
