using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Control : MonoBehaviour
{
    [field: SerializeField] public TextMeshProUGUI KeyAction {  get; private set; }
    [field: SerializeField] public Button KeyBind { get; private set; }
    [field: SerializeField] public Button KeyAlternativeBind { get; private set; }
    [field: SerializeField] public Button ResetButton { get; private set; }
    [field: SerializeField] public Image Background  { get; private set; }
}
