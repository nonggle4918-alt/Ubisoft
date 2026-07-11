# Operations Rules

> 이 파일은 AI가 이 프로젝트를 작업할 때 따라야 할 규칙, 제약사항, 워크플로우를 정의합니다.
> 새 AI 세션 시작 시 반드시 이 파일과 `CONTEXT.md`를 먼저 읽어야 합니다.

---

## 1. Communication

- **Language**: 사용자와의 모든 대화는 **한국어**로 진행
- **Tone**: 간결하고 직설적으로. 불필요한 설명/요약/서문/맺음말 생략
- **Length**: 특별한 요청이 없으면 답변은 3~4줄 이내로 제한
- **Emoji**: 사용자가 요청하지 않는 한 **절대 사용 금지**
- **Output format**: GitHub-flavored markdown, monospace 렌더링

## 2. Approval Workflow

- **모든 변경 전에 사용자 승인 필수**
- 각 단계를 실행하기 전에 **무엇을 할 것인지, 왜 하는지** 간단히 설명
- 변경 적용 후 결과를 보고

## 3. Code Style

- **주석 금지**: 코드에 주석을 절대 추가하지 않음 (`//`, `/* */`, `///` 모두 금지)
- **기존 컨벤션 준수**: 주변 코드의 스타일, 네이밍, 패턴을 그대로 따름
- **새 파일 생성 자제**: 기존 파일 수정을 우선시. 정말 필요한 경우에만 새 파일 생성
- **수정 전 읽기**: `edit`/`write` 도구 사용 전에 반드시 `Read`로 파일 내용 확인
- **라이브러리 확인**: 새 라이브러리 사용 전에 `package.json`/`manifest.json` 등에서 이미 사용 중인지 확인

## 4. Project Context

- **Unity**: 6000.3.19f1, URP, 2D mode
- **Camera**: Orthographic, Size=5, Position=(3.5, 3.5, -10)
- **Grid**: 8×8, integer positions (0,0)~(7,7), tile size = 1.0×1.0 world units
- **GridManager/PieceManager**: Transform position 고정 (0,0,0)
- **Gold**: Start=100, Enemy kill=+10
- **Lives**: Start=20, Enemy reaches end=-1
- **Wave timing**: First wave after 10s, subsequent waves 5s after previous ends
- **Waypoints**: 36 points clockwise perimeter, start (-1,-1)
- **Wave N composition**: Pawn×(2+N) + Queen×max(0, N-2)
- **Projectile cooldown**: Tracked per piece via `Dictionary<Piece, float> lastAttackTime`
- **Phase 상태**: `CONTEXT.md`의 Phase Status 참고

## 5. Tools & Skills

- **씬/에셋/GameObject 조작**: Unity MCP 도구 사용 (`gameobject-find`, `gameobject-component-get/modify/add`, `assets-find`, `assets-get-data`, etc.)
- **코드 수정**: `script-update-or-create`보다 `edit`/`write` 우선. `script-execute`는 임시 실행/검증에만 사용
- **Play 모드 제어**: `editor-application-set-state` (via bash + unity-mcp-cli)
- **검증**: 변경 후 Play 모드 진입하여 에러 확인 (`console-get-logs`)
- **프리팹 수정**: `assets-prefab-open`으로 편집 모드 진입 → 수정 → `assets-prefab-close` (save=true)

## 6. Context 유지보수

- **`CONTEXT.md`** (`Y:\Unity\Ubisoft\CONTEXT.md`):
  - 새 Phase 완료 시 Phase Status 업데이트
  - 새 스크립트/프리팹/에셋 추가 시 Project Structure 반영
  - 스크립트 변경(인터페이스/주요 로직) 시 Script Architecture 업데이트
  - Scene 변경 시 Scene Setup 테이블 업데이트
  - 버그 수정/이슈 해결 시 Known Issues 섹션에 기록
  - Phase 시작 시 Next Steps 업데이트
- **`RULES.md`** (`Y:\Unity\Ubisoft\.opencode\RULES.md`):
  - 워크플로우나 규칙에 변경이 있을 때만 업데이트
- **README/Guidelines 문서를 함부로 생성하지 말 것**
- **에셋 정리**: 불필요하게 생성된 파일/폴더는 정리

## 7. Git

- **명시적으로 요청받을 때만 커밋**
- 커밋 전: `git status`, `git diff`, `git log --oneline -10` 확인
- 관련 없는 파일은 스테이징하지 말 것
- 시크릿/키 값 커밋 금지
