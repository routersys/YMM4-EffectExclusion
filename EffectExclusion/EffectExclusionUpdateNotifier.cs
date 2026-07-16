using System.Net.Http;
using System.Windows;
using GitHubUpdateNotifier;
using Newtonsoft.Json.Linq;
using YukkuriMovieMaker.Plugin.Update;
using YukkuriMovieMaker.Project.Items;
using Notifier = GitHubUpdateNotifier.GitHubUpdateNotifier;

namespace EffectExclusion
{
    internal static class EffectExclusionUpdateNotifier
    {
        private const string Owner = "routersys";
        private const string Repository = "YMM4-EffectExclusion";
        private const string CompatibilityUrl = "https://raw.githubusercontent.com/routersys/YMM4-EffectExclusion/main/.github/compatibility.json";

        private static int _started;

        public static void EnsureCheckedOnce()
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
                return;

            _ = RunAsync();
        }

        private static async Task RunAsync()
        {
            try
            {
                var options = new UpdateNotifierOptions
                {
                    CurrentVersion = PluginVersion.FromAssemblyInformationalVersion(typeof(EffectExclusionUpdateNotifier)),
                    IncludePrerelease = false,
                    MessageFormatter = FormatMessage,
                    NotificationHandler = ShowNotificationAsync,
                };

                await new Notifier(Owner, Repository, options).NotifyAsync().ConfigureAwait(false);
            }
            catch
            {
            }
            try
            {
                await NotifyUnsupportedYmm4Async().ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private static async Task NotifyUnsupportedYmm4Async()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(Repository);
            var json = await client.GetStringAsync(CompatibilityUrl).ConfigureAwait(false);
            var compatibility = JObject.Parse(json);
            if (compatibility.Value<bool>("isLatestSupported"))
                return;
            if (!Version.TryParse(compatibility.Value<string>("lastGoodYmm4"), out var lastGoodVersion))
                return;
            var currentVersion = typeof(GroupItem).Assembly.GetName().Version;
            if (currentVersion is null || currentVersion <= lastGoodVersion)
                return;
            var message = string.Format(Texts.UnsupportedYmm4Message, currentVersion, lastGoodVersion);
            await ShowMessageBoxAsync(message, MessageBoxImage.Warning).ConfigureAwait(false);
        }

        private static string FormatMessage(UpdateInfo update)
            => string.Format(Texts.UpdateAvailableMessage, update.TagName) + Environment.NewLine + update.ReleaseUrl;

        private static Task ShowNotificationAsync(UpdateNotification notification, CancellationToken cancellationToken)
            => ShowMessageBoxAsync(notification.Message, MessageBoxImage.Information);

        private static Task ShowMessageBoxAsync(string message, MessageBoxImage image)
        {
            var application = Application.Current;
            if (application is null)
                return Task.CompletedTask;

            return application.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(message, Texts.EffectExclusionEffectName, MessageBoxButton.OK, image)).Task;
        }
    }
}
