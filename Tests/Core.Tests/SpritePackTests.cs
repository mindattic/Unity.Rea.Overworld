using System;
using System.IO;
using MindAttic.Overworld.Core;
using NUnit.Framework;

namespace MindAttic.Overworld.Tests
{
    /// <summary>
    /// U.OVW-T35 (RECONSTRUCTION 4.4; BIBLE "Selectable sprite packs"): the original's own unused Belmont and girl
    /// sprite sets, kept as a hero's selectable art instead of the one shared Chrono set every hero drew with before.
    /// </summary>
    public class SpritePackTests
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
            party.SpritePacks = Registry.SpritePacks;
            Assert.IsTrue(game.Submit(new JoinGame(PlayerId.One)).Accepted);
            chrono = party.Heroes[0];
        }

        [Test]
        public void TheContentHasExactlyBelmontAndGirlEachWithTheirOwnFrameCounts()
        {
            Assert.AreEqual(2, Registry.SpritePacks.Packs.Count);
            var belmont = Registry.SpritePacks.Find("Belmont");
            Assert.IsNotNull(belmont);
            Assert.AreEqual(5, belmont.WalkFrames);
            Assert.AreEqual(5, belmont.RunFrames);
            Assert.AreEqual(6, belmont.DrawFrames);
            Assert.AreEqual(6, belmont.BattleFrames);

            var girl = Registry.SpritePacks.Find("Girl");
            Assert.IsNotNull(girl);
            Assert.AreEqual(5, girl.WalkFrames);
            Assert.AreEqual(5, girl.RunFrames);
            Assert.AreEqual(0, girl.DrawFrames);
            Assert.AreEqual(0, girl.BattleFrames);
        }

        [Test]
        public void AHeroDrawsWithTheSharedSetUntilGivenAPackOfItsOwn()
        {
            Assert.IsNull(chrono.SpritePackName);
            Assert.AreSame(party.Data.Sprites, party.SpritesFor(chrono));
            Assert.AreEqual(chrono.Definition.WalkFrames, party.WalkFramesFor(chrono));
        }

        [Test]
        public void SelectingAPackGivesTheHeroItsOwnSpritesAndFrameCountsAndResetsItsFrame()
        {
            chrono.Frame = 3;
            var result = game.Submit(new SelectSpritePack(PlayerId.One, chrono.Index, "Belmont"));
            Assert.IsTrue(result.Accepted);
            Assert.AreEqual("Belmont", chrono.SpritePackName);
            Assert.AreEqual(0, chrono.Frame);
            Assert.AreSame(Registry.SpritePacks.Find("Belmont").Sprites, party.SpritesFor(chrono));
            Assert.AreEqual(5, party.WalkFramesFor(chrono));
            Assert.AreEqual(6, party.DrawFramesFor(chrono));
        }

        [Test]
        public void SelectingNullOrEmptyClearsTheHeroBackToTheSharedSet()
        {
            Assert.IsTrue(game.Submit(new SelectSpritePack(PlayerId.One, chrono.Index, "Girl")).Accepted);
            Assert.AreEqual("Girl", chrono.SpritePackName);

            Assert.IsTrue(game.Submit(new SelectSpritePack(PlayerId.One, chrono.Index, null)).Accepted);
            Assert.IsNull(chrono.SpritePackName);
            Assert.AreSame(party.Data.Sprites, party.SpritesFor(chrono));
        }

        [Test]
        public void AnUnknownPackNameIsRejectedAndTheHeroKeepsItsCurrentPack()
        {
            var result = game.Submit(new SelectSpritePack(PlayerId.One, chrono.Index, "Nonexistent"));
            Assert.IsFalse(result.Accepted);
            Assert.IsNull(chrono.SpritePackName);
        }

        [Test]
        public void AnOutOfRangeHeroIndexIsRejected()
        {
            var result = game.Submit(new SelectSpritePack(PlayerId.One, party.Heroes.Count, "Belmont"));
            Assert.IsFalse(result.Accepted);
        }

        [Test]
        public void EachHeroKeepsItsOwnPackIndependentlyOfTheOthers()
        {
            var belmont = party.Heroes[0];
            var another = party.Heroes[1];
            Assert.IsTrue(game.Submit(new SelectSpritePack(PlayerId.One, belmont.Index, "Belmont")).Accepted);
            Assert.AreEqual("Belmont", belmont.SpritePackName);
            Assert.IsNull(another.SpritePackName);
            Assert.AreSame(party.Sprites, party.SpritesFor(another));
        }
    }
}
