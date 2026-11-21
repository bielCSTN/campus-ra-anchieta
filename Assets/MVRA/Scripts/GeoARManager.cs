using System.Collections.Generic;
using UnityEngine;

namespace MVRA
{
    [DefaultExecutionOrder(100)]
    public class GeoARManager : MonoBehaviour
    {
        [Header("Referências")]
        public Camera arCamera;
        public LocationProvider gps;
        public HeadingProvider bussola;
        [Tooltip("Modelo do marcador (map_pin.fbx). Se vazio, usa um pin procedural.")]
        public GameObject modeloPin;

        [Header("Pontos do campus")]
        public List<GeoPoint> pontos = CampusData.Padrao();

        [Header("Exibição")]
        public float distanciaMaxRender = 60f;
        public float distanciaMinRender = 4f;
        [Tooltip("Altura do pin em relação à distância (0,1 = ~5,7° de altura aparente).")]
        public float tamanhoAngular = 0.1f;
        [Tooltip("Ângulo mínimo de elevação do pin acima do horizonte (graus).")]
        public float elevacaoMinima = 2.5f;
        [Tooltip("Ângulo máximo de elevação do pin (graus) – evita que prédios próximos joguem o pin para fora da tela.")]
        public float elevacaoMaxima = 9f;
        [Tooltip("Altura dos olhos/celular em relação ao chão (m).")]
        public float alturaCelular = 1.5f;

        [Header("Fusão de sensores")]
        [Tooltip("Constante de tempo (s) da suavização do alinhamento com a bússola.")]
        public float suavizacaoNorte = 1.5f;
        [Tooltip("Ganho máximo da correção pelo GPS em cada leitura (0..1).")]
        public float ganhoGpsMax = 0.35f;

        public float YawNorte { get; private set; }
        public bool NorteOk { get; private set; }
        public bool PosicaoOk { get; private set; }
        public bool Calibrado { get; private set; }
        public Vector2 PosicaoUsuarioENU { get; private set; }
        public bool Pronto => NorteOk && PosicaoOk;
        public IReadOnlyList<GeoPinView> Pins => pins;

        public float AzimuteCamera => GeoMath.Normalizar360(YawCamera() - YawNorte);

        double latRef, lonRef;
        readonly List<GeoPinView> pins = new List<GeoPinView>();
        Vector3 ultimaPosCam;
        bool temUltimaPos;
        int leiturasGps;

        void Start()
        {
            if (arCamera == null) arCamera = Camera.main;
            if (gps == null) gps = FindObjectOfType<LocationProvider>();
            if (bussola == null) bussola = FindObjectOfType<HeadingProvider>();

            arCamera.farClipPlane = Mathf.Max(arCamera.farClipPlane, 1000f);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;

            CalcularReferencia();
            CriarPins();
            gps.NovaLeitura += AoReceberGps;
        }

        public Vector2 ParaENU(double lat, double lon) => GeoMath.ParaENU(lat, lon, latRef, lonRef);

        void OnDestroy()
        {
            if (gps != null) gps.NovaLeitura -= AoReceberGps;
        }

        void CalcularReferencia()
        {
            double sLat = 0, sLon = 0;
            int n = 0;
            foreach (var p in pontos)
            {
                if (!p.ativo) continue;
                sLat += p.latitude; sLon += p.longitude; n++;
            }
            if (n == 0) { latRef = -23.2139; lonRef = -46.8925; return; }
            latRef = sLat / n;
            lonRef = sLon / n;
        }

        void CriarPins()
        {
            var pai = new GameObject("Pins").transform;
            foreach (var p in pontos)
            {
                if (!p.ativo) continue;
                Vector2 enu = GeoMath.ParaENU(p.latitude, p.longitude, latRef, lonRef);
                var pin = GeoPinView.Criar(p, enu, modeloPin, pai);
                pin.gameObject.SetActive(false);
                pins.Add(pin);
            }
        }

        void AoReceberGps()
        {
            Vector2 medido = GeoMath.ParaENU(gps.Latitude, gps.Longitude, latRef, lonRef);
            leiturasGps++;

            if (!PosicaoOk)
            {
                PosicaoUsuarioENU = medido;
                PosicaoOk = true;
                return;
            }

            float ganho = Mathf.Clamp(1.5f / Mathf.Max(gps.Precisao, 1f), 0.03f, ganhoGpsMax);
            if (leiturasGps < 6) ganho = Mathf.Max(ganho, 0.5f);
            PosicaoUsuarioENU = Vector2.Lerp(PosicaoUsuarioENU, medido, ganho);
        }

        float YawCamera()
        {
            Transform cam = arCamera.transform;
            Vector3 f = cam.forward; f.y = 0f;
            if (f.sqrMagnitude < 0.09f) { f = cam.up; f.y = 0f; }
            return GeoMath.Yaw(f);
        }

        void Update()
        {
            Transform cam = arCamera.transform;
            Vector3 posCam = cam.position;

            if (!Calibrado && bussola != null && bussola.Valido)
            {
                float candidato = YawCamera() - bussola.Azimute;
                if (!NorteOk)
                {
                    YawNorte = candidato;
                    NorteOk = true;
                }
                else
                {
                    float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.05f, suavizacaoNorte));
                    YawNorte += Mathf.DeltaAngle(YawNorte, candidato) * k;
                }
            }

            if (temUltimaPos && NorteOk && PosicaoOk)
            {
                Vector3 d = posCam - ultimaPosCam;
                d.y = 0f;
                Vector3 dEnu = Quaternion.Euler(0f, -YawNorte, 0f) * d;
                PosicaoUsuarioENU += new Vector2(dEnu.x, dEnu.z);
            }
            ultimaPosCam = posCam;
            temUltimaPos = true;

            Quaternion enuParaAR = Quaternion.Euler(0f, YawNorte, 0f);
            foreach (var pin in pins)
            {
                Vector2 rel = pin.ENU - PosicaoUsuarioENU;
                float dist = rel.magnitude;
                pin.Distancia = dist;
                pin.Azimute = GeoMath.Azimute(rel);

                if (!Pronto) { pin.gameObject.SetActive(false); continue; }
                pin.gameObject.SetActive(true);

                Vector3 dir = dist > 0.01f ? enuParaAR * new Vector3(rel.x / dist, 0f, rel.y / dist) : cam.forward;
                float dRender = Mathf.Clamp(dist, distanciaMinRender, distanciaMaxRender);

                float elev = Mathf.Atan2(pin.Ponto.alturaPredio - alturaCelular + 1f, Mathf.Max(dist, 1f)) * Mathf.Rad2Deg;
                elev = Mathf.Clamp(elev, elevacaoMinima, elevacaoMaxima);

                Vector3 pos = posCam + dir * dRender + Vector3.up * (dRender * Mathf.Tan(elev * Mathf.Deg2Rad));
                float fatorPerto = Mathf.Lerp(1.25f, 0.8f, Mathf.InverseLerp(30f, 350f, dist));
                float escala = dRender * tamanhoAngular * fatorPerto;

                pin.Posicionar(pos, escala, cam);
                pin.BaseMundo = posCam + dir * dRender + Vector3.down * (alturaCelular * dRender / Mathf.Max(dist, 1f));
            }
        }

        public bool CalibrarCom(int indice)
        {
            if (!PosicaoOk || indice < 0 || indice >= pins.Count) return false;
            Vector2 rel = pins[indice].ENU - PosicaoUsuarioENU;
            float azimute = GeoMath.Azimute(rel);
            YawNorte = YawCamera() - azimute;
            NorteOk = true;
            Calibrado = true;
            return true;
        }

        public void AjusteFino(float graus)
        {
            YawNorte += graus;
            NorteOk = true;
            Calibrado = true;
        }

        public void VoltarBussolaAutomatica()
        {
            Calibrado = false;
        }

        public void ResetarPosicaoGps()
        {
            if (!gps.TemFix) return;
            PosicaoUsuarioENU = GeoMath.ParaENU(gps.Latitude, gps.Longitude, latRef, lonRef);
            PosicaoOk = true;
        }
    }
}
