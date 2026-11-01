namespace MindAttic.Overworld.Core
{
    /// <summary>A hero cast a spell from the Skill ring entry (U.OVW-T36): where it appeared and how long it holds.</summary>
    public sealed class SpellCast : IGameEvent
    {
        public SpellCast(long tick, PlayerId player, int hero, string spell, float x, float y, float duration)
        {
            Tick = tick;
            Player = player;
            Hero = hero;
            Spell = spell;
            X = x;
            Y = y;
            Duration = duration;
        }

        public long Tick { get; }
        public PlayerId Player { get; }
        public int Hero { get; }
        public string Spell { get; }
        public float X { get; }
        public float Y { get; }
        public float Duration { get; }
    }

    /// <summary>
    /// The Skill ring entry (RECONSTRUCTION 12.2; BIBLE "Skill ring entry", U.OVW-T36): <c>checkRingEffect</c> left it
    /// doing nothing at all, same as Attack before U.OVW-T33. With Magic.vb's one defined spell, Flame, now kept as
    /// content (<see cref="MagicData"/>), choosing Skill casts it at the caster's own point (the original never gave
    /// "a given point" an aim, so none is invented) when its mana allows; the other four schools stay a taxonomy with
    /// no spell of their own, as the source left them.
    /// </summary>
    public sealed partial class Party
    {
        /// <summary>The spells a hero may cast with the Skill ring entry; null: Skill always refused, as the original left it.</summary>
        public MagicData Magic { get; set; }
    }
}
