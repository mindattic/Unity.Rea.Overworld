using System;
using System.IO;
using MindAttic.Overworld.Core;
using NUnit.Framework;

namespace MindAttic.Overworld.Tests
{
    /// <summary>
    /// U.OVW-US-A29 (RECONSTRUCTION 12.4; BIBLE "Combat"): attacking and being attacked, with every number a documented
    /// decision (the original had no damage code at all).
    /// </summary>
    public class CombatTests
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
        Hero chrono;

        [SetUp]
        public void SetUp()
        {
            game = new Game();
            party = new Party(Registry.Heroes, null, new Random(5)).Install(game);
            enemies = new WorldEnemies(Registry.PlayableWorld, party).Install(game);
            party.FindEnemyInRange = enemies.NearestInRange;
            Assert.IsTrue(game.Submit(new JoinGame(PlayerId.One)).Accepted);
            chrono = party.Heroes[0];
        }

        void Steps(int n)
        {
            for (var i = 0; i < n; i++) game.Step();
        }

        /// <summary>Turns the ring one entry and steps until it settles (RingMenuTests' own pattern).</summary>
        void Turn(int step)
        {
            Assert.IsTrue(game.Submit(new TurnRing(PlayerId.One, step)).Accepted);
            var ring = party.RingOf(PlayerId.One);
            var ticks = 0;
            while (ring.Mode != RingMode.Select && ticks < 100)
            {
                game.Step();
                ticks++;
            }
        }

        /// <summary>Turns the ring from Move to Attack (one turn right: Move, End, Skill, Item, Attack is the left order, so right from Move reaches Attack directly).</summary>
        void TurnToAttack()
        {
            Turn(1);
            Assert.AreEqual(RingEntry.Attack, party.RingOf(PlayerId.One).SelectedEntry);
        }

        EnemyView Find(string name)
        {
            foreach (var e in enemies.Enemies) if (e.Name == name) return e;
            return null;
        }

        [Test]
        public void DamageWeightsTheWeaponsSplitByTheTargetsClassAndIsNeverBelowOne()
        {
            var steelSword = Registry.Weapons.Weapons[0]; // rating 8, slashing 0.5, piercing 0.15, blunt 0
            Assert.AreEqual("Steel Sword", steelSword.Name);
            var fineAxe = Registry.Weapons.Weapons[1]; // rating 12, slashing 0.4, piercing 0, blunt 0.9
            Assert.AreEqual("Fine Axe", fineAxe.Name);

            Assert.AreEqual(8, Combat.Damage(steelSword, "Animal")); // vulnerable to piercing, resists blunt: a wash near rating
            Assert.AreEqual(14, Combat.Damage(fineAxe, "Paranormal")); // slimes split under a blunt blow: boosted
            Assert.Greater(Combat.Damage(fineAxe, "Paranormal"), Combat.Damage(steelSword, "Paranormal"), "the axe's blunt split suits slimes better than the sword's edge");
            Assert.AreEqual(0, Combat.Damage(null, "Animal"));
        }

        [Test]
        public void HeroDatabasesFourWeaponsResolveTheirOwnSimplerFigure()
        {
            var weapons = Registry.HeroDatabase.Weapons;
            Assert.AreEqual(4, weapons.Count);
            var random = new Random(1);
            foreach (var w in weapons)
            {
                var damage = Combat.DamageFromRecord(w, random);
                Assert.GreaterOrEqual(damage, Math.Max(1, w.Power), w.Name);
                Assert.LessOrEqual(damage, w.Power + w.Random, w.Name);
            }

            Assert.AreEqual(0, Combat.DamageFromRecord(null, random));
        }

        [Test]
        public void AttackingAWerelynxInRangeDamagesItWearsTheWeaponAndPassesTheTurn()
        {
            var wolf = Find("Werelynx"); // range 0.8, class Animal
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, wolf.X - 0.05f, wolf.Y)).Accepted);
            Assert.IsTrue(game.Submit(new ToggleBattle(PlayerId.One)).Accepted); // enters battle, ring open at Move
            Steps(1);
            // Turn the ring to Attack (Move, End, Skill, Item, Attack: four turns right from Move).
            TurnToAttack();
            var fullDurability = chrono.WeaponDurability;
            var healthBefore = wolf.Health;
            var result = game.Submit(new ChooseFromRing(PlayerId.One));
            Assert.IsTrue(result.Accepted);
            EnemyHit hit = null;
            foreach (var e in result.Events) if (e is EnemyHit found) hit = found;
            Assert.IsNotNull(hit, "an EnemyHit event was raised");
            Assert.AreEqual(wolf.Id, hit.EnemyId);
            Assert.Less(wolf.Health, healthBefore);
            Assert.AreEqual(healthBefore - hit.Damage, wolf.Health);
            Assert.AreEqual(fullDurability - 1f, chrono.WeaponDurability);
        }

        [Test]
        public void DefeatZeroesHealthHidesItAndItNeverActsAgain()
        {
            var slime = Find("Blue Slime"); // max health 2
            slime.Health = 1;
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, slime.X - 0.05f, slime.Y)).Accepted);
            Assert.IsTrue(game.Submit(new ToggleBattle(PlayerId.One)).Accepted);
            Steps(1);
            TurnToAttack();
            game.Submit(new ChooseFromRing(PlayerId.One));
            Assert.AreEqual(0, slime.Health);

            var x = slime.X;
            Steps(60);
            Assert.AreEqual(x, slime.X, "a defeated enemy holds still");
        }

        [Test]
        public void AnAggressiveEnemyInContactHurtsTheLeaderAtTheSameRateAsThePoisonBox()
        {
            var fly = Find("Botherfly"); // Aggressive
            var hp = chrono.Health;
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, fly.X, fly.Y)).Accepted); // in contact at once
            Steps(1);
            Assert.AreEqual(hp - 1, chrono.Health, "hit on the first tick in contact");
            Steps(29);
            Assert.AreEqual(hp - 1, chrono.Health, "the attack rate has not elapsed again yet");
            Steps(1);
            Assert.AreEqual(hp - 2, chrono.Health);
        }
    }
}
