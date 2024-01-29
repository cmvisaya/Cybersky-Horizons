using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;
using Unity.Netcode;


public class PlayerController : NetworkBehaviour
{
    public float moveSpeed, sprintSpeed, slideSpeed, speed, bulletTimeSpeedMult, jumpForce, gravityScale, storedGravityScale;
    public float accel = 0.5f;
    [HideInInspector] public bool grounded = true;

    public CharacterController controller;
    [HideInInspector] public Vector3 moveDirection;

    public Animator anim;
    public Transform lookTarget;

    public Transform pivot;
    private float rotateSpeed;
    public float airRotateSpeed, groundRotateSpeed;

    public GameObject playerModel;

    [HideInInspector] public bool hasControl, jumpEnabled, sprintEnabled, aimEnabled = true;

    [HideInInspector] public bool aiming, shooting = false;

    //Wallrunning declarations
    public LayerMask whatIsWall;
    bool isWallRunning;
    public float maxWallRunCameraTilt, wallRunCameraTilt;
    public Transform orientation;

    [SerializeField] bool onLeftWall, onRightWall;
    RaycastHit leftWallHit, rightWallHit;
    Vector3 wallNormal;
    public CinemachineCollider walkingCollider;
    bool wallRunOnCD = false;
    public CameraController cc;
    [SerializeField] private float wallrunTilt;
    public WeaponController wc;

    [SerializeField] private Transform respawnLocation;

    //Debug vars
    private bool cursorLocked;

    public ulong debugid;

    public int characterId;

    private Vector3 slideForward;
    private bool isSliding, isGPound;
    [SerializeField] private float slideTimer, maxSlideTimer;
    [SerializeField] private float gpoundCancelTimer, maxGPoundCancelTimer;

    private void StartWallRun() {
        if(!isWallRunning) {
            cc.ActivateCamera(2);
        }
        if(onLeftWall) {
            playerModel.transform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, playerModel.transform.localEulerAngles.z - wallrunTilt);
        } else {
            playerModel.transform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, playerModel.transform.localEulerAngles.z + wallrunTilt);
        }
        gravityScale = 0f;
        wallNormal = onLeftWall ? leftWallHit.normal : rightWallHit.normal;
        moveDirection = Vector3.Cross(wallNormal, Vector3.up);
        grounded = true;
        isWallRunning = true;
        //Debug.Log("Start");
        aimEnabled = false;
        if (Vector3.Dot(moveDirection, pivot.transform.forward) < 0) moveDirection = -moveDirection;
        walkingCollider.enabled = false;

        wc.LoadBullets();
    }
    private void StopWallRun() {
        if(isWallRunning) cc.ActivateCamera(0);
        gravityScale = storedGravityScale;
        isWallRunning = false;
        aimEnabled = true;
        StartCoroutine(DelayCameraClipthrough());
        //Debug.Log("Stop");
        cc.ActivateCamera(0);
    }
    private void CheckForWall() {
        onLeftWall = Physics.Raycast(pivot.transform.position, -pivot.transform.right, out leftWallHit, 0.75f, whatIsWall);
        onRightWall = Physics.Raycast(pivot.transform.position, pivot.transform.right, out rightWallHit, 0.75f, whatIsWall);
        //Debug.Log(!isWallRunning + " | " + !wallRunOnCD + " | " + !grounded);
        if (((onRightWall || onLeftWall) && speed >= sprintSpeed * 0.5f) && !isWallRunning && !wallRunOnCD && !grounded) StartWallRun();
        if (((!onRightWall && !onLeftWall) || speed < sprintSpeed * 0.9f) && isWallRunning) StopWallRun();
    }

    private IEnumerator DelayCameraClipthrough() {
        wallRunOnCD = true;
        yield return new WaitForSeconds(0.3f);
        walkingCollider.enabled = true;
        wallRunOnCD = false;
    }

    private void CheckForSlide() {
        if (Input.GetKeyDown(KeyCode.C)) { 
            if(speed > moveSpeed && !isSliding && !isWallRunning && grounded) {
                InitiateSlide();
            }
        }
    }

    private void InitiateSlide() {
        cc.ActivateCamera(2);
        isSliding = true;
        slideForward = playerModel.transform.forward;
        slideTimer = maxSlideTimer;
    }

    private void Awake() {
        //if(characterId != GameManager.Instance.selectedCharacterCode) gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        //cc = GameObject.Find("Main Camera").GetComponent<CameraController>();

        // if (GameObject.Find("Pivot"))
        // {
        //     pivot = GameObject.Find("Pivot").transform;
        // }

        //orientation = GameObject.Find("Main Camera").transform;
        //walkingCollider = GameObject.Find("WalkingCamera").GetComponent<CinemachineCollider>();

        GameObject prePlayerCam = GameObject.Find("Pre-Player Cam");
        if(prePlayerCam != null) {
            prePlayerCam.SetActive(false);
        }

        GameObject tt = GameObject.Find("TTRunner");
        if (tt != null) { tt.GetComponent<TTRunner>().ResetTimer(); }
        
        controller = GetComponent<CharacterController>();
        cc.target = lookTarget;

        Cursor.lockState = CursorLockMode.Locked;
        cursorLocked = true;

        storedGravityScale = gravityScale;
        maxGPoundCancelTimer = 0.5f;
        cc.ActivateCamera(0);

        //respawnLocation = transform.position;

        debugid = OwnerClientId;

        int teamId = GetComponent<WeaponController>().teamId;
        if (teamId == 0) respawnLocation = GameObject.Find("Red Spawn").transform;
        else if (teamId == 1) respawnLocation = GameObject.Find("Blue Spawn").transform;
        else respawnLocation = GameObject.Find("NEUTRAL SPAWN KILL THIS MF").transform;

        //Respawn();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        if (GameManager.Instance.pauseMenu.activeSelf) Cursor.lockState = CursorLockMode.None;
        else Cursor.lockState = CursorLockMode.Locked;

        if (hasControl)
        {
            CheckForWall();
            CheckForSlide();
            //Stick Movement
            float yStore = moveDirection.y;
            float vertInput = Input.GetAxis("Vertical");
            float horizInput = Input.GetAxis("Horizontal");
            if (isGPound) {
                gpoundCancelTimer -= 1f * Time.deltaTime;
                if (gpoundCancelTimer < 0) {
                    isGPound = false;
                }
            }
            if(isSliding) {
                moveDirection = slideForward.normalized * slideSpeed;
                slideTimer -= 1f * Time.deltaTime;
                if (slideTimer < 0) {
                    cc.ActivateCamera(0);
                    isSliding = false;
                }
            } else {
                if(!wallRunOnCD) moveDirection = (pivot.forward * vertInput) + (pivot.right * horizInput);
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
                    if(grounded) speed -= accel * 0.1f * Time.deltaTime;
                    if(speed < 0) { speed = 0; }
                }
                float appliedSpeed = (aiming && !grounded) ? speed * bulletTimeSpeedMult : speed * 1f;
                moveDirection = moveDirection.normalized * appliedSpeed;
            }
            moveDirection.y = yStore;

            //Jump/Gravity Control
            if (controller.isGrounded)
            {
                moveDirection.y = -1f;
                if (Input.GetButton("Jump") && jumpEnabled)
                {
                    // /StopWallRun();
                    moveDirection.y = jumpForce;
                    isSliding = false;
                    isGPound = false;
                } else if (isGPound) {
                    InitiateSlide();
                    isGPound = false;
                }
            } else {
                if(Input.GetKeyDown(KeyCode.C)) {
                    moveDirection.y = -jumpForce;
                    isGPound = true;
                    gpoundCancelTimer = maxGPoundCancelTimer;
                }
            }

            if (isWallRunning && Input.GetButtonDown("Jump")) {
                moveDirection = (orientation.forward + wallNormal) * jumpForce * 10f;
                moveDirection.y = jumpForce * 0.8f;
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
                grounded = controller.isGrounded || (distanceToGround < 1.5f && distanceToGround > 0);
                //Debug.Log(controller.isGrounded + " | " + distanceToGround);
            } else { grounded = true; }

            //Rotation
            if(aiming || isWallRunning) {
                playerModel.transform.rotation = Quaternion.Euler(0f, pivot.localEulerAngles.y, playerModel.transform.localEulerAngles.z);
            }
            else if ((Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0))
            {
                rotateSpeed = grounded ? groundRotateSpeed : airRotateSpeed;
                Quaternion newRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0f, moveDirection.z));
                if(!isWallRunning) playerModel.transform.rotation = Quaternion.Slerp(playerModel.transform.rotation, newRotation, rotateSpeed * Time.deltaTime);
            }

        }
        else { speed = 0; }

        anim.SetBool("aiming", aiming);
        anim.SetBool("shooting", shooting);
        anim.SetBool("isGrounded", grounded);
        anim.SetBool("isSliding", isSliding);
        anim.SetFloat("Speed", speed);

        //MyServerRpc(transform.position);
    }

    public void Respawn() {
        Debug.Log("Respawning... " + OwnerClientId);
        if (OwnerClientId == 0) {
            RespawnServerRpc();
        } else {
            RespawnClientRpc(OwnerClientId);
        }
    }

    public void Respawn(int teamId) {
        Debug.Log("Respawning... teamId" + OwnerClientId);
        if (teamId == 0) respawnLocation = GameObject.Find("Red Spawn").transform;
        else if (teamId == 1) respawnLocation = GameObject.Find("Blue Spawn").transform;
        else respawnLocation = GameObject.Find("NEUTRAL SPAWN KILL THIS MF").transform;
        if (OwnerClientId == 0) {
            RespawnServerRpc();
        } else {
            RespawnClientRpc(OwnerClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RespawnServerRpc() {
        if(gameObject.GetComponent<Collider>().tag != "Player" || respawnLocation == null) return;
        controller.enabled = false;
        transform.position = respawnLocation.position;
        playerModel.transform.rotation = Quaternion.Euler(0f, respawnLocation.localEulerAngles.y, 0f);
        pivot.rotation = Quaternion.Euler(0f, respawnLocation.localEulerAngles.y, 0f);
        controller.enabled = true;
    }

    [ClientRpc]
    public void RespawnClientRpc(ulong ownerId) {
        if(OwnerClientId != ownerId || respawnLocation == null) return;
        controller.enabled = false;
        transform.position = respawnLocation.position;
        playerModel.transform.rotation = Quaternion.Euler(0f, respawnLocation.localEulerAngles.y, 0f);
        pivot.rotation = Quaternion.Euler(0f, respawnLocation.localEulerAngles.y, 0f);
        controller.enabled = true;
    }

    // [ServerRpc]
    // private void MyServerRpc(Vector3 positionVector)
    // {
    //     // Make sure to only execute on the server
    //     if (!IsServer) return;

    //     // Use the properties directly, no need for parameters
    //     Debug.Log("TestServerRPC " + OwnerClientId + " | " + transform.position.x + " | " + transform.position.y + " | " + transform.position.z);
        
    //     // Use the transform.position directly in the server RPC
    //     transform.position = positionVector;
    // }
}
