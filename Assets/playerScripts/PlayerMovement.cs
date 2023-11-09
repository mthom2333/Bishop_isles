using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO.Ports;
using System.Threading;
using System;
using UnityEngine.UI;
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D CC;
    [SerializeField] private LayerMask jumpableArea;
    private Animator anim;

    private float dirX = 0f;
    private bool att = false;
    bool fisherman_facing; //false for not flipped facing right //true for flipped facing left
    [SerializeField]
    private float moveSpeed = 7f;
    [SerializeField]
    private float jumpForce = 14f;

    private SpriteRenderer sprite;

    private float timebtwAttack;
    public float startTimeBtwAttack;

    Thread _t2;
    bool connected = false;
    string inData = "6\n";
    int last_movement = 0;
    public static SerialPort our_controller = new SerialPort("/dev/cu.usbserial-0001", 115200);


    //attack
    private float attackCooldown = 1.0f;
    private bool canAttack = true;

    public Animator animator;
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 100;
    bool jump_controller = false;
    //public GameOverScreen GameOverScreen;
    public GameManagerScript gameManager;
    private bool isDead;



    [SerializeField] private AudioSource running_sound;

    //[SerializeField] private Transform characterTransform;

    private enum MovementState
    {
        idle,
        running,
        attacking,
        jumping,
        falling,
        spear_attack
    }


    public Transform Spear;

    Vector2 direction;
    public GameObject Projectile;

    public float ProjectileSpeed;

    public Transform ShootPoint;

    public float fireRate;
    float ReadyForNextShot;

    bool from_keyboard = false;
    void _func2(object obj)
    {
        //Debug.Log("IM HEREEE");
        connected = true;
        try
        {
            our_controller.Open();
        }
        catch
        {
            Debug.Log("NO CONNECTOR");
            connected = false;
        }
        if (connected)
            while (true)
            {
                try
                {
                    inData = our_controller.ReadLine();
                    if (inData.Length > 3 || inData.Length == 1)
                        inData = "6\n";
                }
                catch
                {
                    Debug.Log("NO CONNECTOR");
                    break;
                }
                Debug.Log(inData);
            }

    }
    private void Start()
    {
        our_controller.Close();

        _t2 = new Thread(_func2);
        _t2.Start();
        //canMove = true;
        CC = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        Debug.Log("RESTARTEEEEEED");
        fisherman_facing = sprite.flipX;


    }


    private void Update()
    {
        //   Debug.Log(sprite.sprite.name);
        if (sprite.sprite.name.Contains("Fisherman"))
        {
            if (sprite.flipX && sprite.flipX != fisherman_facing)
            {
                //Spear.Rotate(0,0, 180);
                fisherman_facing = sprite.flipX;
                Debug.Log("ROTATED to TRUE");
                direction = new Vector2(-1, 0);


            }
            if (!sprite.flipX && sprite.flipX != fisherman_facing)
            {
                //Spear.Rotate(0,0 , -180);
                fisherman_facing = sprite.flipX;
                Debug.Log("ROTATED to False");
                direction = new Vector2(1, 0);


            }


        }
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //direction = mousePos - (Vector2)Spear.position;
        //  Debug.Log(direction);
        FaceMouse();



        int controller = int.Parse(inData);
        //att = Input.GetKey(KeyCode.H);

        bool rod_attack = false;
        
        bool spear_attack = false;
        //change int.Parse to compare to string in future
        if (controller == 7)
        {
            last_movement = 7;
            dirX = 1;
        }
        else if (controller == 6)
        {
            last_movement = 6;
            dirX = 0;
        }
        else if (controller == 4)
        {
            dirX = 0;
            last_movement = 4;
        }
        else if (controller == 5)
        {
            last_movement = 5;
            dirX = -1;
        }
        else if (controller == 8)
            rod_attack = true;
        else if (controller == 9)
            spear_attack = true;
        else if (controller == 15)
        {
            rod_attack = false;
            spear_attack = false;

        }
        else if (controller == 2)
        {
            jump_controller = true;
        }
        else if (controller == 3)
        {
            jump_controller = false;
        }
        float dirX2 = Input.GetAxisRaw("Horizontal");

        if (dirX2 != 0)
        {
            dirX = dirX2;
            from_keyboard = true;
            last_movement = 6;
            inData = last_movement.ToString() + "\n";
        }
        else
        { from_keyboard = false; }
        // Debug.log(dirX);
        rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y);

        if ((Input.GetKeyDown(KeyCode.Mouse0) && canAttack) || (rod_attack && canAttack))
        {
            attack();

            canAttack = false;
            rod_attack = false;
            StartCoroutine(attackCoolDown());
            if (!from_keyboard)
                inData = last_movement.ToString() + "\n";

        }

        if (((Input.GetKeyDown(KeyCode.Space) || jump_controller) && isGrounded()/*anotherthing*/) /*&& isGrounded()*/)
        {

            rb.velocity = new Vector2(rb.velocity.x, 12f);

            Debug.Log("HEEEYYYY");
            if (!from_keyboard)
                inData = last_movement.ToString() + "\n";
            Debug.Log(inData);
            jump_controller = false;


            //Debug.Log(KeyCode.Space);
        }


        if (transform.position.y < -14.5)
        {
            Debug.Log(transform.position.y);
            //isDead(true);
            gameOver();
            //gameObject.SetActive(false);
            our_controller.Close();
            _t2.Abort();
            gameManager.gameOver();
            Debug.Log("Dead");

        }
        float delay = 15000f;
        float timer = 0f;



        if (Input.GetMouseButton(1) || spear_attack)
        {
            if (Time.time > ReadyForNextShot)
            {
                animator.SetTrigger("spear");
                if (!from_keyboard)
                    inData = last_movement.ToString() + "\n";
                ReadyForNextShot = Time.time + 1 / fireRate;
                /*while(timer < delay)
                {
                    timer += Time.deltaTime;
                    //yield return null;
                }*/
                //shoot();
                StartCoroutine(ShootAfterDelay(.25f));
            }


        }

        UpdateAnimationState();

    }
    void attack()
    {
        animator.SetTrigger("Attack");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        //Collider2D[] hitEnemies  = Physics2D.OverlapBoxAll(attackPoint.position, new Vector2(X,Y), enemyLayers);


        foreach (Collider2D enemy in hitEnemies)
        {

            enemy.GetComponent<Bishop_Crab>().TakeDamage(attackDamage);
        }


    }

    IEnumerator attackCoolDown()
    {
        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        //Gizmos.DrawWireCube(attackPoint.position, new Vector3(X, Y, 1));
    }
    private void UpdateAnimationState()
    {
        MovementState state;
        if (dirX > 0f)
        {

            state = MovementState.running;
            if (!running_sound.isPlaying)
            {
                running_sound.Play();
            }
            //characterTransform.localScale = new Vector3(1f, 1f, 1f);
            sprite.flipX = false;


        }
        else if (dirX < 0f)
        {
            ;
            state = MovementState.running;

            if (!running_sound.isPlaying)
            {
                running_sound.Play();
            }
            //characterTransform.localScale = new Vector3(-1f, 1f, 1f);
            sprite.flipX = true;


        }
        else
        {
            running_sound.Stop();
            state = MovementState.idle;
            //characterTransform.localScale = new Vector3(1f, 1f, 1f);

        }

        if (rb.velocity.y > .1f)
        {
            state = MovementState.jumping;

        }
        else if (rb.velocity.y < -.1f)
        {
            state = MovementState.falling;
        }

        anim.SetInteger("state", (int)state);







    }

    private bool isGrounded()
    {
        return Physics2D.BoxCast(CC.bounds.center, CC.bounds.size, 0f, Vector2.down, .1f, jumpableArea);


    }

    public void gameOver()
    {
        //GameOverScreen.Setup();
        //_t2 = new Thread(_func2);
        //_t2.Start();

        //_t2.Abort();
        //our_controller.Close();
        gameManager.gameOver();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        //gameManager.gameOver();

    }
    void FaceMouse()
    {
        Spear.transform.right = direction;

    }
    void shoot()
    {
        
        GameObject spearIns = Instantiate(Projectile, ShootPoint.position, ShootPoint.rotation);
        spearIns.GetComponent<Rigidbody2D>().AddForce(spearIns.transform.right * ProjectileSpeed);
        

        Destroy(spearIns, (float)0.3);
    }

    IEnumerator ShootAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        GameObject spearIns = Instantiate(Projectile, ShootPoint.position, ShootPoint.rotation);
        spearIns.GetComponent<Rigidbody2D>().AddForce(spearIns.transform.right * ProjectileSpeed);
        

        Destroy(spearIns, 0.3f);
    }
}
