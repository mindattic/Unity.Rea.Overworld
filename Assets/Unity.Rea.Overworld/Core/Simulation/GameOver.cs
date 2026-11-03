using System;
using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>R (a frmMain key, like W or F7 / F8): forces the same reset as a completed fade, fade or no fade.</summary>
    public sealed class ResetGame : ICommand
    {
        public ResetGame(PlayerId player) { Player = player; }
        public PlayerId Player { get; }
    }

    /// <summary>Every tick a player's leader's health is 0 or below, the fade is one step closer to 0 (255 at full light).</summary>
    public sealed class GameOverFading : IGameEvent
    {
        public GameOverFading(long tick, PlayerId player, int fadeOut) { Tick = tick; Player = player; FadeOut = fadeOut; }
        public long Tick { get; }
        public PlayerId Player { get; }
        public int FadeOut { get; }
    }

    /// <summary>The fade reached 0, or <see cref="ResetGame"/> ran: Root.Reset happened.</summary>
    public sealed class GameReset : IGameEvent
    {
        public GameReset(long tick, PlayerId by) { Tick = tick; By = by; }
        public long Tick { get; }
        public PlayerId By { get; }
    }

    /// <summary>
    /// Lights.GameOver and Root.Reset (RECONSTRUCTION 6.5; BIBLE "Death and Reset", U.OVW-T28): each tick a player's
    /// leader's health is 0 or below, the screen fades toward black (<see cref="FadeStart"/> down to 0; presentation reads
    /// <see cref="FadeOutOf"/> for the ambient colour (fadeOut, fadeOut, fadeOut)). At 0, Reset runs: every player's menus
    /// shut, the FPS counter back on and the pause off, the tutorial re-queued (<see cref="InterfaceControl.Reset"/>), the
    /// game speed back to 1 and the day period Morning (the clock's date and time untouched,
    /// <see cref="WorldClock.ResetForGameOver"/>), every player's camera home at (0, 0, -1), every pickup returned
    /// (<see cref="Pickups.RestoreAllPickedUp"/>), the one shared pack emptied and every player's panel shut, and
    /// (replacing the original's defect of restoring nothing, so it called Reset every frame forever with the screen
    /// still black) every hero restored to full health, mana and stamina at its start position
    /// (<see cref="Party.RestoreAfterGameOver"/>). <see cref="ResetGame"/> (R) forces the same reset at once.
    /// </summary>
    public sealed class GameOver : ITickSystem
    {
        public const int FadeStart = 255;

        readonly Party party;
        readonly Pickups pickups;
        readonly InventoryControl inventory;
        readonly WorldClock clock;
        readonly CameraControl view;
        readonly MessageBoard messages;
        readonly InterfaceControl ui;

        readonly Dictionary<PlayerId, int> fadeOut = new Dictionary<PlayerId, int>();

        public GameOver(Party party, Pickups pickups = null, InventoryControl inventory = null, WorldClock clock = null,
            CameraControl view = null, MessageBoard messages = null, InterfaceControl ui = null)
        {
            this.party = party ?? throw new ArgumentNullException(nameof(party));
            this.pickups = pickups;
            this.inventory = inventory;
            this.clock = clock;
            this.view = view;
            this.messages = messages;
            this.ui = ui;
        }

        /// <summary>How far through the fade <paramref name="player"/> is; <see cref="FadeStart"/> while its leader is not at 0 health.</summary>
        public int FadeOutOf(PlayerId player) => fadeOut.TryGetValue(player, out var f) ? f : FadeStart;

        public GameOver Install(Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            game.Register<ResetGame>(HandleReset);
            game.AddSystem(this);
            return this;
        }

        public void Step(Game game, long tick, ICollection<IGameEvent> events)
        {
            foreach (var player in game.Players)
            {
                var leader = party.Leader(player);
                if (leader == null || leader.Health > 0)
                {
                    fadeOut[player] = FadeStart;
                    continue;
                }

                var f = FadeOutOf(player);
                if (f <= 0) continue; // Reset already ran and restored health; should not still be here
                f--;
                fadeOut[player] = f;
                events.Add(new GameOverFading(tick, player, f));
                if (f == 0) DoReset(game, player, tick, events);
            }
        }

        CommandResult HandleReset(Game game, ResetGame command)
        {
            var events = new List<IGameEvent>();
            DoReset(game, command.Player, game.Tick, events);
            return CommandResult.Accept(events.ToArray());
        }

        void DoReset(Game game, PlayerId triggeredBy, long tick, ICollection<IGameEvent> events)
        {
            ui?.Reset(messages);
            clock?.ResetForGameOver();
            pickups?.RestoreAllPickedUp();
            if (inventory != null)
            {
                inventory.ClearPack();
                foreach (var p in game.Players) inventory.SetOpen(p, false, tick, events);
            }

            if (view != null)
                foreach (var p in game.Players)
                    view.For(p)?.Camera?.MoveTo(0f, 0f, -1f);

            party.RestoreAfterGameOver(tick, events);
            foreach (var p in game.Players) fadeOut[p] = FadeStart;
            events.Add(new GameReset(tick, triggeredBy));
        }
    }
}
