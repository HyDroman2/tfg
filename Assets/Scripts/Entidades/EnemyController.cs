public abstract class EnemyController : MovableEntity
{
    protected virtual void Awake()
    {
        animationNamesInit("enemyAttack");
    }


}   
