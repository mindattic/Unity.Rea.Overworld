using System;
using System.IO;
using MindAttic.Overworld.Core;
using NUnit.Framework;

namespace MindAttic.Overworld.Tests
{
    /// <summary>
    /// U.OVW-US-A27 (RECONSTRUCTION 16.2; BIBLE "NPCs met"): the Peasant Woman (standing, its front picture) and the
    /// Magitek Armor (pursuing, cycling its walk-right frames in place) placed in the Grasslands.
    /// </summary>
    public class WorldNpcsTests
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
        public void TheTwoNpcsArePlacedWithTheirOwnSizeAndDirectory()
        {
            var world = Registry.PlayableWorld;
            var npcs = new WorldNpcs(world, world.Start.AreaId);
            Assert.AreEqual(2, npcs.Npcs.Count);

            var woman = npcs.Npcs[0];
            Assert.AreEqual("Peasant Woman", woman.Name, "W6 fixes the original's 'Peasent'");
            Assert.AreEqual((1f, -0.2f), (woman.X, woman.Y));
            Assert.AreEqual((0.144f, 0.072f), (woman.Width, woman.Height));
            Assert.AreEqual("gfx/npc/woman/", woman.Directory);
            Assert.AreEqual("stand", woman.State);
            Assert.AreEqual("gfx/npc/woman/npc_front.PNG", woman.Sprite);

            var magitek = npcs.Npcs[1];
            Assert.AreEqual("Magitek Armor", magitek.Name);
            Assert.AreEqual((4f, -0.2f), (magitek.X, magitek.Y));
            Assert.AreEqual("pursue", magitek.State);
            Assert.AreEqual("gfx/npc/magitek/npc_walk_right_1.PNG", magitek.Sprite);
        }

        [Test]
        public void APursuingNpcCyclesItsWalkFramesInPlaceAStandingOneNever()
        {
            var game = new Game();
            var world = Registry.PlayableWorld;
            var npcs = new WorldNpcs(world, world.Start.AreaId).Install(game);
            var woman = npcs.Npcs[0];
            var magitek = npcs.Npcs[1];
            var x = magitek.X;
            var y = magitek.Y;

            for (var i = 0; i < 9; i++) game.Step(); // short of WalkRate (0.15 s = 9 ticks)
            Assert.AreEqual("gfx/npc/magitek/npc_walk_right_1.PNG", magitek.Sprite, "the rate has not elapsed yet");
            game.Step(); // the 10th tick: 0.1667 s, past 0.15 s
            Assert.AreEqual("gfx/npc/magitek/npc_walk_right_2.PNG", magitek.Sprite);
            Assert.AreEqual((x, y), (magitek.X, magitek.Y), "it never moves here: chasing is the encounters story (U.OVW-T32)");

            Assert.AreEqual("gfx/npc/woman/npc_front.PNG", woman.Sprite, "standing never animates");
        }
    }
}
