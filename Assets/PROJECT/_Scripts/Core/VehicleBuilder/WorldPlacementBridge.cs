using UnityEngine;
using UnityEngine.InputSystem;

public class WorldPlacementBridge : MonoBehaviour
{
    public Builder.GridGhostPlacer Placer;

    [Header("Optional limits")]
    public Collider BuildVolume3D;      
    public Camera WorldCamera;        

    void Awake()
    {
        if (!WorldCamera) WorldCamera = Camera.main;
    }

    void Update()
    {
        var item = Inventory.DragContext.Item;
        var module = item?.Config?.Module;

        var pos = GetPointer();
        Placer?.SetExternalPointer(pos);

        bool releasedThisFrame = WasPrimaryReleasedThisFrame(); 

        if (module == null)
        {

            if (!releasedThisFrame)
            {
                if (Placer && Placer.IsActive) Placer.End();
                Placer?.ReleaseExternalPointer();
            }
            return;
        }

        bool overUI = UIChecker.IsOverUI(pos);
        bool inside3D = IsPointerInsideBuildVolume(pos);

        if (!overUI && inside3D)
        {
            if (Placer && !Placer.IsActive) Placer.Begin(module);
        }
        else
        {
            if (Placer && Placer.IsActive) Placer.End();
        }
    }

    bool IsPointerInsideBuildVolume(Vector2 screenPos)
    {
        if (!BuildVolume3D) return true;
        var ray = (WorldCamera ? WorldCamera : Camera.main).ScreenPointToRay(screenPos);
        return BuildVolume3D.Raycast(ray, out _, 10000f);
    }

    static Vector2 GetPointer() =>
        Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

    static bool WasPrimaryReleasedThisFrame() =>
        Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
}
