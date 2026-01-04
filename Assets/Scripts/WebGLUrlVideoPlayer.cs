using UnityEngine;
using UnityEngine.Video;
using System.IO;

public class WebGLUrlVideoPlayer : MonoBehaviour
{
    [SerializeField] private VideoPlayer vp;

    [Header("StreamingAssets file name (e.g. bg.mp4)")]
    [SerializeField] private string fileName = "bg.mp4";

    [Header("WebGL autoplay is often blocked. Turn this off and call PlayFromUserGesture() on click.")]
    [SerializeField] private bool tryAutoPlay = false;

    void Awake()
    {
        if (!vp) vp = GetComponent<VideoPlayer>();

        vp.source = VideoSource.Url;
        vp.isLooping = true;

        // 배경이면 오디오 끄는 게 가장 안정적 (브라우저 자동재생 정책 영향 줄어듦)
        vp.audioOutputMode = VideoAudioOutputMode.None;

        // URL 구성 (WebGL에선 StreamingAssets가 웹 경로로 서빙됨)
        vp.url = Path.Combine(Application.streamingAssetsPath, fileName);

        vp.errorReceived += (_, msg) => Debug.LogError($"[VideoPlayer] {msg}");
        vp.prepareCompleted += _ => Debug.Log($"[VideoPlayer] Prepared. url={vp.url}");
    }

    void Start()
    {
        // 자동재생은 브라우저에서 막힐 수 있음
        if (tryAutoPlay)
        {
            vp.Prepare();
            vp.Play();
        }
        else
        {
            // 준비만 해두고, 첫 클릭에서 Play하는 걸 권장
            vp.Prepare();
        }
    }

    // UI 버튼 OnClick이나, 아무 클릭/터치 이벤트에서 이걸 호출
    public void PlayFromUserGesture()
    {
        if (!vp.isPrepared) vp.Prepare();
        vp.Play();
    }
}
