using UnityEngine;

[System.Serializable]
public class VFXData
{
    public string name;
    public GameObject vfxPrefab;
    public float destroyDelay = 2f; // Optional: Time before destroying the instantiated VFX
}