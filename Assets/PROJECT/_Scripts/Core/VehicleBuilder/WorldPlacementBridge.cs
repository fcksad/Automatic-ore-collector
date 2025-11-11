using UnityEngine;
using UnityEngine.InputSystem;

public class WorldPlacementBridge : MonoBehaviour
{
    public Builder.ConnectorGridGhostPlacer Placer;
    public Collider BuildVolume;    
    public Camera WorldCamera;

    private void Awake() { if (!WorldCamera) WorldCamera = Camera.main; }

    private void Update()
    {
        var item = Inventory.DragContext.Item;

        var module = item?.Config?.Module;

        var pos = GetPointer();

        if (module == null)
        {
            if (Placer && Placer.IsActive) Placer.End();

            return;
        }

        if (UIChecker.IsOverUI(pos))
        {
            if (Placer && Placer.IsActive) Placer.End();
            return;
        }

/*        if (BuildVolume && !IsPointerInsideBuildVolume(pos))
        {
            if (Placer && Placer.IsActive) Placer.End();
            return;
        }*/


        if (Placer && !Placer.IsActive) Placer.Begin(module);
    }

    private bool IsPointerInsideBuildVolume(Vector2 screenPos)
    {
        if (!BuildVolume) return true;
        var ray = (WorldCamera ? WorldCamera : Camera.main).ScreenPointToRay(screenPos);
        return BuildVolume.Raycast(ray, out _, 10000f);
    }

    private Vector2 GetPointer() => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
}
