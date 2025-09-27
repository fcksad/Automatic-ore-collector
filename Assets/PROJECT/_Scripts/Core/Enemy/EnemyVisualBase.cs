using UnityEngine;

public class EnemyVisualBase : MonoBehaviour
{
    [field: SerializeField] public EnemyVisualConfig EnemyVisualConfig {  get; private set; }
    [field: SerializeField] public CustomAnimator Animmator { get; private set; }

    void Start()
    {
       // Animmator.Play(EnemyVisualConfig.Idle, timeScale: 1f, loop: true);

        // событие «удар» на предпоследнем кадре
        // (или через встроенные события клипа + ClipEventFired)
        // anim.OnPenultimateFrame(()=> DoHit());
    }

    public void PlayRun(float speedMul) => Animmator.Play(EnemyVisualConfig.Run, timeScale: speedMul, loop: true);

    public void PlayAttack()
    {
        Animmator.PlayScaledForAttack(EnemyVisualConfig.Attack, attacksPerSecond: 2, loop: false); //todo => attack speed
        Animmator.OnPenultimateFrame(() => DoHit());
    }

    void DoHit() { /* нанесение урона */ }
}
