using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RpgPlayerController : MonoBehaviour
{
    public static PlayerMovement SP;
    [SerializeField] private GameControls _controls;

    [SerializeField] private SpriteRenderer PlayerVisual;

    [SerializeField] private Sprite PlayerBaseFront;
    [SerializeField] private Sprite PlayerBaseBack;

    [SerializeField] private Animator animator;

    private Vector2 movement;
    public Rigidbody2D rb;
    public Vector2 velocity;
    private float baseAttackSpeed = 1.5f;
    private bool isAttacking = false;
    private float attackTimer;

    [SerializeField] OffHandSO offHand;
    [SerializeField] MainHandSO mainHand;

    [SerializeField] SpriteRenderer[] gearSprites = new SpriteRenderer[4];
    enum Gear
    {
        Armor,
        MainHand,
        OffHand,
        Helmet
    }
    [SerializeField] Dictionary<Gear, SpriteRenderer> slot = new Dictionary<Gear, SpriteRenderer>();

    //sorting order
    int behindPlayer = -10;
    int frontPlayer = 10;

    private void Awake()
    {
        _controls = new GameControls();
        SP = this;
        rb = gameObject.GetComponent<Rigidbody2D>();

        for (int i = 0; i < gearSprites.Length; i++)
        {
            slot.Add((Gear) i, gearSprites[i]);
        }
    }

    private void OnEnable()
    {
        _controls.Input.Movement.performed += OnCharacterMove;
        _controls.Input.Movement.canceled += OnCharacterMove;
        _controls.Input.Movement.Enable();

        _controls.Input.Attack.started += OnPlayerAttack;
        _controls.Input.Attack.performed += OnPlayerAttack;
        _controls.Input.Attack.canceled += OnPlayerAttack;
        _controls.Input.Attack.Enable();


    }
    private void OnDisable()
    {
        _controls.Input.Movement.performed -= OnCharacterMove;
        _controls.Input.Movement.canceled -= OnCharacterMove;
        _controls.Input.Movement.Disable();

        _controls.Input.Attack.started -= OnPlayerAttack;
        _controls.Input.Attack.performed -= OnPlayerAttack;
        _controls.Input.Attack.canceled -= OnPlayerAttack;
        _controls.Input.Attack.Enable();
    }

    #region Visual

    private void CharacterFaceDirection(Vector2 mouseDir)
    {
        
        
        //front view directions
        if (mouseDir.y <= 0)
        {
            this.transform.localScale = Vector3.one;
            PlayerVisual.sprite = PlayerBaseFront;

            if (mouseDir.x < 0)
            {

                PlayerVisual.flipX = true;

                MainhandInFront(true, true);
                OffhandInFront(false, false);
            }
            else if (mouseDir.x >= 0)
            {
                PlayerVisual.flipX = false;

                MainhandInFront(false, false);
                OffhandInFront(true, false);
            }
        }
        //back view directions
        else if (mouseDir.y > 0)
        {
            this.transform.localScale = new Vector3(-1, 1, 1);
            PlayerVisual.sprite = PlayerBaseBack;

            if (mouseDir.x < 0)
            {
                PlayerVisual.flipX = false;

                MainhandInFront(true, false);
                OffhandInFront(false, false);
            }
            else if (mouseDir.x >= 0)
            {
                PlayerVisual.flipX = true;

                MainhandInFront(false, true);
                OffhandInFront(true, false);
            }
        }
        
    }

    private void AnimateWalk(bool shouldAnimate)
    {
            animator.SetBool("Moving", shouldAnimate);
    }

    #region SpriteSorting
    private void OffhandInFront(bool isInFront, bool shouldFlip)
    {
        slot[Gear.OffHand].flipX = shouldFlip;
        if (isInFront)
        {
            slot[Gear.OffHand].sortingOrder = frontPlayer;
            slot[Gear.OffHand].sprite = offHand.frontSprite;
        } else
        {
            slot[Gear.OffHand].sprite = offHand.backSprite;
            slot[Gear.OffHand].sortingOrder = behindPlayer;
        }
    }

    private void MainhandInFront(bool isInFront, bool shouldFlip)
    {
        slot[Gear.MainHand].flipX = shouldFlip;
        slot[Gear.MainHand].sortingOrder = isInFront ? frontPlayer : behindPlayer;
    }
    #endregion



    #endregion

    #region Input and Physics
    private void Update()
    {
        attackTimer += Time.deltaTime;
    }
    private void FixedUpdate()
    {
        rb.velocity = (movement * velocity * Time.deltaTime);
    }

    private void OnCharacterMove(InputAction.CallbackContext context)
    {
        movement = context.ReadValue<Vector2>();

        if (!isAttacking)
            CharacterFaceDirection(movement);

        bool isMoving = movement.magnitude > 0 ? true : false;
        AnimateWalk(isMoving);
        return;
    }

    private void OnPlayerAttack(InputAction.CallbackContext context)
    {
        
        if (context.started && !isAttacking && attackTimer > (baseAttackSpeed / mainHand.attackSpeed))
        {
            isAttacking = true;
            StartCoroutine(StartAttack());
        }
        else if (context.canceled)
        {
            isAttacking = false;
        }
        return;
    }

    IEnumerator StartAttack()
    {
        attackTimer = 0;
        StartCoroutine(AttackSprite());
        
        Instantiate(mainHand.projectile, (Vector2) this.transform.position + (Vector2.up * 1.5f), Quaternion.identity);

        yield return new WaitForSeconds(baseAttackSpeed / mainHand.attackSpeed);

        if (isAttacking)
        {
            StartCoroutine(StartAttack());
        }
    }

    IEnumerator AttackSprite()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 mouseDir = mousePos - (Vector2)this.transform.position;
        CharacterFaceDirection(mouseDir);

        slot[Gear.MainHand].sprite = mainHand.slashSprite;
        yield return new WaitForSeconds(0.1f);
        slot[Gear.MainHand].sprite = mainHand.idleSprite;
    }
    #endregion

}
