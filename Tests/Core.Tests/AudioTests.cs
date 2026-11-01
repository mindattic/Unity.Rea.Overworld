using System;
using System.IO;
using MindAttic.Overworld.Core;
using NUnit.Framework;

namespace MindAttic.Overworld.Tests
{
    /// <summary>
    /// U.OVW-T26 (RECONSTRUCTION 15; BIBLE "Music never plays"): the regen test (Sound.vb's only wired sound effect, F7
    /// plays sfx/hero/regen.wav looping and F8 stops it) and audio.json's volumes.
    /// </summary>
    public class AudioTests
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
        public void TheVolumesAreTheOriginalsConvertedToLinearGain()
        {
            var a = Registry.Audio;
            Assert.AreEqual(0, a.MusicCentibels);
            Assert.AreEqual(-1000, a.EffectCentibels);
            Assert.AreEqual(-1500, a.AmbientCentibels);
            Assert.AreEqual(1f, a.MusicVolume, 1e-6f, "0 centibels is full volume");
            Assert.AreEqual(0.31622776f, a.EffectVolume, 1e-6f, "-1000 centibels is -10 dB");
            Assert.AreEqual(0.17782794f, a.AmbientVolume, 1e-6f, "-1500 centibels is -15 dB");
        }

        [Test]
        public void TheSoundsAreWhatTheOriginalShipped()
        {
            var a = Registry.Audio;
            Assert.AreEqual("sfx/music/Music.mp3", a.Music);
            Assert.AreEqual("sfx/music/Battle.mp3", a.BattleMusic);
            Assert.AreEqual("sfx/ambient/zombies.mp3", a.AmbientZombies);
            Assert.AreEqual("sfx/hero/regen.wav", a.Regen);
            Assert.AreEqual(2, a.WalkFootsteps.Count);
            Assert.AreEqual("sfx/hero/heavy_footstep.wav", a.RunFootstep);
            Assert.AreEqual("sfx/hero/attack/sword_draw.wav", a.SwordDraw);
            Assert.AreEqual(7, a.SwordSwing.Count);
            Assert.AreEqual(3, a.SwordHit.Count);
            Assert.AreEqual("sfx/hero/charge.wav", a.Charge);
            Assert.AreEqual(4, a.Death.Count);
        }

        [Test]
        public void F7PlaysLoopingAndF8Stops()
        {
            var game = new Game();
            var regenTest = new RegenTest().Install(game);
            Assert.IsTrue(game.Submit(new JoinGame(PlayerId.One)).Accepted);
            Assert.IsFalse(regenTest.Playing);

            var played = game.Submit(new PlayRegenTest(PlayerId.One));
            Assert.IsTrue(played.Accepted);
            Assert.IsTrue(regenTest.Playing);
            Assert.AreEqual(1, played.Events.Count);
            var changed = (RegenTestChanged)played.Events[0];
            Assert.IsTrue(changed.Playing);
            Assert.AreEqual(PlayerId.One, changed.Player);

            var stopped = game.Submit(new StopRegenTest(PlayerId.One));
            Assert.IsTrue(stopped.Accepted);
            Assert.IsFalse(regenTest.Playing);
            Assert.IsFalse(((RegenTestChanged)stopped.Events[0]).Playing);
        }
    }
}
