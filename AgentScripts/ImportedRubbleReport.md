# 가져온 잔해 에셋 적용

## 확인한 패키지

- `Assets/RubbleAndDebris`: 잔해 더미, 콘크리트/벽돌 조각, 노출 철근 벽체. 이번 환경 교체에 사용.
- `Assets/StompyRobot`: SRDebugger/SRF 디버깅 도구. 이번 작업에서 설정하거나 게임에 추가하지 않음.

## 변경 파일

- `Assets/_Project/Scenes/ConfinedPrototype.unity`: 기존 구조물/파편 329개의 메시와 재질 교체. 실제 프리팹 6개를 추가해 전면 잔해층, 하체 주변, 양옆 벽과 천장을 보강. 작은 부유 장식 64개와 임시 균열/골재/철근 장식 34개의 렌더링을 비활성화. 구조 천장 프리팹의 LOD0 렌더러 2개를 기존 엔딩 차광 판정에 추가.
- `Assets/_Project/Art/ImportedRubble/` 및 메타 파일: 원본 메시의 UV와 서브메시를 유지한 배치용 메시 사본, 원본 텍스처를 참조하는 URP Lit 재질 사본 13개. 현재 씬은 그중 7개 재질 사용. 보정 과정의 초기 메시 사본도 삭제하지 않고 보존.
- `Assets/_Project/Scenes/Backups/BeforeImportedRubble_20261106_094847.unity` 및 메타 파일: 작업 전 전체 씬 백업.
- `AgentScripts/InspectImportedRubble.cs`, `BuildImportedRubble.cs`, `DressImportedRubble.cs`, `ImportedRubblePreview.cs`, `VerifyImportedRubble.cs`: 검사, 교체, 배치, 캡처, PC 통합 검증 도구. Assets 밖에서 실행하며 게임 빌드에 포함되지 않음.
- 이 보고서.

원본 유료 에셋의 메시/재질/프리팹, XR Origin, 입력, 휴대폰 UI, 기존 게임 규칙 코드는 수정하지 않았다. 기존 상호작용 물체의 위치와 충돌체를 유지했다. 새 프리팹의 추가 충돌체는 비활성화했다. 새 벽/천장은 기존 여진 및 구조 대상 아래에 배치해 기존 동작을 따른다.

## 검증

`Temp/ImportedRubbleReview/Verification.json`의 최종 PC Play Mode 결과: PASS.

- 프레임 진행 확인: `job wait`, frameCount 2940 → 2973, met=true.
- 가져온 재질을 사용하는 메시 렌더러 345개(비활성 LOD 포함), 사용 재질 7개에 URP Lit 및 알베도 텍스처 존재.
- XR Origin 1개.
- 여진의 천장 하강, 18초 이후 Isolation 전환, 벽체 파편 6개 분리와 원상 복구.
- 기존 구조 대상 19개가 움직이며 새 자식 천장도 함께 제거됨. 엔딩 완료/화이트아웃/복구 정상.
- 검증 실행 중 HMD 카메라 Transform 변경 없음. 실제 HMD는 연결해 시험하지 않음.
- Unity compilationFailed=false, compiling=false, consoleErrors=0. 검증 스크립트 컴파일 진단 없음.
- 시작 시 정지 상태였으므로 작업용 Play Mode를 종료함. Run In Background 오버라이드를 해제해 프로젝트의 기존 true 값을 유지. Auto-tick은 작업 전부터 활성 상태였으며 동일한 16ms 설정을 유지.

## 화면 및 남은 확인

`Temp/ImportedRubbleReview/Before_Front.png`와 `After_Front.png`, `After_Phone.png`, `After_Rear.png`에 비교 화면을 저장했다. **형태를 확인하기 위한 임시 검수 조명이 있는 캡처**다. 이 조명은 씬에 저장하지 않았으며 실제 게임의 암전/휴대폰 조명은 유지했다. 원래 카메라의 위치와 별도로 만든 임시 카메라의 방향으로 검토했다.

작업 중 캡처 코드에서 사용한 stereoTargetEye가 URP 경고를 냈다. 해당 호출을 제거했고 최종 캡처는 그 호출을 사용하지 않는다. 과거 콘솔 기록은 지우지 않았다. 기존 XR Interaction Simulator의 hand aim 설정 경고는 이번 환경 작업 범위 밖이다.

실제 헤드셋의 시야, 손 뻗기 여유, 프레임 시간과 메모리 사용은 확인이 필요하다. 기존 물체의 충돌체를 유지했으므로 깨진 외곽의 세부 모양과 충돌면이 완전히 일치하지는 않는다. 새 잔해를 집을 수 있는 기능은 추가하지 않았다.

## 적용한 검토 기준

`unity-cli`와 `craft-game-floors`의 `art-direction.md`, `placeholder-assets.md`, `visual-scorecard.md`, `playmode-verification-loop.md`를 읽었다. 유료 에셋 사용이라는 사용자 지시에 따라 임시 도형보다 가져온 실제 메시를 우선했다. 프로젝트에 기존 미커밋 작업이 있으므로 별도 커밋 대신 전체 씬 백업을 복원점으로 사용했다. 기존 어두운 재난 공간의 조명/색감과 XR 규칙을 유지하고 새 색상 표준이나 UI를 만들지 않았다.

범위 내 주관적 화면 평가(검수 조명): 색감 3/5 유지, 환경 스타일 통일 2→3/5, 조명 3/5 유지, 붕괴 구조의 형태 2→3/5. UI와 캐릭터 평가는 이번 환경 변경 범위에서 제외했다. 여진/구조 피드백은 위 통합 검증으로 확인했다.

참고한 Unity 6000.3 공식 문서:

- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/prefabutility/instantiateprefab
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/application/runinbackground
