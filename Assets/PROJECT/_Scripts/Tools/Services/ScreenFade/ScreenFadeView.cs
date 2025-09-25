using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFadeView : MonoBehaviour
{
    [Header("Loading Screen UI")]

    [SerializeField] private Image _fadeImage; 

    public void SetAlpha(float alpha)
    {
        var color = _fadeImage.color;
        color.a = alpha;
        _fadeImage.color = color;
    }

    public async Task FadeIn(float duration = 1f)
    {
        float time = 0f;

        while (time < duration)
        {
            if (this == null || _fadeImage == null) 
                return;

            time += Time.unscaledDeltaTime;
            float t = time / duration;
            SetAlpha(1 - Mathf.SmoothStep(0, 1, t));
            await Task.Yield();
        }

        SetAlpha(0);
        _fadeImage.gameObject.SetActive(false);
    }

    public async Task FadeOut(float duration = 0.1f)
    {
        _fadeImage.gameObject.SetActive(true);
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            SetAlpha(Mathf.SmoothStep(0, 1, t));
            await Task.Yield();
        }
        SetAlpha(1);
    }
}
