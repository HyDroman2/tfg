using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    private static float cameraSpeed = 0.1f;
    private static float zoomSpeed = 0.05f;
    private GameManager gm;
    private Camera cm;
    private CAMERA_MODES actualMode = CAMERA_MODES.STOP;
    private bool oneTime = false;
    private bool twoTime = false;
    public enum CAMERA_MODES
    {
        STOP, MANUAL, FOLLOW_PLAYER
    }
    public Vector2 getIntroducedMovementVector()
    {
        Vector2 movement = Vector2.zero;

        if (Input.GetKey(KeyCode.W))
            movement = Vector2.up * cameraSpeed;
        else if (Input.GetKey(KeyCode.S))
            movement = Vector2.down * cameraSpeed;
        else if (Input.GetKey(KeyCode.D))
            movement = Vector2.right * cameraSpeed;
        else if (Input.GetKey(KeyCode.A))
            movement = Vector2.left * cameraSpeed;

        if (Input.GetKey(KeyCode.E))
            cm.orthographicSize += zoomSpeed;
        else if (Input.GetKey(KeyCode.Q))
            cm.orthographicSize = Mathf.Max(5, cm.orthographicSize - zoomSpeed);


        if (Input.GetKey(KeyCode.I) && !oneTime)
        {
            GameManager.instance.endGame();
            oneTime = true;
        }
        if (Input.GetKey(KeyCode.H) && !twoTime) {
            GameManager.instance.resetGame();
            twoTime = true;
        }

        return movement;
    }

    void Start()
    {
        gm = GameManager.instance;
        cm = GetComponent<Camera>();
    }
    void Update()
    {

        switch (actualMode)
        {
            case CAMERA_MODES.STOP:
                break;
            case CAMERA_MODES.MANUAL:
                moveCameraManual();
                break;
            case CAMERA_MODES.FOLLOW_PLAYER:
                cameraFollowPlayer();
                break;
            default:
                throw new System.Exception("Error en la camara");
        }

    }

    public void setCameraMode(CAMERA_MODES mode) {
        actualMode = mode;
    }
    private void cameraFollowPlayer()
    {
        Vector3 translation = gm.playerPosition - transform.position;
        translation.z = 0;
        transform.Translate(translation, 0);
    }

    private void moveCameraManual() {
            Vector2 move = getIntroducedMovementVector();
            Vector3 move3 = new Vector3(move.x, move.y);
            transform.position += move3; 
    }

    public Vector3 screenToWorldPoint(Vector3 mousePosition) {
        return cm.ScreenToWorldPoint(mousePosition); 
    }
}
