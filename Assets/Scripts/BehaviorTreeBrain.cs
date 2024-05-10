using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VectorMethods;
using static BattleManagerUtils;

public abstract class Tarea {
    public abstract bool run();
}

public abstract class ControlTask: Tarea {
    protected Tarea[] hijos;    
    public ControlTask(Tarea[] hijos){ this.hijos = hijos;}

}

public class Selector : ControlTask // Cambiar la clase de Tarea a ControlTask
{
    public Selector(Tarea[] hijos) : base(hijos) { }

    public override bool run()
    {
        foreach (Tarea h in hijos)
            if (h.run())
                return true;
        return false;
    }
}

public class NonDeterministicSelector : ControlTask {
    public NonDeterministicSelector(Tarea[] hijos) : base(hijos) { }
    public override bool run() // TODO change
    {
        foreach (Tarea h in ShuffleList<Tarea>.shuffle(hijos))
            if (h.run())
                return true;
        return false;
    }
}

public class Sequence : ControlTask
{
    public Sequence(Tarea[] hijos) : base(hijos) { }
    public override bool run()
    {
        foreach (Tarea h in hijos)
            if (!h.run())
                return false;
        return true;
    }
}


public class NonDeterministicSequence : ControlTask
{
    public NonDeterministicSequence(Tarea[] hijos) : base(hijos) { }
    public override bool run() // TODO change
    {
        foreach (Tarea h in ShuffleList<Tarea>.shuffle(hijos))
            if (!h.run())
                return false;
        return true;
    }
}


public abstract class GameStatTask : Tarea
{
    protected BehaviorTreeBrain bt;
    protected GameState gs { get { return bt.gs; }} 

    protected GameStatTask(BehaviorTreeBrain bt)
    { this.bt = bt; }
}


public class ChasePlayerTask : GameStatTask
{
    public ChasePlayerTask(BehaviorTreeBrain bt) : base(bt) { }


    public override bool run()
    {
        if (gs.ActualExecutor.pos.distance(gs.player.pos) >= 2) { // Mirar bien esto TODO
            Action act = bt.getNearestMoveTo(gs.ActualExecutor, gs.player);
            if (act != null)
            {
                bt.lastActionAdded = act;
                return true;
            }
        }

        return false;
    }
    

}
public class AttackTask : GameStatTask
{
    public AttackTask(BehaviorTreeBrain bt) : base(bt){}

    public override bool run()
    {

        foreach (Action action in gs.legalMoves())
            if (action is Attack) {
                bt.lastActionAdded = action;
                return true;
            }

        return false;
    }

}


public class PlayerInSameRoomTask : GameStatTask
{
    public PlayerInSameRoomTask(BehaviorTreeBrain bt) : base(bt) { }

    public override bool run()
    {
        Room roomPlayer = gs.getRoomWithinEntity(gs.player);
        Room roomEnemy = gs.getRoomWithinEntity(gs.ActualExecutor);

        if (roomPlayer is null || roomEnemy is null)
            return false;
        else
            return roomPlayer.Equals(roomEnemy);
    }
}
public class NumEnemiesInSameRoomTask : GameStatTask {

    public NumEnemiesInSameRoomTask(BehaviorTreeBrain bt):base(bt){}

    public override bool run()
    {
        int enemiesInSameRoom = 0;
        CharacterState actualExecutor = gs.ActualExecutor;
        Room roomExecutor = gs.getRoomWithinEntity(actualExecutor);
        Room roomEnemy;

        if (roomExecutor == null)
            return false;

        foreach (CharacterState enemy in gs.enemiesAlive.Where(ene => ene.id != actualExecutor.id))
            if ((roomEnemy = gs.getRoomWithinEntity(enemy)) != null && roomExecutor.Equals(roomEnemy))
                enemiesInSameRoom++;

        return (enemiesInSameRoom > 0) ? true: false;
    }


}

public class LoseTimeTask : GameStatTask
{
    public LoseTimeTask(BehaviorTreeBrain bt) : base(bt) { }

    public override bool run()
    { 
        float maxDistance = float.MinValue;
        CharacterState actualExecutor = gs.ActualExecutor;
        Action act; // Poner bien
        act = new Move(actualExecutor, MovableEntity.Movements.STAY);

        foreach (Action move in gs.legalMoves().Where(m => m is Move)) // Actual executor
        {
            Vector2Int newPos = actualExecutor.pos + ((Move)move).direction.Vect;
            if (!gs.map.indexVectorRoom.ContainsKey(newPos))
                continue;
            float distance = newPos.distance(gs.player.pos);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                act = move;
            }
        }
        bt.lastActionAdded = act;
        return true;
    }

}

public class InCorridor : GameStatTask
{
    public InCorridor(BehaviorTreeBrain bt) : base(bt) { }

    public override bool run()
    {
        return bt.IsEntityInCorridor(gs.ActualExecutor);
    }

}



public class GoToNearestRoomTask : GameStatTask
{
    public GoToNearestRoomTask(BehaviorTreeBrain bt) : base(bt) { }

    private Room getNearestRoomToPos(Vector2Int pos)
    {

        float minDistance = float.MaxValue;
        Room retRoom = null;
        foreach (Room room in gs.map.rooms)
        {
            float dist = room.Center.distance(pos);
            if (dist < minDistance)
            {
                minDistance = dist;
                retRoom = room;
            }
        }
        return retRoom;

    }

    public override bool run()
    {
        Vector2Int pos = gs.ActualExecutor.pos;
        Room nearestRoom = getNearestRoomToPos(pos);
        bt.lastActionAdded = bt.getNearestMoveToPos(gs.ActualExecutor, nearestRoom.Center);

        return true;
    }

 
}

public class BehaviorTreeBrain : EnemiesBrain{

    private Tarea root;
    private List<Action> accionesGeneradas;
    public Action lastActionAdded;
    public BehaviorTreeBrain(GameState gs): base(gs){
        accionesGeneradas = new List<Action>();

        Tarea inNarrowPosition = new InCorridor(this);
        Tarea attack = new AttackTask(this);
        Tarea loseTime = new LoseTimeTask(this);
        Tarea goToNearestRoom = new GoToNearestRoomTask(this);
        Tarea areMoreEnemiesInSameRoom = new NumEnemiesInSameRoomTask(this);
        Tarea chasePlayer = new ChasePlayerTask(this);
        Tarea isPlayerInSameRoom = new PlayerInSameRoomTask(this);

        Tarea SelectorGoToWiderPosition = new NonDeterministicSelector(new Tarea[] { goToNearestRoom, attack });
        Tarea sequenceAvoidCorridor = new Sequence(new Tarea[] { inNarrowPosition, SelectorGoToWiderPosition });

        Tarea attackPlayerSelector = new Selector(new Tarea[] { chasePlayer, attack }); // Poner tarea chase Player

        Tarea sequenceMultipleEnemiesRoom = new Sequence(new Tarea[] { isPlayerInSameRoom, areMoreEnemiesInSameRoom, attackPlayerSelector });


        Tarea sequenceFewEnemiesRoom = new NonDeterministicSelector(new Tarea[] { loseTime, attack});


        root = new Selector(new Tarea[] { sequenceAvoidCorridor, sequenceMultipleEnemiesRoom, sequenceFewEnemiesRoom });


    }

    public void UpdateGamestate(Action act)
    {
        gs.applyAction(act);
    }

    public void takeADecision()
    {
        root.run();
    }


    public override List<Action> makeDecision() // TODO resolver nulos, lo hare por la noche
    {
        accionesGeneradas.Clear();
        foreach (CharacterState cs in new List<CharacterState>(gs.enemiesAlive))
        {
            takeADecision();
            if (lastActionAdded == null)
                lastActionAdded = new Move(cs, MovableEntity.Movements.STAY);
            
            accionesGeneradas.Add(lastActionAdded);

            gs.applyAction(lastActionAdded);
            lastActionAdded = null;
            // Hay que updatear el gamestate
        }
    
        return new List<Action>(accionesGeneradas);
    }




}