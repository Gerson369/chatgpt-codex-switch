# Security

- Never attach `auth.json`, access/refresh tokens, DPAPI backups or real-account screenshots to an issue or pull request.
- Use synthetic accounts and redact personal information when reporting bugs.
- Saved profiles are encrypted for the current Windows user using DPAPI. Processes running as that user can still access that user's credentials; encryption does not defend against a compromised Windows account.
- The live Codex `auth.json` remains in the location required by Codex. Temporary login directories can remain after a crash.
- The tool has no custom account proxy or upload service. It invokes the installed Codex login and account interfaces.
- Verify release checksums and source provenance. This preview is not code-signed.
- Do not disclose credential-related vulnerabilities or sensitive details in a public issue. If a private GitHub vulnerability-reporting option is available, use it; otherwise request a private contact without posting the sensitive details.
