using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFadeView : MonoBehaviour
{
    [Header("Loading Screen UI")]
    [SerializeField] private Image _fadeImage;

    [Header("Fade tuning")]
    [SerializeField, Range(0f, 3f)] private float _fadeInHold = 0.35f;  
    [SerializeField, Range(1f, 6f)] private float _fadeInPower = 3f;    
    [SerializeField, Range(1f, 6f)] private float _fadeOutPower = 2f;

    public void SetAlpha(float alpha)
    {
        var color = _fadeImage.color;
        color.a = alpha;
        _fadeImage.color = color;
    }

    public async Task FadeIn(float duration = 1f)
    {
        if (_fadeImage == null) return;

        _fadeImage.gameObject.SetActive(true);
        SetAlpha(1f);

        float tHold = 0f;
        while (tHold < _fadeInHold)
        {
            if (this == null || _fadeImage == null) return;
            tHold += Time.unscaledDeltaTime;
            await Task.Yield();
        }


        float t = 0f;
        while (t < duration)
        {
            if (this == null || _fadeImage == null) return;

            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            float a = 1f - Mathf.Pow(u, _fadeInPower);
            SetAlpha(a);

            await Task.Yield();
        }

        SetAlpha(0f);
        _fadeImage.gameObject.SetActive(false);
    }

    public async Task FadeOut(float duration = 0f)
    {
        if (_fadeImage == null) return;

        _fadeImage.gameObject.SetActive(true);
        float t = 0f;
        while (t < duration)
        {
            if (this == null || _fadeImage == null) return;

            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            float a = Mathf.Pow(u, _fadeOutPower);
            SetAlpha(a);

            await Task.Yield();
        }
        SetAlpha(1f);
    }
}
