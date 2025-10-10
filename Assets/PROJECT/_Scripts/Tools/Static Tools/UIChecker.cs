using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public static class UIChecker
{

    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (EventSystem.current.IsPointerOverGameObject(-1))
            return true;

        return GraphicRaycastAt(GetScreenPointerPosition());
    }


    public static bool IsOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        return GraphicRaycastAt(screenPos);
    }

    public static bool IsOverRect(RectTransform rect, Vector2 screenPos, Camera uiCamera = null)
    {
        if (!rect) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, uiCamera);
    }

    private static Vector2 GetScreenPointerPosition()
    {
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    private static bool GraphicRaycastAt(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}
