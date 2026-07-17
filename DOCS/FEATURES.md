# GZ::CTF — Feature Ideas & AI-Enhanced Roadmap

This document lists **features that can be built on top of GZ::CTF**, with emphasis on
**AI-powered capabilities**. Each feature notes the relevant existing architecture so you
know where to plug it in.

> GZ::CTF is ASP.NET Core (backend) + React/Vite (frontend, `ClientApp`).
> Key extension points:
> - **Backend**: `Controllers/`, `Models/`, `Services/`, `Extensions/Startup/`
> - **Frontend**: `ClientApp/src/pages/`, `ClientApp/src/components/`
> - **Config**: `Models/Data/Config.cs`, `Services/Config/`, admin `Settings.tsx`
> - **Writeups**: `Game.WriteupRequired` / `WriteupDeadline`, `WriteupInfoModel`, admin writeup APIs
> - **Real-time**: SignalR `Hubs/` (AdminHub, MonitorHub, UserHub)

---

## 🤖 AI-Powered Features

### 1. AI Auto Grading for Writeups
**Status**: Detailed spec below.
- Auto-evaluates student writeups against an official solution + rubric.
- **Provider-agnostic**: OpenCode, OpenAI-compatible, Ollama, OpenRouter, Gemini, Claude, vLLM, LM Studio.
- Returns structured JSON: `score`, `breakdown`, `feedback`, `strengths`, `weaknesses`.
- **Where to plug in**:
  - Backend: new `Services/AI/` (e.g. `AIGradingService.cs`), `Controllers/WriteupController.cs` or extend `AdminController` writeup endpoints.
  - Models: `Models/Data/Challenge.cs` (add `ModelAnswer`, `Rubric`, `MaxScore`, `AiPromptTemplate`, `AiGradingEnabled`), `Models/Data/Game.cs` or `Submission.cs` (store AI score).
  - Config: `Models/Data/Config.cs` → add `AiProviderConfig` (Provider, BaseUrl, ApiKey, Model, Temperature, MaxTokens, Timeout).
  - Frontend: `pages/games/[id]/` (upload + "🤖 Auto Grade"), `pages/admin/` (score breakdown, Approve/Edit/Regrade).
- **Supported files**: `.md`, `.txt`, `.pdf`, `.docx` (use `PdfPig` / `DocX` libs for extraction).

### 2. AI Hints / Dynamic Hint Generation
Generate contextual hints for a challenge based on the player's current progress.
- Backend: `Services/AI/AiHintService.cs`, extend `GameController` or `ExerciseController`.
- Model: `Challenge` gets `HintBudget`, `AiHintsEnabled`.
- Frontend: hint panel in `pages/games/[id]/challenge`.

### 3. AI Cheat / Collaboration Detection
Extend existing `CheatInfo.cs` with semantic similarity between submissions/writeups.
- Compare writeup embeddings; flag near-duplicates.
- Backend: `Services/AI/PlagiarismService.cs`, hook into `FlagChecker.cs` or submission flow.

### 4. AI Challenge Generator (Training Mode)
Leverage the existing **Exercise mode** (`ExerciseController`, `ExerciseChallenge`) to auto-generate
practice challenges from a syllabus.
- Backend: `Services/AI/ChallengeGenService.cs`.
- Frontend: `pages/admin/` exercise editor.

### 5. AI Writeup Summarizer & Knowledge Base
After a game, auto-summarize all writeups into a searchable knowledge base.
- Backend: `Services/AI/SummarizerService.cs`, store in `Post.cs` or new `KnowledgeBase` model.

### 6. AI-Powered Admin Assistant (Chat)
A SignalR-backed chat in the admin panel that answers "why did this container fail?" using
platform logs (`LogModel.cs`) as context (RAG).
- Backend: `Hubs/AdminHub.cs` extension, `Services/AI/AdminAssistantService.cs`.

### 7. AI Traffic Analysis Helper
The platform already supports TCP-over-WebSocket traffic capture (`ProxyController`,
`Services/Traffic/`). Add AI to summarize captured PCAP-like streams and suggest next steps.

### 8. Natural Language Game/Challenge Configuration
Let admins describe a game in plain text and have AI produce the `GameInfoModel` / challenge config.
- Backend: `Services/AI/ConfigGenService.cs`, extend `EditController`.

---

## 🔧 General (Non-AI) Features

### 9. Team Merge / Split Tool
Admin tool to merge two teams' scores or split a team post-event.
- Backend: `AdminController`, `Participation.cs`, `Team.cs`.

### 10. Challenge Dependency Graph Viewer
Visualize `Dependency.cs` relationships in the admin UI.
- Frontend: new component in `pages/admin/games/[id]/`.

### 11. Bulk Writeup Download as ZIP with Metadata CSV
Extend `EditController` "Delete All WriteUps" area with export.
- Backend: `Services/Transfer/` or new `ExportService`.

### 12. SMTP Test Button + Email Template Editor
Enhance `Mail/` service with a test-send and WYSIWYG template.
- Frontend: `pages/admin/Settings.tsx`.

### 13. Container Resource Quotas per Team
Extend `Services/Container/` to enforce CPU/memory limits per participation.
- Model: `Participation.cs` or `GameInstance.cs`.

### 14. OAuth / SSO Login Providers
Extend `AccountController` with generic OAuth (the upstream already has copilot branches for this).
- Backend: `Providers/`, `AccountController.cs`.

### 15. Public Scoreboard Embed / Widget
A read-only embeddable widget for external sites.
- Frontend: new `pages/embed/`, backend: `InfoController` or `GameController` public endpoint.

### 16. Challenge Category Icons / Custom Branding per Game
Extend `Game.cs` with `IconUrl`, `BannerUrl`; use in `pages/games/Index.tsx`.

### 17. Automated Backup to Object Storage
Extend `Storage/` to schedule DB + files backup to MinIO/S3.
- Backend: `Services/CronJob/`.

### 18. Webhook Notifications on Solve
Fire webhooks on first-solve / challenge solve (upstream has `feat/webhook` branch).
- Backend: `GameEvent.cs` → `Services/Webhook/`.

---

## 📋 Detailed Spec: AI Auto Grading for Writeups

### Overview
Implement an **AI Auto Grading** feature for GZCTF that automatically evaluates student writeups
after submission. The feature is **AI-provider agnostic** — administrators can use:

- OpenCode models
- Any OpenAI-compatible API
- Ollama (local models)
- OpenRouter
- Gemini
- Claude
- OpenAI
- Any custom configured model

The grading backend must not depend on a specific provider.

### Workflow
1. Student completes the challenge.
2. Student uploads a `README.md` (or `.txt`, `.pdf`, `.docx`) writeup.
3. Instructor clicks **"Auto Grade"** (or enables automatic grading).
4. The system:
   - Loads the official solution (Model Answer).
   - Loads the grading rubric.
   - Sends both, together with the student's report, to the configured AI model.
5. The AI returns a structured evaluation.
6. The system displays the generated score and feedback.
7. The instructor may edit the score before approving it.

### Challenge Configuration
Each challenge should support:
- Official Solution / Model Answer (Markdown)
- Grading Rubric
- Maximum Score
- AI Prompt Template (optional override)
- Enable/Disable AI Auto Grading

### Rubric Example
```
Recon: 20
Enumeration: 20
Exploitation: 30
Privilege Escalation: 20
Report Quality: 10
```

### Expected AI Output
```json
{
  "score": 91,
  "breakdown": {
    "Recon": 20,
    "Enumeration": 18,
    "Exploitation": 28,
    "PrivilegeEscalation": 17,
    "ReportQuality": 8
  },
  "feedback": [
    "Excellent reconnaissance.",
    "SMB enumeration could be more complete.",
    "Privilege escalation explanation needs more detail.",
    "Missing mitigation recommendations."
  ],
  "strengths": ["...", "..."],
  "weaknesses": ["...", "..."]
}
```

### AI Prompt (default)
Instruct the model to:
- Compare the student's writeup against the official solution.
- Follow the grading rubric.
- Reward correct methodology, even if different from the official solution.
- Avoid penalizing valid alternative approaches.
- Ignore formatting unless Report Quality is being graded.
- Return JSON only.
- Never hallucinate challenge steps.
- Base the evaluation only on the provided inputs.

### Supported File Types
- Markdown (`.md`)
- Text (`.txt`)
- PDF
- DOCX

### AI Configuration (Admin)
- Provider
- Base URL
- API Key
- Model Name
- Temperature
- Max Tokens
- Timeout

Works with any OpenAI-compatible endpoint:
OpenCode, Ollama, OpenRouter, OpenAI, Gemini, Claude, LM Studio, vLLM.

### UI
**Challenge Submission**
```
Upload Writeup
[ README.md ]
[ Upload ]
[ 🤖 Auto Grade ]
```

**Instructor View**
```
AI Score: 91 / 100
Breakdown:
  Recon                 20/20
  Enumeration           18/20
  Exploitation          28/30
  Privilege Escalation  17/20
  Report Quality         8/10
Feedback:
  • Excellent reconnaissance.
  • Improve SMB enumeration.
  • Add more explanation for privilege escalation.
[ Approve ] [ Edit Score ] [ Regrade ]
```

### Future Enhancements
- Batch grading for multiple submissions.
- AI plagiarism detection between student writeups.
- Compare against previous submissions.
- Confidence score for grading.
- Multiple AI providers with voting/ensemble grading.
- Store grading history for auditing.
- Allow instructors to regenerate feedback without changing the score.

---

## 🗺️ Implementation Checklist (AI Auto Grading)

- [ ] Add `AiProviderConfig` to `Models/Data/Config.cs` + migration
- [ ] Add `ModelAnswer`, `Rubric`, `MaxScore`, `AiPromptTemplate`, `AiGradingEnabled` to `Challenge`
- [ ] Create `Services/AI/IAIGradingService.cs` + `OpenAiCompatibleGrader.cs`
- [ ] Add grading endpoint to admin writeup controller
- [ ] Store AI result on `Submission` / new `WriteupGrade` model
- [ ] Frontend: upload + Auto Grade button (`pages/games/[id]/`)
- [ ] Frontend: instructor grading view (`pages/admin/`)
- [ ] PDF/DOCX text extraction utility
- [ ] Unit tests in `GZCTF.Test/`
