# 잔해 완전 매몰 및 자연광 구조 엔딩

## 결과

외벽/바닥/검은 가림판의 렌더링을 제거하고, 원본 `RubbleAndDebris` 프리팹 83개를 추가해 좌우/앞뒤를 3겹, 천장을 3겹으로 둘렀다. 틈막이도 같은 유료 에셋의 잔해 더미다. 원본 유료 에셋은 수정하지 않았다. 기존 외벽 오브젝트는 여진/구조 참조와 기존 물리 경계를 유지하기 위해 렌더러만 비활성화했다. 평면 외벽이 보이는 방식으로 차광하지 않는다.

차광용 외곽 잔해는 원본 LOD0 메시를 고정 사용한다. LOD 전환으로 구멍 모양이 달라져 빛이 새는 문제를 막기 위한 씬 인스턴스 설정이다. 기존 상호작용 물체, XR Origin, 입력, UI는 유지했다. 추가 잔해의 충돌체는 꺼 두었다.

## 자연광 엔딩

- 화면 앞의 광선 쿼드, 밝은 외부 평면, 흰색 페이드를 사용하지 않는다.
- 노출을 시간에 따라 크게 올리는 효과를 제거했다. 노출 +0.1 EV, Bloom 0.06/threshold 1.2/scatter 0.45의 약한 보정만 사용한다.
- 실제 그림자를 만드는 방향광과 원거리 하늘을 사용한다. 주변광과 반사는 매몰 중 0을 유지한다.
- 천장 3개 층은 기존 구조 흐름에 연결해 각각 6/20/35초부터 옆으로 걷힌다. 잔해가 이동하며 생긴 불규칙한 틈이 빛과 하늘을 드러낸다.
- 미약한 반사광은 차광 판정으로 열린 면적을 측정해 반영한다. 밝기 변화율을 초당 0.2로 제한해 갑자기 켜지지 않게 했다.
- 60초 완료 시 주변 잔해와 열린 하늘을 볼 수 있는 상태로 끝난다. 화면을 강제로 하얗게 덮지 않는다.
- 복구 시 천장, 충돌체 상태, 카메라 후처리/클리어 설정, 하늘, 태양, 렌더 파이프라인을 되돌린다.

## 변경 파일

- `Assets/_Project/Scenes/ConfinedPrototype.unity`: 외벽 렌더러 11개 비활성화, 다중 잔해층/틈막이, 구조 대상/차광 판정 갱신, 기존 환경 광원 비활성화.
- `Assets/_Project/Scripts/RescueEndingDirector.cs`: 실제 개구부와 그림자에 따른 자연광, 완만한 반사광, 화면 효과 제거, 하늘/카메라 상태 복구.
- `Assets/_Project/Art/RescueEnding/NaturalDaylightSky.mat` 및 메타 파일.
- `Assets/_Project/Art/RescueEnding/NaturalOpeningVolume.asset` 및 메타 파일.
- `Assets/_Project/Scenes/Backups/BeforeSealedBurial_20261106_145055.unity` 및 메타 파일: 시작 시 에디터에 있던 미저장 변경도 포함한 작업 전 복원점.
- `AgentScripts/BuildSealedBurial.cs`, `InspectSealedBurial.cs`, `ReviewSealedBurial.cs`: 배치/검사/광선 및 화면 검증 도구.
- `AgentScripts/VerifyRescueEnding.cs`, `VerifyImportedRubble.cs`: 이전의 흰색 완료 조건을 새 자연광 완료 조건으로 갱신.
- `AgentScripts/RescueEndingDirector.BeforeSealedBurial.cs.txt`: 수정 전 코드 사본.
- 이 보고서.

## 검증 결과

- Unity recompile: errors 0, warnings 0. 최종 콘솔: errors 0, compilationFailed=false.
- PC 프레임 진행: `job wait` met=true.
- 머리 주변 6곳 × 2,048방향 = 12,288개 광선을 실제 LOD0 삼각형에 대입: 바깥으로 열린 방향 0개. 양면 교차 검사.
- 초기 광선 검사에서 놓친 작은 틈은 최종 렌더 이미지에서 방향을 추출해 추가 유료 잔해로 보강했다.
- 하늘을 켠 채 모든 실내 조명/휴대폰 자체 표시를 제외한 최종 여섯 방향 화면: **mean brightness=0, clipped=0**. 검사 해상도 1100×850, sRGB. 결과: `Temp/SealedBurialReview/Darkness.json`.
- 실제 게임에서는 휴대폰 화면과 손전등은 계속 사용할 수 있다. 검증에서만 이 자체 발광 요소를 제외했다.
- 119 실제 연결, 신고 유지, 무전 기회, 잡은 파이프 신호 조건의 통합 검사: PASS. 조건 없이 구조되지 않음.
- 여진, 벽체 파편 6개 분리/복구, 구조 대상 22개 이동/복구, 단일 XR Origin: PASS.
- 새 엔딩 0/10/22/34/46/60초, 6방향 캡처: PASS. 화면 흰색 덮개 0, 완료/복구 정상. HMD 카메라 위치/회전 변경 없음.
- 작업용 Play Mode를 종료하고 Run In Background 오버라이드를 해제했다. 기존 활성 auto-tick 설정은 유지했다.

## 검토 이미지

- `Temp/SealedBurialReview/Closed_NoPhoneLight_*.png`: 차광 검사 화면.
- `Temp/SealedBurialReview/Inspection_*.png`: 형태 확인용 임시 검수 조명. 이 조명은 씬에 저장하지 않음.
- `Temp/SealedBurialReview/Rescue_22_Ceiling.png`: 작은 틈으로 드러나는 하늘.
- `Temp/SealedBurialReview/Rescue_46_Front.png`: 주변 잔해에 들어오는 자연광.
- `Temp/SealedBurialReview/Rescue_60_Ceiling.png`: 열린 천장. 실제 구조 광원, 추가 검수 조명 없음.

이전 ImportedRubbleReport의 흰색 완료/외벽 유지 내용은 이번 사용자 지시로 대체되었다. 옛 빌더 스크립트를 다시 실행하면 이전 스타일이 복원될 수 있으므로 최신 씬을 기준으로 작업한다.

## 남은 확인

실제 헤드셋 착용/컨트롤러 도달 범위와 기기 성능은 미검증이다. 외곽 차광 메시의 LOD0 고정으로 메시 비용이 늘었으므로 목표 기기 확정 후 프로파일링이 필요하다. 차광 검증은 기록된 머리 범위와 화면 각도에서의 PC 검사이며, 모든 가능한 물리적 머리 이동에 대한 수학적 보장은 아니다. 기존 XR Interaction Simulator의 hand aim 경고는 환경 수정 범위 밖이다.

## 사용 기준

`unity-cli`, `craft-game-floors`의 scene-lighting/visual-scorecard 및 `urp-postprocessing`의 effect-reference를 적용했다. 완전 매몰이라는 사용자 지시에 따라 일반적인 실내 주변광 기본값은 적용하지 않았다. UI 변경은 없다.

환경 스타일/매몰 형태: 3→4/5, 자연광 전달: 2→3/5로 검토했다. 차광의 근거는 위 광선/픽셀 검사이며 점수는 주관적인 시각 평가다.

확인한 공식 문서: https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/rendering/shadowcastingmode/twosided
