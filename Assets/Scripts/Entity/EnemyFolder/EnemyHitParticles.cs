using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public sealed class EnemyHitParticles : MonoBehaviour
{
    private static EnemyHitParticles instance;
    private ParticleSystem sparks;
    private Material material;

    public static void Emit(Vector3 position, Vector2 direction)
    {
        if (instance == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return;

            instance = new GameObject("Enemy Hit Sparks").AddComponent<EnemyHitParticles>();
            instance.Configure(shader);
        }

        instance.Burst(position, direction);
    }

    private void Configure(Shader shader)
    {
        sparks = GetComponent<ParticleSystem>();
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = sparks.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 384;
        main.gravityModifier = 0f;

        var emission = sparks.emission;
        emission.enabled = false;
        var shape = sparks.shape;
        shape.enabled = false;

        var fade = sparks.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        material = new Material(shader);
        var particleRenderer = GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = material;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortingOrder = 5;
        sparks.Play();
    }

    private void Burst(Vector3 position, Vector2 direction)
    {
        bool hasDirection = direction.sqrMagnitude > 0f;
        float heading = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float spread = hasDirection ? 65f : 180f;

        for (int i = 0; i < 9; i++)
        {
            float angle = heading + Random.Range(-spread, spread);
            float radians = angle * Mathf.Deg2Rad;
            Vector3 travel = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            var particle = new ParticleSystem.EmitParams
            {
                position = position + travel * 0.06f,
                velocity = travel * Random.Range(2.4f, 5f),
                startLifetime = Random.Range(0.12f, 0.26f),
                startSize3D = new Vector3(Random.Range(0.1f, 0.2f), Random.Range(0.035f, 0.075f), 1f),
                rotation = angle,
                startColor = i < 3 ? new Color32(255, 244, 178, 255) : new Color32(255, 153, 48, 255)
            };
            sparks.Emit(particle, 1);
        }
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}
