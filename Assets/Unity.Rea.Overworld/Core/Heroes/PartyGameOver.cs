using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>Root.Reset's part of the party (RECONSTRUCTION 6.5, U.OVW-T28).</summary>
    public sealed partial class Party
    {
        /// <summary>
        /// Unlike the original, which restored nothing (so the game kept calling Reset every frame while the screen
        /// stayed black; BIBLE "Death and Reset"), every hero is restored to full health, mana and stamina, its start
        /// position, facing, mode and AI state, and the game speed is set back to 1 (Root.Reset's <c>gameSpeed = 1</c>).
        /// </summary>
        internal void RestoreAfterGameOver(long tick, ICollection<IGameEvent> events)
        {
            GameSpeed = 1f;
            // Every hero goes back to adventure mode below, so every battle ring shuts as leaving battle shuts it (left
            // open, its arrows kept marking only and the move button did nothing, holding the reset leader still).
            foreach (var ring in rings.Values)
            {
                ring.Open = false;
                ring.ShowMove = false;
                ring.ShowAttack = false;
                ring.NoOneReady = false;
            }

            foreach (var hero in heroes)
            {
                var def = hero.Definition;
                events.Add(Place(hero.Index, def.Start.X, def.Start.Y, tick));

                var vitalsBefore = (hero.Health, hero.Mana, hero.Stamina);
                hero.Health = hero.MaxHealth;
                hero.Mana = hero.MaxMana;
                hero.Stamina = hero.MaxStamina;
                if ((hero.Health, hero.Mana, hero.Stamina) != vitalsBefore)
                    events.Add(new HeroVitalsChanged(tick, hero.Index, hero.Health, hero.Mana, hero.Stamina));

                var statusBefore = hero.Status;
                hero.Status = HeroStatus.Fine;
                if (hero.Status != statusBefore) events.Add(new HeroStatusChanged(tick, hero.Index, hero.Status, statusBefore));

                hero.Kneeling = false;
                hero.Laying = false;
                hero.PickingUp = false;
                hero.Running = false;
                hero.MoveLeft = hero.MoveRight = hero.MoveUp = hero.MoveDown = false;
                hero.BattleAction = BattleAction.Ready;

                var stateBefore = hero.State;
                var facingBefore = hero.Facing;
                hero.State = def.State;
                hero.Facing = def.Facing;
                hero.Mode = def.Mode;
                hero.Frame = 0;
                if (hero.State != stateBefore || hero.Facing != facingBefore) events.Add(new HeroStateChanged(tick, hero.Index, hero.State, hero.Facing));
            }
        }
    }
}
