using UnityEngine;

public enum ArcAnchor { Front, Back, Left, Right, Custom }

[CreateAssetMenu(fileName = "TurretConfig", menuName = "Configs/Vehicle/TurretConfig")]
public class TurretConfig : ScriptableObject
{
    [field: SerializeField] public string TurretId { get; private set; }

    [Header("Aiming")]
    [Tooltip(" уда направлен центр допустимой дуги относительно  ќ–ѕ”—ј танка")]
    [field: SerializeField] public ArcAnchor Anchor { get; private set; } = ArcAnchor.Right;

    [Tooltip("≈сли Anchor=Custom Ч сдвиг центра дуги относительно корпуса, градусы")]
    [field: SerializeField] public float CustomAnchorOffsetDeg { get; private set; } = 0f;

    [Tooltip("Ћева€/права€ границы ќ“Ќќ—»“≈Ћ№Ќќ центра дуги, градусы (по факту полудуга)")]
    [field: SerializeField] public float YawMinDeg { get; private set; } = -90f;  // -90..+90 => 180∞
    [field: SerializeField] public float YawMaxDeg { get; private set; } = 90f;

    [Tooltip("—корость поворота (град/сек)")]
    [field: SerializeField] public float RotationSpeed { get; private set; } = 40f;

    [Header("Detection/Fire")]
    [field: SerializeField] public LayerMask DamageableMask { get; private set; }
    [field: SerializeField] public float DetectionRadius { get; private set; } = 12f;
    [field: SerializeField] public float FireRate { get; private set; } = 4f;
    [field: SerializeField] public float FireAngleTolerance { get; private set; } = 2f;
    [field: SerializeField] public float ProjectileSpeed { get; private set; } = 30f;
    [field: SerializeField] public float Damage { get; private set; } = 15f;
    [field: SerializeField] public float SpreadDeg { get; private set; } = 1.5f;
    [field: SerializeField] public float SpriteForwardOffsetDeg { get; private set; } = 180f;
    [field: SerializeField] public Projectile ProjectilePrefab { get; private set; }
}
