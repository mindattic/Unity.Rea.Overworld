using System;
using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>
    /// `Event` rectangles, evaluated (RECONSTRUCTION 9.4, U.OVW-T30; BIBLE "Events"): each tick, for each player's leader
    /// standing inside one of the playable world's event boxes (<see cref="EventRecord.X"/> + <see cref="EventRecord.Left"/>
    /// etc., as <see cref="WorldPlayability"/> already combines them), its script runs through
    /// <see cref="IScriptRunner"/> — once on entry, and again every <see cref="ScriptRecord.Rate"/> seconds while the
    /// leader stays inside (0: only the once). Standing in the 2007 database's three events: the Poison Box hurts
    /// (-1 HP every 0.5 s), the Healing Box heals (+1 HP every 0.5 s), and Entering Dark Woods turns the day to night
    /// once. The 2006 engine database's own three events (Test, Dark Woods, Cinematic) and scripts are not evaluated
    /// here: the compiled engine never read them either, and they stay a reference record (<see cref="ContentRegistry.ReferenceWorld"/>),
    /// not something this rebuild runs.
    /// </summary>
    public sealed class AreaEvents : ITickSystem
    {
        readonly WorldData world;
        readonly Party party;
        readonly IScriptRunner scripts;
        readonly Dictionary<(PlayerId, int), bool> inside = new Dictionary<(PlayerId, int), bool>();
        readonly Dictionary<(PlayerId, int), double> lastRun = new Dictionary<(PlayerId, int), double>();

        public AreaEvents(WorldData world, Party party, IScriptRunner scripts)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.party = party ?? throw new ArgumentNullException(nameof(party));
            this.scripts = scripts;
        }

        public AreaEvents Install(Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            game.AddSystem(this);
            return this;
        }

        /// <summary>Whether (x, y) falls within <paramref name="e"/>'s box, its origin added to its edges (0 for the 2006 database's own events, which have none).</summary>
        public static bool Contains(EventRecord e, float x, float y) =>
            x >= e.X + e.Left && x <= e.X + e.Right && y >= e.Y + e.Bottom && y <= e.Y + e.Top;

        public void Step(Game game, long tick, ICollection<IGameEvent> events)
        {
            if (scripts == null || game.IsHeld) return;
            var now = tick * Clock.TickSeconds;
            foreach (var player in game.Players)
            {
                var leader = party.Leader(player);
                if (leader == null) continue;
                foreach (var e in world.Events)
                {
                    var key = (player, e.Id);
                    var was = inside.TryGetValue(key, out var w) && w;
                    var isInside = Contains(e, leader.X, leader.Y);
                    inside[key] = isInside;
                    if (!isInside) continue;

                    var script = world.FindScript(e.ScriptId);
                    if (script == null) continue;
                    if (!was)
                    {
                        scripts.Run(game, player, leader.Index, script.Text, tick, events);
                        lastRun[key] = now;
                    }
                    else if (script.Rate > 0f && now + 1e-9 >= lastRun.GetValueOrDefault(key, double.NegativeInfinity) + script.Rate)
                    {
                        scripts.Run(game, player, leader.Index, script.Text, tick, events);
                        lastRun[key] = now;
                    }
                }
            }
        }
    }
}
