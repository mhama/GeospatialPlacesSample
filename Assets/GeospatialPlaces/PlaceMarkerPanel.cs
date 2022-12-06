using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレースを2D表示するパネル
/// Anchorの位置に表示する
/// 距離によってスケールを調整する
/// </summary>
public class PlaceMarkerPanel : MonoBehaviour
{
    [SerializeField]
    Text titleText;
    [SerializeField]
    Text distanceText;
    [SerializeField]
    Text starsText;

    [SerializeField]
    GameObject rootPanel;

    private PlaceItem place;

    private Canvas canvas;

    public float LastDistance { get; private set; }

    /// <summary>
    /// このAnchorに合致するスクリーン位置にUIを表示する
    /// </summary>
    [SerializeField]
    private GameObject targetAnchor;

    // Start is called before the first frame update
    void Start()
    {
        canvas = transform.GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        UpdateScreenPosition();
        UpdateText();
        float uiScale = CalcUIScale();
        rootPanel.transform.localScale = new Vector3(uiScale, uiScale, 1.0f);
    }

    private void UpdateScreenPosition()
    {
        if (targetAnchor == null)
        {
            return;
        }

        var screenPos = Camera.main.WorldToScreenPoint(targetAnchor.transform.position);

        // カメラ（自分）の後ろにあるやつを描画しない
        rootPanel.SetActive(screenPos.z > 0);

        // screenPosを指定するとPanelの左下端がAnchor位置に合致してしまうので、センターに合致するようオフセットする。
        var rectTrans = (transform as RectTransform);
        Vector2 offset = new Vector2(rectTrans.rect.width, rectTrans.rect.height) * 0.5f;
        (this.transform as RectTransform).localPosition = new Vector3(screenPos.x - offset.x, screenPos.y - offset.y, 0);
    }

    public void SetPlace(PlaceItem place)
    {
        this.place = place;
        titleText.text = place.Title;
    }

    private void UpdateText()
    {
        if (place == null || targetAnchor == null)
        {
            titleText.text = "Unknown";
            distanceText.text = "(-- m)";
            starsText.text = "--";
            return;
        }
        LastDistance = DistanceFromCamera();
        titleText.text = $"{place?.Title}";
        distanceText.text = $"({LastDistance:F0} m)";
        starsText.text = $"{place?.Rating:F1}";
    }

    private float DistanceFromCamera()
    {
        return (Camera.main.transform.position - targetAnchor.transform.position).magnitude;
    }

    public void SetTargetAnchor(GameObject targetAnchor)
    {
        this.targetAnchor = targetAnchor;
    }

    private const float nearScaleDistance = 30.0f;
    private const float farScaleDistance = 200.0f;

    /// <summary>
    /// UIのスケールを返す
    /// LastDistanceが nearScaleDistance より近ければ2.0を返す
    /// nearScaleDistance と farScaleDistance の間であれば内分。
    /// farScaleDistance より遠ければ1.0を返す
    /// 0の場合は例外的に1を返す
    /// </summary>
    /// <returns></returns>
    private float CalcUIScale()
    {
        if (LastDistance == 0)
        {
            return 1.0f;
        }
        float ratio = Mathf.InverseLerp(nearScaleDistance, farScaleDistance, LastDistance);
        return Mathf.Lerp(2.0f, 1.0f, ratio);
    }
}
