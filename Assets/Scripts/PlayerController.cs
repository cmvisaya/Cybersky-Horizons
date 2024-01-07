using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerController : MonoBehaviour
{


    public float moveSpeed, sprintSpeed, speed, jumpForce, gravityScale, storedGravityScale;
    public float accel = 0.5f;
    public bool grounded = true;

    public CharacterController controller;
    private Vector3 moveDirection;

    public Animator anim;

    public Transform pivot;
    public float rotateSpeed;

    public GameObject playerModel;

    public bool hasControl, jumpEnabled, sprintEnabled = true;

    public bool aiming = false;

    //Wallrunning declarations
    public LayerMask whatIsWall;
    public float wallrunForce, wallStickiness, maxWallSpeed;
    bool isWallRight, isWallLeft;
    bool isWallRunning;
    public float maxWallRunCameraTilt, wallRunCameraTilt;
    public Transform orientation;

    private void WallRunInput() {
        if (Input.GetAxis("Horizontal") > 0 && isWallRight && !controller.isGrounded) StartWallRun();
        if (Input.GetAxis("Horizontal") < 0 && isWallLeft && !controller.isGrounded) StartWallRun();
    }
    private void StartWallRun() {
        gravityScale = 0f;
        isWallRunning = true;
        grounded = true;
        if (speed <= maxWallSpeed) {
            moveDirection += orientation.forward * wallrunForce;
            if (isWallRight) moveDirection += orientation.right * wallrunForce / 5;
            else moveDirection -= orientation.right * wallrunForce / 5;
            moveDirection.y = 0;
            controller.Move(moveDirection * Time.deltaTime);
        }
    }
    private void StopWallRun() {
        gravityScale = storedGravityScale;
        isWallRunning = false;
    }
    private void CheckForWall() {
        isWallRight = Physics.Raycast(transform.position, orientation.right, 0.5f, whatIsWall);
        isWallLeft = Physics.Raycast(transform.position, -orientation.right, 0.5f, whatIsWall);

        if ((Input.GetAxis("Horizontal") >= 0 && isWallLeft) || (Input.GetAxis("Horizontal") <= 0 && isWallRight)) StopWallRun();
    }


    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (GameObject.Find("Pivot"))
        {
            pivot = GameObject.Find("Pivot").transform;
        }
        Cursor.lockState = CursorLockMode.Locked;

        storedGravityScale = gravityScale;
    }


    // Update is called once per frame
    void Update()
    {
        if (hasControl)
        {
            CheckForWall();
            WallRunInput();
            //Stick Movement
            float yStore = moveDirection.y;
            float vertInput = Input.GetAxis("Vertical");
            float horizInput = Input.GetAxis("Horizontal");
            moveDirection = (pivot.forward * vertInput) + (pivot.right * horizInput);
            if(Mathf.Abs(vertInput) > 0 || Mathf.Abs(horizInput) > 0) {
                speed += accel * Time.deltaTime;
                if (Input.GetButton("Sprint") && sprintEnabled)
                {
                    if(speed > sprintSpeed) { speed = sprintSpeed; }
                }
                else
                {
                    if(speed > moveSpeed) { speed = moveSpeed; }
                }
            } else {
                speed -= accel * Time.deltaTime;
                if(speed < 0) { speed = 0; }
            }
            moveDirection = moveDirection.normalized * speed;
            moveDirection.y = yStore;

            //Jump/Gravity Control
            if (controller.isGrounded)
            {
                moveDirection.y = -1f;
                if (Input.GetButton("Jump") && jumpEnabled)
                {
                    moveDirection.y = jumpForce;
                }
            }

            if (isWallRunning && Input.GetButton("Jump")) {
                if (isWallLeft && !(horizInput > 0) || isWallRight && !(horizInput < 0)) {
                    moveDirection.y = jumpForce;
                }

                if (isWallRight || isWallLeft && horizInput != 0) moveDirection.y = -jumpForce;
                if (isWallRight && horizInput < 0) moveDirection -= orientation.right * jumpForce * 3.2f;
                if (isWallLeft && horizInput > 0) moveDirection += orientation.right * jumpForce * 3.2f;

                moveDirection += orientation.forward * jumpForce;
            }

            moveDirection.y = moveDirection.y + (Physics.gravity.y * gravityScale * Time.deltaTime);

            //Extended Grounded Check
            RaycastHit hit = new RaycastHit();
            var distanceToGround = 0f; ;
            if (Physics.Raycast(transform.position, -Vector3.up, out hit))
            {
                distanceToGround = hit.distance;
            }

            //Move Direction Application
            if(!isWallRunning) {
                controller.Move(moveDirection * Time.deltaTime);
                grounded = controller.isGrounded || distanceToGround < 1.5f;
            }

            //Rotation
            if(aiming) {
                transform.rotation = Quaternion.Euler(0f, pivot.localEulerAngles.y, 0f);
                pivot.transform.rotation = transform.rotation;
                playerModel.transform.rotation = Quaternion.Euler(0f, pivot.localEulerAngles.y, 0f);
            }
            else if ((Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0))
            {
                Quaternion newRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0f, moveDirection.z));
                playerModel.transform.rotation = Quaternion.Slerp(playerModel.transform.rotation, newRotation, rotateSpeed * Time.deltaTime);
            }

        }
        else { speed = 0; }

        anim.SetBool("aiming", aiming);
        anim.SetBool("isGrounded", grounded);
        anim.SetFloat("Speed", speed); 
    }
}
