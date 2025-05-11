//using System;
using System.Collections;
using UnityEngine;

public class ProtoplanetaryDiskSimulator : MonoBehaviour
{
    [Header("Simulation Parameters")]
    public int particleCount = 1000;
    public float diskRadius = 20f;
    public float thickness = 2f;
    public float centralMass = 10000f;
    public float minMass = 0.1f;
    public float maxMass = 2f;
    public float gravitationalConstant = 0.06674f;
    public float softeningFactor = 0.5f;
    public float maxParticleSpeed = 20f;

    [Header("Rendering")]
    public ComputeShader gravityComputeShader;
    public Material particleMaterial;
    public float particleSize = 0.2f;
    public GameObject starPrefab;

    private ComputeBuffer particleBuffer;
    private Particle[] particles;
    private int kernelHandle;
    private uint threadGroupSize;
    private int groups;
    private GameObject centralStar;

    [Header("Debug Visualization")]
    public bool showTrajectories = true;
    public Color trajectoryColor = new Color(0, 1, 1, 0.3f);
    public int trajectoriesToShow = 100;

    [Header("Star Corona Settings")]
    public bool enableCorona = true;
    public float coronaDensity = 1f;
    public float starRadius = 2f;
    private ParticleSystem starCorona;

    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
    }

    void Start()
    {
        if (gravityComputeShader == null)
        {
            Debug.LogError("Compute Shader не назначен!");
            return;
        }
        CreateCentralStar();
        InitializeParticles();
        InitializeComputeShader();

    }

    //[System.Obsolete]
    void CreateCentralStar()
    {

        if (starPrefab != null)
        {
            centralStar = Instantiate(starPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            centralStar = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            centralStar.transform.position = Vector3.zero;
            centralStar.transform.localScale = Vector3.one * 3f;

            Renderer r = centralStar.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = Color.yellow;
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", Color.yellow * 2f);

            Light light = centralStar.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4f;
            light.intensity = 10f;
            light.renderMode = LightRenderMode.ForceVertex;
            //.lightmappingMode = LightmappingMode.Mixed;
            object halo = light.GetComponent("Halo");
            halo.GetType().GetProperty("enabled");
        }
        if (enableCorona)
        {
            starCorona = CreateCoronaEffect(centralStar, starRadius);
            EnhanceCoronaEffect(starCorona, starRadius);

            // Опционально: добавить вспышки
            StartCoroutine(CoronalFlashes());
        }
    }
    IEnumerator CoronalFlashes()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2f, 5f));

            if (starCorona != null)
            {
                var emission = starCorona.emission;
                float originalRate = emission.rateOverTime.constant;

                // Имитация вспышки
                emission.rateOverTime = originalRate * 5f;
                yield return new WaitForSeconds(0.3f);
                emission.rateOverTime = originalRate;
            }
        }
    }

    //[System.Obsolete]
    ParticleSystem CreateCoronaEffect(GameObject star, float starRadius)
    {
        // Создаем объект для системы частиц
        GameObject coronaObj = new GameObject("StarCorona");
        coronaObj.transform.SetParent(star.transform);
        coronaObj.transform.localPosition = Vector3.zero;

        // Добавляем и настраиваем ParticleSystem
        ParticleSystem corona = coronaObj.AddComponent<ParticleSystem>();
        var main = corona.main;
        main.loop = true;
        //main.startLifetime = 5f;
       // main.startSpeed = 0.5f;
       // main.startSize = starRadius * 0.7f;
        main.startColor = Color.yellow;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Настройка формы излучения
        var shape = corona.shape;
       // shape.shapeType = ParticleSystemShapeType.Sphere;
      //  shape.radius = starRadius * 0.8f;
        //shape.randomDirection = true;




        main.startSize = 3f;
        main.startSpeed = 0.5f;
        main.startLifetime = 4f;
        main.maxParticles = 1000;

        var emission = corona.emission;
        emission.rateOverTime = 100f;


        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 4f;






        coronaObj.SetActive(true);
        main.playOnAwake = true; // Важно!

        return corona;
    }
    void EnhanceCoronaEffect(ParticleSystem corona, float starRadius)
    {
        // Основные параметры
        var main = corona.main;
        main.startSize = new ParticleSystem.MinMaxCurve(
            starRadius * 0.1f,  // Минимальный размер
            starRadius * 0.5f   // Максимальный размер
        );
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            0.1f,
            starRadius * 0.5f
        );

        // Эмиссия
        var emission = corona.emission;
        emission.rateOverTime = 50f;
        emission.rateOverTimeMultiplier = 2f;

        // Вращение и скорость
        var velocity = corona.velocityOverLifetime;
        velocity.enabled = true;
        velocity.radial = new ParticleSystem.MinMaxCurve(-0.1f, -0.5f);

        // Цветовой градиент
        var color = corona.colorOverLifetime;
        color.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
            new GradientColorKey(new Color(1f, 0.8f, 0.4f), 0f),
            new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f)
            },
            new GradientAlphaKey[] {
            new GradientAlphaKey(0.8f, 0f),
            new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;

        // Размер частиц со временем
        var size = corona.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, 0f)
            )
        );

        ParticleSystemRenderer renderer = corona.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
    }
    void InitializeParticles()
    {
        particles = new Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            float angle = Random.Range(0, 2 * Mathf.PI);
            float radius = Random.Range(0, diskRadius);
            float mass = Random.Range(minMass, maxMass);

            // Распределение в 3D диске
            Vector3 pos = new Vector3(
                radius * Mathf.Cos(angle),
                Random.Range(-thickness/2, thickness/2),
                radius * Mathf.Sin(angle)
            );

            // Кеплеровская орбитальная скорость с небольшим случайным отклонением
            float orbitalSpeed = Mathf.Sqrt(gravitationalConstant * centralMass / radius) * 
                               Random.Range(0.95f, 1.05f);
            Vector3 vel = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle)) * orbitalSpeed;

            particles[i] = new Particle
            {
                position = pos,
                velocity = vel,
                mass = mass
            };
        }
    }

    void InitializeComputeShader()
    {
        kernelHandle = gravityComputeShader.FindKernel("GravityStep");
        gravityComputeShader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSize, out _, out _);
        groups = Mathf.CeilToInt(particleCount / (float)threadGroupSize);

        particleBuffer = new ComputeBuffer(particleCount, sizeof(float) * 7);
        particleBuffer.SetData(particles);

        gravityComputeShader.SetBuffer(kernelHandle, "particles", particleBuffer);
    }

    void Update()
    {
        if (!enabled) return;

        // Обновляем параметры в Compute Shader
        gravityComputeShader.SetFloat("deltaTime", Time.deltaTime);
        gravityComputeShader.SetFloat("gravitationalConstant", gravitationalConstant);
        gravityComputeShader.SetFloat("softeningFactor", softeningFactor);
        gravityComputeShader.SetVector("starPosition", Vector3.zero);
        gravityComputeShader.SetFloat("starMass", centralMass);
        gravityComputeShader.SetFloat("maxParticleSpeed", maxParticleSpeed);

        // Запускаем вычисления
        gravityComputeShader.Dispatch(kernelHandle, groups, 1, 1);

        // Рендеринг
        RenderParticles();
    }

    void RenderParticles()
    {
        if (particleMaterial == null || !particleMaterial.enableInstancing)
            return;

        Mesh particleMesh = CreateParticleMesh();
        Matrix4x4[] matrices = new Matrix4x4[particleCount];
        MaterialPropertyBlock props = new MaterialPropertyBlock();

        particleBuffer.GetData(particles);

        for (int i = 0; i < particleCount; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                particles[i].position,
                Quaternion.identity,
                Vector3.one * particleSize * Mathf.Pow(particles[i].mass, 0.33f)
            );
        }

        Graphics.DrawMeshInstanced(
            particleMesh,
            0,
            particleMaterial,
            matrices,
            particleCount,
            props,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false
        );
    }

    Mesh CreateParticleMesh()
    {
        return GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().sharedMesh;
    }

    void OnDestroy()
    {
        if (particleBuffer != null)
            particleBuffer.Release();
    }

    void OnDrawGizmos()
    {
        if (!showTrajectories || particles == null || particles.Length == 0) return;

        Gizmos.color = trajectoryColor;
        int step = Mathf.Max(1, particles.Length / trajectoriesToShow);

        for (int i = 0; i < particles.Length; i += step)
        {
            Vector3 nextPos = particles[i].position + particles[i].velocity * Time.deltaTime * 10f;
            Gizmos.DrawLine(particles[i].position, nextPos);
        }
    }

    void OnDrawGizmosSelected()
    {
        //Gizmos.color = new Color(1, 1, 0, 0.1f);
        //Gizmos.DrawWireSphere(Vector3.zero, diskRadius);
    }
}