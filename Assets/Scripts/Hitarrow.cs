using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script that is attached to the uninstantiated prefab of hit arrow UI element.
 * Indicator for when player takes damage, denoting from which direction.
 */

public class Hitarrow : MonoBehaviour
{
    // UI element representing the arrow's texture
    public RawImage arrowTexture;

    // Reference to the RectTransform of this arrow for UI positioning and rotation
    public RectTransform myTransform;

    // Target position that the arrow will follow
    public Vector3 followTarget;

    // Position from which the arrow's direction is calculated
    public Vector3 belongsToPosition;

    // The pivot GameObject that serves as a reference for calculating rotation
    public GameObject myPivot;

    // Called when the script instance is being loaded
    void Awake()
    {
        // Initially make the arrow texture invisible
        arrowTexture.CrossFadeAlpha(0f, 0f, false);

        // Start a coroutine that destroys this arrow after a set time
        StartCoroutine(TimedDeath());
    }

    // Initializes the arrow's target position, origin position, and pivot object
    public void Init(Vector3 ft, Vector3 btp, GameObject p) {
        // Fade in the arrow texture
        arrowTexture.CrossFadeAlpha(1f, 0f, false);

        // Fade out the arrow texture over 1 second
        arrowTexture.CrossFadeAlpha(0f, 1f, false);

        // Set the follow target, origin position, and pivot
        followTarget = ft;
        belongsToPosition = btp;
        myPivot = p;
    }

    // Update is called once per frame
    void Update()
    {
        // Ensure both followTarget and belongsToPosition are set
        if(followTarget != null && belongsToPosition != null) {
            // Calculate the x and z distance between the target and origin positions
            float xDist = followTarget.x - belongsToPosition.x;
            float zDist = followTarget.z - belongsToPosition.z;

            // Calculate the angle to the objective based on the x and z distances
            float angleToObjective = CalculateAngle(zDist, xDist);

            // Adjust the angle with the pivot's local Y rotation and offset by -90 degrees
            Vector3 angleCalc = new Vector3(0f, 0f, angleToObjective + myPivot.transform.localEulerAngles.y - 90f);

            // Output the calculated angle to the debug console
            Debug.Log(angleCalc);

            // Rotate the arrow UI element to point towards the target
            myTransform.rotation = Quaternion.Euler(0f, 0f, angleCalc.z);
        }
    }

    // Calculates the angle in degrees between the origin and target positions
    private float CalculateAngle(float dx, float dz) {
        // Calculate the angle using arctangent and convert to degrees
        float angle = 90f - Mathf.Atan2(dz, dx) * 180f / Mathf.PI;

        // Normalize the angle to be within the range -180 to 180 degrees
        if(angle > 180f) { angle -= 360f; }

        return angle;
    }

    // Coroutine that waits for a specified time and then destroys the arrow object
    private IEnumerator TimedDeath() {
        // Wait for 1 second
        yield return new WaitForSeconds(1f);

        // Destroy the arrow game object
        Destroy(gameObject);
    }
}
