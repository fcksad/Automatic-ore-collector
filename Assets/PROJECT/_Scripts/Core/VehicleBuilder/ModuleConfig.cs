using UnityEngine;

namespace Builder
{
    public enum RotationMode { None, Any, HorizontalOnly, VerticalOnly}

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

        //вес модуля

        //броня(хп)

        //грузоподьемность

        //для колес отдельные характеристики как и для оружий так и для кузова(типо сколько туррелей поддерживает и тд или урон от блока, типо бамбер с шипаим

        //

        [Header("Поворот")]
        public RotationMode RotationMode;

        [Header("Грид")]
        public float CellSize = 0.25f;        
        public Vector3Int GridSize = Vector3Int.one; 

        [Header("Занятые клетки")]
        public ModuleOccupancyMask Occupancy = new ModuleOccupancyMask();
    }
}
