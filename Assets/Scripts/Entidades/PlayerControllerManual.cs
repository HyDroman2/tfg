using UnityEngine;

public class PlayerControllerManual : PlayerController { 

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



    // Antes estaba en playerController
    public override bool moveTask(Movements mv)
    {

        if (!GameManager.instance.executePlayerMove(mv)) {
            changeFaceDirection(mv);
            return false;
        }
        return base.moveTask(mv);
    }
    private void Update()
    {
        if (hasMoved())
            return;

        Movements mv = getMovement();
        if (mv != Movements.STAY && !inMovement) {
            moveTask(mv);
        }
        else if (Input.GetKeyDown(KeyCode.A)) {
            Vector2Int pos = new Vector2Int((int)transform.position.x, (int)transform.position.y) + face.Vect; // TODO
            bool attackSucceded = GameManager.instance.executePlayerAttack(pos);
            if (attackSucceded)
                attack(pos);
        }
           
    }
}
