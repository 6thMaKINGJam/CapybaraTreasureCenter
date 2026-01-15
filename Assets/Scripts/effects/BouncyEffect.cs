using UnityEngine;
using DG.Tweening;

public class BouncyEffect : MonoBehaviour
{
    [SerializeField] private AnimationCurve scaleCurve;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private bool autoStart = true;

    private Tweener bounceTween;
    private Tweener restoreTween;
    private Vector3 originalScale;
    private bool originalScaleCaptured;

    private void Awake()
    {
        CaptureOriginalScale();
        SetupDefaultCurve();
    }

    private void OnEnable()
    {
        // 켜질 때마다 혹시 스케일이 깨져있으면 원복할 기회
        CaptureOriginalScale();

        if (autoStart) StartBounce();
    }

    private void OnDisable()
    {
        // 비활성화될 때는 "트윈 생성"하지 말고 그냥 정리 + 즉시 복구가 안전
        DOTween.Kill(transform);
        transform.localScale = originalScale;
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }

    private void CaptureOriginalScale()
    {
        if (originalScaleCaptured) return;

        originalScale = transform.localScale;
        // 혹시 0으로 잡혔으면 안전장치
        if (originalScale == Vector3.zero)
            originalScale = Vector3.one;

        originalScaleCaptured = true;
    }

    public void StartBounce()
    {
        // 이 Transform에 걸린 스케일 트윈 싹 정리
        DOTween.Kill(transform);

        transform.localScale = originalScale;

        bounceTween = DOTween.To(
            () => 0f,
            progress =>
            {
                float m = scaleCurve.Evaluate(progress);
                transform.localScale = originalScale * m;
            },
            1f,
            duration
        )
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart)
        .SetAutoKill(false);
    }

    public void StopBounce()
    {
        // 트윈 정리 후 즉시 복구(=확실하게 보이게)
        DOTween.Kill(transform);
        transform.localScale = originalScale;
    }

    private void SetupDefaultCurve() { /* 네 코드 그대로 */ }
}
