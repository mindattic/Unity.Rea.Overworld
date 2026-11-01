using System;
using System.IO;
using MindAttic.Overworld.Core;
using NUnit.Framework;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;

namespace MindAttic.Overworld.Tests
{
    /// <summary>
    /// U.OVW-T36 (RECONSTRUCTION 18; BIBLE "Skill ring entry", "Cutscene"): Magic.vb's five spell schools and its one
    /// spell, Flame, and Cutscene.vb's own "Hello" record, kept as content instead of dropped.
    /// </summary>
    public class MagicTests
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

        [Test]
        public void TheFiveSchoolsAndTheOneSpellTheOriginalDefinedAreRead()
        {
            CollectionAssert.AreEqual(new[] { "Regeneration", "Destruction", "Alteration", "Conjuration", "Alchemy" }, Registry.Magic.Schools);
            Assert.AreEqual(1, Registry.Magic.Spells.Count);
            var flame = Registry.Magic.Find("Flame");
            Assert.IsNotNull(flame);
            Assert.AreEqual("Destruction", flame.School);
            Assert.AreEqual(5, flame.Cost);
            Assert.AreEqual(0.2f, flame.Range);
            Assert.AreEqual(10f, flame.Duration);
            Assert.IsNull(Registry.Magic.Find("Meteor"), "only Flame was ever given a cost, range and duration; none of the sketched spells is invented");
        }

        [Test]
        public void TheOneCutsceneIsReadWithItsOtherFieldsEmptyNotInvented()
        {
            Assert.AreEqual(1, Registry.Cutscenes.Cutscenes.Count);
            var hello = Registry.Cutscenes.Find("Hello");
            Assert.IsNotNull(hello);
            Assert.AreEqual("", hello.Title);
            Assert.AreEqual("", hello.StartScript);
            Assert.AreEqual("", hello.EndScript);
            Assert.AreEqual("", hello.Objective);
        }
    }

    /// <summary>U.OVW-T36: the Skill ring entry, which the original left doing nothing at all, now casts Flame.</summary>
    public class SkillRingTests
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
        Hero chrono;

        [SetUp]
        public void SetUp()
        {
            game = new Game();
            party = new Party(Registry.Heroes, null, new Random(5)).Install(game);
            party.Magic = Registry.Magic;
            Assert.IsTrue(game.Submit(new JoinGame(PlayerId.One)).Accepted);
            chrono = party.Heroes[0];
        }

        void Steps(int n)
        {
            for (var i = 0; i < n; i++) game.Step();
        }

        /// <summary>Move, End, Skill, Item, Attack is the left order; two left turns from Move reach Skill.</summary>
        void TurnToSkill()
        {
            for (var i = 0; i < 2; i++)
            {
                Assert.IsTrue(game.Submit(new TurnRing(PlayerId.One, -1)).Accepted);
                var ring = party.RingOf(PlayerId.One);
                var ticks = 0;
                while (ring.Mode != RingMode.Select && ticks < 100)
                {
                    game.Step();
                    ticks++;
                }
            }

            Assert.AreEqual(RingEntry.Skill, party.RingOf(PlayerId.One).SelectedEntry);
        }

        [Test]
        public void ChoosingSkillWithEnoughManaCastsFlameAtTheCastersOwnPointAndPassesTheTurn()
        {
            Assert.IsTrue(game.Submit(new ToggleBattle(PlayerId.One)).Accepted);
            Steps(1);
            TurnToSkill();
            var manaBefore = chrono.Mana;
            Assert.GreaterOrEqual(manaBefore, 5, "Chrono starts with enough mana to cast Flame");
            var x = chrono.X;
            var y = chrono.Y;
            var result = game.Submit(new ChooseFromRing(PlayerId.One));
            Assert.IsTrue(result.Accepted);
            SpellCast cast = null;
            foreach (var e in result.Events) if (e is SpellCast found) cast = found;
            Assert.IsNotNull(cast, "a SpellCast event was raised");
            Assert.AreEqual("Flame", cast.Spell);
            Assert.AreEqual(x, cast.X);
            Assert.AreEqual(y, cast.Y);
            Assert.AreEqual(10f, cast.Duration);
            Assert.AreEqual(manaBefore - 5, chrono.Mana);
        }

        [Test]
        public void ChoosingSkillWithoutEnoughManaDoesNothing()
        {
            Assert.IsTrue(game.Submit(new ToggleBattle(PlayerId.One)).Accepted);
            Steps(1);
            TurnToSkill();
            chrono.Mana = 4;
            var result = game.Submit(new ChooseFromRing(PlayerId.One));
            Assert.IsTrue(result.Accepted);
            foreach (var e in result.Events) Assert.IsNotInstanceOf<SpellCast>(e);
            Assert.AreEqual(4, chrono.Mana);
            Assert.IsTrue(party.RingOf(PlayerId.One).Open, "nothing was taken, so the ring stays open");
        }

        [Test]
        public void WithNoMagicContentSkillIsRefusedJustAsTheOriginalLeftIt()
        {
            party.Magic = null;
            Assert.IsTrue(game.Submit(new ToggleBattle(PlayerId.One)).Accepted);
            Steps(1);
            TurnToSkill();
            var manaBefore = chrono.Mana;
            var result = game.Submit(new ChooseFromRing(PlayerId.One));
            Assert.IsTrue(result.Accepted);
            foreach (var e in result.Events) Assert.IsNotInstanceOf<SpellCast>(e);
            Assert.AreEqual(manaBefore, chrono.Mana);
        }
    }
}
