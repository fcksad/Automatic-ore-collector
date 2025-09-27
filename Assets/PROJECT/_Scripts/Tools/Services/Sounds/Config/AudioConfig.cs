using System.Collections.Generic;
using UnityEngine;

public enum SpatialMode { TwoD, ThreeD }

[CreateAssetMenu(menuName = "Configs/Audio")] 
public class AudioConfig : ScriptableObject
{
    [field: SerializeField] public string AudioName { get; private set; }
    [field: SerializeField] public AudioType Type { get; private set; }
    [field: SerializeField] public List<AudioClip> AudioClips { get; private set; }
    [field: SerializeField] public bool OneShoot { get; private set; } = true;
    [field: SerializeField] public bool Loop { get; private set; } = false;

    [Range(-3, 3)] public float MinPitch = 1f;
    [Range(-3, 3)] public float MaxPitch = 1f;
    //[Range(-3, 3)] public float VolumeScale = 1f;
    [field: SerializeField] public SpatialMode Spatial { get; private set; } = SpatialMode.TwoD;
    [Range(0, 1f)] public float SpatialBlend = 0;
    [field: SerializeField] public float MinDistance { get; private set; } = 1f;
    [field: SerializeField] public float MaxDistance { get; private set; } = 50f;


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
