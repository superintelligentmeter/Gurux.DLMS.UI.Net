# Supporting-dialog UI checks

Build `tests/UiDialogReviewTests/UiDialogReviewTests.csproj` in Release, then run the built executable with `audit OUTPUT` or `monitors OUTPUT`. An optional third argument filters a type name. Use a fresh absolute output directory for each run.

The suite discovers all compiled supporting Forms (43 at the time of this review), constructs explicit offline fixtures, and renders their default state, tabs, compact size and restored size. `monitors` also moves each dialog across the available physical displays and exercises minimize/restore. The current test workstation provides 96 and 120 DPI.

Keyboard checks preserve the original Enter/OK and Escape/Cancel mappings; recorded tab traversal is available in `results.json`. Ciphering tests cycle suite/signing modes, check that controls are retained without duplication, and verify that the bottom key action can be reached by scrolling. The target-object dialog has a regression check for its supplied read-only attribute index.

Each dialog runs in an isolated process with a 30-second timeout. The fixtures use disposable object collections and temporary key/certificate folders. Key forms receive a null system title so their constructors cannot request certificates. No transport is created and no key generation, certificate service, meter action or preference save is invoked. Process-local setting values are cleared before construction; user settings files are untouched.

Artifacts include per-dialog JSON, screenshots, contact sheets, keyboard order, geometry candidates and a coverage ledger. Overlaps and ancestor clipping require review: controls below a scrollable viewport are expected and separately checked for reachability. Native `DrawToBitmap` limitations apply. These checks do not establish screen-reader, high-contrast, physical meter or exhaustive protocol-variant coverage.

The companion `UiReviewTests` covers every object view. Director's `HostUiReviewTests` covers host dialogs and transactional editing, and its existing dashboard suites cover the optional dashboard module.

The `combos OUTPUT` mode inventories every native combo and captures wide/narrow layouts on each physical monitor, then minimizes/restores and returns to the first monitor. Fixed-size dialogs retain their supported size. It records native preferred heights, immediate-parent clipping, decorative-frame candidates, font/DPI, DropDownStyle and DrawMode. Safe enabled selectors are opened and closed without selecting a different item; their text, selection and native styles must remain unchanged. Ciphering suite 1 is also rendered to cover its key selectors. Combo geometry candidates must be reviewed against screenshots; native popup menus are not included by DrawToBitmap.

Fixtures explicitly apply the same ApplicationTypography used by the production Idle handler before measuring controls, since the bounded DoEvents pump does not trigger that normal idle path. The output includes the exact tested UI assembly SHA-256.

Native height comparisons allow the observed two-pixel 120-to-96 DPI rounding/cache difference; native item-rectangle/font fit and immediate-parent containment remain separate checks. Device ciphering fixtures explicitly exercise GeneralSigning and StaticUnifiedModel so all four key selectors are rendered on both monitors.

The complete scalar-width and structured-area audit is documented in ../UiLayoutAudit.README.md. Its layout-only mode uses fresh physical-monitor fixtures and preserves the behavioral suites above.
