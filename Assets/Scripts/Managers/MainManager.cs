using UnityEngine;

public class MainManager : MonoBehaviour
{
    public static Transform thePit;

    public static CharacterManager characterManager;
    public static SceneTransition sceneManager;
    public static RoomManager roomManager;
    public static CombatManager combatManager;

    public static void GameOver()
    {
        characterManager.ClearCharacters();
        characterManager.AddCharacter("One Armed Knight");

        sceneManager.LoadScene("Title", sceneManager.GetSceneName());
    }

    private void Awake()
    {
        ActivateManagers();
        thePit = transform.Find("The Pit");
    }

    private void ActivateManagers()
    {
        characterManager = transform.GetComponent<CharacterManager>();
        sceneManager = transform.GetComponent<SceneTransition>();
        roomManager = transform.GetComponent<RoomManager>();
        combatManager = transform.GetComponent<CombatManager>();

        characterManager.DefineCharacters();
        combatManager.DefineCards();
        roomManager.GenerateArea();

        characterManager.AddCharacter("One Armed Knight");

        sceneManager.LoadScene("Title");
    }
}