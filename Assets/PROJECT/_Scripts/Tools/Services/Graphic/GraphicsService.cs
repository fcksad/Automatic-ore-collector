using Settings;
using System;
using UnityEngine;

namespace Service
{
    public enum GraphicType
    {
        QualityLevel = 0,
        VSync = 1,
        ScreenMode = 2,
        Resolution = 3,
        FPS = 4,
    }

    public class GraphicsService : IGraphicsService, IInitializable
    {
        private ISaveService _saveService;

        public GraphicsService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void Initialize()
        {
            Load();
        }

        private void Load()
        {
            SetResolution(GetResolution());
            foreach (GraphicType type in Enum.GetValues(typeof(GraphicType)))
            {
                if (type != GraphicType.Resolution)
                    Set(type, Get(type));
            }

        }

        public void Set(GraphicType type, int value)
        {
            switch (type)
            {
                case GraphicType.QualityLevel:
                    _saveService.SettingsData.GraphicsData.QualityLevel = value;
                    QualitySettings.SetQualityLevel(value);
                    break;
                case GraphicType.VSync:
                    _saveService.SettingsData.GraphicsData.VSync = value;
                    QualitySettings.vSyncCount = value;
                    break;
                case GraphicType.ScreenMode:
                    _saveService.SettingsData.GraphicsData.ScreenMode = value;
                    ScreenMode mode = (ScreenMode)value;

                    Screen.fullScreenMode = mode switch
                    {
                        ScreenMode.Windowed => FullScreenMode.Windowed,
                        ScreenMode.Maximized => FullScreenMode.MaximizedWindow,
                        ScreenMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
                        _ => Screen.fullScreenMode
                    };
                    break;
                case GraphicType.FPS:
                    _saveService.SettingsData.GraphicsData.FPS = value;
                    Application.targetFrameRate = value;
                    break;
            }
        }

        public void SetResolution(ResolutionData resolution)
        {
            _saveService.SettingsData.GraphicsData.Resolution = resolution;

            var refreshRate = new RefreshRate
            {
                numerator = (uint)resolution.RefreshRate,
                denominator = 1u
            };

            Screen.SetResolution(resolution.Width, resolution.Height, Screen.fullScreenMode, refreshRate);
        }
        public int Get(GraphicType type)
        {
            return type switch
            {
                GraphicType.QualityLevel => _saveService.SettingsData.GraphicsData.QualityLevel,
                GraphicType.VSync => _saveService.SettingsData.GraphicsData.VSync,
                GraphicType.ScreenMode => _saveService.SettingsData.GraphicsData.ScreenMode,
                GraphicType.FPS => _saveService.SettingsData.GraphicsData.FPS,
                _ => 0
            };
        }

        public ResolutionData GetResolution()
        {
            return _saveService.SettingsData.GraphicsData.Resolution;
        }
    }
}
