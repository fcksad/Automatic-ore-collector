namespace Service   
{
    public class ScreenFadeController : IInitializable, IDisposable
    {
        private ScreenFadeView _view;
        private ISceneService _sceneService;

        public ScreenFadeController(ISceneService sceneService, ScreenFadeView screenFadeView)
        {
            _sceneService = sceneService;
            _view = screenFadeView;
        }
        public void Initialize()
        {
            _sceneService.OnSceneLoadEvent += OnSceneLoad;
            _sceneService.OnSceneUnloadEvent += OnSceneUnload;
        }

        public void Dispose()
        {
            _sceneService.OnSceneLoadEvent -= OnSceneLoad;
            _sceneService.OnSceneUnloadEvent -= OnSceneUnload;
        }


        private async void OnSceneLoad() => await _view.FadeIn();
        private async void OnSceneUnload() => await _view.FadeOut();
    }

}
