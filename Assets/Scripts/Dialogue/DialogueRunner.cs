using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueRunner : MonoBehaviour
{
    public DialogueEvent myEvent;
    public float delayTime;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(myEvent.StartWithDelayTime(delayTime));
    }
}
