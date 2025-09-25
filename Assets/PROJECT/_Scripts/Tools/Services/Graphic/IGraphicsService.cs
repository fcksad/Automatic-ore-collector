using Settings;

namespace Service
{
    public interface IGraphicsService
    {
        public void Set(GraphicType type, int value);
        public int Get(GraphicType type);

        public void SetResolution(ResolutionData resolution);
        public ResolutionData GetResolution();

    }

}
