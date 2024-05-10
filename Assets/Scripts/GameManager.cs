using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
public class GameManager : MonoBehaviour
{

    public GameObject playerPrefab;
    public GameObject[] enemiesPrefab;
    public GameObject[] entitiesPrefab;
    public UIController uiController;
    public int numHabs; // Puede ser random
    public int numEnemies;

    //Static instance of GameManager which allows it to be accessed by any other script.
    public static GameManager instance = null;

    private CameraController cameraController; // cambiar nombre.
    protected RoomManager roomManager;
    protected BattleManager battleManager;
    protected GameState actualGamestate;

    public GameState ActualGameState { get { return actualGamestate.clone(); } }

    protected int level = 0;
    protected int score = 0;
    protected GameObject player;

    public List<EnemyController> enemiesCtrl = new List<EnemyController>();
    protected PlayerController playerCtrl;
    public Vector3 playerPosition { get { return playerCtrl.transform.position; } }

    public bool autoplayEnabled = true;
    protected bool mainLoopActive = true;
    //Esta función ha sido extraida del tutorial unity RogueGame2D
    //Awake is always called before any Start functions
    void Awake()
    {
        //Check if instance already exists
        if (instance == null)
            //if not, set instance to this
            instance = this;
        //If instance already exists and it's not this:
        else if (instance != this)
            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);

        roomManager = GetComponent<RoomManager>();
        battleManager = GetComponent<BattleManager>();
        uiController = GameObject.Find("GameUI").GetComponent<UIController>();
        cameraController = GameObject.Find("Main Camera").GetComponent<CameraController>();


    }

    protected virtual void Start()
    {
        InitGame();
        cameraController.setCameraMode(CameraController.CAMERA_MODES.FOLLOW_PLAYER);

    }

    public void setCameraMode(CameraController.CAMERA_MODES mode) => cameraController.setCameraMode(mode);
    public void stopBattle() => battleManager.disableBattleManager();
    public void enableBattle() => battleManager.enableBattleManager();


    public void endGame()
    {
        mainLoopActive = false;
        setCameraMode(CameraController.CAMERA_MODES.MANUAL);
        stopBattle();
        removeAllEnemiesCtrl();
        removePlayerCtrl();
    }

    private void removeAllEnemiesCtrl()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Enemy"))
            go.GetComponent<MovableEntity>().eliminate();
    }

    private void removePlayerCtrl()
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<MovableEntity>().eliminate();
    }

    public virtual void resetGame()
    {
        InitGame();
        setCameraMode(CameraController.CAMERA_MODES.FOLLOW_PLAYER);
        mainLoopActive = true;
    }
    public void loseGame()
    {
        UIController.deadScreen(level, score);
        throw new System.Exception("El juego ha acabado");
    }

    public void increaseScore()
    {
        score++;
        UIController.updateScore(score);
    }


    public void increaseDeadEnemyCount() => increaseScore();


    public void floorCompletedMsg()
    {
        UIController.completeFloorScreen();

    }
    public virtual void chargeNextLevel()
    {
        chargeLevel(createNewGameState(numEnemies, numHabs, actualGamestate), level + 1);
    }
    protected void chargeLevel(GameState newGS, int level)
    {
        this.level = level;
        loadGame(newGS); // Optimizar esto
        battleManager.initBattle(playerCtrl, enemiesCtrl);
    }

    private IEnumerable<GameObject> instantiateMultipleEntitiesState(CharacterState[] stateList, bool checkPos = true)
    {
        return stateList.Select(s => instantiateEntityState(s, checkPos));
    }


    private GameObject instantiateEntityState(CharacterState state, bool checkPos = true)
    {
        if (checkPos && actualGamestate.isAnEmptyTile(state.pos)) // Igual meto aqui una excepcion 
            return null;
        GameObject go = Instantiate(enemiesPrefab[(int)state.type]);
        go.GetComponent<MovableEntity>().statsInit(state);
        return go;
    }

    public void instantiateEnemy(Vector2Int pos, MovableEntity.ENTITIES_TYPE type)
    {

        if (isEntityInPos(pos))
            return;

        CharacterState newEnemy = EnemyGenerator.generateEnemy(pos, type, actualGamestate.enemies.Count); // TODO cambiar a algo mas elegante.
        actualGamestate.addEnemy(newEnemy);
        GameObject enemy = instantiateEntityState(newEnemy, false);
        enemiesCtrl.Add(enemy.GetComponent<EnemyController>());
    }


    public void instantiatePlayer()
    {
        player = Instantiate(playerPrefab);
        playerCtrl = player.GetComponent<PlayerController>();
        player.GetComponent<MovableEntity>().statsInit(PlayerController.defaultStatePlayer);
    }


    public void removeEnemyController(int id)
    {

        if (id < 0)
            return;

        EnemyController enemyCtrl = enemiesCtrl[id];
        enemiesCtrl[enemyCtrl.id] = null;
        enemyCtrl.eliminate();
        increaseDeadEnemyCount();
    }

    public void removeEntity(int id)
    {
        actualGamestate.eliminateEnemyById(id);
        removeEnemyController(id);
    }
    public void moveGameStateByID(int id, Vector2Int newPos)
    {
        actualGamestate.changePositionEntity(id, newPos);
    }

    public GameObject getEntityInPos(Vector2Int pos)
    {

        CharacterState state = actualGamestate.getEntityInPos(pos);
        if (state == null)
            return null;

        return (state.isPlayer) ? player.gameObject : enemiesCtrl[state.id].gameObject;

    }

    public int getEntityIdInPos(Vector2Int pos)
    {
        CharacterState state = actualGamestate.getEntityInPos(pos);
        return (state != null) ? state.id : -2;
    }


    public bool isEntityInPos(Vector2Int pos)
    {
        return actualGamestate.getEntityInPos(pos) != null;
    }

    public virtual void InitGame()
    {
        GameState newGS = createNewGameState(numEnemies, numHabs);
        instantiatePlayer();
        chargeLevel(newGS, 0);
    }

    protected GameState createNewGameState(int numEnemies, int numHabs, GameState gameState = null)
    {
        GameStateGenerator gs = new GameStateGenerator();
        return gs.generateNewGameState(numEnemies, numHabs, gameState); // Mejorar la gestion del mapa, se genera un mapa y luego la raw information y luego el mapa es extraño.
    }


    public void loadGame(GameState newGS)
    {
        newGS = newGS.clone();
        deleteAllEnemies();
        score = newGS.score;
        roomManager.loadMap(newGS.rawTiles);

        playerCtrl.teletransport(newGS.player.pos);

        enemiesCtrl = new List<EnemyController>();
        foreach (GameObject go in instantiateMultipleEntitiesState(newGS.getEnemiesState().ToArray(), false)) // La posición hace que se devuelvan nulos.
            enemiesCtrl.Add(go.GetComponent<EnemyController>());
        actualGamestate = newGS;
    }


    public void deleteAllEnemies()
    {
        foreach (EnemyController enem in enemiesCtrl.Where(e => e != null))
            enem.eliminate(); // se que hace conteo de puntos pero me sirve.
        enemiesCtrl.Clear();
    }


    //Update is called every frame.
    protected virtual void Update()
    {

        if (!mainLoopActive)
            return;

        if (actualGamestate.juegoFinalizado)
        {
            stopBattle();
            if (actualGamestate.player.hp <= 0)
                loseGame();
            else if (!autoplayEnabled)
                floorCompletedMsg();
            else
                chargeNextLevel();


        }
    }

    public Vector2Int getMouseTilePosition()
    {
        Vector2 mousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        Vector3 worldPos = cameraController.screenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 10));
        return new Vector2Int((int)Mathf.Round(worldPos.x), (int)MathF.Round(worldPos.y));
    }


    public MovableEntity getControllerById(int id)
    {
        return (id == -1) ? playerCtrl : enemiesCtrl[id];
    }

    public bool executePlayerAttack(Vector2Int pos)
    {
        if (!battleManager.playerTurn || autoplayEnabled)
            return false;

        CharacterState enemy = actualGamestate.getEntityInPos(pos);

        if (enemy == null)
            return false;
        battleManager.storePlayerAction(new Attack(actualGamestate.player, enemy));

        return true;

    }


    public bool executePlayerMove(MovableEntity.Movements mv) // Hacer que se checkee el movimiento del player
    {
        if (!battleManager.playerTurn || autoplayEnabled)
            return false;

        if (!actualGamestate.isAnEmptyTile(actualGamestate.player.pos + mv.Vect))
            return false;

        battleManager.storePlayerAction(new Move(actualGamestate.player, mv));

        return true;
    }

    //TODO realizar esto bien. Problema, los enemigos tienen garantizado que sus acciones tienen sentido el player en modo manual no.


    public void executePlayerAction(Action action, bool dashTurn = false)
    { // TODO
        actualGamestate.applyAction(action, dashTurn);

        playerCtrl.executeAction(action);
        if (action is Attack)
        {
            int enemyID = ((Attack)action).victim.id;
            if (actualGamestate.haMuertoEnemigo(enemyID))
                removeEnemyController(enemyID);
            else
                updateHealthBar(enemyID);
        }
    }
    public void executeActions(Action[] actions)
    {

        int[] actionsInt = actions.Select(p => p.executor.id).ToArray();
        foreach (Action action in actions)
            enemiesCtrl[action.executor.id].executeAction(action); // Poner que a los enemigos les baje la barra de vida

        foreach (Action action in actions)
        {
            actualGamestate.applyAction(action);
            if (action is Attack)
            {
                updateHealthBar(-1);
            }
        }


    }

    private void updateHealthBar(int id)
    {
        MovableEntity ent = getControllerById(id);
        CharacterState cs = actualGamestate.getCharacterStateByID(id);

        float maxHP = MovableEntity.models[cs.type].hp;
        float actualHP = cs.hp;
        ent.reduceBarHealth(actualHP / maxHP);
    }
}



public class ShuffleList<T> {

    
    public static IList<T> shuffle(IList<T> lista){
        List<T> ret = new List<T>(lista);
        for (int i = ret.Count-1; i > 0; i--) 
        {
            int j = Random.Range(0, i + 1);
            T aux = ret[i];
            ret[i] = ret[j];
            ret[j] = aux;
        }
        return ret;

    }

    public static List<T> pickRandomElements(IList<T> lista, int numberOfElements)
    {
        T[] auxList = lista.ToArray();

        for (int i = lista.Count - 1; i > 1; i--) // Parar el bucle cuando sea necesario
        {
            int j = Random.Range(0, i);
            T aux = auxList[i];
            auxList[i] = auxList[j];
            auxList[j] = aux;
        }
        return auxList.TakeLast(numberOfElements).ToList();

    }

    public static T pickRandomElement(IList<T> lista)
    {
        return lista[Random.Range(0, lista.Count)];
    }

}