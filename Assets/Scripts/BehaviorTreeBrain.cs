using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VectorMethods;
using static BattleManagerUtils;
using Random = UnityEngine.Random;

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
                return false;
        return true;
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

public class BlockCorridor : GameStatTask
{

    public BlockCorridor(BehaviorTreeBrain bt) : base(bt) { }

    public override bool run()
    {
        
        return false;
    }


}

public class MultipleEnemiesTask : GameStatTask {

    public MultipleEnemiesTask(BehaviorTreeBrain bt):base(bt){}

    public override bool run()
    {
        GameState gs = GameManager.instance.ActualGameState; // Incorrecto
        int numEnemiesNear = 0;

        foreach (CharacterState enemy in gs.enemiesAlive)
            if (enemy.id == gs.idActualExecutor && enemy.position.distance(gs.getActualExecutor().position) <= 5)
                numEnemiesNear++;

        if (numEnemiesNear >= 3)
            return true;

        return false;
    }


}

public class LoseTimeTask : GameStatTask
{
    public LoseTimeTask(BehaviorTreeBrain bt) : base(bt) { }

    public override bool run()
    { 
        float maxDistance = float.NegativeInfinity;
        CharacterState actualExecutor = gs.getActualExecutor();
        Action act; // Poner bien
        act = new Move(actualExecutor, MovableEntity.Movements.STAY);

        foreach (Action move in gs.legalMoves().Where(m => m is Move)) // Actual executor
        {
            float distance = (actualExecutor.position + ((Move)move).direction.Vect).distance(gs.player.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                act = move;
            }
        }
        bt.lastActionAdded = act;
        return true;
    }


    public Action getNearestMoveTo(CharacterState entity)
    {
        float minDistance = float.PositiveInfinity;
        Action moveTo = null;
        foreach (Action move in gs.legalMoves().Where(m => m is Move))
        {
            float distance = (gs.getActualExecutor().position + ((Move)move).direction.Vect).distance(entity.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                moveTo = move;
            }
        }

        return moveTo;
    }

}

public class InCorridor : GameStatTask
{
    public InCorridor(BehaviorTreeBrain bt) : base(bt) { }

    public override bool run()
    {
        return gs.map.corridorTiles.Contains(gs.getActualExecutor().position);
    }

}


public class GoToWiderPosition : GameStatTask
{
    public GoToWiderPosition(BehaviorTreeBrain bt) : base(bt) { }

    public override bool run()
    {
        Room habitacion = ShuffleList<Room>.pickRandomElement(gs.map.rooms);
        Vector2Int baldosa = ShuffleList<Vector2Int>.pickRandomElement(habitacion.getTiles());
        List<Vector2Int> positions = SearchMethods.astar(gs.getActualExecutor().position, baldosa, gs); // No pasa la accion.
        if (positions != null && positions.Count > 1) {
            Vector2Int moveVect = positions[1] - gs.getActualExecutor().position;
            bt.lastActionAdded = new Move(gs.getActualExecutor(), MovableEntity.Movements.vectToMovement[moveVect]);
            return true; // GoToWiderPosition
        }

        return false;
    }

 
    public bool isPlayerInRange()
    {
        return Array.Exists(gs.legalMoves(), move => move is Attack);
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
        Tarea goToWiderPosition = new GoToWiderPosition(this);
        Tarea multipleEnemiesNear = new MultipleEnemiesTask(this);
        

        Tarea SelectorGoToWiderPosition = new Selector(new Tarea[] { goToWiderPosition, attack });
        Tarea sequenceAvoidCorridor = new Sequence(new Tarea[] { inNarrowPosition, SelectorGoToWiderPosition });

        Tarea nonDeterministicSelector = new NonDeterministicSelector(new Tarea[] { loseTime, attack }); // Aqui seria block corridor
        Tarea sequenceMultipleEnemiesRoom = new Sequence(new Tarea[] { multipleEnemiesNear, nonDeterministicSelector });

        Tarea sequenceFewEnemiesRoom = new Sequence(new Tarea[] { loseTime, attack});


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
                accionesGeneradas.Add(new Move(cs, MovableEntity.Movements.STAY));
            else
                accionesGeneradas.Add(lastActionAdded);

            gs.applyAction(lastActionAdded);
            lastActionAdded = null;
            // Hay que updatear el gamestate
        }
    
        return new List<Action>(accionesGeneradas);
    }




}