using System.Collections.Generic;

namespace MindAttic.Overworld.Core
{
    /// <summary>
    /// One Cutscene.vb record (compiled but unreachable; RECONSTRUCTION 18, U.OVW-T36): a name, title, start/end
    /// script and objective, nothing ever playing it. <c>loadCutscenes</c> set only one, named "Hello", and left its
    /// other fields unassigned; they are read here as empty, not invented.
    /// </summary>
    public sealed class CutsceneDefinition
    {
        public string Name { get; internal set; }
        public string Title { get; internal set; }
        public string StartScript { get; internal set; }
        public string EndScript { get; internal set; }
        public string Objective { get; internal set; }
    }

    /// <summary>Cutscene.vb's own record, kept as data so the hook is not dropped (U.OVW-T36).</summary>
    public sealed class CutsceneData
    {
        public IReadOnlyList<CutsceneDefinition> Cutscenes { get; internal set; }

        public CutsceneDefinition Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var c in Cutscenes) if (c.Name == name) return c;
            return null;
        }

        internal static CutsceneData Read(IContentSource source, string file, List<string> errors)
        {
            var root = ContentFiles.Open(source, file, errors);
            if (root == null) return null;
            var list = new List<CutsceneDefinition>();
            foreach (var r in root.Objects("cutscenes"))
            {
                var c = new CutsceneDefinition
                {
                    Name = r.Text("name"),
                    Title = r.Text("title", allowEmpty: true),
                    StartScript = r.Text("startScript", allowEmpty: true),
                    EndScript = r.Text("endScript", allowEmpty: true),
                    Objective = r.Text("objective", allowEmpty: true),
                };
                if (list.Exists(x => x.Name == c.Name)) r.Fail("name", "\"" + c.Name + "\" is defined twice");
                r.Done();
                list.Add(c);
            }

            root.Done();
            return new CutsceneData { Cutscenes = list };
        }
    }
}
