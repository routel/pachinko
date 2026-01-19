using System;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "Pachinko/VideoFxCatalog", fileName = "VideoFxCatalog")]
public class VideoFxCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;            // —á: "reach_normal", "reach_strong", "win", "lose"
        public VideoClip clip;        // mp4 import‚µ‚½VideoClip
        public bool loopByDefault = true;
    }

    public Entry[] entries;

    public bool TryGet(string key, out Entry entry)
    {
        if (entries != null)
        {
            foreach (var e in entries)
            {
                if (e != null && e.key == key)
                {
                    entry = e;
                    return true;
                }
            }
        }
        entry = null;
        return false;
    }
}
