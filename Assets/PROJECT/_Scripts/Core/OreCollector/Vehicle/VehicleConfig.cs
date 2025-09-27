using UnityEngine;

[CreateAssetMenu(fileName = "VehicleConfig", menuName = "Configs/Vehicle/VehicleConfig")]
public class VehicleConfig : ScriptableObject
{
    [field: SerializeField] public string VehicleId { get; private set; }

    [Header("Physics")]
    [field: SerializeField] public float MoveSpeed { get; private set; } = 5f;
    [field: SerializeField] public float RotateSpeed { get; private set; } = 15;

    [Header("Track Stamps (per-vehicle)")]
    public TrackStampConfig TrackStamps;


    [Header("Other")]
    [field: SerializeField] public float CollectSpeed { get; private set; }
    [field: SerializeField] public float ArmorValue { get; private set; }


}
