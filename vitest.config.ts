import { defineConfig } from "vitest/config";

// Chỉ test của repo chính: không quét .claude/worktrees (bản sao nhánh khác).
export default defineConfig({
  test: { include: ["backend/**/*.test.ts"], exclude: ["**/node_modules/**", ".claude/**"] },
});
