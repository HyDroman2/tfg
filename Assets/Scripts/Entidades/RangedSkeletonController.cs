public class RangedSkeletonController : EnemyController
{

    private static string ENEMYBASENAME = "RangedSkeleton";
    private static int BASE_HP = 10;
    private static int BASE_ATTACK = 10;
    private static int BASE_DEFENSE = 0;
    private static int BASE_RANGE = 2;
    private static CharacterState data = new CharacterState(BASE_HP, BASE_ATTACK, BASE_DEFENSE, new UnityEngine.Vector2Int(-1,-1), 1, ENTITIES_TYPE.RANGED_SKELETON, BASE_RANGE);
    public static CharacterState Data = data.clone();

    void Awake()
    {
        animationNamesInit("enemyAttack");
        name = ENEMYBASENAME;
    }
}
