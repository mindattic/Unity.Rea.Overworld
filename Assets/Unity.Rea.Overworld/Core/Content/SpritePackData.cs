using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>
    /// One selectable alternate sprite pack (RECONSTRUCTION 4.4; BIBLE "Selectable sprite packs", U.OVW-T35): the
    /// original's own unused Belmont and girl sets, read in exactly the shape heroes.json's own "sprites" are (so the
    /// same <see cref="HeroSprites"/> the default pack uses draws these too), each with its own frame counts, since
    /// neither set has anywhere near Chrono's 24. Kneel, sleep, draw and battle poses neither set ever had fall back to
    /// the nearest one that exists (stand, or Belmont's own attack cycle for both draw and battle), not invented art.
    /// </summary>
    public sealed class SpritePackDefinition
    {
        public string Name { get; internal set; }
        public HeroSprites Sprites { get; internal set; }
        public int WalkFrames { get; internal set; }
        public int RunFrames { get; internal set; }
        public int DrawFrames { get; internal set; }
        public int BattleFrames { get; internal set; }
    }

    public sealed class SpritePackData
    {
        public IReadOnlyList<SpritePackDefinition> Packs { get; internal set; }

        public SpritePackDefinition Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var p in Packs) if (p.Name == name) return p;
            return null;
        }

        internal static SpritePackData Read(IContentSource source, string file, List<string> errors)
        {
            var root = ContentFiles.Open(source, file, errors);
            if (root == null) return null;
            var packs = new List<SpritePackDefinition>();
            foreach (var r in root.Objects("packs"))
            {
                var walkFrames = r.Integer("walkFrames", 0);
                var runFrames = r.Integer("runFrames", 0);
                var drawFrames = r.Integer("drawFrames", 0);
                var battleFrames = r.Integer("battleFrames", 0);
                var pack = new SpritePackDefinition
                {
                    Name = r.Text("name"),
                    WalkFrames = walkFrames,
                    RunFrames = runFrames,
                    DrawFrames = drawFrames,
                    BattleFrames = battleFrames,
                    Sprites = HeroSprites.Read(r.Section("sprites"), walkFrames, runFrames, drawFrames, battleFrames),
                };
                r.Done();
                packs.Add(pack);
            }

            root.Done();
            return new SpritePackData { Packs = packs };
        }
    }
}
