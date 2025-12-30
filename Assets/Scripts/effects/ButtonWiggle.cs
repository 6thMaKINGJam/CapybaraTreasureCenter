using DG.Tweening;
using UnityEngine;

public class ButtonWiggle : MonoBehaviour
{
    public float idleTime = 0.6f;
    public float wiggleDuration = 0.4f;
    public float strength = 6f;

    void OnEnable()
    {
        var rt = (RectTransform)transform;

        // "가만히" -> "살짝 흔들" -> "가만히" 반복
        DOVirtual.DelayedCall(idleTime, () =>
        {
            rt.DOKill();
            rt.DOShakeRotation(wiggleDuration, new Vector3(0,0,strength), vibrato: 8, randomness: 90, fadeOut: true)
              .OnComplete(() => rt.localRotation = Quaternion.identity);
        }).SetLoops(-1, LoopType.Restart);
    }

    void OnDisable()
    {
        transform.DOKill();
    }
}
