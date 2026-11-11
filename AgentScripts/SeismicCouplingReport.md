# 전체 공간의 여진과 누적 하중 붕괴 — 2026-11-10

후속: 사용자의 강도 피드백에 따라 최대 맵 이동·회전을 높였다. 현재 적용값과 새 검증은 [여진 강도 상승 보정](AftershockStrengthReport.md)을 따른다. 아래 수치는 그 이전 버전의 기록이다.

바닥·벽·천장이 함께 흔들리는 공통 지반 운동을 연결하고, 잔해가 그 하중을 버티다가 떨어지도록 수정했다. 기존의 초 단위 파편 해제, 바깥쪽 발사 속도, 정해진 시각의 전역 먼지 폭발을 주 경로에서 제거했다.

[최종 31.5초 PC 재생 영상](ReviewImages/SeismicCoupling/Episode.mp4) · [실제 재생 측정](ReviewImages/SeismicCoupling/Runtime.json) · [진동 0 대조군](ReviewImages/SeismicCoupling/ZeroExcitation.json) · [충돌체 검사](ReviewImages/SeismicCoupling/Audit.json)

## 변경한 동작

- 공통 지반 변위·회전에 기존 구조물 363개와 바닥·받침 등 추가 소유 Transform 15개를 연결했다. 하위 메시도 그 소유자를 따라 움직인다. 바닥의 정적 충돌체 10개는 운동학 Rigidbody로 바꿔 움직이는 접촉면으로 사용한다. 자유 낙하물과 잡은 물체를 이동하는 부모에 넣지 않는다.
- 각 구조물은 같은 지반 가속도를 받되 무게에 따른 고유 주기, 감쇠, 받침 방향이 다른 스프링 응답으로 움직인다. 미리 예약한 개별 흔들림 펄스 대신 실제 입력 하중을 사용한다.
- 연결부가 누적 하중으로 서서히 벌어지고 미끄러진 뒤 저항값을 넘으면 중력에 넘긴다. `failureResistance`가 실제 파괴 기준이다. 이전 `releaseAt`은 씬을 변환할 때 저항값 분포를 정하는 참고값으로만 사용했다. 런타임 파괴 조건에서 시각을 비교하지 않는다.
- 101개 구조 잔해, 별도 보조 파편 84개, 강관의 지지점 해제에 하중을 연결했다. 큰 파편의 초기 속도는 실제 받침 이동 속도를 이어받고 최대 0.16m/s로 제한한다. 별도 발사 속도·회전 설정을 제거했다.
- 큰 파편 29개의 볼록 충돌체는 주변 구조물과 최대 수 cm 겹쳐 있었다. 보이는 메시를 그대로 두고, 그 내부에서 주변 충돌체와 겹치지 않는 접촉 영역을 복합 BoxCollider로 맞췄다. 인접 영역을 병합해 585개에서 201개로 줄였다. 기존 MeshCollider와 원본 메시 참조는 보존하되 이 파편들의 MeshCollider는 비활성화했다. 모든 활성 접촉체에 대한 초기 0.5mm 초과 겹침 검사 결과는 0이다.
- 방출 시 먼지는 작은 국소 가루로 줄였다. 큰 접촉 가루와 충돌음은 실제 물리 접촉에서 나온다. 가까운 실제 접촉만 보호 판정 대상이며, 원격 잔해의 낙하가 자동으로 머리 피격 화면을 만들지 않는다.
- 0–5초 상승, 5–10초 최대, 10–20초 진동 감쇠를 유지했다. 먼지는 연속 방출로 채우고 10–30초에 걸쳐 없어진다. 최대 분진 입자는 100개이며 기존 시스템을 재사용한다.
- 플레이어의 여진 감각은 공간 운동·음향과 양손 컨트롤러의 약한 연속 진동을 연결했다. HMD·컨트롤러 추적 Transform에는 인공 움직임을 추가하지 않았다. 햅틱 호출은 실제 추적 HMD가 있는 경우에만 한다. 실기기 체감은 검증하지 않았다.

## 검증 결과

| 항목 | 결과 |
| --- | --- |
| 진동 0, 20.3초 실제 재생 | 구조 파괴 0, 보조 파편 낙하 0, 강관 해제 없음 |
| 정상 32초 재생 | 5초 전 큰 파괴 0, 첫 파괴 5.50초, 마지막 9.17초, 101개 해제 |
| 바닥 5곳의 실제 최대 변위 | 약 2.47–2.56cm |
| 주 지반 운동 | 20초 이후 0 |
| 분진 | 7.5초 밀도 2.0, 15초 1.69, 30.3초 0 및 비활성 |
| 추적 Transform | PC에서 머리·양손 위치 변화 0, 머리 회전 변화 0 |
| 머리와 큰 파편 접촉체 내부 겹침 표본 | 0 |
| 시스템 재사용 | ParticleSystem 49 → 49 |
| 리셋 | 파편 재부착, 하중 초기화, 바닥 복원, 분진 제거, 이전 균열선 숨김 통과 |
| 구조 단계 API | 완료·개방·채광 확인, 화이트아웃 0 |
| 최종 컴파일·콘솔 오류 | 0 |

PC 테스트는 실제 Play Mode 프레임 5,917개가 진행된 동안 수행했다. 영상은 같은 카메라 위치·화각·후처리·깊이 설정으로 렌더링한 960×720, 20fps, 630프레임의 무음 MP4다. 저장 후 Unity VideoPlayer로 길이와 디코딩 가능 여부를 확인했다. 인코딩 프레임 수는 VR 기기 성능 수치가 아니다.

검증 스크립트가 처음에는 벽시계로 게임 진행을 수동 구동해, 촬영 중 Unity 게임 시간과 분진 Update 시간이 어긋났다. 최종 검증은 둘 다 `Time.time` 기준이다. 초기 결과는 `Runtime_FirstDraft.json`과 `Episode_FirstDraft.mp4`로 구분해 보존했다. 마지막 코드 정리는 사용하지 않는 폭발 설정 필드 제거와 같은 역할의 Prepare 메서드 정리이며, 그 뒤에도 컴파일 오류 0을 확인했다.

[리셋](ReviewImages/SeismicCoupling/Reset.json) · [구조 단계](ReviewImages/SeismicCoupling/Rescue.json) · [영상 정보](ReviewImages/SeismicCoupling/Movie.json) · [컴파일](ReviewImages/SeismicCoupling/Compilation.json) · [콘솔 오류](ReviewImages/SeismicCoupling/ConsoleErrors.json) · [경고 기록](ReviewImages/SeismicCoupling/ConsoleWarnings.json) · [종료 상태](ReviewImages/SeismicCoupling/EndState.json)

## 수정 파일

- `Assets/_Project/Scripts/AftershockEnvironmentMotion.cs`: 공통 지반 운동과 전체 공간 연결.
- `RubbleStructuralCollapse.cs`: 지반 입력에 대한 개별 응답·누적 하중·점진적 연결부 실패.
- `AftershockFallingChunk.cs`: 받침 속도 상속, 복합 충돌체, 약한 머리 주변 안전 반응.
- `AftershockDebris.cs`, `AftershockPipeDrop.cs`: 보조 파편·강관의 하중 기반 해제.
- `AftershockDirector.cs`, `ConcreteImpactDust.cs`: 실제 접촉 기반 충격과 국소 가루.
- `AftershockAudio.cs`: 진동 강도에 맞춘 연속 햅틱 연결.
- `AftershockRubbleRain.cs`, `AftershockDustEffect.cs`: 실제 진동 강도에 따른 잔가루와 연속 분진.
- `Assets/_Project/Scenes/ConfinedPrototype.unity`: 공간 연결, 저항값, 접촉체, 분진 설정.
- `Assets/_Project/Art/AftershockTiming/PeakDust.mat`: 이 여진용 분진 불투명도 조정.
- `AgentScripts/BuildSeismicCoupling.cs`, `FitSeismicBearings.cs`, `RefineSeismicDust.cs`, `InspectSeismicCoupling.cs`, `AuditSeismicCoupling.cs`, `VerifySeismicCoupling.cs`와 이 보고서 및 측정 자료.

기존 미커밋 변경, 원본 유료 모델, XR Origin, Input Action Manager, 패키지·SDK, 휴대폰 UI는 보존했다. UI 변경은 하지 않았다. 씬 저장 후 Play Mode를 종료했다. XR Origin과 Input Action Manager는 각각 1개다.

## 복구·남은 한계

변경 전 주요 코드·씬·분진 재질은 `AgentScripts/Backups/SeismicCoupling_20261110/`에 있다. `AftershockPipeDrop.reconstructed-before.cs.txt`와 `AftershockRubbleRain.reconstructed-before.cs.txt`는 이번 편집 구문을 역적용한 텍스트이며 최초 시점의 바이트 복사본과 구분한다. 이름에 `partial` 또는 `after-count-change`가 있는 파일은 완전한 변경 전 백업이 아니다. 이미 적용한 빌더는 다시 실행하지 않는다.

처음 시도한 단일 충돌체 축소는 큰 축소가 필요하고 3곳의 겹침이 남아 채택하지 않았다. `Art/SeismicContacts/`의 실험용 파생 메시와 `Build.json`은 이력으로 보존했으며 최종 씬은 이 메시들을 참조하지 않는다. 최종 접촉체는 원본 시각 메시 내부를 근사한 복합 충돌체다. 실제 콘크리트 응력 해석이나 파괴 역학 해석은 아니다.

실제 헤드셋·양손 햅틱 체감과 목표 기기 성능은 미검증이다. 큰 낙하물이 잠깐 시야를 가리고 가까운 휴대폰 조명에서 파편 면이 과노출되는 기존 시각적 한계가 남는다. 강관의 기존 간헐적 잡기 범위 실패를 이번 변경으로 해결했다고 주장하지 않는다. 구조 단계 검사는 API 진행 확인이며 전체 통화부터 구조까지의 장시간 플레이 검사는 아니다.

콘솔을 지우지 않았다. 최종 오류는 0이고 경고 1개가 남는다. 누적 기록에는 제거 전 미사용 필드 경고, 기존 XR 시뮬레이터의 잡기 햅틱 기능 조회 경고, WindowsMediaFoundation의 영상 색상 원색 안내가 남아 있다.

## 참고와 범위 내 시각 검토

이전 [실제 영상 검토·연출 수정 기록](AftershockContactRevisionReport.md)에 이어 공통 운동과 개별 응답을 연결했다. [UC San Diego의 실내·진동대 영상 자료 목록](https://jacobsschool.ucsd.edu/news/release/3609?id=3609)도 확인했다. 이 목록의 모든 영상을 새로 시청했다고 주장하지 않는다. 진동대 구조물의 운동을 게임의 가속도 수치에 그대로 복제한 것은 아니다.

Unity 6000.3의 [Rigidbody.MovePosition](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.MovePosition.html), [Physics.ComputePenetration](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.ComputePenetration.html), [MeshCollider](https://docs.unity3d.com/6000.3/Documentation/Manual/class-MeshCollider.html) 문서를 확인했다. `unity-cli`, `unity-pipeline`, `fix-3d-physics-collisions`, `craft-game-floors`의 해당 실행·물리·연출 검토 절차를 사용했다.

범위 내 자체 시각 검토: 기존 회색 콘크리트·벽돌·휴대폰 조명 유지 4/5, 공간 진동과 개별 지지 반응의 구분 3/5, 파괴와 실제 접촉의 연결 3/5. 물리 기기 체감이나 사실성의 객관적인 점수는 아니다. 정면·좌우·후면 캡처를 검토했으며 근접 조명의 과노출은 남겨진 한계로 기록한다.
