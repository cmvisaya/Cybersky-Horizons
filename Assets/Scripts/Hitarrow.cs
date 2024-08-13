using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hitarrow : MonoBehaviour
{
    public RawImage arrowTexture;
    public RectTransform myTransform;
    public Vector3 followTarget, belongsToPosition;
    public GameObject myPivot;
    // Start is called before the first frame update
    void Awake()
    {
        arrowTexture.CrossFadeAlpha(0f, 0f, false);
        StartCoroutine(TimedDeath());
    }

    public void Init(Vector3 ft, Vector3 btp, GameObject p) {
        arrowTexture.CrossFadeAlpha(1f, 0f, false);
        arrowTexture.CrossFadeAlpha(0f, 1f, false);
        followTarget = ft;
        belongsToPosition = btp;
        myPivot = p;
    }

    // Update is called once per frame
    void Update()
    {
        if(followTarget != null && belongsToPosition != null) {
            float xDist = followTarget.x - belongsToPosition.x;
            float zDist = followTarget.z - belongsToPosition.z;
            float angleToObjective = CalculateAngle(zDist, xDist);
            Vector3 angleCalc = new Vector3(0f, 0f, angleToObjective + myPivot.transform.localEulerAngles.y - 90f);
            Debug.Log(angleCalc);
            myTransform.rotation = Quaternion.Euler(0f, 0f, angleCalc.z);
        }
    }

    private float CalculateAngle(float dx, float dz) {
        float angle = 90f - Mathf.Atan2(dz, dx) * 180f / Mathf.PI;
        if(angle > 180f) { angle -= 360f; }
        return angle;
    }

    private IEnumerator TimedDeath() {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
