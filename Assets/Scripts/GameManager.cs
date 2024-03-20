using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
public class GameManager : MonoBehaviour
{

    public GameObject playerPrefab;
    public GameObject playerAutoPrefab;
    public GameObject[] enemiesPrefab;
    public GameObject[] entitiesPrefab;
    public UIController uiController;
    public int numHabs = 3; // Puede ser random
    public int numEnemies = 1;
    public bool jugadorHaSidoDerrotado = false;
  
    //Static instance of GameManager which allows it to be accessed by any other script.
    public static GameManager instance = null;

    public CameraController cameraController; // cambiar nombre.
    protected RoomManager roomManager;
    protected BattleManager battleManager;
    private GameState actualGamestate;

    public GameState ActualGameState { get { return actualGamestate.clone(); } }

    protected int level = 0;
    protected int score = 0;
    protected GameObject player;

    public List<EnemyController> enemiesCtrl = new List<EnemyController>();
    protected PlayerController playerCtrl;
    
    public bool camaraFollowsPlayer = false;
    public bool autoplayEnabled = true;
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

    public void setCameraMode(CameraController.CAMERA_MODES mode)
    {
        cameraController.setCameraMode(mode);
    }


    public void stopGame() {
        battleManager.disableBattleManager();
    }
    public void enableGame()
    {
        battleManager.enableBattleManager();
    }

    public void loseGame() {
        UIController.deadScreen(level, score);
        jugadorHaSidoDerrotado = true;
        throw new System.Exception("El juego ha acabado");
    }

    public void increaseScore()
    {
        score++;
        UIController.updateScore(score);
    }


    public void increaseDeadEnemyCount() {
        increaseScore();
    }

    public void floorCompletedMsg() {
        UIController.completeFloorScreen();

    }
    protected virtual void resetGame() {
        chargeNextLevel(GenerateNewGameState(numEnemies, ActualGameState)); 
    }
    protected void chargeNextLevel(GameState newGS, bool keepScore = false) {
        int auxScore = (keepScore) ? score : 0; 
        level++;
        EnemyGenerator.LAST_ENEMY_NUMBER = 0;
        loadGame(newGS, auxScore); // Optimizar esto
        battleManager.initBattle(playerCtrl, enemiesCtrl);
    }

    private IEnumerable<GameObject> instantiateMultipleEntitiesState(CharacterState[] stateList, bool checkPos = true)
    {
        return stateList.Select(s => instantiateEntityState(s, checkPos));
    }


    private GameObject instantiateEntityState(CharacterState state, bool checkPos = true)
    {
        if (checkPos && actualGamestate.isAnEmptyTile(state.position)) // Igual meto aqui una excepcion 
            return null;
        GameObject go = Instantiate(enemiesPrefab[(int)state.type]);
        go.GetComponent<MovableEntity>().statsInit(state);
        return go;
    }



    public void deleteEnemy(int id) {

        if (id < 0)
            return;
         
        EnemyController enemyCtrl = enemiesCtrl[id];
        enemiesCtrl[enemyCtrl.id] = null;
        enemyCtrl.eliminate();
    }

    public void moveGameStateByID(int id, Vector2Int newPos) {
        actualGamestate.changePositionEntity(id, newPos);
    }

    public GameObject getEntityInPos(Vector2Int pos)
    {

        CharacterState state = actualGamestate.getEntityInPos(pos);
        if (state == null)
            return null;

        return (state.id == -1) ? player.gameObject : enemiesCtrl[state.id].gameObject; 

    }

    public int getEntityIdInPos(Vector2Int pos) {
        CharacterState state = actualGamestate.getEntityInPos(pos);
        return (state != null) ? state.id : -2;
    }


    public bool isEntityInPos(Vector2Int pos) {
        return actualGamestate.getEntityInPos(pos) != null;
    }

    //Initializes the game for each level.
    // Puedo hacer que todos los enemigos spawneen fuera y luego se les meta el state, en función del tipo. TODO
    void InitGame()
    {
        GameState newGS = GenerateNewGameState(numEnemies);
        player = instantiatePlayer(PlayerController.defaultStatePlayer);
        playerCtrl = player.GetComponent<PlayerController>();

        chargeNextLevel(newGS, false);
    }

    protected GameState GenerateNewGameState(int numEnemies, GameState gameState = null) { // TODO realizar siguiente nivel del anterior. Falta posicionar al player
        int score;
        CharacterState player;

        Map map = new MapFactory().generateNewMap("simple", numHabs); // El mapa como tal no se suele usar leer comentario de abajo

        if (numEnemies + 1 > map.tiles.Count)
            throw new Exception("El número de enemigos no entra en el mapa generado");

        List<Vector2Int> positions = ShuffleList<Vector2Int>.pickRandomElements(map.tiles.ToList(), numEnemies + 1); // Cambiar por el generic
        Vector2Int playerPos = positions[positions.Count - 1];
        positions.RemoveAt(positions.Count - 1);

        if (gameState != null)
        {
            player = gameState.player;
            score = gameState.score;
        }
        else {
            player = PlayerController.defaultStatePlayer;
            score = 0;
        }

        player.position = new Vector2Int(playerPos.x, playerPos.y);

        List<CharacterState> enemies = new EnemyGenerator().generateRandomEnemiesStatesPos(positions.ToArray()).ToList();

        return new GameState(player, enemies, -1, map, score); // Mejorar la gestion del mapa, se genera un mapa y luego la raw information y luego el mapa es extraño.

    }


    public void loadGame(GameState gameState, int score = -1) {  //TODO cambiar el nombre
        GameState newGS = gameState.clone();
        deleteAllEnemies();
        score = (score < 0) ? newGS.score:score; // Cambiar a this.score
        RoomManager.get().loadMap(newGS.rawTiles);

        playerCtrl.teletransport(newGS.player.position);

        enemiesCtrl = new List<EnemyController>();
        foreach (GameObject go in instantiateMultipleEntitiesState(newGS.getEnemiesState().ToArray(), false)) // La posición hace que se devuelvan nulos.
            enemiesCtrl.Add(go.GetComponent<EnemyController>());
        actualGamestate = newGS;
    }


    public void deleteAllEnemies() {
        foreach (EnemyController enem in enemiesCtrl.Where(e => e != null))
            enem.eliminate(); // se que hace conteo de puntos pero me sirve.
        enemiesCtrl.Clear();
    }

    public GameObject instantiatePlayer(CharacterState playerState) {
        GameObject go = Instantiate(autoplayEnabled ? playerAutoPrefab : playerPrefab);
        go.GetComponent<MovableEntity>().statsInit(playerState);
        return go;
    }


    //Update is called every frame.
    void Update()
    {
 
        if (actualGamestate.juegoFinalizado){
            stopGame();
            if (actualGamestate.player.hp <= 0)
                loseGame();
            else if (!autoplayEnabled)
                floorCompletedMsg();
            else
                resetGame();

        }
    }


    public void instantiateEnemy(Vector2Int pos, MovableEntity.ENTITIES_TYPE type) {

        if (isEntityInPos(pos))
            return;

        CharacterState newEnemy = EnemyGenerator.generateEnemy(pos, type);
        actualGamestate.addEnemy(newEnemy);
        GameObject enemy = instantiateEntityState(newEnemy, false);
        enemiesCtrl.Add(enemy.GetComponent<EnemyController>());
    }

    public Vector3 getPlayerPosition() {
        return playerCtrl.transform.position;
    }


    public Vector2Int getMouseTilePosition() {
        Vector2 mousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        Vector3 worldPos = cameraController.screenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 10));
        return new Vector2Int((int)Mathf.Round(worldPos.x), (int)MathF.Round(worldPos.y));
    }


    public MovableEntity getControllerById(int id) { 
        return (id == -1) ? playerCtrl: enemiesCtrl[id];
    }

    public bool executePlayerAttack(Vector2Int pos)
    {

        CharacterState enemy = actualGamestate.getEntityInPos(pos);

        if (enemy == null)
            return false;

        executePlayerAction(new Attack(actualGamestate.player, enemy)); // Cambiar
        return true;
              
    }


    public bool executePlayerMove(MovableEntity.Movements mv) // Hacer que se checkee el movimiento del player
    {

        Move playerMove = new Move(actualGamestate.player, mv);
        if (!actualGamestate.isAnEmptyTile(playerMove.executor.position + playerMove.direction.Vect))
            return false;

        executePlayerAction(playerMove);

        return true;
    }

    //TODO realizar esto bien. Problema, los enemigos tienen garantizado que sus acciones tienen sentido el player en modo manual no.

    public void executeAutoPlayerAction(Action action)
    {
        
        ((PlayerControllerAuto)playerCtrl).executeAction(action);
        executePlayerAction(action);
    }

    public void executePlayerAction(Action action, bool dashTurn = false) { // TODO
        actualGamestate.applyAction(action, battleManager.dashTurn);

        if (action is Attack)
        {
            int enemyID = ((Attack)action).victim.id;
            if (actualGamestate.haMuertoEnemigo(enemyID))
                deleteEnemy(enemyID);
            else
                updateHealthBar(enemyID);
        }
    }
    public void executeActions(Action[] actions) 
    {
    
        foreach (Action action in actions)
            enemiesCtrl[action.executor.id].executeAction(action); // Poner que a los enemigos les baje la barra de vida
       
        foreach (Action action in actions) {
            actualGamestate.applyAction(action);    
            if (action is Attack)
                updateHealthBar(-1);
        }


    }

    private void updateHealthBar(int id)
    {
        MovableEntity ent = getControllerById(id);
        CharacterState cs = actualGamestate.getCharacterStateByID(id);
        
        float maxHP = MovableEntity.models[cs.type].hp;
        float actualHP = cs.hp;
        ent.reduceBarHealth(actualHP/maxHP);
    }
}



public class EnemyGenerator {
    public static int LAST_ENEMY_NUMBER = 0;

    public IEnumerable<int> randomNumberList(int numsToGenerate, int maxValue)
    {
        return new int[numsToGenerate].Select(i => Random.Range(0, maxValue));
    }

    private CharacterState[] generateRandomEnemiesStates(int numEnemies) { // Buscar forma de almacenar todos los valores por defecto.
        MovableEntity.ENTITIES_TYPE[] listaEnemigos = MovableEntity.enemiesType;
        List<MovableEntity.ENTITIES_TYPE> enemiesType = randomNumberList(numEnemies, listaEnemigos.Length).Select(p => listaEnemigos[p]).ToList();
        IEnumerable<CharacterState> enemies = enemiesType.Select(p => MovableEntity.models[p].clone());
        return enemies.ToArray();
    }

    public CharacterState[] generateRandomEnemiesStatesPos(Vector2Int[] positions)
    { // Buscar forma de almacenar todos los valores por defecto.
        CharacterState[] enemiesStates = generateRandomEnemiesStates(positions.Length);
        for (int i = 0; i < positions.Length; i++)
        {
            enemiesStates[i].id = i;
            enemiesStates[i].position = new Vector2Int(positions[i].x, positions[i].y);
        }
        LAST_ENEMY_NUMBER += positions.Length;
        return enemiesStates;
    }

    public static CharacterState generateEnemy(Vector2Int pos, MovableEntity.ENTITIES_TYPE type) {
        CharacterState newEnemy = MovableEntity.models[type];
        newEnemy.position = pos;
        newEnemy.id = LAST_ENEMY_NUMBER;
        LAST_ENEMY_NUMBER++;
        return newEnemy;

    }
}

public class ShuffleList<T> {

    
    public static IList<T> shuffle(IList<T> lista){
        List<T> ret = new List<T>(lista);
        for (int i = ret.Count - 1; i > 1; i--) 
        {
            int j = Random.Range(0, i);
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