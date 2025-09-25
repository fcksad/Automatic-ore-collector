using Service;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class AudioDropdownListener : MonoBehaviour
{
    [SerializeField] private AudioConfig _soundConfig;
    [SerializeField] private TMP_Dropdown _dropDownListen;

    private IAudioService _audioService;

    private void Start()
    {
        _audioService = ServiceLocator.Get<IAudioService>();
        _dropDownListen.onValueChanged.AddListener(_ => PlaySound());
    }

    private void PlaySound()
    {
        _audioService.Play(_soundConfig);
    }

    private void OnValidate()
    {
        if (_dropDownListen == null)
        {
            _dropDownListen = GetComponent<TMP_Dropdown>();
        }
    }
}
