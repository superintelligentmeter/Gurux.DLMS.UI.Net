# Secured Connections — grouped native layout

Operate-mode extension of the existing Device Properties dialog. User request on 2026-10-31 supersedes the earlier flat secured page and SNRM/Certificate row.

Use native GroupBoxes in reading order: Security, System titles, Keys, Invocation counter. Preserve Segoe UI/application typography, existing colors, original editors, event handlers and property getters/setters. Align caption columns and use 8 logical pixels between control rows and groups. Keep the original dialog width, content-driven height and monitor-height scrolling fallback.

Security contains suite, security and signing. System titles contains client/server titles and the original pre-established-association option. Keys contains the original symmetric fields, optional dedicated key/challenge and existing mode-specific signing selectors. Its original Generate dedicated-key action is captioned Generate keys; Certificate shares its trailing action bar. Disable Keys when Security is None and show a readable hint outside that disabled group. Invocation counter follows the other groups; when the monitor-height cap is reached it remains reachable by native scrolling, while OK/Cancel stay fixed outside the scroll viewport.

Ignore SNRM moves to the existing Frame page, retaining its original checkbox and association-controlled enabled state. The standalone ciphering form retains a Framing page for that control.

Placeholders and inline feedback are presentation-only. Use existing suite lengths (16 bytes for suites0/1;32 for suite2), existing hex parser and ASCII mode. Preserve empty-value acceptance, optional dedicated/broadcast behavior and variable challenge length; never write key values, auto-truncate, invoke generation or change save/property validation. Feedback never echoes key contents.

Finish requires native rendered review, valid/invalid/empty and mode-transition tests, both physical monitor scales, work-area containment, original binding/control checks, zero settings writes and enabled/feature-free builds. No dlms.net changes.