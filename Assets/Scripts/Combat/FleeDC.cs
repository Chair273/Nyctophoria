using UnityEngine;
using TMPro;

public class FleeDC : MonoBehaviour
{
    public TextMeshProUGUI text;

    private void OnMouseEnter()
    {
        text.text = "DC: " + (3 + 2 * CombatHandler.main.GetParticipants(1).Count);
        text.enabled = true;
    }

    private void OnMouseExit()
    {
        text.enabled = false;
    }
}
