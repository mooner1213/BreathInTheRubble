# 구조 엔딩 속도와 햇빛 수정

2026-11-05. RescueEndingReport.md의 초기 34초 연출을 수정한 최신 상태다.

사용자 피드백: 잔해가 너무 빨리 치워지고 외부 빛이 약하며, 잔해 제거와 무관하게 늦은 흰색 페이드가 시작됐다.

## 변경

- 전체 엔딩 34초 → 60초. 주요 잔해의 제거 시간은 7초 → 9.5초다. 큰 덮개와 그 밑의 겹친 슬래브를 같은 작업 묶음으로 이동시켜, 열린 틈이 다른 천장층에 가려지는 문제를 줄였다.
- 첫 작업 묶음은 3초, 다음 큰 묶음은 14초, 전면 가장자리는 26초부터 진행한다. 뒤쪽 조각은 37/44/47/50초 등으로 나누며 마지막 제거는 59.5초에 끝난다.
- 제거 초반에는 부착 가장자리를 먼저 들어 올리고, 이후 옆으로 천천히 빼낸다. 카메라나 손 추적은 움직이지 않는다.
- 약한 스포트라이트의 출력 증가를 대신해 강한 방향성 태양광과 실제 잔해 그림자를 사용한다. 외부 밝은 면은 HDR 재질로 표현한다.
- 틈의 빛은 12개 그림자 샘플로 제한한 산란 패스로 보강한다. 실제 깊이와 태양 그림자를 사용하며, 엔딩 때만 렌더링하고 후반에는 끈다.
- 35개 개구부 샘플과 주요 천장 제거 진행도를 이용해 노출·Bloom·눈부심을 연결했다. 고정된 마지막 11초 페이드 타이머는 제거했다.
- 초반에는 강한 빛과 어두운 잔해의 대비를 유지한다. 흰색 전환은 개구부가 충분히 열린 뒤 점차 늘린다. HDR 노출이 반투명 흰색을 너무 일찍 포화시키지 않도록 보정했다.
- 기존 34초 작업음은 엔딩 동안 반복하여 후반이 무음이 되지 않도록 했다.

## 확인된 렌더링 원인과 수정 범위

현재 활성 Performance URP Config는 HDR=false, 추가 조명 그림자=false였다. 연결된 Android Preset 렌더러에는 postProcessData도 없었다. 따라서 초기 스포트라이트의 그림자와 Bloom 설정이 의도한 효과를 내지 못했다.

기존 공통 렌더링 자산은 수정하지 않았다. 엔딩 전용 RescueHDRPipeline과 RescueRenderer를 추가해 HDR과 기존 Standalone Preset의 후처리 리소스를 연결했다. 엔딩 시작에만 적용하고 복원 시 원래 Quality Pipeline, 카메라 후처리/깊이, RenderSettings.sun을 돌린다. 태양 그림자 맵은 전용 설정에서 2048로 제한했다. Bloom은 Quarter, 최대 3회, 고품질 필터 꺼짐을 유지한다.

## 변경 파일

- Assets/_Project/Scripts/RescueEndingDirector.cs
- Assets/_Project/Art/RescueWhiteout.shader
- Assets/_Project/Art/RescueSunShafts.shader (추가)
- Assets/_Project/Art/RescueEnding/RescueHDRPipeline.asset (추가)
- Assets/_Project/Art/RescueEnding/RescueRenderer.asset (추가)
- Assets/_Project/Art/RescueEnding/SunlitExterior.mat, SunShafts.mat (추가)
- Assets/_Project/Scenes/ConfinedPrototype.unity
- AgentScripts/TuneRescueSunlight.cs (조사/설정/화면 검토용 추가)
- AgentScripts/VerifyRescueEnding.cs (엔딩 길이에 맞게 완료 검사 수정)
- ExperienceRoadmap.md와 이 문서, Unity 신규 메타 파일

수정 전 씬: Assets/_Project/Scenes/Backups/BeforeSlowerSunlitRescue_20261105_184130.unity. 반복 검토 중 추가 백업도 보존했다.

## 검증

- Unity 재컴파일 오류 0, 경고 0. 실제 렌더링 뒤 최종 Console groundTruth의 consoleErrors=0, compilationFailed=false.
- VerifyRescueEnding.Run PASS: 기존 두 조건, 60초 완료, 재시작, 천장/카메라 복원, HMD/양손 위치 유지.
- TuneRescueSunlight.Review PASS: 0/8/18/30/42/54/60초 캡처 및 원래 Pipeline 복원 확인.
- Temp/SunlitRescueReview/At_08s.png: 흰색 덮임 0이지만 이미 틈의 햇빛과 Bloom이 보인다.
- At_18s.png: 약 43%의 가중 제거 진행과 약 66%의 개구부 샘플이 열려 있다. 흰색 덮임은 거의 0이며 외부 빛이 확장된다.
- At_30s.png와 At_42s.png: 잔해 윤곽과 비치는 면을 유지하며 눈부심이 증가한다.
- At_54s.png와 At_60s.png: 시야가 빛으로 채워진다. 화면 검토는 임시 카메라를 같은 머리 위치에 두고 위쪽을 보는 PC 캡처이며, 검사 조명은 넣지 않았다.
- 개발 도중 생긴 햇빛 셰이더의 HLSL 예약어 오류는 수정했다. Pipeline의 누적 오류 기록과 현재 콘솔 오류 0건을 구분한다.
- 검증 Play Mode를 종료했다. 실제 헤드셋의 눈부심 편안함/양안 렌더링/GPU 성능은 아직 검증하지 않았다.

기존 Unity PC 테스트 창의 '엔딩 시작 / 재시작' 버튼으로 최신 연출을 바로 확인한다. 두 구조 조건과 PC 버튼 동작은 변경하지 않았다.
