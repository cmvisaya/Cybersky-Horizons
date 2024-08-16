using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script that is attached to character prefabs in the Character Select Scene.
 * Stores stats to be displayed on stat cards in the Character Select Menu for the character that this script is attached to.
 * Stats are edited in the inspector.
 */

public class CharSelectStats : MonoBehaviour
{
    public string className;
    [Range(0, 100)]
    public int speed, damage, range, fireRate, magSize;
}
