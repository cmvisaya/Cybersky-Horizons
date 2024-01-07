using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;


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
    [SerializeField] bool isWallRunning;
    public float maxWallRunCameraTilt, wallRunCameraTilt;
    public Transform orientation;

    public float wallRunSpeedIncrease, wallRunSpeedDecrease, wallRunGravity;
    [SerializeField] bool onLeftWall, onRightWall;
    RaycastHit leftWallHit, rightWallHit;
    Vector3 wallNormal;
    public CinemachineCollider walkingCollider;
    bool wallRunOnCD = false;

    private void StartWallRun() {
        gravityScale = 0f;
        wallNormal = onLeftWall ? leftWallHit.normal : rightWallHit.normal;
        moveDirection = Vector3.Cross(wallNormal, Vector3.up);
        grounded = true;
        isWallRunning = true;
        Debug.Log("Start");
        if (Vector3.Dot(moveDirection, playerModel.transform.forward) < 0) moveDirection = -moveDirection;
        walkingCollider.enabled = false;
    }
    private void StopWallRun() {
        gravityScale = storedGravityScale;
        isWallRunning = false;
        StartCoroutine(DelayCameraClipthrough());
        Debug.Log("Stop");
    }
    private void CheckForWall() {
        onLeftWall = Physics.Raycast(playerModel.transform.position, -playerModel.transform.right, out leftWallHit, 0.55f, whatIsWall);
        onRightWall = Physics.Raycast(playerModel.transform.position, playerModel.transform.right, out rightWallHit, 0.55f, whatIsWall);
        if (((onRightWall || onLeftWall) && speed >= sprintSpeed * 0.9f) && !isWallRunning && !wallRunOnCD) StartWallRun();
        if (((!onRightWall && !onLeftWall) || speed < sprintSpeed * 0.9f) && isWallRunning) StopWallRun();
    }

    private IEnumerator DelayCameraClipthrough() {
        wallRunOnCD = true;
        yield return new WaitForSeconds(0.3f);
        walkingCollider.enabled = true;
        wallRunOnCD = false;
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

            if (isWallRunning && Input.GetButtonDown("Jump")) {
                if (onLeftWall || onRightWall) moveDirection = transform.up * jumpForce;
                if (onLeftWall) moveDirection += -playerModel.transform.right * jumpForce * 3.2f;
                if (onRightWall) moveDirection += playerModel.transform.right * jumpForce * 3.2f;
                moveDirection += orientation.forward * jumpForce;
                StopWallRun();
            }

            Debug.DrawRay(transform.position, moveDirection, Color.blue);

            //Extended Grounded Check
            RaycastHit hit = new RaycastHit();
            var distanceToGround = 0f; ;
            if (Physics.Raycast(transform.position, -Vector3.up, out hit))
            {
                distanceToGround = hit.distance;
            }

            //Move Direction Application
            moveDirection.y = moveDirection.y + (Physics.gravity.y * gravityScale * Time.deltaTime);
            if(isWallRunning) moveDirection.y = 0f;
            controller.Move(moveDirection * Time.deltaTime);
            if(!isWallRunning) {
                grounded = controller.isGrounded || distanceToGround < 1.5f;
            } else { grounded = true; }

            //Rotation
            if(aiming || isWallRunning) {
                transform.rotation = Quaternion.Euler(0f, pivot.localEulerAngles.y, 0f);
                pivot.transform.rotation = transform.rotation;
                playerModel.transform.rotation = Quaternion.Euler(0f, pivot.localEulerAngles.y, 0f);
            }
            else if ((Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0))
            {
                Quaternion newRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0f, moveDirection.z));
                if(!isWallRunning) playerModel.transform.rotation = Quaternion.Slerp(playerModel.transform.rotation, newRotation, rotateSpeed * Time.deltaTime);
            }

        }
        else { speed = 0; }

        anim.SetBool("aiming", aiming);
        anim.SetBool("isGrounded", grounded);
        anim.SetFloat("Speed", speed); 
    }
}
