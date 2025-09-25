using TMPro;
using UnityEngine;

public class BuildVersionDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _versionText;
    private void Start()
    {
        _versionText.text = $"Version: {Application.version}";
    }
}
