using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TouchHintLoop : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image screenImage;
    [SerializeField] private Image handImage;
    [SerializeField] private Image buttonImage;

    [Header("Sprites")]
    [SerializeField] private Sprite spriteA;
    [SerializeField] private Sprite spriteB;

    [Header("Touch Target")]
    [SerializeField] private RectTransform buttonRect;
    [SerializeField] private Vector2 handOffsetOnButton = new Vector2(0f, 20f);

    [Header("Colors")]
    [SerializeField] private Color buttonDefaultColor = Color.white;
    [SerializeField] private Color buttonPressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Header("Timings")]
    [SerializeField] private float startDelay = 1.0f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float moveToButtonDuration = 0.25f;
    [SerializeField] private float pressDelay = 0.05f;
    [SerializeField] private float pressScale = 0.7f;
    [SerializeField] private float pressScaleDuration = 0.1f;
    [SerializeField] private float holdPressed = 0.1f;
    [SerializeField] private float releaseScaleDuration = 0.1f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float beforeResetDelay = 0.1f;

    private Sequence _seq;
    private CanvasGroup _handCg;
    private RectTransform _handRect;
    private Vector2 _handIdlePos;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (handImage == null) return;

        _handRect = handImage.rectTransform;

        _handCg = handImage.GetComponent<CanvasGroup>();
        if (_handCg == null) _handCg = handImage.gameObject.AddComponent<CanvasGroup>();

        _handIdlePos = _handRect.anchoredPosition;
        ResetState(); // 초기값 세팅만 (재생은 안 함)
        Play();
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼질 때 트윈 남는 것 방지
        Stop();
    }

    /// <summary>
    /// 외부에서 호출: 루프 시작
    /// </summary>
    public void Play(bool restartIfPlaying = true)
    {
        Debug.Log("TouchHintLoop Play called");
        if (!isActiveAndEnabled) return;

        if (IsPlaying)
        {
            Debug.Log("TouchHintLoop is already playing");
            if (!restartIfPlaying) return;
            Stop(); // 재시작이면 정지 후 다시 빌드
        }

Debug.Log("TouchHintLoop starting playback");
        ResetState();
        BuildSequence();
        _seq.Play();
        Debug.Log("TouchHintLoop sequence started");    

        IsPlaying = true;
    }

    /// <summary>
    /// 외부에서 호출: 루프 정지(즉시 정지 + 상태 리셋은 선택)
    /// </summary>
    public void Stop(bool resetVisual = false)
    {
        if (_seq != null && _seq.IsActive())
            _seq.Kill();

        _seq = null;
        IsPlaying = false;

        if (resetVisual)
            ResetState();
    }

    /// <summary>
    /// 외부에서 필요 시: 화면을 A로, 손 숨김 등 “처음 상태”로 되돌림
    /// </summary>
    public void ResetState()
    {
        if (screenImage) screenImage.sprite = spriteA;
        if (buttonImage) buttonImage.color = buttonDefaultColor;

        if (handImage)
        {
            handImage.gameObject.SetActive(false);
            if (_handCg) _handCg.alpha = 0f;
            if (_handRect) _handRect.localScale = Vector3.one;

            if (_handRect) _handRect.anchoredPosition = _handIdlePos;
        }
    }

    private void BuildSequence()
    {
        // 안전장치
        if (screenImage == null || handImage == null || buttonImage == null || buttonRect == null)
        {
            Debug.LogWarning("[TouchHintLoopController] Missing references.");
            return;
        }

        Vector2 targetPos = buttonRect.anchoredPosition + handOffsetOnButton;

        _seq = DOTween.Sequence();
        _seq.SetUpdate(true); // timeScale 무시 (unscaled time)

        _seq.SetAutoKill(false);

        _seq.AppendInterval(startDelay);

        _seq.AppendCallback(() =>
        {
            handImage.gameObject.SetActive(true);
            _handCg.alpha = 0f;
            _handRect.localScale = Vector3.one;
            _handRect.anchoredPosition = _handIdlePos;
        });

        _seq.Append(_handCg.DOFade(1f, fadeInDuration));
        _seq.Append(_handRect.DOAnchorPos(targetPos, moveToButtonDuration).SetEase(Ease.OutQuad));
        _seq.AppendInterval(pressDelay);

        _seq.AppendCallback(() =>
        {
            buttonImage.color = buttonPressedColor;
            screenImage.sprite = spriteB;
        });

        _seq.Append(_handRect.DOScale(pressScale, pressScaleDuration).SetEase(Ease.OutQuad));
        _seq.AppendInterval(holdPressed);
        _seq.Append(_handRect.DOScale(1f, releaseScaleDuration).SetEase(Ease.OutQuad));

        _seq.Append(_handCg.DOFade(0f, fadeOutDuration));
        _seq.AppendCallback(() =>
        {
            handImage.gameObject.SetActive(false);
            buttonImage.color = buttonDefaultColor; // 화면은 B 유지
        });

        _seq.AppendInterval(beforeResetDelay);
        _seq.AppendCallback(() => { screenImage.sprite = spriteA; });

        _seq.SetLoops(-1, LoopType.Restart);
    }

}
