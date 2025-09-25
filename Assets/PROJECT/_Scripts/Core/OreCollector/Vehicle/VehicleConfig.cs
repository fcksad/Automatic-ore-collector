using UnityEngine;

[CreateAssetMenu(fileName = "VehicleConfig", menuName = "Configs/Vehicle/VehicleConfig")]
public class VehicleConfig : ScriptableObject
{

    [field: SerializeField] public string VehicleId { get; private set; }
    [Header("Physics")]
    [field: SerializeField] public float MoveSpeed { get; private set; } = 50f;
    [field: SerializeField] public float RotateSpeed { get; private set; } = 50f;

    [Header("Configurations")]


    [Header("Decals")]
    [field: SerializeField] public VehicleDecal DecalPrefab { get; private set; }
    [field: SerializeField, Min(0f)] public float DecalLifeTime { get; private set; } = 6f;
    [field: SerializeField, Min(0f)] public float DecalFadeTime { get; private set; } = 0.4f;
    [field: SerializeField, Min(0.01f)] public float DecalSpacing { get; private set; } = 1.3f; // шаг между следами в метрах
    [field: SerializeField] public bool DecalRandomize { get; private set; } = true;
    [field: SerializeField, Range(0f, 8f)] public float DecalRandomRot { get; private set; } = 3f; // ±градусы
    [field: SerializeField, Range(0f, 0.05f)] public float DecalRandomOffset { get; private set; } = 0.01f; // ±метры


    [Header("Other")]
    [field: SerializeField] public float CollectSpeed { get; private set; }
    [field: SerializeField] public float ArmorValue { get; private set; }


}
