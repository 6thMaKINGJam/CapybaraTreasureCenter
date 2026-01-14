using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 전광판처럼 "깜빡/미세 흔들림/밝기 요동"을 주며 O/X가 나타났다 사라지는 연출.
/// - oImage / xImage: 사용자가 그려둔 스프라이트 Image를 인스펙터에서 연결
/// </summary>
public class SignboardFlickerIndicator : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private Image oImage;
    [SerializeField] private Image xImage;
    [SerializeField] private GameObject gemInfo;

    [Header("연출 기본값")]
    [SerializeField] private float showDuration = 0.9f;      // 전체 표시 시간(대략)
    [SerializeField] private float fadeIn = 0.08f;
    [SerializeField] private float fadeOut = 0.16f;

    [Header("전광판 '불빛' 느낌")]
    [SerializeField] private int flickerSteps = 14;          // 깜빡임 횟수(많을수록 더 전광판 느낌)
    [SerializeField] private float flickerMinAlpha = 0.35f;  // 깜빡일 때 최소 밝기
    [SerializeField] private float flickerMaxAlpha = 1.0f;   // 깜빡일 때 최대 밝기
    [SerializeField] private float maxJitterDeg = 1.2f;      // 미세 흔들림 회전(도)
    [SerializeField] private float maxJitterScale = 0.02f;   // 미세 흔들림 스케일(비율)

    Tween currentTween;

    void Awake()
    {
        // 시작 상태 정리
        SetVisible(oImage, false);
        SetVisible(xImage, false);
    }

    public void PlaySuccessO() => Play(oImage);
    public void PlayFailX()    => Play(xImage);

    private void Play(Image target)
    {
        if (target == null) return;

        // 진행 중인 연출 정리
        currentTween?.Kill();
        ResetVisuals();
        gemInfo.SetActive(false);

        // 타겟만 켜기
        SetVisible(oImage, target == oImage);
        SetVisible(xImage, target == xImage);

        // 알파 0에서 시작
        SetAlpha(target, 0f);

        float flickerTime = Mathf.Max(0.05f, showDuration - fadeIn - fadeOut);
        float stepTime = flickerTime / flickerSteps;

        // 시퀀스 구성
        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true); // 타임스케일 영향 최소화(원하면 제거)

        // 1) 빠른 점등
        seq.Append(DOTween.To(() => GetAlpha(target), a => SetAlpha(target, a), 1f, fadeIn)
            .SetEase(Ease.OutQuad));

        // 2) 전광판 깜빡임 + 미세 흔들림
        for (int i = 0; i < flickerSteps; i++)
        {
            float a = Random.Range(flickerMinAlpha, flickerMaxAlpha);
            float jitterZ = Random.Range(-maxJitterDeg, maxJitterDeg);
            float jitterS = Random.Range(-maxJitterScale, maxJitterScale);

            seq.AppendCallback(() =>
            {
                // 밝기 요동
                SetAlpha(target, a);

                // 미세 흔들림(회전/스케일)
                RectTransform rt = target.rectTransform;
                rt.localRotation = Quaternion.Euler(0, 0, jitterZ);
                rt.localScale = Vector3.one * (1f + jitterS);
            });
            seq.AppendInterval(stepTime);
        }

        // 3) 마무리: 살짝 더 깜빡이며 꺼짐 느낌(끝쪽 알파 흔들 + 페이드아웃)
        seq.AppendCallback(() => SetAlpha(target, Random.Range(0.55f, 1f)));
        seq.Append(DOTween.To(() => GetAlpha(target), a => SetAlpha(target, a), 0f, fadeOut)
            .SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            ResetVisuals();
        });

        currentTween = seq;
      
    }

    private void ResetVisuals()
    {
        // 이미지 끄고, 트랜스폼 초기화
        if (oImage != null)
        {
            oImage.rectTransform.localRotation = Quaternion.identity;
            oImage.rectTransform.localScale = Vector3.one;
            SetAlpha(oImage, 0f);
            SetVisible(oImage, false);
        }
        if (xImage != null)
        {
            xImage.rectTransform.localRotation = Quaternion.identity;
            xImage.rectTransform.localScale = Vector3.one;
            SetAlpha(xImage, 0f);
            SetVisible(xImage, false);
        }

          gemInfo.SetActive(true);
    }

    private static void SetVisible(Image img, bool on)
    {
        if (img != null) img.gameObject.SetActive(on);
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private static float GetAlpha(Image img) => img != null ? img.color.a : 0f;
}
