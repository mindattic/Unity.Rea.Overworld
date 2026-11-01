using System;
using System.Collections.Generic;
using System.IO;

namespace MindAttic.Overworld.Core
{
    /// <summary>
    /// What the one authority's game looks like at one moment, as it travels to the online peers (U.OVW-T46): the tick,
    /// the in-game time and light, the weather, the pause, every hero (where it stands, its facing, mode, vitals and the
    /// pictures it shows) and every player in the game with the hero it leads and its view (camera and cursor). Captured
    /// from the authority's own systems (any may be null); a client presents it through a <see cref="WorldMirror"/>
    /// (U.OVW-T51). Nothing changes the authority's game with it.
    /// </summary>
    public sealed class WorldSnapshot
    {
        /// <summary>The wire version; bytes with any other first byte are refused (2: U.OVW-T51 adds the pictures, light and views).</summary>
        public const byte Version = 2;

        /// <summary>One hero as the authority has it.</summary>
        public readonly struct HeroShown
        {
            public HeroShown(float x, float y, float z, Facing facing, HeroMode mode, int health, int mana, int stamina,
                string sprite = null, string weaponSprite = null, HeroStatus status = default, string statusIcon = null)
            {
                X = x; Y = y; Z = z; Facing = facing; Mode = mode; Health = health; Mana = mana; Stamina = stamina;
                Sprite = sprite; WeaponSprite = weaponSprite; Status = status; StatusIcon = statusIcon;
            }

            public float X { get; }
            public float Y { get; }
            public float Z { get; }
            public Facing Facing { get; }
            public HeroMode Mode { get; }
            public int Health { get; }
            public int Mana { get; }
            public int Stamina { get; }

            /// <summary>The hero's picture now (its animation frame), null when the authority had none.</summary>
            public string Sprite { get; }

            /// <summary>The weapon's picture now, null when none.</summary>
            public string WeaponSprite { get; }

            public HeroStatus Status { get; }
            public string StatusIcon { get; }
        }

        /// <summary>One player in the authority's game, the hero it leads (-1 when there is no party) and its view.</summary>
        public readonly struct PlayerShown
        {
            public PlayerShown(PlayerId id, int leader) : this(id, leader, false, 0f, 0f, 0f, false, 0f, 0f) { }

            public PlayerShown(PlayerId id, int leader, bool hasView, float cameraX, float cameraY, float cameraZ, bool freeCamera,
                float cursorX, float cursorY)
            {
                Id = id; Leader = leader; HasView = hasView;
                CameraX = cameraX; CameraY = cameraY; CameraZ = cameraZ; FreeCamera = freeCamera;
                CursorX = cursorX; CursorY = cursorY;
            }

            public PlayerId Id { get; }
            public int Leader { get; }

            /// <summary>Whether the authority sent this player's view (it had a camera control).</summary>
            public bool HasView { get; }

            public float CameraX { get; }
            public float CameraY { get; }
            public float CameraZ { get; }
            public bool FreeCamera { get; }
            public float CursorX { get; }
            public float CursorY { get; }
        }

        readonly List<HeroShown> heroes = new List<HeroShown>();
        readonly List<PlayerShown> players = new List<PlayerShown>();

        /// <summary>Counts up with each snapshot the authority sends, so a peer keeps only the newest.</summary>
        public int Sequence { get; private set; }

        public long Tick { get; private set; }

        /// <summary>The in-game date and time (DateTime ticks; 0 when the authority has no clock).</summary>
        public long TimeTicks { get; private set; }

        /// <summary>The colour the world is lit in (white when the authority has no clock).</summary>
        public LightColour Light { get; private set; } = new LightColour(255, 255, 255);

        /// <summary>The weather now (Clear when the authority has none).</summary>
        public WeatherKind WeatherNow { get; private set; }

        /// <summary>Whether the authority's game is held (paused).</summary>
        public bool Held { get; private set; }

        public IReadOnlyList<HeroShown> Heroes => heroes;
        public IReadOnlyList<PlayerShown> Players => players;

        /// <summary>The player <paramref name="id"/>'s entry, when the snapshot has it.</summary>
        public bool TryGetPlayer(PlayerId id, out PlayerShown player)
        {
            foreach (var p in players)
                if (p.Id == id) { player = p; return true; }
            player = default;
            return false;
        }

        /// <summary>The authority's game now. Party, clock and weather are optional.</summary>
        public static WorldSnapshot Capture(Game game, Party party, WorldClock clock, Weather weather, int sequence) =>
            Capture(game, party, clock, weather, null, sequence);

        /// <summary>The authority's game now, with each player's view from <paramref name="view"/>. Everything but the game is optional.</summary>
        public static WorldSnapshot Capture(Game game, Party party, WorldClock clock, Weather weather, CameraControl view, int sequence)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            var s = new WorldSnapshot
            {
                Sequence = sequence,
                Tick = game.Tick,
                TimeTicks = clock != null ? clock.Now.Ticks : 0L,
                WeatherNow = weather != null ? weather.Kind : default,
                Held = game.IsHeld,
            };
            if (clock != null) s.Light = clock.Light;
            if (party != null)
                foreach (var h in party.Heroes)
                    s.heroes.Add(new HeroShown(h.X, h.Y, h.Z, h.Facing, h.Mode, h.Health, h.Mana, h.Stamina,
                        h.Sprite, h.WeaponSprite, h.Status, h.StatusIcon));
            var ids = new List<PlayerId>(game.Players);
            ids.Sort((a, b) => a.Value.CompareTo(b.Value));
            foreach (var id in ids)
            {
                var leader = party != null ? party.LeaderOf(id) : -1;
                if (view == null)
                {
                    s.players.Add(new PlayerShown(id, leader));
                    continue;
                }

                var v = view.For(id);
                s.players.Add(new PlayerShown(id, leader, true, v.Camera.X, v.Camera.Y, v.Camera.Z, v.Camera.FreeCamera,
                    v.Cursor.X, v.Cursor.Y));
            }

            return s;
        }

        public byte[] Encode()
        {
            using (var stream = new MemoryStream(64 + heroes.Count * 64 + players.Count * 32))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(Version);
                w.Write(Sequence);
                w.Write(Tick);
                w.Write(TimeTicks);
                w.Write(Light.R);
                w.Write(Light.G);
                w.Write(Light.B);
                w.Write((int)WeatherNow);
                w.Write(Held);
                w.Write(heroes.Count);
                foreach (var h in heroes)
                {
                    w.Write(h.X);
                    w.Write(h.Y);
                    w.Write(h.Z);
                    w.Write((int)h.Facing);
                    w.Write((int)h.Mode);
                    w.Write(h.Health);
                    w.Write(h.Mana);
                    w.Write(h.Stamina);
                    w.Write(h.Sprite ?? string.Empty);
                    w.Write(h.WeaponSprite ?? string.Empty);
                    w.Write((int)h.Status);
                    w.Write(h.StatusIcon ?? string.Empty);
                }

                w.Write(players.Count);
                foreach (var p in players)
                {
                    w.Write(p.Id.Value);
                    w.Write(p.Leader);
                    w.Write(p.HasView);
                    if (!p.HasView) continue;
                    w.Write(p.CameraX);
                    w.Write(p.CameraY);
                    w.Write(p.CameraZ);
                    w.Write(p.FreeCamera);
                    w.Write(p.CursorX);
                    w.Write(p.CursorY);
                }

                w.Flush();
                return stream.ToArray();
            }
        }

        /// <summary>The snapshot <paramref name="bytes"/> carry; null for bytes that are not one of this version.</summary>
        public static WorldSnapshot Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 1 || bytes[0] != Version) return null;
            try
            {
                using (var stream = new MemoryStream(bytes))
                using (var r = new BinaryReader(stream))
                {
                    r.ReadByte();
                    var s = new WorldSnapshot
                    {
                        Sequence = r.ReadInt32(),
                        Tick = r.ReadInt64(),
                        TimeTicks = r.ReadInt64(),
                        Light = new LightColour(r.ReadByte(), r.ReadByte(), r.ReadByte()),
                        WeatherNow = (WeatherKind)r.ReadInt32(),
                        Held = r.ReadBoolean(),
                    };
                    if (s.TimeTicks < DateTime.MinValue.Ticks || s.TimeTicks > DateTime.MaxValue.Ticks) return null;
                    var heroCount = r.ReadInt32();
                    if (heroCount < 0 || heroCount > 1024) return null;
                    for (var i = 0; i < heroCount; i++)
                        s.heroes.Add(new HeroShown(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), (Facing)r.ReadInt32(), (HeroMode)r.ReadInt32(),
                            r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), OrNull(r.ReadString()), OrNull(r.ReadString()),
                            (HeroStatus)r.ReadInt32(), OrNull(r.ReadString())));
                    var playerCount = r.ReadInt32();
                    if (playerCount < 0 || playerCount > 1024) return null;
                    for (var i = 0; i < playerCount; i++)
                    {
                        var id = new PlayerId(r.ReadInt32());
                        var leader = r.ReadInt32();
                        if (!r.ReadBoolean())
                        {
                            s.players.Add(new PlayerShown(id, leader));
                            continue;
                        }

                        s.players.Add(new PlayerShown(id, leader, true, r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadBoolean(),
                            r.ReadSingle(), r.ReadSingle()));
                    }

                    return stream.Position == stream.Length ? s : null;
                }
            }
            catch (EndOfStreamException) { return null; }
            catch (IOException) { return null; }
            catch (FormatException) { return null; }
        }

        static string OrNull(string value) => string.IsNullOrEmpty(value) ? null : value;
    }
}
