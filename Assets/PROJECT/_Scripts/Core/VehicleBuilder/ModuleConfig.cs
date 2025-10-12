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

        [Header("Placement")]
        public RotationMode RotationMode = RotationMode.Snap90;

        [Tooltip("Локальный footprint в клетках X/Z (для коннекторных поверхностей).")]
        public Vector2Int[] Footprint2D;

        [Tooltip("Как минимум столько клеток должно попасть в разрешённые на поверхности.")]
        public int RequiredOverlap = 1;

        [Tooltip("Насколько приподнимать модуль от поверхности (толщина «подошвы», метры).")]
        public float MountHeight = 0.01f;

        [Tooltip("Если у префаба нет коллайдеров — габариты для overlap-проверки.")]
        public Vector3 BoundsSize = new Vector3(0.5f, 0.5f, 0.5f);

    }
}
