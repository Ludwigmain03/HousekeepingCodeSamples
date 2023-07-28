using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]

public class Movement : MonoBehaviour
{
    [Header ("Movement Properties")]
    public float speed;
    float speedTarget;
    public float speedReal;
    public float acceleration;
    public float turnSpeed;
    Vector3 directionInput;
    float stickMagnitude;
    public Vector3 additionalInfluence;

    PlayerInput controls;
    Rigidbody rb;
    Animator anim;
    public Transform animationRoot;

    Transform cameraTransform;

    float horizontalInput;
    float verticalInput;

    public ParticleSystem ps; //run effect

    void Start()
    {
        controls = GetComponent<PlayerInput>();

        rb = GetComponent<Rigidbody>();

        cameraTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;

        GetModelAnimProperties();
    }

    // Update is called once per frame
    void Update()
    {
        //Detects when the left stick is being held
        bool usingStick = ((Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput)) > 0.1f);

        directionInput = (cameraTransform.right * horizontalInput) + (cameraTransform.forward * verticalInput);
        if(directionInput.magnitude > 1)
            directionInput.Normalize();

        stickMagnitude = directionInput.magnitude;

        if (ps != null)
        {
            if (!ps.isPlaying)
                ps.Play();
        }
        speedTarget = stickMagnitude * speed;

        Vector3 roteDirection = transform.position + directionInput;

        if(usingStick)
        {
            //Turning
            Quaternion targetRotation = Quaternion.LookRotation(directionInput, Vector3.up);
            Quaternion newRotation = Quaternion.Lerp(rb.rotation, targetRotation, turnSpeed * Time.deltaTime);
            rb.MoveRotation(newRotation);

            //Acceleration
            if (speedReal < speedTarget)
                speedReal += acceleration * Time.deltaTime;
            else if (speedReal > speedTarget + 0.3f)
                speedReal = speedReal -= acceleration * Time.deltaTime;
            else
                speedReal = speedTarget;
        }
        else
        {
            if (speedReal > 0)
                speedReal -= acceleration * Time.deltaTime;
            else
                speedReal = 0;

            if (ps != null)
            {
                if (ps.isPlaying)
                    ps.Stop();
            }
        }

        rb.velocity = transform.forward * speedReal + new Vector3(0, rb.velocity.y, 0) + additionalInfluence;
        anim.SetFloat("Speed", speedReal);
    }

    void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;
        if (anim.GetFloat("Speed") < 0.1f)
        {
            animationRoot.position = transform.position;
            animationRoot.rotation = transform.rotation;
        }
    }

    public void GetModelAnimProperties()
    {
        //Get animation and model from player (Child objects that the player instantiates
        anim = GetComponent<Player>().anim;
        animationRoot = anim.transform;
    }

    private void OnMove(InputValue movementValue)
    {
        //Gets left stick input
        Vector2 movementVector = movementValue.Get<Vector2>();

        horizontalInput = movementVector.x;
        verticalInput = movementVector.y;
    }
}
