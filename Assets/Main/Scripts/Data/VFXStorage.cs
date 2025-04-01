using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "VFXStorage", menuName = "TowerDefense/VFXStorage")]
public class VFXStorage : ScriptableObject
{
    public List<VFXData> vfxList = new List<VFXData>();

    public VFXData GetVFXData(string name)
    {
        return vfxList.FirstOrDefault(vfx => vfx.name == name);
    }
}