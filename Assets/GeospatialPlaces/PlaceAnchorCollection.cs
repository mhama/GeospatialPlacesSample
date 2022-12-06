using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Google.XR.ARCoreExtensions;

/// <summary>
/// PlaceとAnchorの紐づけを保持するクラス
/// </summary>
public class PlaceAnchorHolder
{
    public ARGeospatialAnchor Anchor { get; internal set; } // constructorの初期化子でセットしたいのでinternal setにした
    public PlaceItem Place { get; internal set; }
    public string uniquePlaceId => Place.PlaceUniqueId;

    public PlaceMarkerPanel PlaceMarkerPanel { get; internal set; }

    public PlaceAnchorHolder()
    {

    }

    public void SetGeospatialAnchor(ARGeospatialAnchor anchor, PlaceMarkerPanel panel)
    {
        ResetGeospatialAnchor();
        Anchor = anchor;
        PlaceMarkerPanel = panel;
    }

    public void ResetGeospatialAnchor()
    {
        if (PlaceMarkerPanel != null)
        {
            GameObject.Destroy(PlaceMarkerPanel?.gameObject);
            PlaceMarkerPanel = null;
        }

        if (Anchor != null)
        {
            GameObject.Destroy(Anchor?.gameObject);
            Anchor = null;
        }
    }
}

/// <summary>
/// 表示対象のPlace情報を保持する
/// </summary>
public class PlaceAnchorCollection : MonoBehaviour
{
    [SerializeField]
    PlaceMarkerPanel placeMarkerPanelPrefab;

    [SerializeField]
    GameObject placeMarkerObjectPrefab;

    [SerializeField]
    Transform placeMarkerParent;

    /// <summary>
    /// Google Places APIのPlaceユニークIDをキーとしてPlaceAnchorHolderを保持するDictionary
    /// </summary>
    private Dictionary<string, PlaceAnchorHolder> placeAnchorDic = new Dictionary<string, PlaceAnchorHolder>();

    private void Update()
    {
        BubbleSortPlaceMarker();
    }

    public void AddPlace(PlaceItem placeItem)
    {
        if (HasPlace(placeItem.PlaceUniqueId))
        {
            return;
        }
        placeAnchorDic.Add(placeItem.PlaceUniqueId, new PlaceAnchorHolder()
        {
            Place = placeItem
        });
    }

    public bool HasPlace(string placeUniqueId)
    {
        return placeAnchorDic.ContainsKey(placeUniqueId);
    }

    /// <summary>
    /// 登録されたプレース情報に対応するGeospatial Anchorを生成する
    /// Anchor生成済みのプレースは無視する
    /// </summary>
    /// <param name="anchorManager"></param>
    public void CreateGeospatialAnchors(ARAnchorManager anchorManager)
    {
        foreach(var place in placeAnchorDic.Values)
        {
            if (place.Anchor != null)
            {
                continue;
            }

            // Terrain Anchorを作成する
            float altitude = (float) place.Place.Altitude;
            var geospatialAnchor = anchorManager.ResolveAnchorOnTerrain(place.Place.Latitude, place.Place.Longitude, altitude, Quaternion.identity);
            if (geospatialAnchor == null)
            {
                Debug.LogError("Couldn't create geospatialAnchor.");
                continue;
            }
            Debug.Log("PlaceAnchorCollection.CreateGeospatialAnchors() geospatialAnchor created.");

            // マーカーオブジェクトprefab (3D) をInstantiateする
            GameObject.Instantiate(placeMarkerObjectPrefab, geospatialAnchor.transform);

            // マーカーパネルprefab (2D) をInstantiateする
            var go = GameObject.Instantiate(placeMarkerPanelPrefab, placeMarkerParent);
            var placeMarkerPanel = go.GetComponent<PlaceMarkerPanel>();
            placeMarkerPanel.SetPlace(place.Place);
            placeMarkerPanel.SetTargetAnchor(geospatialAnchor.gameObject);

            // Place情報に表示用prefabを紐づける
            place.SetGeospatialAnchor(geospatialAnchor, placeMarkerPanel);
        }
    }

    /// <summary>
    /// placeMarkerParentの子のPlaceMarkerPanelをソートする
    /// バブルソートの１回し分の処理のみ行っており、何度も呼ぶことでソートが完成する
    /// </summary>
    public void BubbleSortPlaceMarker()
    {
        List<PlaceMarkerPanel> panels = new List<PlaceMarkerPanel>();
        
        for(int i=0 ; i< placeMarkerParent.childCount ; i++)
        {
            panels.Add(placeMarkerParent.GetChild(i).GetComponent<PlaceMarkerPanel>());
        }

        // バブルソートの１回し分の処理
        for (int i=0; i< panels.Count-1; i++)
        {
            var x = panels[i];
            var y = panels[i + 1];
            if (x.LastDistance < y.LastDistance)
            {
                y.transform.SetSiblingIndex(i);
            }
        }
    }

    public void Clear()
    {
        foreach (var place in placeAnchorDic.Values)
        {
            place.ResetGeospatialAnchor();
        }
        placeAnchorDic.Clear();
    }
}
