using Service;
using System.Collections;
using UnityEngine;

public class VehicleDecal : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private IInstantiateFactoryService _factory;
    private Coroutine _lifeCo;

    public void Init(float lifeTime, float fadeTime, IInstantiateFactoryService factory)
    {
        _factory = factory;
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_lifeCo != null) StopCoroutine(_lifeCo);
        _lifeCo = StartCoroutine(LifeRoutine(lifeTime, fadeTime));
    }

    private IEnumerator LifeRoutine(float life, float fade)
    {
        if (_spriteRenderer != null)
        {
            var c = _spriteRenderer.color; c.a = 1f; _spriteRenderer.color = c;
        }

        if (life > 0f) yield return new WaitForSeconds(life);

        if (fade > 0f && _spriteRenderer != null)
        {
            float t = 0f;
            Color c0 = _spriteRenderer.color;
            while (t < fade)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / fade);
                _spriteRenderer.color = new Color(c0.r, c0.g, c0.b, k);
                yield return null;
            }
        }


        if (_factory != null) _factory.Release(this);
        else gameObject.SetActive(false);
    }
}
   
