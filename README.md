# SCPS

`SCP와 재단` 게임 모드를 제공하는 EXILED 플러그인입니다.

## Requirements

- .NET Framework 4.8.1
- EXILED 9.14.2
- GG (`GG.dll`)
- MultiBroadcast
- AudioPlayerApi
- ProjectMER와 `SCPS` 맵
- `%APPDATA%\EXILED\Plugins\audio` 아래의 SCPS 오디오 파일

## References

게임 및 플러그인 DLL은 저장소에 포함하지 않습니다. 기본적으로 프로젝트는 이 저장소와 같은 상위 작업 폴더의 `EXILED_REFERENCES` 디렉터리를 사용합니다.

다른 위치를 사용하려면 빌드할 때 `EXILED_REFERENCES`를 지정합니다.

```powershell
dotnet build .\SCPS.sln -c Release -p:EXILED_REFERENCES="D:\path\to\EXILED_REFERENCES"
```

## Build and deployment

```powershell
dotnet build .\SCPS.sln -c Release
```

Windows에서는 빌드한 `SCPS.dll`이 기본적으로 `%APPDATA%\EXILED\Plugins`에 복사됩니다. 자동 배포를 끄려면 `-p:ExiledPluginsDirectory=`를 추가합니다.

## GG 패널에서 맵 위치 설정

GG 공통 SSS에서 패널을 연 다음 `SCPS` 패널로 이동합니다. Remote Admin 권한이 있는 사용자는 `맵 위치 설정`에서 항목을 선택하고 `Enter`를 눌러 현재 위치와 바라보는 방향을 저장할 수 있습니다.

- `위 / 아래`: 위치 항목 선택
- `Enter`: 현재 위치와 방향 저장
- `왼쪽 / 오른쪽`: 이전 화면
- `L`: 패널 닫기

설정은 `%APPDATA%\EXILED\Configs\GG\SCPS\Map\layout.json`에 저장되며, 기존 파일은 `layout.json.backup`으로 백업됩니다. 패널 언어는 GG 공통 SSS의 `Language / 언어` 설정을 따릅니다.

## 밤 진행도와 커스텀

- 신규 사용자는 `1일밤`부터 시작하며, 현재 도전 가능한 밤을 클리어할 때 다음 밤이 해금됩니다.
- `1일밤`부터 `6일밤`까지의 SCP 난이도는 EXILED 설정의 `night_presets`에서 지정합니다. 프리셋에서는 `0`으로 SCP를 비활성화할 수 있습니다.
- `6일밤`을 클리어한 사용자에게만 패널의 `커스텀` 옵션이 표시됩니다. 커스텀에서는 각 SCP 난이도를 `1~20`으로 조절할 수 있습니다.
- 커스텀 플레이는 밤 진행도를 올리지 않습니다.
- 한 사용자가 게임을 시작해도, 각 참가자의 클리어 기록은 해당 밤이 본인에게 해금되어 있었고 6시까지 살아남은 경우에만 갱신됩니다.

사용자별 기록은 `%APPDATA%\EXILED\Configs\GG\SCPS\Data\Players\<사용자 ID>\progress.json`에 즉시 저장되며, 갱신 시 이전 파일은 `progress.backup.json`으로 보관됩니다.
