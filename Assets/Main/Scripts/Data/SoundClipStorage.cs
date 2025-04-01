using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "SoundClipStorage", menuName = "TowerDefense/Sound Clip Storage", order = 100)]
public class SoundClipStorage : ScriptableObject
{
    public List<SoundClipData> soundClips = new List<SoundClipData>();

    public SoundClipData GetSoundClipData(string name)
    {
        return soundClips.FirstOrDefault(clip => clip.name == name);
    }
}