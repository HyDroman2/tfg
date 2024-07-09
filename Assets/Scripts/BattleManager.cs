using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BattleManagerUtils;

public class BattleManager : MonoBehaviour
{
    public bool playerTurn;
    private bool canEnemiesAct;
    private bool playerActed;
    private Action playerManualAction;

    private List<EnemyController> enemiesCtrl; // index 0: Player  1+: Enemies
    private IEnumerable<EnemyController> enemiesCtrlAlive { get { return enemiesCtrl.Where(chr => chr != null); } }
    PlayerController playerCtrl;
    private static BattleManager battleManager;
    public bool isDashTurn { get { return numTurn % (3 * 2) == 0; } }

    private bool dashActivated;
    public int numTurn { get; set; }
   
    public BRAIN_TYPES enemiesBrain { get; set; }
    private Brain actualEnemiesBrain;

    private Brain playerBrain;

    public static BattleManager get()
    {
        if (battleManager != null)
            return battleManager;
        else
            throw new System.InvalidOperationException("An attempt was made to access the BattleManager without being instantiated."); 
    }


    public void initBattle(PlayerController playerCtrl, List<EnemyController> enemiesCtrl)
    {
        battleManager = this;
        this.enemiesCtrl = enemiesCtrl;
        this.playerCtrl = playerCtrl;
        coldStartBattleManager();
    }



    public void initBattle(PlayerController playerCtrl, List<EnemyController> enemiesCtrl, BRAIN_TYPES enemiesBrain = BRAIN_TYPES.EAGER_BRAIN)
    {
        this.enemiesBrain = enemiesBrain;
        initBattle(playerCtrl, enemiesCtrl);
    }

    void Update()
    {
        if (GameManager.instance.actualGamestate.juegoFinalizado)
            return;
        if (playerTurn)
            managePlayerTurn();
        else if (isDashTurn)
            enablePlayerMovement(dashActivated = true);
        else
            manageEnemyTurn();
    }
    public void storePlayerAction(Action act) {
        if (playerManualAction == null)
            playerManualAction = act;
    
    }
    // QU¡ue la acción pueda venir del usuario o del auto y que el control de la GUI se comunique con rl battleManager
    private void managePlayerTurn() {

        if (!playerActed)
        {
            Action playerMove = null;

            if (GameManager.instance.autoplayEnabled) {
                playerBrain.gs = GameManager.instance.ActualGameState;
                playerMove = playerBrain.makeDecision()[0];
            } else
                playerMove = playerManualAction;


            if (playerMove != null) {
                playerCtrl.executeAction(playerMove);
                GameManager.instance.executePlayerAction(playerMove, dashActivated); 
                playerActed = true;

            }

        }
   
        if (!playerCtrl.hasMoved())
            return;

        enableEnemiesMovement();
    }


    public void enableEnemiesMovement() {

        playerTurn = false;
        foreach (EnemyController enemy in enemiesCtrlAlive)
            enemy.acted = false;
        canEnemiesAct = true;
        numTurn++;

    }

    public void enablePlayerMovement(bool dashActivated = false) {
        playerTurn = true;
        playerCtrl.acted = false;
        playerActed = false;
        numTurn++;
        playerManualAction = null;
        this.dashActivated = dashActivated;
        Debug.Log(numTurn);
    }

    private void manageEnemyTurn() {


        if (canEnemiesAct) {
            canEnemiesAct = false;
            updateBrainInformation();
            Action[] acciones = actualEnemiesBrain.makeDecision().ToArray();
            GameManager.instance.executeActions(acciones);
        }

        if (allEnemiesTakeAction())
            enablePlayerMovement();

    }

 
    public void createBrain(BRAIN_TYPES brainType, GameState state) {;

        switch (brainType)
        {
            case BRAIN_TYPES.EAGER_BRAIN:
                actualEnemiesBrain = new EagerEnemiesBrain(state);
                break;
            case BRAIN_TYPES.EAGER_ASTAR_BRAIN:
                actualEnemiesBrain = new EagerastarEnemiesBrain(state);
                break;
            case BRAIN_TYPES.ALPHABETA_BRAIN:
                actualEnemiesBrain = new AlphaBetaPruneBrain(state);
                break;
            case BRAIN_TYPES.DECISION_TREE_BRAIN:
                actualEnemiesBrain = new DecisionTreeBrain(state);
                break;
            case BRAIN_TYPES.BEHAVIOR_TREE_BRAIN:                
                actualEnemiesBrain = new BehaviorTreeBrain(state);
                break;
            default:
                throw new System.Exception("An untreated type has been introduced");
        }
        enemiesBrain = brainType;
          
    }


    public void setEnemiesBrainType(BRAIN_TYPES brainType)
    {
        createBrain(brainType, GameManager.instance.ActualGameState.clone());
    }

    public void updateBrainInformation() => actualEnemiesBrain.gs = GameManager.instance.ActualGameState.clone(); 
    
    private bool allEnemiesTakeAction()
    {
        return enemiesCtrlAlive.Where(ene => !ene.hasMoved()).ToArray().Length == 0;
    }

    private void coldStartBattleManager() {
        createBrain(enemiesBrain, GameManager.instance.ActualGameState);
        playerBrain = new PlayerBrain(GameManager.instance.ActualGameState);
        playerTurn = true;
        dashActivated = false;
        canEnemiesAct = false;
        playerActed = false;
        enabled = true;
        numTurn = 1;
    }

    public void enableBattleManager() => enabled = true;   

    public void disableBattleManager() => enabled = false;


}


