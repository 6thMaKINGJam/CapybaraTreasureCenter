using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용하신다면 추가

public class RecordItem : MonoBehaviour
{
    [SerializeField] private Image bgImage;
    [SerializeField] private Text rankText;
    [SerializeField] private Text nicknameText;
    [SerializeField] private Text timeText;

    public void SetData(int rank, string nickname, long ms, bool isMine)
    {
        rankText.text = rank.ToString();
        nicknameText.text = nickname;
        
        // 시간 포맷팅: mm:ss:ms
        System.TimeSpan t = System.TimeSpan.FromMilliseconds(ms);
        timeText.text = string.Format("{0:D2}:{1:D2}:{2:D3}", t.Minutes, t.Seconds, t.Milliseconds);

        // 내 항목 강조 (연노랑)
        if (isMine) bgImage.color = new Color(1f, 1f, 0.7f); 
        else bgImage.color = Color.white;
    }
}