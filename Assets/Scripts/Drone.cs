using UnityEngine;
using System.Collections;
using System;

public class Drone : Enemy {
    public float maxHealth;
    public int maxRange = 1500;
    public float repairRate = 20;
    public float separationDistance = 25.0f;
    public float cohesionDistance = 50.0f;
    public float separationStrength = 250.0f;
    public float cohesionStrength = 25.0f;
    private Vector3 cohesionPos = new Vector3(0, 0, 0);
    private int boidIndex = 0;

    private float attackOrFlee;

    public GameObject laser;
    public GameObject motherShip;
    public Vector3 scoutPosition;

    public Vector3 roamPos;

    GameManager gameManager;

    Rigidbody rb;

    //Movement & Rotation Variables
    public float speed = 50.0f;
    private float rotationSpeed = 5.0f;
    private float adjRotSpeed;
    private Quaternion targetRotation;
    public GameObject target;
    public float targetRadius = 200f;

    public GameObject forageTarget;

    public float carrying = 0;
    public float capacity = 30;
    public float forageRate = 10;
    private bool elite = false;

    private float scoutTimer;
    private float detectTimer;
    private float scoutTime = 10.0f;
    private float detectTime = 5.0f;
    private float detectionRadius = 400.0f;
    private int newResourceVal = 0;
    public GameObject newResourceObject;
    public float currentResource;

    private float fireTimer;
    private float fireTime = 1;
    private Vector3 tarVel;
    private Vector3 tarPrevPos;
    private Vector3 attackPos;
    private Vector3 fleePos;
    private float distanceRatio = 0.05f;

    public enum DroneBehaviours {
        Idle,
        Scouting,
        Foraging,
        Attacking,
        Fleeing,
        Return
    }

    public DroneBehaviours droneBehaviour;

    //Boid Steering/Flocking Variables

    // Use this for initialization
    void Start() {
        maxHealth = health;
        health = UnityEngine.Random.Range(0, health);
        forageRate = UnityEngine.Random.Range(0, 30);
        capacity = UnityEngine.Random.Range(0, 100);
        maxRange = UnityEngine.Random.Range(Mathf.RoundToInt(targetRadius * 2), 1500);

        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        rb = GetComponent<Rigidbody>();

        motherShip = gameManager.alienMothership;
        scoutPosition = motherShip.transform.position;
    }

    // Update is called once per frame
    void Update() {
        //Acquire player if spawned in
        if (gameManager.gameStarted) {
            target = gameManager.playerDreadnaught;

            if (droneBehaviour == DroneBehaviours.Attacking
                || droneBehaviour == DroneBehaviours.Fleeing) {
                attackOrFlee = health * Friends();

                if (attackOrFlee >= 1000)
                    droneBehaviour = DroneBehaviours.Attacking;
                else if (attackOrFlee < 1000)
                    droneBehaviour = DroneBehaviours.Fleeing;
            }
        }

        //Move towards valid targets
        else if (target)
            MoveTowardsTarget(target.transform.position);

        BoidBehaviour();

        switch (droneBehaviour) {
            case DroneBehaviours.Scouting:
                Scouting();
                break;
            case DroneBehaviours.Attacking:
                Attacking();
                break;
            case DroneBehaviours.Fleeing:
                Fleeing();
                break;
            case DroneBehaviours.Idle:
                Repair();
                Roam();
                return;
            case DroneBehaviours.Foraging:
                Forage();
                return;
            case DroneBehaviours.Return:
                Return();
                return;
        }
    }

    public void Return() {
        if (Vector3.Distance(transform.position, motherShip.transform.position) > targetRadius) {
            MoveTowardsTarget(motherShip.transform.position);
        } else {
            if (motherShip.GetComponent<Mothership>().drones.Contains(gameObject))
                motherShip.GetComponent<Mothership>().drones.Remove(gameObject);

            if (motherShip.GetComponent<Mothership>().scouts.Contains(gameObject))
                motherShip.GetComponent<Mothership>().scouts.Remove(gameObject);

            if (motherShip.GetComponent<Mothership>().foragers.Contains(gameObject))
                motherShip.GetComponent<Mothership>().foragers.Remove(gameObject);

            if (motherShip.GetComponent<Mothership>().eliteForagers.Contains(gameObject))
                motherShip.GetComponent<Mothership>().eliteForagers.Remove(gameObject);

            Destroy(gameObject);
        }
    }

    public void Roam() {
        if (Vector3.Distance(transform.position, roamPos) <= targetRadius || Vector3.Distance(roamPos, motherShip.transform.position) > targetRadius * 2) {
            Vector3 pos = motherShip.transform.position;
            pos.x += UnityEngine.Random.Range(-targetRadius * 2, targetRadius * 2);
            pos.y += UnityEngine.Random.Range(-targetRadius * 2, targetRadius * 2);
            pos.z += UnityEngine.Random.Range(-targetRadius * 2, targetRadius * 2);

            roamPos = pos;
        }

        MoveTowardsTarget(roamPos);
        Debug.DrawLine(transform.position, roamPos, Color.blue);
    }

    private void Repair() {
        if (health < maxHealth) {
            health += repairRate * Time.deltaTime;
            Debug.Log("Repair");
        }
        if (health > maxHealth) {
            health = maxHealth;
            if (gameManager.gameStarted) {
                attackOrFlee = health * Friends();

                if (attackOrFlee >= 1000)
                    droneBehaviour = DroneBehaviours.Attacking;
                else if (attackOrFlee < 1000)
                    droneBehaviour = DroneBehaviours.Fleeing;
            }
        }
    }

    public void StartForaging(bool elite) {
        droneBehaviour = DroneBehaviours.Foraging;
        target = motherShip.GetComponent<Mothership>().FindResourceInRange(maxRange, gameObject);
        currentResource = target.GetComponent<Asteroid>().resource;
        forageTarget = target;

        this.elite = elite;
    }

    public void Forage() {
        if (elite) {
            Debug.DrawLine(transform.position, target.transform.position, Color.magenta);

            if (Vector3.Distance(transform.position, roamPos) < targetRadius && Time.time < scoutTimer && target != motherShip) {
                Vector3 pos = motherShip.transform.position;
                pos.x += UnityEngine.Random.Range(-targetRadius * 2, targetRadius * 2);
                pos.y += UnityEngine.Random.Range(-targetRadius * 2, targetRadius * 2);
                pos.z += UnityEngine.Random.Range(-targetRadius * 2, targetRadius * 2);

                roamPos = pos;

                MoveTowardsTarget(roamPos);

                if (!newResourceObject) {
                    if (Time.time > detectTimer) {
                        newResourceObject = DetectNewResources();
                        if (newResourceObject && newResourceObject.GetComponent<Asteroid>().resource <= currentResource)
                            newResourceObject = null;
                        detectTimer = Time.time + detectTime;
                    }
                }
            }

            if (newResourceObject) {
                Debug.DrawLine(transform.position, target.transform.position, Color.black);

                if (Vector3.Distance(transform.position, target.transform.position) < targetRadius) {
                    motherShip.GetComponent<Mothership>().drones.Add(gameObject);
                    motherShip.GetComponent<Mothership>().scouts.Remove(gameObject);

                    motherShip.GetComponent<Mothership>().resourceObjects.Add(newResourceObject);
                    newResourceVal = 0;
                    newResourceObject = null;

                    droneBehaviour = DroneBehaviours.Idle;
                }
            }
        } else
            Debug.DrawLine(transform.position, target.transform.position, Color.cyan);

        if (Vector3.Distance(target.transform.position, transform.position) < targetRadius) {
            scoutTimer = Time.deltaTime + scoutTime;
            Asteroid a = forageTarget.GetComponent<Asteroid>();
            float previous = a.resource;

            if (carrying + forageRate * Time.deltaTime < capacity)
                a.DrainResource(forageRate * Time.deltaTime);
            else
                a.DrainResource(capacity - carrying);

            carrying += previous - a.resource;
            if (a.resource <= 0 || (elite && newResourceObject)) {
                if (Vector3.Distance(transform.position, motherShip.transform.position) > targetRadius) {
                    if (target != motherShip) {
                        target = motherShip;
                    }
                } else {
                    motherShip.GetComponent<Mothership>().resourceObjects.Remove(forageTarget);
                    motherShip.GetComponent<Mothership>().harvested += carrying;
                    carrying = 0;
                    droneBehaviour = DroneBehaviours.Idle;

                    if (elite)
                        motherShip.GetComponent<Mothership>().eliteForagers.Remove(gameObject);
                    else
                        motherShip.GetComponent<Mothership>().foragers.Remove(gameObject);
                    motherShip.GetComponent<Mothership>().drones.Add(gameObject);
                }
            } else if (carrying >= capacity) {
                if (Vector3.Distance(transform.position, motherShip.transform.position) > targetRadius) {
                    target = motherShip;
                } else {
                    motherShip.GetComponent<Mothership>().harvested += carrying;
                    carrying = 0;
                    target = forageTarget;
                }
            }
        }
    }

    private int Friends() {
        int clusterStrength = 0;

        for (int i = 0; i < gameManager.enemyList.Length; i++) {
            if (Vector3.Distance(transform.position, gameManager.enemyList[i].transform.position) < targetRadius) {
                clusterStrength++;
            }
        }

        return clusterStrength;
    }

    public void Scouting() {
        if (!newResourceObject) {
            if (Vector3.Distance(transform.position, scoutPosition) < detectionRadius && Time.time > scoutTimer) {
                Vector3 position;
                position.x = motherShip.transform.position.x + UnityEngine.Random.Range(-maxRange, maxRange);
                position.y = motherShip.transform.position.y + UnityEngine.Random.Range(-400, 400);
                position.z = motherShip.transform.position.z + UnityEngine.Random.Range(-maxRange, maxRange);

                scoutPosition = position;

                scoutTimer = Time.time + scoutTime;
            } else {
                MoveTowardsTarget(scoutPosition);
                Debug.DrawLine(transform.position, scoutPosition, Color.yellow);
            }

            if (Time.time > detectTimer) {
                newResourceObject = DetectNewResources();
                detectTimer = Time.time + detectTime;
            }
        } else {
            target = motherShip;
            Debug.DrawLine(transform.position, target.transform.position, Color.green);

            if (Vector3.Distance(transform.position, target.transform.position) < targetRadius) {
                motherShip.GetComponent<Mothership>().drones.Add(gameObject);
                motherShip.GetComponent<Mothership>().scouts.Remove(gameObject);

                motherShip.GetComponent<Mothership>().resourceObjects.Add(newResourceObject);
                newResourceVal = 0;
                newResourceObject = null;

                droneBehaviour = DroneBehaviours.Idle;
            }
        }
    }

    private void Attacking() {
        tarVel = (target.transform.position - tarPrevPos) / Time.deltaTime;
        tarPrevPos = target.transform.position;

        attackPos = target.transform.position + distanceRatio * Vector3.Distance(transform.position, target.transform.position) * tarVel;

        attackPos.y = attackPos.y + 10;
        Debug.DrawLine(transform.position, attackPos, Color.red);

        if (Vector3.Distance(transform.position, attackPos) > targetRadius)
            MoveTowardsTarget(attackPos);
        else {
            targetRotation = Quaternion.LookRotation(target.transform.position - transform.position);
            adjRotSpeed = Mathf.Min(rotationSpeed * Time.deltaTime, 1);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);

            if (Time.time > fireTimer) {
                Instantiate(laser, transform.position + transform.forward * 0.5f, transform.rotation);
                fireTimer = Time.time + fireTime;
            }
        }
    }

    private void Fleeing() {
        fleePos = transform.position - distanceRatio * Vector3.Distance(transform.position, target.transform.position) * (transform.forward * speed);

        fleePos.y = fleePos.y + 10;
        Debug.DrawLine(transform.position, fleePos, Color.red);

        if (Vector3.Distance(transform.position, target.transform.position) < targetRadius)
            MoveTowardsTarget(fleePos);
        else {
            if (Vector3.Distance(transform.position, motherShip.transform.position) < targetRadius)
                droneBehaviour = DroneBehaviours.Idle;
            else
                MoveTowardsTarget(motherShip.transform.position);
        }
    }

    private GameObject DetectNewResources() {
        for (int i = 0; i < gameManager.asteroids.Length; i++) {
            if (Vector3.Distance(transform.position, gameManager.asteroids[i].transform.position)
                <= detectionRadius) {
                if (gameManager.asteroids[i].GetComponent<Asteroid>().resource > newResourceVal) {
                    newResourceObject = gameManager.asteroids[i];
                }
            }
        }

        if (motherShip.GetComponent<Mothership>().resourceObjects.Contains(newResourceObject)) {
            return null;
        } else {
            return newResourceObject;
        }
    }

    private void MoveTowardsTarget(Vector3 targetPos) {
        //Rotate and move towards target if out of range
        if (Vector3.Distance(targetPos, transform.position) > targetRadius) {

            //Lerp Towards target
            targetRotation = Quaternion.LookRotation(targetPos - transform.position);
            adjRotSpeed = Mathf.Min(rotationSpeed * Time.deltaTime, 1);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);

            rb.AddRelativeForce(Vector3.forward * speed * 20 * Time.deltaTime);
        }
    }

    private void BoidBehaviour() {
        boidIndex++;

        if (boidIndex >= gameManager.enemyList.Length) {
            Vector3 cohesiveForce = (cohesionStrength / Vector3.Distance(cohesionPos, transform.position)) * (cohesionPos -
                transform.position);

            rb.AddForce(cohesiveForce);
            boidIndex = 0;
            cohesionPos.Set(0, 0, 0);
        }

        Vector3 pos = gameManager.enemyList[boidIndex].transform.position;
        Quaternion rot = gameManager.enemyList[boidIndex].transform.rotation;
        float dist = Vector3.Distance(transform.position, pos);

        if (dist > 0) {
            if (dist <= separationDistance) {
                float scale = separationStrength / dist;
                rb.AddForce(scale * Vector3.Normalize(transform.position - pos));
            } else if (dist < cohesionDistance && dist > separationDistance) {
                cohesionPos = cohesionPos + pos * (1f / (float)gameManager.enemyList.Length);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 1);
            }
        }
    }
}
