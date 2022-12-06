using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.Networking;
using System.Linq;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using GeospatialPlaces;

/// <summary>
/// GoogleのPlaceを読みだして配置するコントローラー
/// </summary>
public class PlacesController : MonoBehaviour
{
    /// <summary>
    /// Google Places APIを利用できるAPI Key
    /// </summary>
    private readonly static string googlePlacesApiKey = "";

    /// <summary>
    /// Google Places APIのURL
    /// </summary>
    private readonly static string googlePlacesApiUrl = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?";

    [SerializeField]
    PlaceAnchorCollection placeAnchorCollection;

    [SerializeField]
    ARAnchorManager anchorManager;

    [SerializeField]
    AREarthManager earthManager;

    [SerializeField]
    Button reloadPlacesButton;

    private bool scanRequested = false;

    private readonly FloorFinder floorFinder = new FloorFinder();

    public void RequestScan()
    {
        scanRequested = true;
    }


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("PlacesController.Start()");

        // リロードボタンのハンドリング。プレース情報をクリアしてプレース検索しなおす
        reloadPlacesButton.onClick.AddListener(() =>
        {
            placeAnchorCollection.Clear();
            RequestScan();
        });
        Loop(this.GetCancellationTokenOnDestroy()).Forget();
    }

    async UniTask Loop(CancellationToken token)
    {
        // トラッキングされていれば、１分に１回データ更新する
        while (true) {
            if (earthManager.EarthState == EarthState.Enabled 
                && earthManager.EarthTrackingState == TrackingState.Tracking)
            {
                Debug.Log("PlacesController call SearchNearbyPlaces()");

                var lat = earthManager.CameraGeospatialPose.Latitude;
                var lon = earthManager.CameraGeospatialPose.Longitude;
                // 近くのPlaceを探す
                await SearchNearbyPlacesFlexible(lat, lon, token);
                scanRequested = false;

                Debug.Log("PlacesController call CreateGeospatialAnchors()");

                // GeospatialAnchorを生成する
                placeAnchorCollection.CreateGeospatialAnchors(anchorManager);

                float waitStartTime = Time.unscaledTime;
                while (Time.unscaledTime - waitStartTime < 60.0f
                    && scanRequested == false)
                {
                    await UniTask.Delay(3000, cancellationToken:token);
                }
            }
            token.ThrowIfCancellationRequested();
            await UniTask.Yield(token);
        }
    }

    /// <summary>
    /// 半径を変えながら、必要なだけの情報をPlace APIから取得する
    /// </summary>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    async UniTask SearchNearbyPlacesFlexible(double latitude, double longitude, CancellationToken token)
    {
        float radius = 100;
        int totalPlaces = 0;
        while(true)
        {
            var places = await SearchNearbyPlaces(latitude, longitude, radius, token);
            Debug.Log($"PlacesController.SearchNearbyPlaces() radius: {radius} places: " + places.Count());

            foreach (var place in places)
            {
                if (!placeAnchorCollection.HasPlace(place.PlaceUniqueId))
                {
                    placeAnchorCollection.AddPlace(place);
                }
            }

            totalPlaces += places.Count;

            if (totalPlaces > 100)
            {
                break;
            }
            radius *= 3;
        }
    }

    /// <summary>
    /// レストラン、カフェ、パン屋のプレースを取得してplacesに追加する
    /// </summary>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="radius"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    async UniTask<List<PlaceItem>> SearchNearbyPlaces(double latitude, double longitude, float radius, CancellationToken token)
    {
        var types = new List<string>() {
            "restaurant",
            "cafe",
            "bakery",
        };
        var tasks = types.Select(type =>
        {
            return CallNearbyPlacesApiPagenated(latitude, longitude, radius, type, token);
        });

        var results = await UniTask.WhenAll(tasks);

        List<PlaceItem> places = new List<PlaceItem>();
        foreach(var result in results)
        {
            places.AddRange(result);
        }
        return places;
    }

    /// <summary>
    /// Google Place APIを一回だけ呼ぶ
    /// 次のページがある場合はreturnタプルの第２要素にnext page tokenを返す。
    /// </summary>
    /// <param name="latiude"></param>
    /// <param name="longiture"></param>
    /// <param name="radius"></param>
    /// <param name="type"></param>
    /// <param name="pageToken"></param>
    /// <param name="token"></param>
    /// <returns>PlaceItem list, next page token</returns>
    async UniTask<(List<PlaceItem>, string)> CallNearbyPlacesApi(double latiude, double longiture, float radius, string type, string pageToken, CancellationToken token)
    {
        var queries = new Dictionary<string, string>();
        queries["location"] = $"{latiude:F6},{longiture:F6}";
        queries["language"] = "ja";
        queries["radius"] = $"{radius:F1}";
        queries["key"] = googlePlacesApiKey;
        queries["type"] = type;
        var queriesStr = string.Join("&", queries.Select(p => (p.Key + "=" + Uri.EscapeUriString(p.Value))));
        var url = googlePlacesApiUrl + queriesStr;
        Debug.Log("call google places url: " + url);
        using (var uwr = new UnityWebRequest(url))
        using (var handler = new DownloadHandlerBuffer())
        {
            uwr.downloadHandler = handler;
            await uwr.SendWebRequest().WithCancellation(token);
            if (uwr.isHttpError || uwr.isNetworkError)
            {
                throw new Exception("api call error. error: " + uwr.error);
            }
            JObject jobj = JObject.Parse(handler.text);
            var places = ConvertJsonToPlaceList(jobj);
            Debug.Log("places: " + string.Join("\n", places.Select(p => p.ToString())));

            string nextPageToken = jobj["next_page_token"]?.ToString();

            return (places, nextPageToken);
        }
    }

    /// <summary>
    /// ページネーションつきで近いPlaceを取得する
    /// </summary>
    /// <param name="latiude"></param>
    /// <param name="longiture"></param>
    /// <param name="radius"></param>
    /// <param name="type"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    async UniTask<List<PlaceItem>> CallNearbyPlacesApiPagenated(double latiude, double longiture, float radius, string type, CancellationToken token)
    {
        List<PlaceItem> places = new List<PlaceItem>();
        string pageToken = null;
        int pageCount = 1;
        while (pageCount < 10)
        {
            Debug.Log($"page[{pageCount}] calling CallNearbyPlacesApi({latiude:F4}, {longiture:F4}, {radius:F1}, {type})");
            var result = await CallNearbyPlacesApi(latiude, longiture, radius, type, pageToken, token);
            if (result.Item2 == null)
            {
                break;
            }
            places.AddRange(result.Item1);
            pageCount++;
        }
        return places;
    }


    /// <summary>
    /// Google Places APIの返却JSONをPlaceItemのリストに変換する
    /// </summary>
    /// <param name="jobj"></param>
    /// <returns></returns>
    List<PlaceItem> ConvertJsonToPlaceList(JObject jobj)
    {
        var placeList = new List<PlaceItem>();
        var status = jobj["status"].ToString();
        if (status != "OK")
        {
            Debug.LogError("api call error. status: " + status);
            return placeList;
        }
        var jresults = jobj["results"].Children().ToList();
        foreach (var jplace in jresults)
        {
            double latitude, longitude;
            var latStr = jplace["geometry"]?["location"]?["lat"]?.ToString();
            var lonStr = jplace["geometry"]?["location"]?["lng"]?.ToString();
            if (latStr == null || lonStr == null 
                || !Double.TryParse(latStr, out latitude)
                || !Double.TryParse(lonStr, out longitude)) {
                Debug.LogError($"json lat lon error. latStr:{latStr} lonStr: {lonStr}");
                continue;
            }

            float rating = 0;
            string ratingStr = jplace["rating"]?.ToString();
            if (ratingStr != null)
            {
                float.TryParse(ratingStr, out rating);
            }

            int floor = 1;
            if (jplace["vicinity"] != null)
            {
                floor = floorFinder.FindFloor(jplace["vicinity"].ToString()) ?? 1;
            }

            var place = new PlaceItem()
            {
                Title = jplace["name"]?.ToString() ?? "No Name",
                Latitude = latitude,
                Longitude = longitude,
                Altitude = (floor - 1) * 3.0f,
                PlaceUniqueId = jplace["place_id"].ToString(),
                Rating = rating,
            };
            placeList.Add(place);
        }
        return placeList;
    }

}
