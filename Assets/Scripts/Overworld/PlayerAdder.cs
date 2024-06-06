using UnityEngine;

public class PlayerAdder : MonoBehaviour
{
    public string characterName;

    private bool debounce = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !debounce)
        {
            debounce = true;
            gameObject.SetActive(false);

            MainManager.characterManager.AddCharacter(characterName, null);

            MainManager.roomManager.RemoveObject(gameObject);
        }
    }
}
