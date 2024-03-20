using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VectorMethods;
using static BattleManagerUtils;

public class DecisionTreeBrain : EnemiesBrain // Tengo que pensar una forma de hacer que haya un temporal gs o encapsular el tree.
{
    CharacterState actualExecutor;
    DecisionTreeNode rangedRoot;
    DecisionTreeNode meleeRoot;
    Dictionary<string, TreeNodeAction> actions;
    public DecisionTreeBrain(GameState gs) : base(gs) {
        actions = new Dictionary<string, TreeNodeAction>();

        actions.Add("Block", new TreeNodeAction(block));
        actions.Add("ChaseEnemy", new TreeNodeAction(chaseEnemy));
        actions.Add("ChasePlayer", new TreeNodeAction(chasePlayer));
        actions.Add("HideBehindEnemy", new TreeNodeAction(hideBehindEnemy));
        actions.Add("AttackPlayer", new TreeNodeAction(attackPlayer));

        rangedRoot = rangedEnemyBehaviourTree();
        meleeRoot = meleeEnemyBehaviourTree();
        actualExecutor = gs.getActualExecutor();
        
    }

    public DecisionTreeNode rangedEnemyBehaviourTree() {
        DecisionTreeNode areMoreEnemiesNear = new Decision(actions["ChaseEnemy"], actions["ChasePlayer"], areEnemyNear);
        DecisionTreeNode areMoreEnemiesAliveNode = new Decision(actions["HideBehindEnemy"], actions["AttackPlayer"], areMoreEnemiesAlive);
        DecisionTreeNode[] options = new DecisionTreeNode[] { areMoreEnemiesAliveNode, actions["AttackPlayer"], areMoreEnemiesNear };
        DecisionTreeNode distanceFromPlayerNode = new MultiDecision(options, distanceFromPlayerIndex);
        return distanceFromPlayerNode;
    }

    public DecisionTreeNode meleeEnemyBehaviourTree() {
        DecisionTreeNode areMoreEnemiesNear = new Decision(actions["Block"], actions["ChasePlayer"], areEnemyNear);
        DecisionTreeNode isPlayerInRangeNode = new Decision(actions["AttackPlayer"], areMoreEnemiesNear, isPlayerInRange);
        return isPlayerInRangeNode;
    }
    public Action[] decisionTreeAction() // Corregir
    { 
        List<Action> acciones = new List<Action>();
        foreach (CharacterState entity in new List<CharacterState>(gs.enemiesAlive))
        {
            TreeNodeAction dec = takeADecision();
            Action act = dec.accion();
            if (act == null)
                act = new Move(entity, MovableEntity.Movements.STAY);
            acciones.Add(act);
            UpdateGamestate(act);
        }

        return acciones.ToArray();
    }


    public override List<Action> makeDecision()
    {
        return new List<Action>(decisionTreeAction());
    }


    public bool areEnemyNear()
    {
        return getNearestEnemy(10) != null;
    }

    public int distanceFromPlayer()
    {
        return (int)actualExecutor.position.distance(gs.player.position);
    }

    public int distanceFromPlayerIndex()
    {
        int distance = distanceFromPlayer();

        if (distance == 1) //Mejorar
            return 0;
        else if (distance <= actualExecutor.attackRange)
            return 1;
        else
            return 2;

    }
    public bool areMoreEnemiesAlive()
    {
        return gs.enemies.Exists(state => state != null && state.id != actualExecutor.id);
    }

    public void UpdateGamestate(Action act)
    {
        gs.applyAction(act);
        actualExecutor = gs.getActualExecutor();
    }

    public TreeNodeAction takeADecision()
    {
        DecisionTreeNode treeNode = (actualExecutor.attackRange > 1 ) ? rangedRoot:meleeRoot; // Aqui hay un bug resolver.
        while (treeNode is not TreeNodeAction)
            treeNode = treeNode.makeDecision();
        return (TreeNodeAction)treeNode;
    }

    public bool isPlayerInRange()
    {
        return Array.Exists(gs.legalMoves(actualExecutor), move => move is Attack);
    }

    public Action attackPlayer()
    {
        return new Attack(actualExecutor, gs.player);

    }

    public bool canReceiveDamageFromPlayer()
    {
        return actualExecutor.position.distance(gs.player.position) == 1;
    }

    public bool areEnemyOneBlockDistance()
    {
        return getEnemyOneBlockDistance() != null;
    }

    public CharacterState getEnemyOneBlockDistance()
    {
        //MovableEntity.Movements.opList.FirstOrDefault(r => gs.entitiesPosition[actualExecutor.position + r.Vect].id != -1); TODO
        foreach (MovableEntity.Movements move in MovableEntity.Movements.opList)
            if (gs.entitiesPosition[actualExecutor.position + move.Vect].id != -1)
                return gs.entitiesPosition[actualExecutor.position + move.Vect];
        return null;
    }


    public Action chaseEnemy()
    {
        CharacterState nearestEnemy = getNearestEnemy();
        return getNearestMoveTo(nearestEnemy);
    }

    public Action chasePlayer()
    {
        return getNearestMoveTo(gs.player);
    }

    public bool isInDeadEnd()
    {
        return SearchMethods.bfs(Vector2Int.CeilToInt(actualExecutor.position), 50, gs) == null;
    }

    public bool canHideBehindEnemy() {
        if (!areMoreEnemiesAlive())
            return false;
        return !isInDeadEnd();
    
    }
    public Action block()
    {
        CharacterState nearestEnemy = getNearestEnemy();
        CharacterState player = gs.player;
        // Probablemente pueda hacer un método.
        float x = (player.position.x + nearestEnemy.position.x) / 2;
        float y = (player.position.y + nearestEnemy.position.y) / 2;
        Vector2Int endPos = new Vector2Int(((int)Math.Ceiling(x)), ((int)Math.Ceiling(y)));
        return getNearestMoveToPos(endPos);

    }

    // Problema es que se trata de esconder cuando no puede esconderse, por lo que se traba.
    public Action hideBehindEnemy()
    {
        CharacterState player = gs.player;
        CharacterState nearestEnemy = getNearestEnemy();

        Vector2 temp = nearestEnemy.position - player.position;

        if (nearestEnemy.position.distance(player.position) > 2)
            return getNearestMoveTo(nearestEnemy);


        if (Math.Abs(temp.x) >= Math.Abs(temp.y))
        { // Refractor 

            if (temp.x > 0)
                return getNearestMoveToPos(nearestEnemy.position + Vector2Int.right);
            else
                return getNearestMoveToPos(nearestEnemy.position + Vector2Int.left);
        }
        else
            if (temp.y > 0)
            return getNearestMoveToPos(nearestEnemy.position + Vector2Int.up);
        else
            return getNearestMoveToPos(nearestEnemy.position + Vector2Int.down);

    }

    public CharacterState getNearestEnemy(float minDistance = float.PositiveInfinity)
    { // TODO igual aqui hay que hacer algo
        CharacterState nearestEnemy = null;
        foreach (CharacterState enemy in gs.enemiesAlive)
        {
            float distance = actualExecutor.position.distance(enemy.position);
            if (distance < minDistance && enemy.id != actualExecutor.id)
            {
                minDistance = distance;
                nearestEnemy = enemy;
            }
        }
        return nearestEnemy;
    }

    public Action getNearestMoveTo(CharacterState entity)
    {
        float minDistance = float.PositiveInfinity;
        Action moveTo = null;
        foreach (Action move in gs.legalMoves().Where(m => m is Move)) { 
            float distance = (actualExecutor.position + ((Move)move).direction.Vect).distance(entity.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                moveTo = move;
            }
        }

        return moveTo;
    }

    public Action getNearestMoveToPos(Vector2Int pos)
    {
        float minDistance = float.PositiveInfinity;
        Action moveTo = new Move(gs.getActualExecutor(), MovableEntity.Movements.STAY);
        foreach (Action move in gs.legalMoves().Where(m => m is Move))
        {
            float distance = (actualExecutor.position + ((Move)move).direction.Vect).distance(pos);
            if (distance < minDistance)
            {
                minDistance = distance;
                moveTo = move;
            }

        }
 
        return moveTo;
    }


}
