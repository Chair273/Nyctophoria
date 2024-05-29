using UnityEngine;
using System.Collections.Generic;

public class CombatStarter : MonoBehaviour
{
    public CapsuleCollider2D mainCollider;

    private bool debounce = false;

    private List<string> validEnemies = new List<string> { "Skeleton", "Crypt Keeper" };

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !debounce && !MainManager.combatManager.combat)
        {
            debounce = true;

            MainManager.roomManager.UnloadInstant();

            int amount = Random.Range(1, 4);

            for (int i = 0; i < amount; i++)
            {
                MainManager.characterManager.AddCharacter(validEnemies[Random.Range(0, validEnemies.Count)]);
            }

            MainManager.sceneManager.LoadScene("Combat");
            MainManager.roomManager.RemoveObject(gameObject);
        }
    }

}