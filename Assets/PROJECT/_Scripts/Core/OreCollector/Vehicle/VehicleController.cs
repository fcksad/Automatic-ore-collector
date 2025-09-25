using Service;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [SerializeField] private List<IControllable> _controllables = new List<IControllable>();

    private IInputService _inputService;

    private void Awake()
    {
        _inputService = ServiceLocator.Get<IInputService>();
        _inputService.ChangeInputMap(InputMapType.Player);
    }

    public void SetVehicle(IControllable controllable)
    {
        _controllables.Add(controllable);
    }

    private void FixedUpdate()
    {
        if (_controllables.Count == 0 || _inputService == null) return;

        var move = _inputService.GetVector2(CharacterAction.Move);
        var rotate = _inputService.GetVector2(CharacterAction.Rotate);

        foreach (var c in _controllables)
        {
            c.Move(move);
            c.Rotate(rotate);
        }
    }
}
