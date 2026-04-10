using Managers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 현재 존재하는 사운드 트랙을 보여줍니다.
/// </summary>
public class SoundPlayer : MonoBehaviour
{
    //public SoundTrack soundTrack;
    //public Transform content;

    ////현재 사운드 트랙
    //public List<SoundTrack> soundTracks = new List<SoundTrack>();
    //AudioSource source;

    //public bool Active { get; set; }

    //public bool Off()
    //{
    //    gameObject.SetActive(false);

    //    Active = false;

    //    return Active;
    //}

    //public bool On()
    //{
    //    gameObject.SetActive(true);

    //    Init();

    //    time = 0;
    //    Active = true;
    //    return Active;
    //}

    //float time;
    //private void Update()
    //{
    //    //0.1초마다 사운드 트랙의 재생목록을 업데이트 합니다.
    //    time -= Time.deltaTime;
    //    if(time < 0)
    //    {
    //        time = 0.1f;

    //        UpdateTrack();
    //    }
    //}

    //void UpdateTrack()
    //{
    //    if (source == null) return;
    //    if (source.clip == null) return;
    //    if (!source.isPlaying) return;


    //    //현재 재생 중인 사운드 이름이랑 일치한다면 표시합니다.
    //    for (int i = 0; i < soundTracks.Count; i++)
    //    {
    //        if (soundTracks[i].text_Name.text == source.clip.name)
    //        {
    //            soundTracks[i].Select(true);
    //        }
    //        else
    //        {
    //            soundTracks[i].Select(false);
    //        }
    //    }
    //}

    ////활성화와 동시에 기존에 추가되지 않은 사운드를 새로 추가합니다.
    //void Init()
    //{
    //    source = SoundManager.Instance.GetBgmSource();

    //    //사운드 매니저에 등록된 음악
    //    string sounds = null;

    //    if (sounds == null) return;

    //    //현재 생성된 사운드 이름
    //    var existNames = soundTracks.Select(x => x.text_Name.text)?.ToArray();

    //    //모든 사운드 선택 초기화
    //    foreach(var track in soundTracks)
    //    {
    //        track.Select(false);
    //    }


    //    //없는 트랙
    //    foreach (var sound in sounds)
    //    {
    //        //생성된 트랙에 사운드가 존재하면 다음 사운드
    //        if(existNames != null)
    //        {
    //            if (existNames.Contains(sound.name)) continue;
    //        }

    //        SoundTrack track = Instantiate(soundTrack, content);
    //        track.Init(sound);
    //        soundTracks.Add(track);
    //    }
    //}
}
