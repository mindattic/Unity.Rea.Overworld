using System;

namespace MindAttic.Overworld.Core
{
    /// <summary>Selects, or clears (null), <paramref name="hero"/>'s alternate sprite pack ("Belmont", "Girl"; RECONSTRUCTION 4.4, U.OVW-T35).</summary>
    public sealed class SelectSpritePack : ICommand
    {
        public SelectSpritePack(PlayerId player, int hero, string pack) { Player = player; Hero = hero; Pack = pack; }
        public PlayerId Player { get; }
        public int Hero { get; }
        public string Pack { get; }
    }

    public sealed class SpritePackChanged : IGameEvent
    {
        public SpritePackChanged(long tick, int hero, string pack) { Tick = tick; Hero = hero; Pack = pack; }
        public long Tick { get; }
        public int Hero { get; }
        public string Pack { get; }
    }

    /// <summary>
    /// The original's own unused Belmont and girl sprite sets, kept as selectable art for a hero (RECONSTRUCTION 4.4;
    /// BIBLE "Selectable sprite packs", U.OVW-T35): every hero drew with the same shared Chrono set before this ticket
    /// (<see cref="Party.Sprites"/>); <see cref="SelectSpritePack"/> gives one hero its own instead, null clearing it
    /// back to the shared set.
    /// </summary>
    public sealed partial class Party
    {
        /// <summary>The alternate packs a hero may be given ("Belmont", "Girl"); null: <see cref="SelectSpritePack"/> always refused.</summary>
        public SpritePackData SpritePacks { get; set; }

        /// <summary>The sprites <paramref name="hero"/> actually draws with now: its own pack if it has one, else the shared set every hero drew with before U.OVW-T35.</summary>
        internal HeroSprites SpritesFor(Hero hero)
        {
            var pack = SpritePacks?.Find(hero.SpritePackName);
            return pack?.Sprites ?? Sprites;
        }

        /// <summary>The frame count <paramref name="hero"/>'s walk cycle actually has now.</summary>
        internal int WalkFramesFor(Hero hero) => SpritePacks?.Find(hero.SpritePackName)?.WalkFrames ?? hero.Definition.WalkFrames;

        internal int RunFramesFor(Hero hero) => SpritePacks?.Find(hero.SpritePackName)?.RunFrames ?? hero.Definition.RunFrames;
        internal int DrawFramesFor(Hero hero) => SpritePacks?.Find(hero.SpritePackName)?.DrawFrames ?? hero.Definition.DrawFrames;
        internal int BattleFramesFor(Hero hero) => SpritePacks?.Find(hero.SpritePackName)?.BattleFrames ?? hero.Definition.BattleFrames;

        CommandResult HandleSelectSpritePack(Game game, SelectSpritePack command)
        {
            if (command.Hero < 0 || command.Hero >= heroes.Count) return CommandResult.Reject("There is no hero " + command.Hero + " in the party.");
            if (!string.IsNullOrEmpty(command.Pack) && SpritePacks?.Find(command.Pack) == null)
                return CommandResult.Reject("There is no sprite pack called \"" + command.Pack + "\".");
            var hero = heroes[command.Hero];
            hero.SpritePackName = string.IsNullOrEmpty(command.Pack) ? null : command.Pack;
            hero.Frame = 0;
            return CommandResult.Accept(new SpritePackChanged(game.Tick, command.Hero, hero.SpritePackName));
        }

        /// <summary>Registers <see cref="SelectSpritePack"/> (called once from <see cref="Install"/>).</summary>
        void InstallSpritePacks(Game game) => game.Register<SelectSpritePack>(HandleSelectSpritePack);
    }
}
