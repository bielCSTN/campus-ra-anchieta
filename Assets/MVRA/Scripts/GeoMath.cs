using System;
using UnityEngine;

namespace MVRA
{
    public static class GeoMath
    {
        public const double RaioTerra = 6378137.0;
        const double Deg2Rad = Math.PI / 180.0;
        const double Rad2Deg = 180.0 / Math.PI;

        public static Vector2 ParaENU(double lat, double lon, double latRef, double lonRef)
        {
            double leste = (lon - lonRef) * Deg2Rad * RaioTerra * Math.Cos(latRef * Deg2Rad);
            double norte = (lat - latRef) * Deg2Rad * RaioTerra;
            return new Vector2((float)leste, (float)norte);
        }

        public static void DeENU(Vector2 enu, double latRef, double lonRef, out double lat, out double lon)
        {
            lat = latRef + (enu.y / RaioTerra) * Rad2Deg;
            lon = lonRef + (enu.x / (RaioTerra * Math.Cos(latRef * Deg2Rad))) * Rad2Deg;
        }

        public static float Azimute(Vector2 enu)
        {
            return Normalizar360(Mathf.Atan2(enu.x, enu.y) * Mathf.Rad2Deg);
        }

        public static float Yaw(Vector3 v)
        {
            return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        }

        public static float Normalizar360(float a)
        {
            a %= 360f;
            return a < 0f ? a + 360f : a;
        }

        public static string FormatarDistancia(float metros)
        {
            return metros < 1000f ? $"{metros:0} m" : $"{metros / 1000f:0.0} km";
        }

        public static string PontoCardeal(float azimute)
        {
            string[] nomes = { "N", "NE", "L", "SE", "S", "SO", "O", "NO" };
            int i = Mathf.RoundToInt(Normalizar360(azimute) / 45f) % 8;
            return nomes[i];
        }
    }
}
