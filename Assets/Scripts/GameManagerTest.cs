using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class GameManagerTest : GameManager
    {
        public GameState[] states;
        private int maxTestNumLevels = 10;
        private BRAIN_TYPES[] test = new BRAIN_TYPES[] { BRAIN_TYPES.DECISION_TREE_BRAIN };
        private int actualBrainIndex = 0;
        private int[] scores;
        protected override void Start()
        {
            scores = new int[test.Length];
            InitGameTest(maxTestNumLevels);
            setCameraMode(CameraController.CAMERA_MODES.FOLLOW_PLAYER);
        }

        private void updateAndStoreScore(int score)
        {
            this.score = score;
            UIController.updateScore(score);
        }

        protected override void resetGame()
        {

            if (actualBrainIndex >= test.Length && level >= maxTestNumLevels ) { 
                loseGame();
                return;
            }

            bool keepScore = true;
            // Cuando llega al máximo nivel de prueba cambia de algoritmo
            if (level >= maxTestNumLevels)
            {
                keepScore = false;
                scores[actualBrainIndex] = score;
                actualBrainIndex++;
                level = 0;
                battleManager.setEnemiesBrainType(test[actualBrainIndex]);
            }
            chargeNextLevel(states[level], keepScore);

        }

        private void storeScoreAndSetNextBrain() { 
        
        
        }

        void InitGameTest(int numStages)
        {
            states = new GameState[numStages];

            for (int i = 0; i < numStages; i++)
                states[i] = GenerateNewGameState(numEnemies);

            player = instantiatePlayer(PlayerController.defaultStatePlayer);
            playerCtrl = player.GetComponent<PlayerController>();

            loadGame(states[0]);
            battleManager.initBattle(playerCtrl = player.GetComponent<PlayerController>(), enemiesCtrl, test[actualBrainIndex]);
        }

        
    }
}