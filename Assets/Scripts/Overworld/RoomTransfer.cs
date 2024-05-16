using UnityEngine;
using TMPro;

public class RoomTransfer : MonoBehaviour
{
    private Vector2Int roomIndex;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && roomIndex != null)
        {
            RoomGenerator.ChangeRoom(roomIndex);
        }
    }

    public void SetRoomIndex(Vector2Int roomIndex)
    {
        this.roomIndex = roomIndex;
        transform.Find("Canvas").Find("Text").GetComponent<TextMeshProUGUI>().text = "(" + roomIndex.x + "," + roomIndex.y + ")";
    }
}
