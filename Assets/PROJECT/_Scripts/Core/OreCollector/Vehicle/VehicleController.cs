using Service;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [SerializeField] private List<IControllable> _controllables = new List<IControllable>();
    [SerializeField] private List<VehicleBase> _allVehicles = new List<VehicleBase>();


    private IInputService _inputService;
    private IInstantiateFactoryService _instantiateFactory;

    private void Awake()
    {
        _inputService = ServiceLocator.Get<IInputService>();

        _instantiateFactory = ServiceLocator.Get<IInstantiateFactoryService>();
    }

    public void SetVehicle(IControllable controllable)
    {
        _controllables.Add(controllable);
    }

    private void FixedUpdate()
    {
        if (_controllables.Count == 0 || _inputService == null) return;

        var move = _inputService.GetVector2(CharacterAction.Move).y;
        var rotate = _inputService.GetVector2(CharacterAction.Move).x;

        foreach (var c in _controllables)
        {
            c.Move(move);
            c.Rotate(rotate);
        }
    }

    public List<VehicleBase> GetVehicles()
    {
        return _allVehicles;    
    }
}
