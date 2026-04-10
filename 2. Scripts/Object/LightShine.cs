using UnityEngine;

[ExecuteInEditMode]
public class LightShine : MonoBehaviour
{
    [Range(0.0f, 0.5f)]
    public float intensityMin = 0.05f;
    [Range(0.05f, 1.0f)]
    public float intensityMax = 0.1f;
    [SerializeField]
    Color color = Color.white;
    [SerializeField]
    float diff = 0.005f;

    float _intensity = 1.0f;
    SpriteRenderer lightImage;

    // Start is called before the first frame update
    void Start()
    {
        lightImage = GetComponent<SpriteRenderer>();
        _intensity = intensityMin + ((intensityMax - intensityMin) * 0.5f);
        color.a = _intensity;
    }

    // Update is called once per frame
    void Update()
    {
        _intensity = Mathf.Clamp(_intensity + Random.Range(-diff, diff), intensityMin, intensityMax);
        color.a = _intensity;
        lightImage.color = color;
    }
}
