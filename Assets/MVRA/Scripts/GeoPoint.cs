using System;
using System.Collections.Generic;
using UnityEngine;

namespace MVRA
{
    [Serializable]
    public class GeoPoint
    {
        public string nome = "Prédio";
        public string subtitulo = "";
        [TextArea(2, 4)]
        [Tooltip("Texto curto mostrado na ficha quando o prédio está em foco (ex.: o que funciona lá).")]
        public string descricao = "";
        public double latitude;
        public double longitude;
        [Tooltip("Altura aproximada do prédio em metros (usada para posicionar o pin acima do telhado).")]
        public float alturaPredio = 14f;
        public bool ativo = true;
        public Color cor = new Color(0.85f, 0.11f, 0.11f);

        public GeoPoint() { }

        public GeoPoint(string nome, string subtitulo, double lat, double lon, float altura, bool ativo = true)
        {
            this.nome = nome;
            this.subtitulo = subtitulo;
            latitude = lat;
            longitude = lon;
            alturaPredio = altura;
            this.ativo = ativo;
        }
    }

    public static class CampusData
    {
        public static List<GeoPoint> Padrao()
        {
            return new List<GeoPoint>
            {
                new GeoPoint("Prédio 1", "", -23.214352057362248, -46.891550357443386, 14f),
                new GeoPoint("Prédio 2", "", -23.21288783009079, -46.893126261695684, 14f),
                new GeoPoint("Prédio 3", "", -23.213839068253836, -46.89286611319947, 14f),
                new GeoPoint("Prédio 4", "Colégio Aurum", -23.214382480645924, -46.89338231329859, 14f),
                new GeoPoint("Prédio Administrativo", "", -23.213872441357633, -46.89220989886967, 9f),
                new GeoPoint("Anfiteatro", "", -23.21528715564712, -46.89106982255262, 6f),

                new GeoPoint("Biblioteca", "CMS", -23.213189205149753, -46.89268352279868, 9f, false),
                new GeoPoint("Cantina", "HUB – Prédio Administrativo", -23.21396297138618, -46.89199872384886, 5f, false),
                new GeoPoint("Cantina", "Prédio 3", -23.214467140654175, -46.89261943960763, 5f, false),
                new GeoPoint("Cantina", "Prédio 4", -23.213104348369964, -46.89427153651782, 5f, false),
            };
        }
    }
}
