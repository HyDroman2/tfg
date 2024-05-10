using UnityEngine;

public class PlayerController : MovableEntity
{

    public static PlayerController instance;
    public Movements face = Movements.DOWN;
    private static int BASE_HP = 100000;
    private static int BASE_ATTACK = 10;
    private static int BASE_DEFENSE = 0;
    private static int BASE_RANGE = 1;
    private static CharacterState data = new CharacterState(BASE_HP, BASE_ATTACK, BASE_DEFENSE, new Vector2Int(-1, -1), -1, ENTITIES_TYPE.PLAYER, BASE_RANGE);
    public static CharacterState defaultStatePlayer { get { return data.clone(); } }


    public int getBaseHp() {
        return BASE_HP;
    }
    public static string getInfoBaseStats()
    {
        return string.Format("Player: HP:{0} ATK: {1}, DEF: {2} Range: {3}", BASE_HP, BASE_ATTACK, BASE_DEFENSE, BASE_RANGE);
    }

    private void Awake()
    {
        instance = this;
        animationNamesInit("playerChop");
        statsInit(new CharacterState(500, 5, 0, Vector2Int.zero, -1, ENTITIES_TYPE.PLAYER, 1));
    }

    
    public override bool moveTask (Movements mv) {
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


    public Movements getMovement()
    {

        Movements movement = Movements.STAY;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            movement = Movements.UP;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            movement = Movements.DOWN;
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            movement = Movements.RIGHT;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            movement = Movements.LEFT;

        return movement;
    }

    private void Update()
    {
        if (GameManager.instance.autoplayEnabled || hasMoved())
            return;

        Movements mv = getMovement();
        if (mv != Movements.STAY && !inMovement)
        {
            changeFaceDirection(mv);
            GameManager.instance.executePlayerMove(mv);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            Vector2Int pos = new Vector2Int((int)transform.position.x, (int)transform.position.y) + face.Vect; // TODO
            GameManager.instance.executePlayerAttack(pos);
        }

    }

}
