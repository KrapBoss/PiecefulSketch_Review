using Managers;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사운드를 보여줄 UI의 구현
/// </summary>
public class SoundTrack : MonoBehaviour
{
    public TMP_Text text_Name;
    public TMP_Text text_Time;

    public Button button_track;
    Image myImage;

    public bool isPlaying = false;

    private void Awake()
    {
        button_track.onClick.AddListener(SoundPlay);
        myImage = button_track.GetComponent<Image>();
    }

    public void Init(AudioClip clip)
    {
        Select(false);
        text_Name.text = clip.name;
        text_Time.text = clip.length.ToString();

    }

    //버튼을 선택하면 해당 사운드가 재생이 됩니다.
    void SoundPlay()
    {
        if (isPlaying)
        {
            SoundManager.Instance.StopBGM();
        }
        else
        {
            SoundManager.Instance.PlayBGM(text_Name.text);
        }
    }

    public void Select(bool b)
    {
        if(b)myImage.color =  new Color(1.0f, 0.847f, 0.973f);
        else { myImage.color = Color.white; }

        isPlaying = b;
    }
}
