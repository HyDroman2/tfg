using static BattleManagerUtils;

public class PlayerControllerAuto : PlayerController
{
    PlayerBrain brain;
    public bool autoStop = false;

    private void Start()
    {
        //brain = new PlayerBrain(null);
    }

    /**
    public void autoMove()
    {
        if (hasMoved() || inMovement)
            return;

        brain.gs = GameManager.instance.ActualGameState;
        Action act = brain.makeDecision()[0];
        if (act is Attack)
            attack(((Attack)act).victim.position);
        else
            moveTask(((Move)act).direction);

    }
    */

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

}
