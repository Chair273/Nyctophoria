using UnityEngine;

public class PlayerAdder : MonoBehaviour
{
    public string characterName;

    private bool debounce = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !debounce)
        {
<<<<<<< Updated upstream
            Manager.AddCharacter(characterName);
            Destroy(gameObject);
=======
            debounce = true;
            gameObject.SetActive(false);

            MainManager.characterManager.AddCharacter(characterName);

            MainManager.roomManager.RemoveObject(gameObject);
>>>>>>> Stashed changes
        }
    }
}
