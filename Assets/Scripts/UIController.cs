using UnityEngine;
using TMPro;

// Hace falta comunicacion con el gameManager
public class UIController : MonoBehaviour 
{
    private static TMP_Text scoreUI = null;
    private static GameObject deadPanel = null;
    private static GameObject floorCompletedPanel = null;
    void Start()
    {
        scoreUI = transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>(); // Igual poner un diccionario
        deadPanel = transform.GetChild(1).gameObject;
        deadPanel.SetActive(false);
        floorCompletedPanel = transform.GetChild(2).gameObject;
        floorCompletedPanel.SetActive(false);
    }


    public static void updateScore(int score)
    {
        scoreUI.text = "Score: " + score;
    }

    // TODO hacer metodo que habilite o instancie el prefab del mensaje de muerte.
    public static void deadScreen(int floor, int score) {
        deadPanel.SetActive(true);
        TMP_Text deadMSG = deadPanel.transform.GetChild(0).GetComponent<TMP_Text>();
        deadMSG.text = string.Format("Has muerto\n Piso:{0} Muertes:{1}",floor, score);
  
    }

    public static void completeFloorScreen() {
        floorCompletedPanel.SetActive(true);
    }


}
