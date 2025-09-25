using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Service
{
    public class HintView : MonoBehaviour, IInitializable
    {
        [SerializeField] private InputHintConfig _inputHintConfig;
        [SerializeField] private HintGroup _hintGroup;
        [SerializeField] private HintImage _hintImage;
        [SerializeField] private HintImage _hintText;
        [SerializeField] private Transform _hintHolder;
        [SerializeField] private HorizontalLayoutGroup _horizontalLayoutGroup;

        private Dictionary<string, HintGroup> _hints = new();

        private IInstantiateFactoryService _instantiateFactoryService;

        public void Initialize()
        {
            _instantiateFactoryService = ServiceLocator.Get<IInstantiateFactoryService>();
        }

        public void Show(string localizationAction, List<CharacterAction> actions, List<string> controlButton, ControlDeviceType deviceType)
        {
            if (_hints.ContainsKey(localizationAction) == false)
            {
                var newHintGroup = _instantiateFactoryService.Create(_hintGroup, parent: _hintHolder, customName: localizationAction.ToString());
                List<HintImage> hints = new List<HintImage>();

                for (int i = 0; i < actions.Count; i++)
                {
                    var hint = _inputHintConfig.GetHint(deviceType, controlButton[i]);
                    HintImage newHint;

                    if (hint.Icon != null)
                    {
                        newHint = _instantiateFactoryService.Create(_hintImage, parent: _hintHolder, customName: actions[i].ToString());
                        newHint.SetImage(hint.Icon);
                    }
                    else
                    {
                        newHint = _instantiateFactoryService.Create(_hintText, parent: _hintHolder, customName: actions[i].ToString());
                        newHint.SetText(hint.ControlName);
                    }

                    hints.Add(newHint);
                }

                newHintGroup.Setup(localizationAction, hints);
                _hints[localizationAction] = newHintGroup;
            }
            else
            {
                var group = _hints[localizationAction];
                if (!group.gameObject.activeSelf)
                    group.gameObject.SetActive(true);
            }
        }

        public void Hide(string localizationAction)
        {
            if (_hints.TryGetValue(localizationAction, out var hint))
            {
                hint.gameObject.SetActive(false);
            }
        }

        public void HideAll()
        {
            foreach (var hint in _hints.Values)
            {
                hint?.gameObject?.SetActive(false);
            }
        }

        public void Clear()
        {
            foreach(var group in _hints.Values)
    {
                foreach (var item in group.GetHints())
                    _instantiateFactoryService.Release(item);

                _instantiateFactoryService.Release(group);
            }
            _hints.Clear();
        }

        public void Toggle(bool value)
        {
            _hintHolder.gameObject.SetActive(value);
            //StartCoroutine(RefreshLayoutNextFrame());
        }

/*        private IEnumerator RefreshLayoutNextFrame()
        {
            yield return new WaitForEndOfFrame(); 
            _horizontalLayoutGroup.enabled = false;
            _horizontalLayoutGroup.enabled = true;
        }*/
    }
}
    
