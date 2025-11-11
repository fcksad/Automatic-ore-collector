using UnityEngine;

namespace Builder
{
    public enum RotationMode { Any, YawOnly, Snap90 }

    [System.Serializable]
    public class ModuleOccupancyMask
    {
        [Tooltip("Клетки, которые занимает модуль в ЛОКАЛЬНЫХ координатах грид-а. Для простого 1x1x1 блока достаточно (0,0,0).")]
        public Vector3Int[] LocalCells = { Vector3Int.zero };
    }


    [CreateAssetMenu(fileName = "Module", menuName = "Configs/Vehicle/Module")]
    public class ModuleConfig : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public GameObject Prefab { get; private set; }

        [Header("Поворот")]
        public RotationMode RotationMode = RotationMode.Snap90;

        [Header("Грид")]
        public float CellSize = 0.25f;        
        public Vector3Int GridSize = Vector3Int.one; // сколько клеток примерно занимает блок (пока просто для инфы)

        [Header("Занятые клетки")]
        public ModuleOccupancyMask Occupancy = new ModuleOccupancyMask();
    }
}
