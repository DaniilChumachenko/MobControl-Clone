using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyCastleScript : MonoBehaviour
{
    [SerializeField] ParticleSystem castleParticular;
    [SerializeField] TextMeshProUGUI health_text;
    [SerializeField] int health = 100;

    [SerializeField] Transform spawnPoint;
    [SerializeField] GameObject enemy;
    [SerializeField] GameObject bigEnemy;

    [Header("Spawn:Time-EnemyAmount-BigEnemyAmount")]
    [SerializeField] Vector3[] spawnEvents;
    [SerializeField, Min(0.05f)] private float timeBetweenEnemySpawns = 0.4f;
    private float spawnTimer;
    private readonly Queue<GameObject> pendingEnemies = new Queue<GameObject>();
    private Coroutine spawnCoroutine;




    void Start()
    {
        spawnTimer = 0;

    }
    void Update()
    {
        if (health <= 0) { Destroy(gameObject); }
        spawnTimer += Time.deltaTime;

        for (int i = 0; i < spawnEvents.Length; i++)
        {
            if (spawnEvents[i].x == (int)spawnTimer)
            {
                QueueSpawn((int)spawnEvents[i].y, (int)spawnEvents[i].z);
                spawnEvents[i].x = 0;
            }
        }
        health_text.text = health.ToString();
    }
    private void QueueSpawn(int enemyAmount, int bigEnemyAmount)
    {
        for (int i = 0; i < enemyAmount; i++)
        {
            pendingEnemies.Enqueue(enemy);
        }
        for (int i = 0; i < bigEnemyAmount; i++)
        {
            pendingEnemies.Enqueue(bigEnemy);
        }

        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnEnemiesOneByOne());
        }
    }

    private IEnumerator SpawnEnemiesOneByOne()
    {
        while (pendingEnemies.Count > 0)
        {
            SpawnEnemy(pendingEnemies.Dequeue());
            yield return new WaitForSeconds(timeBetweenEnemySpawns);
        }

        spawnCoroutine = null;
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        Vector3 newSpawnPoint = spawnPoint.position;
        newSpawnPoint.x += Random.Range(-2, 2);
        newSpawnPoint.z += Random.Range(-3, 3);

        GameObject enemySpawned = Instantiate(enemyPrefab, newSpawnPoint, Quaternion.identity);
        enemySpawned.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }
    public void getHit(int damage)
    {
        health -= damage;
        CastleHitEffect();
    }

    private void CastleHitEffect()
    {
        castleParticular.Play();
    }
}
