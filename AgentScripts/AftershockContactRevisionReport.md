# 균열과 개별 잔해 리듬 수정 — 2026-11-10

후속 타이밍·공간 변경은 [여진 5·10·20·30초 흐름 기록](AftershockTimingReport.md)을 기준으로 한다. 아래의 개별 반응 후보 수와 정면 거리 65cm는 이전 구현 기록이며, 현재는 반응 후보 11개와 정면 약 73cm다.

검은 선을 표면에 그리던 균열과 모든 잔해에 지속적으로 사인파를 적용하던 움직임을 교체했다. 이 기록은 이전 `Collapse360Report.md`의 균열·진동 방식보다 최신이다. 파이프 낙하 위치의 반복 검증은 완전 통과하지 못했으며 아래에 따로 기록한다.

## 참고한 영상과 자료

Roma Tre University 구조 연구 그룹의 다음 원본 영상 3개를 내려받아, 각 영상의 시간대별 디코딩 프레임을 비교했다. 전편을 연속 시청했다고 주장하지 않는다. 조적 구조 실험을 콘크리트 잔해와 동일한 조건으로 취급하지 않으며, 개구부·연결부 주변 손상과 박리·분리 형태를 시각 참고로 사용했다.

- [무보강 조적 구조 시험 11](https://www.romatrestrutture.eu/wp-content/uploads/2018/02/Shake_Table_CRM_1.mp4): 약 53초 영상. 14.2–27.5초 시험 장면, 32–45.3초 다른 각도의 느린 장면 표본을 비교했다.
- [보강 구조 시험 22](https://www.romatrestrutture.eu/wp-content/uploads/2018/02/Shake_Table_CRM_2.mp4): 약 29초 영상. 12.6–22.3초 표본에서 보강된 면과 개구부를 확인했다.
- [보강 구조 강진 시험 46](https://www.romatrestrutture.eu/wp-content/uploads/2018/02/Shake_Table_CRM_3.mp4): 약 68초 영상. 12.5–29.6초, 35.2–58초 표본에서 개구부와 접합부 주변의 손상 차이를 비교했다.
- [UC San Diego / RAPID 철근콘크리트 실험·손상 계측 자료](https://nheri.ucsd.edu/sites/default/files/workshops/archived-docs/2020/Calvi_UCSD_Rapid_webinar_2020_12_15.pdf): 균열의 위치·방향·벌어짐, 박리, 잔류 변형을 각각 구분하는 자료를 읽었다. PDF 스크린샷 확보에는 실패했으므로 사진을 직접 확인했다고 주장하지 않는다.

영상 프레임 시간은 [ReferenceFrames.json](ReviewImages/AftershockRevision/ReferenceFrames.json)에 남겼다. 원본 영상과 검토용 연락판은 `Temp/AftershockReference/`에 있으며 게임 에셋으로 가져오지 않았다. NIED E-Defense 공식 영상 페이지는 현재 점검 공지로 연결되어 확인 자료에서 제외했다. 브라우저 연결이 없어 Unity VideoPlayer로 공개 원본을 디코딩했다.

아래 움직임 값은 이 장면을 위한 연출 값이다. 실험 기록의 구조 응답이나 균열 역학을 수치 재현한 구조 해석은 아니다.

## 변경

- 기존 LineRenderer 균열 58개를 보존한 채 렌더링을 비활성화했다. 이전 균열 드라이버도 비활성화했다. 휴대폰 유리의 기존 파손 표현은 변경하지 않았다.
- 정면 콘크리트·조적과 덮개 파편 18개에 새 파생 메시를 연결했다. 원본 외장 텍스처와 UV를 유지하면서, 서로 다른 각도의 분기와 굴곡이 있는 분리 면을 만들었다. 실루엣과 맞물린 경계가 실제로 벌어지며 내부 면이 드러난다. 단순한 검은 선이나 바둑판 모양의 교차선으로 표현하지 않는다.
- 각 메시의 실제 낮은 꼭짓점을 받침점으로 골라 회전한다. 크기가 조정된 휴대폰·물병 받침도 피벗 계산에 반영했다.
- 지속적인 고주파 진동을 중단했다. 잔해별로 네 차례의 국소 반응 후보를 두되 일부 반응은 생략하고, 시작 시점·지속시간·반응 방향·감쇠를 다르게 했다. 큰 부재는 느리게 반응하며 반응 사이에는 정지 구간이 있다. 미세한 미끄러짐과 기존 내려앉음, 파단 직전의 작은 틈 벌어짐을 합성한다.
- 개별 반응의 결정 값은 시작 때 준비한다. 프레임마다 무작위 값을 다시 뽑지 않는다. 분진 시스템은 기존 풀을 재사용한다.
- 파편 해제 이후에는 기존 PhysX 중력·접촉을 사용한다. HMD와 양손의 추적 Transform을 움직이는 연출은 추가하지 않았다.

파이프 회귀 검증에서 기존 장애물 검사 누락도 발견했다. 부모에 충돌체가 있고 자식에 LOD 렌더러가 있는 모델을 검사에 포함했으며, 목표 지점뿐 아니라 이동 경로 중간도 검사한다. 물리 전환 속도는 기존의 강한 수평 발사 대신 아래로 미끄러지는 방향을 유지한다. 막힌 곳을 통과시키거나 정해진 착지 위치로 순간 이동시키지 않는다. 시험한 출구 위치 변경과 물리 반복 횟수 증가는 저장하지 않았고, 기존 출구·충돌 반경을 유지했다.

## 검증과 검토 자료

최종 컴파일 실패 없음, 콘솔 오류 0건이다. 마지막 에디터 콘솔에는 경고 3건이 있고, 누적 버퍼에는 19건이 기록돼 있다. 기존 미사용 필드·XR 시뮬레이터/초기화 경고와 참고 영상 디코딩의 색상 메타데이터 경고를 구분하며 콘솔을 지우지 않았다. [최종 콘솔](ReviewImages/AftershockRevision/Console.json)과 [종료 상태](ReviewImages/AftershockRevision/EndState.json)를 보존했다. 종료 시 재생 중지, 씬 dirty=false, XR Origin과 Input Action Manager는 각각 1개다.

- [18초 무음 움직임 영상](ReviewImages/AftershockRevision/Motion.mp4): 960×720, 20fps 저장. 같은 HMD 위치의 임시 확인 카메라, 70도 화각과 약한 진단 조명이다. 실제 기기 프레임 성능을 뜻하지 않는다.
- [시작: 실제 휴대폰 조명](ReviewImages/AftershockRevision/Contact_0_Phone.png), [파단 직전: 실제 휴대폰 조명](ReviewImages/AftershockRevision/Contact_2_Phone.png), [분리 중: 진단 조명](ReviewImages/AftershockRevision/Contact_3_Inspection.png).
- [실제 재생 기록](ReviewImages/AftershockRevision/Runtime.json): 약 29초 재생. 활성 파편 101개 방출, 바닥 이탈·머리 내부 표본·강제 카메라 이동/회전 여부, 분진 수, 주변 지지 부재의 실제 움직임을 측정한다. 시각 검토와 별개의 동작 검사다.
- 가까운 지지 잔해 16개에서 움직임·정지 시퀀스를 측정했다. 서로 다른 패턴 16개가 관측됐고, 모든 표본에 정지 구간이 있었다. 강한 검은 선의 재생 중 활성 수는 0이다. 정확한 최신 수치는 위 JSON을 기준으로 한다.
- [리셋](ReviewImages/AftershockRevision/Reset.json), [구조](ReviewImages/AftershockRevision/Rescue.json): 원위치·분진·선 비활성 복원, 구조 완료·개방·채광과 화이트아웃 0을 검사한다. 구조는 API 시간 진행이며 실제 60초 대기나 전체 통화부터의 플레이 검증은 아니다.
- [초기 배치 검사](ReviewImages/AftershockRevision/Audit.json): 정면 약 65cm, 새 구조와 초기 잡기 물품의 충돌 겹침 0개.

## 파이프의 남은 문제 — 전체 통과로 표시하지 않음

최종 속도·장애물 검사 보완 후 동일 경로의 독립 낙하 3회 중 2회는 관통 0과 잡기 범위 검사를 모두 통과했다. 세 번째는 관통은 없었으나 파편 뒤쪽 `(−0.027, 1.039, 1.059)`에서 멈춰 기본 눈 위치 기준 85cm 잡기 범위를 벗어났다. 따라서 파이프 접근성은 간헐적으로 실패하며 해결 완료로 간주하지 않는다. 실제 좁은 공간에서 접촉으로 결정되는 착지를 레벨 배치와 함께 추가 보완해야 한다.

- [최종 낙하 1](ReviewImages/AftershockRevision/PipeFinal_1.json), [최종 낙하 2](ReviewImages/AftershockRevision/PipeFinal_2.json), [최종 낙하 3: 접근 실패](ReviewImages/AftershockRevision/PipeFinal_3.json).
- 초기의 관통·걸림 사례도 `PipeObstruction*.json`에 보존했다. 시험 중 한 번 성공한 결과만 골라 최종 통과라고 기록하지 않았다.
- 붕괴 중 단발 타격은 콘크리트 충돌음 뒤 금속음이 누락된 사례가 있었다. [실패 기록](ReviewImages/AftershockRevision/EarlyStrike.json)을 유지한다. 붕괴 20초 뒤 실제 PC 반복 휘두르기 검사는 별도로 [RebarStrike.json](ReviewImages/AftershockRevision/RebarStrike.json)에 기록한다. 신호 이벤트가 구조 조건 최종 수락과 같은 의미는 아니다.

## 변경 파일과 복구 지점

- 런타임: `Assets/_Project/Scripts/RubbleStructuralCollapse.cs`, `AftershockPipeDrop.cs`.
- 씬: `Assets/_Project/Scenes/ConfinedPrototype.unity` — 새 메시 참조, 기존 선·균열 드라이버 비활성화, 작은 분리 오프셋.
- 신규 파생 에셋: `Assets/_Project/Art/ContactFractures/`의 메시 18개와 메타 파일. 기존 유료 모델·기존 파생 메시·공용 재질은 보존했다.
- 도구: `AgentScripts/RefineAftershockContacts.cs`, `InspectQuakeReference.cs`, `VerifyContactRevision.cs`; 기존 `VerifyAvalanche.cs`에 장애물·실제 충돌체 진단을 추가하고, `VerifyTightPocket.cs`에 반복 타격 및 기다리는 시간 인자를 추가했다.
- 이 보고서, `Collapse360Report.md`의 최신 기록 링크, `ReviewImages/AftershockRevision/`의 JSON·PNG·MP4.
- 수정 전 코드와 씬 복사본: `AgentScripts/Backups/AftershockRevision_20261110/`. 기존 미커밋 변경을 보존하기 위해 범위를 좁힌 복사본을 사용했고, 전체 변경을 임의로 커밋하거나 초기화하지 않았다. 빌더는 이미 적용됐으므로 다시 실행하지 않는다.

패키지·SDK, XR Origin, Input Action Manager, UI·폰트·테마는 이 작업에서 수정하지 않았다. 실제 HMD·양손 조작과 기기 성능은 미검증이다. 가까운 휴대폰 조명의 과도한 하이라이트와 구조 후 외곽 잔해의 절단 면은 기존 시각적 한계로 남아 있다.

## 범위를 좁힌 craft 검토

`unity-cli`의 playmode-verification-loop, `craft-game-floors`의 vfx 및 visual-scorecard 참조를 읽었다. Unity 6000.3의 ObjectPool, Application.runInBackground, MediaEncoder 공식 문서도 확인했다. 현재 URP, 회색 콘크리트·붉은 벽돌·낡은 금속과 휴대폰 중심 조명을 유지했으며 별도 아트 방향을 만들지 않았다.

palette discipline: 4/5 — 기존 재질과 회갈색 분진 유지. 새 기본 회색 재질을 추가하지 않았다.

style uniformity (fracture scope): 1/5 → 3/5 — 굵고 반복적인 선 대신 맞물린 불규칙 분리 면과 내부 면. 조명 과노출은 별도 한계다.

feedback visibility (contact motion scope): 1/5 → 3/5 — 지속 진동 대신 움직임·정지·분리·낙하를 영상과 실제 Transform 표본으로 구분했다. 공포감이나 VR 체감의 객관적 점수는 아니다.

조명 전체, UI, 캐릭터 실루엣과 게임 셸은 이번 변경 범위가 아니며 새로 평가하거나 변경하지 않았다.
