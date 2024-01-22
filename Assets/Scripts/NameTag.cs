using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NameTag : MonoBehaviour
{
    public TMP_Text displayName;

    public void SetName(string newName) {
        displayName.text = newName;
    }

    public void SetTeam(int teamId) {
        switch (teamId) {
            case 0: //Red
                displayName.color = Color.red;
                break;
            case 1:
                displayName.color = Color.blue;
                break;
        }
    }
}
