using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VectorMethods;

public abstract class MovableEntity : MonoBehaviour
{
    public enum ENTITIES_TYPE
    {
        PLAYER = -1, SKELETON=0, ARMORED_SKELETON=1, RANGED_SKELETON=2
    }

    public static ENTITIES_TYPE[] enemiesType = { ENTITIES_TYPE.SKELETON, ENTITIES_TYPE.ARMORED_SKELETON, ENTITIES_TYPE.RANGED_SKELETON };


    public static Dictionary<ENTITIES_TYPE, CharacterState> models = new Dictionary<ENTITIES_TYPE, CharacterState>()
    {        
        { ENTITIES_TYPE.SKELETON, SkeletonController.Data},
        { ENTITIES_TYPE.ARMORED_SKELETON, ArmoredSkeletonController.Data},
        { ENTITIES_TYPE.RANGED_SKELETON, RangedSkeletonController.Data},
        { ENTITIES_TYPE.PLAYER, PlayerController.defaultStatePlayer}
    };
    public static List<ENTITIES_TYPE> listaEnemigos { get { return models.Keys.ToList(); } }
    protected int percentageHealthBar = 100;
    private Slider healthBar;
    private string attackAnimationName;
    private bool isSelected = false;
    protected bool inMovement = false;
    public bool acted = false;
    public static float MOVEMENT_SPEED = 1f / 0.1f;
    public int id;
    public sealed class Movements
    {
        
        public int Id { get; set; }
        public Vector2Int Vect { get; set; }

        public static readonly Movements UP    = new (0, Vector2Int.up);
        public static readonly Movements DOWN  = new (1, Vector2Int.down);
        public static readonly Movements LEFT  = new (2, Vector2Int.left);
        public static readonly Movements RIGHT = new (3, Vector2Int.right);
        public static readonly Movements STAY  = new (4, Vector2Int.zero);

        public static readonly Movements[] opList= new Movements[] { UP, DOWN, LEFT, RIGHT };
        public static readonly Dictionary<Vector2Int, Movements> vectToMovement = new Dictionary<Vector2Int, Movements> {
            { Vector2Int.up, UP },
            { Vector2Int.down, DOWN },
            { Vector2Int.left, LEFT },
            { Vector2Int.right, RIGHT },
        };
        private Movements(int id, Vector2Int vect) {
            Id = id;
            Vect = vect;
        }
       
    }

    public virtual bool moveTask(Movements mv)
    {
        // Precondición se puede mover.
        if (inMovement)
            return false;

        if (mv == Movements.STAY)
        {
            acted = true;
            return true;
        }

        // Mejorar implementacion de esta movida.
        Vector2Int endPostition = new Vector2Int((int)transform.position.x, (int)transform.position.y) + mv.Vect;
        inMovement = true;
        //await SmoothMovementTask(endPostition);
        StartCoroutine(SmoothMovement(endPostition));
        return true;

    }



    public bool hasMoved()
    {
        return acted;
    }

    public virtual void attack(Vector2 pos) { // TODO solo esta hecho el ataque del personaje principal.Resolver no sirve para los que atacan de lejos. mirar solo la posicion
        if (inMovement) //No Ataca porque esta inMovement pensar en si ataca a alguien lejos que haya uno en el camino de por medio. //Como lo hice puede atacar a travñes de los muros.
            return;

        Animator anim = GetComponent<Animator>();   
        anim.SetTrigger(attackAnimationName);
        acted = true;

    }
    
    protected IEnumerator SmoothMovement(Vector2 end) {

        Vector2 start = transform.position;
        while ((start-end).sqrMagnitude > 1e-4)
        {
            start = transform.position = Vector2.Lerp(start, end, MOVEMENT_SPEED * Time.deltaTime);
            yield return null;
        }
        inMovement = false;
        transform.position = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));
        acted = true;

    }

    protected async Task SmoothMovementTask(Vector2 end)
    {
       
        Vector2 start = transform.position;
        while ((start - end).sqrMagnitude > 1e-4)
        {
            start = transform.position = Vector2.Lerp(start, end, (1f / 0.1f) * Time.deltaTime);
            await Task.Yield();
        }
        transform.position = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));
        inMovement = false;
    }

     
    protected IEnumerator SmoothHealthBar(float percentage) {
        Vector2 endPercentage = new Vector2(percentage, 0);
        Vector2 sliderPercentage = new Vector2(healthBar.value, 0);
        while ((sliderPercentage - endPercentage).sqrMagnitude > 1e-4)
        {
            sliderPercentage = Vector2.Lerp(sliderPercentage, endPercentage, MOVEMENT_SPEED * Time.deltaTime);
            healthBar.value = sliderPercentage.x;
            yield return null;
        }
    }


    public virtual void reduceBarHealth(float percentage)
    {

        if (percentage < 0 || percentage > 100 || percentage > percentageHealthBar)
            return;

        // Checkear si el porcentaje está dentro de los valores
        StartCoroutine(SmoothHealthBar(percentage));
    }


    public void statsInit(CharacterState state) { // Separar de la obtencion de la barra
        id = state.id;
        healthBar = transform.GetChild(0).GetChild(0).GetComponent<Slider>();
        teletransport(state.position);
        name = name + ' ' + state.id;
    }

    public void animationNamesInit(string attackAnimationName)
    { // Pensar en poner los mismos nombres para las animaciones, tipo que el ataque sea compartido.
        this.attackAnimationName = attackAnimationName;
    }
    public void eliminate() {
        GameObject.Destroy(gameObject);
    }


    private void OnMouseEnter()
    {
        GetComponent<SpriteRenderer>().color = new Color32(150, 150, 150, 255);
     
    }

    private void OnMouseDrag()
    {
        isSelected = true;
    }

    private void OnMouseUp()
    {
        isSelected = false;
    }

    private void OnMouseExit()
    {
        if(!isSelected)
            GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 255);
    }

    public void teletransport(Vector2Int newPos)
    {
        transform.position = new Vector3(newPos.x, newPos.y);
    }

}
