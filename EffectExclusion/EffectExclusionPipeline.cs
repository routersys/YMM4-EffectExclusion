using System.Collections;
using System.Reflection;
using HarmonyLib;
using YukkuriMovieMaker.Plugin.Effects;
using YukkuriMovieMaker.Project.Items;

namespace EffectExclusion
{
    internal static class EffectExclusionPipeline
    {
        private const string HarmonyId = "EffectExclusion";

        private static int _initialized;
        private static FieldInfo? _itemField;
        private static PropertyInfo? _parentEffectsProperty;
        private static PropertyInfo? _timeSourceProperty;
        private static PropertyInfo? _entryKeyProperty;
        private static ConstructorInfo? _entryListConstructor;

        public static bool IsActive { get; private set; }

        public static void Initialize()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;
            try
            {
                IsActive = Apply();
            }
            catch
            {
                IsActive = false;
            }
        }

        private static bool Apply()
        {
            var assembly = typeof(GroupItem).Assembly;
            var effectedItemSourceType = assembly.GetType("YukkuriMovieMaker.Player.Video.EffectedItemSource");
            var timelineSourceType = assembly.GetType("YukkuriMovieMaker.Player.Video.TimelineSource");
            var pairType = assembly.GetType("YukkuriMovieMaker.Player.Video.TimeSourceAndEffectPair");
            if (effectedItemSourceType is null || timelineSourceType is null || pairType is null)
                return false;
            _itemField = effectedItemSourceType.GetField("item", BindingFlags.NonPublic | BindingFlags.Instance);
            _parentEffectsProperty = effectedItemSourceType.GetProperty("ParentEffects", BindingFlags.Public | BindingFlags.Instance);
            _timeSourceProperty = pairType.GetProperty("TimeSource", BindingFlags.Public | BindingFlags.Instance);
            var updateEffectsMethod = effectedItemSourceType.GetMethod("UpdateEffects", BindingFlags.NonPublic | BindingFlags.Instance);
            var orderedResourcesMethod = timelineSourceType.GetMethod("GetOrderedTimelineResources", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_itemField is null || _parentEffectsProperty is null || _timeSourceProperty is null || updateEffectsMethod is null || orderedResourcesMethod is null)
                return false;
            var entryType = typeof(KeyValuePair<,>).MakeGenericType(typeof(IVideoItem), effectedItemSourceType);
            _entryKeyProperty = entryType.GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
            _entryListConstructor = typeof(List<>).MakeGenericType(entryType).GetConstructor(Type.EmptyTypes);
            if (_entryKeyProperty is null || _entryListConstructor is null)
                return false;
            var harmony = new Harmony(HarmonyId);
            harmony.Patch(updateEffectsMethod, prefix: new HarmonyMethod(typeof(EffectExclusionPipeline), nameof(FilterParentEffects)));
            harmony.Patch(orderedResourcesMethod, postfix: new HarmonyMethod(typeof(EffectExclusionPipeline), nameof(ReorderTimelineResources)));
            return true;
        }

        private static void FilterParentEffects(object __instance)
        {
            var parentEffects = (IList?)_parentEffectsProperty!.GetValue(__instance);
            if (parentEffects is null || parentEffects.Count == 0)
                return;
            if (_itemField!.GetValue(__instance) is not IVideoItem item)
                return;
            var itemEffects = item.VideoEffects;
            if (itemEffects is null)
                return;
            for (var i = parentEffects.Count - 1; i >= 0; i--)
            {
                if (_timeSourceProperty!.GetValue(parentEffects[i]) is not GroupItem group)
                    continue;
                if (IsExcluded(itemEffects, group.Remark))
                    parentEffects.RemoveAt(i);
            }
        }

        private static void ReorderTimelineResources(ref object __result)
        {
            if (__result is not IEnumerable resultEntries)
                return;
            var entries = new List<object?>();
            foreach (var entry in resultEntries)
                entries.Add(entry);
            var count = entries.Count;
            if (count < 2)
                return;
            var items = new IVideoItem?[count];
            for (var i = 0; i < count; i++)
                items[i] = entries[i] is null ? null : _entryKeyProperty!.GetValue(entries[i]) as IVideoItem;
            var sortKeys = ComputeSortKeys(items);
            if (sortKeys is null)
                return;
            var orderedEntries = entries.ToArray();
            Array.Sort(sortKeys, orderedEntries);
            var reordered = (IList)_entryListConstructor!.Invoke(null);
            foreach (var entry in orderedEntries)
                reordered.Add(entry);
            __result = reordered;
        }

        internal static ulong[]? ComputeSortKeys(IReadOnlyList<IVideoItem?> items)
        {
            var count = items.Count;
            var needsReorder = false;
            var sortKeys = new ulong[count];
            for (var i = 0; i < count; i++)
            {
                sortKeys[i] = ((ulong)i << 33) | (uint)i;
                var itemEffects = items[i]?.VideoEffects;
                if (itemEffects is null)
                    continue;
                for (var j = count - 1; j > i; j--)
                {
                    if (items[j] is not EffectItem candidate)
                        continue;
                    if (!IsExcluded(itemEffects, candidate.Remark))
                        continue;
                    sortKeys[i] = ((ulong)j << 33) | (1uL << 32) | (uint)i;
                    needsReorder = true;
                    break;
                }
            }
            return needsReorder ? sortKeys : null;
        }

        internal static bool IsExcluded(IEnumerable<IVideoEffect> effects, string? remark)
        {
            foreach (var effect in effects)
            {
                if (effect is EffectExclusionEffect { IsEnabled: true } exclusion && exclusion.Matches(remark))
                    return true;
            }
            return false;
        }
    }
}
