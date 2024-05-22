using UnityEngine;

public class Menu : MonoBehaviour
{
    private bool debounce = false;

    void Update()
    {
        if (Input.anyKeyDown && !debounce)
        {
            debounce = true;
            MainManager.sceneManager.LoadScene("Overworld", "Title");
        }
    }
}
