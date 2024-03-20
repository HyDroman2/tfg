using System.Collections;
using System.Collections.Generic;
public class EnemyController : MovableEntity
{



    public virtual bool executeAction(Action action)
    {
        if (action == null)
            return false;
        if (action is Move)
            moveTask(((Move)action).direction);
        else
            attack(((Attack)action).victim.position);
        return true;

    }
    private void OnDestroy()
    {
        GameManager.instance.increaseDeadEnemyCount();
    }
}   
