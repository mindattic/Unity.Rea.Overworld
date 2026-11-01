using System;
using MindAttic.Overworld.Core;
using MindAttic.Overworld.Runtime;
using Unity.Multiplayer;
using UnityEngine;

namespace MindAttic.Overworld.Online
{
    /// <summary>
    /// One network peer's game (U.OVW-T46): the Core <see cref="Core.Game"/> its screen plays and the systems a
    /// <see cref="WorldSnapshot"/> is captured from (any may be null). On the server it is the one authority the online
    /// players' commands go to; on a client it is that screen's game, whose player commands are forwarded to the server
    /// (<see cref="Core.Game.Forward"/>) instead of changing it.
    /// </summary>
    public sealed class OverworldPeer
    {
        public OverworldPeer(Game game, Party party = null, WorldClock clock = null, Weather weather = null, GameHost host = null,
            CameraControl view = null, Clouds clouds = null, InterfaceControl interfaceControl = null)
        {
            Game = game ?? throw new ArgumentNullException(nameof(game));
            Party = party;
            Clock = clock;
            Weather = weather;
            Host = host;
            View = view;
            Clouds = clouds;
            Interface = interfaceControl;
        }

        public Game Game { get; }
        public Party Party { get; }
        public WorldClock Clock { get; }
        public Weather Weather { get; }

        /// <summary>What steps <see cref="Game"/> on this peer (null when nothing does).</summary>
        public GameHost Host { get; }

        /// <summary>The authority's camera and cursor per player, captured into each snapshot (U.OVW-T51; null: not captured).</summary>
        public CameraControl View { get; }

        /// <summary>A client's own screen's clouds, moved by its <see cref="WorldMirror"/> between snapshots (U.OVW-T51; null: not moved).</summary>
        public Clouds Clouds { get; }

        /// <summary>A client's own screen's interface, so its <see cref="WorldMirror"/> can mirror the authority's pause (U.OVW-T51; null: not shown).</summary>
        public InterfaceControl Interface { get; }

        /// <summary>The player this screen's own controls play as: the original's one player.</summary>
        public static PlayerId LocalPlayer => PlayerId.One;

        /// <summary>
        /// The player a roster seat plays as in the authority's game: seat 0 (the host's own, or a dedicated server's first
        /// player) is <see cref="PlayerId.One"/>, the scene's own player; seat n is player n + 1.
        /// </summary>
        public static PlayerId PlayerOf(int seat) => new PlayerId(seat + 1);

        /// <summary>The authority's game now, as it travels to the peers, with every player's camera and cursor (U.OVW-T51).</summary>
        public WorldSnapshot Capture(int sequence) => WorldSnapshot.Capture(Game, Party, Clock, Weather, View, sequence);
    }

    /// <summary>
    /// Which <see cref="OverworldPeer"/> a network peer's game is (U.OVW-T46). A game runs one peer per process, so the
    /// default is the scene's (the <see cref="OverworldBootstrap"/>'s game); a test that runs a server and clients in one
    /// process sets <see cref="Resolve"/> to give each peer its own (and <see cref="Reset"/> afterwards).
    /// </summary>
    public static class OverworldAuthority
    {
        /// <summary>The game of a peer's network session (null when it has none).</summary>
        public static Func<NetSession, OverworldPeer> Resolve { get; set; } = ScenePeer;

        /// <summary>Back to the scene's game.</summary>
        public static void Reset() => Resolve = ScenePeer;

        /// <summary>The game of <paramref name="session"/>'s peer.</summary>
        public static OverworldPeer For(NetSession session) => Resolve?.Invoke(session);

        static OverworldPeer cached;

        static OverworldPeer ScenePeer(NetSession session)
        {
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<OverworldBootstrap>();
            if (bootstrap == null || bootstrap.Host == null || bootstrap.Host.Game == null) return null;
            if (cached == null || cached.Game != bootstrap.Host.Game)
                cached = new OverworldPeer(bootstrap.Host.Game, bootstrap.Party, bootstrap.Clock, bootstrap.Weather, bootstrap.Host,
                    bootstrap.Control, bootstrap.Clouds, bootstrap.Interface);
            return cached;
        }
    }
}
