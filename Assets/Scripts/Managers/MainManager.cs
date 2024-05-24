using UnityEngine;
using UnityEngine.EventSystems;

public class MainManager : MonoBehaviour
{
    public bool _LowGraphicsMode;
    public static bool LowGraphicsMode;

    public static Transform thePit;
    public static Transform theSquares;

    public static CharacterManager characterManager;
    public static SceneTransition sceneManager;
    public static RoomManager roomManager;
    public static CombatManager combatManager;

    public static Camera mainCamera;
    public static EventSystem eventSystem;

    public static void GameOver()
    {
        characterManager.ClearCharacters();
        characterManager.AddCharacter("One Armed Knight");

        sceneManager.LoadScene("Title");
    }

    private void Awake()
    {
        thePit = transform.Find("The Pit");
        theSquares = transform.Find("The Squares");
        mainCamera = transform.Find("Camera").GetComponent<Camera>();
        eventSystem = transform.Find("EventSystem").GetComponent<EventSystem>();

        LowGraphicsMode = _LowGraphicsMode;

        ActivateManagers();
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
