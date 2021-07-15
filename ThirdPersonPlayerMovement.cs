using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//still in-progress 3rd person movement controller for a high mobility action game
public class ThirdPersonPlayerMovement : MonoBehaviour
{
    ActionMap input;

    public Transform virtualCam;

    enum MovementState
    {
        Grounded,
        Aerial,
        Wallrunning
    }
    [SerializeField] MovementState state = MovementState.Grounded;

    Vector2 movementDirection;
    Vector3 moveDirHorizontal;
    Vector2 aimDirectionChange;

    public float movementSpeed;
    public float aerialSpeed;
    public float wallrunSpeed;
    public Vector2 mouseSensitivity;

    public float throwingForce;


    [SerializeField] private CharacterController controller;
    [SerializeField] Animator animator;

    public Transform hipsPos, footPos, neckPos, handPos;
    public LayerMask groundMask;

    Vector3 velocity;
    private int timesJumped;
    private int maxTimesJumped = 2;
    [SerializeField] private bool jumpedFromGround;

    public GameObject bomb;
    Vector3 wallDir;

    Vector3[] directions = new Vector3[]
    {
        Vector3.right,
        Vector3.left
    };

    private void OnValidate()
    {
        controller = this.gameObject.GetComponent<CharacterController>();
        animator = this.gameObject.GetComponentInChildren<Animator>();
    }

    #region Movement and Camera

    private void FixedUpdate()
    {
        MovePlayer();
        GroundCheck();
        WallRunCheck();
        WallJumpCheck();
    }

    void LateUpdate()
    {
        AimCamera();
    }

    void MovePlayer()
    {
        if (state == MovementState.Grounded)
        {
            moveDirHorizontal = (transform.right * movementDirection.x + transform.forward * movementDirection.y) * movementSpeed;
            controller.Move(moveDirHorizontal * Time.deltaTime);
            velocity = Vector3.down;
        }
        else if (state == MovementState.Aerial)
        {
            moveDirHorizontal = (transform.right * movementDirection.x + transform.forward * movementDirection.y) * aerialSpeed;
            controller.Move(moveDirHorizontal * Time.deltaTime);
            velocity += Physics.gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);

        Debug.DrawRay(hipsPos.position, moveDirHorizontal * 10, Color.magenta);

        SetAnimator();
    }

    void WallRunCheck()
    {
        if (state == MovementState.Grounded)
            return;

        Vector3 wallNormal = Vector3.zero;
        bool huggingWall = false;

        foreach (Vector3 dir in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(hipsPos.position, transform.TransformDirection(dir), out hit, 1, groundMask))
            {
                Debug.DrawRay(hipsPos.position, transform.TransformDirection(dir) * hit.distance, Color.green);
                wallDir = dir;
                wallNormal = hit.normal;
                timesJumped = 1;
                huggingWall = true;
            }
            else
            {
                Debug.DrawRay(hipsPos.position, transform.TransformDirection(dir) * 1, Color.red);
            }
        }

        if (huggingWall && Vector3.Dot(wallNormal, moveDirHorizontal) < -0.1f)
        {
            //pushes player against wall
            velocity.y = -1;
            controller.Move(-wallNormal + transform.forward * wallrunSpeed * Time.deltaTime);
            state = MovementState.Wallrunning;
        }
        animator.SetFloat("WallDir", wallDir.x);
        animator.SetBool("WallRunning", state == MovementState.Wallrunning);

        GroundCheck();
    }

    void WallJumpCheck()
    {
        if (state == MovementState.Grounded)
            return;

        Vector3 wallDir = Vector3.zero;
        Vector3 wallNormal = Vector3.zero;

        RaycastHit hit;
        if (Physics.Raycast(hipsPos.position, moveDirHorizontal, out hit, 1, groundMask))
            {
                Debug.DrawRay(hipsPos.position, moveDirHorizontal * hit.distance, Color.green);
            }
    }

    public void GroundCheck()
    {
        bool grounded = Physics.CheckSphere(footPos.position, 0.5f, groundMask);

        if (!grounded)
        {
            if (timesJumped == 0)
                timesJumped = 1;
            //inair
            jumpedFromGround = false;
            state = MovementState.Aerial;
        }
        else if (jumpedFromGround)
        {
            //jumped, not out of ground range 
            state = MovementState.Aerial;
        }
        else
        {
            //grounded
            timesJumped = 0;
            jumpedFromGround = false;
            state = MovementState.Grounded;
        }


    }

    void AimCamera()
    {
        float mouseDelta = aimDirectionChange.y * mouseSensitivity.y;

        transform.Rotate(Vector3.up, aimDirectionChange.x * mouseSensitivity.x);
        virtualCam.RotateAround(neckPos.position, this.transform.right, -mouseDelta);
    }

    #endregion

    #region Animations and AnimationEvents

    //Gets called in Thrown Animation
    public void ThrowBomb()
    {
        //optimize
        var b = Instantiate(bomb, handPos.position, Quaternion.identity);
        Rigidbody rbb = b.GetComponent<Rigidbody>();
        rbb.AddForce((virtualCam.forward * throwingForce) + (transform.up * throwingForce/2), ForceMode.Impulse);
    }

    private void SetAnimator()
    {
        animator.SetFloat("InputX", movementDirection.x, 1.2f, Time.deltaTime * movementSpeed);
        animator.SetFloat("InputY", movementDirection.y, 1.2f, Time.deltaTime * movementSpeed);

        animator.SetFloat("VelocityY", velocity.y, 1.2f, Time.deltaTime * 10);

        animator.SetBool("Grounded", state == MovementState.Grounded);
        
    }
    #endregion

    #region Inputhandling

    private void OnMovement(InputAction.CallbackContext context)
    {
        movementDirection = context.ReadValue<Vector2>().normalized;
    }

    private void OnAim(InputAction.CallbackContext context)
    {
        aimDirectionChange = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        GroundCheck();
        if (state == MovementState.Wallrunning)
        {
            state = MovementState.Aerial;
            
            //trigger walljump anim
            animator.SetTrigger("Jump");
            //todo push away from wall
            velocity.y = Mathf.Sqrt(3 * -2f * Physics.gravity.y);

            jumpedFromGround = true;
            timesJumped += 1;

            return;
        }
        if (timesJumped < maxTimesJumped)
        {
            state = MovementState.Aerial;
            animator.SetTrigger("Jump");
            velocity.y = Mathf.Sqrt(3 * -2f * Physics.gravity.y);
            jumpedFromGround = true;
            timesJumped += 1;
        }
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        Debug.Log("Fired");
        animator.SetTrigger("ElbowDrop");
    }
    #endregion

    #region Input Subscriptions

    private void OnEnable()
    {
        GroundCheck();
        input = new ActionMap();

        input.Game.Movement.performed += OnMovement;
        input.Game.Movement.canceled += OnMovement;

        input.Game.Aim.performed += OnAim;
        input.Game.Aim.canceled += OnAim;

        input.Game.Fire.performed += OnFire;
        input.Game.Jump.performed += OnJump;

        input.Game.Enable();
    }

    private void OnDisable()
    {
        input.Game.Movement.performed -= OnMovement;
        input.Game.Movement.canceled -= OnMovement;

        input.Game.Aim.performed -= OnAim;
        input.Game.Aim.canceled -= OnAim;

        input.Game.Fire.performed -= OnFire;
        input.Game.Jump.performed -= OnJump;

        input.Game.Disable();
    }
    #endregion

    private void OnDrawGizmos()
    {
        //grounded sphere
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(footPos.position, 0.5f);
    }
}
