using Service;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TooltipImageListener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ITooltipService _toolTipService;

    private void Start()
    {
        _toolTipService  = ServiceLocator.Get<ITooltipService>(); 
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _toolTipService.Show("asdsad asdsadsa asdsadsa dasd sad asdsa dsad asd asd asd asd");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _toolTipService.Hide();
    }
}
