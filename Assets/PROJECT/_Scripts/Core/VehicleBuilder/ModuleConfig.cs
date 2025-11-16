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
        public Vector3Int GridSize = Vector3Int.one; 

        [Header("Занятые клетки")]
        public ModuleOccupancyMask Occupancy = new ModuleOccupancyMask();


#if UNITY_EDITOR
        [ContextMenu("Rebuild Occupancy (Full Grid)")]
        private void RebuildOccupancyFull()
        {
            GridSize.x = Mathf.Max(1, GridSize.x);
            GridSize.y = Mathf.Max(1, GridSize.y);
            GridSize.z = Mathf.Max(1, GridSize.z);

            int total = GridSize.x * GridSize.y * GridSize.z;

            var cells = new Vector3Int[total];
            int i = 0;

            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    for (int z = 0; z < GridSize.z; z++)
                    {
                        cells[i++] = new Vector3Int(x, y, z);
                    }
                }
            }

            if (Occupancy == null)
                Occupancy = new ModuleOccupancyMask();

            Occupancy.LocalCells = cells;

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ModuleConfig] RebuildOccupancyFull для '{name}', {total} клеток.");
        }
#endif
    }
}

