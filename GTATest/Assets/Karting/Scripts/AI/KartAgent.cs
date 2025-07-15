using KartGame.KartSystems;
using UnityEngine;
using UnityEngine.AI;

namespace KartGame.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class KartAgent : MonoBehaviour, IInput
    {
        [Header("AI Settings")]
        public Transform playerTransform;
        [Tooltip("Включить/выключить преследование игрока (Q)")]
        public bool chasePlayer = true;
        public float detectionDistance = 30f;
        public float ramDistance = 2f;
        public LayerMask visionMask;
        public bool showRay = true;

        [Header("Patrol Settings")]
        public Transform[] checkpoints;
        public float checkpointReachDistance = 3f;

        ArcadeKart m_Kart;
        NavMeshAgent m_NavAgent;
        bool m_Acceleration;
        bool m_Brake;
        float m_Steering;
        int m_CurrentCheckpoint;

        void Awake()
        {
            m_Kart = GetComponent<ArcadeKart>();
            m_NavAgent = GetComponent<NavMeshAgent>();
            m_NavAgent.updateRotation = false;
            m_NavAgent.updateUpAxis = false;
           
            // m_NavAgent.updatePosition = false; // <--- Убрать тряску, 
            m_NavAgent.isStopped = false;
        }

        void Update()
        {
            // Переключение режима погони по клавише Q
            if (Input.GetKeyDown(KeyCode.Q))
            {
                chasePlayer = !chasePlayer;
            }

            bool isChasing = false;

            if (chasePlayer && playerTransform != null)
            {
                Vector3 directionToPlayer = playerTransform.position - transform.position;
                float distanceToPlayer = directionToPlayer.magnitude;

                // Проверяем видимость игрока (Raycast)
                bool canSeePlayer = false;
                if (distanceToPlayer < detectionDistance)
                {
                    Ray ray = new Ray(transform.position + Vector3.up, directionToPlayer.normalized);
                    if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance, visionMask))
                    {
                        if (hit.transform == playerTransform)
                            canSeePlayer = true;

                        if (showRay)
                            Debug.DrawLine(ray.origin, hit.point, Color.red);
                    }
                    else if (showRay)
                    {
                        Debug.DrawRay(ray.origin, directionToPlayer.normalized * detectionDistance, Color.yellow);
                    }
                }

                if (canSeePlayer)
                {
                    isChasing = true;
                    m_NavAgent.isStopped = false;
                    m_NavAgent.SetDestination(playerTransform.position);

                    // Если путь построен и есть хотя бы 2 точки, едем к следующей
                    if (m_NavAgent.path != null && m_NavAgent.path.corners.Length > 1)
                    {
                        Vector3 nextCorner = m_NavAgent.path.corners[1];
                        MoveToTarget(nextCorner, distanceToPlayer < ramDistance);
                    }
                    else
                    {
                        MoveToTarget(playerTransform.position, distanceToPlayer < ramDistance);
                    }
                }
            }

            if (!isChasing)
            {
                m_NavAgent.isStopped = false;

                if (checkpoints != null && checkpoints.Length > 0)
                {
                    Transform target = checkpoints[m_CurrentCheckpoint];
                    float dist = Vector3.Distance(transform.position, target.position);

                    if (dist < checkpointReachDistance)
                    {
                        m_CurrentCheckpoint = (m_CurrentCheckpoint + 1) % checkpoints.Length;
                        target = checkpoints[m_CurrentCheckpoint];
                    }

                    m_NavAgent.SetDestination(target.position);

                    if (m_NavAgent.path != null && m_NavAgent.path.corners.Length > 1)
                    {
                        Vector3 nextCorner = m_NavAgent.path.corners[1];
                        MoveToTarget(nextCorner, false);
                    }
                    else
                    {
                        MoveToTarget(target.position, false);
                    }
                }
                else
                {
                    m_Acceleration = false;
                    m_Brake = true;
                    m_Steering = 0f;
                }
            }

        }

        void MoveToTarget(Vector3 targetPos, bool brake)
        {
            Vector3 flatDirection = targetPos - transform.position;
           // flatDirection.y = 0; кажется лишним
            float distance = flatDirection.magnitude;
            if (distance > 0.1f)
                flatDirection.Normalize();
            else
                flatDirection = transform.forward;

            float angle = Vector3.SignedAngle(transform.forward, flatDirection, Vector3.up);
            m_Steering = Mathf.Clamp(angle / 45f, -1f, 1f);

            if (brake)
            {
                m_Acceleration = false;
              //  m_Brake = true; убрали дрифт
            }
            else
            {
                m_Acceleration = true;
               // m_Brake = false;
            }
        }

        public InputData GenerateInput()
        {
            return new InputData
            {
                Accelerate = m_Acceleration,
                Brake = m_Brake,
                TurnInput = m_Steering
            };
        }
    }
}