using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource), typeof(Button))]
public class UIElement : MonoBehaviour
{
    public AudioClip[] clips;

    AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        GetComponent<Button>().onClick.AddListener(Click);
    }
    
    void Click()
    {
        if(clips.Length > 0)
        {
            audioSource.PlayOneShot(clips[Random.Range(0,clips.Length)]);
        }
    }
}
