using UnityEngine;
using DG.Tweening;
using TMPro;

[DisallowMultipleComponent]
public class TMP_ImpactTypeIn : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI tmp;
    public RectTransform popupPanel; // 선택: 글자 쿵! 할 때 패널도 살짝 반동

    [Header("Timing")]
    public float charInterval = 0.08f;     // 글자 간 간격
    public bool ignoreTimeScale = true;    // 팝업에서 timeScale=0이어도 재생되게

    [Header("Impact 느낌")]
    public float startYOffset = 55f;       // 위에서 떨어지는 시작 높이(px)
    public float hitDown = 8f;             // 바닥에 ‘쿵’하며 살짝 눌리는 느낌(아래로)
    [Range(1.0f, 2.0f)] public float punchScale = 1.32f; // 순간 크게(가로 글자라도 과하지 않게)
    [Range(0f, 15f)] public float startTilt = 8f;        // 시작할 때 살짝 기울었다가 정렬

    [Header("Popup 반동(선택)")]
    public bool panelRecoil = true;
    public Vector2 panelPunch = new Vector2(0f, -6f);  // 아래로 살짝 “쿵”
    public float panelPunchDuration = 0.14f;

    Sequence _seq;

    // per-character animated params
    float[] _scales;
    float[] _yOffsets;
    float[] _rotZ;

    TMP_TextInfo _textInfo;
    TMP_MeshInfo[] _cachedMeshInfo;

    void Reset()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    void Awake()
    {
        if (tmp == null) tmp = GetComponent<TextMeshProUGUI>();
    }

    void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        Stop();

        tmp.ForceMeshUpdate(true, true);
        _textInfo = tmp.textInfo;
        _cachedMeshInfo = _textInfo.CopyMeshInfoVertexData();

        int charCount = _textInfo.characterCount;
        _scales = new float[charCount];
        _yOffsets = new float[charCount];
        _rotZ = new float[charCount];

        for (int i = 0; i < charCount; i++)
        {
            _scales[i] = 1f;
            _yOffsets[i] = 0f;
            _rotZ[i] = 0f;
        }

        tmp.maxVisibleCharacters = 0;

        _seq = DOTween.Sequence()
            .SetUpdate(ignoreTimeScale)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        for (int i = 0; i < charCount; i++)
        {
            int ci = i;

            // 글자 보이게(공백/줄바꿈 포함해서 인덱스는 올려야 함)
            _seq.AppendCallback(() =>
            {
                tmp.maxVisibleCharacters = Mathf.Max(tmp.maxVisibleCharacters, ci + 1);

                if (!_textInfo.characterInfo[ci].isVisible) return;

                _scales[ci] = 0f;
                _yOffsets[ci] = startYOffset;
                _rotZ[ci] = Random.Range(-startTilt, startTilt);
            });

            if (_textInfo.characterInfo[ci].isVisible)
            {
                // 1) 튀어나오며 크게 + 떨어지기
                _seq.Append(DOTween.To(() => _scales[ci], v => _scales[ci] = v, punchScale, 0.10f).SetEase(Ease.OutQuad));
                _seq.Join(DOTween.To(() => _yOffsets[ci], v => _yOffsets[ci] = v, -hitDown, 0.10f).SetEase(Ease.InQuad));

                // 패널도 아주 살짝 “쿵”
                if (panelRecoil && popupPanel != null)
                {
                    _seq.Join(popupPanel.DOPunchAnchorPos(panelPunch, panelPunchDuration, vibrato: 8, elasticity: 0.65f));
                }

                // 2) 정착(원래 크기/위치로 바운스) + 틸트 정렬
                _seq.Append(DOTween.To(() => _scales[ci], v => _scales[ci] = v, 1f, 0.20f).SetEase(Ease.OutBack));
                _seq.Join(DOTween.To(() => _yOffsets[ci], v => _yOffsets[ci] = v, 0f, 0.24f).SetEase(Ease.OutBounce));
                _seq.Join(DOTween.To(() => _rotZ[ci], v => _rotZ[ci] = v, 0f, 0.20f).SetEase(Ease.OutCubic));
            }

            _seq.AppendInterval(charInterval);
        }

        // 매 프레임 TMP 메시 갱신
        _seq.OnUpdate(ApplyVertexAnimation);
        _seq.OnComplete(ApplyVertexAnimation);

        // 시작 프레임도 한 번 적용
        ApplyVertexAnimation();
    }

    public void Stop()
    {
        if (_seq != null && _seq.IsActive())
        {
            _seq.Kill();
            _seq = null;
        }
    }

    void ApplyVertexAnimation()
    {
        if (tmp == null) return;

        // TMP 내부 meshInfo를 최신 참조로
        _textInfo = tmp.textInfo;

        // 원본 버텍스로 되돌린 뒤, 글자별 변형 적용
        for (int m = 0; m < _textInfo.meshInfo.Length; m++)
        {
            var src = _cachedMeshInfo[m].vertices;
            var dst = _textInfo.meshInfo[m].vertices;
            if (src == null || dst == null) continue;
            System.Array.Copy(src, dst, Mathf.Min(src.Length, dst.Length));
        }

        int charCount = _textInfo.characterCount;

        for (int c = 0; c < charCount; c++)
        {
            var ch = _textInfo.characterInfo[c];
            if (!ch.isVisible) continue;

            int materialIndex = ch.materialReferenceIndex;
            int vertexIndex = ch.vertexIndex;

            Vector3[] verts = _textInfo.meshInfo[materialIndex].vertices;

            // 문자 중심
            Vector3 mid = (verts[vertexIndex] + verts[vertexIndex + 2]) * 0.5f;

            Matrix4x4 matrix = Matrix4x4.TRS(
                new Vector3(0f, _yOffsets[c], 0f),
                Quaternion.Euler(0f, 0f, _rotZ[c]),
                new Vector3(_scales[c], _scales[c], 1f)
            );

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = verts[vertexIndex + j];
                verts[vertexIndex + j] = mid + matrix.MultiplyPoint3x4(v - mid);
            }
        }

        // 메시 업데이트
        for (int m = 0; m < _textInfo.meshInfo.Length; m++)
        {
            _textInfo.meshInfo[m].mesh.vertices = _textInfo.meshInfo[m].vertices;
            tmp.UpdateGeometry(_textInfo.meshInfo[m].mesh, m);
        }
    }
}
