using System;
using System.Collections.Generic;
using UnityEngine;
using VectorMethods;
using static BattleManagerUtils;

public class DecisionTreeBrain : EnemiesBrain // Tengo que pensar una forma de hacer que haya un temporal gs o encapsular el tree.
{
    private const int DISTANCE_CONSIDERED_NEAR = 10;
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
        actions.Add("Stay", new TreeNodeAction(stay));

        rangedRoot = rangedEnemyBehaviourTree();
        meleeRoot = meleeEnemyBehaviourTree();
        actualExecutor = gs.getActualExecutor();
        
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

        DecisionTreeNode arePlayerNearNode = new Decision(isPlayerInRangeNode, areMoreEnemiesNear, arePlayerNear); // Hacer are player near TODO
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
        return getNearestEnemyTo(gs.getActualExecutor(), DISTANCE_CONSIDERED_NEAR) != null;
    }

    public bool arePlayerNear()
    {
        return gs.getActualExecutor().pos.distance(gs.player.pos) <= DISTANCE_CONSIDERED_NEAR;
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
        return canAttackOneEntityToOther(gs.ActualExecutor, gs.player);
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
        CharacterState nearestEnemy = getNearestEnemyTo(gs.getActualExecutor()); // TODO: Aqui se genera el problema
        return getNearestMoveTo(gs.ActualExecutor, nearestEnemy);
    }

    public Action stay()
    {
        return new Move(gs.ActualExecutor, MovableEntity.Movements.STAY);
    }

    public Action chasePlayer()
    {
        return getNearestMoveTo(gs.ActualExecutor, gs.player);
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
        CharacterState nearestEnemy = getNearestEnemyTo(gs.getActualExecutor());
        CharacterState player = gs.player;
        // Probablemente pueda hacer un método.
        float x = (player.pos.x + nearestEnemy.pos.x) / 2;
        float y = (player.pos.y + nearestEnemy.pos.y) / 2;
        Vector2Int endPos = new Vector2Int(((int)Math.Ceiling(x)), ((int)Math.Ceiling(y)));
        return getNearestMoveToPos(gs.ActualExecutor, endPos);
    }

    // Problema es que se trata de esconder cuando no puede esconderse, por lo que se traba.
    public Action hideBehindEnemy()
    {
        CharacterState player = gs.player;
        CharacterState nearestEnemy = getNearestEnemyTo(gs.getActualExecutor());

        Vector2 temp = nearestEnemy.pos - player.pos;

        if (nearestEnemy.pos.distance(player.pos) > 2)
            return getNearestMoveTo(gs.ActualExecutor,nearestEnemy);


        if (Math.Abs(temp.x) >= Math.Abs(temp.y))
        { // Refractor 

            if (temp.x > 0)
                return getNearestMoveToPos(gs.ActualExecutor,nearestEnemy.pos + Vector2Int.right);
            else
                return getNearestMoveToPos(gs.ActualExecutor,nearestEnemy.pos + Vector2Int.left);
        }
        else
            if (temp.y > 0)
            return getNearestMoveToPos(gs.ActualExecutor,nearestEnemy.pos + Vector2Int.up);
        else
            return getNearestMoveToPos(gs.ActualExecutor,nearestEnemy.pos + Vector2Int.down);

    }



}
