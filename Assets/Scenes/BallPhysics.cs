using UnityEngine;
using System.Collections.Generic;

public class BallPhysics : MonoBehaviour
{
    [Header("Physics Settings")]
    public float radius = 0.5f;
    public float mass = 1f;
    public Vector3 initialVelocity = Vector3.zero;
    public bool useGravity = false;
    public float gravity = -9.81f;
    public float worldBoundary = 10f;
    public float collisionThreshold = -0.001f;
    public float e = 1.0f;

    [Header("Debug")]
    public bool showDebugInfo = false;
    public Color debugColor = Color.cyan;

    private Vector3 velocity;
    private Vector3 position;
    private static List<BallPhysics> allBalls = new List<BallPhysics>();

    private void HandleBallCollisions()
    {
        for (int i = 0; i < allBalls.Count; i++)
        {
            BallPhysics other = allBalls[i];

            if (other == this) continue; // ���������� ����

            Vector3 delta = other.position - position; //������ �� ���� � ������� ����
            float distance = delta.magnitude;          //��������� ����� ������
            float minDistance = radius + other.radius; //����������� ��������� ��� ������������

            if (distance < minDistance) //���� ��������� ������ ����������� ��� ������������
            {
                Vector3 normal = delta.normalized; //������� �� ���� �� ������� ����
                Vector3 relativeVelocity = velocity - other.velocity; //�������� ������������ ������� ����
                float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal); //��������� ��������� �� �������,
                                                                                   //�������� ���������������� �������� � �������

                if (velocityAlongNormal < collisionThreshold) //������������� ����� �������� ��������������� ��������������
                                                               //�������� � ������� ������� �� ������� ����, �������� �� ����
                    continue;                                  //����������� �� ������������  

                // ���������� ������������ ������� ��������������� ����� (��������� ������� ������������ � = 1)
                float j = -(e+1) * velocityAlongNormal / (1 / mass + 1 / other.mass);
                Vector3 impulse = j * normal;

                // ���������� �������
                velocity += impulse / mass;
                other.velocity -= impulse / other.mass;

                // ��������� ��������� ��� �������������� �����������
                float penetration = minDistance - distance;
                Vector3 correction = penetration * normal * 0.5f;
                position -= correction;
                other.position += correction;

                // ���������� �������
                transform.position = position;
                other.transform.position = other.position;
            }
        }
    }

    void Start()
    {
        position = transform.position;
        velocity = new Vector3(Random.Range(-35,35), Random.Range(-35, 35), Random.Range(-35, 35));
        mass = Random.Range(1, 20);
        radius = Mathf.Pow(  (3.0f*mass)/(4.0f*3.14f),1.0f/3.0f);
        gameObject.transform.localScale = 2.0f*Vector3.one * radius;
    }

    void OnEnable()
    {
        allBalls.Add(this);
    }

    void OnDisable()
    {
        allBalls.Remove(this);
    }

    void Update()
    {
        // Apply gravity
        if (useGravity)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // Update position
        position += velocity * Time.deltaTime;
        transform.position = position;

        // Handle collisions
        HandleWallCollisions();
        HandleBallCollisions();
    }

    private void HandleWallCollisions()
    {
        for (int i = 0; i < 3; i++) // Check all 3 axes (x, y, z)
        {
            if (position[i] - radius < -worldBoundary)
            {
                position[i] = -worldBoundary + radius;
                velocity[i] = -e*velocity[i];
            }
            else if (position[i] + radius > worldBoundary)
            {
                position[i] = worldBoundary - radius;
                velocity[i] = -e*velocity[i];
            }
        }
    }

    void OnDrawGizmos()
    {
        if (showDebugInfo)
        {
            Gizmos.color = debugColor;
            Gizmos.DrawWireSphere(transform.position, radius);

            // Draw velocity vector
            //Gizmos.color = Color.red;
            //Gizmos.DrawLine(transform.position, transform.position + velocity);
        }
    }
}