using System.Collections.Generic;
using UnityEngine;

namespace Service
{
    public interface ISurfaceAudioService
    {
        public AudioConfig GetConfigByMaterial(Renderer renderer, SourceType sourceType);
    }
}
