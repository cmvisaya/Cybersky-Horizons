using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;
using Unity.Netcode;

/*
 * Script that is attached to all player gameobjects
 * Handles 3rd person movement, including ground slide, wallrunning, ground pound, etc
 */

public class PlayerController : NetworkBehaviour
{
    // Movement variables
    public float moveSpeed; // Regular movement speed
    public float sprintSpeed; // Sprinting speed
    public float slideSpeed; // Speed while sliding
    public float speed; // Current speed
    public float bulletTimeSpeedMult; // Speed multiplier during bullet time
    public float jumpForce; // Force applied when jumping
    public float gravityScale; // Scale factor for gravity
    public float storedGravityScale; // Stored gravity scale for resetting

    // Acceleration and grounded status
    public float accel = 0.5f; // Acceleration rate
    [HideInInspector] public bool grounded = true; // Is the player grounded?

    // Components and references
    public CharacterController controller; // Character controller component
    [HideInInspector] public Vector3 moveDirection; // Current movement direction

    public Animator anim; // Animator for character animations
    public Transform lookTarget; // Target for the camera to look at

    public Transform pivot; // Pivot point for rotations
    private float rotateSpeed; // Speed of rotation
    public float airRotateSpeed; // Rotation speed while in the air
    public float groundRotateSpeed; // Rotation speed while grounded

    public GameObject playerModel; // Player's 3D model

    [HideInInspector] public bool hasControl; // Does the player have control?
    [HideInInspector] public bool jumpEnabled; // Is jumping enabled?
    [HideInInspector] public bool sprintEnabled; // Is sprinting enabled?
    [HideInInspector] public bool aimEnabled = true; // Is aiming enabled?

    [HideInInspector] public bool aiming; // Is the player aiming?
    [HideInInspector] public bool shooting = false; // Is the player shooting?

    // Wallrunning-related variables
    public LayerMask whatIsWall; // Layer mask for detecting walls
    bool isWallRunning; // Is the player currently wallrunning?
    public float maxWallRunCameraTilt; // Maximum camera tilt during wallrun
    public float wallRunCameraTilt; // Current camera tilt during wallrun
    public Transform orientation; // Orientation for wallrunning

    [SerializeField] bool onLeftWall, onRightWall; // Flags for wall detection
    RaycastHit leftWallHit, rightWallHit; // Raycast hits for wall detection
    Vector3 wallNormal; // Normal of the wall for movement calculation
    public CinemachineCollider walkingCollider; // Collider for camera clipping
    bool wallRunOnCD = false; // Wallrun cooldown
    public CameraController cc; // Camera controller for camera management
    [SerializeField] private float wallrunTilt; // Tilt amount for wallrunning
    public WeaponController wc; // Weapon controller for weapon management

    [SerializeField] private Transform respawnLocation; // Location to respawn the player

    // Debugging variables
    private bool cursorLocked; // Is the cursor locked?
    public ulong debugid; // Debug ID for the player
    public int characterId; // Character ID for player selection

    private Vector3 slideForward; // Direction of the slide
    private bool isSliding, isGPound; // Flags for sliding and ground pounding
    [SerializeField] private float slideTimer, maxSlideTimer; // Timers for sliding
    [SerializeField] private float gpoundCancelTimer, maxGPoundCancelTimer; // Timers for ground pounding

    public Camera cam; // Camera for teleportation and other actions
    public bool eTeleportEnabled = false; // Is teleportation enabled?
    public float tpRange; // Range for teleportation
    public LayerMask whatIsGrabbable; // Layer mask for grabbable objects

    // Start wallrunning
    private void StartWallRun() {
        if (!isWallRunning) {
            cc.ActivateCamera(2); // Activate wallrun camera
        }

        // Adjust the player model's rotation based on the wall's side
        if (onLeftWall) {
            playerModel.transform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, playerModel.transform.localEulerAngles.z - wallrunTilt);
        } else {
            playerModel.transform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, playerModel.transform.localEulerAngles.z + wallrunTilt);
        }

        // Set gravity scale to 0 and calculate movement direction for wallrunning
        gravityScale = 0f;
        wallNormal = onLeftWall ? leftWallHit.normal : rightWallHit.normal;
        moveDirection = Vector3.Cross(wallNormal, Vector3.up);
        grounded = true;
        isWallRunning = true;
        aimEnabled = false;

        // Ensure the movement direction is correctly aligned
        if (Vector3.Dot(moveDirection, pivot.transform.forward) < 0) moveDirection = -moveDirection;

        walkingCollider.enabled = false;
        wc.LoadBullets(); // Reload bullets when starting wallrun
    }

    // Stop wallrunning
    private void StopWallRun() {
        if (isWallRunning) cc.ActivateCamera(0); // Activate default camera
        gravityScale = storedGravityScale;
        isWallRunning = false;
        aimEnabled = true;
        StartCoroutine(DelayCameraClipthrough()); // Delay re-enabling camera collider
        cc.ActivateCamera(0);
    }

    // Check for walls and determine if wallrunning should start or stop
    private void CheckForWall() {
        // Check for walls on the left and right
        onLeftWall = Physics.Raycast(pivot.transform.position, -pivot.transform.right, out leftWallHit, 0.75f, whatIsWall);
        onRightWall = Physics.Raycast(pivot.transform.position, pivot.transform.right, out rightWallHit, 0.75f, whatIsWall);

        // Start wallrunning if conditions are met
        if (((onRightWall || onLeftWall) && speed >= sprintSpeed * 0.5f && Input.GetAxis("Vertical") > 0) && !isWallRunning && !wallRunOnCD && !grounded) StartWallRun();
        
        // Stop wallrunning if conditions are met
        if (((!onRightWall && !onLeftWall) || speed < sprintSpeed * 0.9f || Input.GetAxis("Vertical") < 0) && isWallRunning) StopWallRun();
    }

    // Delay before re-enabling the walking camera collider
    private IEnumerator DelayCameraClipthrough() {
        wallRunOnCD = true;
        yield return new WaitForSeconds(0.3f);
        walkingCollider.enabled = true;
        wallRunOnCD = false;
    }

    // Check for sliding input
    private void CheckForSlide() {
        if (Input.GetKeyDown(KeyCode.C)) { 
            if (speed > moveSpeed && !isSliding && !isWallRunning && grounded) {
                InitiateSlide(); // Start sliding
            }
        }
    }

    // Check for teleportation input
    private void CheckForTeleport() {
        if (Input.GetKeyDown(KeyCode.E) && wc.GetBullets() > 0) {
            RaycastHit hit;
            Vector3 direction = cam.transform.forward;

            // Check if a valid teleport location is hit
            if (Physics.Raycast(cam.transform.position, direction, out hit, wc.GetBullets() * tpRange, whatIsGrabbable)) {
                Teleport(hit.point); // Teleport to the hit location
                wc.ResetBullets(); // Reset bullets after teleportation
            }
        }
    }

    // Initiate sliding
    private void InitiateSlide() {
        AudioManager.Instance.PlaySoundEffect(4, 1f); // Play sliding sound effect
        cc.ActivateCamera(2); // Activate sliding camera
        isSliding = true;
        slideForward = playerModel.transform.forward; // Set sliding direction
        slideTimer = maxSlideTimer; // Set slide timer
    }

    private void Awake() {
        // Placeholder for any setup that might be needed
    }

    // Initialization
    void Start()
    {
        // Setup camera and other initial settings
        GameObject prePlayerCam = GameObject.Find("Pre-Player Cam");
        if (prePlayerCam != null) {
            prePlayerCam.SetActive(false);
        }

        GameObject tt = GameObject.Find("TTRunner");
        if (tt != null) { tt.GetComponent<TTRunner>().ResetTimer(); }
        
        controller = GetComponent<CharacterController>();
        cc.target = lookTarget;

        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor for FPS control
        cursorLocked = true;

        storedGravityScale = gravityScale;
        maxGPoundCancelTimer = 0.5f;
        cc.ActivateCamera(0);

        debugid = OwnerClientId; // Assign debug ID

        // Set respawn location based on team ID
        int teamId = GetComponent<WeaponController>().teamId;
        if (teamId == 0) respawnLocation = GameObject.Find("Red Spawn").transform;
        else if (teamId == 1) respawnLocation = GameObject.Find("Blue Spawn").transform;
        else respawnLocation = GameObject.Find("NEUTRAL SPAWN KILL THIS MF").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return; // Ensure only the owner controls the character

        // Handle cursor locking based on game pause state
        if (GameManager.Instance.pauseMenu.activeSelf) Cursor.lockState = CursorLockMode.None;
        else Cursor.lockState = CursorLockMode.Locked;

        if (hasControl)
        {
            CheckForWall(); // Check if wallrunning is needed
            CheckForSlide(); // Check if sliding is needed
            if (eTeleportEnabled) CheckForTeleport(); // Check if teleportation is needed

            // Stick Movement
            float yStore = moveDirection.y;
            float vertInput = Input.GetAxis("Vertical");
            float horizInput = Input.GetAxis("Horizontal");

            // Handle ground pound state and timer
            if (isGPound) {
                gpoundCancelTimer -= 1f * Time.deltaTime;
                if (gpoundCancelTimer < 0) {
                    // Reset ground pound if timer runs out
                }
            }

            // Handle sliding
            if (isSliding) {
                moveDirection = slideForward.normalized * slideSpeed;
                slideTimer -= 1f * Time.deltaTime;
                if (slideTimer < 0) {
                    cc.ActivateCamera(0); // Deactivate sliding camera
                    isSliding = false;
                }
            } else {
                // Handle movement and speed adjustments
                if (!wallRunOnCD) moveDirection = (pivot.forward * vertInput) + (pivot.right * horizInput);
                if (Mathf.Abs(vertInput) > 0 || Mathf.Abs(horizInput) > 0) {
                    speed += accel * Time.deltaTime; // Accelerate
                    if (Input.GetButton("Sprint") && sprintEnabled)
                    {
                        if (speed > sprintSpeed) { speed = sprintSpeed; } // Cap speed to sprintSpeed
                    }
                    else
                    {
                        if (speed > moveSpeed) { speed = moveSpeed; } // Cap speed to moveSpeed
                    }
                } else {
                    if (grounded) speed -= accel * 0.1f * Time.deltaTime; // Decelerate
                    if (speed < 0) { speed = 0; } // Ensure speed is not negative
                }

                // Apply speed with potential bullet time multiplier
                float appliedSpeed = (aiming && !grounded) ? speed * bulletTimeSpeedMult : speed * 1f;
                moveDirection = moveDirection.normalized * appliedSpeed;
            }

            moveDirection.y = yStore;

            // Handle jumping and gravity
            if (controller.isGrounded)
            {
                moveDirection.y = -1f;
                if (Input.GetButton("Jump") && jumpEnabled)
                {
                    moveDirection.y = jumpForce; // Apply jump force
                    AudioManager.Instance.PlaySoundEffect(2, 2f); // Play jump sound effect
                    isSliding = false;
                    isGPound = false;
                } else if (isGPound && Input.GetKey(KeyCode.C)) {
                    InitiateSlide(); // Initiate slide from ground pound
                    isGPound = false;
                }
            } else {
                if (Input.GetKeyDown(KeyCode.C) && !grounded && !isGPound) {
                    moveDirection.y = -jumpForce; // Apply ground pound force
                    AudioManager.Instance.PlaySoundEffect(3, 2f); // Play ground pound sound effect
                    isGPound = true;
                    gpoundCancelTimer = maxGPoundCancelTimer; // Reset ground pound timer
                }
            }

            // Handle wallrunning jump
            if (isWallRunning && Input.GetButtonDown("Jump")) {
                AudioManager.Instance.PlaySoundEffect(2, 2f); // Play jump sound effect
                moveDirection = (orientation.forward + wallNormal) * jumpForce * 10f;
                moveDirection.y = jumpForce * 0.8f;
                StopWallRun(); // Stop wallrunning after jump
            }

            // Debug ray for movement direction
            Debug.DrawRay(transform.position, moveDirection, Color.blue);

            // Extended grounded check using raycast
            RaycastHit hit = new RaycastHit();
            var distanceToGround = 0f;
            if (Physics.Raycast(transform.position, -Vector3.up, out hit))
            {
                distanceToGround = hit.distance;
            }

            // Apply movement direction and gravity
            moveDirection.y = moveDirection.y + (Physics.gravity.y * gravityScale * Time.deltaTime);
            if (isWallRunning) moveDirection.y = 0f;
            controller.Move(moveDirection * Time.deltaTime);

            // Update grounded status
            if (!isWallRunning) {
                grounded = controller.isGrounded || (distanceToGround < 1.5f && distanceToGround > 0);
            } else { grounded = true; }

            // Handle rotation
            if (aiming || isWallRunning) {
                playerModel.transform.rotation = Quaternion.Euler(0f, pivot.localEulerAngles.y, playerModel.transform.localEulerAngles.z);
            }
            else if ((Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0))
            {
                rotateSpeed = grounded ? groundRotateSpeed : airRotateSpeed;
                Quaternion newRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0f, moveDirection.z));
                if (!isWallRunning) playerModel.transform.rotation = Quaternion.Slerp(playerModel.transform.rotation, newRotation, rotateSpeed * Time.deltaTime);
            }
        }
        else {
            speed = 0; // Stop movement if not in control
        }

        // Update animator parameters
        anim.SetBool("aiming", aiming);
        anim.SetBool("shooting", shooting);
        anim.SetBool("isGrounded", grounded);
        anim.SetBool("isSliding", isSliding);
        anim.SetFloat("Speed", speed);
    }

    // Respawn the player
    public void Respawn() {
        Debug.Log("Respawning... " + OwnerClientId);
        GetComponent<Shootable>().IncrementDeathsServerRpc();
        if (OwnerClientId == 0) {
            RespawnServerRpc();
        } else {
            RespawnClientRpc(OwnerClientId);
        }
    }

    // Respawn the player with a specified team ID
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

    // Server RPC for respawning
    [ServerRpc(RequireOwnership = false)]
    public void RespawnServerRpc() {
        if (gameObject.GetComponent<Collider>().tag != "Player" || respawnLocation == null) return;
        controller.enabled = false;
        transform.position = respawnLocation.position;
        playerModel.transform.rotation = Quaternion.Euler(0f, respawnLocation.localEulerAngles.y, 0f);
        pivot.rotation = Quaternion.Euler(0f, respawnLocation.localEulerAngles.y, 0f);
        controller.enabled = true;
        gameObject.GetComponent<WeaponController>().ResetBullets();
    }

    // Client RPC for teleportation
    [ClientRpc]
    public void TeleportClientRpc(Vector3 tpLoc, ulong clientId) {
        if (IsOwner && clientId == OwnerClientId) {
            Teleport(tpLoc); // Teleport to the specified location
        }
    }

    // Perform teleportation
    public void Teleport(Vector3 tpLoc) {
        controller.enabled = false;
        Vector3 movVector = tpLoc - transform.position;
        float movMag = movVector.magnitude - 2f;
        transform.position += movVector.normalized * movMag;
        controller.enabled = true;
    }

    // Client RPC for respawning
    [ClientRpc]
    public void RespawnClientRpc(ulong ownerId) {
        if (OwnerClientId != ownerId || respawnLocation == null) return;
        controller.enabled = false;
        transform.position = respawnLocation.position;
        playerModel.transform.rotation = Quaternion.Euler(0f, respawnLocation.localEulerAngles.y, 0f);
        pivot.rotation = Quaternion.Euler(0f, respawnLocation.localEulerAngles.y, 0f);
        controller.enabled = true;
        gameObject.GetComponent<WeaponController>().ResetBullets();
    }
}
