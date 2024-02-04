using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharSelectStats : MonoBehaviour
{
    public string className;
    [Range(0, 100)]
    public int speed, damage, range, fireRate, magSize;
}
