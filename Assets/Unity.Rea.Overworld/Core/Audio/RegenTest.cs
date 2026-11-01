namespace MindAttic.Overworld.Core
{
    /// <summary>F7 (Sound.vb, RECONSTRUCTION 15): plays sfx/hero/regen.wav looping.</summary>
    public sealed class PlayRegenTest : ICommand
    {
        public PlayRegenTest(PlayerId player) { Player = player; }
        public PlayerId Player { get; }
    }

    /// <summary>F8: stops it.</summary>
    public sealed class StopRegenTest : ICommand
    {
        public StopRegenTest(PlayerId player) { Player = player; }
        public PlayerId Player { get; }
    }

    /// <summary>The regen test sound started or stopped; presentation plays <see cref="AudioData.Regen"/> looping while <see cref="Playing"/> says so.</summary>
    public sealed class RegenTestChanged : IGameEvent
    {
        public RegenTestChanged(long tick, PlayerId player, bool playing) { Tick = tick; Player = player; Playing = playing; }
        public long Tick { get; }
        public PlayerId Player { get; }
        public bool Playing { get; }
    }

    /// <summary>
    /// F7 plays sfx/hero/regen.wav looping (at the effect volume, -1000 centibels) and F8 stops it: Sound.vb's only wired
    /// sound effect (RECONSTRUCTION 15; BIBLE "Music never plays"), kept as the same global test tone the original's one
    /// DirectSound secondary buffer was, not tied to any hero's own regeneration. <see cref="Playing"/> mirrors the
    /// original's single buffer (one flag, not per hero); presentation does the actual looping and stopping.
    /// </summary>
    public sealed class RegenTest
    {
        public bool Playing { get; private set; }

        public RegenTest Install(Game game)
        {
            game.Register<PlayRegenTest>(HandlePlay);
            game.Register<StopRegenTest>(HandleStop);
            return this;
        }

        CommandResult HandlePlay(Game game, PlayRegenTest command)
        {
            Playing = true;
            return CommandResult.Accept(new RegenTestChanged(game.Tick, command.Player, true));
        }

        CommandResult HandleStop(Game game, StopRegenTest command)
        {
            Playing = false;
            return CommandResult.Accept(new RegenTestChanged(game.Tick, command.Player, false));
        }
    }
}
