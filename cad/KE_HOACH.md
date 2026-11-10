# Kế hoạch: vẽ CAD tự động, tốn ít token nhất (ROADMAP Pha 6 + 4b)

**Phạm vi làm ngay (bạn chốt): chỉ MCP CAD = giai đoạn G0, G1, G2.** Các phần còn lại (G3–G9) để lại, ghi vào `cad/TIEN_DO.md` làm việc sau.

## 1. Bối cảnh
Bạn muốn gõ yêu cầu rồi AutoCAD tự vẽ: thiết bị PCCC, đi ống, đi dây báo cháy, đèn exit/EM, sơ đồ nguyên lý từ mặt bằng + mặt đứng, PDF → CAD, vẽ theo mô tả tự do. Chat được ngay trong CAD hoặc từ VS Code / Claude / Gemini, chọn model và thư mục dự án. Thêm bộ cài kiểu "MCP Hub" và tab xem YouTube. Mọi thứ nằm trong `cad/` để dễ sửa.

Gói Claude đang dùng là **Pro** (hạn mức nhỏ), nên thiết kế phải ép token từ gốc chứ không tối ưu sau.

Đã có sẵn: `cad/src/Lenh.cs` (8 lệnh `PCCC_*`, C# 5, ~900 dòng), `cad/test/chay_thu.ps1` (chạy thử không cần mở AutoCAD), `runner/src/engines/claude.ts` (`hoiClaude`: gọi `claude -p` bằng đăng nhập CLI, không API key). ROADMAP Pha 6 (MCP) và Pha 4b (`PCCC_RAI`) chưa làm.

## 2. Cách ép token (xương sống của thiết kế)
| Chỗ tốn token | Cách làm ở đây |
|---|---|
| AI viết từng nét bằng LISP (ảnh Claude + `execute_lisp`) | AI chỉ viết **spec** ngắn (tham số), plugin C# sinh hình. Lưới trục 10 × 5 là 1 dòng spec thay cho cả trang LISP |
| Nhiều lượt gọi công cụ | 1 lần gọi = 1 gói nhiều thao tác = 1 bước Undo |
| Kết quả dài, ảnh chụp mỗi lượt | Chỉ trả số đếm + lỗi + cảnh báo (≤ 2 KB). Ảnh chỉ khi AI xin, trả đường dẫn file |
| AI hỏi lại để tự kiểm | Plugin tự kiểm (đếm, chữ đè, ra ngoài vùng, bước rải) rồi trả ✓ / ⚠ |
| Việc thiết kế lặp lại | Thuật toán trong plugin: rải thiết bị, đi tuyến ống/dây, sơ đồ nguyên lý. AI chỉ đưa tham số |
| Việc bấm tay hằng ngày | Bảng nút trong CAD, 0 token |
| PDF → CAD | Lệnh `PDFIMPORT` có sẵn của AutoCAD + chỉnh tỉ lệ 2 điểm, 0 token |
| Ngữ cảnh phình (hook, plugin, skill, MCP khác) | Khung chat trong CAD gọi AI ở chế độ gầy: chỉ công cụ CAD + từ vựng spec |
| Model đắt | Mặc định Sonnet effort low (hoặc Haiku, Gemini Flash) cho việc vẽ. Opus/Fable chỉ khi cần cân nhắc thiết kế |
| Hết hạn mức giữa chừng | Mọi trạng thái nằm trong file (spec đã chạy, `TIEN_DO`), AI khác (Gemini) mở lên làm tiếp được |

## 3. Kiến trúc
```
Claude Code / VS Code / Antigravity (Gemini) / Codex ─┐
Khung chat trong CAD (gọi claude -p hoặc gemini) ─────┴─> TKPCCC.Mcp.exe (2 công cụ)
                                                              │ ghi file việc
Bảng nút trong CAD (0 token) ─────────┐                       ▼
                                      └─> lệnh PCCC_SPEC <── hàng đợi %LOCALAPPDATA%\TKPCCC\cad\
                                             │  plugin: 1 bước Undo, tự kiểm, trả kết quả gọn
accoreconsole (test, vẽ không cần mở CAD) ───┘
```
- Công cụ MCP: `cad_tt` (trạng thái + đếm), `cad_chay` (chạy gói spec: vẽ, sửa, xoá, đọc). `cad_anh` (ảnh khi cần) để lại, làm cùng bảng UI.
- Hàng đợi bằng file thay cho cổng 127.0.0.1 ghi trong ROADMAP: không mở cổng mạng, không kẹt cổng, file việc chạy lại được để test.
- AI không chạy được LISP hay lệnh tự do, chỉ các thao tác có trong từ vựng.
- Từ vựng spec có một nguồn duy nhất `cad/spec/TU_VUNG.md`. Máy chủ MCP gửi nó cho mọi AI lúc kết nối, nên Gemini/Codex dùng được mà không cần cài skill.
- Máy chủ MCP viết bằng C# (file `.exe`) thay cho Node ghi trong ROADMAP: máy nào có Windows cũng chạy, không phải cài Node/Python.

## 4. Thư mục `cad/` sau khi làm
```
cad/
  README.md  KE_HOACH.md  TIEN_DO.md  LICH_SU.md  cau_hinh.json   sổ sách + cấu hình (máy AI, model, thư mục)
  build.bat          build cả 4 phần bằng csc.exe (C# 5)
  src/    lõi plugin -> TKPCCC.Cad.dll (chạy được trong accoreconsole)
          Lenh.cs (có sẵn) · Spec.cs · Ve.cs · Cau.cs (bơm hàng đợi)   | để lại: Rai.cs · Tuyen.cs · SoDo.cs
  chung/  HangDoi.cs: mã dùng chung plugin, MCP, hub (đường dẫn hàng đợi, JSON)
  ui/     (để lại) bảng trong CAD -> TKPCCC.UI.dll: Bang.cs · Chat.cs · Web.cs
  mcp/    Mcp.cs -> TKPCCC.Mcp.exe
  hub/    (để lại) Hub.cs -> TKPCCC.Hub.exe (cài đặt, đồng bộ cấu hình AI, chẩn đoán)
  spec/   TU_VUNG.md · vi_du/*.json
  test/   chay_thu.ps1 (có sẵn, thêm spec) · thu_mcp.ps1
  TKPCCC.bundle/     (có sẵn)
```
Thư mục gốc (repo TKPCCC) giữ skill + tri thức. Mỗi dự án một thư mục riêng, mặc định `ket_qua/<dự án>/cad/`: `du_an.json` (số liệu thật), DWG, `spec/*.json` đã chạy (chạy lại 0 token). Bài học mới chép ngược về skill và `knowledge/`.

## 5. Ngôn ngữ: C# là chính
- **C#** cho plugin, cầu nối, máy chủ MCP, hub. API chính thức của AutoCAD là .NET; đã có 900 dòng + bộ test; Windows có sẵn trình biên dịch và runtime nên máy khác không phải cài gì; ký số và làm rối mã được.
- **C++ (ObjectARX)**: mạnh nhất và khó dịch ngược nhất, nhưng dễ lỗi bộ nhớ và phải build riêng từng đời AutoCAD. Chỉ dùng khi .NET không làm được.
- **Rust**: an toàn bộ nhớ, khó dịch ngược, nhưng không viết được plugin AutoCAD. Hợp cho một mô-đun nhỏ kiểm bản quyền sau này.
- **Python**: giữ cho script nội bộ (PDF, bảng đối chiếu). Không dùng cho phần giao cho người khác: lộ mã nguồn, phải cài runtime.
- Bảo mật sau này: giấy phép ký số (RSA/ECDSA có sẵn trong .NET, khoá bí mật không nằm trong máy khách hay repo), mã máy, làm rối mã, ký Authenticode. **Repo GitHub đang công khai**: trước khi làm bản quyền phải chuyển sang riêng tư hoặc tách phần đó ra repo riêng.

## 6. Các giai đoạn
Mỗi giai đoạn là một đầu mục: kiểm → ghi `cad/TIEN_DO.md`, `cad/LICH_SU.md` → commit → push, rồi mới sang giai đoạn kế. **Làm ngay: G0, G1, G2. Để lại: G3–G9.**

| G | Làm gì | Xong khi |
|---|---|---|
| 0 | Sổ sách: `cad/README.md`, `KE_HOACH.md` (chép kế hoạch này), `TIEN_DO.md`, `LICH_SU.md`; 1 dòng trỏ từ `TIEN_DO.md` gốc; 1 dòng luật trong `CLAUDE.md` | file có trong repo |
| 1 | Lõi `PCCC_SPEC`: tách phần vẽ khỏi phần hỏi trong `DatThietBi`, `VeOng`; thao tác `dat`, `ong`, `net`, `tron`, `chu`, `kt`, `luoi_truc`, `xoa`, `doc`, `dem`; kết quả JSON gọn | `chay_thu.ps1` ra `TAT CA DAT` với spec mẫu; xem ảnh PNG đúng hình |
| 2 | Cầu nối (`src/Cau.cs`: timer trên luồng chính, không bật trong accoreconsole) + `TKPCCC.Mcp.exe` (`cad_tt`, `cad_chay`) + skill `tkpccc-ve-cad` + `.mcp.json` | từ Claude Code gõ 1 câu, AutoCAD đang mở vẽ được lưới trục; ghi số token đo được |
| 3 | Thuật toán PCCC: `rai` (đầu phun, đầu báo, đèn exit/EM, bình theo vùng), `tuyen` (ống kiểu xương cá, dây nối tiếp), lệnh gõ tay `PCCC_RAI` | test số đếm, bước rải ≤ S; chiều dài tuyến lên `PCCC_BOC` |
| 4 | PDF → CAD: thao tác `pdf` (PDFIMPORT + tỉ lệ 2 điểm); phương án ghi chú trên PDF (`tuong`, `cua`, `net`) → spec | một bản vẽ mẫu vào CAD đúng kích thước |
| 5 | Sơ đồ nguyên lý `so_do`: số đếm theo tầng (mặt bằng) + cao độ tầng (mặt đứng) | sơ đồ báo cháy + chữa cháy cho 1 hồ sơ mẫu |
| 6 | Bảng trong CAD: tab Công cụ (0 token, dropdown từ `vat_tu.json`), tab Chat (chọn máy AI, model, effort, thư mục dự án; hiện token mỗi lượt) | chat trong CAD vẽ được như G2 |
| 7 | Hub: tìm AutoCAD, cài bundle, ghi cấu hình MCP cho các AI có trên máy (có sao lưu), thử bắt tay MCP, chẩn đoán, nhật ký | chạy đúng trên thư mục giả; máy thật sau khi bạn đồng ý |
| 8 | Tab YouTube (WebView2) | mở được video trong bảng |
| 9 | Sau này: bản quyền + bảo mật | — |

## 7. Dùng lại cái có sẵn
- `cad/src/Lenh.cs`: `Chay` (bọc lỗi), `DocDanhMuc`, `TimMa`, `DamBaoLayer`, `DamBaoKieuChu`, `HeSoMet`, `MauHe`, `NapVaoBanVe`, `Duyet` + `BoiCanh` (đếm), `ChonKhu` (tách tầng). Đổi `class Lenh` thành `partial` để file mới gọi thẳng, không đổi quyền truy cập.
- `JavaScriptSerializer` (đã tham chiếu `System.Web.Extensions`) cho JSON ở cả 4 phần, không thêm thư viện.
- `cad/test/chay_thu.ps1`, `.claude/skills/tkpccc-cad-console/scripts/in_png.ps1`.
- Bộ cờ `claude -p` trong `runner/src/engines/claude.ts` (`--setting-sources project,local`, `--strict-mcp-config`, bỏ `ANTHROPIC_API_KEY`) cho khung chat, thêm `--model`, `--effort`, `--mcp-config`, `--output-format stream-json`, `--resume` (CLI 2.1.301 trên máy có đủ).
- `data/vat_tu.json` là nguồn duy nhất cho dropdown. `doc_spec` của skill `tkpccc-ghi-chu-ban-ve` đọc phương án trên PDF.
- Sửa kèm: `cad/build.bat`, `PackageContents.xml` (thêm `PCCC_SPEC`), `STRUCTURE.md`, `ROADMAP.md`, bảng module `CLAUDE.md` (thêm skill `tkpccc-ve-cad`), skill `tkpccc-cad-console` (đường MCP đổi sang `cad/mcp/`).

## 8. Kiểm chứng
- Mỗi giai đoạn: `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1` phải ra `TAT CA DAT`; in PNG rồi xem bằng mắt.
- G2: `cad\test\thu_mcp.ps1` đưa `initialize`, `tools/list`, `tools/call` vào `TKPCCC.Mcp.exe` và so câu trả lời. Đầu-cuối: AutoCAD mở, gọi `cad_chay` với `cad/spec/vi_du/luoi_truc.json`, so số đếm bằng `cad_tt`.
- Đo token: cùng một đề (lưới trục A–J × 1–5 bước 3600 như ảnh tham khảo), ghi `usage` của `claude -p --output-format json` vào `cad/LICH_SU.md`. Mục tiêu: 1 lần gọi công cụ.
- Trước khi báo xong: `npm test`, `python -m pytest thamdinh/test .claude/skills -q`, `chay_thu.ps1`.

## 9. Luật làm việc
- Đã dùng **≥ 85 %** hạn mức (5 giờ hoặc tuần; đo `get_usage` sau mỗi giai đoạn và trước việc lớn): dừng, ghi `cad/TIEN_DO.md` + `cad/LICH_SU.md`, commit + push theo QUY TẮC SỐ 1, rồi hỏi bạn.
- Code các giai đoạn nên chạy Sonnet (đúng frontmatter skill CAD). Fable/Opus chỉ cần cho thuật toán G3, G5.

## 10. Lấy gì từ ảnh tham khảo
- FP AI: khung chat trong CAD, chọn model + effort, đồng hồ token, 1 bước Undo, bảng ✓ tự kiểm, "thêm dự án để vẽ theo số thật".
- NĐD Tool: bảng nút theo hệ, dropdown vật tư / DN / cao độ.
- Claude + `execute_lisp`: bài học ngược (tốn token, phải sửa lại) → dùng spec.
- Skill vẽ cầu: tham số → script sinh hình; tài liệu tham chiếu chỉ đọc khi cần.
- Antigravity + tin nhắn thư mục gốc/dự án: tách skill gốc và thư mục dự án, bài học chép ngược.
- MCP Hub Pro: bộ cài từng bước, đồng bộ cấu hình nhiều AI có sao lưu, thử bắt tay, bảng kết quả, nhật ký. Không làm phần shop.
- ArchViz render: không dùng (ngoài phạm vi PCCC).

## 11. Giả định (sửa nếu sai)
1. "85 %" là đã dùng 85 %, như QUY TẮC SỐ 1.
2. "Bảo mật" là khoá bản quyền khi phát hành và không lộ hồ sơ khách.
3. Hub bản đầu chỉ cài đặt, đồng bộ, chẩn đoán. "Trợ lý TCVN" dùng tab Hướng dẫn của web đã có. "Đa não" là chuyển tiếp giữa các AI qua file trạng thái.
4. Gemini dùng qua Antigravity hoặc Gemini CLI trên máy bạn, không dùng API key.
5. PDF dạng vector, không phải bản scan. AutoCAD 2021.

## 12. Chưa kiểm, rủi ro
- `PDFIMPORT` và `Editor.Command` có chạy trong accoreconsole không; bundle có UI DLL thì accoreconsole tự nạp có báo lỗi không.
- Cờ dòng lệnh của Gemini CLI; CLI có phát % hạn mức không (nếu không, khung chat chỉ hiện token mỗi lượt).
- Đi tuyến bản đầu chưa né tường, là phương án nháp để kỹ sư chỉnh.
- Kiểm đầu-cuối cần bạn mở AutoCAD và tắt mở lại sau mỗi lần build.
- WebView2 phải tải thư viện của Microsoft, sẽ hỏi bạn trước khi tải.
- Tổng khối lượng là nhiều phiên làm việc với gói Pro; G0–G2 là phần phải có trước, các phần sau đổi thứ tự được.
