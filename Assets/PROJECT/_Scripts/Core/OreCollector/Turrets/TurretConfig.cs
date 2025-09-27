using UnityEngine;

[CreateAssetMenu(fileName = "TurretConfig", menuName = "Configs/Vehicle/TurretConfig")]
public class TurretConfig : ScriptableObject
{
    [field: SerializeField] public string TurretId { get; private set; }

    [Tooltip("")]
    [field: SerializeField] public float HorizontalRotationSpeed { get; private set; } = 40f;
    [field: SerializeField] public float VerticalRotationSpeed { get; private set; } = 40f;

    [field: SerializeField] public float MaxVerticalAngle { get; private set; } = 20f;
    [field: SerializeField] public float MaxHorizontalAngle { get; private set; } = 360f;

    [Header("Detection/Fire")]
    [field: SerializeField] public LayerMask TargetMask { get; private set; }
    [field: SerializeField] public float DetectionRadius { get; private set; } = 12f;
    [field: SerializeField] public float FireRate { get; private set; } = 4f;
    [field: SerializeField] public float Damage { get; private set; } = 15f;
    [Tooltip("Allow shooting only when muzzle is within this angle to target (deg)")]
    [field: SerializeField] public float FireAngleTolerance { get; private set; } = 6f;
    [field: SerializeField] public bool ReacquireIfOutOfAngles { get; private set; } = true;
    [field: SerializeField, Min(0f)] public float ReacquireDelay { get; private set; } = 0.25f;

    [Tooltip("Effects")]
    [field: SerializeField] public ParticleController MuzzleParticle { get; private set; }

    [Tooltip("Sound")]
    [field: SerializeField] public AudioConfig ShotSound { get; private set; }
    [field: SerializeField] public AudioConfig NoTargetSound { get; private set; }

    [Range(1, 100)] public float MaxDistanceSound = 30f;

    [Header("Behavior")]
    [Tooltip("Time between target reacquire scans")]
    [field: SerializeField] public float ScanInterval { get; private set; } = 0.15f;
    [Tooltip("Return to initial pose when target lost")]
    [field: SerializeField] public bool ReturnToRest { get; private set; } = false;
    [field: SerializeField] public float ReturnSpeedMul { get; private set; } = 0.5f;
}
