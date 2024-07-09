using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VectorMethods;

public enum BRAIN_TYPES { 
    EAGER_BRAIN, EAGER_ASTAR_BRAIN, ALPHABETA_BRAIN, DECISION_TREE_BRAIN, BEHAVIOR_TREE_BRAIN
}


public class BattleManagerUtils : MonoBehaviour
{
    public static class SearchMethods {
        private static Vector2Int ROOT = new Vector2Int(-1, -1);

        public static List<Vector2Int> dfs(Vector2Int pos, GameState gs)
        {
            return computedfs(new HashSet<Vector2Int>(), pos, gs);
        }

        private static List<Vector2Int> computedfs(HashSet<Vector2Int> explored, Vector2Int pos, GameState gs) {
            
            explored.Add(pos);
            
            if (gs.entitiesPosition.ContainsKey(pos) && !gs.entitiesPosition[pos].isPlayer)
            {
                return new List<Vector2Int>();
            }

            foreach (MovableEntity.Movements item in MovableEntity.Movements.opList)
            {
                Vector2Int newPos = pos + Vector2Int.FloorToInt(item.Vect);
                if (!gs.isTileInsideMap(newPos) || explored.Contains(newPos))
                    continue;

                List<Vector2Int> retVal = computedfs(explored, newPos, gs);
                if (retVal != null)
                {
                    retVal.Add(newPos);
                    return retVal;
                }

            }

            return null;
        }

        private static List<Vector2Int> bfsPath(Dictionary<Vector2Int, Vector2Int> paths, Vector2Int end)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int aux = end;
            while (aux != ROOT)
            {
                path.Add(aux);
                aux = paths[aux];
            }
            path.Reverse();
            return path;

        }
        public static List<Vector2Int> bfs(Vector2Int pos, int maxDeep, GameState gs)
        {
            const int PLAYER_DEEPNESS = 0;
            Queue<(Vector2Int, int)> discoveredPos = new Queue<(Vector2Int, int)>();
            Dictionary<Vector2Int, Vector2Int> explTree = new Dictionary<Vector2Int, Vector2Int>();

            explTree.Add(pos, ROOT);
            discoveredPos.Enqueue((pos, PLAYER_DEEPNESS));

            while (discoveredPos.Count != 0)
            {
                (Vector2Int pos, int deepness) exploring = discoveredPos.Dequeue();
                if (exploring.deepness > maxDeep)
                    break; 

                if (gs.entitiesPosition.ContainsKey(exploring.pos) && exploring.deepness != PLAYER_DEEPNESS) 
                {
                    return bfsPath(explTree, exploring.pos);
                }

                Vector2Int[] newValidPos = MovableEntity.Movements.opList.Select(m => m.Vect + exploring.pos).
                        Where(p => gs.isTileInsideMap(p) && !explTree.ContainsKey(p)).ToArray();

                foreach (Vector2Int newPos in newValidPos)
                {
                    discoveredPos.Enqueue((newPos, exploring.deepness + 1));
                    explTree.Add(newPos, exploring.pos);
                }
            }
            return null;
        }



        public static List<Vector2Int> astar(Vector2Int initialPos, Vector2Int endPos, GameState gs)
        {
            Vector2Int extractedPos;
            Queue<(Vector2Int, float)> positions = new Queue<(Vector2Int, float)>();

            Dictionary<Vector2Int, Vector2Int> explored = new Dictionary<Vector2Int, Vector2Int>();

            explored.Add(initialPos, ROOT);
            positions.Enqueue((initialPos, 0));
            int[,] rawTiles = gs.rawTiles;

            while (positions.Count != 0)
            {
                (extractedPos, _) = positions.Dequeue();

                if (extractedPos == endPos)
                    return bfsPath(explored, extractedPos);

                foreach (MovableEntity.Movements item in MovableEntity.Movements.opList)
                {
                    Vector2Int newPos = extractedPos + Vector2Int.FloorToInt(item.Vect);
                    if (!gs.isTileInsideMap(newPos) || explored.ContainsKey(newPos))
                        continue;

                    positions.Enqueue((newPos, endPos.distance(newPos) + initialPos.manhattanDistance(newPos)));
                    explored.Add(newPos, extractedPos);

                }


                positions = new Queue<(Vector2Int, float)>(positions.OrderBy(tup => tup.Item2));

            }
            return new List<Vector2Int>();

        }

        public static List<Vector2Int> astarPlayer(Vector2Int initialPos, Vector2Int endPos, GameState gs)
        {
            Vector2Int extractedPos;
            Queue<(Vector2Int, float)> positions = new Queue<(Vector2Int, float)>(); 
            Dictionary<Vector2Int, Vector2Int> explored = new Dictionary<Vector2Int, Vector2Int>();

            explored.Add(initialPos, ROOT);
            positions.Enqueue((initialPos, 0));
            int[,] rawTiles = gs.rawTiles;

            while (positions.Count != 0)
            {
                (extractedPos, _) = positions.Dequeue();

                if (extractedPos == endPos)
                    return bfsPath(explored, extractedPos);

                bool entityInPos = gs.entitiesPosition.ContainsKey(extractedPos);
                if (entityInPos && !gs.entitiesPosition[extractedPos].isPlayer)
                    return bfsPath(explored, extractedPos);

                foreach (MovableEntity.Movements item in MovableEntity.Movements.opList)
                {
                    Vector2Int newPos = extractedPos + Vector2Int.FloorToInt(item.Vect);
                    if (!gs.isTileInsideMap(newPos) || explored.ContainsKey(newPos))
                        continue;

                    positions.Enqueue((newPos, endPos.distance(newPos) + initialPos.manhattanDistance(newPos)));
                    explored.Add(newPos, extractedPos);

                }

                positions = new Queue<(Vector2Int, float)>(positions.OrderBy(tup => tup.Item2)); 

            }
            return null;

        }

    }
    public abstract class Brain
    {
        public GameState gs { get; set; }

        public Brain(GameState gs)
        {
            this.gs = gs;
        }
        public abstract List<Action> makeDecision(); // TODO cambiar nombre


        public CharacterState getNearestEnemyTo(CharacterState source, float minDistance = float.PositiveInfinity)
        { 

            CharacterState nearestEnemy = null;

            foreach (CharacterState enemy in gs.enemiesAlive)
            {
                float distance = source.pos.distance(enemy.pos);
                if (distance < minDistance && enemy.id != source.id)
                {
                    minDistance = distance;
                    nearestEnemy = enemy;
                }
            }
            return nearestEnemy;
        }

        public Action getNearestMoveTo(CharacterState entitySource, CharacterState entitydst)
        {
            return getNearestMoveToPos(entitySource, entitydst.pos); 
        }

        public Action getNearestMoveToPos(CharacterState entitySource, Vector2Int pos)
        {
            float minDistance = float.PositiveInfinity;
            Action moveTo = new Move(gs.ActualExecutor, MovableEntity.Movements.STAY);
            foreach (Action move in gs.legalMoves().Where(m => m is Move))
            {
                float distance = (entitySource.pos + ((Move)move).direction.Vect).distance(pos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    moveTo = move;
                }

            }

            return moveTo;
        }

        public bool IsEntityInCorridor(CharacterState cs) {
            return gs.map.corridorTiles.Contains(cs.pos);
        }

        public bool canAttackOneEntityToOther(CharacterState agressor, CharacterState victim) {
            return agressor.attackRange >= agressor.pos.distance(victim.pos);
        }

    }


    public class PlayerBrain : Brain
    {

        public PlayerBrain(GameState gs) : base(gs) { }
        private const int TILEDISTANCEVIEW = 8;
        private Queue<MovableEntity.Movements> memory = null;

        public CharacterState getPlayerNearestEnemy()
        { 
            return getNearestEnemyTo(gs.player);
        }


        public override List<Action> makeDecision()
        {
            List<Action> actions = new List<Action>();
            Action act = checkNearToThink();

            if(act != null) // Se ha encontrado a un enemigo cerca
                memory = null; 
            else {
                if (memory == null || memory.Count == 0)
                    memory = getComplexPathAStar(gs.player.pos, getPlayerNearestEnemy().pos); 
                act = (memory.Count != 0) ? new Move(gs.player, memory.Dequeue()): new Move(gs.player, MovableEntity.Movements.STAY);
            }
       
            actions.Add(act);
            return actions;
        }

        private Action checkNearToThink()
        {
            List<Vector2Int> res = SearchMethods.bfs(Vector2Int.FloorToInt(gs.player.pos), TILEDISTANCEVIEW, gs); 
            Action act = null;
            if (res != null)
            {
                Vector2Int firstStepVector = res[1] - res[0];
                if (res.Count > 2)
                    act = new Move(gs.player, MovableEntity.Movements.vectToMovement[firstStepVector]);
                else
                {
                    Vector2Int enemyPosition = res[1];
                    act = new Attack(gs.player, gs.entitiesPosition[enemyPosition]);
                }
            }

            return act;
        }

        public Queue<MovableEntity.Movements> getComplexPathAStar(Vector2Int startPos, Vector2Int endPos) {
            List<Vector2Int> camino = SearchMethods.astarPlayer(startPos, endPos, gs); // Astar player
            Queue<MovableEntity.Movements> actionPath = new Queue<MovableEntity.Movements>(camino.Count);
            for (int i = 0; i < camino.Count - 1; i++) 
                actionPath.Enqueue(MovableEntity.Movements.vectToMovement[camino[i + 1] - camino[i]]);

            return actionPath;

        }

    }


    public class EagerEnemiesBrain: EnemiesBrain {


        public EagerEnemiesBrain(GameState gs) : base(gs.clone()) { }

        public virtual Action[] eagerDecision()
        { 

            List<Action> eagerActions = new List<Action>(gs.enemies.Count);
            GameState auxGS = gs.clone();
            foreach (CharacterState enemy in gs.enemiesAlive.ToArray())
            {
                Action bestAction = eagerDecisionSingleEnemy(enemy);
                gs.applyAction(bestAction);
                eagerActions.Add(bestAction);
            }

            gs = auxGS;
            return eagerActions.ToArray();
        }
        
        public virtual Action eagerDecisionSingleEnemy(CharacterState enemy)
        {
            Action bestAction = Array.Find(gs.legalMoves(enemy), act => act is Attack);

            if (bestAction != null)
                return bestAction;

            return getNearestMoveTo(enemy, gs.player); 

        }


        public override List<Action> makeDecision()
        {
            return new List<Action>(eagerDecision());
        }
    }



    public class EagerastarEnemiesBrain : EagerEnemiesBrain
    {
        private Dictionary<CharacterState, Queue<MovableEntity.Movements>> memory = new Dictionary<CharacterState, Queue<MovableEntity.Movements>>();
        public EagerastarEnemiesBrain(GameState gs) : base(gs) { }
        private const int numberOfTilesConsideredNear = 10;
        private const int euclideanDistanceConsideredNear = 5;
        public override Action eagerDecisionSingleEnemy(CharacterState enemy) 
        {
            bool eagerDecision = false;
            if (memory.ContainsKey(enemy) && memory[enemy].Count <= numberOfTilesConsideredNear 
                || gs.player.pos.distance(enemy.pos) <= euclideanDistanceConsideredNear)
                eagerDecision = true;
            else {
                manageEnemyMemory(enemy);
                if (memory[enemy].Count == 0)
                    eagerDecision = true;
            }

            if (eagerDecision)
            {
                if (memory.ContainsKey(enemy))
                    memory.Clear();
                return base.eagerDecisionSingleEnemy(enemy);
            }
            Action move = new Move(enemy, memory[enemy].Peek());
            if (gs.entitiesPosition.ContainsKey(move.executeAction().pos))
                move =  new Move(enemy, MovableEntity.Movements.STAY);
            else 
                memory[enemy].Dequeue();

            return move;
     
        }

   
        private void manageEnemyMemory(CharacterState enemy)
        {

            if (memory.ContainsKey(enemy))
            {
                if (memory.Count == 0)
                    memory[enemy] = getComplexPathAStar(enemy.pos, gs.player.pos);
            }
            else
                memory.Add(enemy, getComplexPathAStar(enemy.pos, gs.player.pos));

        }



        public Queue<MovableEntity.Movements> getComplexPathAStar(Vector2Int startPos, Vector2Int endPos)
        {
            List<Vector2Int> camino = SearchMethods.astar(startPos, endPos, gs); // Astar player
            Queue<MovableEntity.Movements> actionPath = new Queue<MovableEntity.Movements>(camino.Count);
            for (int i = 0; i < camino.Count - 1; i++)
                actionPath.Enqueue(MovableEntity.Movements.vectToMovement[camino[i + 1] - camino[i]]);

            return actionPath;

        }
    }

  




    public abstract class EnemiesBrain : Brain {
        public EnemiesBrain(GameState gs) : base(gs) { }

    }
    public class AlphaBetaPruneBrain: EnemiesBrain 
    {

        public AlphaBetaPruneBrain(GameState gs) : base(gs) { }
        public override List<Action> makeDecision()
        {
            return new List<Action>(alphaBetaPrune(3));
        }

        public Action[] alphaBetaPrune(int leftMoves) 
        {
            List<Action> optimalMovements;
            List<Action> nextMovements = new List<Action>(); 

            (_, optimalMovements) = alphaBetaPruneRecTurns(gs, leftMoves, float.MinValue, float.MaxValue); 

            for (int actIndex = optimalMovements.Count - 1; actIndex >= 0; actIndex--)
            { // Aqui esta el error porque sale dos veces el id 1 en lugar del -1 mirar.
                Action act = optimalMovements[actIndex];
                if (act.executor.isPlayer)
                    break;
                nextMovements.Add(act);
            }

            return nextMovements.ToArray();
        }

        public (float, List<Action>) alphaBetaPruneRec(GameState gs, int leftmoves,
            float alfa, float beta)
        {
            Action lastActionApplied = null;
            if (leftmoves == 0 || gs.juegoFinalizado)
            {
                return (score(gs), new List<Action>());
            }
            float newBeta, newAlfa;
            List<Action> optimalPath = null, auxOptimalPath;

            if (gs.idActualExecutor != -1)
            { //min
                foreach (Action act in gs.legalMoves())
                {
                    (newBeta, auxOptimalPath) = alphaBetaPruneRec(gs.generateNewState(act), leftmoves - 1, alfa, beta);
                    if (newBeta < beta)
                    {
                        beta = newBeta;
                        optimalPath = auxOptimalPath;
                        lastActionApplied = act;
                    }
                    if (alfa >= beta)
                        break;
                }
                if (optimalPath != null)
                    optimalPath.Add(lastActionApplied);
                return (beta, optimalPath);
            }
            else
            { // max
                foreach (Action act in gs.legalMoves())
                {
                    (newAlfa, auxOptimalPath) = alphaBetaPruneRec(gs.generateNewState(act), leftmoves - 1, alfa, beta);
                    if (newAlfa > alfa)
                    {
                        alfa = newAlfa;
                        optimalPath = auxOptimalPath;
                        lastActionApplied = act;
                    }
                    if (alfa >= beta)
                        break;
                }
                if (optimalPath != null)
                    optimalPath.Add(lastActionApplied);
                return (alfa, optimalPath);
            }

        }

        public (float, List<Action>) alphaBetaPruneRecTurns(GameState gs, int leftTurn,
        float alfa, float beta)
        { 
            Action lastActionApplied = null;
            if (leftTurn == 0 || gs.juegoFinalizado)
            {
                return (score(gs), new List<Action>());
            }
            float newBeta, newAlfa;
            List<Action> optimalPath = null, auxOptimalPath;

            if (gs.idActualExecutor != -1)
            { //min
                foreach (Action act in gs.legalMoves())
                {
                    (newBeta, auxOptimalPath) = alphaBetaPruneRecTurns(gs.generateNewState(act), leftTurn, alfa, beta);
                    if (newBeta < beta)
                    {
                        beta = newBeta;
                        optimalPath = auxOptimalPath;
                        lastActionApplied = act;
                    }
                    if (alfa >= beta)
                        break;
                }
                if (optimalPath != null)
                    optimalPath.Add(lastActionApplied);
                return (beta, optimalPath);
            }
            else
            { // max
                foreach (Action act in gs.legalMoves())
                {
                    (newAlfa, auxOptimalPath) = alphaBetaPruneRecTurns(gs.generateNewState(act), leftTurn - 1, alfa, beta);
                    if (newAlfa > alfa)
                    {
                        alfa = newAlfa;
                        optimalPath = auxOptimalPath;
                        lastActionApplied = act;
                    }
                    if (alfa >= beta)
                        break;
                }
                if (optimalPath != null)
                    optimalPath.Add(lastActionApplied);
                return (alfa, optimalPath);
            }

        }


        private float score(GameState gs)
        {
            float sumSqrDist = 0;
            float enemyKillScore = 0;
            foreach (CharacterState enemy in gs.enemies)
            {
                if (enemy is null)
                    enemyKillScore += 1;
                else
                    sumSqrDist += (gs.player.pos - enemy.pos).sqrMagnitude;
            }

            return (PlayerController.getBaseHp() - gs.player.hp) - sumSqrDist * 0.1f - enemyKillScore;
        }

    }
}





