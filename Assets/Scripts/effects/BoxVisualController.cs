using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections;
using Coffee.UIExtensions;
public class BoxVisualController : MonoBehaviour
{
    [Serializable]
    public class BoxPair
    {
        [Header("박스 컨테이너(이 RectTransform만 움직임)")]
        public RectTransform root;

        [Header("열린 상자 오브젝트")]
        public GameObject openObj;

        [Header("닫힌 상자 오브젝트")]
        public GameObject closedObj;

[Header("보석(번들 UI)이 붙을 위치")]
public RectTransform gemTrf;

  public CanvasGroup GetCanvasGroup()
{
    var cg = root.GetComponent<CanvasGroup>();
    if (cg == null) cg = root.gameObject.AddComponent<CanvasGroup>();
    return cg;
}
        public void SetOpen()
        {
            if (openObj != null) openObj.SetActive(true);
            if (closedObj != null) closedObj.SetActive(false);
        }

        public void SetClosed()
        {
            if (openObj != null) openObj.SetActive(false);
            if (closedObj != null) closedObj.SetActive(true);
        }

        public void ResetVisual()
        {
            SetOpen();
            if (root != null)
    {
        root.localScale = Vector3.one;
        root.localRotation = Quaternion.identity;

        var cg = root.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;
    }
        }
    }

    

    [Header("상자 오브젝트 2개 (교대로 사용)")]
    public BoxPair boxA; // 현재 화면 중앙에 있는 상자
    public BoxPair boxB; // 화면 왼쪽 밖에서 대기 중인 상자

    [Header("UI 파티클 효과 (UIParticle)")]
    public UIParticle departureDust; // 오른쪽으로 출발할 때 (상자 뒤)
    public UIParticle arrivalDust;   // 중앙에 도착할 때 (상자 바닥)

    [Header("연출 설정")]
    public float moveDuration = 0.1f; // 이동 시간
    public float punchScale = 0.3f;   // 닫힐 때 찌그러짐 강도

[Header("컨베이어/낙하 연출")]
public float incomingDelay = 0.08f;     // 다음 상자 들어오는 딜레이(컨베이어 느낌)
public float fallOffsetY = 350f;        // 떨어지는 거리(아래로)
public float rotateDuration = 0.3f;    // 90도 회전 시간
public float fadeDuration = 0.2f;      // 페이드아웃 시간 (회전 완료 시점부터)
public float moveEase = 0.35f;          // 오른쪽으로 가는 가속감(0~1 느낌으로 쓰고 싶으면)


[Header("넘어지기 전 살짝 이동")]
public float tipOffsetX = 120f;      // 살짝 밀리는 거리(해상도 따라 60~180 추천)
public float nudgeDuration = 0.10f;  // 살짝 밀리는 시간(0.08~0.15)


    private BoxPair currentBox;
    private BoxPair nextBox;

    private Vector2 centerPos;
    private Vector2 leftPos;
    private Vector2 rightPos;
    
    public RectTransform CurrentGemTrf => currentBox.gemTrf;
public event Action<RectTransform> OnGemTrfChanged;


    void Awake()
    {
        // 초기 위치 기준(에디터에 배치된 상태 기준)
        centerPos = boxA.root.anchoredPosition;

        // 화면 너비 기준 좌우 좌표 계산
        float offset = 1200f;
        leftPos  = new Vector2(centerPos.x - offset, centerPos.y);
        rightPos = new Vector2(centerPos.x + offset, centerPos.y);

        currentBox = boxA;
        nextBox = boxB;

        // next는 왼쪽 밖으로
        nextBox.root.anchoredPosition = leftPos;

        // 둘 다 열린 상태로 시작
        currentBox.ResetVisual();
        nextBox.ResetVisual();
        OnGemTrfChanged?.Invoke(CurrentGemTrf);

    }



    

   public float incomingTilt = -10f;       // 들어올 때 기울기
public float straightenDuration = 0.2f; // 끼익! 복귀 시간
public float straightenDelay = 0.18f;    // 들어오기 시작 후 얼마 뒤에 복귀할지(조절 포인트)

public float fallDuration = 0.5f;      // 낙하 시간 (moveDuration과 분리)
public float fallEasePower = 1f;        // 안 써도 됨(옵션)
public float rotateLead = 0.06f;    // 이동이 끝나기 '이만큼 전'부터 회전/낙하 시작

public void PlayCompleteAnimation(Action onComplete)
{
    currentBox.root.DOKill();
    nextBox.root.DOKill();

    Sequence seq = DOTween.Sequence();

    var outBox = currentBox;
    var inBox = nextBox;

    var outCg = outBox.GetCanvasGroup();
    var inCg  = inBox.GetCanvasGroup();

    // 들어오는 박스 준비: 열림 + 약간 기울인 상태로 시작
    inBox.ResetVisual();
    inCg.alpha = 1f;
    inBox.root.localRotation = Quaternion.Euler(0, 0, incomingTilt);

    // ===== 1) 닫힘(찌그러짐) =====
    seq.Append(outBox.root.DOScale(new Vector3(1.1f, 0.9f, 1f), 0.1f));
    seq.AppendCallback(() =>
    {
        outBox.SetClosed();
        SoundManager.Instance.PlayFX(SoundType.OneBoxFailue);
    });
    seq.Append(outBox.root.DOScale(Vector3.one, 0.08f).SetEase(Ease.OutQuad));

    // ===== 2) 출발 먼지 =====
    seq.AppendCallback(() =>
    {
        if (departureDust != null) departureDust.Play();
    });

    // ===== 3-A) 컨베이어 구간: out는 오른쪽으로, in은 딜레이 후 중앙으로 =====
   
// tipPos (살짝 오른쪽)
Vector2 tipPos = new Vector2(centerPos.x + tipOffsetX, centerPos.y);
float fallY = tipPos.y - fallOffsetY;

// 3-A) outBox는 X만 오른쪽으로 이동 (컨베이어)
seq.Append(outBox.root.DOAnchorPosX(tipPos.x, nudgeDuration).SetEase(Ease.InQuad));

// inBox 들어오는 건 기존처럼 Join + delay
seq.Join(inBox.root.DOAnchorPos(centerPos, moveDuration)
    .SetDelay(incomingDelay)
    .SetEase(Ease.OutQuad));
 seq.AppendCallback(() =>
    {
        if (arrivalDust != null) arrivalDust.Play();
    });

// inBox '끼익' 복귀도 기존대로 Join
seq.Join(inBox.root.DOLocalRotate(Vector3.zero, straightenDuration)
    .SetDelay(incomingDelay + straightenDelay)
    .SetEase(Ease.OutBack));


// ✅ 이동 끝나기 직전부터 회전 + 낙하를 "겹치게" 시작
float overlapStart = seq.Duration() - rotateLead;

// 회전(넘어짐)
seq.Insert(overlapStart,
    outBox.root.DOLocalRotate(new Vector3(0, 0, -90f), rotateDuration)
        .SetEase(Ease.InQuad));

// 낙하(Y만 아래로)
seq.Insert(overlapStart,
    outBox.root.DOAnchorPosY(fallY, fallDuration)
        .SetEase(Ease.InQuad));

// 페이드는 "90도 회전 완료 시점부터"
seq.Insert(overlapStart + rotateDuration,
    outCg.DOFade(0f, fadeDuration).SetEase(Ease.OutQuad));


    // ===== 4) 도착 먼지(들어온 박스가 중앙에 자리잡은 타이밍) =====
   
    // ===== 5) 스왑/리셋 =====
    seq.OnComplete(() =>
    {
        BoxPair temp = currentBox;
        currentBox = nextBox;
        nextBox = temp;

        OnGemTrfChanged?.Invoke(CurrentGemTrf);

        nextBox.root.anchoredPosition = leftPos;
        nextBox.ResetVisual();

        onComplete?.Invoke();
    });
}



}