using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSelectData : MonoBehaviour
{
    public int selectedCharacterCode = 0;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
