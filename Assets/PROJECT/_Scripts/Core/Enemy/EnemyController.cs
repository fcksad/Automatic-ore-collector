using Service;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private EnemyBase enemyPrefab;
    [SerializeField] private List<Transform> spawnPoints = new();
    [SerializeField] private int maxAlive = 24;
    [SerializeField] private float spawnInterval = 1.0f;   

    [Header("Tick budget")]
    [SerializeField] private int ticksPerFrame = 12;        

    // services
    private IInstantiateFactoryService _factory;
    private IAudioService _audio;
    private IParticleService _particles;

    private VehicleController _vehicleController;

    private readonly List<EnemyBase> _alive = new();
    private float _nextSpawnTime;
    private int _tickCursor;

    private readonly List<VehicleBase> _targets = new();


    private void Awake()
    {
        _factory = ServiceLocator.Get<IInstantiateFactoryService>();
        _audio = ServiceLocator.Get<IAudioService>();
        _particles = ServiceLocator.Get<IParticleService>();
    }

    private void Start()
    {
        _vehicleController = SceneServiceLocator.Current.Get<VehicleController>();
        RefreshTargets();
    }

    private void Update()
    {
        if (enemyPrefab && spawnPoints.Count > 0 && _alive.Count < maxAlive && Time.time >= _nextSpawnTime)
        {
            _nextSpawnTime = Time.time + spawnInterval;
            SpawnAt(spawnPoints[Random.Range(0, spawnPoints.Count)]);
        }

        if (_alive.Count == 0) return;

        int n = Mathf.Min(ticksPerFrame, _alive.Count);
        for (int k = 0; k < n; k++)
        {
            if (_alive.Count == 0) break;
            _tickCursor %= _alive.Count;
            var e = _alive[_tickCursor];
            if (!e)
            {
                _alive.RemoveAt(_tickCursor);
                continue;
            }

            e.Tick();             

            _tickCursor++;
        }
    }

    public EnemyBase SpawnAt(Transform spawnPoint)
        => SpawnAt(spawnPoint.position, spawnPoint.rotation);

    public EnemyBase SpawnAt(Vector3 pos, Quaternion rot)
    {
        var enemy = _factory.Create(enemyPrefab, position: pos, rotation: rot, parent: transform);
        enemy.gameObject.SetActive(true);

        enemy.Initialize(this, _audio, _particles);

        enemy.AssignTarget(GetNearestVehicle(enemy.transform.position));

        _alive.Add(enemy);
        return enemy;
    }

    public void Release(EnemyBase enemy)
    {
        if (!enemy) return;

        _factory.Release(enemy);
    }

    public void RefreshTargets()
    {
        _targets.Clear();
        _targets.AddRange(_vehicleController.GetVehicles());
    }

    public VehicleBase GetNearestVehicle(Vector3 from)
    {
        VehicleBase best = null;
        float bestSqr = float.PositiveInfinity;
        for (int i = 0; i < _targets.Count; i++)
        {
            var v = _targets[i];
            if (!v) continue;
            float sqr = (v.transform.position - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = v;
            }
        }
        return best;
    }

}
