using System;
using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>One placed enemy, drawn and moved (RECONSTRUCTION 16.2, U.OVW-T32).</summary>
    public sealed class EnemyView
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public float X { get; internal set; }
        public float Y { get; internal set; }
        public int Health { get; internal set; }
        public int MaxHealth { get; internal set; }
        public float Speed { get; internal set; }
        public float Range { get; internal set; }

        /// <summary>Passive, Aggressive, Defensive or Afraid, the database's own word (Enemy.enemySTATE).</summary>
        public string State { get; internal set; }

        /// <summary>Walking, Flying or Hopping, the database's own word (Enemy.enemyMOTION); moves the same way regardless (BIBLE).</summary>
        public string Motion { get; internal set; }

        public string Class { get; internal set; }

        /// <summary>The picture shown now.</summary>
        public string Sprite { get; internal set; }

        /// <summary>True once <see cref="State"/> Defensive has been approached within half its range and turned aggressive (RECONSTRUCTION 16.2 "Enemy AI states").</summary>
        internal bool Triggered;

        internal string StandLeft, StandRight;
        internal IReadOnlyList<string> WalkFrames;
        internal int Frame;
        internal double LastFrameTime;
        internal bool FacingLeft;
        internal double LastAttackTime = double.NegativeInfinity;
    }

    /// <summary>
    /// The `Enemy` table's five enemies, met in the Grasslands (RECONSTRUCTION 16.2; BIBLE "Enemy AI states",
    /// "Checked and not changed... the five enemies with no placement"; U.OVW-T32). `Area_Enemies` is empty (the
    /// original shipped no placement for any of them), so where each stands is this ticket's own decision, spread along
    /// the Grasslands' ground (RECONSTRUCTION 16.1, U.OVW-US-A28). Each moves by its data: Passive ignores the party;
    /// Aggressive moves toward the leader within its range; Defensive holds until the leader comes within half its
    /// range, then (having nothing else to do without the combat story, A29) does the same as Aggressive; Afraid moves
    /// away once the leader is within range. Motion (Walking, Flying, Hopping) does not change how it moves, only what
    /// it would have looked like with art this rebuild does not have (BIBLE). Werelynx draws the catman set (stand, walk,
    /// facing left or right); Blue Slime and Red Slime draw their own coloured picture, the generic slime set having
    /// no colour variants to animate; Botherfly and Toadie, whose `Enemy_Images` rows (and all art) were never shipped
    /// either, draw the slime's plain picture as a placeholder, flagged here for replacement once real art exists.
    /// </summary>
    public sealed class WorldEnemies : ITickSystem
    {
        public const float WalkRate = 0.1f;
        public const int WalkFrameCount = 4;

        /// <summary>How close an acting enemy must be to land a hit (U.OVW-T33: "being attacked"), and how often while it stays there (the Poison Box's own rate, 0.5 s).</summary>
        public const float ContactRange = 0.1f;

        public const float AttackRate = 0.5f;

        readonly List<EnemyView> enemies = new List<EnemyView>();
        readonly Party party;

        public WorldEnemies(WorldData world, Party party)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            this.party = party ?? throw new ArgumentNullException(nameof(party));

            // The owner's own placements (BIBLE): spread along the Grasslands' ground (y -0.2, the NPCs' own height),
            // away from the start, the NPCs and each other.
            var placements = new (string Name, float X)[]
            {
                ("Werelynx", 6.5f),
                ("Botherfly", 8f),
                ("Blue Slime", 9.5f),
                ("Red Slime", 11f),
                ("Toadie", 13f),
            };

            foreach (var (name, x) in placements)
            {
                var e = world.Enemies.Find(r => r.Name == name);
                if (e == null) continue;
                var view = new EnemyView
                {
                    Id = e.Id,
                    Name = e.Name,
                    X = x,
                    Y = -0.2f,
                    Health = e.MaxHealth,
                    MaxHealth = e.MaxHealth,
                    Speed = e.MoveSpeed,
                    Range = e.Range,
                    State = e.State,
                    Motion = e.Motion,
                    Class = e.Class,
                    FacingLeft = true,
                };
                SetArt(view);
                view.Sprite = view.StandLeft;
                enemies.Add(view);
            }
        }

        static void SetArt(EnemyView view)
        {
            switch (view.Name)
            {
                case "Werelynx":
                    view.StandLeft = "gfx/enemy/catman/stand_left.PNG";
                    view.StandRight = "gfx/enemy/catman/stand_right.PNG";
                    view.WalkFrames = null; // set per facing in Step
                    break;
                case "Blue Slime":
                    view.StandLeft = view.StandRight = "gfx/enemy/slime_blue.PNG";
                    break;
                case "Red Slime":
                    view.StandLeft = view.StandRight = "gfx/enemy/slime_red.PNG";
                    break;
                default:
                    // Botherfly, Toadie: no art ever shipped for either (BIBLE); the slime's plain picture stands in.
                    view.StandLeft = view.StandRight = "gfx/enemy/slime/slime_1.PNG";
                    break;
            }
        }

        public IReadOnlyList<EnemyView> Enemies => enemies;

        /// <summary>The nearest living enemy within <paramref name="range"/> of (x, y), or null (U.OVW-T33, the ring menu's Attack).</summary>
        public EnemyView NearestInRange(float x, float y, float range)
        {
            EnemyView nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e.Health <= 0) continue;
                var d = Distance(x, y, e.X, e.Y);
                if (d <= range && d < nearestDistance)
                {
                    nearest = e;
                    nearestDistance = d;
                }
            }

            return nearest;
        }

        public WorldEnemies Install(Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            game.AddSystem(this);
            return this;
        }

        public void Step(Game game, long tick, ICollection<IGameEvent> events)
        {
            if (game.IsHeld) return;
            var now = tick * Clock.TickSeconds;

            foreach (var enemy in enemies)
            {
                if (enemy.Health <= 0) continue; // defeated (U.OVW-T33): holds still, drawn no more
                Hero target = null;
                PlayerId targetPlayer = default;
                var nearestDistance = float.MaxValue;
                foreach (var player in game.Players)
                {
                    var leader = party.Leader(player);
                    if (leader == null) continue;
                    var d = Distance(enemy.X, enemy.Y, leader.X, leader.Y);
                    if (target == null || d < nearestDistance)
                    {
                        target = leader;
                        targetPlayer = player;
                        nearestDistance = d;
                    }
                }

                var moving = false;
                var acting = false;
                if (target != null)
                {
                    var distance = Distance(enemy.X, enemy.Y, target.X, target.Y);
                    switch (enemy.State)
                    {
                        case "Aggressive":
                            acting = true;
                            moving = distance <= enemy.Range;
                            if (moving) MoveToward(enemy, target.X, target.Y);
                            break;
                        case "Afraid":
                            moving = distance <= enemy.Range;
                            if (moving) MoveAway(enemy, target.X, target.Y);
                            break;
                        case "Defensive":
                            if (!enemy.Triggered && distance <= enemy.Range / 2f) enemy.Triggered = true;
                            acting = enemy.Triggered;
                            moving = enemy.Triggered && distance <= enemy.Range;
                            if (moving) MoveToward(enemy, target.X, target.Y);
                            break;
                        default: // Passive: ignores the party
                            break;
                    }

                    // Being attacked (U.OVW-T33): an acting enemy within contact range hits the target, at the Poison
                    // Box's own rate, so retreating out of contact (or past Range, which drops it back to idle) ends it.
                    if (acting && distance <= ContactRange && target.Health > 0 &&
                        now + 1e-9 >= enemy.LastAttackTime + AttackRate)
                    {
                        target.Health = Math.Max(0, target.Health - 1);
                        enemy.LastAttackTime = now;
                        events.Add(new HeroVitalsChanged(tick, target.Index, target.Health, target.Mana, target.Stamina));
                    }
                }

                Animate(enemy, now, moving);
            }
        }

        static float Distance(float x1, float y1, float x2, float y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        static void MoveToward(EnemyView e, float x, float y) => Step(e, x - e.X, y - e.Y);
        static void MoveAway(EnemyView e, float x, float y) => Step(e, e.X - x, e.Y - y);

        static void Step(EnemyView e, float dx, float dy)
        {
            var length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1e-6f) return;
            e.FacingLeft = dx < 0;
            e.X += dx / length * e.Speed;
            e.Y += dy / length * e.Speed;
        }

        void Animate(EnemyView e, double now, bool moving)
        {
            if (e.Name != "Werelynx")
            {
                e.Sprite = e.FacingLeft ? e.StandLeft : e.StandRight;
                return;
            }

            if (!moving)
            {
                e.Sprite = e.FacingLeft ? e.StandLeft : e.StandRight;
                e.Frame = 0;
                return;
            }

            if (now + 1e-9 >= e.LastFrameTime + WalkRate)
            {
                e.Frame = (e.Frame + 1) % WalkFrameCount;
                e.LastFrameTime = now;
            }

            var side = e.FacingLeft ? "left" : "right";
            e.Sprite = "gfx/enemy/catman/walk_" + side + "_" + (e.Frame + 1) + ".PNG";
        }
    }
}
