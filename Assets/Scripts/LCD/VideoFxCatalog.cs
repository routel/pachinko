using System;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "Pachinko/VideoFxCatalog", fileName = "VideoFxCatalog")]
public class VideoFxCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;
        public VideoClip clip;

        [Header("Video")]
        public bool loopByDefault = false;

        [Header("Reach Timing")]
        [Tooltip("動画開始から何秒後にPUSHを出すか")]
        public float pushDelay = 1.8f;

        [Tooltip("動画を止めるときのフェード秒")]
        public float stopFade = 0.12f;

        [Tooltip("動画をスロットより前に出すか")]
        public bool videoAboveSlot = false;
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
