using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Cinemachine;
using DG.Tweening;

//Sideview car game where you can charge in two directions, when you're in air you can charge only once more, when your car is grounded it will reset these jumps, 
//when rolled over the car will roll itself back on its wheels
public class VehicleMovement : MonoBehaviour
{
    public @InputSystem _controls;

    Rigidbody rb;

    private float boostForce;
    int maximumBoostForce = 1;
    [SerializeField] Slider boostForceIndicator;
    float inputDirection;
    private Vector3 currentAngle;
    CinemachineVirtualCamera camBehaviour;

    [SerializeField] List<ParticleSystem> chargePS = new List<ParticleSystem>();
    [SerializeField] List<ParticleSystem> boostPS = new List<ParticleSystem>();
    ParticleSystem directionalPS;

    [SerializeField] Transform carVisual;
    [SerializeField] List<GameObject> carWheels = new List<GameObject>();

    [SerializeField] float cameraZoomAmount;
    [SerializeField] float cameraTargetFov;

    Material outlineMat;

    private int timesJumped = 0;
    bool isGrounded;
    bool isCharging = false;

    void OnValidate()
    {
        carWheels = GameObject.FindGameObjectsWithTag("Wheels").ToList();
        rb = this.gameObject.GetComponent<Rigidbody>();
        camBehaviour = GameObject.FindObjectOfType<CinemachineVirtualCamera>();
        directionalPS = GameObject.Find("DirectionalPS").GetComponent<ParticleSystem>();
        chargePS = GameObject.Find("BoostChargeParent").GetComponentsInChildren<ParticleSystem>().ToList();
        boostPS = GameObject.Find("BoostLaunchParent").GetComponentsInChildren<ParticleSystem>().ToList();
        outlineMat = this.gameObject.GetComponentInChildren<MeshRenderer>().sharedMaterial;
    }

    private void Start()
    {
        currentAngle = this.transform.position;
        MultiParticleFX(chargePS, false);
        directionalPS.Stop();
        SetTimesJumped(timesJumped = 0);
        GroundedBehaviour();
    }

    private void OnEnable()
    {
        _controls = new InputSystem();

        _controls.Game.Movement.performed += BoostInput;
        _controls.Game.Movement.canceled += BoostInput;
        _controls.Game.Movement.Enable();

        _controls.Game.Zoom.performed += ZoomBehaviour;
        _controls.Game.Zoom.canceled += ZoomBehaviour;
        _controls.Game.Zoom.Enable();
    }
    private void OnDisable()
    {
        _controls.Game.Movement.performed -= BoostInput;
        _controls.Game.Movement.canceled -= BoostInput;
        _controls.Game.Movement.Disable();

        _controls.Game.Zoom.performed -= ZoomBehaviour;
        _controls.Game.Zoom.canceled -= ZoomBehaviour;
        _controls.Game.Zoom.Disable();
    }

    void Update()
    {
        BoostUpdate();
        RollWheels();
        RolloverBehaviour();
        GroundedBehaviour();
    }

    private void BoostInput(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        if (timesJumped >= 2)
            return;

        if (value != 0 && inputDirection == 0)
        {
            isCharging = true;
            MultiParticleFX(chargePS, true);
            directionalPS.Play();
            Quaternion rotationTarget = Quaternion.Euler(0, Mathf.Round(value) * 90, 0);
            directionalPS.transform.localRotation = rotationTarget;

            inputDirection = Mathf.Round(value);
        }
        if (value != 0 && inputDirection == Mathf.Round(value))
        {
            RotateCarDirection();
        }
        if (value == 0)
        {
            isCharging = false;
            Time.timeScale = 1;
            DOTween.To(() => camBehaviour.m_Lens.FieldOfView, x => camBehaviour.m_Lens.FieldOfView = x, cameraTargetFov, 0.2f);

            MultiParticleFX(chargePS, false);
            MultiParticleFX(boostPS, true);

            directionalPS.Stop();
            directionalPS.Clear();

            if (timesJumped < 2)
            {
                rb.AddForce((transform.right * inputDirection * 20) * boostForce, ForceMode.Impulse);
            }
            timesJumped++;
            SetTimesJumped(timesJumped);
            inputDirection = 0;
            boostForce = 0;
            boostForceIndicator.value = boostForce;
        }
    }

    public void BoostUpdate()
    {
        if (isCharging)
        {
            camBehaviour.m_Lens.FieldOfView = cameraTargetFov - Mathf.SmoothStep(0, cameraZoomAmount, boostForce);
            Time.timeScale = 1 - boostForce / 2;
            if (boostForce < maximumBoostForce)
            {
                boostForce += 1.5f * Time.deltaTime;
                rb.velocity = rb.velocity - (rb.velocity * boostForce);
            }
            boostForceIndicator.value = boostForce;
        }
    }

    public void RollWheels()
    {
        float dot = Vector3.Dot(this.transform.forward, carVisual.transform.forward);
        foreach (GameObject go in carWheels)
        {
            go.transform.Rotate(new Vector3(0, 0, (dot * rb.velocity.x * -2)));
        }
    }

    private void RolloverBehaviour()
    {
        if (rb.velocity.magnitude < 1 && boostForce < 0.1f)
        {
            RaycastHit hit;
            // Does the ray intersect any objects excluding the player layer
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.up), out hit, 1))
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.up));
                rb.AddForce(-transform.up);
                rb.AddTorque(0, 0f, -360, ForceMode.Impulse);
            }
        }
    }

    private void ZoomBehaviour(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            DOTween.To(() => camBehaviour.m_Lens.FieldOfView, x => camBehaviour.m_Lens.FieldOfView = x, 100, 1);
        } else
        {
            DOTween.To(() => camBehaviour.m_Lens.FieldOfView, x => camBehaviour.m_Lens.FieldOfView = x, cameraTargetFov, 1);
        }
    }

    private void GroundedBehaviour()
    {
        //todo: convert to raycast, tagcheck

        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit, 0.3f))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * 0.3f);
            SetTimesJumped(timesJumped = 0);
            isGrounded = true;
        }
        else
        {
            if (isGrounded == true)
            {
                SetTimesJumped(timesJumped = 1);
                isGrounded = false;
            }
        }
    }

    private void RotateCarDirection()
    {
        Vector3 currentRot = carVisual.localEulerAngles;
        float rotationVal = carVisual.localEulerAngles.y;
        switch (inputDirection)
        {
            case -1:
                DOVirtual.Float(rotationVal, 180, 0.2f, angle => {
                    carVisual.localEulerAngles = new Vector3(currentRot.x, angle, currentRot.z);
                });
                break;
            case 1:
                DOVirtual.Float(rotationVal, 0, 0.2f, angle => {
                    carVisual.localEulerAngles = new Vector3(currentRot.x, angle, currentRot.z);
                });
                break;
            default:
                break;
        }
    }

    private void SetTimesJumped(int timesJumped)
    {
        switch (timesJumped)
        {
            case 0:
                outlineMat.SetColor("_OutlineColor", Color.yellow);
                break;
            case 1:
                outlineMat.SetColor("_OutlineColor", new Color(1, 0.80f, 0.016f));
                break;
            case 2:
                outlineMat.SetColor("_OutlineColor", Color.grey);
                break;
            default:
                break;
        }
    }

    public void MultiParticleFX(List<ParticleSystem> psList, bool shouldPlay)
    {
        foreach (ParticleSystem ps in psList)
        {
            if (shouldPlay)
                ps.Play();
            else
                ps.Stop();
        }
    }
}
