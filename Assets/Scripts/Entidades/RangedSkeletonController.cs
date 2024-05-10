public class RangedSkeletonController : EnemyController
{

    private static string ENEMYBASENAME = "RangedSkeleton";
    private static int BASE_HP = 10;
    private static int BASE_ATTACK = 15;
    private static int BASE_DEFENSE = 0;
    private static int BASE_RANGE = 2;
    private static CharacterState data = new CharacterState(BASE_HP, BASE_ATTACK, BASE_DEFENSE, new UnityEngine.Vector2Int(-1,-1), 1, ENTITIES_TYPE.RANGED_SKELETON, BASE_RANGE);
    public static CharacterState Data = data.clone();

    protected override void Awake()
    {
        base.Awake();
        name = ENEMYBASENAME;
    }


    public static string getInfoBaseStats() {
        return string.Format("{0}: HP:{1} ATK: {2}, DEF: {3} Range: {4}", 
            ENEMYBASENAME, BASE_HP, BASE_ATTACK, BASE_DEFENSE, BASE_RANGE);
    }
}
