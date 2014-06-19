using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WanderHuman : MonoBehaviour 
{
	// movement speed of the human
    public float MoveSpeed;
	
	// force due to gravity (constant for the purposes of this assignment)
    public float Gravity = -50; 
	
	// interval to update the target position
    public float Jitter;
	
	// interval to update the target position (jitter more vs. rage zombies)
    public float OnFireJitter; 
	
	// distance from the human to the edge of the steering circle
    public float Distance; 
	
	// radius of the steering circle
    public float Radius; 

	// the character controller that is told how to move based on vector calculations
    private CharacterController controller; 
	
	// position in world space the character will wander towards
    private Vector3 targetPosition; 
	
	// position of the steering circle
    private Vector3 steeringCirclePosition; 
	
	 // counter used for updating target position
    private float timeSinceUpdate;
	
	private bool wall = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>(); 
        targetPosition = Vector3.zero; 
        steeringCirclePosition = Vector3.zero; 
        timeSinceUpdate = Jitter;
    }

    void Update()
    {
		// if the character controller is touching the ground
        if (controller.isGrounded) 
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, 10, 1 << LayerMask.NameToLayer("Enemy"));
			
			// if we current have no close enemies
            if (enemies.Length == 0)
            {
                MoveSpeed = 3;
				// if it's been enough time since the last update
                if (timeSinceUpdate <= Time.time) 
                {
					// pick a random point on the edge of a circle
                    Vector2 randomDirection = Random.insideUnitCircle.normalized * Radius; 
					
					// update the target position moving towards the random point drawn in front of the current character
                    targetPosition = new Vector3(randomDirection.x, 0, randomDirection.y) + steeringCirclePosition; 
					 // update the timer
                    timeSinceUpdate = Jitter + Time.time;
                }
				
				// update steering circle position
                steeringCirclePosition = transform.position + transform.forward * (Distance + Radius); 

                if (wall)
                {
                    steeringCirclePosition *= -1;
                    wall = false;
                }
            }

			
			// else, run away!
            else
            {
                MoveSpeed = 8;
				
				// init closest enemy with just the first enemy
                GameObject closestEnemy = enemies[0].gameObject;

				// find closest enemy, comparing distances
                foreach (Collider enemyCollider in enemies)
                {
                    if (Vector3.Distance(transform.position, enemyCollider.transform.position) < Vector3.Distance(transform.position, closestEnemy.transform.position))
                    {
                        closestEnemy = enemyCollider.gameObject;
                    }
                }
                
				// move in the opposite direction of the closest enemy
                targetPosition = closestEnemy.transform.position;
                Vector3 vectorToTarget = transform.position - targetPosition;
                targetPosition = transform.position + vectorToTarget;
            }
        }

		// rotate towards target position
        transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z)); 

        Debug.DrawRay(transform.position, targetPosition - transform.position, Color.blue);

        // create a new force vector using MoveSpeed to determine the x and z components and 
        // gravity to determine the y component
        Vector3 moveForce = new Vector3(transform.forward.x * MoveSpeed, Gravity, transform.forward.z * MoveSpeed);
		// translate force with respect to time
        moveForce = moveForce * Time.deltaTime; 
		// apply force 
		controller.Move(moveForce); 
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.tag == "HideObject")
        {
			wall = true;
        }
    }
}
