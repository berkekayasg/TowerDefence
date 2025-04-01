using UnityEngine;

[System.Serializable]
public class SoundClipData
{
    public string name = "New Sound"; // Name to reference the sound by
    public AudioClip clip;           // The actual audio clip
    [Range(0f, 2f)]
    public float volumeScale = 1f; // Allows adjusting volume relative to the global effects volume
    [Range(0.1f, 3f)]
    public float pitch = 1f;
    public bool loop = false;
}