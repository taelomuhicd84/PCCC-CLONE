# Claude Usage Meter

Sidebar "Claude Usage" trong VS Code:
- Account & Usage: token hôm nay / 5 giờ / 7 ngày, biểu đồ 7 ngày, % hạn mức nhập tay.
- Session Manager: đọc `~/.claude/projects/**/*.jsonl`, bấm một phiên để chạy `claude --resume <id>` trong terminal.

Không gọi mạng, không đọc `.credentials.json`.

Cài:

```
npx @vscode/vsce package --allow-missing-repository --skip-license
code --install-extension claude-usage-meter-0.1.0.vsix
```
