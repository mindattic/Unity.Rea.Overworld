using System;
using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>
    /// Cycles the weapon equipped on hero.mdb's own hero, Alexander, among its own four (RECONSTRUCTION 16.3,
    /// U.OVW-T34): -1 the previous, +1 the next, wrapping.
    /// </summary>
    public sealed class CycleEquippedWeapon : ICommand
    {
        public CycleEquippedWeapon(PlayerId player, int step) { Player = player; Step = step; }
        public PlayerId Player { get; }
        public int Step { get; }
    }

    public sealed class EquippedWeaponChanged : IGameEvent
    {
        public EquippedWeaponChanged(long tick, PlayerId player, int weaponId) { Tick = tick; Player = player; WeaponId = weaponId; }
        public long Tick { get; }
        public PlayerId Player { get; }
        public int WeaponId { get; }
    }

    /// <summary>
    /// The hero database's own hero, Alexander, and the four weapons he can be equipped with (RECONSTRUCTION 16.3;
    /// BIBLE "Equipping Alexander", U.OVW-T34): the compiled engine never read hero.mdb at all, so there is nothing to
    /// resurrect here beyond the data being real and switchable rather than dead weight. The hero screen (Page Up)
    /// shows Alexander beside the party while it is open; <see cref="CycleEquippedWeapon"/> (Left / Right, while the
    /// hero screen is open) steps the weapon worn. The head, body, arms and feet armour slots, and the relic slot,
    /// have no items at all in the original (every one of those tables is empty) and are shown as empty rather than
    /// invented; a later ticket that gives the owner armour or relics to design would fill them.
    /// </summary>
    public sealed class Equipment
    {
        readonly HeroDatabase database;
        readonly Dictionary<PlayerId, int> equipped = new Dictionary<PlayerId, int>();

        public Equipment(HeroDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>hero.mdb's one hero (Alexander), or null if somehow absent.</summary>
        public HeroRecord Hero => database.Heroes.Count > 0 ? database.Heroes[0] : null;

        /// <summary>The weapon <paramref name="player"/> has equipped (the hero's own starting weapon until changed).</summary>
        public WeaponRecord WeaponOf(PlayerId player)
        {
            var id = equipped.TryGetValue(player, out var w) ? w : Hero?.WeaponId ?? 0;
            return database.Weapons.Find(x => x.Id == id);
        }

        public Equipment Install(Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            game.Register<CycleEquippedWeapon>(HandleCycle);
            return this;
        }

        CommandResult HandleCycle(Game game, CycleEquippedWeapon command)
        {
            if (database.Weapons.Count == 0) return CommandResult.Reject("There is nothing to equip.");
            var current = WeaponOf(command.Player);
            var index = current != null ? database.Weapons.IndexOf(current) : 0;
            var next = ((index + command.Step) % database.Weapons.Count + database.Weapons.Count) % database.Weapons.Count;
            var weaponId = database.Weapons[next].Id;
            equipped[command.Player] = weaponId;
            return CommandResult.Accept(new EquippedWeaponChanged(game.Tick, command.Player, weaponId));
        }
    }
}
