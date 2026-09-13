using BeatSaverSharp;
using BeatSaverSharp.Models;
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
        private readonly IPlatform _platformUserModel;

        internal LevelDataService(SiraLog siraLog, IPlatform platformUserModel, UBinder<Plugin, PluginMetadata> metadataBinder)
        {
            _siraLog = siraLog;
            _platformUserModel = platformUserModel;
            _beatSaver = new BeatSaver("DiTails", Version.Parse(metadataBinder.Value.HVersion.ToString()));
        }

        public void LateDispose()
        {
            _beatSaver.Clear();
            _beatSaver.Dispose();
        }

        internal async Task<Beatmap?> GetBeatmap(BeatmapLevel difficultyBeatmap, CancellationToken token)
        {
            if (!difficultyBeatmap.levelID.Contains("custom_level_"))
            {
                return null;
            }
            var hash = difficultyBeatmap.levelID.Replace("custom_level_", "");
            var beatmap = await _beatSaver.BeatmapByHash(hash, token);
            return beatmap ?? null;
        }

        internal async Task<Beatmap> Vote(Beatmap beatmap, bool upvote, CancellationToken token)
        {
            try
            {
                bool steam = _platformUserModel.vendor == Vendor.Valve;
                if (!steam && _platformUserModel.vendor != Vendor.Meta)
                    return beatmap;

                _siraLog.Debug("Beat Saver voting requires the removed legacy platform auth-token API.");
                return beatmap;
            }
            catch (Exception e)
            {
                _siraLog.Error(e.Message);
            }
            return beatmap;
        }
    }
}
