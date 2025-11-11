using UnityEngine;
using UnityEngine.InputSystem;
using Builder;
using Inventory;

public class WorldToInventoryPickup : MonoBehaviour
{
    public Camera Cam;
    public LayerMask ModuleMask;                 // слой модулей (НЕ коннекторов)
    public BuildGridState GridState;
    public InventoryGridController InventoryController;

    private void Awake()
    {
        if (!Cam) Cam = Camera.main;
        if (!GridState) GridState = FindObjectOfType<BuildGridState>();
        if (!InventoryController) InventoryController = FindObjectOfType<InventoryGridController>();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (Inventory.DragContext.Item != null)
            return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            var pos = mouse.position.ReadValue();
            var ray = Cam.ScreenPointToRay(pos);

            if (Physics.Raycast(ray, out var hit, 1000f, ModuleMask))
            {
                var runtime = hit.collider.GetComponentInParent<BuildModuleRuntime>();
                if (runtime != null)
                {
                    Pickup(runtime);
                }
            }
        }
    }

    private void Pickup(BuildModuleRuntime runtime)
    {
        var cfg = runtime.SourceConfig;
        if (!cfg)
        {
            Debug.LogWarning("[WorldToInventoryPickup] runtime.SourceConfig is null");
            return;
        }

        if (runtime.GridState)
            runtime.GridState.Remove(runtime);

        if (InventoryController != null)
        {
            InventoryController.AddItem(cfg, 1);
        }
        Destroy(runtime.gameObject);
    }
}
