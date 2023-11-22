namespace DPBlazorMapLibrary
{
    public class LatLng
    {
        public LatLng()
        {
        }

        public LatLng(double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
            Alt = 0;
        }

        public LatLng(double lat, double lng, double alt)
        {
            Lat = lat;
            Lng = lng;
            Alt = alt;
        }

        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Alt { get; set; }

        /// <summary>
        /// Формула сферического закона косинусов,
        /// для расчета дистанции в милях
        /// </summary>
        /// <param name="to">точка</param>
        /// <returns>расстояние до точки в милях</returns>
        /// <see cref="http://www.movable-type.co.uk/scripts/latlong.html"
        public double GetDistanceToPointInMiles(LatLng to)
        {
            double _radLat1 = Lat * Math.PI / 180;
            double _radLat2 = to.Lat * Math.PI / 180;
            double _dLatHalf = (_radLat2 - _radLat1) / 2;
            double _dLonHalf = Math.PI * (to.Lng - Lng) / 360;

            // intermediate result
            double _a = Math.Sin(_dLatHalf);
            _a *= _a;

            // intermediate result
            double _b = Math.Sin(_dLonHalf);
            _b *= _b * Math.Cos(_radLat1) * Math.Cos(_radLat2);

            // central angle, aka arc segment angular distance
            double _centralAngle = 2 * Math.Atan2(Math.Sqrt(_a + _b), Math.Sqrt(1 - _a - _b));

            // great-circle (orthodromic) distance on Earth between 2 points
            return 3959 * _centralAngle;
        }

        /// <summary>
        /// Формула сферического закона косинусов,
        /// для расчета дистанции в километрах
        /// </summary>
        /// <param name="to">точка</param>
        /// <returns>расстояние до точки в км</returns>
        /// <see cref="http://www.movable-type.co.uk/scripts/latlong.html"
        public double GetDistanceToPointInKillometers(LatLng to)
        {
            return GetDistanceToPointInMiles(to) * 1.60934;
        }

        /// <summary>
        /// Формула сферического закона косинусов,
        /// для расчета дистанции в метрах
        /// </summary>
        /// <param name="to">точка</param>
        /// <returns>расстояние до точки в метрах</returns>
        /// <see cref="http://www.movable-type.co.uk/scripts/latlong.html"
        public double GetDistanceToPointInMeters(LatLng to)
        {
            return GetDistanceToPointInKillometers(to) * 1000;
        }
    }
}
