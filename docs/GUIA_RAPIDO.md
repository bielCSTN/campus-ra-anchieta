# Campus RA Anchieta – Guia rápido (Atividade 03 MVRA)

App Android de Realidade Aumentada **markerless por geolocalização**: mostra um pin 3D
(map_pin.fbx) sobre cada prédio do campus com o nome e a distância logo acima.

## 1. Preparar o projeto (uma vez)
1. Menu **MVRA > 1. Configurar projeto (Android + ARCore)**
   - troca a plataforma para Android, IL2CPP/ARM64, retrato, OpenGL ES 3
   - ativa o ARCore em XR Plug-in Management
   - muda *Active Input Handling* para **Both** → o Unity pede para reiniciar: aceite
2. Depois de reiniciar: **MVRA > 2. Criar cena CampusRA**
   (cria `Assets/MVRA/Scenes/CampusRA.unity` e a define como cena do build)

## 2. Testar no Editor
- Play. O GPS é simulado (pátio entre o Prédio 1 e o Anfiteatro), e o Norte fica em +Z.
- **Botão direito + arrastar** = olhar | **WASD** = andar | **Shift** = correr.

## 3. Rodar no celular
1. No celular: Configurações > Sobre > toque 7x em "Número da versão" → Opções do desenvolvedor → **Depuração USB**.
2. Conecte no USB e use **MVRA > 4. Gerar APK e instalar no celular**
   (ou **MVRA > 3. Gerar APK** → `Builds/CampusRA_Anchieta.apk` e instale manualmente).
3. Permita câmera e localização. Ative o GPS em **alta precisão**.

## 4. Usando no campus
- Segure o celular **em pé** e mexa devagar até os pins aparecerem.
- Se os pins ficarem tortos: **Calibrar** → escolha um prédio que você está vendo →
  aponte a mira (+) para o centro dele → **Alinhar**. Use −2°/+2° para o ajuste fino.
- **Bússola automática** volta ao modo sem calibração; **Recentrar GPS** descarta a deriva.
- Para filmar, fique a **50 m ou mais** dos prédios (requisito da atividade).

## Estrutura
| Arquivo | Função |
|---|---|
| `Scripts/GeoPoint.cs` | Coordenadas dos prédios (editáveis no Inspector) |
| `Scripts/GeoMath.cs` | Conversão lat/lon → metros (ENU), azimute, formatação |
| `Scripts/LocationProvider.cs` | GPS: permissão, início, reconexão, simulação no Editor |
| `Scripts/HeadingProvider.cs` | Bússola da câmera traseira (acelerômetro + magnetômetro + declinação) |
| `Scripts/GeoARManager.cs` | Alinhamento Norte↔mundo AR, fusão GPS+ARCore, posicionamento dos pins |
| `Scripts/GeoPinView.cs` | Pin 3D: normalização do modelo, cor, billboard, animação |
| `Scripts/ARHud.cs` | Interface: rótulos, indicadores de borda, radar, calibração |
| `Scripts/EditorSimulator.cs` | Controles de teste no Editor |
| `Editor/MVRASetup.cs` | Menu MVRA: configuração, cena e build |
