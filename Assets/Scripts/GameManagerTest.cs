using System.Collections.Generic;
using System.IO;
using System.Linq;
public class Score
{
    public string id { get; set; }

    public int numMuertes { get; set; }

    public Dictionary<MovableEntity.ENTITIES_TYPE, int> muertesPorTipo;
    public int numTurnos { get; set; }
    public int hp { get; set; }

    public Score(string id, int numTurnos, int hp, List<CharacterState> enemigosMuertos) {

        muertesPorTipo = new Dictionary<MovableEntity.ENTITIES_TYPE, int>();

        foreach (var t in MovableEntity.enemiesType)
            muertesPorTipo[t] = 0;

        foreach (MovableEntity.ENTITIES_TYPE t in enemigosMuertos.Select(ene => ene.type))               
            muertesPorTipo[t]++;
       

        numMuertes = muertesPorTipo.Values.Sum();

        this.numTurnos = numTurnos;
        this.id = id;
        this.hp = hp;

    }


    public string enemiesMurderedNum() {
        string datos = "";
        foreach (var parClaveValor in muertesPorTipo)
            datos += string.Format("{0}:{1}\n", parClaveValor.Key, parClaveValor.Value);
        return datos;
    }

    public override string ToString()
    {
        return string.Format("({0}) Score: {1} turnos: {2} hp: {3}\n", id.Replace("_BRAIN", ""), numMuertes, numTurnos, hp); ;
    }

}

public class ScoreManager {
    private BRAIN_TYPES[] brains;
    private int intialPlayerHP;
    public Score[,] partialScores { get; set; }

     


    public ScoreManager(BRAIN_TYPES[] brains,  int numLevels, int initialPlayerHP)
    {
        this.brains = brains;
        this.intialPlayerHP = initialPlayerHP;
        partialScores = new Score[brains.Length, numLevels];
    }


    public void addPartialScore(int brainIndex, int numLevel, Score score) {
        partialScores[brainIndex, numLevel] = score;
    }

    public string globalResults(){
  
        string globalData = "";

        for (int i = 0; i < partialScores.GetLength(0); i++) {
            int hp = intialPlayerHP;
            int numMuertes = 0;
            int numTurnos = 0;
            for (int j = 0; j < partialScores.GetLength(1); j++) {
                hp -= (intialPlayerHP - partialScores[i, j].hp);
                numMuertes += partialScores[i, j].numMuertes;
                numTurnos += partialScores[i, j].numTurnos;
            }

            globalData += string.Format("({0}) Score: {1} turnos: {2} hp: {3}\n", brains[i].ToString().Replace("_BRAIN", ""), numMuertes, numTurnos, hp);
         }

        return globalData;
    }

}
public enum GAMESTATETEST 
{ 
    TEST_NOT_END, LAST_LVL_REACHED, LAST_LVL_AND_LAST_BRAIN_REACHED
}
public class GameManagerTest : GameManager
{
    private BRAIN_TYPES[] test = new BRAIN_TYPES[] {
            BRAIN_TYPES.EAGER_ASTAR_BRAIN,
            BRAIN_TYPES.EAGER_BRAIN,
            BRAIN_TYPES.DECISION_TREE_BRAIN,
            BRAIN_TYPES.BEHAVIOR_TREE_BRAIN
      
    };
    public GameState[] states;
    private int numLevels = 20;
    private int actualBrainIndex = 0;
    private ScoreManager scoreMng;
    private bool endTest;


    private List<(int, int)> testParams;
    private int indexTestParams = 0;

    private void generateTestValues() {
        IEnumerable<int> roomsTest = Enumerable.Range(2,1);
        IEnumerable<int> numEnemiesTest = new List<int>(Enumerable.Range(1,10).Select(n => 5 * n));
        testParams = new List<(int, int)>();
        foreach (int numRoom in roomsTest)
            foreach (int numEnemies in numEnemiesTest)
                testParams.Add((numRoom, numEnemies));

    }

    protected override void Start()
    {
        generateTestValues();
        manageDungeonTestParameters();
        scoreMng = new ScoreManager(test, numLevels, PlayerController.defaultStatePlayer.hp);
        InitGame();
        setCameraMode(CameraController.CAMERA_MODES.FOLLOW_PLAYER);
    }


    private void manageDungeonTestParameters() {
        (int numHabs, int numEnemies) newParams = testParams[indexTestParams];
        indexTestParams++;
        numHabs = newParams.numHabs;
        numEnemies = newParams.numEnemies;

    }

    public override void resetGame()
    {
        generateTestLevels();          
        base.resetGame();
    }

    private bool esUltimoNivel() {
        return level + 1 >= numLevels;
    }


    protected override void Update()
    {
        if (mainLoopActive){
            base.Update();
            return;
        }

        if (endTest && indexTestParams < testParams.Count) {
            actualBrainIndex = 0;
            level = 0;
            manageDungeonTestParameters();
            resetGame();
        } else if(indexTestParams >= testParams.Count)
            showScoreScreen();

    }


    public override void chargeNextLevel()
    {
        scoreMng.addPartialScore(actualBrainIndex, level, new Score(test[actualBrainIndex].ToString(), 
            battleManager.numTurn, actualGamestate.player.hp, states[level].getEnemiesState().ToList()));


        GAMESTATETEST gameState = getActualGameStateTest();

        if(gameState == GAMESTATETEST.LAST_LVL_AND_LAST_BRAIN_REACHED){ 
            endTestMet(true);
            return;
        }

        bool keepScore = true;
        if (gameState == GAMESTATETEST.LAST_LVL_REACHED) {
            keepScore = false;
            level = -1;
            actualBrainIndex++;
            battleManager.setEnemiesBrainType(test[actualBrainIndex]);
            DebTools.changeTextCurrentAlgorithm(test[actualBrainIndex]);
        }

        level++;
        GameState newGS = states[level].clone();

        newGS.score = (keepScore) ? actualGamestate.score : 0;

        chargeLevel(newGS, level); 

    }


    private GAMESTATETEST getActualGameStateTest()
    {
        if (esUltimoNivel()) { 
            if (actualBrainIndex + 1 > test.Length - 1)
                return GAMESTATETEST.LAST_LVL_AND_LAST_BRAIN_REACHED;
            return GAMESTATETEST.LAST_LVL_REACHED;
        }

        return GAMESTATETEST.TEST_NOT_END;
    }
    public void endTestMet(bool writeResults) {
        endGame();
        if(writeResults)
            writeResultsInFile(scoreMng);
        endTest = true;
    }

    public void showScoreScreen()
    {
        UIController.showEndTestScreen();
    }

    
    public override void InitGame()
    {
        generateTestLevels();
        instantiatePlayer();
        loadGame(states[0]);

        battleManager.initBattle(playerCtrl = player.GetComponent<PlayerController>(), enemiesCtrl, test[actualBrainIndex]);
        DebTools.changeTextCurrentAlgorithm(test[actualBrainIndex]);


    }

    private void generateTestLevels() {
        states = new GameState[numLevels];

        for (int i = 0; i < numLevels; i++)
            states[i] = createNewGameState(numEnemies, numHabs);
    }

    public void writeResultsInFile(ScoreManager scoreMngs) {

    
        string docPath = "C:\\Users\\Antonio\\Desktop\\Trabajo\\tfg\\Resources";

        string nameFich = string.Format("ene{0}_rooms_{1}.txt", numEnemies, numHabs);
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, nameFich)))
        {
            writeEntitiesStats(outputFile);
            writeDungeonParams(outputFile);
            writeGlobalResults(outputFile, scoreMng);
            writePartialScores(outputFile, scoreMng.partialScores);
        }

    }

    public void writeEntitiesStats(StreamWriter outputFile) {
        outputFile.WriteLine("Entities Stats: \n");
        outputFile.WriteLine(PlayerController.getInfoBaseStats());
        outputFile.WriteLine(SkeletonController.getInfoBaseStats());
        outputFile.WriteLine(ArmoredSkeletonController.getInfoBaseStats());
        outputFile.WriteLine(RangedSkeletonController.getInfoBaseStats());
        outputFile.WriteLine("End entities Stats: \n");
    }

    public void writeDungeonParams(StreamWriter outputFile) {
        outputFile.WriteLine("Dungeon config params: ");
        outputFile.WriteLine("No Stages: " + numLevels);
        outputFile.WriteLine("No Enemies: " + numEnemies);
        outputFile.WriteLine("No Rooms: " + numHabs);
        outputFile.WriteLine("End Dungeon config params \n");
    }


    public void writeGlobalResults(StreamWriter outputFile, ScoreManager scoreMng) {
        outputFile.WriteLine("Accumulative end results:");
        outputFile.Write(scoreMng.globalResults());
        outputFile.WriteLine("");

    }

    public void writePartialScores(StreamWriter outputFile, Score[,] partialScores) {
        outputFile.WriteLine("Partial results:");
        for (int c = 0; c < partialScores.GetLength(1); c++) { 
            outputFile.WriteLine(string.Format("STAGE {0}: ", c));
            outputFile.WriteLine("Number of enemies by type");
            outputFile.Write(partialScores[0, c].enemiesMurderedNum()); // To avoid repetition only print one 
            for (int r = 0; r < partialScores.GetLength(0); r++) 
                outputFile.Write(partialScores[r, c]);
            outputFile.WriteLine("");

        }
    }




}
