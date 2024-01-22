using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int selectedCharacterCode, teamId, numTeams;
    public string displayName;
    public Dictionary<int, int> charCodes = new Dictionary<int, int>(); //First int is network client id

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //Default team values
        numTeams = 2;
        teamId = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }

    public void LoadScene(int id) {
        SceneManager.LoadScene(id);
    }

    public void ChangeTeam() {
        teamId = (teamId + 1) % numTeams;
    }
}
