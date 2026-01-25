using BeatSaverSharp;
using BeatSaverSharp.Models;
using IPA.Loader;
using SiraUtil.Logging;
using SiraUtil.Zenject;
using System;
using System.Threading;
using System.Threading.Tasks;
using DiTails.Utilities;
using OculusStudios.Platform.Core;
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
            if (level.TryGetHash(out var hash))
            {
                var beatmap = await _beatSaver.BeatmapByHash(hash, token);
                return beatmap ?? null;
            }

            return null;
        }

        internal async Task<Beatmap> Vote(Beatmap beatmap, bool upvote, CancellationToken token)
        {
            try
            {
                bool steam = false;

                if (_platform.vendor == Vendor.Valve)
                {
                    steam = true;
                }
                else if (_platform.vendor != Vendor.Meta)
                {
                    _siraLog.Debug("Current platform cannot vote.");
                    return beatmap;
                }

                var ticket = await _platform.user.GetAccessTokenAsync();

                _siraLog.Debug("Starting Vote...");
                if (steam)
                {
                    ticket = ticket.Replace("-", "");
                }

                var response = await beatmap.LatestVersion.Vote(upvote ? BeatSaverSharp.Models.Vote.Type.Upvote : BeatSaverSharp.Models.Vote.Type.Downvote,
                    steam ? BeatSaverSharp.Models.Vote.Platform.Steam : BeatSaverSharp.Models.Vote.Platform.Oculus,
                    _platform.user.userId.ToString(),
                    ticket, token);

                _siraLog.Info(response.Successful);
                _siraLog.Info(response.Error ?? "good");
                if (response.Successful)
                {
                    await beatmap.Refresh();
                }
                _siraLog.Debug($"Voted. Upvote? ({upvote})");
            }
            catch (Exception e)
            {
                _siraLog.Error(e.Message);
            }
            return beatmap;
        }
    }
}