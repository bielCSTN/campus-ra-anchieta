#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace MVRA.EditorTools
{
    public static class MVRASetup
    {
        const string PastaRaiz = "Assets/MVRA";
        const string CaminhoCena = PastaRaiz + "/Scenes/CampusRA.unity";
        const string CaminhoModelo = PastaRaiz + "/Models/map_pin.fbx";
        const string CaminhoApk = "Builds/CampusRA_Anchieta.apk";

        [MenuItem("MVRA/1. Configurar projeto (Android + ARCore)", priority = 1)]
        public static void ConfigurarProjeto()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.companyName = "Grupo Anchieta";
            PlayerSettings.productName = "Campus RA Anchieta";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "br.anchieta.mvra.campusra");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if ((int)PlayerSettings.Android.minSdkVersion < 24 || (int)PlayerSettings.Android.minSdkVersion > 29)
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = false;

            string xr = AtivarARCore();
            bool precisaReiniciar = AtivarInputBoth();
            AssetDatabase.SaveAssets();

            string msg = "Configuração aplicada:\n\n" +
                         "• Plataforma: Android (IL2CPP, ARM64, API mínima 29, OpenGL ES 3)\n" +
                         "• Orientação: Retrato\n" +
                         "• " + xr + "\n" +
                         (precisaReiniciar
                             ? "\n⚠ 'Active Input Handling' foi alterado para 'Both'. O Unity vai pedir para REINICIAR — aceite."
                             : "• Input Handling: Both (ok)");
            EditorUtility.DisplayDialog("MVRA – Projeto configurado", msg, "OK");
        }

        static string AtivarARCore()
        {
            try
            {
                EditorBuildSettings.TryGetConfigObject("com.unity.xr.management.loader_settings", out UnityEngine.Object porTarget);
                if (porTarget == null)
                    return "XR: abra Project Settings > XR Plug-in Management e marque 'ARCore' na aba Android";

                var mSettings = porTarget.GetType().GetMethod("SettingsForBuildTarget");
                var geral = mSettings?.Invoke(porTarget, new object[] { BuildTargetGroup.Android });
                if (geral == null)
                    return "XR: abra Project Settings > XR Plug-in Management, aba Android, e marque 'ARCore'";

                var manager = geral.GetType().GetProperty("Manager")?.GetValue(geral);
                var store = Type.GetType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore, Unity.XR.Management.Editor");
                var assign = store?.GetMethod("AssignLoader", BindingFlags.Public | BindingFlags.Static);
                if (manager == null || assign == null)
                    return "XR: marque 'ARCore' manualmente em XR Plug-in Management (Android)";

                bool ok = (bool)assign.Invoke(null, new object[] { manager, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android });
                return ok ? "XR Plug-in Management: ARCore ativado" : "XR: confira se 'ARCore' está marcado em XR Plug-in Management (Android)";
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MVRA] Não consegui ativar o ARCore automaticamente: " + e.Message);
                return "XR: marque 'ARCore' manualmente em XR Plug-in Management (Android)";
            }
        }

        static bool AtivarInputBoth()
        {
            var ps = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            var so = new SerializedObject(ps);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue == 2) return false;
            prop.intValue = 2;
            so.ApplyModifiedProperties();
            return true;
        }

        [MenuItem("MVRA/2. Criar cena CampusRA", priority = 2)]
        public static void CriarCena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(PastaRaiz + "/Scenes");
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var luz = new GameObject("Directional Light").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");

            if (UnityEngine.Object.FindObjectOfType<ARSession>() == null)
            {
                var s = new GameObject("AR Session");
                s.AddComponent<ARSession>();
                s.AddComponent<ARInputManager>();
            }

            var origin = UnityEngine.Object.FindObjectOfType<XROrigin>();
            if (origin == null || origin.Camera == null)
            {
                EditorUtility.DisplayDialog("MVRA",
                    "Não encontrei o XR Origin. Crie manualmente: GameObject > XR > XR Origin (Mobile AR) " +
                    "e rode este menu de novo.", "OK");
                return;
            }

            Camera cam = origin.Camera;
            cam.farClipPlane = 1000f;
            cam.nearClipPlane = 0.1f;
            cam.tag = "MainCamera";

            var app = new GameObject("CampusRA");
            var gps = app.AddComponent<LocationProvider>();
            gps.alvoSimulado = cam.transform;
            var bussola = app.AddComponent<HeadingProvider>();
            bussola.cameraEditor = cam.transform;
            var geo = app.AddComponent<GeoARManager>();
            geo.arCamera = cam;
            geo.gps = gps;
            geo.bussola = bussola;
            geo.modeloPin = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoModelo);
            var hud = app.AddComponent<ARHud>();
            hud.geo = geo; hud.gps = gps; hud.bussola = bussola;
            var sim = app.AddComponent<EditorSimulator>();
            sim.cameraAR = cam.transform;
            sim.rig = origin.transform;

            if (geo.modeloPin == null)
                Debug.LogWarning("[MVRA] map_pin.fbx não encontrado em " + CaminhoModelo + " — será usado um pin procedural.");

            EditorSceneManager.SaveScene(cena, CaminhoCena);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(CaminhoCena, true) };
            Selection.activeGameObject = app;

            EditorUtility.DisplayDialog("MVRA – Cena criada",
                "Cena salva em " + CaminhoCena + " e definida como única cena do build.\n\n" +
                "Teste no Editor: Play → botão direito + arrastar para olhar, WASD para andar.\n" +
                "No celular: menu MVRA > 3. Gerar APK.", "OK");
        }

        [MenuItem("MVRA/3. Gerar APK", priority = 3)]
        public static void GerarApk()
        {
            if (!File.Exists(CaminhoCena)) { EditorUtility.DisplayDialog("MVRA", "Rode antes o menu 2 (Criar cena).", "OK"); return; }
            Directory.CreateDirectory("Builds");

            var opcoes = new BuildPlayerOptions
            {
                scenes = new[] { CaminhoCena },
                locationPathName = CaminhoApk,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var rel = BuildPipeline.BuildPlayer(opcoes);
            if (rel.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(Path.GetFullPath(CaminhoApk));
                EditorUtility.DisplayDialog("MVRA", $"APK gerado ({rel.summary.totalSize / 1048576f:0.0} MB):\n{Path.GetFullPath(CaminhoApk)}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("MVRA", "O build falhou. Veja o Console para os erros.", "OK");
            }
        }

        [MenuItem("MVRA/4. Gerar APK e instalar no celular (USB)", priority = 4)]
        public static void GerarEInstalar()
        {
            if (!File.Exists(CaminhoCena)) { EditorUtility.DisplayDialog("MVRA", "Rode antes o menu 2 (Criar cena).", "OK"); return; }
            Directory.CreateDirectory("Builds");
            BuildPipeline.BuildPlayer(new[] { CaminhoCena }, CaminhoApk, BuildTarget.Android, BuildOptions.AutoRunPlayer);
        }
    }
}
#endif
