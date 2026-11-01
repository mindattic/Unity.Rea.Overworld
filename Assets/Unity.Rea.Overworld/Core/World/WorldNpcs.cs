using System;
using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>One placed NPC, drawn (RECONSTRUCTION 16.2, U.OVW-T31).</summary>
    public sealed class NpcView
    {
        public int NpcId { get; internal set; }
        public string Name { get; internal set; }
        public float X { get; internal set; }
        public float Y { get; internal set; }
        public float Width { get; internal set; }
        public float Height { get; internal set; }

        /// <summary>"stand" or "pursue", the database's own word (Area_Npcs.npcSTATE).</summary>
        public string State { get; internal set; }

        /// <summary>The NPC's own sprite folder (Npc.Directory, e.g. "gfx/npc/woman/").</summary>
        public string Directory { get; internal set; }

        /// <summary>The picture shown now, under <see cref="Directory"/>.</summary>
        public string Sprite { get; internal set; }
    }

    /// <summary>
    /// Area_Npcs placed in the playable world (RECONSTRUCTION 16.2; BIBLE "NPCs met"; U.OVW-T31): the Peasant Woman
    /// (standing, its front picture) and the Magitek Armor (pursuing). The original never wrote any code reading these
    /// tables at all (no behaviour, no animation), so there is nothing to port for an NPC's walk cycle; the owner's own
    /// figure stands in for one, cycling a pursuing NPC's walk-right frames in place to read as alert and active (BIBLE),
    /// distinct from a standing one's still front picture. Chasing the party (what "pursue" would actually do) is the
    /// NPC encounters story (U.OVW-T32), not this one, which only places and shows them: the ticket's own "I meet" scope.
    /// </summary>
    public sealed class WorldNpcs : ITickSystem
    {
        /// <summary>Seconds per walk frame for a pursuing NPC (the owner's figure; the original never animated these).</summary>
        public const float WalkRate = 0.15f;

        public const int WalkFrames = 4;

        readonly List<NpcView> npcs = new List<NpcView>();
        readonly double[] lastFrameTime;
        readonly int[] frame;

        public WorldNpcs(WorldData world, int areaId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            foreach (var a in world.AreaNpcs)
            {
                if (a.AreaId != areaId) continue;
                var npc = world.FindNpc(a.NpcId);
                if (npc == null) continue;
                npcs.Add(new NpcView
                {
                    NpcId = npc.Id,
                    Name = npc.Name,
                    X = a.X,
                    Y = a.Y,
                    Width = npc.Width,
                    Height = npc.Height,
                    State = a.State,
                    Directory = npc.Directory,
                    Sprite = npc.Directory + (a.State == "pursue" ? "npc_walk_right_1.PNG" : "npc_front.PNG"),
                });
            }

            lastFrameTime = new double[npcs.Count];
            frame = new int[npcs.Count];
        }

        public IReadOnlyList<NpcView> Npcs => npcs;

        public WorldNpcs Install(Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            game.AddSystem(this);
            return this;
        }

        public void Step(Game game, long tick, ICollection<IGameEvent> events)
        {
            if (game.IsHeld) return;
            var now = tick * Clock.TickSeconds;
            for (var i = 0; i < npcs.Count; i++)
            {
                var npc = npcs[i];
                if (npc.State != "pursue") continue;
                if (now + 1e-9 < lastFrameTime[i] + WalkRate) continue;
                frame[i] = (frame[i] + 1) % WalkFrames;
                lastFrameTime[i] = now;
                npc.Sprite = npc.Directory + "npc_walk_right_" + (frame[i] + 1) + ".PNG";
            }
        }
    }
}
