using UnityEngine;
using DG.Tweening;

public class BouncyEffect : MonoBehaviour
{
    [SerializeField] private AnimationCurve scaleCurve;
    [SerializeField] private float duration = 0.8f;


    private Tweener bounceTween;
    private Tweener restoreTween;
    private Vector3 originalScale;
    private bool originalScaleCaptured;

    private void Awake()
    {
        CaptureOriginalScale();
        SetupDefaultCurve();
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
    
    // ★ 안전장치 강화: 0이거나 너무 작으면 1로
    if (originalScale.magnitude < 0.01f)
    {
        originalScale = Vector3.one;
        transform.localScale = Vector3.one; // ★ 즉시 복구
    }

    originalScaleCaptured = true;
}
public void StartBounce()
{
    // 기존 트윈만 정리
    if(bounceTween != null)
    {
        bounceTween.Kill();
        bounceTween = null;
    }

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
      // 특정 트윈만 Kill
    if(bounceTween != null)
    {
        bounceTween.Kill();
        bounceTween = null;
    }
    
    transform.localScale = originalScale;
    }

    private void SetupDefaultCurve() { /* 네 코드 그대로 */ }
}
