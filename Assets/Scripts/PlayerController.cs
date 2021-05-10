using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    float moveSpeed = 5f;
    [SerializeField]
    Transform movePoint;

    [SerializeField]
    LayerMask whatStopsMovement;

    void Start()
    {
        movePoint.parent = null;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, movePoint.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, movePoint.position) <= 0.05f) 
        {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f)
            {
                Vector3 desiredMovement = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f);
                if (!Physics2D.OverlapCircle(movePoint.position + desiredMovement, .2f, whatStopsMovement)) 
                {
                    movePoint.position += desiredMovement;
                }
            } 
            else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f)
            {
                Vector3 desiredMovement = new Vector3(0f, Input.GetAxisRaw("Vertical"), 0f);
                if (!Physics2D.OverlapCircle(movePoint.position + desiredMovement, .2f, whatStopsMovement)) 
                {
                    movePoint.position += new Vector3(0f, Input.GetAxisRaw("Vertical"), 0f);
                }
            }
        }
    }
}
