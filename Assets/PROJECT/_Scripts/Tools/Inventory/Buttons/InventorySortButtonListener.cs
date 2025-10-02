using Inventory;
using UnityEngine;

public class InventorySortButtonListener : CustomButton
{
    [SerializeField] private GridInventoryView _view;
    private Inventory.IInventoryModel _model;


    private void Start()
    {
        Button.onClick.AddListener(SortByKindName);
    }

    private void SortByKindName()
    {
        var field = typeof(GridInventoryView).GetField("_model", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _model = (Inventory.IInventoryModel)field.GetValue(_view);
        _model?.Sort(new Inventory.SortByName());
    }


    private void OnDestroy()
    {
        Button.onClick.RemoveListener(SortByKindName);
    }
}
