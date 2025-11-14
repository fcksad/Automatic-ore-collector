using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Builder;
using Inventory;
using Service;

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
    public ConnectorGridGhostPlacer Placer;

    private bool _isDraggingFromWorld;
    private Coroutine _dragRoutine;

    private IInputService _inputService;

    private Action _onLeftStarted;
    private Action _onLeftCanceled;
    private Action _onRightStarted;

    private void Awake()
    {
        if (!Cam) Cam = Camera.main;
        if (!GridState) GridState = FindObjectOfType<BuildGridState>();
        if (!UiCanvas) UiCanvas = FindObjectOfType<Canvas>();
        if (!InventoryController) InventoryController = FindObjectOfType<InventoryGridController>();
        if (!Placer) Placer = FindObjectOfType<ConnectorGridGhostPlacer>();

        _inputService = ServiceLocator.Get<IInputService>();
    }

    private void Start()
    {
        _onLeftStarted = OnLeftClickStarted;
        _onLeftCanceled = OnLeftClickCanceled;
        _onRightStarted = OnRightClickStarted;

        _inputService.AddActionListener(CharacterAction.LeftClick,
            onStarted: _onLeftStarted,
            onCanceled: _onLeftCanceled);

        _inputService.AddActionListener(CharacterAction.RightClick,
            onStarted: _onRightStarted);
    }

    private void OnDestroy()
    {
        if (_inputService != null)
        {
            _inputService.RemoveActionListener(CharacterAction.LeftClick,
                onStarted: _onLeftStarted,
                onCanceled: _onLeftCanceled);

            _inputService.RemoveActionListener(CharacterAction.RightClick,
                onStarted: _onRightStarted);
        }
    }

    private void OnLeftClickStarted()
    {
        if (DragContext.Item != null || _isDraggingFromWorld)
            return;

        var mouse = Mouse.current;
        if (mouse == null) return;

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

    private void OnLeftClickCanceled()
    {
        if (!_isDraggingFromWorld)
            return;

        var item = DragContext.Item;
        var mouse = Mouse.current;
        if (mouse == null)
        {
            CleanupDrag();
            return;
        }

        var pos = mouse.position.ReadValue();
        bool overUI = UIChecker.IsOverUI(pos);

        if (item != null && InventoryController != null)
        {
            if (overUI)
            {
                InventoryController.AddItem(item.Config, item.Stack);

                if (Placer && Placer.IsActive)
                    Placer.End(false);
            }
            else
            {
                if (Placer && Placer.IsActive)
                {
                    if (Placer.HasValidPlacement)
                    {
                        Placer.End(true);
                    }
                    else
                    {
                        Placer.End(false);
                        InventoryController.AddItem(item.Config, item.Stack);
                    }
                }
                else
                {
                    InventoryController.AddItem(item.Config, item.Stack);
                }
            }
        }

        CleanupDrag();
    }

    private void OnRightClickStarted()
    {
        if (DragContext.Item != null || _isDraggingFromWorld)
            return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        var pos = mouse.position.ReadValue();

        if (UIChecker.IsOverUI(pos))
            return;

        var ray = Cam.ScreenPointToRay(pos);
        if (Physics.Raycast(ray, out var hit, 1000f, ModuleMask))
        {
            var runtime = hit.collider.GetComponentInParent<BuildModuleRuntime>();
            if (runtime != null)
                QuickPickup(runtime);
        }
    }

    private void QuickPickup(BuildModuleRuntime runtime)
    {
        var cfg = runtime.SourceConfig;
        if (!cfg)
        {
            Debug.LogWarning("[WorldDrag] QuickPickup: runtime.SourceConfig is null");
            return;
        }

        if (runtime.GridState)
            runtime.GridState.Remove(runtime);

        if (InventoryController != null)
            InventoryController.AddItem(cfg, 1);

        Destroy(runtime.gameObject);
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

        if (_dragRoutine != null)
            StopCoroutine(_dragRoutine);

        _dragRoutine = StartCoroutine(DragFromWorldRoutine());
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

    private System.Collections.IEnumerator DragFromWorldRoutine()
    {
        while (_isDraggingFromWorld)
        {
            var mouse = Mouse.current;
            if (mouse == null) yield break;

            if (DragContext.Ghost != null && UiCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    UiCanvas.transform as RectTransform,
                    mouse.position.ReadValue(),
                    UiCanvas.worldCamera,
                    out var local);

                DragContext.Ghost.anchoredPosition = local;
            }

            yield return null;
        }
    }

    private void CleanupDrag()
    {
        _isDraggingFromWorld = false;

        if (_dragRoutine != null)
        {
            StopCoroutine(_dragRoutine);
            _dragRoutine = null;
        }

        DragContext.Clear();
    }
}
