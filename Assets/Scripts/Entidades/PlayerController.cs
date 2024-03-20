using UnityEngine;

public abstract class PlayerController : MovableEntity
{

    public static PlayerController instance;
    public Movements face = Movements.DOWN;
    public bool dashActive = false;
    public bool moves = false;
    private static int BASE_HP = 1000;
    private static int BASE_ATTACK = 10;
    private static int BASE_DEFENSE = 0;
    private static int BASE_RANGE = 1;
    private static CharacterState data = new CharacterState(BASE_HP, BASE_ATTACK, BASE_DEFENSE, new UnityEngine.Vector2Int(-1, -1), -1, ENTITIES_TYPE.PLAYER, BASE_RANGE);
    public static CharacterState defaultStatePlayer { get { return data.clone(); } }

    private void Awake()
    {
        instance = this;
        animationNamesInit("playerChop");
        statsInit(new CharacterState(500, 5, 0, Vector2Int.zero, -1, ENTITIES_TYPE.PLAYER, 1));
    }

    
    public override bool moveTask(Movements mv) {
        if (acted && inMovement)
            return false;
        changeFaceDirection(mv);
        base.moveTask(mv);
        return true;
    }



    public override void attack(Vector2 pos) // TODO: Overridear esto yo creo que es la clave.
    {
        if (acted)
            return;
        base.attack(pos);
        
    }


    protected void changeFaceDirection(Movements mv) {
        var spriteRender = GetComponent<SpriteRenderer>();
        if (Movements.LEFT == mv)
            (spriteRender.flipX, spriteRender.flipY) = (true, false);
        else if (Movements.RIGHT == mv | Movements.DOWN == mv)
            (spriteRender.flipX, spriteRender.flipY) = (false, false);
        else
            (spriteRender.flipX, spriteRender.flipY) = (false, true);

        face = mv;

    }


    private void OnDestroy()
    {
        GameManager.instance.loseGame();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Wall");
    }

   
}
