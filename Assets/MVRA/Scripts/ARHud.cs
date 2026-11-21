using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace MVRA
{
    [DefaultExecutionOrder(200)]
    public class ARHud : MonoBehaviour
    {
        public GeoARManager geo;
        public LocationProvider gps;
        public HeadingProvider bussola;

        [Header("Bússola em fita")]
        [Tooltip("Graus visíveis de cada lado do centro da fita.")]
        public float meiaFaixaFita = 60f;

        [Header("Foco")]
        [Tooltip("Ângulo (graus) entre a mira e o prédio para ele entrar em foco.")]
        public float anguloFoco = 9f;
        [Tooltip("Velocidade média de caminhada para estimar o tempo a pé (m/s).")]
        public float velocidadeCaminhada = 1.25f;

        Canvas canvas;
        RectTransform raiz, seguro, camadaCena;
        float alturaTopo = 250f;

        RectTransform fita;
        float pxPorGrau;
        readonly List<(RectTransform rt, float az)> marcasFita = new List<(RectTransform, float)>();
        TextMeshProUGUI txtRumo, txtGps, txtModo;
        Image pontoGps;

        class Rotulo
        {
            public GeoPinView pin;
            public RectTransform cartao;
            public Image fundo;
            public Image icone;
            public TextMeshProUGUI texto;
            public RectTransform conector;
            public Image feixe, brilho;
            public RectTransform marcaFita;
            public TextMeshProUGUI siglaFita;
            public RectTransform seta;
        }
        readonly List<Rotulo> rotulos = new List<Rotulo>();
        GeoPinView focado;

        GameObject painelMapa, painelCalib, barraInferior;
        RectTransform areaMapa, usuarioMapa, pulsoMapa;
        TextMeshProUGUI txtMapaStatus, txtAlvo, txtDica, txtToast;
        RectTransform pilulaDica, pilulaToast;
        GameObject mira;
        int alvoCalib;
        float toastAte;
        Vector2 mapaMin, mapaMax;
        Rect ultimaSafe;

        void Start()
        {
            if (geo == null) geo = FindObjectOfType<GeoARManager>();
            if (gps == null) gps = FindObjectOfType<LocationProvider>();
            if (bussola == null) bussola = FindObjectOfType<HeadingProvider>();

            GarantirEventSystem();
            MontarCanvas();
        }

        static void GarantirEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        void MontarCanvas()
        {
            var go = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            raiz = (RectTransform)go.transform;

            camadaCena = UiKit.NovoRect("Cena", raiz);
            UiKit.Esticar(camadaCena);

            seguro = UiKit.NovoRect("AreaSegura", raiz);
            AplicarAreaSegura();

            MontarFita();
            MontarRotulos();
            MontarDicas();
            MontarBarraInferior();
            MontarCalibracao();
            MontarMapa();

            painelMapa.SetActive(true);
        }

        void AplicarAreaSegura()
        {
            Rect s = Screen.safeArea;
            ultimaSafe = s;
            seguro.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            seguro.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
            seguro.offsetMin = seguro.offsetMax = Vector2.zero;
        }

        void MontarFita()
        {
            var bloco = UiKit.NovoRect("Topo", seguro);
            UiKit.Ancorar(bloco, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(-48, 200));

            var fundo = UiKit.Painel("FundoFita", bloco, UiKit.Vidro);
            UiKit.Ancorar(fundo.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 118));

            fita = UiKit.NovoRect("Fita", fundo.transform);
            UiKit.Esticar(fita, 6);
            fita.gameObject.AddComponent<RectMask2D>();

            pxPorGrau = 470f / meiaFaixaFita;

            for (int az = 0; az < 360; az += 5)
            {
                bool cardeal = az % 45 == 0;
                bool medio = az % 15 == 0;
                var marca = UiKit.NovoRect("Marca" + az, fita);
                marca.anchorMin = marca.anchorMax = new Vector2(0.5f, 0);
                marca.pivot = new Vector2(0.5f, 0);
                marca.sizeDelta = new Vector2(80, 100);

                var traco = UiKit.Imagem("Traco", marca, null, cardeal ? UiKit.CorTexto : new Color(1, 1, 1, medio ? 0.55f : 0.25f));
                UiKit.Ancorar(traco.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                              new Vector2(0, 10), new Vector2(cardeal ? 4 : 3, cardeal ? 26 : (medio ? 18 : 10)));

                if (cardeal || medio)
                {
                    string rotulo = cardeal ? GeoMath.PontoCardeal(az) : az.ToString();
                    var t = UiKit.Texto("Rotulo", marca, rotulo, cardeal ? 34 : 22, TextAlignmentOptions.Center, cardeal ? FontStyles.Bold : FontStyles.Normal);
                    t.color = az == 0 ? UiKit.Ambar : (cardeal ? UiKit.CorTexto : UiKit.TextoSuave);
                    UiKit.Ancorar(t.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(80, 40));
                }
                marcasFita.Add((marca, az));
            }

            var ponteiro = UiKit.Imagem("Ponteiro", fundo.transform, UiKit.Seta, UiKit.Ambar);
            UiKit.Ancorar(ponteiro.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, -2), new Vector2(30, 22));

            var pilulaRumo = UiKit.Painel("Rumo", bloco, UiKit.Vidro);
            UiKit.Ancorar(pilulaRumo.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(230, 58));
            txtRumo = UiKit.Texto("Txt", pilulaRumo.transform, "--", 30, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Esticar(txtRumo.rectTransform);

            var pilulaGps = UiKit.Painel("Gps", bloco, UiKit.Vidro);
            UiKit.Ancorar(pilulaGps.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -130), new Vector2(250, 58));
            pontoGps = UiKit.Imagem("Ponto", pilulaGps.transform, UiKit.Circulo, Color.gray);
            UiKit.Ancorar(pontoGps.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(18, 18));
            txtGps = UiKit.Texto("Txt", pilulaGps.transform, "GPS", 26, TextAlignmentOptions.Left);
            UiKit.Ancorar(txtGps.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(26, 0), new Vector2(-62, 0));

            var pilulaModo = UiKit.Painel("Modo", bloco, UiKit.Vidro);
            UiKit.Ancorar(pilulaModo.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -130), new Vector2(250, 58));
            txtModo = UiKit.Texto("Txt", pilulaModo.transform, "", 26, TextAlignmentOptions.Center);
            UiKit.Esticar(txtModo.rectTransform);
        }

        void MontarRotulos()
        {
            foreach (var pin in geo.Pins)
            {
                var r = new Rotulo { pin = pin };

                r.feixe = UiKit.Imagem("Feixe – " + pin.Ponto.nome, camadaCena, UiKit.Degrade, UiKit.Vermelho);
                r.feixe.preserveAspect = false;
                r.brilho = UiKit.Imagem("Brilho", camadaCena, UiKit.Circulo, new Color(UiKit.Vermelho.r, UiKit.Vermelho.g, UiKit.Vermelho.b, 0.45f));
                r.brilho.preserveAspect = false;

                r.conector = UiKit.Imagem("Conector", camadaCena, null, new Color(1, 1, 1, 0.55f)).rectTransform;

                r.fundo = UiKit.Painel("Cartao – " + pin.Ponto.nome, camadaCena, UiKit.Vidro);
                r.cartao = r.fundo.rectTransform;
                r.cartao.pivot = new Vector2(0.5f, 0f);
                r.icone = UiKit.Imagem("Icone", r.cartao, UiKit.Pin, UiKit.Vermelho);
                r.texto = UiKit.Texto("Texto", r.cartao, "", 34, TextAlignmentOptions.Left);
                r.texto.rectTransform.pivot = new Vector2(0, 0.5f);

                r.seta = UiKit.Imagem("Seta", r.cartao, UiKit.Seta, UiKit.Ambar).rectTransform;
                r.seta.sizeDelta = new Vector2(26, 22);

                r.marcaFita = UiKit.NovoRect("Fita – " + pin.Ponto.nome, fita);
                r.marcaFita.anchorMin = r.marcaFita.anchorMax = new Vector2(0.5f, 0);
                r.marcaFita.pivot = new Vector2(0.5f, 0);
                r.marcaFita.sizeDelta = new Vector2(90, 100);
                var ic = UiKit.Imagem("Pin", r.marcaFita, UiKit.Pin, UiKit.Vermelho);
                UiKit.Ancorar(ic.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 44), new Vector2(30, 39));
                r.siglaFita = UiKit.Texto("Sigla", r.marcaFita, Sigla(pin.Ponto), 20, TextAlignmentOptions.Center, FontStyles.Bold);
                UiKit.Ancorar(r.siglaFita.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(90, 28));

                rotulos.Add(r);
            }
        }

        void MontarDicas()
        {
            var dica = UiKit.Painel("Dica", seguro, UiKit.VidroForte);
            pilulaDica = dica.rectTransform;
            UiKit.Ancorar(pilulaDica, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(760, 150));
            txtDica = UiKit.Texto("Txt", pilulaDica, "", 32, TextAlignmentOptions.Center);
            txtDica.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Esticar(txtDica.rectTransform, 24);

            var toast = UiKit.Painel("Toast", seguro, UiKit.Ambar);
            pilulaToast = toast.rectTransform;
            UiKit.Ancorar(pilulaToast, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 300), new Vector2(600, 80));
            txtToast = UiKit.Texto("Txt", pilulaToast, "", 30, TextAlignmentOptions.Center, FontStyles.Bold);
            txtToast.color = new Color(0.1f, 0.08f, 0.02f);
            UiKit.Esticar(txtToast.rectTransform);
            pilulaToast.gameObject.SetActive(false);
        }

        void MontarBarraInferior()
        {
            var barra = UiKit.Painel("BarraInferior", seguro, UiKit.Vidro);
            barraInferior = barra.gameObject;
            UiKit.Ancorar(barra.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(560, 116));

            var bMapa = UiKit.Botao("Mapa", barra.transform, "Mapa", new Color(1, 1, 1, 0.08f), UiKit.CorTexto, 32, () => painelMapa.SetActive(true));
            UiKit.Ancorar((RectTransform)bMapa.transform, new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ((RectTransform)bMapa.transform).offsetMin = new Vector2(12, 12);
            ((RectTransform)bMapa.transform).offsetMax = new Vector2(-6, -12);

            var bCal = UiKit.Botao("Calibrar", barra.transform, "Calibrar", new Color(1, 1, 1, 0.08f), UiKit.CorTexto, 32, () => painelCalib.SetActive(!painelCalib.activeSelf));
            UiKit.Ancorar((RectTransform)bCal.transform, new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ((RectTransform)bCal.transform).offsetMin = new Vector2(6, 12);
            ((RectTransform)bCal.transform).offsetMax = new Vector2(-12, -12);
        }

        void MontarCalibracao()
        {
            var p = UiKit.Painel("PainelCalibracao", seguro, UiKit.VidroForte);
            painelCalib = p.gameObject;
            p.raycastTarget = true;
            var rt = p.rectTransform;
            UiKit.Ancorar(rt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 170), new Vector2(-48, 600));

            var titulo = UiKit.Texto("Titulo", rt, "Calibrar direção", 40, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Ancorar(titulo.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(40, -34), new Vector2(-80, 50));
            var instr = UiKit.Texto("Instrucao", rt,
                $"Escolha um prédio que você está vendo, coloque o centro dele na <color={UiKit.HexAmbar}>mira</color> e toque em <b>Alinhar</b>.",
                28, TextAlignmentOptions.TopLeft);
            instr.color = UiKit.TextoSuave;
            instr.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Ancorar(instr.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(40, -96), new Vector2(-80, 80));

            var seletor = UiKit.Painel("Seletor", rt, new Color(1, 1, 1, 0.06f));
            UiKit.Ancorar(seletor.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(-64, 110));
            var bAnt = UiKit.Botao("Anterior", seletor.transform, "<", new Color(1, 1, 1, 0.10f), UiKit.CorTexto, 44, () => TrocarAlvo(-1));
            UiKit.Ancorar((RectTransform)bAnt.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(110, 90));
            var bProx = UiKit.Botao("Proximo", seletor.transform, ">", new Color(1, 1, 1, 0.10f), UiKit.CorTexto, 44, () => TrocarAlvo(1));
            UiKit.Ancorar((RectTransform)bProx.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(110, 90));
            txtAlvo = UiKit.Texto("Alvo", seletor.transform, "", 36, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Esticar(txtAlvo.rectTransform);
            txtAlvo.rectTransform.offsetMin = new Vector2(130, 0);
            txtAlvo.rectTransform.offsetMax = new Vector2(-130, 0);

            var bMenos = UiKit.Botao("Menos", rt, "-2°", new Color(1, 1, 1, 0.10f), UiKit.CorTexto, 34, () => { geo.AjusteFino(-2f); Toast("Girado -2°"); });
            var bAlinhar = UiKit.Botao("Alinhar", rt, "Alinhar", UiKit.Vermelho, Color.white, 38, AlinharComAlvo);
            var bMais = UiKit.Botao("Mais", rt, "+2°", new Color(1, 1, 1, 0.10f), UiKit.CorTexto, 34, () => { geo.AjusteFino(2f); Toast("Girado +2°"); });
            Linha(new[] { (RectTransform)bMenos.transform, (RectTransform)bAlinhar.transform, (RectTransform)bMais.transform },
                  new[] { 0.22f, 0.56f, 0.22f }, -326, 110);

            var bAuto = UiKit.Botao("Auto", rt, "Bússola automática", new Color(1, 1, 1, 0.06f), UiKit.TextoSuave, 28, () => { geo.VoltarBussolaAutomatica(); Toast("Usando a bússola do celular"); });
            var bGps = UiKit.Botao("Gps", rt, "Recentrar GPS", new Color(1, 1, 1, 0.06f), UiKit.TextoSuave, 28, () => { geo.ResetarPosicaoGps(); Toast("Posição = última leitura do GPS"); });
            Linha(new[] { (RectTransform)bAuto.transform, (RectTransform)bGps.transform }, new[] { 0.5f, 0.5f }, -456, 96);

            mira = UiKit.NovoRect("Mira", raiz).gameObject;
            var mrt = (RectTransform)mira.transform;
            UiKit.Ancorar(mrt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110));
            var anel = UiKit.Imagem("Anel", mrt, UiKit.Anel, UiKit.Ambar);
            UiKit.Esticar(anel.rectTransform);
            var centro = UiKit.Imagem("Centro", mrt, UiKit.Circulo, UiKit.Ambar);
            UiKit.Ancorar(centro.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));

            painelCalib.SetActive(false);
            TrocarAlvo(0);
        }

        static void Linha(RectTransform[] itens, float[] pesos, float y, float altura)
        {
            float margem = 32f, gap = 14f, x = 0f;
            for (int i = 0; i < itens.Length; i++)
            {
                var rt = itens[i];
                rt.anchorMin = new Vector2(x, 1);
                rt.anchorMax = new Vector2(x + pesos[i], 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.offsetMin = new Vector2(i == 0 ? margem : gap * 0.5f, y - altura);
                rt.offsetMax = new Vector2(i == itens.Length - 1 ? -margem : -gap * 0.5f, y);
                x += pesos[i];
            }
        }

        void MontarMapa()
        {
            var p = UiKit.Painel("PainelMapa", raiz, UiKit.AzulEscuro, UiKit.ArredondadoPequeno);
            painelMapa = p.gameObject;
            p.raycastTarget = true;
            UiKit.Esticar(p.rectTransform);

            Rect s = Screen.safeArea;
            float topoSeguro = (Screen.height - s.yMax) / canvas.scaleFactor;

            var faixa = UiKit.Imagem("Cabecalho", p.transform, null, UiKit.AzulAnchieta);
            faixa.preserveAspect = false;
            UiKit.Ancorar(faixa.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 190 + topoSeguro));
            var amarelo = UiKit.Imagem("Filete", faixa.transform, null, UiKit.Ambar);
            amarelo.preserveAspect = false;
            UiKit.Ancorar(amarelo.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 10));

            var marca = UiKit.Texto("Marca", faixa.transform, "<b>Campus UniAnchieta</b>", 58, TextAlignmentOptions.BottomLeft);
            UiKit.Ancorar(marca.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(48, 72), new Vector2(-96, 80));
            var subMarca = UiKit.Texto("Sub", faixa.transform, "Mapa do campus", 30, TextAlignmentOptions.BottomLeft);
            subMarca.color = UiKit.TextoSuave;
            UiKit.Ancorar(subMarca.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(50, 30), new Vector2(-100, 40));

            var conteudo = UiKit.NovoRect("Conteudo", p.transform);
            conteudo.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            conteudo.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
            conteudo.offsetMin = conteudo.offsetMax = Vector2.zero;

            var cartao = UiKit.Painel("Mapa", conteudo, new Color(1, 1, 1, 0.04f), UiKit.ArredondadoPequeno);
            cartao.rectTransform.anchorMin = new Vector2(0, 0);
            cartao.rectTransform.anchorMax = new Vector2(1, 1);
            cartao.rectTransform.offsetMin = new Vector2(32, 300);
            cartao.rectTransform.offsetMax = new Vector2(-32, -220);
            areaMapa = UiKit.NovoRect("Area", cartao.transform);
            UiKit.Esticar(areaMapa, 70);

            var norte = UiKit.Imagem("Norte", cartao.transform, UiKit.Seta, UiKit.Ambar);
            UiKit.Ancorar(norte.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-46, -40), new Vector2(24, 24));
            var tn = UiKit.Texto("N", cartao.transform, "N", 26, TextAlignmentOptions.Center, FontStyles.Bold);
            tn.color = UiKit.Ambar;
            UiKit.Ancorar(tn.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-46, -72), new Vector2(40, 30));

            mapaMin = new Vector2(float.MaxValue, float.MaxValue);
            mapaMax = new Vector2(float.MinValue, float.MinValue);
            foreach (var pin in geo.Pins) { mapaMin = Vector2.Min(mapaMin, pin.ENU); mapaMax = Vector2.Max(mapaMax, pin.ENU); }
            Vector2 folga = (mapaMax - mapaMin) * 0.12f + Vector2.one * 20f;
            mapaMin -= folga; mapaMax += folga;

            foreach (var pin in geo.Pins)
            {
                var ponto = UiKit.NovoRect("Ponto – " + pin.Ponto.nome, areaMapa);
                ponto.anchorMin = ponto.anchorMax = NoMapa(pin.ENU);
                ponto.pivot = new Vector2(0.5f, 0);
                ponto.sizeDelta = new Vector2(44, 57);
                var ic = UiKit.Imagem("Pin", ponto, UiKit.Pin, UiKit.Vermelho);
                UiKit.Esticar(ic.rectTransform);
                var nome = UiKit.Texto("Nome", ponto, NomeCurto(pin.Ponto), 26, TextAlignmentOptions.Center, FontStyles.Bold);
                UiKit.Ancorar(nome.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(300, 34));
            }

            pulsoMapa = UiKit.Imagem("Pulso", areaMapa, UiKit.Anel, UiKit.Ciano).rectTransform;
            usuarioMapa = UiKit.Imagem("Voce", areaMapa, UiKit.Circulo, UiKit.Ciano).rectTransform;
            usuarioMapa.sizeDelta = new Vector2(30, 30);

            txtMapaStatus = UiKit.Texto("Status", conteudo, "", 28, TextAlignmentOptions.Center);
            txtMapaStatus.color = UiKit.TextoSuave;
            UiKit.Ancorar(txtMapaStatus.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(-96, 40));

            var bIniciar = UiKit.Botao("Iniciar", conteudo, "Iniciar", UiKit.Ambar, UiKit.AzulEscuro, 40, () => painelMapa.SetActive(false));
            UiKit.Ancorar((RectTransform)bIniciar.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(-96, 124));
        }

        Vector2 NoMapa(Vector2 enu)
        {
            Vector2 tam = mapaMax - mapaMin;
            float lado = Mathf.Max(tam.x, tam.y);
            Vector2 centro = (mapaMin + mapaMax) * 0.5f;
            Vector2 n = (enu - centro) / lado + new Vector2(0.5f, 0.5f);
            return n;
        }

        void TrocarAlvo(int delta)
        {
            if (geo.Pins.Count == 0) return;
            alvoCalib = (alvoCalib + delta + geo.Pins.Count) % geo.Pins.Count;
            txtAlvo.text = NomeCompleto(geo.Pins[alvoCalib].Ponto);
        }

        void AlinharComAlvo()
        {
            if (geo.CalibrarCom(alvoCalib)) Toast("Alinhado com " + geo.Pins[alvoCalib].Ponto.nome);
            else Toast("Aguarde o GPS antes de calibrar");
        }

        void Toast(string s, float dur = 2.2f)
        {
            txtToast.text = s;
            pilulaToast.sizeDelta = new Vector2(txtToast.GetPreferredValues(s).x + 70, 80);
            toastAte = Time.time + dur;
        }

        void LateUpdate()
        {
            if (canvas == null) return;
            if (Screen.safeArea != ultimaSafe) AplicarAreaSegura();

            bool mapa = painelMapa.activeSelf;
            barraInferior.SetActive(!mapa);
            mira.SetActive(painelCalib.activeSelf && !mapa);
            pilulaToast.gameObject.SetActive(Time.time < toastAte);

            AtualizarStatus();
            AtualizarFita();
            AtualizarFoco();
            AtualizarCartoes();
            if (mapa) AtualizarMapa();
        }

        void AtualizarStatus()
        {
            float az = geo.AzimuteCamera;
            txtRumo.text = geo.NorteOk ? $"{az:0}°  <color={UiKit.HexAmbar}>{GeoMath.PontoCardeal(az)}</color>" : "--";

            if (gps.TemFix)
            {
                txtGps.text = $"GPS ±{gps.Precisao:0} m";
                pontoGps.color = gps.Precisao <= 8f ? new Color(0.3f, 0.9f, 0.5f) : (gps.Precisao <= 20f ? UiKit.Ambar : UiKit.Vermelho);
            }
            else
            {
                txtGps.text = "GPS...";
                pontoGps.color = Color.gray;
            }
            txtModo.text = geo.Calibrado ? $"<color={UiKit.HexAmbar}>Calibrado</color>" : (bussola.Valido ? "Bússola" : "Bússola --");

            string dica = "";
            bool rastreando = Application.isEditor || ARSession.state == ARSessionState.SessionTracking;
            if (!Application.isEditor && ARSession.state == ARSessionState.Unsupported) dica = "Este celular não suporta ARCore";
            else if (!rastreando) dica = "Mova o celular devagar para iniciar o rastreamento";
            else if (!gps.TemFix) dica = gps.Status;
            else if (!geo.NorteOk) dica = "Segure o celular em pé";
            bool mostrar = dica.Length > 0 && !painelMapa.activeSelf;
            pilulaDica.gameObject.SetActive(mostrar);
            if (mostrar) txtDica.text = dica;
        }

        void AtualizarFita()
        {
            float az = geo.AzimuteCamera;
            float limite = meiaFaixaFita * pxPorGrau;
            foreach (var (rt, a) in marcasFita)
            {
                float x = Mathf.DeltaAngle(az, a) * pxPorGrau;
                bool vis = Mathf.Abs(x) <= limite + 40;
                if (rt.gameObject.activeSelf != vis) rt.gameObject.SetActive(vis);
                if (vis) rt.anchoredPosition = new Vector2(x, 0);
            }

            foreach (var r in rotulos)
            {
                bool vis = geo.Pronto;
                r.marcaFita.gameObject.SetActive(vis);
                if (!vis) continue;
                float d = Mathf.DeltaAngle(az, r.pin.Azimute);
                bool dentro = Mathf.Abs(d) <= meiaFaixaFita;
                float x = Mathf.Clamp(d, -meiaFaixaFita, meiaFaixaFita) * pxPorGrau;
                r.marcaFita.anchoredPosition = new Vector2(x, 0);
                float s = r.pin == focado ? 1.25f : 1f;
                r.marcaFita.localScale = Vector3.one * s;
                var cg = r.marcaFita.GetComponentsInChildren<Graphic>();
                foreach (var g in cg) { var c = g.color; c.a = dentro ? 1f : 0.45f; g.color = c; }
                r.marcaFita.SetAsLastSibling();
            }
        }

        void AtualizarFoco()
        {
            GeoPinView melhor = null;
            float melhorAng = float.MaxValue;
            float az = geo.AzimuteCamera;
            if (geo.Pronto)
            {
                foreach (var r in rotulos)
                {
                    float ang = Mathf.Abs(Mathf.DeltaAngle(az, r.pin.Azimute));
                    float limite = r.pin == focado ? anguloFoco * 1.5f : anguloFoco;
                    if (ang < limite && ang < melhorAng) { melhor = r.pin; melhorAng = ang; }
                }
            }
            focado = melhor;
            foreach (var r in rotulos) r.pin.Focado = r.pin == focado;
        }

        void AtualizarCartoes()
        {
            Camera cam = geo.arCamera;
            float esc = canvas.scaleFactor;
            float w = Screen.width, h = Screen.height;
            Rect sa = Screen.safeArea;
            float topoLivre = sa.yMax - alturaTopo * esc;
            float baseLivre = sa.yMin + 170f * esc;
            float margem = 24f * esc;
            int foraEsq = 0, foraDir = 0;
            var ocupados = new List<Rect>();

            rotulos.Sort((a, b) =>
            {
                if (a == b) return 0;
                if (a.pin == focado) return -1;
                if (b.pin == focado) return 1;
                return a.pin.Distancia.CompareTo(b.pin.Distancia);
            });

            foreach (var r in rotulos)
            {
                bool ativo = geo.Pronto && !painelMapa.activeSelf;
                r.cartao.gameObject.SetActive(ativo);
                if (!ativo) { r.feixe.gameObject.SetActive(false); r.brilho.gameObject.SetActive(false); r.conector.gameObject.SetActive(false); continue; }

                Vector3 spCorpo = cam.WorldToScreenPoint(r.pin.transform.position + Vector3.up * r.pin.Escala * 0.6f);
                Vector3 spTopo = cam.WorldToScreenPoint(r.pin.TopoMundo);
                Vector3 spPonta = cam.WorldToScreenPoint(r.pin.transform.position);
                Vector3 spBase = cam.WorldToScreenPoint(r.pin.BaseMundo);
                bool naTela = spCorpo.z > 0 && spCorpo.x > 0 && spCorpo.x < w && spCorpo.y > baseLivre * 0.5f && spCorpo.y < topoLivre;
                bool emFoco = r.pin == focado && naTela;

                bool feixeOk = naTela && spBase.z > 0 && spPonta.y > spBase.y + 4;
                r.feixe.gameObject.SetActive(feixeOk);
                r.brilho.gameObject.SetActive(feixeOk);
                if (feixeOk)
                {
                    UiKit.Segmento(r.feixe.rectTransform, spBase, spPonta, (emFoco ? 14f : 8f));
                    var cf = UiKit.Vermelho; cf.a = emFoco ? 0.85f : 0.5f; r.feixe.color = cf;
                    r.brilho.rectTransform.position = spBase;
                    float tb = (emFoco ? 70f : 44f) + Mathf.Sin(Time.time * 3f) * 6f;
                    r.brilho.rectTransform.sizeDelta = new Vector2(tb, tb * 0.35f);
                }

                string nome = r.pin.Ponto.nome;
                string dist = GeoMath.FormatarDistancia(r.pin.Distancia);
                Vector2 tam;
                r.seta.gameObject.SetActive(!naTela);

                if (emFoco)
                {
                    int min = Mathf.Max(1, Mathf.RoundToInt(r.pin.Distancia / velocidadeCaminhada / 60f));
                    string s = $"<size=46><b>{nome}</b></size>";
                    if (!string.IsNullOrEmpty(r.pin.Ponto.subtitulo)) s += $"\n<size=28><color={UiKit.HexSuave}>{r.pin.Ponto.subtitulo}</color></size>";
                    if (!string.IsNullOrEmpty(r.pin.Ponto.descricao)) s += $"\n<size=27>{r.pin.Ponto.descricao}</size>";
                    s += $"\n<size=30><color={UiKit.HexAmbar}><b>{dist}</b></color>    {min} min a pé    {GeoMath.PontoCardeal(r.pin.Azimute)}</size>";
                    r.texto.textWrappingMode = TextWrappingModes.Normal;
                    r.texto.alignment = TextAlignmentOptions.Left;
                    r.texto.text = s;
                    Vector2 pref = r.texto.GetPreferredValues(s, 560f, 0f);
                    tam = new Vector2(Mathf.Min(pref.x, 560f) + 120f, pref.y + 44f);
                    r.fundo.color = UiKit.VidroForte;
                    LayoutIcone(r, tam, 46f);
                }
                else if (naTela)
                {
                    string s = $"<b>{nome}</b>  <color={UiKit.HexAmbar}>{dist}</color>";
                    r.texto.textWrappingMode = TextWrappingModes.NoWrap;
                    r.texto.alignment = TextAlignmentOptions.Left;
                    r.texto.fontSize = 32;
                    r.texto.text = s;
                    Vector2 pref = r.texto.GetPreferredValues(s);
                    tam = new Vector2(pref.x + 90f, 70f);
                    r.fundo.color = UiKit.Vidro;
                    LayoutIcone(r, tam, 30f);
                }
                else
                {
                    Vector3 local = cam.transform.InverseTransformPoint(r.pin.transform.position);
                    bool direita = local.x >= 0;
                    string s = $"<b>{nome}</b>  <color={UiKit.HexAmbar}>{dist}</color>";
                    r.texto.textWrappingMode = TextWrappingModes.NoWrap;
                    r.texto.fontSize = 28;
                    r.texto.text = s;
                    Vector2 pref = r.texto.GetPreferredValues(s);
                    tam = new Vector2(pref.x + 110f, 62f);
                    r.fundo.color = new Color(UiKit.Vidro.r, UiKit.Vidro.g, UiKit.Vidro.b, 0.6f);
                    LayoutIcone(r, tam, 26f, direita);
                    int k = direita ? foraDir++ : foraEsq++;
                    spTopo = new Vector3(direita ? w : 0, h * 0.56f - k * 78f * esc, 1);
                }

                r.cartao.sizeDelta = tam;
                float meia = tam.x * esc * 0.5f;
                Vector2 pos = new Vector2(Mathf.Clamp(spTopo.x, meia + margem, w - meia - margem), spTopo.y + 10f * esc);

                if (naTela)
                {
                    pos.y = Mathf.Clamp(pos.y, baseLivre, topoLivre - tam.y * esc);
                    Rect ret = new Rect(pos.x - meia, pos.y, tam.x * esc, tam.y * esc);
                    for (int tent = 0; tent < 8; tent++)
                    {
                        bool colidiu = false;
                        foreach (var o in ocupados)
                        {
                            if (!o.Overlaps(ret)) continue;
                            float cima = o.yMax + 8 * esc;
                            ret.y = cima + ret.height <= topoLivre ? cima : o.yMin - ret.height - 8 * esc;
                            colidiu = true;
                        }
                        if (!colidiu) break;
                    }
                    ocupados.Add(ret);
                    pos = new Vector2(ret.center.x, ret.y);
                }

                r.cartao.position = pos;

                Vector2 alvo = spTopo;
                bool conectar = naTela && Vector2.Distance(pos, alvo) > 30f * esc;
                r.conector.gameObject.SetActive(conectar);
                if (conectar) UiKit.Segmento(r.conector, alvo, pos, 3f);
            }

            foreach (var r in rotulos) { r.feixe.transform.SetAsFirstSibling(); r.brilho.transform.SetAsFirstSibling(); }
            for (int i = rotulos.Count - 1; i >= 0; i--) rotulos[i].cartao.SetAsLastSibling();
        }

        static void LayoutIcone(Rotulo r, Vector2 tam, float tamIcone, bool direita = false)
        {
            var ic = r.icone.rectTransform;
            ic.anchorMin = ic.anchorMax = new Vector2(0, 0.5f);
            ic.pivot = new Vector2(0, 0.5f);
            ic.sizeDelta = new Vector2(tamIcone, tamIcone * 1.3f);
            ic.anchoredPosition = new Vector2(24, 0);

            var t = r.texto.rectTransform;
            t.anchorMin = t.anchorMax = new Vector2(0, 0.5f);
            t.pivot = new Vector2(0, 0.5f);
            float xTexto = 24 + tamIcone + 18;
            t.anchoredPosition = new Vector2(xTexto, 0);
            t.sizeDelta = new Vector2(tam.x - xTexto - 24, tam.y - 20);

            if (r.seta.gameObject.activeSelf)
            {
                r.seta.anchorMin = r.seta.anchorMax = new Vector2(direita ? 1 : 0, 0.5f);
                r.seta.anchoredPosition = new Vector2(direita ? -22 : 22, 0);
                r.seta.localRotation = Quaternion.Euler(0, 0, direita ? -90 : 90);
                if (!direita)
                {
                    ic.anchoredPosition = new Vector2(48, 0);
                    t.anchoredPosition = new Vector2(48 + tamIcone + 14, 0);
                }
            }
        }

        void AtualizarMapa()
        {
            bool mostrar = gps.TemFix && geo.PosicaoOk;
            Vector2 n = NoMapa(geo.PosicaoUsuarioENU);
            bool dentro = mostrar && n.x >= 0 && n.x <= 1 && n.y >= 0 && n.y <= 1;
            usuarioMapa.gameObject.SetActive(dentro);
            pulsoMapa.gameObject.SetActive(dentro);
            if (dentro)
            {
                usuarioMapa.anchorMin = usuarioMapa.anchorMax = n;
                usuarioMapa.anchoredPosition = Vector2.zero;
                pulsoMapa.anchorMin = pulsoMapa.anchorMax = n;
                pulsoMapa.anchoredPosition = Vector2.zero;
                float t = Mathf.Repeat(Time.time, 1.6f) / 1.6f;
                pulsoMapa.sizeDelta = Vector2.one * Mathf.Lerp(30f, 110f, t);
                var c = UiKit.Ciano; c.a = 1f - t; pulsoMapa.GetComponent<Image>().color = c;
            }

            if (!gps.TemFix) txtMapaStatus.text = gps.Status;
            else
            {
                Vector2 centro = (mapaMin + mapaMax) * 0.5f;
                float d = Vector2.Distance(geo.PosicaoUsuarioENU, centro);
                txtMapaStatus.text = dentro
                    ? $"Você está no campus (GPS ±{gps.Precisao:0} m)"
                    : $"Você está a {GeoMath.FormatarDistancia(d)} do campus";
            }
        }

        static string NomeCompleto(GeoPoint p) => string.IsNullOrEmpty(p.subtitulo) ? p.nome : p.nome + " - " + p.subtitulo;

        static string NomeCurto(GeoPoint p)
        {
            if (p.nome == "Prédio Administrativo") return "Administrativo";
            return p.nome;
        }

        static string Sigla(GeoPoint p)
        {
            string n = p.nome.Trim();
            if (n.StartsWith("Prédio ") && n.Length <= 9) return "P" + n.Substring(7);
            if (n.StartsWith("Prédio Adm")) return "ADM";
            if (n.StartsWith("Anfi")) return "ANF";
            if (n.StartsWith("Bibli")) return "BIB";
            return n.Length <= 3 ? n.ToUpper() : n.Substring(0, 3).ToUpper();
        }
    }
}
