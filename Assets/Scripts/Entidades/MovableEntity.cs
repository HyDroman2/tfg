using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

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
    protected int percentageHealthBar = 100;
    private Slider healthBar;
    private string attackAnimationName;
    private bool isSelected = false;
    protected bool inMovement = false;
    public bool acted = false;
    public static float MOVEMENT_SPEED = 1f / 0.001f;
    public int id { get; set; }
    public sealed class Movements
    {
        public Vector2Int Vect { get; set; }

        public static readonly Movements UP    = new (Vector2Int.up);
        public static readonly Movements DOWN  = new (Vector2Int.down);
        public static readonly Movements LEFT  = new (Vector2Int.left);
        public static readonly Movements RIGHT = new (Vector2Int.right);
        public static readonly Movements STAY  = new (Vector2Int.zero);

        public static readonly Movements[] opList= new Movements[] { UP, DOWN, LEFT, RIGHT };
        public static readonly Dictionary<Vector2Int, Movements> vectToMovement = new Dictionary<Vector2Int, Movements> {
            { Vector2Int.up, UP },
            { Vector2Int.down, DOWN },
            { Vector2Int.left, LEFT },
            { Vector2Int.right, RIGHT },
        };
        private Movements(Vector2Int vect) {
            Vect = vect;
        }
       
    }

    public virtual bool executeAction(Action action)
    {
        if (action == null)
            return false;
        if (action is Move)
            moveTask(((Move)action).direction);
        else
            attack(((Attack)action).victim.pos);
        return true;

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

    public virtual void attack(Vector2 pos) { 
        if (inMovement) 
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

        StartCoroutine(SmoothHealthBar(percentage));
    }


    public void statsInit(CharacterState state) { 
        id = state.id;
        healthBar = transform.GetChild(0).GetChild(0).GetComponent<Slider>();
        teletransport(state.pos);
        name = name + ' ' + state.id;
    }

    public void animationNamesInit(string attackAnimationName)
    { 
        this.attackAnimationName = attackAnimationName;
    }
    public void eliminate() {
        Destroy(gameObject);
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
