using System;
using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>
    /// Sound.vb's volumes and the sound files the rebuild wires to the events they were evidently made for
    /// (RECONSTRUCTION 15; BIBLE "Music never plays"), audio.json. Volumes are the original's DirectSound centibels
    /// (hundredths of a decibel below full) converted to the linear gain Unity's AudioSource.volume wants
    /// (10 ^ (centibels / 100 / 20)): music 0 (full), effects -1000 (-10 dB), ambient -1500 (-15 dB).
    /// </summary>
    public sealed class AudioData
    {
        public int MusicCentibels { get; internal set; }
        public int EffectCentibels { get; internal set; }
        public int AmbientCentibels { get; internal set; }

        public float MusicVolume => Gain(MusicCentibels);
        public float EffectVolume => Gain(EffectCentibels);
        public float AmbientVolume => Gain(AmbientCentibels);

        static float Gain(int centibels) => (float)Math.Pow(10.0, centibels / 100.0 / 20.0);

        /// <summary>sfx/music/Music.mp3: constructed by Sound.vb but never played (playMusic was never called). Fixed: it plays, looping, from the start.</summary>
        public string Music { get; internal set; }

        /// <summary>sfx/music/Battle.mp3: played instead of <see cref="Music"/> while any hero is in battle mode.</summary>
        public string BattleMusic { get; internal set; }

        /// <summary>sfx/ambient/zombies.mp3: loops while the leader stands in the "Entering Dark Woods" event box.</summary>
        public string AmbientZombies { get; internal set; }

        /// <summary>sfx/hero/regen.wav: the original's one wired sound effect, F7 plays it looping and F8 stops it (<see cref="RegenTest"/>).</summary>
        public string Regen { get; internal set; }

        /// <summary>sfx/footstep_1.wav, sfx/footstep_2.wav: alternated once per half stride while a hero walks.</summary>
        public IReadOnlyList<string> WalkFootsteps { get; internal set; }

        /// <summary>sfx/hero/heavy_footstep.wav: once per half stride while a hero runs.</summary>
        public string RunFootstep { get; internal set; }

        /// <summary>sfx/hero/attack/sword_draw.wav: once a hero's draw cycle begins (entering battle, or its turn gauge filling while Recharging).</summary>
        public string SwordDraw { get; internal set; }

        /// <summary>sfx/hero/attack/sword_swing_1..7.wav: one at random once the draw cycle ends and the battle stance begins.</summary>
        public IReadOnlyList<string> SwordSwing { get; internal set; }

        /// <summary>
        /// sfx/hero/attack/sword_hit_1..3.wav: one at random right after the swing. The shipped game never resolves an
        /// attack against anything (no combat yet), so this is the same drawing flourish as the swing, not a landed blow.
        /// </summary>
        public IReadOnlyList<string> SwordHit { get; internal set; }

        /// <summary>sfx/hero/charge.wav: once a hero's turn gauge finishes recharging in battle (Recharging to Ready).</summary>
        public string Charge { get; internal set; }

        /// <summary>sfx/death_1..4.wav: one at random when a hero's health reaches 0.</summary>
        public IReadOnlyList<string> Death { get; internal set; }

        internal static AudioData Read(IContentSource source, string file, List<string> errors)
        {
            var root = ContentFiles.Open(source, file, errors);
            if (root == null) return null;
            var data = new AudioData
            {
                MusicCentibels = root.Integer("musicCentibels"),
                EffectCentibels = root.Integer("effectCentibels"),
                AmbientCentibels = root.Integer("ambientCentibels"),
                Music = root.Text("music"),
                BattleMusic = root.Text("battleMusic"),
                AmbientZombies = root.Text("ambientZombies"),
                Regen = root.Text("regen"),
                RunFootstep = root.Text("runFootstep"),
                SwordDraw = root.Text("swordDraw"),
                Charge = root.Text("charge"),
            };
            data.WalkFootsteps = NonEmpty(root, "walkFootsteps");
            data.SwordSwing = NonEmpty(root, "swordSwing");
            data.SwordHit = NonEmpty(root, "swordHit");
            data.Death = NonEmpty(root, "death");
            root.Done();
            return data;
        }

        static IReadOnlyList<string> NonEmpty(ContentReader r, string key)
        {
            var list = r.Texts(key);
            if (list.Count == 0) r.Fail(key, "must name at least one sound");
            return list;
        }
    }
}
