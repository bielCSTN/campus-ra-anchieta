using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace MVRA
{
    public class LocationProvider : MonoBehaviour
    {
        public bool TemFix { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public float Precisao { get; private set; } = 999f;
        public string Status { get; private set; } = "Iniciando GPS...";
        public int Leituras { get; private set; }

        public event Action NovaLeitura;

        [Header("GPS")]
        [Tooltip("Precisão desejada em metros (quanto menor, mais bateria).")]
        public float precisaoDesejada = 1f;
        [Tooltip("Distância mínima (m) para o sistema reportar nova posição.")]
        public float distanciaAtualizacao = 0.5f;

        [Header("Simulação no Editor")]
        public bool simularNoEditor = true;
        [Tooltip("Posição inicial simulada (padrão: pátio entre o Prédio 1 e o Anfiteatro).")]
        public double latSimulada = -23.21478;
        public double lonSimulada = -46.89140;
        public float precisaoSimulada = 4f;
        [Tooltip("Transform que representa o usuário no Editor (normalmente a câmera AR).")]
        public Transform alvoSimulado;

        double ultimoTimestamp = -1;
        bool modoSimulado;
        Vector3 posInicialSim;
        float proximaLeituraSim;

        IEnumerator Start()
        {
            modoSimulado = Application.isEditor && simularNoEditor;
            if (modoSimulado)
            {
                if (alvoSimulado == null && Camera.main != null) alvoSimulado = Camera.main.transform;
                posInicialSim = alvoSimulado != null ? alvoSimulado.position : Vector3.zero;
                Status = "GPS simulado (Editor)";
                yield break;
            }

#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Status = "Pedindo permissão de localização...";
                Permission.RequestUserPermission(Permission.FineLocation);
                float t = 0f;
                while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && t < 20f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
#endif
            while (true)
            {
                if (!Input.location.isEnabledByUser)
                {
                    Status = "Ative a Localização (GPS) do celular";
                    yield return new WaitForSecondsRealtime(1.5f);
                    continue;
                }

                Input.compass.enabled = true;
                Input.location.Start(precisaoDesejada, distanciaAtualizacao);
                Status = "Conectando ao GPS...";

                float espera = 0f;
                while (Input.location.status == LocationServiceStatus.Initializing && espera < 30f)
                {
                    espera += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (Input.location.status == LocationServiceStatus.Running)
                {
                    Status = "GPS ativo";
                    while (Input.location.status == LocationServiceStatus.Running)
                        yield return new WaitForSecondsRealtime(1f);
                }

                Status = "Falha no GPS – tentando novamente...";
                Input.location.Stop();
                yield return new WaitForSecondsRealtime(3f);
            }
        }

        void Update()
        {
            if (modoSimulado)
            {
                AtualizarSimulacao();
                return;
            }

            if (Input.location.status != LocationServiceStatus.Running) return;

            LocationInfo d = Input.location.lastData;
            if (d.timestamp == ultimoTimestamp) return;
            ultimoTimestamp = d.timestamp;

            Latitude = d.latitude;
            Longitude = d.longitude;
            Precisao = Mathf.Max(d.horizontalAccuracy, 0.5f);
            TemFix = true;
            Leituras++;
            Status = $"GPS ±{Precisao:0} m";
            NovaLeitura?.Invoke();
        }

        void AtualizarSimulacao()
        {
            if (Time.time < proximaLeituraSim) return;
            proximaLeituraSim = Time.time + 1f;

            Vector3 desloc = alvoSimulado != null ? alvoSimulado.position - posInicialSim : Vector3.zero;
            GeoMath.DeENU(new Vector2(desloc.x, desloc.z), latSimulada, lonSimulada, out double lat, out double lon);

            Latitude = lat;
            Longitude = lon;
            Precisao = precisaoSimulada;
            TemFix = true;
            Leituras++;
            Status = $"GPS simulado ±{Precisao:0} m";
            NovaLeitura?.Invoke();
        }

        void OnDisable()
        {
            if (!modoSimulado && Input.location.status != LocationServiceStatus.Stopped)
                Input.location.Stop();
        }
    }
}
