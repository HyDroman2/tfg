using UnityEngine;
using TMPro;

// Hace falta comunicacion con el gameManager
public class UIController : MonoBehaviour 
{
    private static TMP_Text scoreUI = null;
    private static GameObject deadPanel = null;
    private static GameObject floorCompletedPanel = null;
    private static GameObject testResultsPanel = null;

    void Start()
    {
        scoreUI = transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>(); // Igual poner un diccionario
        deadPanel = transform.GetChild(1).gameObject;
        deadPanel.SetActive(false);
        floorCompletedPanel = transform.GetChild(2).gameObject;
        floorCompletedPanel.SetActive(false);
        testResultsPanel = transform.GetChild(3).gameObject;
        testResultsPanel.SetActive(false);
    }


    public static void updateScore(int score)
    {
        scoreUI.text = "Score: " + score;
    }

    // TODO hacer metodo que habilite o instancie el prefab del mensaje de muerte.
    public static void deadScreen(int floor, int score) {
        TMP_Text deadMSG = deadPanel.transform.GetChild(0).GetComponent<TMP_Text>();
        deadMSG.text = string.Format("Has muerto\n Piso:{0} Muertes:{1}",floor, score);
        deadPanel.SetActive(true);

    }

    public static void completeFloorScreen() {
        floorCompletedPanel.SetActive(true);
    }

    
    public static void showEndTestScreen()
    {
        string msg = "Se han finalizado las pruebas";
        TMP_Text scoredMSG = testResultsPanel.transform.GetChild(1).GetComponent<TMP_Text>();
        scoredMSG.text = msg;
        testResultsPanel.SetActive(true);

    }


}
