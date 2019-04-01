using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberDataWrappers
{
    public class ChangedProperties
    {
        public static readonly ChangedProperties AllButNoteCut = new ChangedProperties(true, true, true, false, true, false);
        public static readonly ChangedProperties Game = new ChangedProperties(true, false, false, false, false, false);
        public static readonly ChangedProperties Beatmap = new ChangedProperties(false, true, false, false, false, false);
        public static readonly ChangedProperties Performance = new ChangedProperties(false, false, true, false, false, false);
        public static readonly ChangedProperties PerformanceAndNoteCut = new ChangedProperties(false, false, true, true, false, false);
        public static readonly ChangedProperties Mod = new ChangedProperties(false, false, false, false, true, false);
        public static readonly ChangedProperties BeatmapEvent = new ChangedProperties(false, false, false, false, false, true);

        public readonly bool game;
        public readonly bool beatmap;
        public readonly bool performance;
        public readonly bool noteCut;
        public readonly bool mod;
        public readonly bool beatmapEvent;

        public ChangedProperties(bool game, bool beatmap, bool performance, bool noteCut, bool mod, bool beatmapEvent)
        {
            this.game = game;
            this.beatmap = beatmap;
            this.performance = performance;
            this.noteCut = noteCut;
            this.mod = mod;
            this.beatmapEvent = beatmapEvent;
        }
    }
}
