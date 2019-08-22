using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mothership : MonoBehaviour {
    public GameObject enemy;
    public int numberOfEnemies = 20;

    public List<GameObject> drones = new List<GameObject>();
    public List<GameObject> scouts = new List<GameObject>();
    public List<GameObject> foragers = new List<GameObject>();
    public List<GameObject> eliteForagers = new List<GameObject>();

    public List<GameObject> resourceObjects = new List<GameObject>();

    public int maxScouts = 4;
    public int maxForagers = 3;
    public int maxEliteForagers = 2;

    public float harvested = 0;
    public int required = 300;

    public GameObject spawnLocation;

    // initialise the boids
    void Start() {

        for (int i = 0; i < numberOfEnemies; i++) {

            Vector3 spawnPosition = spawnLocation.transform.position;

            spawnPosition.x = spawnPosition.x + Random.Range(-50, 50);
            spawnPosition.y = spawnPosition.y + Random.Range(-50, 50);
            spawnPosition.z = spawnPosition.z + Random.Range(-50, 50);

            GameObject thisEnemy = Instantiate(enemy, spawnPosition, spawnLocation.transform.rotation) as GameObject;
            drones.Add(thisEnemy);
        }
    }

    // Update is called once per frame
    void Update() {
        if (harvested >= required) {
            for (int i = 0; i < scouts.Count; i++) {
                drones.Add(scouts[i]);
            }
            scouts.Clear();

            for (int i = 0; i < foragers.Count; i++) {
                drones.Add(foragers[i]);
            }
            foragers.Clear();

            for (int i = 0; i < eliteForagers.Count; i++) {
                drones.Add(eliteForagers[i]);
            }
            eliteForagers.Clear();

            if (drones.Count > 0)
                foreach (GameObject drone in drones) {
                    drone.GetComponent<Drone>().droneBehaviour = Drone.DroneBehaviours.Return;
                } else
                Destroy(gameObject);
        } else {
            if (scouts.Count < maxScouts)
                for (int i = scouts.Count; i < maxScouts; i++) {
                    scouts.Add(drones[0]);
                    drones.RemoveAt(0);

                    GameObject best = scouts[i];
                    for (int j = 0; j < drones.Count; j++) {
                        if (drones[j].GetComponent<Drone>().capacity < best.GetComponent<Drone>().capacity && drones[j].GetComponent<Drone>().maxRange >= 800)
                            best = drones[j];
                    }

                    if (best != scouts[i]) {
                        drones.Add(scouts[i]);
                        scouts.RemoveAt(i);
                        scouts.Insert(i, best);
                        drones.Remove(best);
                    }

                    scouts[i].GetComponent<Drone>().droneBehaviour = Drone.DroneBehaviours.Scouting;
                }

            if (resourceObjects.Count > 0 && eliteForagers.Count < maxEliteForagers) {
                List<GameObject> best = new List<GameObject>();
                best.Add(resourceObjects[0]);
                if (resourceObjects.Count > maxEliteForagers)
                    for (int i = 1; i < maxEliteForagers; i++) {
                        best.Add(resourceObjects[i]);
                    } else {
                    for (int i = 1; i < resourceObjects.Count; i++) {
                        best.Add(resourceObjects[i]);
                    }
                }

                foreach (GameObject asteroid in resourceObjects) {
                    for (int i = 0; i < best.Count; i++)
                        if (asteroid != best[i] && asteroid.GetComponent<Asteroid>().resource > best[i].GetComponent<Asteroid>().resource && !best.Contains(asteroid))
                            best[i] = asteroid;
                }

                int count;

                if (maxEliteForagers < resourceObjects.Count)
                    count = maxEliteForagers;
                else
                    count = resourceObjects.Count;

                for (int i = eliteForagers.Count; i < count; i++) {
                    eliteForagers.Add(drones[0]);
                    drones.Remove(eliteForagers[0]);

                    GameObject bestForager = eliteForagers[i];
                    for (int j = 0; j < drones.Count; j++) {
                        if (drones[j].GetComponent<Drone>().capacity + drones[j].GetComponent<Drone>().forageRate + drones[j].GetComponent<Drone>().maxRange >
                            eliteForagers[i].GetComponent<Drone>().capacity + eliteForagers[i].GetComponent<Drone>().forageRate + eliteForagers[i].GetComponent<Drone>().maxRange) {
                            bestForager = drones[j];
                        }
                    }

                    if (bestForager != eliteForagers[i]) {
                        drones.Add(eliteForagers[i]);
                        eliteForagers.Remove(eliteForagers[i]);
                        eliteForagers.Insert(i, bestForager);
                        drones.Remove(bestForager);
                    }

                    eliteForagers[i].GetComponent<Drone>().StartForaging(true);
                }
            }

            if (resourceObjects.Count > maxEliteForagers && foragers.Count < maxForagers) {
                List<GameObject> best = new List<GameObject>();
                best.Add(resourceObjects[0]);
                if (resourceObjects.Count > maxForagers + maxEliteForagers)
                    for (int i = 1; i < maxForagers + maxEliteForagers; i++) {
                        best.Add(resourceObjects[i]);
                    } else {
                    for (int i = 1; i < resourceObjects.Count; i++) {
                        best.Add(resourceObjects[i]);
                    }
                }

                foreach (GameObject asteroid in resourceObjects) {
                    for (int i = 0; i < best.Count; i++)
                        if (asteroid != best[i] && asteroid.GetComponent<Asteroid>().resource > best[i].GetComponent<Asteroid>().resource && !best.Contains(asteroid))
                            best[i] = asteroid;
                }

                List<GameObject> tops = new List<GameObject>();
                for (int i = 0; i < maxEliteForagers; i++) {
                    tops.Add(best[i]);

                    for (int j = 0; j < best.Count; j++) {
                        if (best[i].GetComponent<Asteroid>().resource > tops[i].GetComponent<Asteroid>().resource && !tops.Contains(best[j]))
                            tops[i] = best[j];
                    }
                }

                foreach (GameObject t in tops)
                    if (best.Contains(t))
                        best.Remove(t);

                int count;

                if (maxForagers < best.Count)
                    count = maxForagers;
                else
                    count = best.Count;

                for (int i = foragers.Count; i < count; i++) {
                    foragers.Add(drones[0]);
                    drones.Remove(foragers[0]);

                    GameObject bestForager = foragers[i];
                    for (int j = 0; j < drones.Count; j++) {
                        if (drones[j].GetComponent<Drone>().capacity + drones[j].GetComponent<Drone>().forageRate + drones[j].GetComponent<Drone>().maxRange >
                            foragers[i].GetComponent<Drone>().capacity + foragers[i].GetComponent<Drone>().forageRate + foragers[i].GetComponent<Drone>().maxRange) {
                            bestForager = drones[j];
                        }
                    }

                    if (bestForager != foragers[i]) {
                        drones.Add(foragers[i]);
                        foragers.Remove(foragers[i]);
                        foragers.Insert(i, bestForager);
                        drones.Remove(bestForager);
                    }

                    foragers[i].GetComponent<Drone>().StartForaging(false);
                }
            }
        }
    }

    public GameObject FindResourceInRange(int range, GameObject drone) {
        List<GameObject> inRange = new List<GameObject>();
        foreach (GameObject asteroid in resourceObjects)
            if (Vector3.Distance(transform.position, asteroid.transform.position) < range)
                inRange.Add(asteroid);

        List<GameObject> assigned = new List<GameObject>();

        if (eliteForagers.Count > 0)
            foreach (GameObject elite in eliteForagers)
                if (elite != drone)
                    assigned.Add(elite.GetComponent<Drone>().forageTarget);

        if (foragers.Count > 0)
            foreach (GameObject forage in foragers)
                if (forage != drone)
                    assigned.Add(forage.GetComponent<Drone>().forageTarget);

        GameObject best = inRange[0];
        foreach (GameObject asteroid in inRange)
            if (asteroid.GetComponent<Asteroid>().resource > best.GetComponent<Asteroid>().resource && !assigned.Contains(asteroid))
                best = asteroid;

        return best;
    }

    public void RemoveDrone(GameObject drone) {
        if (drones.Contains(drone))
            drones.Remove(drone);
    }
}

