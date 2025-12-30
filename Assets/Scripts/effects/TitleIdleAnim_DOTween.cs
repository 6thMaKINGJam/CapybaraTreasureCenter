using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class TitleLogoIdleAnim : MonoBehaviour
{
    [Header("Target (비우면 자기 자신)")]
    public RectTransform target;

    [Header("Breath (살짝 커졌다가 돌아오기)")]
    [Range(1.00f, 1.15f)] public float scaleUp = 1.035f;   // 가로 로고는 1.025~1.05 추천
    public float scaleUpTime = 0.60f;
    public float scaleDownTime = 0.60f;

    [Header("Tilt (아주 살짝 좌우 기울기)")]
    [Range(0f, 6f)] public float tiltAngle = 1.6f;          // 가로 로고는 1.2~2.2도 추천
    public float tiltTime = 0.55f;

    [Header("Float (선택: 아주 살짝 위아래)")]
    public bool enableFloat = true;
    public float floatY = 4f;                               // 2~6px 추천
    public float floatTime = 0.60f;

    [Header("Pauses")]
    public float pauseAfterBreath = 0.10f;
    public float pauseAfterTilt = 0.16f;

    [Header("Overall")]
    public bool playOnEnable = true;
    public bool ignoreTimeScale = true;                     // UI 타이틀은 보통 true가 좋음

    // ✅ Sequence로 선언해야 Append가 됩니다!
    Sequence _seq;

    Vector3 _baseScale;
    Vector2 _basePos;
    Quaternion _baseRot;

    void Reset()
    {
        target = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (target == null) target = GetComponent<RectTransform>();
        CacheBase();
    }

    void OnEnable()
    {
        CacheBase(); // 해상도/레이아웃 변동 대비
        if (playOnEnable) Play();
    }

    void OnDisable()
    {
        Stop();
        RestoreBase();
    }

    void CacheBase()
    {
        if (target == null) return;
        _baseScale = target.localScale;
        _baseRot = target.localRotation;
        _basePos = target.anchoredPosition;
    }

    public void Play()
    {
        Stop();

        _seq = DOTween.Sequence()
            .SetUpdate(ignoreTimeScale)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        // 1) Breath: 살짝 커짐 -> 원래
        _seq.Append(target.DOScale(_baseScale * scaleUp, scaleUpTime).SetEase(Ease.OutSine));
        _seq.Append(target.DOScale(_baseScale, scaleDownTime).SetEase(Ease.InOutSine));
        _seq.AppendInterval(pauseAfterBreath);

        // 2) Tilt: 좌우로 아주 살짝 기울 -> 복귀 (가로 로고에 잘 어울림)
        _seq.Append(target.DOLocalRotateQuaternion(Quaternion.Euler(0, 0, +tiltAngle), tiltTime * 0.5f).SetEase(Ease.OutSine));
        _seq.Append(target.DOLocalRotateQuaternion(Quaternion.Euler(0, 0, -tiltAngle), tiltTime).SetEase(Ease.InOutSine));
        _seq.Append(target.DOLocalRotateQuaternion(_baseRot, tiltTime * 0.5f).SetEase(Ease.InSine));
        _seq.AppendInterval(pauseAfterTilt);

        // 3) Breath 한 번 더 (조금 더 약하게)
        _seq.Append(target.DOScale(_baseScale * (scaleUp * 0.985f), scaleUpTime * 0.9f).SetEase(Ease.OutSine));
        _seq.Append(target.DOScale(_baseScale, scaleDownTime * 0.9f).SetEase(Ease.InOutSine));

        // (선택) 둥실둥실: 위아래 아주 미세하게 같이 주면 “타이틀 로고” 느낌 확 올라감
        if (enableFloat)
        {
            _seq.Join(target.DOAnchorPosY(_basePos.y + floatY, floatTime).SetEase(Ease.OutSine));
            _seq.Append(target.DOAnchorPosY(_basePos.y, floatTime).SetEase(Ease.InOutSine));
        }

        _seq.AppendInterval(0.20f);
        _seq.SetLoops(-1, LoopType.Restart);
    }

    public void Stop()
    {
        if (_seq != null && _seq.IsActive())
        {
            _seq.Kill();
            _seq = null;
        }
        if (target != null) target.DOKill();
    }

    void RestoreBase()
    {
        if (target == null) return;
        target.localScale = _baseScale;
        target.localRotation = _baseRot;
        target.anchoredPosition = _basePos;
    }
}
