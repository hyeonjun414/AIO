# 프로젝트 구조 기록

이 문서는 AIO 프로젝트에서 직접 작성한 프레임워크 구조를 누적 기록하기 위한 문서다.
새 시스템을 추가하거나 기존 시스템의 책임이 바뀌면 이 문서를 함께 갱신한다.

## 기본 방향

- Unity와 C# 기반의 자체 게임 프레임워크를 구축한다.
- 게임 코드가 Unity API, Addressables, SceneManager를 직접 광범위하게 호출하지 않도록 프레임워크 계층으로 감싼다.
- 시스템별 책임을 분리한다.
  - Scene: 씬 전환과 현재 씬 상태 관리
  - Resource: Addressables 로드와 핸들 생명주기 관리
  - Pooling: Addressables 인스턴스 기반 오브젝트 풀 관리
  - UI: Page, Popup, View 단위 UI 생명주기 관리

## 주요 경로

```text
Assets/
  Addressables/
    Prefabs/
    UI/
      Pages/
      Popups/
      Views/
    Data/
    Audio/
    Effects/
    Materials/
    Sprites/

  Scripts/
    Framework/
      Game.Framework.asmdef
      SingletonScene.cs
      SceneManagement/
      ResourceManagement/
      Pooling/
      UI/
```

## Assembly Definition

경로:

```text
Assets/Scripts/Framework/Game.Framework.asmdef
```

현재 참조:

```text
Unity.Addressables
Unity.ResourceManager
UnityEngine.UI
```

프레임워크 코드의 기본 asmdef다. Addressables 기반 리소스 시스템과 uGUI 기반 UI 시스템이 이 asmdef 아래에 들어간다.

## Scene Management

경로:

```text
Assets/Scripts/Framework/SceneManagement/
```

파일:

```text
SceneId.cs
SceneNames.cs
GameSceneManager.cs
```

책임:

- 게임 씬을 `SceneId`로 식별한다.
- 실제 Unity 씬 이름은 `SceneNames`에서 관리한다.
- `GameSceneManager`는 런타임에 자동 생성된다.
- 씬 로딩 중복 요청을 막고, 현재 로딩 상태와 진행률을 제공한다.
- 현재 씬 갱신, 씬 재로드, additive unload를 지원한다.

현재 씬 매핑:

```text
SceneId.Splash -> SplashScene
SceneId.Intro  -> IntroScene
SceneId.Lobby  -> LobbyScene
SceneId.Game   -> GameScene
```

주요 API:

```csharp
GameSceneManager.Instance.TryLoad(SceneId.Intro);
GameSceneManager.Instance.Load(SceneId.Game);
GameSceneManager.Instance.TryReloadCurrent();
GameSceneManager.Instance.TryUnloadAdditive(SceneId.Lobby);
```

## Resource Management

경로:

```text
Assets/Scripts/Framework/ResourceManagement/
```

파일:

```text
ResourceKey.cs
ResourceLabels.cs
GameResourceManager.cs
```

책임:

- Addressables 초기화와 로드 진입점을 관리한다.
- 게임 코드가 Addressables key 문자열을 직접 흩뿌리지 않도록 `ResourceKey`를 사용한다.
- `AsyncOperationHandle`을 캐시하고 참조 카운트를 관리한다.
- Addressables 인스턴스 생성과 해제를 한 곳에서 처리한다.
- label 기반 프리로드를 지원한다.

기본 label:

```text
common
preload_splash
preload_intro
preload_lobby
preload_game
```

주요 API:

```csharp
await GameResourceManager.Instance.InitializeAsync();

var prefab = await GameResourceManager.Instance.LoadAsync<GameObject>("Player");
GameResourceManager.Instance.Release("Player");

var instance = await GameResourceManager.Instance.InstantiateAsync("Player", parent);
GameResourceManager.Instance.ReleaseInstance(instance);

await GameResourceManager.Instance.LoadAssetsByLabelAsync<UnityEngine.Object>(ResourceLabels.Game);
GameResourceManager.Instance.ReleaseLabel<UnityEngine.Object>(ResourceLabels.Game);
```

주의:

- Addressables로 로드한 에셋과 인스턴스는 반드시 프레임워크 API를 통해 해제한다.
- Unity 에디터에서 Addressables Groups/Profile 설정이 생성되어 있어야 실제 런타임 로드가 가능하다.

## Pooling

경로:

```text
Assets/Scripts/Framework/Pooling/
```

파일:

```text
GameObjectPoolManager.cs
```

책임:

- Addressables prefab 인스턴스 기반 오브젝트 풀을 관리한다.
- 풀 생성, 사전 생성, Spawn, Despawn, Pool 해제를 담당한다.
- 실제 인스턴스 생성과 해제는 `GameResourceManager`에 위임한다.

주요 API:

```csharp
var key = new ResourceKey("Player");

await GameObjectPoolManager.Instance.PrewarmAsync(key, 10);

var player = await GameObjectPoolManager.Instance.SpawnAsync(key, parent);
GameObjectPoolManager.Instance.Despawn(player);

GameObjectPoolManager.Instance.ReleasePool(key);
GameObjectPoolManager.Instance.ReleaseAll();
```

정책:

- 풀에 들어간 인스턴스는 `SetActive(false)` 상태로 보관한다.
- `Despawn`은 인스턴스를 삭제하지 않고 비활성 큐로 되돌린다.
- `ReleasePool`은 활성/비활성 인스턴스를 모두 `GameResourceManager.ReleaseInstance`로 해제한다.

## UI System

경로:

```text
Assets/Scripts/Framework/UI/
```

파일:

```text
UIParam.cs
UIKey.cs
UIKeys.cs
UILayer.cs
UIBase.cs
UIBaseWithParam.cs
UIPage.cs
UIPopup.cs
UIView.cs
UIRoot.cs
GameUIManager.cs
```

### UI 분류

`Page`

- 화면의 주 상태를 담당한다.
- 기본 정책은 한 번에 하나만 활성화한다.
- 예: LobbyPage, GameHudPage, ResultPage

`Popup`

- Page 위에 stack으로 쌓이는 UI다.
- top popup부터 닫는 구조를 기본으로 한다.
- 예: ConfirmPopup, SettingsPopup, ErrorPopup

`View`

- Page나 Popup 내부에서 재사용되는 작은 UI 단위다.
- 독립적인 화면 흐름을 소유하지 않는다.
- 예: InventorySlotView, CharacterCardView, CurrencyView

### UI Root

`UIRoot`는 런타임에 자동 생성된다.

```text
[GameUIRoot]
  PageLayer
  ViewLayer
  PopupLayer
  ToastLayer
  SystemLayer
```

현재 Canvas 정책:

```text
RenderMode: ScreenSpaceOverlay
SortingOrder: 1000
ReferenceResolution: 1920 x 1080
ScreenMatchMode: MatchWidthOrHeight
Match: 0.5
```

### UI Param

각 UI는 고유한 Param record를 가질 수 있다.
Param이 없는 UI도 지원한다.

Param 루트:

```csharp
public abstract record UIParam;
```

Param이 있는 UI 예:

```csharp
public sealed record LobbyPageParam(string UserName, int Level) : UIParam;

public sealed class LobbyPage : UIPage<LobbyPageParam>
{
    protected override Task OnApplyParamAsync(LobbyPageParam param)
    {
        return Task.CompletedTask;
    }
}
```

Param이 없는 UI 예:

```csharp
public sealed class LoadingPopup : UIPopup
{
}
```

### UI Addressables Key 규칙

UI 프리팹 권장 위치:

```text
Assets/Addressables/UI/Pages/
Assets/Addressables/UI/Popups/
Assets/Addressables/UI/Views/
```

Addressables address 권장 규칙:

```text
UI/Page/LobbyPage
UI/Popup/LoadingPopup
UI/View/InventorySlotView
```

코드에서는 `UIKeys`를 통해 생성한다.

```csharp
UIKeys.Page("LobbyPage");
UIKeys.Popup("LoadingPopup");
UIKeys.View("InventorySlotView");
```

### 주요 API

Param 없는 Page:

```csharp
await GameUIManager.Instance.OpenPageAsync<IntroPage>(
    UIKeys.Page("IntroPage")
);
```

Param 있는 Page:

```csharp
await GameUIManager.Instance.OpenPageAsync<LobbyPage, LobbyPageParam>(
    UIKeys.Page("LobbyPage"),
    new LobbyPageParam("Player", 10)
);
```

Param 없는 Popup:

```csharp
await GameUIManager.Instance.OpenPopupAsync<LoadingPopup>(
    UIKeys.Popup("LoadingPopup")
);
```

Param 있는 Popup:

```csharp
await GameUIManager.Instance.OpenPopupAsync<ConfirmPopup, ConfirmPopupParam>(
    UIKeys.Popup("ConfirmPopup"),
    new ConfirmPopupParam()
);
```

View 생성:

```csharp
var view = await GameUIManager.Instance.CreateViewAsync<InventorySlotView, InventorySlotViewParam>(
    UIKeys.View("InventorySlotView"),
    new InventorySlotViewParam(),
    parent
);

await GameUIManager.Instance.ReleaseViewAsync(view);
```

Popup 닫기:

```csharp
GameUIManager.Instance.CloseTopPopup();
await GameUIManager.Instance.CloseAllPopupsAsync();
```

## 현재 검증 상태

프레임워크 빌드 확인:

```text
msbuild Game.Framework.csproj /t:Build /p:RestorePackages=false /verbosity:minimal
```

결과:

```text
Game.Framework.dll 생성 성공
```

참고:

- `dotnet build`는 현재 환경에서 중간에 멈추는 현상이 있었다.
- Mono `msbuild` 기준으로는 컴파일이 통과했다.
- Unity UIToolkit SourceGenerator analyzer 경고가 발생했지만, 프레임워크 코드 컴파일 실패는 아니었다.

## 앞으로 추가할 수 있는 항목

- Scene 전환과 Resource label preload 연동
- UI Page history와 뒤로가기 처리
- Popup dim 처리
- UI transition animation
- View 전용 pooling
- Addressables 에디터 검증 도구
- Scene별 리소스 자동 해제 정책
- 프레임워크 샘플 UI prefab과 테스트 씬
