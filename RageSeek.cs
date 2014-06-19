using UnityEngine;
using System.Collections;

public class RageSeek : MonoBehaviour
{
	// current target
    public GameObject target;
	
	// speed modifier
    public float SpeedMod;
	
	// current target position
    private Vector3 targetPosition;
	
	// horde of zombies
    public ZombieBoidsAlgorithm2D flock;
    

    void Awake()
    {
        targetPosition = Vector3.zero;
        flock = GameObject.Find("Zombies").GetComponent<ZombieBoidsAlgorithm2D>();
    }

	
    void Update()
    {
        if (_flock._humanList.Count > 0)
        {
			// if there's no target, grab one from the list of human targets
            if (target == null)
            {
                target = flock.humanList[Random.Range(0, flock.humanList.Count)];
            }

            targetPosition = target.transform.position;
			// move towards the target in a beeline
            Vector3 newPosition = Vector3.MoveTowards(transform.position, target.transform.position, SpeedMod * GetComponent<BoidInfo>().Speed);
            transform.position = newPosition;

            if (target.tag == "Human")
            {
				// if we're within range to explode, and we are indeed a ragezombie (sanity check), then explode
                if (Vector3.Distance(transform.position, target.transform.position) <= flock.flockRadius)
                {
                    if (GetComponent<BoidInfo>().ZombieType == ZombieType.rage)
                    {
                        Debug.Log("Explode!");
                        flock.TargetExplode(target, gameObject);
                    }
                }
            }
        }
    }
}
