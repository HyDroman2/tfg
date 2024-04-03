using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BattleManagerUtils;

public class BattleManager : MonoBehaviour
{
    private bool playerTurn;
    private bool canEnemiesAct;
    private bool canPlayerAct;
    private List<EnemyController> enemiesCtrl; // tiene en el index 0 al jugador y de ahi en arriba al resto.
    private IEnumerable<EnemyController> enemiesCtrlAlive { get { return enemiesCtrl.Where(chr => chr != null); } }
    PlayerController playerCtrl;
    private static BattleManager battleManager;
    public bool dashTurn;
    private int countDash;
   
    public BRAIN_TYPES enemiesBrain { get; set; }


    public static BattleManager get()
    {
        if (battleManager != null)
            return battleManager;
        else
            throw new System.InvalidOperationException("Se ha intentado acceder al Battle sin haberse instanciado"); //Cambiar el tipo de excepcion
    }


    public void initBattle(PlayerController playerCtrl, List<EnemyController> enemiesCtrl, BRAIN_TYPES enemiesBrain = BRAIN_TYPES.EAGER_BRAIN)
    {
        battleManager = this;
        this.enemiesCtrl = enemiesCtrl;
        this.playerCtrl = playerCtrl;
        this.enemiesBrain = enemiesBrain;
        coldStartBattleManager();
    }

    void Update()
    {
        if (playerTurn)
            managePlayerTurn();
        else if (countDash > 2) {
            countDash = 0;
            enablePlayerMovement();
            dashTurn = true;
            Debug.Log("Dashed");
        }
        else
            manageEnemyTurn();
    }

    private void managePlayerTurn() {

        if (playerCtrl is PlayerControllerAuto && canPlayerAct) { // Se puede acelerar algo 
            PlayerBrain playerBrain = new PlayerBrain(GameManager.instance.ActualGameState);
            Action act = playerBrain.makeDecision()[0];
            ((PlayerControllerAuto)playerCtrl).executeAction(act);
            GameManager.instance.executeAutoPlayerAction(act); // Temp  Aqui tengo que tener en cuenta el dash
            canPlayerAct = false;
            Debug.Log("Player acts");

        }

        if (!playerCtrl.hasMoved())
            return;

        if (dashTurn)
            dashTurn = false;
        countDash++;
        enableEnemiesMovement();
    }


    public void enableEnemiesMovement() {

        playerTurn = false;
        foreach (EnemyController enemy in enemiesCtrlAlive)
            enemy.acted = false;
        canEnemiesAct = true;

    }
    public void enablePlayerMovement() {
        playerTurn = true;
        playerCtrl.acted = false;
        canPlayerAct = true;
    }

    // Creo que es cuando cambia de mapa
    private void manageEnemyTurn() {


        if (canEnemiesAct) {
            canEnemiesAct = false;
            Brain brain = createBrain(enemiesBrain, GameManager.instance.ActualGameState);
            Action[] acciones = brain.makeDecision().ToArray();
            GameManager.instance.executeActions(acciones);
            Debug.Log("Enemy acts");
        }

        if (allEnemiesTakeAction())
            enablePlayerMovement();

    }

    public void setEnemiesBrainType(BRAIN_TYPES brainType) {
        enemiesBrain = brainType;
    }
    public Brain createBrain(BRAIN_TYPES brainType, GameState state) {
        Brain ret;

        switch (brainType)
        {
            case BRAIN_TYPES.EAGER_BRAIN:
                ret = new EagerEnemiesBrain(state);
                break;
            case BRAIN_TYPES.ALPHABETA_BRAIN:
                ret = new AlphaBetaPruneBrain(state);
                break;
            case BRAIN_TYPES.DECISION_TREE_BRAIN:
                ret = new DecisionTreeBrain(state);
                break;
            case BRAIN_TYPES.BEHAVIOR_TREE_BRAIN:                //ret = new BehaviorTree();
                ret = new BehaviorTreeBrain(state);
                break;

            default:
                throw new System.Exception("Se ha introducido");
        }
        return ret;
    }

    private bool allEnemiesTakeAction()
    {
        foreach (EnemyController enemy in enemiesCtrlAlive)
            if (!enemy.hasMoved())
                return false;
        return true;
    }

    private void coldStartBattleManager() {
        playerTurn = true;
        canEnemiesAct = false;
        canPlayerAct = true;
        dashTurn = false;
        countDash = 0;
        enabled = true;
    }

    public void enableBattleManager(bool coldStart = false)
    {
        enabled = true;   
    }

 
    public void disableBattleManager()
    {
        enabled = false;
    }


}


