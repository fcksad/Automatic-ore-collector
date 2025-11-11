using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Builder;
using Inventory;

public class WorldToInventoryAndWorldDrag : MonoBehaviour
{
    [Header("World")]
    public Camera Cam;
    public LayerMask ModuleMask;   

    [Header("Grid")]
    public BuildGridState GridState;

    [Header("UI")]
    public Canvas UiCanvas;        

    [Header("Inventory")]
    public InventoryGridController InventoryController;

    [Header("Builder")]
    public Builder.ConnectorGridGhostPlacer Placer;

    private bool _isDraggingFromWorld;

    private void Awake()
    {
        if (!Cam) Cam = Camera.main;
        if (!GridState) GridState = FindObjectOfType<BuildGridState>();
        if (!UiCanvas) UiCanvas = FindObjectOfType<Canvas>();
        if (!InventoryController) InventoryController = FindObjectOfType<InventoryGridController>();
        if (!Placer) Placer = FindObjectOfType<Builder.ConnectorGridGhostPlacer>(); 
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (_isDraggingFromWorld)
        {
            UpdateGhost(mouse);
            return;
        }

        if (DragContext.Item != null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            var pos = mouse.position.ReadValue();

            if (UIChecker.IsOverUI(pos))
                return;

            var ray = Cam.ScreenPointToRay(pos);

            if (Physics.Raycast(ray, out var hit, 1000f, ModuleMask))
            {
                var runtime = hit.collider.GetComponentInParent<BuildModuleRuntime>();
                if (runtime != null)
                    StartDragFromWorld(runtime);
            }
        }
    }

    private void StartDragFromWorld(BuildModuleRuntime runtime)
    {
        var cfg = runtime.SourceConfig;
        if (!cfg)
        {
            Debug.LogWarning("[WorldDrag] runtime.SourceConfig is null");
            return;
        }

        if (runtime.GridState)
            runtime.GridState.Remove(runtime);

        var tempItem = new InventoryItemBase(cfg, 1);

        DragContext.Model = null;
        DragContext.FromIndex = -1;
        DragContext.Item = tempItem;
        DragContext.Owner = this;

        Destroy(runtime.gameObject);
        CreateGhost(cfg.Icon);


        _isDraggingFromWorld = true;
    }

    private void CreateGhost(Sprite icon)
    {
        if (!UiCanvas)
        {
            Debug.LogWarning("[WorldDrag] UiCanvas is not assigned");
            return;
        }

        var go = new GameObject("DragGhost", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(Image));

        var rt = go.GetComponent<RectTransform>();
        var cc = go.GetComponent<Canvas>();
        var group = go.GetComponent<CanvasGroup>();
        var img = go.GetComponent<Image>();

        DragContext.Ghost = rt;
        DragContext.GhostCanvas = cc;
        DragContext.GhostGroup = group;

        rt.SetParent(UiCanvas.transform, false);
        cc.overrideSorting = true;
        cc.sortingOrder = 999;
        group.blocksRaycasts = false;
        img.raycastTarget = false;

        img.sprite = icon;
        rt.sizeDelta = new Vector2(64, 64);
    }

    private void UpdateGhost(Mouse mouse)
    {
        if (DragContext.Ghost != null && UiCanvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                UiCanvas.transform as RectTransform,
                mouse.position.ReadValue(),
                UiCanvas.worldCamera,
                out var local);

            DragContext.Ghost.anchoredPosition = local;
        }

        // кнопка больше не зажата — заканчиваем drag
        if (!mouse.leftButton.isPressed)
        {
            var pos = mouse.position.ReadValue();
            bool overUI = UIChecker.IsOverUI(pos);

            var item = DragContext.Item;
            bool hitModule = false;

            // если не над UI – проверяем, попали ли по модульному коллайдеру
            if (!overUI)
            {
                var ray = Cam.ScreenPointToRay(pos);
                hitModule = Physics.Raycast(ray, out var hit, 1000f, ModuleMask);
            }

            if (item != null && InventoryController != null)
            {
                // 1) Над UI -> вернуть в инвентарь + отменить плейсер
                // 2) Не над UI, но НЕ по модулю (пустота) -> тоже вернуть в инвентарь + отменить плейсер
                if (overUI || !hitModule)
                {
                    InventoryController.AddItem(item.Config, item.Stack);

                    if (Placer && Placer.IsActive)
                        Placer.End(false); // 👈 ЯВНО говорим "не коммить, просто убери ghost"
                }
                // 3) Не над UI и есть hit по ModuleMask:
                //    считаем, что хотим построить в мире → НЕ добавляем в инвентарь,
                //    НЕ трогаем Placer – WorldPlacementBridge сам вызовет End(commit:true)
            }

            // В ЛЮБОМ СЛУЧАЕ чистим DragContext (и UI-ghost)
            DragContext.Clear();
            _isDraggingFromWorld = false;
        }
    }
}

