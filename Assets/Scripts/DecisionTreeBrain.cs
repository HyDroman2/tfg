using System;
using System.Collections.Generic;
using UnityEngine;
using VectorMethods;
using static BattleManagerUtils;

public class DecisionTreeBrain : EnemiesBrain 
{
    private const int DISTANCE_CONSIDERED_NEAR = 10;
    CharacterState actualExecutor { get { return gs.ActualExecutor; } }
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
        actions.Add("Stay", new TreeNodeAction(stay));

        rangedRoot = rangedEnemyBehaviourTree();
        meleeRoot = meleeEnemyBehaviourTree();
        
    }
   
    public DecisionTreeNode rangedEnemyBehaviourTree() {
        DecisionTreeNode areMoreEnemiesNear = new Decision(actions["ChaseEnemy"], actions["ChasePlayer"], areEnemyNear);
        DecisionTreeNode areMoreEnemiesAliveNode = new Decision(actions["HideBehindEnemy"], actions["AttackPlayer"], canHideBehindEnemy);
        DecisionTreeNode isPlayerFightingNode = new Decision(actions["ChasePlayer"], actions["Stay"] , isPlayerFighting);
        DecisionTreeNode[] options = new DecisionTreeNode[] { areMoreEnemiesAliveNode, actions["AttackPlayer"], isPlayerFightingNode, areMoreEnemiesNear };
        DecisionTreeNode distanceFromPlayerNode = new MultiDecision(options, distanceFromPlayerIndex);
        return distanceFromPlayerNode;
    }

    public DecisionTreeNode meleeEnemyBehaviourTree() {

        DecisionTreeNode areMoreEnemiesNear = new Decision(actions["ChaseEnemy"], actions["ChasePlayer"], areEnemyNear);

        DecisionTreeNode isPlayerInRangeNode = new Decision(actions["AttackPlayer"], actions["ChasePlayer"], isPlayerInRange);

        DecisionTreeNode arePlayerNearNode = new Decision(isPlayerInRangeNode, areMoreEnemiesNear, arePlayerNear); 
        return isPlayerInRangeNode;
    }
    public Action[] decisionTreeAction() // Corregir
    { 
        List<Action> acciones = new List<Action>();
        GameState aux = gs.clone();
        foreach (CharacterState entity in new List<CharacterState>(gs.enemiesAlive))
        {
            TreeNodeAction dec = takeADecision();
            Action act = dec.accion();
            if (act == null)
                act = new Move(entity, MovableEntity.Movements.STAY);
            acciones.Add(act);
            UpdateGamestate(act);
        }
        gs = aux;
        return acciones.ToArray();
    }


    public override List<Action> makeDecision()
    {
        return new List<Action>(decisionTreeAction());
    }


    public bool areEnemyNear()
    {
        return getNearestEnemyTo(actualExecutor, DISTANCE_CONSIDERED_NEAR) != null;
    }

    public bool arePlayerNear()
    {
        return actualExecutor.pos.distance(gs.player.pos) <= DISTANCE_CONSIDERED_NEAR;
    }

    public float distanceFromPlayer()
    {
        return actualExecutor.pos.distance(gs.player.pos);
    }

    public int distanceFromPlayerIndex()
    {
        float distance = distanceFromPlayer();

        if (distance == 1) 
            return 0;
        else if (distance <= actualExecutor.attackRange)
            return 1;
        else if (distance < 5)
            return 2;
        else
            return 3;

    }
    public bool areMoreEnemiesAlive()
    {
        return gs.enemies.Exists(state => state != null && state.id != actualExecutor.id);
    }

    public void UpdateGamestate(Action act)
    {
        gs.applyAction(act);
    }

    public TreeNodeAction takeADecision()
    {
        DecisionTreeNode treeNode = (actualExecutor.attackRange > 1 ) ? rangedRoot:meleeRoot; 
        while (treeNode is not TreeNodeAction)
            treeNode = treeNode.makeDecision();
        return (TreeNodeAction)treeNode;
    }

    public bool isPlayerInRange()
    {
        return canAttackOneEntityToOther(actualExecutor, gs.player);
    }

    public Action attackPlayer()
    {
        return new Attack(actualExecutor, gs.player);

    }

    public bool canReceiveDamageFromPlayer()
    {
        return actualExecutor.pos.distance(gs.player.pos) == 1;
    }

    public Action chaseEnemy()
    {
        CharacterState nearestEnemy = getNearestEnemyTo(actualExecutor);
        return getNearestMoveTo(actualExecutor, nearestEnemy);
    }

    public Action stay()
    {
        return new Move(actualExecutor, MovableEntity.Movements.STAY);
    }

    public Action chasePlayer()
    {
        return getNearestMoveTo(actualExecutor, gs.player);
    }

    public bool isPlayerFighting() {
        CharacterState enemy = getNearestEnemyTo(gs.player);

        if (enemy == null || enemy.pos.distance(gs.player.pos) != 1)
            return false;
        else
            return true;
    
    }

    public bool isInDeadEnd()
    {
        return SearchMethods.bfs(Vector2Int.CeilToInt(actualExecutor.pos), 50, gs) == null;
    }

    public bool canHideBehindEnemy() {
        if (!areMoreEnemiesAlive())
            return false;
        return !isInDeadEnd();
    
    }
    public Action block()
    {
        CharacterState nearestEnemy = getNearestEnemyTo(actualExecutor);
        CharacterState player = gs.player;
        float x = (player.pos.x + nearestEnemy.pos.x) / 2;
        float y = (player.pos.y + nearestEnemy.pos.y) / 2;
        Vector2Int endPos = new Vector2Int(((int)Math.Ceiling(x)), ((int)Math.Ceiling(y)));
        return getNearestMoveToPos(actualExecutor, endPos);
    }


    public Action hideBehindEnemy()
    {
        CharacterState player = gs.player;
        CharacterState nearestEnemy = getNearestEnemyTo(actualExecutor);

        Vector2 temp = nearestEnemy.pos - player.pos;

        if (nearestEnemy.pos.distance(player.pos) > 2)
            return getNearestMoveTo(actualExecutor,nearestEnemy);


        if (Math.Abs(temp.x) >= Math.Abs(temp.y))
        { // Refractor 

            if (temp.x > 0)
                return getNearestMoveToPos(actualExecutor,nearestEnemy.pos + Vector2Int.right);
            else
                return getNearestMoveToPos(actualExecutor,nearestEnemy.pos + Vector2Int.left);
        }
        else
            if (temp.y > 0)
            return getNearestMoveToPos(actualExecutor,nearestEnemy.pos + Vector2Int.up);
        else
            return getNearestMoveToPos(actualExecutor,nearestEnemy.pos + Vector2Int.down);

    }



}
