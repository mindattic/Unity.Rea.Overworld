using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>
    /// One engine spell (Magic.vb, compiled but unreachable; RECONSTRUCTION 18, U.OVW-T36): the mana cost, the range
    /// from the caster the original gave it and how long it holds before clearing.
    /// </summary>
    public sealed class SpellDefinition
    {
        public string Name { get; internal set; }
        public string School { get; internal set; }
        public string Description { get; internal set; }
        public int Cost { get; internal set; }
        public float Range { get; internal set; }
        public float Duration { get; internal set; }
    }

    /// <summary>
    /// Magic.vb's five spell schools and its one defined spell, Flame, kept as content so the Skill ring entry (which
    /// the original left doing nothing at all) has something to cast (U.OVW-T36; BIBLE "Skill ring entry").
    /// </summary>
    public sealed class MagicData
    {
        public IReadOnlyList<string> Schools { get; internal set; }
        public IReadOnlyList<SpellDefinition> Spells { get; internal set; }

        public SpellDefinition Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var s in Spells) if (s.Name == name) return s;
            return null;
        }

        internal static MagicData Read(IContentSource source, string file, List<string> errors)
        {
            var root = ContentFiles.Open(source, file, errors);
            if (root == null) return null;
            var schools = root.Texts("schools");
            var spells = new List<SpellDefinition>();
            foreach (var r in root.Objects("spells"))
            {
                var spell = new SpellDefinition
                {
                    Name = r.Text("name"),
                    School = r.Text("school"),
                    Description = r.Text("description"),
                    Cost = r.Integer("cost", 0),
                    Range = r.Positive("range"),
                    Duration = r.Positive("duration"),
                };
                if (!schools.Contains(spell.School)) r.Fail("school", "\"" + spell.School + "\" is not one of the schools");
                if (spells.Exists(x => x.Name == spell.Name)) r.Fail("name", "\"" + spell.Name + "\" is defined twice");
                r.Done();
                spells.Add(spell);
            }

            root.Done();
            return new MagicData { Schools = schools, Spells = spells };
        }
    }
}
