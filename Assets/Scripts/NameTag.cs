using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;

/*
 * Script that is attached to Nametag gameobjects which are children of player prefabs
 * Handles initialization of name display tag above players heads in multiplayer
 */

public class NameTag : MonoBehaviour
{
    public TMP_Text displayNameText; // Reference to the TextMeshPro text component

    // Set the color directly in the inspector if needed
    [SerializeField] private Color redTeamColor = Color.red;
    [SerializeField] private Color blueTeamColor = Color.blue;

    public void SetName(string newName)
    {

        //Debug.Log(newName);
        displayNameText.text = newName;
    }

    public void SetTeam(int teamId)
    {
        switch (teamId)
        {
            case 0: // Red
                displayNameText.color = redTeamColor;
                break;
            case 1: // Blue
                displayNameText.color = blueTeamColor;
                break;
        }
    }
}
