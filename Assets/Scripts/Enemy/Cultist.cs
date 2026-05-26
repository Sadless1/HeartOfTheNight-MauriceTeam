using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Cultist : MonoBehaviour, IDamageable
    {
        private enum State { IdleAim, Retreat }

        [Header("Data")]
        [SerializeField] private CultistStatsSO stats;

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private CultistBullet bulletPrefab;

        [Header("Layers")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Health")]
        [SerializeField] private int maxHealth = 30;

        private Rigidbody2D rb;
        private SpriteRenderer sprite;
        private State current = State.IdleAim;
        private float fireTimer;
        private int   health;
        private int   facing = 1;

        private void Awake()
        {
            rb     = GetComponent<Rigidbody2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            health = maxHealth;

            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }
        }

        private void Update()
        {
            if (player == null || stats == null) return;

            float dx       = player.position.x - transform.position.x;
            float distance = Mathf.Abs(dx);
            facing         = dx >= 0 ? 1 : -1;
            FaceTarget();

            DecideState(distance);

            switch (current)
            {
                case State.IdleAim: TickIdleAim(distance); break;
                case State.Retreat: TickRetreat();         break;
            }
        }

        private void FixedUpdate()
        {
            if (stats == null) return;

            if (current == State.Retreat) ApplyRetreatVelocity();
            else                          Decelerate();
        }

        private void DecideState(float distance)
        {
            if (current == State.IdleAim && distance < stats.minSafeDistance)
                current = State.Retreat;
            else if (current == State.Retreat &&
                     distance > stats.minSafeDistance + stats.hysteresis)
                current = State.IdleAim;
        }

        private void TickIdleAim(float distance)
        {
            fireTimer -= Time.deltaTime;
            if (distance <= stats.detectRange && fireTimer <= 0f)
            {
                Fire();
                fireTimer = stats.fireCooldown;
            }
        }

        private void TickRetreat()
        {
            fireTimer = stats.fireCooldown;
        }

        private void ApplyRetreatVelocity()
        {
            int retreatDir = -facing;

            if (!HasGroundAhead(retreatDir) || IsWallAhead(retreatDir))
            {
                Decelerate();
                return;
            }

            float target = retreatDir * stats.moveSpeed;
            float newX   = Mathf.MoveTowards(rb.linearVelocity.x, target,
                                             stats.groundAccel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        private void Decelerate()
        {
            float newX = Mathf.MoveTowards(rb.linearVelocity.x, 0f,
                                           stats.groundAccel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        private bool HasGroundAhead(int dir)
        {
            Vector2 origin = (Vector2)transform.position
                           + new Vector2(stats.edgeCheckOffset.x * dir,
                                         stats.edgeCheckOffset.y - 0.5f);
            return Physics2D.OverlapBox(origin, stats.groundCheckSize, 0f, groundLayer);
        }

        private bool IsWallAhead(int dir)
        {
            Vector2 origin = (Vector2)transform.position
                           + new Vector2(stats.edgeCheckOffset.x * dir, 0f);
            return Physics2D.OverlapBox(origin, stats.edgeCheckSize, 0f, groundLayer);
        }

        private void FaceTarget()
        {
            if (sprite != null) sprite.flipX = facing < 0;
        }

        private void Fire()
        {
            if (bulletPrefab == null || firePoint == null || player == null) return;

            Vector2 dir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
            var bullet  = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            bullet.Launch(dir, stats.bulletSpeed, stats.bulletDamage, stats.bulletLifetime);
        }

        public void TakeDamage(int amount)
        {
            health -= amount;
            if (health <= 0) Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.detectRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stats.minSafeDistance);

            int dir = facing == 0 ? 1 : facing;
            Gizmos.color = Color.cyan;
            Vector3 edge = transform.position
                + new Vector3(stats.edgeCheckOffset.x * -dir,
                              stats.edgeCheckOffset.y - 0.5f, 0f);
            Gizmos.DrawWireCube(edge, stats.groundCheckSize);
        }
    }
}
