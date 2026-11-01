using System.Collections.Generic;
using UnityEngine;

namespace MindAttic.Overworld.Runtime
{
    /// <summary>
    /// The original's sound files, by the path they have in the original's sfx folder ("sfx/hero/regen.wav"). They are
    /// imported verbatim under Audio/Resources at the same path, as <see cref="TextureLibrary"/> imports the pictures
    /// under Art/Resources, so a data file names a clip once and the game finds it.
    /// </summary>
    public sealed class AudioLibrary
    {
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        /// <summary>The clip at <paramref name="file"/>, or null (logged) when it was not imported.</summary>
        public AudioClip Load(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (cache.TryGetValue(file, out var clip)) return clip;
            clip = Resources.Load<AudioClip>(TextureLibrary.ResourcePath(file));
            if (clip == null) Debug.LogError("Unity.Overworld: the sound " + file + " is not imported (Audio/Resources/" + file + ").");
            cache[file] = clip;
            return clip;
        }
    }
}
