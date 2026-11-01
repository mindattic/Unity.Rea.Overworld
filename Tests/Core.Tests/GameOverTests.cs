using System;
using System.Collections.Generic;
using System.IO;
using MindAttic.Overworld.Core;
using NUnit.Framework;

namespace MindAttic.Overworld.Tests
{
    /// <summary>
    /// U.OVW-US-A24 (RECONSTRUCTION 6.5; BIBLE "Death and Reset"): the leader's health reaching 0 fades the screen to
    /// black over 255 ticks, then resets the game (pickups returned, pack emptied and shut, every menu shut, the FPS
    /// counter back on and the pause off, the tutorial re-queued, the game speed back to 1, the day period Morning, the
    /// camera home), and, replacing the original's never-restored-health defect, restores every hero to full vitals at
    /// its start position. R forces the same reset at once.
    /// </summary>
    public class GameOverTests
    {
        const int HealthRoot = 0;

        static string ContentDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Content");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Assets/StreamingAssets/Content");
        }

        static readonly ContentRegistry Registry = ContentRegistry.Load(new DirectoryContentSource(ContentDirectory()));

        Game game;
        Party party;
        Pickups pickups;
        InventoryControl inventory;
        CameraControl view;
        WorldClock clock;
        MessageBoard messages;
        InterfaceControl ui;
        GameOver over;
        Hero chrono;
        List<IGameEvent> raised;

        [SetUp]
        public void SetUp()
        {
            game = new Game();
            party = new Party(Registry.Heroes, null, new Random(5));
            pickups = new Pickups(Registry.Pickups, party, null, new Random(7)).Install(game);
            view = new CameraControl(Registry.View, party).Install(game);
            inventory = new InventoryControl(pickups, Registry.Hud.Inventory, Registry.Hud.QuickBar, view).Install(game);
            party.Install(game);
            messages = new MessageBoard(Registry.Hud);
            ui = new InterfaceControl(Registry.Hud, party).Install(game);
            clock = new WorldClock(Registry.Time, Registry.Heroes.Locomotion.GameSpeed, messages);
            clock.Install(game);
            messages.Install(game);
            over = new GameOver(party, pickups, inventory, clock, view, messages, ui).Install(game);
            Assert.IsTrue(game.Submit(new JoinGame(PlayerId.One)).Accepted);
            chrono = party.Heroes[0];
            raised = new List<IGameEvent>();
            game.EventRaised += raised.Add;
        }

        void Steps(int n)
        {
            for (var i = 0; i < n; i++) game.Step();
        }

        void Kill() => Assert.IsTrue(game.Submit(new SetHeroVitals(PlayerId.One, 0, 0, null, null, null)).Accepted);

        /// <summary>Places Chrono so pickup <paramref name="index"/> is in reach and selects it (PickupTests.ReachFor's pattern).</summary>
        void ReachFor(int index)
        {
            var p = pickups.All[index];
            var reach = Registry.Pickups.Reach;
            var item = Registry.Pickups.ItemSize;
            var size = party.Data.HeroSize;
            var x = p.X + item * reach.ItemPoint.X - size * reach.HeroPoint.X;
            var y = p.Y + item * reach.ItemPoint.Y - size * reach.HeroPoint.Y;
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, x, y)).Accepted);
            game.Step();
            for (var i = 0; i < 13 && pickups.Selected(PlayerId.One) != index; i++)
                Assert.IsTrue(game.Submit(new CyclePickup(PlayerId.One, 1)).Accepted);
            Assert.AreEqual(index, pickups.Selected(PlayerId.One));
        }

        [Test]
        public void FadingDecreasesByOneEachTickAndStaysPutAboveZeroHealth()
        {
            Assert.AreEqual(255, over.FadeOutOf(PlayerId.One));
            Steps(10);
            Assert.AreEqual(255, over.FadeOutOf(PlayerId.One), "the leader is not dead yet");
            Kill();
            Steps(1);
            Assert.AreEqual(254, over.FadeOutOf(PlayerId.One));
            Steps(9);
            Assert.AreEqual(244, over.FadeOutOf(PlayerId.One));
        }

        [Test]
        public void ResetRestoresTheWholePartyAndTheWorldAfterTheFade()
        {
            // Carry a pickup, open the pack and options, move the camera, then die.
            ReachFor(HealthRoot);
            Assert.IsTrue(game.Submit(new SetMoveInput(PlayerId.One, default(MoveInput).WithPickUp(true))).Accepted);
            game.Step();
            Assert.IsTrue(pickups.All[HealthRoot].PickedUp);
            Assert.AreEqual(1, pickups.Inventory.Items.Count);
            Assert.IsTrue(game.Submit(new SetMoveInput(PlayerId.One, default)).Accepted);
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, 5f, 5f)).Accepted);
            Assert.IsTrue(game.Submit(new ToggleInventory(PlayerId.One)).Accepted);
            Assert.IsTrue(game.Submit(new ToggleOptions(PlayerId.One)).Accepted);
            Assert.IsTrue(ui.For(PlayerId.One).OptionsOpen);
            view.For(PlayerId.One).Camera.MoveTo(9f, 9f, -2f);

            Kill();
            Steps(255); // 254 fading ticks plus the one that reaches 0 and resets
            Assert.AreEqual(GameOver.FadeStart, over.FadeOutOf(PlayerId.One), "the fade is cleared by the reset");

            // The party: full vitals at its start position (not the original's defect of staying dead).
            Assert.AreEqual(chrono.MaxHealth, chrono.Health);
            Assert.AreEqual(chrono.MaxMana, chrono.Mana);
            Assert.AreEqual(chrono.MaxStamina, chrono.Stamina);
            Assert.AreEqual(HeroStatus.Fine, chrono.Status);
            Assert.AreEqual(chrono.Definition.Start.X, chrono.X);
            Assert.AreEqual(chrono.Definition.Start.Y, chrono.Y);

            // The world: pickups returned, pack emptied and shut, options shut, camera home.
            Assert.IsFalse(pickups.All[HealthRoot].PickedUp);
            Assert.AreEqual(0, pickups.Inventory.Items.Count);
            Assert.IsFalse(inventory.For(PlayerId.One).Open);
            Assert.IsFalse(ui.For(PlayerId.One).OptionsOpen);
            Assert.AreEqual((0f, 0f, -1f), (view.For(PlayerId.One).Camera.X, view.For(PlayerId.One).Camera.Y, view.For(PlayerId.One).Camera.Z));

            // The clock: game speed back to 1, day period Morning.
            Assert.AreEqual(1f, clock.GameSpeed);
            Assert.AreEqual("Morning", clock.Period.Name);

            // The tutorial is queued again.
            Assert.Greater(messages.Waiting, 0);
        }

        [Test]
        public void RForcesTheSameResetAtOnceEvenAtFullHealth()
        {
            Assert.AreEqual(chrono.MaxHealth, chrono.Health);
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, 3f, 3f)).Accepted);
            var result = game.Submit(new ResetGame(PlayerId.One));
            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(chrono.Definition.Start.X, chrono.X);
            Assert.AreEqual(1f, clock.GameSpeed);
        }
    }
}
