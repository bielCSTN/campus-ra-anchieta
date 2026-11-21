using UnityEngine;

namespace MVRA
{
    public class HeadingProvider : MonoBehaviour
    {
        public float Azimute { get; private set; }
        public float Qualidade { get; private set; }
        public bool Valido { get; private set; }
        public float Declinacao { get; private set; }
        public bool SinalInvertido => votosInvertido > votosNormal + 15;

        [Tooltip("Declinação magnética usada se o sistema não informar (Jundiaí/SP 2026 ≈ -21,5°).")]
        public float declinacaoPadrao = -21.5f;
        [Tooltip("Constante de tempo da suavização (s).")]
        public float suavizacao = 0.4f;
        [Tooltip("No Editor a bússola é simulada pelo yaw desta câmera.")]
        public Transform cameraEditor;

        Vector2 vetorSuave;
        bool iniciado;
        int votosNormal, votosInvertido;

        void OnEnable()
        {
            Input.compass.enabled = true;
            Declinacao = declinacaoPadrao;
        }

        void Update()
        {
            if (Application.isEditor)
            {
                if (cameraEditor == null && Camera.main != null) cameraEditor = Camera.main.transform;
                if (cameraEditor != null)
                {
                    Azimute = GeoMath.Normalizar360(GeoMath.Yaw(cameraEditor.forward));
                    Qualidade = 1f;
                    Valido = true;
                }
                return;
            }

            Vector3 acel = Input.acceleration;
            Vector3 mag = Input.compass.rawVector;
            if (acel.sqrMagnitude < 0.25f || mag.sqrMagnitude < 1f)
            {
                Valido = false;
                return;
            }

            Vector3 cima = -acel.normalized;
            Vector3 horiz = mag - Vector3.Dot(mag, cima) * cima;
            if (horiz.sqrMagnitude < 1e-4f) { Valido = false; return; }

            Vector3 norte = horiz.normalized;
            Vector3 leste = Vector3.Cross(norte, cima);

            AutoVerificarSinal(cima, norte, leste);
            if (SinalInvertido) { norte = -norte; leste = -leste; }

            float ce = -leste.z;
            float cn = -norte.z;
            Qualidade = Mathf.Clamp01(Mathf.Sqrt(ce * ce + cn * cn));
            float azMag = Mathf.Atan2(ce, cn) * Mathf.Rad2Deg;

            AtualizarDeclinacao();
            float az = azMag + Declinacao;

            Vector2 alvo = new Vector2(Mathf.Sin(az * Mathf.Deg2Rad), Mathf.Cos(az * Mathf.Deg2Rad));
            if (!iniciado) { vetorSuave = alvo; iniciado = true; }
            float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, suavizacao));
            vetorSuave = Vector2.Lerp(vetorSuave, alvo, k);
            Azimute = GeoMath.Normalizar360(Mathf.Atan2(vetorSuave.x, vetorSuave.y) * Mathf.Rad2Deg);

            Valido = Qualidade > 0.45f;
        }

        void AutoVerificarSinal(Vector3 cima, Vector3 norte, Vector3 leste)
        {
            if (Mathf.Abs(cima.z) < 0.7f) return;
            float headingSistema = Input.compass.magneticHeading;
            if (headingSistema == 0f) return;

            float azTopo = Mathf.Atan2(leste.y, norte.y) * Mathf.Rad2Deg;
            float dif = Mathf.Abs(Mathf.DeltaAngle(azTopo, headingSistema));
            if (dif < 45f) votosNormal++;
            else if (dif > 135f) votosInvertido++;

            if (votosNormal + votosInvertido > 400) { votosNormal /= 2; votosInvertido /= 2; }
        }

        void AtualizarDeclinacao()
        {
            if (Input.location.status != LocationServiceStatus.Running) return;
            float d = Mathf.DeltaAngle(Input.compass.magneticHeading, Input.compass.trueHeading);
            if (Mathf.Abs(d) > 1f && Mathf.Abs(d) < 45f)
                Declinacao = Mathf.Lerp(Declinacao, d, 0.05f);
        }
    }
}
