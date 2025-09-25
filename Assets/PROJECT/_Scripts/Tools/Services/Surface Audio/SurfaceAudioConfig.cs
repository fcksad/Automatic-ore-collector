using System.Collections.Generic;
using System;
using UnityEngine;

namespace Service
{
    [Serializable]
    public class Surface
    {
        [field: SerializeField] public SurfaceType Type { get; private set; } = SurfaceType.Default;
        [field: SerializeField] public List<Material> Materials { get; private set; }
        [field: SerializeField] public AudioConfig Audio { get; private set; }
    }

    [CreateAssetMenu(fileName = "Surface", menuName = "Configs/Service/SurfaceAudio/Surface")]
    public class SurfaceAudioConfig : ScriptableObject
    {
        [field: SerializeField] public SourceType SourceType { get; private set; }
        [field: SerializeField] public List<Surface> Surfaces { get; private set; }
    }
}
