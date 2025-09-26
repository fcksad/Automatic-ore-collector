using UnityEngine;

[CreateAssetMenu(fileName = "TrackStampConfig", menuName = "Configs/Vehicle/Track Stamps")]
public class TrackStampConfig : ScriptableObject
{
    [Header("Render")]
    [field: SerializeField] public Material Material { get; private set; }
    [field: SerializeField] public Vector2 StampSize { get; private set; } = new(1.3f, 0.25f);
    [Min(1)] public int MaxStamps { get; private set; } = 3000;
    [field: SerializeField] [Range(0f, 0.05f)] public float YOffset { get; private set; } = 0.003f; 

    [Header("Placement")]
    [field: SerializeField][Min(0.01f)] public float Spacing { get; private set; } = 0.3f;
    [field: SerializeField] public bool Randomize { get; private set; } = true;
    [field: SerializeField][Range(0f, 8f)] public float RandomRot { get; private set; } = 3f;
    [field: SerializeField][Range(0f, 0.05f)] public float RandomOffset { get; private set; } = 0.01f;
}
