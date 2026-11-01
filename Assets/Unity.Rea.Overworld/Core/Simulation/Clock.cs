using System;

namespace MindAttic.Overworld.Core
{
    /// <summary>
    /// The fixed simulation step. The original moved things a fixed amount per rendered frame, so the rebuild
    /// steps at 60 Hz on every machine and keeps those per-frame constants numerically as per-tick values.
    /// </summary>
    public static class Clock
    {
        public const int TicksPerSecond = 60;
        public const double TickSeconds = 1.0 / TicksPerSecond;

        /// <summary>A per-tick speed expressed per second (what a per-frame figure was on a 60 Hz display).</summary>
        public static float PerSecond(float perTick) => perTick * TicksPerSecond;

        /// <summary>A per-second speed expressed per tick.</summary>
        public static float PerTick(float perSecond) => perSecond / TicksPerSecond;
    }

    /// <summary>
    /// Turns elapsed real time into whole 60 Hz ticks with its own accumulator, independent of any engine
    /// fixed update. Leftover time carries to the next call; a long stall is capped so the game never
    /// spirals trying to catch up.
    /// </summary>
    public sealed class FixedStepper
    {
        public const int DefaultMaxTicksPerAdvance = 10;

        readonly int maxTicks;
        double accumulator;

        public FixedStepper(int maxTicksPerAdvance = DefaultMaxTicksPerAdvance)
        {
            if (maxTicksPerAdvance <= 0) throw new ArgumentOutOfRangeException(nameof(maxTicksPerAdvance));
            maxTicks = maxTicksPerAdvance;
        }

        /// <summary>How far through the next tick we are, 0 up to (not including) 1, for presentation to interpolate.</summary>
        public float Alpha => (float)(accumulator / Clock.TickSeconds);

        /// <summary>Adds elapsed seconds and returns how many ticks to run now.</summary>
        public int Advance(double elapsedSeconds) => Advance(elapsedSeconds, uncapped: false);

        /// <summary>
        /// Adds elapsed seconds and returns how many ticks to run now. <paramref name="uncapped"/> skips the stall
        /// cap (U.OVW-T57): while the game is held, a tick only runs the light work that "went on in the draw"
        /// (BIBLE Pause row) and the original never throttled oneFrameRender at all (RECONSTRUCTION 2.4), so a long
        /// pause has nothing to spiral and should still land on the real tick, not be dropped like an unpaused stall.
        /// </summary>
        public int Advance(double elapsedSeconds, bool uncapped)
        {
            if (double.IsNaN(elapsedSeconds) || elapsedSeconds < 0) elapsedSeconds = 0;
            accumulator += elapsedSeconds;
            // The epsilon absorbs binary rounding so sixty 1/60 s frames are exactly sixty ticks.
            int ticks = (int)(accumulator / Clock.TickSeconds + 1e-9);
            if (!uncapped && ticks > maxTicks)
            {
                ticks = maxTicks;
                accumulator = 0; // dropped the backlog
            }
            else
            {
                accumulator -= ticks * Clock.TickSeconds;
                if (accumulator < 0) accumulator = 0;
            }
            return ticks;
        }

        public void Reset() { accumulator = 0; }
    }
}
