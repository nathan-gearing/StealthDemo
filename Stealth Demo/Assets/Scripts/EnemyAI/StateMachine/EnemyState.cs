using UnityEngine;

public abstract class EnemyState 
{
    protected EnemyStateManager enemy;

    public EnemyState(EnemyStateManager enemy)
    {
        this.enemy = enemy;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
