using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : MonoBehaviour
{
    [SerializeField] private Animator unitAnimator;
    Vector3 targetPosition;
    Vector3 direction;
    private int speed = 5;
    private float stoppingdistance = 0.1f;

    private void Awake()
    {
        targetPosition = transform.position;
    }




    void Update()
    {

        direction = (targetPosition - transform.position).normalized;
        if (Vector3.Distance(transform.position, targetPosition) > stoppingdistance)
        {

            transform.position += direction * speed * Time.deltaTime;
            unitAnimator.SetBool("IsWalking", true);
            float rotateSpeed = 15f;
            transform.forward = Vector3 .Lerp (transform.forward,direction,rotateSpeed*Time.deltaTime);
        }
        else
        {
            unitAnimator.SetBool("IsWalking", false);
        }
    }

    public void Move(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
    }
}
