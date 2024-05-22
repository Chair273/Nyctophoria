using UnityEngine;
using TMPro;

public class DrawPileDisplay : MonoBehaviour
{
    public TextMeshProUGUI text;

    private void OnMouseEnter()
    {
        Character current = CombatHandler.main.GetCurrentCharacter();

        if (current == null || current.GetGridPos().x == 1)
        {
            return;
        }
        int amount = current.GetDrawPileCount();

        text.text = (amount == 0 ? "No" : amount) + " card" + (amount != 1 ? "s " : " ") + "remaining."; //No cards remaining. 1 card remaining. # cards remaining.
        text.enabled = true;
    }

    private void OnMouseExit()
    {
        text.enabled = false;
    }
}
