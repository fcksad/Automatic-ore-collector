using UnityEngine;
using UnityEngine.Localization;
namespace Localization
{
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "Configs/Service/Localization/LocalizationConfig")]
    public class LocalizationConfig : ScriptableObject
    {
        [SerializeField] private LocalizedString _localizedString;
        public LocalizedString LocalizedString => _localizedString;
    }
}

