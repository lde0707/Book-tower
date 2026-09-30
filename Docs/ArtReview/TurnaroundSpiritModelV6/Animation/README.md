# V6 정령 Unity 적용 · 걷기/달리기 · 점프 소리

최신 모자, 안감, 연결된 팔다리, 긴 옷자락을 포함한 모델을 기존 Unity FBX 에셋 경로에 교체했다. 프리팹과 씬을 다시 생성하지 않고 기존 GUID를 유지했다. 씬 파일의 해시는 적용 전후 동일하다. 이전 아트 파일은 Logs/SpiritV6IntegrationBackup에 보관했다.

23개 관절과 60fps 애니메이션 10개: Idle, Move, Run, JumpRise, Fall, Glide, Land, LookAround, TidyBookmark, Stretch. 새 다리·무릎·발 관절을 추가했고 Run은 더 짧은 주기, 큰 발걸음과 팔 흔들림, 후행하는 모자·옷자락 움직임을 사용한다. 애니메이션은 이동 경로를 직접 바꾸지 않는다.

기본 걷기 3.8u/s, Shift를 누르는 동안 달리기 5.9u/s. 기존 속도 5.5보다 걷기는 약 31% 느리고 달리기는 약 7% 빠르다. 좌·우 Shift 모두 지원한다. 설정의 조작 화면에 안내를 추가했다. 이전 씬의 moveSpeed 값 대신 새 walkSpeed/runSpeed 필드를 사용하므로 기존 씬도 새 기본값을 적용한다.

점프 소리는 Tools/prepare_spirit_jump.py로 만든 원본 합성 효과음이다. 0.34초, 48kHz mono PCM, 피크 0.18. Resources/Sfx/spirit_jump.wav로 저장했고 접지 상태에서 점프가 시작될 때 한 번 재생한다. 기존 효과음 볼륨 설정을 따른다.

검증: 복제한 최신 InGame 씬에서 실제 플레이 모드로 입력·이동·점프를 검사했다. W 걷기, 좌/우 Shift 달리기, Shift 해제, 이동 정지, Space 상승과 효과음 1회, 착지, 일시정지 입력 차단이 통과했다. 기존 점프/하강/활공/착지 FSM, 대기 행동 취소, 일시정지 애니메이션 정지 및 콜라이더 보존 검사도 통과했다. 모든 클립을 5개 시점으로 샘플링하여 메시가 유한한 좌표를 갖는지 검사했다.

레이아웃 도달거리 계산의 최대 속도도 5.9로 갱신했다. 검사 오류는 0건이며 기존 긴 통로 경고 5건은 남아 있다. Unity 로그에는 기존 에디터 검색 및 일부 맵 콜라이더 경고가 있으므로 프로젝트 전체가 경고 없이 깨끗하다는 의미는 아니다.

재생성: Blender에서 Tools/rig_turnaround_spirit.py 실행 후 검증 프로젝트에서 BookTower.EditorTools.TurnaroundSpiritIntegration.Run. 검증된 에셋과 원본 프로젝트 파일의 동일성은 applied-hashes.json에 기록했다.
