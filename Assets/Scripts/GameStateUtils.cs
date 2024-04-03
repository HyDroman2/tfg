using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VectorMethods;

public class CharacterState {
    public int id { get; set; }

    public MovableEntity.ENTITIES_TYPE type;
    public int attackRange;
    public int hp { get; set;}
    public int attackDamage { get; set;  }
    public int defense { get; set; }
    public Vector2Int position { get; set; }



    public CharacterState(int hp, int attackDamage, int defense, Vector2Int position, int id, MovableEntity.ENTITIES_TYPE type, int attackRange) {
        this.hp = hp;
        this.attackDamage = attackDamage;
        this.type = type;
        this.defense = defense;
        this.position = position;
        this.id = id;
        this.attackRange = attackRange;
    }

    public CharacterState clone() { // Hacer interfaz cloneable
        return new CharacterState(hp, attackDamage, defense, position, id, type, attackRange);
    }
}


public abstract class Action {

    public CharacterState executor {get; set;}

    public Action(CharacterState executor) {
        this.executor = executor;
    }

    public abstract CharacterState executeAction();

}

public class Attack : Action {
    public CharacterState victim;

    public Attack(CharacterState executor, CharacterState victim) :
        base(executor) {
        this.victim = victim;
    }

    public override CharacterState executeAction()
    {
        CharacterState newGameStateVictim = victim.clone();
        newGameStateVictim.hp -= (executor.attackDamage - newGameStateVictim.defense);
        return newGameStateVictim;
    }
}

public class Move : Action
{
    public MovableEntity.Movements direction;
    public Move(CharacterState executor, MovableEntity.Movements direction) : base(executor) { this.direction = direction; }

    public override CharacterState executeAction()
    {
        CharacterState newCharacterState = executor.clone();
        newCharacterState.position += direction.Vect;
        return newCharacterState;
    }
}


public class GameState {
    public CharacterState player { get; set; }
    public List<CharacterState> enemies; // Posible cambio
    public IEnumerable<CharacterState> enemiesAlive { get { return enemies.Where(chr => chr != null); }  } 
    public int[,] rawTiles { get { return map.rawTiles; } }
    public Map map;
    public Dictionary<Vector2Int, CharacterState> entitiesPosition;
    public int idActualExecutor = -1;
    public int numEnemigosMuertos = 0;
    public bool juegoFinalizado = false;
    public int score = 0;
   

    // El objetivo va a ser que sea rapido

    public GameState(CharacterState player, List<CharacterState> enemies, int idActualExecutor, Map map, int score) { // Ponerlo aqui
        this.player = player;
        this.enemies = enemies;
        this.map = map;
        this.score = score;
        this.idActualExecutor = idActualExecutor;
        entitiesPosition = new Dictionary<Vector2Int, CharacterState>(enemies.Count + 1);
        entitiesPosition.Add(player.position, player);

        foreach (CharacterState enemy in enemies.Where(e => e is not null))
            entitiesPosition.Add(enemy.position, enemy);
             
    }

    public CharacterState getCharacterStateByID(int id) { 
        return (id == -1) ? player: enemies[id];
    
    }

    public IEnumerable<CharacterState> getEnemiesState()
    {
        return enemies.Select(e => e.clone());
    }

    public CharacterState getPlayerState()
    {
        return player.clone();
    }

    public CharacterState getEntityInPos(Vector2Int pos) {
  
        return entitiesPosition.ContainsKey(pos) ? entitiesPosition[pos]:null;
    }
    public Action[] legalMoves(CharacterState executor)
    {
        List<Action> legalActions = new List<Action>();

        foreach (MovableEntity.Movements dir in MovableEntity.Movements.opList)
        {
            Vector2Int pos = executor.position + dir.Vect;
            if (rawTiles[pos.y, pos.x] == 1 && !entitiesPosition.ContainsKey(pos))
                legalActions.Add(new Move(executor, dir));
        }

        legalActions.AddRange(getAttackPosibilities(executor));
        if (legalActions.Count == 0)
            legalActions.Add(new Move(executor, MovableEntity.Movements.STAY));

        return legalActions.ToArray();

    }


    public bool isAnEmptyTile(Vector2Int pos)
    {
        return rawTiles[pos.y, pos.x] == 1 && !entitiesPosition.ContainsKey(pos);
    }


    // Direccion no es necesario como tal hasta la hora del ataque
    public List<Attack> getAttackPosibilities(CharacterState character) {
        Vector2Int pos;

        List<Attack> enemiesInRange = new List<Attack>();
        if (character.id == -1){
            foreach (MovableEntity.Movements dir in MovableEntity.Movements.opList)
                if (entitiesPosition.ContainsKey(pos = character.position + dir.Vect))
                    enemiesInRange.Add(new Attack(character, entitiesPosition[pos]));
        }
        else
            if ((player.position.distance(character.position)) <= character.attackRange)
                enemiesInRange.Add(new Attack(character, player));

        return enemiesInRange;
            
    }
    
    public Action[] legalMoves()
    {
        return legalMoves(getActualExecutor());
    }

    public void applyAction(Action action, bool keepTurn=false) { 
        if (action is Attack)
            ManageAttack((Attack)action);
        else
            ManageMove((Move)action);

        if(!keepTurn)
            idActualExecutor = getNextExecutor();

    }

    private void ManageMove(Move mv) {
        CharacterState newCS = mv.executeAction(); // bug se mueve dos veces el mismo esqueleto
        entitiesPosition.Remove(mv.executor.position);
        entitiesPosition.Add(newCS.position, newCS);

        // Update character state
        if (newCS.id == -1)
            player = newCS;
        else
            enemies[newCS.id] = newCS;
    }


    public bool haMuertoEnemigo(int enemyID) {
        return enemies[enemyID] == null;
    }

    private void ManageAttack(Attack attack) {
        CharacterState newCS = attack.executeAction();
        bool haMuerto = (newCS.hp <= 0) ? true:false;

        // Update character state
        if (newCS.id == -1)
            player = newCS;
        else
            enemies[newCS.id] = newCS;
        entitiesPosition[newCS.position] = newCS;

        if (haMuerto)
            if (newCS.id != -1)
                eliminateEnemy(newCS);
            else if (newCS.id == -1)
                juegoFinalizado = true;

    }

    private void eliminateEnemy(CharacterState entity) {
        entitiesPosition.Remove(entity.position);
        enemies[entity.id] = null;
        numEnemigosMuertos++;
        score++;
        if (numEnemigosMuertos >= enemies.Count)
            juegoFinalizado = true;
        else if (idActualExecutor == entity.id)
            idActualExecutor = getNextExecutor();
         // Se que es O(n pero es por el bien de la trama)
    }

    public void addEnemy(CharacterState entity) {
        if (entitiesPosition.ContainsKey(entity.position))
            return;
        enemies.Add(entity);
        entitiesPosition.Add(entity.position, entity);
    }



    public GameState clone() {
        CharacterState clonedPlayer = player.clone();   
        List<CharacterState> clonedEnemies = new List<CharacterState>(enemies);
        return new GameState(clonedPlayer, clonedEnemies, idActualExecutor, map, score);
    }
    public int getNextExecutor() { // Esto esta raro.
        for (int i = idActualExecutor + 1; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
                return i;
        }
        return -1;
    }

    public CharacterState getActualExecutor() {
        if (idActualExecutor == -1)
            return player;
        else
            return enemies[idActualExecutor];
    }
    public GameState generateNewState(Action action) { // Cambiar
        if (juegoFinalizado)
            throw new System.Exception("El juego ya ha finalizado asi que no tiene sentido haber llegado aqui");
        GameState updatedGS = clone();
        updatedGS.applyAction(action);
        return updatedGS;

    }

    public bool isTileInsideMap(Vector2Int pos) { 
        
        return map.tiles.Contains(pos);
    }

    public void changePositionEntity(int id, Vector2Int newPos) {

        if (entitiesPosition.ContainsKey(newPos))
            return;
        CharacterState cs = getCharacterStateByID(id);
        cs.position = newPos;
        entitiesPosition.Remove(cs.position);
        entitiesPosition.Add(cs.position, cs);
    }

  
    

}