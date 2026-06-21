using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyController : MonoBehaviour
{
    private static readonly List<EnemyController> activeEnemies = new List<EnemyController>();

    [SerializeField] private ParticleSystem enemyParticular;
    private Collider unitCollider;


    [Header("Health Controller")]

    [SerializeField] int maxHealth;
    private int health;

    [SerializeField] int damage;

    [SerializeField] float fireCd;
    private float fireTimer;

    [Header("Settings")]
    [SerializeField] private float moveSpeed;
    private Transform target;

    [SerializeField] bool isBig;
    private Vector3 startScale;

    private void Awake()
    {
        unitCollider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        if (!activeEnemies.Contains(this))
        {
            activeEnemies.Add(this);
        }
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
    }

    public static bool TryGetClosestWithinSurfaceDistance(
        Collider sourceCollider,
        float maxDistance,
        out Vector3 direction)
    {
        direction = Vector3.forward;
        if (sourceCollider == null)
        {
            return false;
        }

        Vector3 sourceCenter = sourceCollider.bounds.center;
        float maxDistanceSquared = maxDistance * maxDistance;
        float closestDistanceSquared = maxDistanceSquared;
        bool enemyFound = false;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = activeEnemies[i];
            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }
            if (!enemy.isActiveAndEnabled || enemy.unitCollider == null)
            {
                continue;
            }

            Vector3 enemySurface = enemy.unitCollider.ClosestPoint(sourceCenter);
            Vector3 playerSurface = sourceCollider.ClosestPoint(enemySurface);
            float surfaceDistanceSquared = (enemySurface - playerSurface).sqrMagnitude;
            if (surfaceDistanceSquared <= closestDistanceSquared)
            {
                Vector3 enemyDirection = enemy.transform.position - sourceCollider.transform.position;
                enemyDirection.y = 0f;
                if (enemyDirection.sqrMagnitude > 0.0001f)
                {
                    direction = enemyDirection.normalized;
                }
                closestDistanceSquared = surfaceDistanceSquared;
                enemyFound = true;
            }
        }

        return enemyFound;
    }

    void Start()
    {
        HistoricalUnitVisual.Attach(gameObject, HistoricalUnitVisual.Faction.Greek);
        health = maxHealth;
        target = GameObject.FindGameObjectWithTag("PlayerCastle").transform;
        target.position -= new Vector3(0, target.position.y, 0);
        startScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        fireTimer -= Time.deltaTime;
        gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        MoveForward();

        if (health <= 0)
        {

            Destroy(gameObject);
        }
    }
    private void MoveForward()
    {
        transform.position = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * moveSpeed);
    }
    public void getHit(int damage)
    {
        health -= damage;
        EnemyHitEffect();

        if (isBig)
        {
            transform.localScale = startScale * health / maxHealth;
            EnemyHitEffect();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && fireTimer <= 0)
        {
            collision.gameObject.GetComponent<PlayerController>().getHit(damage);

            fireTimer = fireCd;
        }
        if (collision.gameObject.CompareTag("PlayerCastle") && fireTimer <= 0)
        {

        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("SpeedReducePoint"))
        {

            moveSpeed = 6;
        }
        if (other.gameObject.CompareTag("EndGame"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
         

        }
    }
    private void EnemyHitEffect()
    {
        enemyParticular.Play();
    }

}
