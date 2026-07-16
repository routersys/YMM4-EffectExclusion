using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using YukkuriMovieMaker.Plugin.Effects;
using YukkuriMovieMaker.Project.Items;

namespace EffectExclusion.Tests;

public sealed class EffectExclusionPipelineTests
{
    private static readonly Assembly Ymm4Assembly = typeof(GroupItem).Assembly;

    [Theory]
    [InlineData("YukkuriMovieMaker.Player.Video.EffectedItemSource")]
    [InlineData("YukkuriMovieMaker.Player.Video.TimelineSource")]
    [InlineData("YukkuriMovieMaker.Player.Video.TimeSourceAndEffectPair")]
    public void Ymm4InternalTypes_Exist(string typeName)
    {
        Assert.NotNull(Ymm4Assembly.GetType(typeName));
    }

    [Fact]
    public void EffectedItemSource_HasPatchTargetMembers()
    {
        var type = Ymm4Assembly.GetType("YukkuriMovieMaker.Player.Video.EffectedItemSource")!;

        Assert.NotNull(type.GetField("item", BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.NotNull(type.GetProperty("ParentEffects", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(type.GetMethod("UpdateEffects", BindingFlags.NonPublic | BindingFlags.Instance));
    }

    [Fact]
    public void TimelineSource_HasPatchTargetMethod()
    {
        var type = Ymm4Assembly.GetType("YukkuriMovieMaker.Player.Video.TimelineSource")!;

        Assert.NotNull(type.GetMethod("GetOrderedTimelineResources", BindingFlags.NonPublic | BindingFlags.Instance));
    }

    [Fact]
    public void TimeSourceAndEffectPair_HasTimeSourceProperty()
    {
        var type = Ymm4Assembly.GetType("YukkuriMovieMaker.Player.Video.TimeSourceAndEffectPair")!;

        Assert.NotNull(type.GetProperty("TimeSource", BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void Initialize_AppliesHarmonyPatchesToPipeline()
    {
        EffectExclusionPipeline.Initialize();

        Assert.True(EffectExclusionPipeline.IsActive);
        var patchedMethods = Harmony.GetAllPatchedMethods().ToList();
        Assert.Contains(patchedMethods, static x => x.Name == "UpdateEffects" && x.DeclaringType?.FullName == "YukkuriMovieMaker.Player.Video.EffectedItemSource");
        Assert.Contains(patchedMethods, static x => x.Name == "GetOrderedTimelineResources" && x.DeclaringType?.FullName == "YukkuriMovieMaker.Player.Video.TimelineSource");
    }

    [Fact]
    public void IsExcluded_EnabledMatchingExclusion_ReturnsTrue()
    {
        var effects = Effects(new EffectExclusionEffect { Targets = "グループA" });

        Assert.True(EffectExclusionPipeline.IsExcluded(effects, "グループA"));
    }

    [Fact]
    public void IsExcluded_DisabledExclusion_ReturnsFalse()
    {
        var effects = Effects(new EffectExclusionEffect { Targets = "グループA", IsEnabled = false });

        Assert.False(EffectExclusionPipeline.IsExcluded(effects, "グループA"));
    }

    [Fact]
    public void IsExcluded_NonMatchingExclusion_ReturnsFalse()
    {
        var effects = Effects(new EffectExclusionEffect { Targets = "グループA" });

        Assert.False(EffectExclusionPipeline.IsExcluded(effects, "グループB"));
    }

    [Fact]
    public void IsExcluded_NoExclusionEffect_ReturnsFalse()
    {
        Assert.False(EffectExclusionPipeline.IsExcluded([], "グループA"));
    }

    [Fact]
    public void ComputeSortKeys_NothingExcluded_ReturnsNull()
    {
        var items = new IVideoItem?[]
        {
            CreateItem<GroupItem>("グループA"),
            CreateItem<EffectItem>("エフェクトA"),
        };

        Assert.Null(EffectExclusionPipeline.ComputeSortKeys(items));
    }

    [Fact]
    public void ComputeSortKeys_MovesExcludedItemAfterMatchingEffectItem()
    {
        var excluded = CreateItem<GroupItem>("", new EffectExclusionEffect());
        var effectItem = CreateItem<EffectItem>("エフェクトA");
        var items = new IVideoItem?[] { excluded, effectItem };

        var order = SortedOrder(items);

        Assert.Equal([effectItem, excluded], order);
    }

    [Fact]
    public void ComputeSortKeys_MovesExcludedItemDirectlyAfterTargetedEffectItem()
    {
        var excluded = CreateItem<GroupItem>("", new EffectExclusionEffect { Targets = "エフェクトA" });
        var targeted = CreateItem<EffectItem>("エフェクトA");
        var other = CreateItem<EffectItem>("エフェクトB");
        var items = new IVideoItem?[] { excluded, targeted, other };

        var order = SortedOrder(items);

        Assert.Equal([targeted, excluded, other], order);
    }

    [Fact]
    public void ComputeSortKeys_IgnoresEffectItemsAboveExcludedItem()
    {
        var targeted = CreateItem<EffectItem>("エフェクトA");
        var excluded = CreateItem<GroupItem>("", new EffectExclusionEffect { Targets = "エフェクトA" });
        var items = new IVideoItem?[] { targeted, excluded };

        Assert.Null(EffectExclusionPipeline.ComputeSortKeys(items));
    }

    [Fact]
    public void ComputeSortKeys_KeepsRelativeOrderOfExcludedItems()
    {
        var excluded1 = CreateItem<GroupItem>("", new EffectExclusionEffect());
        var excluded2 = CreateItem<GroupItem>("", new EffectExclusionEffect());
        var effectItem = CreateItem<EffectItem>("エフェクトA");
        var items = new IVideoItem?[] { excluded1, excluded2, effectItem };

        var order = SortedOrder(items);

        Assert.Equal([effectItem, excluded1, excluded2], order);
    }

    private static ImmutableList<IVideoEffect> Effects(params IVideoEffect[] effects)
        => [.. effects];

    private static T CreateItem<T>(string remark, params IVideoEffect[] effects) where T : VisualItem
    {
        var item = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        typeof(BaseItem).GetField("remark", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(item, remark);
        typeof(VisualItem).GetField("videoEffects", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(item, Effects(effects));
        return item;
    }

    private static IVideoItem?[] SortedOrder(IVideoItem?[] items)
    {
        var sortKeys = EffectExclusionPipeline.ComputeSortKeys(items);
        Assert.NotNull(sortKeys);
        var order = (IVideoItem?[])items.Clone();
        Array.Sort(sortKeys, order);
        return order;
    }
}
