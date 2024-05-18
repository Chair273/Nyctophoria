using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    private void Start()
    {
        SceneManager.LoadScene("Manager", LoadSceneMode.Additive);
        Manager.AddCharacter("One Armed Knight");
    }

    void Update()
    {
        if (Input.anyKeyDown)
        {
            SceneManager.LoadScene("RoomGenerator");
        }
    }
}
