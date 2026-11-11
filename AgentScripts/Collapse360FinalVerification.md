# 360도 붕괴 최종 PC 검증 — 2026-11-10

중단됐던 작업의 저장 씬 `Assets/_Project/Scenes/ConfinedPrototype.unity`를 Unity 6000.3.22f1에서 다시 재생했다. 런타임 코드, 씬, 기존 에셋, UI는 이번 마무리에서 수정하지 않았다. 누락된 검증 자료를 생성하고 기존 보고서의 실제 설정값과 링크를 바로잡았다. 결과는 `AgentScripts/ReviewImages/Collapse360/`에 보존한다.

## 결과

| 항목 | 측정 결과 |
| --- | --- |
| 컴파일·콘솔 | compilationFailed=false, 콘솔 오류 0, 기존 경고 2 |
| 실제 프레임 진행 | job wait에서 6250→6280, condition met=true |
| 초기 구조 | 개별 진동 363개, 위상 363개, 큰 파편 29개, 균열 선 58개 |
| 초기 거리 | 정면 0.650m, 뒤 0.535m, 오른쪽 0.441m, 왼쪽 0.500m, 위 0.291m |
| 초기 물품 | 새 구조와 활성 잡기 물품의 충돌 겹침 0 |
| 붕괴 | 실제 약 29초, 4547프레임, 101/101개 방출·렌더러 유지, 바닥 아래 이탈 0 |
| 큰 파편 | 29개 중 28개가 15cm 초과 이동, 23개가 12cm 초과 하강, 동시 이동 최고 19개 |
| 균열·먼지 | 균열 선 동시 최고 20개, 분진 입자 최고 814개, 파티클 시스템 39→39개 |
| 추적 보호 표본 | 공통 부모 이동 0, 카메라 위치 이동 0, 큰 파편 내부 머리 표본 0 |
| 리셋 | 파편 원위치 복원, 균열 숨김, 분진 지우기 모두 통과 |
| 파이프 낙하 | 관통 검사 156회·관통 0, 잡기 가능, 최종 위치 (0.016, 0.986, 0.657) |
| 파이프 치수 | 렌더링 반경 약 11mm, 충돌 반경 27mm |
| 실제 PC 타격 | 금속 충돌 포함 8회, 오디오 재생 확인, 신호 이벤트 2회, 잡은 상태·고정 제약 없음 |
| 구조 연출 | API로 60초 진행, 완료=true, 개방=1, 채광=1, 화이트아웃=0, 운반 중 파편 키네마틱 유지 |
| 종료 상태 | 재생 중지, 씬 dirty=false, XR Origin 1개, Input Action Manager 1개 |

파편은 실제 물리 접촉에 따라 걸리거나 내려앉는다. 29개 전부가 동일한 거리로 떨어진다는 의미는 아니다. 파이프의 관통 검사는 렌더링 반경의 캡슐 근사와 주변 메시 삼각형 사이의 거리 검사이며 1.5mm 허용 오차를 사용한다.

## 화면과 원자료

- [실제 PC 휴대폰 조명: 시작](ReviewImages/Collapse360/Initial_Phone.png)
- [정면 균열: 진단 조명](ReviewImages/Collapse360/At_3_Front.png)
- [상부 낙하: 진단 조명](ReviewImages/Collapse360/At_2_Above.png)
- [후면 붕괴: 진단 조명](ReviewImages/Collapse360/At_7_Rear.png)
- [붕괴 뒤 상부층: 진단 조명](ReviewImages/Collapse360/At_7_Above.png)
- [구조 뒤 열린 상부](ReviewImages/Collapse360/Rescue_Above.png)
- [초기 구조·거리](ReviewImages/Collapse360/Audit.json), [붕괴 전체 측정](ReviewImages/Collapse360/Runtime.json), [리셋](ReviewImages/Collapse360/Reset.json), [구조](ReviewImages/Collapse360/Rescue.json)
- [파이프 관통](ReviewImages/Collapse360/PipeClearance.json), [실제 타격](ReviewImages/Collapse360/RebarStrike.json), [최종 콘솔](ReviewImages/Collapse360/Console.json), [종료 상태](ReviewImages/Collapse360/EndState.json)

## 재실행

로컬 Unity CLI의 `script run`으로 다음 기존 진입점을 순서대로 호출한다. 프로젝트 경로와 `--caller plugin --skill unity-cli`를 지정한다. 작업 전 에디터가 정지 상태인지 확인한다. 빌더는 이미 적용됐으므로 실행하지 않는다.

1. 편집 모드: `AgentScripts/VerifyCollapse360.cs` → `VerifyCollapse360.Audit`.
2. 에디터 auto-tick과 백그라운드 진행을 확인한 뒤 재생을 시작한다. `job wait`에서 `Time.frameCount` 증가를 확인한다.
3. PC 재생: 같은 파일 → `VerifyCollapse360.Runtime` (실행 제한 80초 이상). `Temp/Collapse360Review/`에 자료를 생성한다.
4. `Temp/AvalancheReview/`와 `Temp/TightPocketReview/`를 만든다. `AgentScripts/VerifyAvalanche.cs` → `VerifyAvalanche.PipeOnly`, `AgentScripts/VerifyTightPocket.cs` → `VerifyTightPocket.Strike`를 실행한다.
5. 콘솔을 읽고 직접 시작한 재생을 종료한다. 각 Temp 경로의 결과를 검토해 영구 검토 폴더에 보존한다. Temp 파일은 영구 백업이 아니다.

## 검증 범위와 남은 사항

- PC 시뮬레이션 검증이다. 실제 헤드셋의 시야, 손 접근, 추적 자세, 멀미, 장치 성능은 검증하지 않았다. 프레임 수는 성능 벤치마크가 아니다.
- 구조는 단계 미리보기 API와 시간 진행으로 검사했다. 실제 60초 대기나 이번 실행에서 실제 119 통화부터 구조 조건 수락까지 이어지는 전체 플레이를 검사한 것은 아니다. 타격의 신호 이벤트 발생과 구조 신호 조건의 최종 수락은 구분한다.
- 실제 휴대폰 조명에는 강한 근접 하이라이트가 남는다. 붕괴 중 PC 잡기 거리 보정으로 휴대폰이 화면을 크게 차지하고 인접 파편의 어두운 뒷면이 보이는 구간이 있다. [원본 캡처](ReviewImages/Collapse360/At_7_Phone.png)를 그대로 보존했다.
- 구조 뒤 기존 외곽 잔해의 절단 면·가느다란 면이 보인다. 이번 결과를 시각적 완성이나 헤드셋 체감 승인으로 간주하지 않는다.
- 콘솔 경고 2건은 검증 시작 전에 발생한 `OpenXRPackageSettings.asset`의 임포터 불일치와 소스 DB 수정시각 불일치다. 콘솔을 지우거나 XR 설정을 변경하지 않았다.

이번 변경 파일은 이 기록, `AgentScripts/Collapse360Report.md`, `AgentScripts/ReviewImages/Collapse360/`의 검증 JSON·PNG다. 기존 미커밋 작업은 유지했다.
