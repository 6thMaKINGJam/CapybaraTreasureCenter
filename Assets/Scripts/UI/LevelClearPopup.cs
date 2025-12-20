using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using DG.Tweening;

public class LevelClearPopup : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public TextMeshProUGUI MessageText;
    public GameObject[] StarObjects;
    
    [Header("버튼 연결")]
    public Button RetryButton; // [변경됨] NextLevelButton -> RetryButton
    public Button HomeButton;

    [Header("연출 설정")]
    public float StarDelay = 0.3f;

    // 콜백 저장용 변수
    private Action onRetryCallback;
    private Action onHomeCallback;

    /// <summary>
    /// 팝업 초기화 및 표시
    /// </summary>
    /// <param name="starCount">별 개수</param>
    /// <param name="message">메시지</param>
    /// <param name="onRetry">다시하기 버튼 기능</param>
    /// <param name="onHome">홈으로 가기 버튼 기능</param>
    public void Setup(int starCount, string message, Action onRetry, Action onHome)
    {
        // 1. 텍스트 설정
        if (MessageText != null)
            MessageText.text = message;

        // 2. 콜백 저장
        onRetryCallback = onRetry;
        onHomeCallback = onHome;

        // 3. 버튼 리스너 연결 (기존 연결 제거 후 새로 추가)
        if(RetryButton != null)
        {
            RetryButton.onClick.RemoveAllListeners();
            RetryButton.onClick.AddListener(() => onRetryCallback?.Invoke());
        }
        
        if(HomeButton != null)
        {
            HomeButton.onClick.RemoveAllListeners();
            HomeButton.onClick.AddListener(() => onHomeCallback?.Invoke());
        }

        // 4. 별 초기화
        foreach (var star in StarObjects)
        {
            star.SetActive(false);
            star.transform.localScale = Vector3.zero;
        }

        // 5. 별 연출 시작
        StartCoroutine(ShowStarsRoutine(starCount));
    }

    private IEnumerator ShowStarsRoutine(int starCount)
    {
        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < starCount; i++)
        {
            if (i < StarObjects.Length)
            {
                GameObject star = StarObjects[i];
                star.SetActive(true);
                star.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
                yield return new WaitForSeconds(StarDelay);
            }
        }
    }
}