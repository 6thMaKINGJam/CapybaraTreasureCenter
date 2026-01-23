using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecordItem : MonoBehaviour
{
    [SerializeField] private Image bgImage;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text timeText;

    public void SetData(int rank, string nickname, string score)
    {
        // 1. 기본 텍스트 할당
        rankText.text = rank.ToString();
        nicknameText.text = nickname;
        
        // 2. 점수 할당 (기존 시간 포맷팅 제거)
        // 파라미터 타입을 string으로 받아 바로 할당하거나, 
        // 숫자인 경우 score.ToString()을 사용하세요.
        timeText.text = score; 

        // 3. UI 스타일 초기화
        bgImage.color = Color.white;
        
        rankText.fontStyle = FontStyles.Normal;
        nicknameText.fontStyle = FontStyles.Normal;
        timeText.fontStyle = FontStyles.Normal;
    }
}