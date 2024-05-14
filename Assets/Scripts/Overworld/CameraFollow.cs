using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance; // Singleton instance
    public Vector3 offset; // Offset to maintain between the camera and the player
    private Transform target; // Reference to the player's Transform

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void MoveCamera()
    {
        // Check if the player's Transform is assigned
        if (RoomTransfer.CameraMove == true)
        {
            // Move the camera's position to match the player's position with an offset
            transform.position += offset;
        }
    }
}