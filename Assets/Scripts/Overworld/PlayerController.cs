using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D Rigidbody2D;

    public float Speed;

    public Animator animator;

    void Start()
    {
        Rigidbody2D = transform.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        float horizontalAxis = Input.GetAxis("Horizontal");
        float verticalAxis = Input.GetAxis("Vertical");

        Vector3 MoveVector = new Vector3(horizontalAxis > 0 ? 1 : horizontalAxis < 0 ? -1 : 0, verticalAxis > 0 ? 1 : verticalAxis < 0 ? -1 : 0, 0);

        animator.SetBool("Moving", !MoveVector.Equals(Vector3.zero));

        Rigidbody2D.MovePosition(transform.position + new Vector3(MoveVector.x, MoveVector.y * (86f / 150f), 0) * Time.deltaTime * Speed);

        if (horizontalAxis != 0)
        {
            float yScale = MoveVector.y != 0 ? MoveVector.y : -1;
            transform.localScale = new Vector3(MoveVector.x * yScale * 0.4f, 0.4f, 1);
        }
        else
        {
            transform.localScale = new Vector3(0.4f, 0.4f, 1);
        }

        animator.SetFloat("Y axis", MoveVector.y);
    }
}