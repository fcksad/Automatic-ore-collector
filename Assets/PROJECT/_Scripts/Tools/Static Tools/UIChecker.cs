using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class UIChecker 
{
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

}
