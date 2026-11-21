using UnityEngine;

namespace MVRA
{
    public class GeoPinView : MonoBehaviour
    {
        public GeoPoint Ponto { get; private set; }
        public Vector2 ENU { get; private set; }
        public float Distancia { get; set; }
        public float Azimute { get; set; }
        public float Escala { get; private set; } = 1f;
        public Vector3 BaseMundo { get; set; }
        public bool Focado { get; set; }
        float fatorFoco = 1f;

        public Vector3 TopoMundo => transform.position + Vector3.up * (Escala * 1.06f);

        float fase;

        public static GeoPinView Criar(GeoPoint ponto, Vector2 enu, GameObject modelo, Transform pai)
        {
            var raiz = new GameObject("Pin – " + ponto.nome + (string.IsNullOrEmpty(ponto.subtitulo) ? "" : " (" + ponto.subtitulo + ")"));
            raiz.transform.SetParent(pai, false);
            raiz.transform.position = Vector3.zero;
            raiz.transform.rotation = Quaternion.identity;

            GameObject visual = modelo != null ? Instantiate(modelo) : CriarPinProcedural();
            visual.name = "Modelo";
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            Normalizar(visual.transform);
            AplicarCor(visual, ponto.cor);
            foreach (var c in visual.GetComponentsInChildren<Collider>()) Destroy(c);

            var view = raiz.AddComponent<GeoPinView>();
            view.Ponto = ponto;
            view.ENU = enu;
            view.fase = Random.value * 10f;
            return view;
        }

        static void Normalizar(Transform visual)
        {
            Bounds b = CalcularBounds(visual);
            float altura = b.size.y > 1e-5f ? b.size.y : 1f;
            visual.localScale *= 1f / altura;

            b = CalcularBounds(visual);
            visual.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
        }

        static Bounds CalcularBounds(Transform t)
        {
            var rends = t.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return new Bounds(t.position, Vector3.one);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        static void AplicarCor(GameObject go, Color cor)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in r.materials)
                {
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", cor);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", cor);
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", cor * 0.35f);
                    }
                }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        static GameObject CriarPinProcedural()
        {
            var root = new GameObject("PinProcedural");
            var cabeca = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cabeca.transform.SetParent(root.transform, false);
            cabeca.transform.localPosition = new Vector3(0, 0.7f, 0);
            cabeca.transform.localScale = Vector3.one * 0.6f;
            var haste = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            haste.transform.SetParent(root.transform, false);
            haste.transform.localPosition = new Vector3(0, 0.25f, 0);
            haste.transform.localScale = new Vector3(0.12f, 0.25f, 0.12f);
            return root;
        }

        public void Posicionar(Vector3 posicao, float escala, Transform cam)
        {
            float alvo = Focado ? 1.18f + Mathf.Sin(Time.time * 4f) * 0.03f : 0.88f;
            fatorFoco = Mathf.Lerp(fatorFoco, alvo, 1f - Mathf.Exp(-Time.deltaTime * 8f));
            escala *= fatorFoco;
            Escala = escala;
            float flutuar = Mathf.Sin(Time.time * 2f + fase) * 0.04f * escala;
            transform.position = posicao + Vector3.up * flutuar;
            transform.localScale = Vector3.one * escala;

            Vector3 olhar = cam.position - transform.position;
            olhar.y = 0f;
            if (olhar.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(-olhar.normalized, Vector3.up);
        }
    }
}
