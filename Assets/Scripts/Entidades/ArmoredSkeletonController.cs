public class ArmoredSkeletonController : EnemyController
{
    private const string ENEMYBASENAME = "ArmoredSkeleton";
    private const int BASE_HP = 50;
    private const int BASE_ATTACK = 10;
    private const int BASE_DEFENSE = 5;
    private const int BASE_RANGE = 1;
    private static CharacterState data = new CharacterState(BASE_HP, BASE_ATTACK, BASE_DEFENSE, new UnityEngine.Vector2Int(-1, -1), 1, ENTITIES_TYPE.ARMORED_SKELETON, BASE_RANGE);
    public static CharacterState Data = data.clone();

    protected override void Awake()
    {
        base.Awake();
        name = ENEMYBASENAME;
    }

    public static string getInfoBaseStats()
    {
        return string.Format("{0}: HP:{1} ATK: {2}, DEF: {3} Range: {4}",
            ENEMYBASENAME, BASE_HP, BASE_ATTACK, BASE_DEFENSE, BASE_RANGE);
    }
}
