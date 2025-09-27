using Localization;
using Service.Coroutines;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Service
{
    [DefaultExecutionOrder(-800)]
    public class ServiceBootstrap : MonoBehaviour , IBootstrapable
    {
        private void Awake()
        {
            ServiceLocator.BindWithInterface<ISceneService, SceneService>();
            ServiceLocator.BindWithInterface<IPoolService, PoolService>();
            ServiceLocator.BindWithInterface<ISaveService, SaveService>();
            ServiceLocator.BindWithInterface<IInstantiateFactoryService, InstantiateFactoryService>();
            ServiceLocator.BindWithInterface<IAudioService, AudioService>();

            ServiceLocator.BindWithInterface<IGraphicsService, GraphicsService>();

            ServiceLocator.BindFromChildren<PlayerInput>();
            ServiceLocator.BindWithInterface<IInputService, InputService>();

            ServiceLocator.BindWithInterface<IControlsService, ControlsService>();

            ServiceLocator.BindWithInterface<ICoroutineService, CoroutineService>();
            ServiceLocator.BindWithInterface<ILocalizationService, LocalizationService>();

            ServiceLocator.BindFromChildren<TooltipeView>();
            ServiceLocator.BindWithInterface<ITooltipService, TooltipService>();

            ServiceLocator.BindWithInterface<IParticleService, ParticleService>();

            ServiceLocator.BindFromChildren<PopupView>();
            ServiceLocator.BindWithInterface<IPopupService, PopupService>();

            ServiceLocator.BindFromChildren<HintView>();
            ServiceLocator.BindWithInterface<IHintService, HintService>();

            ServiceLocator.BindFromChildren<DialogueView>();
            ServiceLocator.BindWithInterface<IDialogueService, DialogueService>();

            ServiceLocator.BindFromChildren<ScreenFadeView>();
            ServiceLocator.BindComponent<ScreenFadeController>();

            ServiceLocator.BindFromChildren<MessageBoxView>();
            ServiceLocator.BindComponent<MessageBoxController>();

            ServiceLocator.BindFromChildren<FpsCounter>();

        }

        private void OnApplicationQuit()
        {
            ServiceLocator.Clear();
        }
    }

}
