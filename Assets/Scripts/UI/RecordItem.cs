using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecordItem : MonoBehaviour
{
    [SerializeField] private Image bgImage;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text timeText;

    public void SetData(int rank, string nickname, long ms, bool isMine)
    {
        rankText.text = rank.ToString();
        nicknameText.text = nickname;
        
        // 시간 포맷팅: mm:ss:ms
        System.TimeSpan t = System.TimeSpan.FromMilliseconds(ms);
        timeText.text = string.Format("{0:D2}:{1:D2}.{2:D3}", t.Minutes, t.Seconds, t.Milliseconds);

        // 내 항목 강조 (연노랑 배경)
        if(isMine)
        {
            bgImage.color = new Color(1f, 1f, 0.7f); // 연노랑
            
            // 텍스트도 굵게 (옵션)
            rankText.fontStyle = FontStyles.Bold;
            nicknameText.fontStyle = FontStyles.Bold;
            timeText.fontStyle = FontStyles.Bold;
        }
        else
        {
            bgImage.color = Color.white;
            
            rankText.fontStyle = FontStyles.Normal;
            nicknameText.fontStyle = FontStyles.Normal;
            timeText.fontStyle = FontStyles.Normal;
        }
    }
}