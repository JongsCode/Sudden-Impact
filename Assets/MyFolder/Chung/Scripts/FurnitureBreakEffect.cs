using System.Collections;
using UnityEngine;

public class FurnitureBreakEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem ps;

    private void Awake()
    {
        if (ps == null) ps = GetComponent<ParticleSystem>();
    }

    public void Play(Vector3 boxSize, Color color, Vector3 hitNormal)
    {
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;

        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color * 1.2f, color * 0.5f);

        transform.rotation = Quaternion.LookRotation(-hitNormal);

        ps.Play();

        float lifetime = main.duration + main.startLifetime.constantMax;
        StartCoroutine(ReturnRoutine(lifetime));
    }

    private IEnumerator ReturnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        FurnitureBreakEffectManager.Instance.Return(this);
    }
}
