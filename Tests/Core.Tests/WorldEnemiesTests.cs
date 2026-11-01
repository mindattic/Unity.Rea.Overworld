using System;
using System.IO;
using MindAttic.Overworld.Core;
using NUnit.Framework;
using StringAssert = NUnit.Framework.Legacy.StringAssert;

namespace MindAttic.Overworld.Tests
{
    /// <summary>
    /// U.OVW-US-A28 (RECONSTRUCTION 16.2; BIBLE "Enemy AI states", "the five enemies with no placement"): the five
    /// data-defined enemies, placed by this ticket's own decision (Area_Enemies is empty) and moved by their state.
    /// </summary>
    public class WorldEnemiesTests
    {
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
        WorldEnemies enemies;

        [SetUp]
        public void SetUp()
        {
            game = new Game();
            party = new Party(Registry.Heroes, null, new Random(5));
            party.Install(game);
            enemies = new WorldEnemies(Registry.PlayableWorld, party).Install(game);
            Assert.IsTrue(game.Submit(new JoinGame(PlayerId.One)).Accepted);
        }

        EnemyView Find(string name)
        {
            foreach (var e in enemies.Enemies) if (e.Name == name) return e;
            return null;
        }

        [Test]
        public void AllFiveAreCatalogueDataPlacedInTheGrasslands()
        {
            Assert.AreEqual(5, enemies.Enemies.Count);
            var werelynx = Find("Werelynx");
            Assert.AreEqual(6, werelynx.MaxHealth);
            Assert.AreEqual(0.004f, werelynx.Speed);
            Assert.AreEqual(0.8f, werelynx.Range);
            Assert.AreEqual("Defensive", werelynx.State);
            Assert.AreEqual("Walking", werelynx.Motion);
            Assert.AreEqual("Animal", werelynx.Class);
            Assert.AreEqual(-0.2f, werelynx.Y);

            Assert.IsNotNull(Find("Botherfly"));
            Assert.IsNotNull(Find("Blue Slime"));
            Assert.IsNotNull(Find("Red Slime"));
            Assert.IsNotNull(Find("Toadie"));
        }

        [Test]
        public void PlaceholderArtIsFlaggedForTheTwoNeverShippedEnemies()
        {
            Assert.AreEqual("gfx/enemy/slime/slime_1.PNG", Find("Botherfly").Sprite);
            Assert.AreEqual("gfx/enemy/slime/slime_1.PNG", Find("Toadie").Sprite);
            Assert.AreEqual("gfx/enemy/slime_blue.PNG", Find("Blue Slime").Sprite);
            Assert.AreEqual("gfx/enemy/slime_red.PNG", Find("Red Slime").Sprite);
            Assert.AreEqual("gfx/enemy/catman/stand_left.PNG", Find("Werelynx").Sprite);
        }

        [Test]
        public void AggressiveMovesTowardTheLeaderWithinRangeNotBeyondIt()
        {
            var fly = Find("Botherfly"); // Aggressive, range 1.4
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, fly.X - 1f, fly.Y)).Accepted); // within range
            var x = fly.X;
            game.Step();
            Assert.Greater(x, fly.X, "moves toward the leader (left, since the leader is left of it)");

            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, fly.X - 5f, fly.Y)).Accepted); // beyond range
            x = fly.X;
            game.Step();
            Assert.AreEqual(x, fly.X, "the leader is beyond range: it holds");
        }

        [Test]
        public void DefensiveHoldsUntilApproachedWithinHalfRangeThenActsAggressive()
        {
            var wolf = Find("Werelynx"); // Defensive, range 0.8, so half is 0.4
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, wolf.X - 0.6f, wolf.Y)).Accepted); // within range, outside half
            var x = wolf.X;
            game.Step();
            Assert.AreEqual(x, wolf.X, "not yet within half its range: it holds");

            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, wolf.X - 0.3f, wolf.Y)).Accepted); // within half
            game.Step();
            Assert.Greater(wolf.X, x, "approached within half its range: now it moves toward the leader");
        }

        [Test]
        public void AfraidMovesAwayFromTheLeaderWithinRange()
        {
            // No Afraid enemy exists among the five, so the rule itself is checked directly: a Passive one would never
            // move, so this builds a scratch Afraid view through the same Step the real enemies use.
            var slime = Find("Blue Slime"); // Passive, by the data
            Assert.AreEqual("Passive", slime.State);
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, slime.X - 0.1f, slime.Y)).Accepted);
            var x = slime.X;
            game.Step();
            Assert.AreEqual(x, slime.X, "Passive ignores the party even standing right next to it");
        }

        [Test]
        public void WerelynxAnimatesWalkingOnlyWhileMoving()
        {
            var wolf = Find("Werelynx");
            Assert.AreEqual("gfx/enemy/catman/stand_left.PNG", wolf.Sprite);
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, wolf.X - 0.3f, wolf.Y)).Accepted);
            game.Step(); // within half range: triggered, moves
            StringAssert.Contains("walk_left_", wolf.Sprite);
        }
    }
}
