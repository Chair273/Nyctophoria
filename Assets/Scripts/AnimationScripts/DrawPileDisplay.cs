using UnityEngine;
using TMPro;

public class DrawPileDisplay : MonoBehaviour
{
    public TextMeshProUGUI amount;

    private void OnMouseEnter()
    {
        amount.text = CombatHandler.GetCurrentCharacter().GetDrawPileCount() + " cards remaining.";
        amount.enabled = true;
    }

    private void OnMouseExit()
    {
        amount.enabled = false;
    }
}
