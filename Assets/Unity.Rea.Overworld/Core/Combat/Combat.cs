using System;
using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>A hero's weapon connected with an enemy, for the amount it took off (RECONSTRUCTION 12.4, U.OVW-T33).</summary>
    public sealed class EnemyHit : IGameEvent
    {
        public EnemyHit(long tick, PlayerId player, int hero, int enemyId, int damage, int health, int maxHealth)
        {
            Tick = tick;
            Player = player;
            Hero = hero;
            EnemyId = enemyId;
            Damage = damage;
            Health = health;
            MaxHealth = maxHealth;
        }

        public long Tick { get; }
        public PlayerId Player { get; }
        public int Hero { get; }
        public int EnemyId { get; }
        public int Damage { get; }
        public int Health { get; }
        public int MaxHealth { get; }
        public bool Defeated => Health <= 0;
    }

    /// <summary>
    /// Combat.vb that was never written (RECONSTRUCTION 12.4; BIBLE "Combat", U.OVW-T33): the original's `Enemy` table
    /// gave every monster a class and every weapon a slashing / piercing / blunt split and a rating, but no code ever
    /// turned them into a hit, so every number below is a documented decision, kept here as one place rather than
    /// scattered tuning constants. A hit's damage is the weapon's <see cref="WeaponDefinition.Rating"/> times the
    /// weighted average of its slashing, piercing and blunt split against the target class's own multiplier for each
    /// (1.5 the class is vulnerable to it, 1 neutral, 0.75 or 0.5 it resists it), rounded to the nearest whole point and
    /// never below 1 (a hit always does something): <c>round(rating * (slashing * classSlashing + piercing *
    /// classPiercing + blunt * classBlunt) / (slashing + piercing + blunt))</c>. The weapon's own durability (the hero's
    /// own copy of it, <see cref="Hero.WeaponDurability"/>, since the shared <see cref="WeaponDefinition"/> is read-only
    /// content) drops by 1 every hit, never below 0; a durability of 0 still swings (no further penalty is modelled,
    /// flagged for a later ticket if the owner wants one).
    /// </summary>
    public static class Combat
    {
        /// <summary>One class's multiplier against each of the three damage types.</summary>
        public readonly struct ClassMultiplier
        {
            public ClassMultiplier(float slashing, float piercing, float blunt)
            {
                Slashing = slashing;
                Piercing = piercing;
                Blunt = blunt;
            }

            public float Slashing { get; }
            public float Piercing { get; }
            public float Blunt { get; }
        }

        /// <summary>
        /// The owner's own table (there being no original to port): animals are hunted with piercing weapons and shrug
        /// off blunt blows; insects are crushed, not cut; paranormal slimes split under an edge and only properly yield
        /// to being battered; reptiles are vulnerable to a blade across scales but absorb a blunt hit; the mechanical
        /// Magitek's armour turns a blade but not a hammer.
        /// </summary>
        static readonly Dictionary<string, ClassMultiplier> Classes = new Dictionary<string, ClassMultiplier>(StringComparer.Ordinal)
        {
            ["Animal"] = new ClassMultiplier(1.0f, 1.25f, 0.75f),
            ["Insect"] = new ClassMultiplier(0.75f, 0.75f, 1.25f),
            ["Paranormal"] = new ClassMultiplier(0.5f, 0.5f, 1.5f),
            ["Reptile"] = new ClassMultiplier(1.25f, 1.0f, 0.75f),
            ["Mechanical"] = new ClassMultiplier(0.5f, 0.75f, 1.25f),
        };

        /// <summary>The damage <paramref name="weapon"/> does to a target of <paramref name="targetClass"/>, at least 1.</summary>
        public static int Damage(WeaponDefinition weapon, string targetClass)
        {
            if (weapon == null) return 0;
            var split = weapon.Slashing + weapon.Piercing + weapon.Blunt;
            if (!(split > 0f)) return Math.Max(1, (int)Math.Round(weapon.Rating, MidpointRounding.AwayFromZero));
            var m = Classes.TryGetValue(targetClass, out var found) ? found : new ClassMultiplier(1f, 1f, 1f);
            var multiplier = (weapon.Slashing * m.Slashing + weapon.Piercing * m.Piercing + weapon.Blunt * m.Blunt) / split;
            return Math.Max(1, (int)Math.Round(weapon.Rating * multiplier, MidpointRounding.AwayFromZero));
        }
    }
}
