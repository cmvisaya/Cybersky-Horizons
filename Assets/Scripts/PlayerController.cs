using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerController : MonoBehaviour
{


    public float moveSpeed;
    public float sprintSpeed;
    public float speed;
    public float accel = 0.5f;
    public bool grounded = true;
    public float jumpForce;
    public float gravityScale;


    public CharacterController controller;
    private Vector3 moveDirection;


    public Animator anim;

    public Transform pivot;
    public float rotateSpeed;

    public GameObject playerModel;

    public bool hasControl = true;
    public bool jumpEnabled = true;
    public bool sprintEnabled = true;

    public bool aiming = false;


    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (GameObject.Find("Pivot"))
        {
            pivot = GameObject.Find("Pivot").transform;
        }
        Cursor.lockState = CursorLockMode.Locked;
    }


    // Update is called once per frame
    void Update()
    {
        if (hasControl)
        {
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
            moveDirection.y = moveDirection.y + (Physics.gravity.y * gravityScale * Time.deltaTime);

            //Move Direction Application
            controller.Move(moveDirection * Time.deltaTime);

            //Extended Grounded Check
            RaycastHit hit = new RaycastHit();
            var distanceToGround = 0f; ;
            if (Physics.Raycast(transform.position, -Vector3.up, out hit))
            {
                distanceToGround = hit.distance;
            }
            grounded = controller.isGrounded || distanceToGround < 1.5f;

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

    public void SnapAimModel() {
        //playerModel.transform.rotation = Quaternion.Euler(0f, pivot.localEulerAngles.y, 0f);
    }
}
