using UnityEngine;
using UnityEngine.InputSystem;

public class WorldPlacementBridge : MonoBehaviour
{
    public Builder.ConnectorGridGhostPlacer Placer;
    public Collider BuildVolume3D;    
    public Camera WorldCamera;

    void Awake() { if (!WorldCamera) WorldCamera = Camera.main; }

    void Update()
    {
        var item = Inventory.DragContext.Item;
        var module = item?.Config?.Module;

        var pos = GetPointer();
        Placer?.SetExternalPointer(pos);

        if (module == null)
        {
            if (Placer && Placer.IsActive) Placer.End();
            Placer?.ReleaseExternalPointer();
            return;
        }

        if (UIChecker.IsOverUI(pos))
        {
            if (Placer && Placer.IsActive) Placer.End();
            return;
        }

        if (BuildVolume3D && !IsPointerInsideBuildVolume(pos))
        {
            if (Placer && Placer.IsActive) Placer.End();
            return;
        }

        if (Placer && !Placer.IsActive) Placer.Begin(module);
    }

    bool IsPointerInsideBuildVolume(Vector2 screenPos)
    {
        if (!BuildVolume3D) return true;
        var ray = (WorldCamera ? WorldCamera : Camera.main).ScreenPointToRay(screenPos);
        return BuildVolume3D.Raycast(ray, out _, 10000f);
    }

    static Vector2 GetPointer() =>
        Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
}
