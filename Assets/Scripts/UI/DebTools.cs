using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Image = UnityEngine.UI.Image;
using UnityEngine.EventSystems;
using System;
using TMPro;

public class DebTools: MonoBehaviour
{
    public static GUIActions lastbuttonpressed = GUIActions.NONE;
    public static bool isDebToolsEnabled = false;
    private bool overGUI = false;
    private List<Sprite> spriteOrder;
    private GameManager gm;
    private RoomManager rm;
    public Sprite tile1Img;
    public Sprite tile2Img;
    private Sprite[] tileOrder;
    private MovableEntity lastEntityClicked = null;
    private int currentSpriteEnemy = 0;
    private int currentSpriteTile = 0;
    private Image PlayContainer;
    private Image EnemyContainer;
    private Image BrushContainer;
    private Image MoveContainer;
    private Image DeleteEnemyContainer;
    private Image currentBlinkingContainer;
    private static TMP_Text currentAlgorithmDisplayed;


    private Color greenColor = new Color32(104, 255, 146, 139);
    private Color whiteColor = new Color32(255, 255, 255, 139);
    private Color greenColorPlay = new Color32(104, 255, 146, 255);
    private Color endBlinkColor;

    public Material borderMaterial;
    // Start is called before the first frame update

    private void Start()
    {
        gm = GameManager.instance;
        spriteOrder = new List<Sprite>();
        foreach (GameObject enemyPrefab in gm.enemiesPrefab)
            spriteOrder.Add(enemyPrefab.GetComponent<SpriteRenderer>().sprite);
       
        tileOrder = new Sprite[] { tile1Img, tile2Img };
        GameObject.Find("EnemyButton").GetComponent<Image>().color = Color.white; // TODO chequear

        EnemyContainer = GameObject.Find("EnemyContainer").GetComponent<Image>();
        BrushContainer = GameObject.Find("BrushContainer").GetComponent<Image>();
        MoveContainer = GameObject.Find("MoveContainer").GetComponent<Image>();
        DeleteEnemyContainer = GameObject.Find("DeleteEnemyContainer").GetComponent<Image>();
        PlayContainer = GameObject.Find("BtnPlay").GetComponent<Image>();
        currentAlgorithmDisplayed = GameObject.Find("CurrentAlgorithmText").GetComponent<TMP_Text>();
        endBlinkColor = greenColor;
        rm = RoomManager.get();

    }
    public enum GUIActions { 
        NONE, Play, BRUSH, ENEMIES, DELETE, MOVE
    }

    public void stopBattleManager() { // Hacer que se centre al centro del mapa
        gm.stopBattle();
        gm.setCameraMode(CameraController.CAMERA_MODES.MANUAL);
        Debug.Log("Stop pressed");
        isDebToolsEnabled = true;
        currentBlinkingContainer = null;
    }

    public void playBattleManager() {
        gm.enableBattle(); // Hacer una que sea reanudar.
        gm.setCameraMode(CameraController.CAMERA_MODES.FOLLOW_PLAYER);
        Debug.Log("Play pressed");
        isDebToolsEnabled = false;
        lastbuttonpressed = GUIActions.NONE;
        changeBlinkEle(PlayContainer, greenColorPlay);

    }

    public void brushEnabled() {
        lastbuttonpressed = GUIActions.BRUSH;
        changeBlinkEle(BrushContainer, endBlinkColor);
        rm.highlightMapArea(Color.green);
        Debug.Log("Brush pressed");
        
    }


    public void enemiesCreationEnabled() {
        if(lastbuttonpressed == GUIActions.ENEMIES)
            currentSpriteEnemy = (currentSpriteEnemy + 1) % spriteOrder.Count;
        lastbuttonpressed = GUIActions.ENEMIES;
        changeBlinkEle(EnemyContainer, endBlinkColor);
        Debug.Log("Enemies" + "Current: " + currentSpriteEnemy);
    }

    public void enemiesChangeImage() {

        if (lastbuttonpressed != GUIActions.ENEMIES)
            return;

        GameObject.Find("EnemyButton").GetComponent<Image>().sprite = 
            spriteOrder[(currentSpriteEnemy + 1) % spriteOrder.Count];
    }

    public void returnEnemyImg() {
        if (lastbuttonpressed != GUIActions.ENEMIES)
            return;
        GameObject.Find("EnemyButton").GetComponent<Image>().sprite =
           spriteOrder[currentSpriteEnemy];
    }

    public void tileIncreaseSize() {
        RectTransform tileImgTransform = (RectTransform)GameObject.Find("ImgTile").transform;
        tileImgTransform.sizeDelta = new Vector2(tileImgTransform.sizeDelta.x * 2, tileImgTransform.sizeDelta.y * 2);

    }

    public void tileDecreaseSize() {
        RectTransform tileImgTransform = (RectTransform)GameObject.Find("ImgTile").transform;
        tileImgTransform.sizeDelta = new Vector2(tileImgTransform.sizeDelta.x / 2, tileImgTransform.sizeDelta.y / 2);
    }

    public static void changeTextCurrentAlgorithm(BRAIN_TYPES brainType) {
        currentAlgorithmDisplayed.text = brainType.ToString().Replace("_BRAIN","");
    }

    public void changeTile()
    {
        GameObject tileImg = GameObject.Find("ImgTile");
        currentSpriteTile = (currentSpriteTile + 1) % tileOrder.Length;
        tileImg.GetComponent<Image>().sprite = tileOrder[currentSpriteTile];

        if (currentSpriteTile == 1)
        {
            rm.refreshMapTiles();
            rm.highlightDungeonArea(Color.red);
        }
        else {
            rm.refreshMapTiles();
            rm.highlightMapArea(Color.green);
        }
    }


    public void moveEnabled()
    {
        lastbuttonpressed = GUIActions.MOVE;
        changeBlinkEle(MoveContainer, endBlinkColor);   
    }

    public void deleteEnemyEnabled()
    {
        lastbuttonpressed = GUIActions.DELETE;
        changeBlinkEle(DeleteEnemyContainer, endBlinkColor);        
    }

  
    private void changeBlinkEle(Image container, Color color) {
        StartCoroutine(containerColorBlink(color, container));
    }


    protected IEnumerator containerColorBlink(Color end, Image container)
    {
        if(currentBlinkingContainer == container)
            yield break;
        
        currentBlinkingContainer = container;
        Color32 prevColor = container.color;
        while (container == currentBlinkingContainer)
        {
            while (((Vector4)end - (Vector4)container.color).sqrMagnitude > 0.001 && container == currentBlinkingContainer)
            {
                container.color =  Color.Lerp(container.color, end, 0.004f);
                yield return null;
            }

            while (((Vector4)whiteColor - (Vector4)container.color).sqrMagnitude > 0.001 && container == currentBlinkingContainer)
            {
                container.color = Color.Lerp(container.color, whiteColor, 0.004f);
                yield return null;
            }

        }
        container.color = prevColor;
    }


    private void Update()
    {
         
        if (!isDebToolsEnabled)
            return;

        if (!overGUI && EventSystem.current.IsPointerOverGameObject())
            overGUI = true;
        else if (overGUI && !EventSystem.current.IsPointerOverGameObject())
            overGUI = false;
        

        if (!overGUI) { 
            devToolsManagement();
        }
    }

    private void devToolsManagement() {
        Vector2Int clickedTile = gm.getMouseTilePosition();
        int entityID = gm.getEntityIdInPos(clickedTile);
        bool isEntityInPos = entityID != -2; 
        
        switch (lastbuttonpressed)
        {
            case GUIActions.BRUSH:
                if (Input.GetMouseButton(0))
                    manageBrushAction(clickedTile);
                break;

            case GUIActions.ENEMIES:
                if (Input.GetMouseButtonUp(0) && !isEntityInPos)
                    gm.instantiateEnemy(clickedTile, MovableEntity.enemiesType[currentSpriteEnemy]);
                break;

            case GUIActions.DELETE:
                if (Input.GetMouseButtonUp(0) && isEntityInPos)
                    gm.removeEntity(entityID);
                break;

            case GUIActions.MOVE:

                if (Input.GetMouseButtonDown(0) && isEntityInPos)
                    lastEntityClicked = gm.getControllerById(entityID);
                else if (Input.GetMouseButtonUp(0)) {
                    Vector3Int pos = Vector3Int.FloorToInt(lastEntityClicked.transform.position);
                    gm.moveGameStateByID(lastEntityClicked.id, (Vector2Int) pos);
                    lastEntityClicked = null;
                }
                   
                if (lastEntityClicked != null) {
                    lastEntityClicked.teletransport(clickedTile);
                }
                break;

            case GUIActions.NONE:
                break;

            default:
                throw new Exception();

        }

    }

    private void manageBrushAction(Vector2Int clickedTile) {
        if (currentSpriteTile == 0)
            rm.addTile(clickedTile);
        else
            rm.deleteTile(clickedTile);
    }


}
