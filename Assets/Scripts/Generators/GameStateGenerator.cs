using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using Random = UnityEngine.Random;


public class EnemyGenerator
{

    public IEnumerable<int> randomNumberList(int numsToGenerate, int maxValue)
    {
        return new int[numsToGenerate].Select(i => Random.Range(0, maxValue));
    }

    private CharacterState[] generateRandomEnemiesStates(int numEnemies)
    { 
        MovableEntity.ENTITIES_TYPE[] listaEnemigos = MovableEntity.enemiesType;
        List<MovableEntity.ENTITIES_TYPE> enemiesType = randomNumberList(numEnemies, listaEnemigos.Length).Select(p => listaEnemigos[p]).ToList();
        IEnumerable<CharacterState> enemies = enemiesType.Select(p => MovableEntity.models[p].clone());
        return enemies.ToArray();
    }

    public CharacterState[] generateRandomEnemiesStatesPos(Vector2Int[] positions)
    {
        CharacterState[] enemiesStates = generateRandomEnemiesStates(positions.Length);
        for (int i = 0; i < positions.Length; i++)
        {
            enemiesStates[i].id = i;
            enemiesStates[i].pos = new Vector2Int(positions[i].x, positions[i].y);
        }
        return enemiesStates;
    }

    public static CharacterState generateEnemy(Vector2Int pos, MovableEntity.ENTITIES_TYPE type, int id)
    {
        CharacterState newEnemy = MovableEntity.models[type].clone();
        newEnemy.pos = pos;
        newEnemy.id = id;
        return newEnemy;

    }
}

public class GameStateGenerator
{

    private Map map;

    private List<CharacterState> generateEnemies(IEnumerable<Vector2Int> positions)
    {
        return new EnemyGenerator().generateRandomEnemiesStatesPos(positions.ToArray()).ToList();
    }
    private CharacterState generatePlayer(Vector2Int pos, GameState gameState)
    {
        CharacterState player = (gameState != null) ? gameState.player : PlayerController.defaultStatePlayer;
        player.pos = new Vector2Int(pos.x, pos.y);
        return player;
    }

    private void generateMap(int numHabs)
    {
        map = new MapGenerator().generateNewMap("simple", numHabs);
    }

    public GameState generateNewGameState(int numEnemies, int numHabs, GameState gameState = null) 
    {
        generateMap(numHabs);

        if (numEnemies + 1 > map.tiles.Count)
            throw new Exception("El número de enemigos no entra en el mapa generado");

        int score = (gameState != null) ? gameState.score : 0;
        List<Vector2Int> positions = ShuffleList<Vector2Int>.pickRandomElements(map.tiles.ToList(), numEnemies + 1);
        CharacterState player = generatePlayer(positions[positions.Count - 1], gameState);
        List<CharacterState> enemies = generateEnemies(positions.Take(positions.Count - 1));
        
        return new GameState(player, enemies, -1, map, score); 

    }
}
