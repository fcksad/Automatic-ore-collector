using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintGroup : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI _action;

    private List<HintImage> _hints = new List<HintImage>();

    public void Setup(string local, List<HintImage> hints)
    {
        _action.SetText(local + ":");

        _hints = hints;
        foreach (var hint in _hints)
        {
            hint.transform.SetParent(transform, false);
            hint.gameObject.SetActive(true);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }

    public List<HintImage> GetHints()
    {
        return _hints;
    }

}
