using UnityEngine;


[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/Enemy/EnemyConfig")]
public class EnemyConfigBase : ScriptableObject
{

    [field: SerializeField] public string EnemyID { get; private set; }

    [Tooltip("")]
    [field: SerializeField] public float MoveSpeed { get; private set; } = 2;
    [field: SerializeField] public float MaxMoveSpeed { get; private set; } = 5;
    [field: SerializeField] public float RotationSpeed { get; private set; } = 60;
    [field: SerializeField] public float Health { get; private set; } = 10;
    [field: SerializeField] public float Damage { get; private set; } = 1;
    [field: SerializeField] public float AttackSpeed { get; private set; } = 4;
    [field: SerializeField] public float AttackRange { get; private set; } = 2;

/*    [Header("Navigation / Bumper")]
    [field: SerializeField] public float BumperLength { get; private set; } = 0.5f;         
    [field: SerializeField] public Vector3 BumperHalfExtents { get; private set; } = new(0.35f, 0.3f, 0.04f);
    [field: SerializeField] public LayerMask ObstacleMask { get; private set; } = ~0;*/

/*    [Header("Skirt (obstacle avoidance)")]
    [field: SerializeField] public float SkirtSideWeight { get; private set; } = 0.9f;  
    [field: SerializeField] public float SkirtFwdWeight { get; private set; } = 0.6f; 
    [field: SerializeField] public float SkirtTurnSpeed { get; private set; } = 180f;
    [field: SerializeField] public float SkirtProbeLen { get; private set; } = 0.7f; 
    [field: SerializeField] public float SkirtMaxTime { get; private set; } = 2.0f; */
}
