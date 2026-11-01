using UnityEngine;

namespace MindAttic.Overworld.Runtime
{
    /// <summary>
    /// Root.loadOptions / saveOptions / setRegKey (RECONSTRUCTION 2.2): the window's width, height, colour depth and
    /// windowed / full-screen mode, kept between runs. The original kept them at <c>HKCU\Software\MindAttic\Overhead</c>;
    /// Unity's <see cref="PlayerPrefs"/> is the same idea (on Windows standalone it is itself the registry, under
    /// <c>HKCU\Software\&lt;company&gt;\&lt;product&gt;</c>). Windowed is always the form's fixed 640 x 480 client size, as
    /// the original's <c>MinimumSize = MaximumSize</c> did; full screen uses the stored width and height. Depth (16 or 32)
    /// is kept and validated for fidelity; modern GPUs render 32-bit regardless of what is asked for, so unlike the
    /// original (an exception box for anything else) an invalid value is just corrected to 32, and depth otherwise has no
    /// visible effect (BIBLE). This is infrastructure, not gameplay (<see cref="Runtime.SessionAudio"/>'s precedent): no
    /// Core command or event, nothing a test can see change.
    /// </summary>
    public sealed class WindowSettings
    {
        public const string WidthKey = "Width";
        public const string HeightKey = "Height";
        public const string DepthKey = "Depth";
        public const string WindowModeKey = "isWindowMode";

        public const int DefaultWidth = 640;
        public const int DefaultHeight = 480;
        public const int DefaultDepth = 32;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Depth { get; private set; }
        public bool Windowed { get; private set; }

        /// <summary>True for the one run that found no saved key (every one missing counts, as a partly-written key would not have come from <see cref="Save"/>): the first-run welcome is shown.</summary>
        public bool FirstRun { get; private set; }

        /// <summary>Reads the saved settings, or the defaults (640 x 480, depth 32, windowed) saved for the first time.</summary>
        public static WindowSettings Load()
        {
            var settings = new WindowSettings
            {
                FirstRun = !PlayerPrefs.HasKey(WidthKey) || !PlayerPrefs.HasKey(HeightKey) ||
                           !PlayerPrefs.HasKey(DepthKey) || !PlayerPrefs.HasKey(WindowModeKey),
            };
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
                settings.Width = Mathf.Max(1, PlayerPrefs.GetInt(WidthKey, DefaultWidth));
                settings.Height = Mathf.Max(1, PlayerPrefs.GetInt(HeightKey, DefaultHeight));
                var depth = PlayerPrefs.GetInt(DepthKey, DefaultDepth);
                settings.Depth = depth == 16 ? 16 : DefaultDepth;
                settings.Windowed = PlayerPrefs.GetInt(WindowModeKey, 1) != 0;
            }

            return settings;
        }

        void Save()
        {
            PlayerPrefs.SetInt(WidthKey, Width);
            PlayerPrefs.SetInt(HeightKey, Height);
            PlayerPrefs.SetInt(DepthKey, Depth);
            PlayerPrefs.SetInt(WindowModeKey, Windowed ? 1 : 0);
            PlayerPrefs.Save();
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
