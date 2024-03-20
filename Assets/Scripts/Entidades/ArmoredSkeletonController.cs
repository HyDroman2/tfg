public class ArmoredSkeletonController : EnemyController
{
    // Pensar si renta solo con el nombre del prefab.
    private const string ENEMYBASENAME = "ArmoredSkeleton";
    private const int BASE_HP = 50;
    private const int BASE_ATTACK = 10;
    private const int BASE_DEFENSE = 5;
    private const int BASE_RANGE = 1;
    private static CharacterState data = new CharacterState(BASE_HP, BASE_ATTACK, BASE_DEFENSE, new UnityEngine.Vector2Int(-1, -1), 1, ENTITIES_TYPE.ARMORED_SKELETON, BASE_RANGE);
    public static CharacterState Data = data.clone();

    // Start is called before the first frame update
    void Awake()
    {
        animationNamesInit("enemyAttack");
        name = ENEMYBASENAME;
    }
}
