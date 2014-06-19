
//adapted from Conrad Parker's pseudocode of Craig W. Reynold's Boid's algorithm
//for Unity3D 
//http://www.red3d.com/cwr
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum ZombieType
{
    normal,
    rage,
    magic
}

//adaptation of Boids pseudocode into Unity taken from http://www.kfish.org/boids/pseudocode.html
public class ZombieBoidsAlgorithm2D : MonoBehaviour
{
    
	// current collection of zombies
    private HashSet<GameObject> zombieList;
	
	// current list of humans
    public List<GameObject> humanList;
	
	// prefabs for zombie types
    public GameObject zombiePrefab;
    public GameObject rageZombiePrefab;
    public GameObject magicZombiePrefab;
	
	// explosion effect
    public GameObject explosion;
	
	// prefab for human
	public GameObject humanPrefab;
	
	// radius of explosion
    public float explosionRadius;
	
	// force of explosion
    public float explosionForce;
	
	// number of humans
    public int humanCount;
	
	// speed limit
    public float velocityLimit;
	
	// strength of the three boids rules: cohesion, separation, allignment
    public float rule1Strength;
    public float rule2Strength;
	public float rule3Strength;
	
	public float tendToStrength;
	
	
	// radius of boids rule 2: separation
    public int rule2Radius;
    
	// radius of boids flock
    public int flockRadius;
	
	// current target of THE HORDE
    public GameObject target;
	
    // target to reset to
    public GameObject resetTarget;
	
    public GameObject humans;

	//make boids
	private void Awake ()
	{
        zombieList = new HashSet<GameObject>();
        humanList = new List<GameObject>();
        SpawnHumans();
        SpawnZombie(resetTarget, true);
        

        StartCoroutine("Retarget");
	}

	private GameObject SpawnZombie(GameObject spawnPoint, bool isStarting)
	{
        int ZombieSpawnRNG = Random.Range(0, 100);
        if (isStarting == true)
        {
            // spawn a normal zombie if this is the first spawn
            ZombieSpawnRNG = 0;
        }
		
        GameObject clone;
        
        if (ZombieSpawnRNG < 66)
        {
            clone = Instantiate(zombiePrefab, spawnPoint.transform.position, transform.localRotation) as GameObject;
            clone.GetComponent<BoidInfo>().ZombieType = ZombieType.normal;
        }

        //else if (ZombieSpawnRNG >= 50 && ZombieSpawnRNG <= 90)
        else
        {
            clone = Instantiate(rageZombiePrefab, spawnPoint.transform.position, transform.localRotation) as GameObject;
            clone.GetComponent<BoidInfo>().ZombieType = ZombieType.rage;
            clone.GetComponent<RageSeek>().target = humanList[Random.Range(0, humanList.Count)];
        }
        /*
        else
        {
            clone = Instantiate(magicZombiePrefab, spawnPoint.transform.position, transform.localRotation) as GameObject;
            clone.GetComponent<BoidInfo>().ZombieType = ZombieType.magic;
        }
        */
        clone.GetComponent<BoidInfo>().Position = spawnPoint.transform.position;
        clone.GetComponent<BoidInfo>().Velocity = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
        clone.name = "Zombie " + zombieList.Count;
        clone.transform.parent = gameObject.transform;
        zombieList.Add(clone);
        
        

        return clone;
	}

    private void SpawnHumans()
    {
		for (int i = 0; i < humanCount; i++)
        {	
            Vector3 spawnPoint = new Vector3(0, 1.08f, 40) + new Vector3 (Random.Range (-5f, 5f), 0f, Random.Range (-5f, 5f));
			GameObject clone = Instantiate (humanPrefab, spawnPoint, transform.localRotation) as GameObject;
			clone.name = "Human " + i;
			clone.transform.parent = humans.transform;
			humanList.Add(clone);		
		}	    
    }

	void Update ()
	{
        if (target == null)
        {
            Retarget();
        }
		// horde algorithm
        MoveBoidsToNewPosition();
	}

	private void MoveBoidsToNewPosition ()
	{
        Vector3 v1 = Vector3.zero,
        v2 = Vector3.zero,
        v3 = Vector3.zero,
        v4 = Vector3.zero;
				
		foreach (GameObject zombie in zombieList) 
        {
            BoidInfo boidInfo = zombie.GetComponent<BoidInfo>();
            if (boidInfo.ZombieType == ZombieType.normal)
            {
                //normalizing these vectors will give the direction the boid should move to
                //the magnitude of these vectors will give how fast the boid should move there
                //relative to the timestep which is time.deltaTime;
                v1 = rule1Strength * Rule1(zombie);  	//Flock Centering (cohesion)										
                v2 = rule2Strength * Rule2(zombie);  //Collision Avoidance (seperation)
                v3 = rule3Strength * Rule3(zombie);  //Velocity Matching (alignment)						
                v4 = tendToStrength * TendToPlace(target, zombie);

                
                //Debug.Log("v1: " + v1);
                //Debug.Log("v2: " + v2);
                //Debug.Log("v3: " + v3);
                //Debug.Log("v4: " + v4);
                

                //the boidInfo.Velocity is the amount of positional change 
                //resulting in the offset vectors
                boidInfo.Velocity = (boidInfo.Velocity + v1 + v2 + v3 + v4);

                zombie.transform.rotation = Quaternion.LookRotation(boidInfo.Velocity);

                LimitSpeed(zombie);

                boidInfo.Velocity = new Vector3(boidInfo.Velocity.x, 0.0f, boidInfo.Velocity.z);
                //Interpret the velocity as how far the boid moves per time step we add it to the current position
                boidInfo.Position = boidInfo.Position + (boidInfo.Velocity * Time.deltaTime);
                //the new position of the boid is calculated by adding the offset vectors (v1,v2...vn) to the position
            }
        }
	}

	#region Rule 1: Boids try to fly towards the centre of mass of neighbouring boids.
		
	private Vector3 Rule1 (GameObject boid) //(cohesion
	{
        if (zombieList.Count > 1)
        {
			//current boid info
            BoidInfo boidInfo = boid.GetComponent<BoidInfo>(); 

            Vector3 perceivedCenter = Vector3.zero;

            foreach (GameObject b in zombieList)
            {
				//neighbors
                BoidInfo bInfo = b.GetComponent<BoidInfo>(); 

                if (b != boid)
                {
                    //doing another calculation to see if I can get some of the boids in the same system to split up at times
                    //for randomness
                    if (Vector3.Distance(bInfo.Position, boidInfo.Position) < flockRadius)
                    {
                        //neighborhood
                        perceivedCenter += bInfo.Position;
                    }
                }
            }

            perceivedCenter /= (zombieList.Count - 1); //dividing by the size of the array -1
            //gives the average perceived center of mass

            perceivedCenter = (perceivedCenter - boidInfo.Position) / 100;
            //how strong the boid will move to the center
            //higher means less strength
            return new Vector3(perceivedCenter.x, 0, perceivedCenter.z);
        }

        else
        {
            return Vector3.zero;
        }
	}

	#endregion

	#region Rule 2: Boids try to keep a small distance away from other objects (including other boids).
		
	private Vector3 Rule2 (GameObject boid)
	{
		//current boid info
		BoidInfo boidInfo = boid.GetComponent<BoidInfo> (); 

		Vector3 displacement = Vector3.zero;

        foreach (GameObject b in zombieList)
        {
			 //neighbor
			BoidInfo bInfo = b.GetComponent<BoidInfo> ();

			if (b != boid) 
            {
				//if the distance between the current boid and his neighbor
				//is less than 10 they are too close and must be seperated
								
				if (Vector3.Distance (bInfo.Position, boidInfo.Position) < rule2Radius) 
                {		
					//calculate a displacement to move them apart
					//the displacement will result in a vector
					//that when added to the original velocity vector will
					//move them away from each other
					displacement = displacement - (bInfo.Position - boidInfo.Position);			
				}
			}
		}

		return displacement;
	}
	#endregion
		
	#region Rule 3: Boids try to match velocity with near boids.

	private Vector3 Rule3 (GameObject boid)
	{
        if (zombieList.Count > 1)
        {
			//current boid info
            BoidInfo boidInfo = boid.GetComponent<BoidInfo>(); 

            Vector3 perceivedVelocity = Vector3.zero;

            foreach (GameObject b in zombieList)
            {
                BoidInfo bInfo = b.GetComponent<BoidInfo>();

                if (b != boid)
                {
                    //if the distance from this boid and another boid is less than a set amount then they are in the same neighborhood	
                    if (Vector3.Distance(boidInfo.Position, bInfo.Position) < flockRadius)
                    {
						
                        perceivedVelocity += bInfo.Velocity;			
                    }
                }
            }

            perceivedVelocity /= (zombieList.Count - 1);
            perceivedVelocity = (perceivedVelocity - boidInfo.Velocity) / 8; 
			//using conrad's magic /8 till i get a better handle on what the vectors are doing
			
            //higher means less strength

            return new Vector3(perceivedVelocity.x, 0, perceivedVelocity.z);
        }

        else
        {
            return Vector3.zero;
        }
	}

	#endregion

	#region Limiting the speed

	private void LimitSpeed (GameObject boid)
	{				
		BoidInfo boidInfo = boid.GetComponent<BoidInfo> ();
        if (boidInfo.Velocity.magnitude > velocityLimit)//if the size of the velocity is greater than the limit set
        {
            //normalize it and scale it by the limit
            boidInfo.Velocity = boidInfo.Velocity.normalized * velocityLimit;
        }


		//magnitude is the length of a vector
		//a^2 + b^2 = c^2
		//or a length c is given by the sqrt(a^2 + b^2)
	}

	#endregion

	#region Tend to place
    private Vector3 TendToPlace (GameObject place, GameObject boid)
    {
		Vector3 tendTo;
		BoidInfo boidInfo = boid.GetComponent<BoidInfo> ();

		tendTo = place.transform.position;
				
		tendTo = tendTo - (boidInfo.Position);
		tendTo = tendTo / 10;
		return new Vector3(tendTo.x, 0, tendTo.z);	
    }


	
	#endregion

    #region fuckshitup

    public void TargetBitten(GameObject poorSap)
    {
        if (humanList.Remove(poorSap))
        {
            SpawnZombie(poorSap, false);
            Destroy(poorSap);
            Retarget();
        }
    }

    public void TargetExplode(GameObject explodeToBits, GameObject rageZombie)
    {
        if (humanList.Remove(explodeToBits))
        {
            
            zombieList.Remove(rageZombie);
            /*
            Collider[] colliders = Physics.OverlapSphere(explodeToBits.transform.position, explosionRadius);

            foreach (Collider item in colliders)
            {
                if (item.gameObject.rigidbody != null)
                {
                    Debug.Log("i explode u");
                    item.gameObject.rigidbody.AddExplosionForce(explosionForce, explodeToBits.transform.position, explosionRadius, 0.0f, ForceMode.Impulse);
                }
            }
            */
            Instantiate(explosion, explodeToBits.transform.position, Quaternion.identity);

            Destroy(explodeToBits);
            Destroy(rageZombie);

        }
    }

    private void Retarget()
    {
        if (humanList.Count > 0)
        {
            //target = humanList[Random.Range(0, humanList.Count)];

            // pass one: just target closest human

            // find center of zombie pack

            Vector3 centerSummation = Vector3.zero;

            foreach(GameObject item in zombieList)
            {
                centerSummation += item.transform.position;
            }

            Vector3 centerOfZombies = centerSummation / zombieList.Count;

            // find closest human to center

            humanList.Sort
            (
                delegate(GameObject object1, GameObject object2)
                {
                    // compare the distance from each object to our position, using the magnitude of a distance vector
                    return ((object1.transform.position - centerOfZombies).magnitude).CompareTo((object2.transform.position - centerOfZombies).magnitude);
                }
            );

            target = humanList[0];

        }

        else
        {
            target = resetTarget;
        }
    }

    #endregion
}
