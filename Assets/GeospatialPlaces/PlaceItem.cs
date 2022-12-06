
/// <summary>
/// Placeを表すValueクラス
/// </summary>
public class PlaceItem
{
    public string PlaceUniqueId { get; internal set; } // constructorの初期化子でセットしたいのでinternal setにした
    public double Latitude { get; internal set; }
    public double Longitude { get; internal set; }
    public double Altitude { get; internal set; }
    public string Title { get; internal set; }

    public float Rating { get; internal set; }


    public override string ToString()
    {
        return $"[PlaceItem] title: {Title} rating: {Rating:F2} location: {Latitude:F5},{Longitude:F5} id: {PlaceUniqueId}";
    }
}
