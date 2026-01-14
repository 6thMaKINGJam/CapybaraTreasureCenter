using UnityEngine;
using DG.Tweening;

/// <summary>
/// GameObject에 뽀용뽀용 크기 변화 효과를 적용하는 독립 스크립트
/// AnimationCurve를 사용한 부드러운 단일 애니메이션
/// </summary>
public class BouncyEffect : MonoBehaviour
{
    [Header("Scale Settings")]
    [Tooltip("크기 변화 곡선: 시간(0~1)에 따른 크기 배율")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);
    
    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.8f;
    
    [Header("Auto Start")]
    [SerializeField] private bool autoStart = true;

    private Tweener bounceTween;
    private Vector3 originalScale;

    private void Awake()
    {
        // 원본 크기 저장
        originalScale = transform.localScale;
        
        // 기본 커브 설정 (Inspector에서 수정 안 했을 경우)
        SetupDefaultCurve();
    }

    private void OnEnable()
    {
        if (autoStart)
        {
            StartBounce();
        }
    }

    private void OnDisable()
    {
        StopBounce();
    }

    private void OnDestroy()
    {
        bounceTween?.Kill();
    }

    /// <summary>
    /// 기본 커브 설정: 1.0 → 1.2 → 0.9 → 1.0
    /// </summary>
    private void SetupDefaultCurve()
    {
        // Inspector에서 키가 4개 미만이면 기본 커브 설정
        if (scaleCurve.keys.Length < 4)
        {
            scaleCurve = new AnimationCurve();
            
            // 4개의 키프레임 생성
            scaleCurve.AddKey(new Keyframe(0.0f, 1.0f));   // 시작: 원본 크기
            scaleCurve.AddKey(new Keyframe(0.3f, 1.2f));   // 30%: 최대 크기
            scaleCurve.AddKey(new Keyframe(0.7f, 0.9f));   // 70%: 최소 크기
            scaleCurve.AddKey(new Keyframe(1.0f, 1.0f));   // 끝: 원본 크기
            
            // 각 키프레임을 부드럽게 연결
            for (int i = 0; i < scaleCurve.keys.Length; i++)
            {
                scaleCurve.SmoothTangents(i, 0.5f);
            }
        }
    }

    /// <summary>
    /// 뽀용뽀용 애니메이션 시작
    /// </summary>
    public void StartBounce()
    {
        // 기존 애니메이션 정리
        if (bounceTween != null && bounceTween.IsActive())
        {
            bounceTween.Kill();
        }

        // 원본 크기로 리셋
        transform.localScale = originalScale;

        // 단일 애니메이션으로 부드러운 크기 변화
        bounceTween = DOTween.To(
            () => 0f,  // 시작값 (진행도 0)
            progress =>
            {
                // 커브에서 현재 진행도에 해당하는 크기 배율 가져오기
                float scaleMultiplier = scaleCurve.Evaluate(progress);
                transform.localScale = originalScale * scaleMultiplier;
            },
            1f,  // 끝값 (진행도 1)
            duration
        )
        .SetEase(Ease.Linear)  // Linear로 설정 (커브가 모든 Ease 제어)
        .SetLoops(-1, LoopType.Restart)  // 무한 반복
        .SetAutoKill(false)  // Tween 재사용 가능
        .SetUpdate(UpdateType.Normal, false);  // 에디터 정지 시 멈춤
    }

    /// <summary>
    /// 애니메이션 정지 및 원래 크기로 복구
    /// </summary>
    public void StopBounce()
    {
        if (bounceTween != null && bounceTween.IsActive())
        {
            bounceTween.Kill();
        }

        // 원본 크기로 부드럽게 복구
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 애니메이션 일시정지
    /// </summary>
    public void PauseBounce()
    {
        if (bounceTween != null && bounceTween.IsActive())
        {
            bounceTween.Pause();
        }
    }

    /// <summary>
    /// 일시정지된 애니메이션 재개
    /// </summary>
    public void ResumeBounce()
    {
        if (bounceTween != null && bounceTween.IsActive())
        {
            bounceTween.Play();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Inspector에서 파라미터 변경 시 실시간 미리보기
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && bounceTween != null && bounceTween.IsActive())
        {
            StartBounce();
        }
    }
#endif
}