using System;
using ShareCore.Data;
using UnityEngine;

namespace ShareCore.Scripts.Data
{
    [Serializable]
    public class SpecialCellVisualPrefabSlot
    {
        public BoardSpecialType Type;
        public GameObject Prefab;
    }
}
