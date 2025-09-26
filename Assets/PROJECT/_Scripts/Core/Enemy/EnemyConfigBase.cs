using UnityEngine;


[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/Enemy/EnemyConfig")]
public class EnemyConfigBase : ScriptableObject
{

    [field: SerializeField] public string EnemyID { get; private set; }

    [Tooltip("")]
    [field: SerializeField] public float Speed { get; private set; } = 5;
    [field: SerializeField] public float RotationSpeed { get; private set; } = 1;
    [field: SerializeField] public float Health { get; private set; } = 10;
    [field: SerializeField] public float Damage { get; private set; } = 1;
    [field: SerializeField] public float AttackSpead { get; private set; } = 4;
}
