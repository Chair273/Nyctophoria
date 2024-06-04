using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool canMove;

    public float Speed;
    public float size;

    public Animator animator;

    private int prevX;

    private Rigidbody2D Rigidbody2D;

    void Start()
    {
        Rigidbody2D = transform.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!MainManager.combatManager.combat && canMove)
        {
            Vector3 MoveVector = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0);

            int yVector = Mathf.RoundToInt(MoveVector.y);
            int xVector = Mathf.RoundToInt(MoveVector.x);

            animator.SetBool("Moving", !MoveVector.Equals(Vector3.zero));
            animator.SetBool("Up", yVector > 0);

            Rigidbody2D.MovePosition(transform.position + new Vector3(xVector, yVector * (86f / 150f), 0) * Time.deltaTime * Speed);

            prevX = xVector != 0 ? xVector : prevX;
            
            transform.localScale = new Vector3((prevX > 0 ? 1 : -1) * (yVector > 0 ? 1 : -1) * size, size, 1);
        }
    }
}