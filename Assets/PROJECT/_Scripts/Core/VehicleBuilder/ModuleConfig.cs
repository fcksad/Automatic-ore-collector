using System.Collections.Generic;
using UnityEngine;
namespace Builder
{

    public enum RotationMode { Any, YawOnly, Snap90 }

    [CreateAssetMenu(fileName = "Module", menuName = "Configs/Vehicle/Module")]
    public class ModuleConfig : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public GameObject Prefab { get; private set; }


        [Header("Stats")]
        [field: SerializeField] public int PowerDraw { get; private set; }
        [field: SerializeField] public int PowerSupply { get; private set; }
        [field: SerializeField] public float Mass { get; private set; }
        [field: SerializeField] public float HP { get; private set; }
        [field: SerializeField] public float Armor { get; private set; }
        [field: SerializeField] public float CabinPower { get; private set; }
        [field: SerializeField] public int Cost { get; private set; }
        [field: SerializeField] public int Energy { get; private set; }

        [Header("Placement")]
        [field: SerializeField] public float CellSize { get; private set; } = 0.25f;
        public Vector3Int SizeCells = new Vector3Int(2, 1, 2);
        [field: SerializeField] public RotationMode AllowedRotations { get; private set; } = RotationMode.Snap90;  
        [field: SerializeField] public Vector3Int ForwardAxis { get; private set; } = new(0, 0, 1);

        [Tooltip("Какие клетки занимает модуль (в локальных координатах, в единицах клеток)")]
        [field: SerializeField] public Vector3Int[] FootprintCells;

        [Tooltip("Минимум контактов с уже построенной конструкцией")]
        [field: SerializeField] public int MinContactCells { get; private set; } = 1;

        public void SetRectFootprint(int sx, int sy, int sz)
        {
            var list = new List<Vector3Int>(sx * sy * sz);
            for (int y = 0; y < sy; y++)
                for (int z = 0; z < sz; z++)
                    for (int x = 0; x < sx; x++)
                        list.Add(new Vector3Int(x, y, z));
            FootprintCells = list.ToArray();
        }

    }
}
