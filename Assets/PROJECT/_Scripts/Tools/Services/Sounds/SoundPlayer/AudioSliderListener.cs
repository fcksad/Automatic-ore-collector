using Service;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class AudioSliderListener : MonoBehaviour
{
    [SerializeField] private AudioConfig _soundConfig;
    [SerializeField] private Slider _slider;

    private Coroutine _debounceCoroutine;
    private float _debounceTime = 0.05f;

    private IAudioService _audioService;

    private void Start()
    {
        _audioService = ServiceLocator.Get<IAudioService>();
        _slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (_debounceCoroutine != null)
        {
            StopCoroutine(_debounceCoroutine);
        }

        _debounceCoroutine = StartCoroutine(DebounceCoroutine());
    }

    private IEnumerator DebounceCoroutine()
    {
        yield return new WaitForSeconds(_debounceTime);
        PlaySound();
    }

    private void PlaySound()
    {
        _audioService.Play(_soundConfig);
    }

    private void OnValidate()
    {
        if (_slider == null)
        {
            _slider = GetComponent<Slider>();
        }
    }
}
