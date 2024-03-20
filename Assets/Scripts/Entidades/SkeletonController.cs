public class SkeletonController : EnemyController
{
    private static string ENEMYBASENAME = "Skeleton";
    private static int BASE_HP = 10;
    private static int BASE_ATTACK = 10;
    private static int BASE_DEFENSE = 0;
    private static int BASE_RANGE = 1;

    private static CharacterState data = new CharacterState(BASE_HP, BASE_ATTACK, BASE_DEFENSE, new UnityEngine.Vector2Int(-1, -1), 1, ENTITIES_TYPE.SKELETON, BASE_RANGE);

    public static CharacterState Data = data.clone();
    // Start is called before the first frame update
    void Awake()
    {
        animationNamesInit("enemyAttack");
        name = ENEMYBASENAME;
    }

}
