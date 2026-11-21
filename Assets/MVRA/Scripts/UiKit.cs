using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MVRA
{
    public static class UiKit
    {
        public static readonly Color AzulAnchieta = new Color(0.05f, 0.25f, 0.55f, 1f);
        public static readonly Color AzulEscuro = new Color(0.02f, 0.10f, 0.25f, 1f);
        public static readonly Color Vidro = new Color(0.02f, 0.11f, 0.27f, 0.80f);
        public static readonly Color VidroForte = new Color(0.02f, 0.09f, 0.22f, 0.94f);
        public static readonly Color Linha = new Color(1f, 1f, 1f, 0.12f);
        public static readonly Color CorTexto = new Color(1f, 1f, 1f, 1f);
        public static readonly Color TextoSuave = new Color(0.74f, 0.81f, 0.93f, 1f);
        public static readonly Color Vermelho = new Color(0.86f, 0.13f, 0.15f, 1f);
        public static readonly Color Ambar = new Color(0.98f, 0.77f, 0.10f, 1f);
        public static readonly Color Ciano = new Color(0.35f, 0.80f, 1f, 1f);

        public const string HexAmbar = "#FAC51A";
        public const string HexSuave = "#BDCFED";

        static Sprite sArredondado, sArredondadoP, sCirculo, sPin, sSeta, sDegrade, sAnel;

        public static Sprite Arredondado => sArredondado ? sArredondado : (sArredondado = CriarArredondado(96, 40));
        public static Sprite ArredondadoPequeno => sArredondadoP ? sArredondadoP : (sArredondadoP = CriarArredondado(48, 18));
        public static Sprite Circulo => sCirculo ? sCirculo : (sCirculo = CriarCirculo(128, 0f));
        public static Sprite Anel => sAnel ? sAnel : (sAnel = CriarCirculo(128, 0.78f));
        public static Sprite Pin => sPin ? sPin : (sPin = CriarPin(128));
        public static Sprite Seta => sSeta ? sSeta : (sSeta = CriarSeta(64));
        public static Sprite Degrade => sDegrade ? sDegrade : (sDegrade = CriarDegrade(8, 128));

        public static RectTransform NovoRect(string nome, Transform pai)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        public static Image Painel(string nome, Transform pai, Color cor, Sprite sprite = null)
        {
            var rt = NovoRect(nome, pai);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? Arredondado;
            img.type = Image.Type.Sliced;
            img.color = cor;
            img.raycastTarget = false;
            return img;
        }

        public static Image Imagem(string nome, Transform pai, Sprite sprite, Color cor)
        {
            var rt = NovoRect(nome, pai);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = cor;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        public static TextMeshProUGUI Texto(string nome, Transform pai, string conteudo, float tamanho,
                                             TextAlignmentOptions alinhamento = TextAlignmentOptions.Center,
                                             FontStyles estilo = FontStyles.Normal)
        {
            var rt = NovoRect(nome, pai);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = conteudo;
            t.fontSize = tamanho;
            t.alignment = alinhamento;
            t.fontStyle = estilo;
            t.color = CorTexto;
            t.richText = true;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        public static Button Botao(string nome, Transform pai, string rotulo, Color fundo, Color corTexto, float tamanho, UnityAction acao)
        {
            var img = Painel(nome, pai, fundo);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            var cores = btn.colors;
            cores.normalColor = Color.white;
            cores.highlightedColor = Color.white;
            cores.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            cores.selectedColor = Color.white;
            btn.colors = cores;
            btn.onClick.AddListener(acao);
            var t = Texto("Rotulo", img.transform, rotulo, tamanho, TextAlignmentOptions.Center, FontStyles.Bold);
            t.color = corTexto;
            Esticar(t.rectTransform);
            return btn;
        }

        public static void Esticar(RectTransform rt, float margem = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margem, margem);
            rt.offsetMax = new Vector2(-margem, -margem);
        }

        public static void Ancorar(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 tam)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = tam;
        }

        public static void Segmento(RectTransform rt, Vector2 a, Vector2 b, float espessuraPx)
        {
            Vector2 d = b - a;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.position = a;
            float escala = rt.lossyScale.x > 0 ? rt.lossyScale.x : 1f;
            rt.sizeDelta = new Vector2(espessuraPx, d.magnitude / escala);
            rt.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(-d.x, d.y) * Mathf.Rad2Deg);
        }

        static Texture2D NovaTextura(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        static Sprite CriarArredondado(int tam, int raio)
        {
            var tex = NovaTextura(tam, tam);
            var px = new Color32[tam * tam];
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, raio, tam - raio);
                    float cy = Mathf.Clamp(y + 0.5f, raio, tam - raio);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    byte a = (byte)(Mathf.Clamp01(raio - d + 0.5f) * 255);
                    px[y * tam + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f, 0,
                                 SpriteMeshType.FullRect, new Vector4(raio, raio, raio, raio));
        }

        static Sprite CriarCirculo(int tam, float furo)
        {
            var tex = NovaTextura(tam, tam);
            float c = tam * 0.5f, r = c - 1f, rf = r * furo;
            var px = new Color32[tam * tam];
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (furo > 0f) a *= Mathf.Clamp01(d - rf + 0.5f);
                    px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f));
        }

        static Sprite CriarPin(int tam)
        {
            int w = tam, h = Mathf.RoundToInt(tam * 1.3f);
            var tex = NovaTextura(w, h);
            float r = w * 0.46f;
            Vector2 centro = new Vector2(w * 0.5f, h - r - 2f);
            Vector2 ponta = new Vector2(w * 0.5f, 2f);
            float furo = r * 0.42f;
            float dist = centro.y - ponta.y;
            float meioAng = Mathf.Asin(Mathf.Clamp01(r / dist));
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dc = Vector2.Distance(p, centro);
                    float a = Mathf.Clamp01(r - dc + 0.5f);
                    Vector2 v = p - ponta;
                    if (v.y > 0f && p.y < centro.y)
                    {
                        float ang = Mathf.Abs(Mathf.Atan2(v.x, v.y));
                        float borda = (meioAng - ang) * v.magnitude;
                        a = Mathf.Max(a, Mathf.Clamp01(borda + 0.5f));
                    }
                    a *= Mathf.Clamp01(dc - furo + 0.5f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f));
        }

        static Sprite CriarSeta(int tam)
        {
            var tex = NovaTextura(tam, tam);
            var px = new Color32[tam * tam];
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++)
                {
                    float fy = (y + 0.5f) / tam;
                    float meia = (1f - fy) * 0.5f;
                    float dx = Mathf.Abs((x + 0.5f) / tam - 0.5f);
                    float a = Mathf.Clamp01((meia - dx) * tam + 0.5f) * Mathf.Clamp01(fy * tam);
                    px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f));
        }

        static Sprite CriarDegrade(int w, int h)
        {
            var tex = NovaTextura(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                byte a = (byte)(Mathf.Pow(t, 1.4f) * 255);
                for (int x = 0; x < w; x++)
                {
                    float bx = 1f - Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * Mathf.Clamp01(bx * 1.5f)));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f));
        }
    }
}
