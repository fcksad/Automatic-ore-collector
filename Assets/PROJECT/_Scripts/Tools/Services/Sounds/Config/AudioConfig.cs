using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Audio")] 
public class AudioConfig : ScriptableObject
{
    [field: SerializeField] public string AudioName { get; private set; }
    [field: SerializeField] public AudioType Type { get; private set; }
    [field: SerializeField] public bool OneShoot { get; private set; } = true;
    [field: SerializeField] public float SpatialBlend { get; private set; } = 0;
    [field: SerializeField] public List<AudioClip> AudioClips { get; private set; }
    [Range(-3, 3)] public float MinPitch = 1f;
    [Range(-3, 3)] public float MaxPitch = 1f;


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (AudioClips == null || AudioClips.Count == 0 || AudioClips[0] == null)
            return;

        if (string.IsNullOrEmpty(AudioName) || AudioName != AudioClips[0].name)
        {
            AudioName = AudioClips[0].name;
        }
    }
#endif
}
