using BeatSaverSharp;
using BeatSaverSharp.Models;
using DiTails.Utilities;
using IPA.Loader;
using OculusStudios.Platform.Core;
using SiraUtil.Logging;
using SiraUtil.Zenject;
using System;
using System.Threading;
using System.Threading.Tasks;
using Zenject;

namespace DiTails
{
    internal class LevelDataService : ILateDisposable
    {
        private readonly SiraLog _siraLog;
        private readonly BeatSaver _beatSaver;
        private readonly IPlatform _platform;

        internal LevelDataService(SiraLog siraLog, IPlatform platform, UBinder<Plugin, PluginMetadata> metadataBinder)
        {
            _siraLog = siraLog;
            _platform = platform;
            _beatSaver = new BeatSaver("DiTails", Version.Parse(metadataBinder.Value.HVersion.ToString()));
        }

        public void LateDispose()
        {
            _beatSaver.Clear();
            _beatSaver.Dispose();
        }

        internal async Task<Beatmap?> GetBeatmap(BeatmapLevel level, CancellationToken token)
        {
            if (!level.TryGetHash(out var hash))
            {
                return null;
            }
            return await _beatSaver.BeatmapByHash(hash, token);
        }

        internal async Task<Beatmap> Vote(Beatmap beatmap, bool upvote, CancellationToken token)
        {
            try
            {
                var steam = _platform.vendor == Vendor.Valve;
                if (!steam && _platform.vendor != Vendor.Meta)
                    return beatmap;

                var user = _platform.user;
                if (user == null || user.userId == 0) return beatmap;

                var ticket = await user.GetAccessTokenAsync();
                if (steam) ticket = ticket.Replace("-", "");

                var response = await beatmap.LatestVersion.Vote(
                    upvote ? BeatSaverSharp.Models.Vote.Type.Upvote : BeatSaverSharp.Models.Vote.Type.Downvote,
                    steam ? BeatSaverSharp.Models.Vote.Platform.Steam : BeatSaverSharp.Models.Vote.Platform.Oculus,
                    user.userId.ToString(), ticket, token);

                _siraLog.Info(response.Successful);
                _siraLog.Info(response.Error ?? "good");
                if (response.Successful) await beatmap.Refresh(token);
            }
            catch (Exception e)
            {
                _siraLog.Error(e.Message);
            }
            return beatmap;
        }
    }
}
