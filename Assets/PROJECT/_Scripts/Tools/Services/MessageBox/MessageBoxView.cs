using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageBoxView : MonoBehaviour
{
    [field: SerializeField] public TextMeshProUGUI Text {  get; private set; }
    [field: SerializeField] public GameObject TextHolder { get; private set; }
    [field: SerializeField] public Button YesButton { get; private set; }
    [field: SerializeField] public Button NoButton { get; private set; }
    [field: SerializeField] public Button OkButton { get; private set; }
    [field: SerializeField] public Button Background { get; private set; }
    [field: SerializeField] public RectTransform RectTransform { get; private set; }
    [field: SerializeField] public List<Graphic> FadeGrafic { get; private set; }

}
