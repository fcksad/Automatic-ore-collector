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

        [Header("Rotateion")]
        public RotationMode RotationMode = RotationMode.Snap90;

        [Header("HightOffset")]
        public float MountHeight = 0.5f;

        [Tooltip("Box Checker")]
        public Vector3 BoundsSize = new Vector3(1f, 1f, 1f);
        [Tooltip("Клетки модуля в локальных координатах сетки (X/Z). Если пусто — 1 клетка (0,0).")]
        public Vector2Int[] Footprint2D = new Vector2Int[] { Vector2Int.zero };
        [Tooltip("Минимум совпадающих разрешённых клеток поверхности")]
        public int RequiredOverlap = 1;

    }
}
