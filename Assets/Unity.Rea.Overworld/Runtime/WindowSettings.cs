using Unity.Shared.Core;
using Unity.Shared.Core.Json;
using UnityEngine;

namespace MindAttic.Overworld.Runtime
{
    /// <summary>
    /// Root.loadOptions / saveOptions / setRegKey (RECONSTRUCTION 2.2): the window's width, height, colour depth and
    /// windowed / full-screen mode, kept between runs. The original kept them at <c>HKCU\Software\MindAttic\Overhead</c>;
    /// the rebuild keeps them the way every MindAttic game does, a MindAttic profile's settings.json (BIBLE "Settings"):
    /// <c>Profiles.Current.Settings.Game["window"]</c>, saved with <see cref="Unity.Shared.Core.Profile.SaveSettings"/>.
    /// Windowed is always the form's fixed 640 x 480 client size, as the original's <c>MinimumSize = MaximumSize</c> did;
    /// full screen uses the stored width and height. Depth (16 or 32) is kept and validated for fidelity; modern GPUs
    /// render 32-bit regardless of what is asked for, so unlike the original (an exception box for anything else) an
    /// invalid value is just corrected to 32, and depth otherwise has no visible effect (BIBLE). This is infrastructure,
    /// not gameplay (<see cref="Runtime.SessionAudio"/>'s precedent): no Core command or event, nothing a test can see change.
    /// </summary>
    public sealed class WindowSettings
    {
        public const string SectionKey = "window";
        public const string WidthKey = "width";
        public const string HeightKey = "height";
        public const string DepthKey = "depth";
        public const string WindowedKey = "windowed";

        public const int DefaultWidth = 640;
        public const int DefaultHeight = 480;
        public const int DefaultDepth = 32;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Depth { get; private set; }
        public bool Windowed { get; private set; }

        /// <summary>True for the one run that found no saved "window" section: the first-run welcome is shown.</summary>
        public bool FirstRun { get; private set; }

        /// <summary>Reads the current profile's saved settings, or the defaults (640 x 480, depth 32, windowed) saved for the first time.</summary>
        public static WindowSettings Load()
        {
            var profile = Profiles.Current;
            var game = profile.Settings.Game;
            var settings = new WindowSettings { FirstRun = !game.ContainsKey(SectionKey) };
            if (settings.FirstRun)
            {
                settings.Width = DefaultWidth;
                settings.Height = DefaultHeight;
                settings.Depth = DefaultDepth;
                settings.Windowed = true;
                settings.Save();
            }
            else
            {
                var window = game.GetObject(SectionKey) ?? new JsonObject();
                settings.Width = Mathf.Max(1, window.GetInt(WidthKey, DefaultWidth));
                settings.Height = Mathf.Max(1, window.GetInt(HeightKey, DefaultHeight));
                var depth = window.GetInt(DepthKey, DefaultDepth);
                settings.Depth = depth == 16 ? 16 : DefaultDepth;
                settings.Windowed = window.GetBool(WindowedKey, true);
            }

            return settings;
        }

        void Save()
        {
            var profile = Profiles.Current;
            var window = profile.Settings.Game.GetOrAddObject(SectionKey);
            window.Set(WidthKey, Width);
            window.Set(HeightKey, Height);
            window.Set(DepthKey, Depth);
            window.Set(WindowedKey, Windowed);
            profile.SaveSettings();
        }

        /// <summary>Applies the stored mode: windowed at the fixed 640 x 480, or full screen at the stored size.</summary>
        public void Apply()
        {
            if (Windowed) Screen.SetResolution(DefaultWidth, DefaultHeight, FullScreenMode.Windowed);
            else Screen.SetResolution(Width, Height, FullScreenMode.FullScreenWindow);
        }

        /// <summary>
        /// F12 (RECONSTRUCTION 2.2): flips windowed / full screen in place and saves it. Unlike the original, which
        /// stopped the loop and restarted the whole engine, this never reloads the scene, so the game is never lost
        /// (U.OVW-T27, the ticket's own requirement).
        /// </summary>
        public void ToggleFullScreen()
        {
            Windowed = !Windowed;
            Save();
            Apply();
        }
    }
}
