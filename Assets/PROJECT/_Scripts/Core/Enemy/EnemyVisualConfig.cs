using UnityEngine;

[CreateAssetMenu(fileName = "EnemyVisualConfig", menuName = "Configs/Enemy/EnemyVisualConfig")]
public class EnemyVisualConfig : ScriptableObject
{
    [field: SerializeField] public CustomAnimClip Idle { get; private set; }
    [field: SerializeField] public CustomAnimClip Run { get; private set; }
    [field: SerializeField] public CustomAnimClip Attack { get; private set; }
}
