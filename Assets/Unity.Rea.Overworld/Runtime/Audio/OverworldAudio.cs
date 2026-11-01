using MindAttic.Overworld.Core;
using UnityEngine;

namespace MindAttic.Overworld.Runtime
{
    /// <summary>
    /// Sound.vb as the rebuild plays it (U.OVW-T26; RECONSTRUCTION 15; BIBLE "Music never plays"): Music.mp3 plays and
    /// loops from the start (the original constructed it but never called playMusic), switching to Battle.mp3 while any
    /// hero is in battle mode; the unused effects are wired to the events they were evidently made for (footsteps while a
    /// hero walks or runs, the sword draw and, following it, a swing and a hit as the battle stance is taken, a charge
    /// once a hero's turn gauge finishes recharging, a death when a hero's health reaches 0); zombies ambient loops while
    /// the leader stands in the "Entering Dark Woods" event box; F7 / F8 play and stop the regen test (<see cref="RegenTest"/>).
    /// <para>
    /// sfx/hero/jump.wav and sfx/defend.wav are imported (Audio/Resources) and catalogued but never played: both are
    /// leftovers of art and sound built for mechanics the shipped top-down game never has (the superseded side-view
    /// jump/dash Hero.vb prototype, and a Defend stance no ring-menu choice ever reached), so there is no event in the
    /// shipped game they were evidently made for; inventing one would be inventing gameplay, which this rebuild does not do.
    /// </para>
    /// </summary>
    public sealed class OverworldAudio : MonoBehaviour
    {
        AudioData data;
        AudioLibrary library;
        Game game;
        Party party;
        EventRecord darkWoods;

        AudioSource music;
        AudioSource battle;
        AudioSource ambient;
        AudioSource effects;
        AudioSource regen;

        HeroState[] previousState;
        BattleAction[] previousAction;
        int[] previousHealth;
        int[] previousFrame;

        bool inDarkWoods;

        public static OverworldAudio Create(AudioData data, Game game, Party party, EventRecord darkWoods)
        {
            var go = new GameObject("OverworldAudio");
            var audio = go.AddComponent<OverworldAudio>();
            audio.Initialize(data, game, party, darkWoods);
            return audio;
        }

        void Initialize(AudioData data, Game game, Party party, EventRecord darkWoods)
        {
            this.data = data;
            this.game = game;
            this.party = party;
            this.darkWoods = darkWoods;
            library = new AudioLibrary();

            music = NewSource(data.MusicVolume, true);
            battle = NewSource(data.MusicVolume, true);
            ambient = NewSource(data.AmbientVolume, true);
            effects = NewSource(data.EffectVolume, false);
            regen = NewSource(data.EffectVolume, true);

            music.clip = library.Load(data.Music);
            battle.clip = library.Load(data.BattleMusic);
            ambient.clip = library.Load(data.AmbientZombies);
            regen.clip = library.Load(data.Regen);

            if (music.clip != null) music.Play();
            if (battle.clip != null)
            {
                battle.Play();
                battle.Pause();
            }

            var count = party.Heroes.Count;
            previousState = new HeroState[count];
            previousAction = new BattleAction[count];
            previousHealth = new int[count];
            previousFrame = new int[count];
            for (var i = 0; i < count; i++)
            {
                var hero = party.Heroes[i];
                previousState[i] = hero.State;
                previousAction[i] = hero.BattleAction;
                previousHealth[i] = hero.Health;
                previousFrame[i] = -1;
            }

            game.EventRaised += OnEvent;
            Debug.Log("Unity.Overworld: audio ready (music " + (music.clip != null) + ", battle " + (battle.clip != null) +
                      ", ambient " + (ambient.clip != null) + (darkWoods != null ? " in [" + (darkWoods.X + darkWoods.Left) + ".." +
                      (darkWoods.X + darkWoods.Right) + "]x[" + (darkWoods.Y + darkWoods.Bottom) + ".." + (darkWoods.Y + darkWoods.Top) + "]" : " (no Dark Woods event)") +
                      ", regen " + (regen.clip != null) + ").");
        }

        AudioSource NewSource(float volume, bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.volume = volume;
            return source;
        }

        void OnDestroy()
        {
            if (game != null) game.EventRaised -= OnEvent;
        }

        void OnEvent(IGameEvent e)
        {
            switch (e)
            {
                case BattleToggled battleToggled:
                    if (battleToggled.Battle) EnterBattleMusic();
                    else LeaveBattleMusic();
                    break;
                case HeroStateChanged stateChanged:
                    HandleStateChanged(stateChanged);
                    break;
                case HeroBattleActionChanged actionChanged:
                    HandleActionChanged(actionChanged);
                    break;
                case HeroVitalsChanged vitalsChanged:
                    HandleVitalsChanged(vitalsChanged);
                    break;
                case RegenTestChanged regenTestChanged:
                    if (regenTestChanged.Playing) regen.Play();
                    else regen.Stop();
                    break;
            }
        }

        void EnterBattleMusic()
        {
            if (music.isPlaying) music.Pause();
            battle.UnPause();
        }

        void LeaveBattleMusic()
        {
            if (battle.isPlaying) battle.Pause();
            music.UnPause();
        }

        void HandleStateChanged(HeroStateChanged e)
        {
            if (e.Hero < 0 || e.Hero >= previousState.Length) return;
            var before = previousState[e.Hero];
            previousState[e.Hero] = e.State;
            if (before == e.State) return; // a facing-only change carries no sound
            if (e.State == HeroState.Drawing)
            {
                PlayOneShot(data.SwordDraw);
            }
            else if (before == HeroState.Drawing && e.State == HeroState.Battle)
            {
                // The shipped game never resolves an attack against anything: the swing and, close behind it, the hit
                // are the same drawing flourish, not a landed blow (see the class remarks).
                PlayOneShot(Pick(data.SwordSwing));
                PlayOneShot(Pick(data.SwordHit));
            }
        }

        void HandleActionChanged(HeroBattleActionChanged e)
        {
            if (e.Hero < 0 || e.Hero >= previousAction.Length) return;
            var before = previousAction[e.Hero];
            previousAction[e.Hero] = e.Action;
            if (before == BattleAction.Recharging && e.Action == BattleAction.Ready) PlayOneShot(data.Charge);
        }

        void HandleVitalsChanged(HeroVitalsChanged e)
        {
            if (e.Hero < 0 || e.Hero >= previousHealth.Length) return;
            var before = previousHealth[e.Hero];
            previousHealth[e.Hero] = e.Health;
            if (before > 0 && e.Health <= 0) PlayOneShot(Pick(data.Death));
        }

        void PlayOneShot(string file)
        {
            var clip = library.Load(file);
            if (clip != null) effects.PlayOneShot(clip);
        }

        static string Pick(System.Collections.Generic.IReadOnlyList<string> files)
        {
            if (files == null || files.Count == 0) return null;
            return files[UnityEngine.Random.Range(0, files.Count)];
        }

        void Update()
        {
            for (var i = 0; i < party.Heroes.Count; i++)
            {
                var hero = party.Heroes[i];
                if (hero.State != HeroState.Walking && hero.State != HeroState.Running)
                {
                    previousFrame[i] = -1;
                    continue;
                }

                var frame = hero.Frame;
                if (frame == previousFrame[i])
                {
                    continue;
                }

                var frames = hero.State == HeroState.Walking ? hero.Definition.WalkFrames : hero.Definition.RunFrames;
                var half = frames / 2;
                if (frame == 0 || frame == half)
                {
                    if (hero.State == HeroState.Running)
                    {
                        PlayOneShot(data.RunFootstep);
                    }
                    else if (data.WalkFootsteps.Count > 0)
                    {
                        PlayOneShot(data.WalkFootsteps[frame == 0 ? 0 : 1 % data.WalkFootsteps.Count]);
                    }
                }

                previousFrame[i] = frame;
            }

            UpdateDarkWoods();
        }

        void UpdateDarkWoods()
        {
            if (darkWoods == null || ambient.clip == null) return;
            var leader = party.Leader(PlayerId.One);
            var left = darkWoods.X + darkWoods.Left;
            var right = darkWoods.X + darkWoods.Right;
            var bottom = darkWoods.Y + darkWoods.Bottom;
            var top = darkWoods.Y + darkWoods.Top;
            var inside = leader.X >= left && leader.X <= right && leader.Y >= bottom && leader.Y <= top;
            if (inside == inDarkWoods) return;
            inDarkWoods = inside;
            if (inside) ambient.Play();
            else ambient.Stop();
        }
    }
}
