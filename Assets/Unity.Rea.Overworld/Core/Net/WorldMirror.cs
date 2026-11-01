using System;

namespace MindAttic.Overworld.Core
{
    /// <summary>
    /// An online client's screen drawing the shared world (U.OVW-T51). A client's own game is frozen while it is remote
    /// (<see cref="Game.IsRemote"/>: its host steps nothing and its player commands go to the authority), so what its
    /// presentation reads is written here from the authority's newest <see cref="WorldSnapshot"/>: every hero's place,
    /// facing, mode, vitals and pictures, the time and light, the weather, the pause, the hero this screen's player leads
    /// and that player's camera and cursor. The snapshot's player <c>shownAs</c> (the client's seat in the authority's
    /// game) is drawn as this screen's own player, so the HUD and camera read exactly as they do in local play.
    /// <para>Between snapshots <see cref="Animate"/> moves the rain and clouds at the 60 Hz tick (they are scenery the
    /// snapshot does not carry, and the original moved them every frame it was not paused). Nothing here touches the
    /// authority's game: it only shows it on this screen.</para>
    /// </summary>
    public sealed class WorldMirror
    {
        readonly FixedStepper stepper = new FixedStepper();

        /// <param name="party">The screen's party (required).</param>
        /// <param name="clock">The screen's clock (null: none shown).</param>
        /// <param name="weather">The screen's weather (null: none shown).</param>
        /// <param name="clouds">The screen's clouds (null: none moved).</param>
        /// <param name="view">The screen's camera control (null: the view is not shown).</param>
        /// <param name="interfaceControl">The screen's interface (null: the pause is not shown).</param>
        public WorldMirror(Party party, WorldClock clock = null, Weather weather = null, Clouds clouds = null, CameraControl view = null,
            InterfaceControl interfaceControl = null)
        {
            Party = party ?? throw new ArgumentNullException(nameof(party));
            Clock = clock;
            Weather = weather;
            Clouds = clouds;
            View = view;
            Interface = interfaceControl;
        }

        public Party Party { get; }
        public WorldClock Clock { get; }
        public Weather Weather { get; }
        public Clouds Clouds { get; }
        public CameraControl View { get; }
        public InterfaceControl Interface { get; }

        /// <summary>The snapshot shown now (null before the first).</summary>
        public WorldSnapshot Shown { get; private set; }

        /// <summary>Snapshots presented.</summary>
        public int Presented { get; private set; }

        /// <summary>Whether the world shown is paused (the authority's game is held).</summary>
        public bool Held => Shown != null && Shown.Held;

        /// <summary>
        /// Shows <paramref name="snapshot"/> on this screen: the authority's player <paramref name="shownAs"/> is drawn as
        /// the screen's own <paramref name="screen"/> player (its leader and view). An older snapshot than the one shown is ignored.
        /// </summary>
        /// <returns>Whether it was shown.</returns>
        public bool Present(WorldSnapshot snapshot, PlayerId shownAs, PlayerId screen)
        {
            if (snapshot == null) return false;
            if (Shown != null && snapshot.Sequence <= Shown.Sequence) return false;

            var heroes = Party.Heroes;
            var count = Math.Min(heroes.Count, snapshot.Heroes.Count);
            for (var i = 0; i < count; i++)
            {
                var h = heroes[i];
                var s = snapshot.Heroes[i];
                // A snapshot is a whole state, not a step: drawn there at once (nothing to ease from).
                h.X = h.PreviousX = s.X;
                h.Y = h.PreviousY = s.Y;
                h.Z = h.PreviousZ = s.Z;
                h.Facing = s.Facing;
                h.Mode = s.Mode;
                h.Health = s.Health;
                h.Mana = s.Mana;
                h.Stamina = s.Stamina;
                h.Status = s.Status;
                h.StatusIcon = s.StatusIcon;
                if (s.Sprite != null) h.Sprite = s.Sprite;
                h.WeaponSprite = s.WeaponSprite;
            }

            if (Clock != null && snapshot.TimeTicks > 0L) Clock.Mirror(new DateTime(snapshot.TimeTicks), snapshot.Light);
            Weather?.Mirror(snapshot.WeatherNow);
            Interface?.MirrorPaused(snapshot.Held);

            if (!shownAs.IsNone && !screen.IsNone && snapshot.TryGetPlayer(shownAs, out var player))
            {
                Party.MirrorLeader(screen, player.Leader);
                if (View != null && player.HasView)
                {
                    var v = View.For(screen);
                    v.Camera.FreeCamera = player.FreeCamera;
                    v.Camera.MoveTo(player.CameraX, player.CameraY, player.CameraZ);
                    v.Cursor.X = player.CursorX;
                    v.Cursor.Y = player.CursorY;
                    v.Cursor.Settle();
                }
            }

            Shown = snapshot;
            Presented++;
            return true;
        }

        /// <summary>
        /// Real time passing on the screen between snapshots: the rain and clouds move once per 60 Hz tick it adds up to,
        /// unless the world shown is paused. Returns the ticks run.
        /// </summary>
        public int Animate(double elapsedSeconds)
        {
            var ticks = stepper.Advance(elapsedSeconds);
            if (Held) return ticks;
            for (var i = 0; i < ticks; i++)
            {
                Weather?.Step();
                Clouds?.Step();
            }

            return ticks;
        }
    }
}
