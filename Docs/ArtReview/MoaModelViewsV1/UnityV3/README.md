# 모아 V3 인게임 연결과 도입 대사 보완

## 수정한 원인

승인된 둥근 새는 `ArtSource/PaperMoaV3`의 Blender 모델과 비행 미리보기에만 존재했습니다. InGame의 OpeningStoryDirector.companion은 이전 C# 생성 새를 참조하고 있었습니다. 이번에는 승인된 `Moa_SculptV3.blend`에서 직접 메시·종이 PBR 텍스처·7본 스킨·비행 클립을 만들어 새 prefab을 씬의 동행 참조에 연결했습니다.

- Unity 자산: `Assets/_BookTower/Art/MoaSculpt/MoaSculpt.prefab`
- 편집 원본: `ArtSource/PaperMoaV3/Moa_UnityRig.blend`
- 2K BaseColor / Normal / Roughness 및 URP MetallicSmoothness.
- Hover / Fly / Flutter, 60Hz 저작, 0.22초 상태 전환.
- 원본 세분화 메시를 줄인 결과 약 23만 삼각형. LOD나 모바일 성능 최적화는 별도입니다.

PaperCompanion의 새 비행 모드는 플레이어의 옆·뒤, 낮은 높이에서 SmoothDamp로 따라갑니다. 작은 위치 변화와 날갯짓을 더했으며 벽 SphereCast와 충돌면을 따른 이동, 플레이어 체크포인트 이동 후 재합류를 사용합니다. 새 자체에는 플레이어를 막는 Collider가 없습니다. 복잡한 장애물 우회 경로 탐색을 하는 NavMesh 시스템은 아닙니다.

OpeningStoryBuilder도 새 prefab을 우선 사용하도록 바꿔 스토리 씬 재생성으로 이전 새가 돌아오는 것을 방지했습니다. 기존 대사/퍼즐 진행 플래그와 2D 컷씬 그림은 유지합니다.

## 도입 대사

기존 8개 문장에는 장소·깨어난 이유·새의 곤란한 상황·플레이어가 돕는 이유 사이 연결이 빠져 있었습니다. 16개 문장으로 다음 순서를 명시했습니다.

1. 반응 없는 정령에게 새가 빛을 나눠 준다.
2. 깨어난 정령이 이곳이 책 속 제본 작업방임을 알게 된다.
3. 정령의 기억 공백과 새가 처음 만난 상대라는 사실을 확인한다.
4. 새는 빛을 나눠 약해졌지만 곁에서 낮게 날 수 있다. 높은 문을 넘어 귀가할 수는 없다.
5. 새가 기억하는 마지막 장의 집과 닫힌 귀갓길을 설명한다.
6. 정령이 도움을 받은 데 대한 응답으로 함께 길을 찾기로 한다.
7. 펼쳐진 작업책을 첫 조사 대상으로 연결한다.

전체 문장은 [OpeningDialogue.md](OpeningDialogue.md)에 있습니다. 초반에는 모아의 이름이나 정령의 출생 비밀을 밝히지 않습니다. 집에 빛이 남아 있는지는 새의 기대이며 확정된 게임 규칙처럼 말하지 않습니다. F/좌클릭으로 전체 표시 → 다음 문장 → 플레이 복귀하는 규칙은 그대로입니다.

## 씬 안전성과 에디터

원본 씬 백업: `Logs/MoaSculptBackup/original-before-moa.unity`. 열린 원본 Unity를 닫지 않고 별도 검증 프로젝트에서 임포트와 씬 교체를 수행합니다. `Temp/MoaSculptApply.request`가 있으면 원본 에디터의 다음 C# 임포트 후 현재 열려 있는 씬에도 같은 교체를 적용합니다. 작업 전 메모리에 있던 씬은 별도로 백업합니다.

정령 prefab 저장 시 루트 이름이 파일명으로 바뀌어 이름 기반 참조가 실패하던 문제도 수정했습니다. 새 정령을 이름보다 컴포넌트로 찾고 중복을 제거한 뒤, 씬 인스턴스 이름을 `종이요정`으로 명시합니다.
