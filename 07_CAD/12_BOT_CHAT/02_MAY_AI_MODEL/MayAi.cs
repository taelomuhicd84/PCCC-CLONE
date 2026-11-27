// Máy AI (Claude / Antigravity), model, effort, gọi CLI, đọc dòng trả về. Tách từ BotChat.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using AcAp = Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices;

namespace TKPCCC
{
    partial class BotChat
    {
        ModelAi ModelDang()
        {
            if (ai == 0) return DanhMucModel.Claude.FirstOrDefault(m => m.Id == modelClaude) ?? DanhMucModel.Claude[0];
            List<ModelAi> ds = DanhMucModel.Antigravity();
            return ds.FirstOrDefault(m => m.Effort == null ? m.Id == modelAgy : modelAgy.StartsWith(m.Id + "-") && m.Effort.Contains(modelAgy.Substring(m.Id.Length + 1)))
                ?? ds.FirstOrDefault(m => m.Id == "gemini-4-argon") ?? ds[0];
        }

        string EffortDang()
        {
            ModelAi m = ModelDang();
            if (m.Effort == null) return null;
            string e = ai == 0 ? effortClaude : (modelAgy.StartsWith(m.Id + "-") ? modelAgy.Substring(m.Id.Length + 1) : "high");
            return m.Effort.Contains(e) ? e : m.Effort.Contains("high") ? "high" : m.Effort.Last();
        }

        /// Tên model gửi cho CLI: Claude = id; agy = gốc-mức (hoặc id model không mức).
        string ModelCli()
        {
            ModelAi m = ModelDang();
            return ai == 0 || m.Effort == null ? m.Id : m.Id + "-" + EffortDang();
        }

        // Như Claude Code: xhigh hiện là "Extra".
        static string Hoa(string e) { return e == null ? "" : e == "xhigh" ? "Extra" : char.ToUpper(e[0]) + e.Substring(1); }

        void VeModel()
        {
            ModelAi m = ModelDang();
            string e = EffortDang();
            nutModel.Text = m.Ten + (e != null ? "  " + Hoa(e) : "") + (ai == 0 && ultra ? " · Ultra" : "") + "  ▾";
            if (nutModel.Parent != null) nutModel.Location = new Point(nutModel.Location.X, 4);
        }

        void ChonModel(ModelAi m, string e)
        {
            if (p != null) { trangThai.Text = "Đang chạy: đợi xong (hoặc Dừng) rồi mới đổi model."; return; }
            ai = m.Ai;
            if (ai == 0)
            {
                modelClaude = m.Id;
                if (e != null) effortClaude = e;
                GiaoDien.LuuCaiDat("model", modelClaude);
                GiaoDien.LuuCaiDat("chat_effort", effortClaude);
            }
            else
            {
                string e2 = m.Effort == null ? null : (e ?? (m.Effort.Contains("high") ? "high" : m.Effort.Last()));
                modelAgy = e2 == null ? m.Id : m.Id + "-" + e2;
                GiaoDien.LuuCaiDat("model_gemini", modelAgy);
            }
            GiaoDien.LuuCaiDat("chat_ai", ai == 0 ? "claude" : "gemini");
            VeModel();
            trangThai.Text = ai == 0 ? "Claude: đọc, ghi file trong dự án, vẽ, sửa CAD." : "Antigravity: chỉ đọc + trả lời (chưa vẽ CAD).";
        }

        /// Menu model như khung chat mẫu: nhóm Claude (Opus, Sonnet các đời), nhóm Antigravity (Gemini 4, Gemini 3.8 Flash, ...), rồi các mức effort của model đang chọn.
        void MenuModel()
        {
            ModelAi dang = ModelDang();
            string eDang = EffortDang();
            ContextMenuStrip m = new ContextMenuStrip();
            Action<string, List<ModelAi>> nhom = delegate(string ten, List<ModelAi> ds)
            {
                m.Items.Add(Nhom(ten));
                foreach (ModelAi x in ds)
                {
                    ModelAi xx = x;
                    ToolStripMenuItem i = new ToolStripMenuItem(x.Ten, null, delegate { ChonModel(xx, null); });
                    i.Checked = x.Ai == dang.Ai && x.Id == dang.Id;
                    m.Items.Add(i);
                }
            };
            nhom("Claude  (claude: đọc, ghi file, vẽ, sửa CAD)", DanhMucModel.Claude);
            m.Items.Add(new ToolStripSeparator());
            nhom("Antigravity  (agy: chỉ đọc)", DanhMucModel.Antigravity());
            if (dang.Effort != null)
            {
                // Người dùng 27/11: nhóm Effort như Claude Code (dòng mô tả mờ, Medium "Recommended", xhigh = Extra).
                m.Items.Add(new ToolStripSeparator());
                m.Items.Add(Nhom("Effort  (" + dang.Ten + ")"));
                m.Items.Add(Nhom("Effort cao hơn = trả lời kỹ hơn, lâu hơn, tốn hạn mức hơn"));
                foreach (string e in dang.Effort)
                {
                    string ee = e;
                    ToolStripMenuItem i = new ToolStripMenuItem(Hoa(e), null, delegate { ChonModel(dang, ee); });
                    i.Checked = e == eDang;
                    if (e == "medium") i.ShortcutKeyDisplayString = "Recommended"; // chữ mờ bên phải
                    m.Items.Add(i);
                }
            }
            if (dang.Ai == 0)
            {
                ToolStripMenuItem u = new ToolStripMenuItem("Ultracode", null, delegate { DatUltracode(!ultra); });
                u.Checked = ultra;
                u.ShortcutKeyDisplayString = "nhiều agent";
                u.ToolTipText = "Claude điều phối nhiều agent (công cụ Agent) cho việc lớn; tốn hạn mức hơn nhiều.";
                m.ShowItemToolTips = true;
                m.Items.Add(u);
                MenuTk(m);
            }
            HienMenu(m, nutModel);
        }

        // ---------- Dự án ----------

        /// <repo>\07_CAD suy từ nguon_vat_tu.txt (build.bat ghi <repo>\07_CAD\..\03_DU_LIEU\vat_tu.json). null = không thấy.
        static string ThuMucCad()
        {
            string f = Path.Combine(Path.GetDirectoryName(typeof(BotChat).Assembly.Location), "nguon_vat_tu.txt"); // build.bat ghi cạnh TKPCCC.BotChat.dll
            if (!File.Exists(f)) return null;
            string vt = Path.GetFullPath(File.ReadAllText(f).Trim());
            string cad = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(vt)), "07_CAD");
            return File.Exists(Path.Combine(cad, ".mcp.json")) ? cad : null;
        }

        /// Tìm file chạy trên PATH và các thư mục thêm; quaCmd = file .cmd (npm) phải chạy qua cmd. null = không thấy.
        static string Tim(string[] ten, string[] them, out bool quaCmd)
        {
            quaCmd = false;
            List<string> thu = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';').Where(d => d.Trim() != "").ToList();
            thu.AddRange(them);
            foreach (string t in ten)
                foreach (string d in thu)
                {
                    string f;
                    try { f = Path.Combine(d.Trim().Trim('"'), t); } catch (System.Exception) { continue; }
                    if (!File.Exists(f)) continue;
                    quaCmd = t.EndsWith(".cmd");
                    return f;
                }
            return null;
        }

        static string TimAgy()
        {
            bool quaCmd;
            return Tim(new[] { "agy.exe" }, new[] { Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "agy", "bin") }, out quaCmd);
        }

        internal static string Ngoac(string s) { return KhoPhien.Ngoac(s); }

        /// Lời nhắn gửi AI: dau (vd bản vẽ của lời nhắn) + thư mục gắn kèm + vùng chọn CAD + ảnh + câu. kem = dòng đính kèm hiện trong bong bóng.
        string Soan(string cau, string dau, out string kem)
        {
            List<string> co = thuMuc.Where(Directory.Exists).ToList();
            List<string> anhCo = anh.Where(File.Exists).ToList();
            string vung = MoTaVung(ai == 0);
            kem = (vung != "" ? "\n[vùng chọn " + vungId.Length + " đối tượng]" : "") + (anhCo.Count > 0 ? "\n[" + anhCo.Count + " ảnh]" : "");
            return (dau ?? "") + (co.Count == 0 ? "" : "Thư mục dự án gắn kèm (đọc khi cần làm ngữ cảnh):\n" + string.Join("\n", co.Select(d => "- " + d)) + "\n\n")
                + vung
                + (anhCo.Count == 0 ? "" : "Ảnh đính kèm (mở xem bằng công cụ Read):\n" + string.Join("\n", anhCo.Select(f => "- " + f)) + "\n\n")
                + cau;
        }

        /// Chạy CLI với lời nhắn cau. false = chưa chạy được (thiếu repo / CLI).
        bool Gui(string cau)
        {
            string cad = ThuMucCad();
            if (cad == null) { trangThai.Text = "Không thấy thư mục repo (07_CAD\\.mcp.json): chạy 07_CAD\\build.bat từ repo TKPCCC."; return false; }
            string model = ModelCli(), effort = EffortDang() ?? "high";
            if (aiPhien != ai) { phien = null; linkTen = null; tach = false; noiChung = false; }
            string lam = ThuMucLam() ?? cad;
            bool laCad = string.Equals(lam.TrimEnd('\\'), cad.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
            List<string> co = thuMuc.Where(Directory.Exists).ToList();
            List<string> anhCo = anh.Where(File.Exists).ToList();
            // Dự án khác 07_CAD\: vẫn gắn 07_CAD\ (skill vẽ CAD tkpccc-ve-cad, luật) bằng --add-dir.
            // Claude luôn gắn thư mục ảnh: ảnh đính kèm lời nhắn gửi gấp giữa lượt cũng đọc được.
            if (ai == 0) Directory.CreateDirectory(THU_MUC_ANH);
            string themDir = string.Concat(co.Select(d => " --add-dir " + Ngoac(d))) + (anhCo.Count > 0 || ai == 0 ? " --add-dir " + Ngoac(THU_MUC_ANH) : "")
                + (laCad ? "" : " --add-dir " + Ngoac(cad));
            string coSkill = "";
            foreach (string d in skill.Where(Directory.Exists))
            {
                string tienTo, ms;
                coSkill += ai == 0 ? KhoPhien.CoSkill(d, out tienTo, out ms) : " --add-dir " + Ngoac(d);
            }
            // Người dùng 19/11: lời nhắn gắn bản vẽ đang hiện lúc gửi (máy chủ MCP nhận qua env, xem KhoPhien.FileMcp): việc của lượt này chạy trên bản vẽ đó
            // dù người dùng sang tab khác. Báo AI khi hội thoại mới / đổi bản vẽ để nó không nhận nhầm tên file cũ trong hội thoại.
            AcAp.Document dv = AcAp.Core.Application.DocumentManager.MdiActiveDocument;
            string banVe = dv == null || ai != 0 ? null : dv.Name;
            string bvMoi = banVe != null && (phien == null || !string.Equals(banVe, banVeTruoc, StringComparison.OrdinalIgnoreCase))
                ? "Bản vẽ của lời nhắn này: \"" + Path.GetFileName(banVe) + "\" (" + banVe + "). Công cụ cad tự chạy trên bản vẽ này kể cả khi tôi sang tab khác, không cần ghi ban_ve.\n\n" : "";
            string kem, gui1 = Soan(cau, bvMoi, out kem);

            bool quaCmd;
            string exe, doiSo;
            if (ai == 0)
            {
                exe = Tim(new[] { "claude.exe", "claude.cmd" }, new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin") }, out quaCmd);
                if (exe == null) { Ghi(null, CAI_CLAUDE, GiaoDien.Do); return false; }
                // Như runner/src/engines/claude.ts: không API key, không nạp cài đặt cá nhân (chỉ project, local của thư mục dự án); thêm MCP cad (đường dẫn
                // tuyệt đối: thư mục làm việc có thể khác 07_CAD\). Người dùng 19/11 ("như VS Code, bot phải ghi được file"): đọc + Edit/Write + Skill + 3 công cụ
                // cad; --permission-mode acceptEdits tự duyệt sửa file trong thư mục dự án + thư mục --add-dir (ngoài đó Claude Code vẫn từ chối).
                // Người dùng 23/11 ("muốn sửa được plugin trong đây, phải có PowerShell"): mở PowerShell (build, chạy test plugin), tự duyệt như Edit;
                // vẫn không mở Bash. Nối phiên nơi khác: --resume <id> --fork-session.
                // --add-dir / --plugin-dir trước --tools: các cờ này nhận nhiều giá trị, cờ kế tiếp mới cắt.
                // --input-format stream-json: stdin mở suốt lượt để Ctrl+X ghi thêm lời nhắn gấp (GuiGap), đóng khi có dòng result (DongVao).
                doiSo = "-p --input-format stream-json --output-format stream-json --verbose --model " + model + " --effort " + effort
                    + " --mcp-config " + Ngoac(KhoPhien.FileMcp(cad, banVe)) + " --strict-mcp-config --setting-sources project,local --permission-mode acceptEdits"
                    + (phien != null ? " --resume " + phien + (tach ? " --fork-session" : "") : "") + themDir + coSkill
                    + " --tools Read Grep Glob Skill Edit Write PowerShell" + (ultra ? " Agent" : "")
                    + " --allowedTools Read Grep Glob Skill Edit Write PowerShell" + (ultra ? " Agent" : "") + " mcp__cad__cad_tt mcp__cad__cad_chay mcp__cad__cad_anh";
                // Ultracode (người dùng 27/11): từ khoá đầu lời nhắn để Claude Code điều phối nhiều agent. CLI 2.1.322 không nhận công cụ Workflow (dò 27/11), chỉ Agent.
                if (ultra && !gui1.StartsWith("/")) gui1 = "ultracode\n\n" + gui1; // "/compact", "/skill ..." phải đứng đầu lời nhắn
            }
            else
            {
                exe = Tim(new[] { "agy.exe" }, new[] { Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "agy", "bin") }, out quaCmd);
                if (exe == null) { Ghi(null, CAI_AGY, GiaoDien.Do); return false; }
                // agy nhận lời nhắn qua -p (exe Go đọc dòng lệnh UTF-16 nên chữ Việt giữ nguyên); --mode plan = chỉ đọc.
                doiSo = "-p " + Ngoac(gui1) + " --output-format stream-json --mode plan --model " + model
                    + (phien != null ? " --conversation " + phien : "") + themDir + coSkill;
            }
            ProcessStartInfo si = quaCmd ? new ProcessStartInfo("cmd.exe", "/d /s /c \"\"" + exe + "\" " + doiSo + "\"") : new ProcessStartInfo(exe, doiSo);
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.WorkingDirectory = lam;
            si.RedirectStandardInput = si.RedirectStandardOutput = si.RedirectStandardError = true;
            si.StandardOutputEncoding = si.StandardErrorEncoding = new UTF8Encoding(false);
            si.EnvironmentVariables.Remove("ANTHROPIC_API_KEY");
            si.EnvironmentVariables.Remove("CLAUDECODE");
            si.EnvironmentVariables["NO_COLOR"] = "1";
            if (ai == 0) DatTk(si, TkChon());

            Ghi("Bạn", cau + kem, GiaoDien.Xanh, anhCo);
            BoDinhKem();
            aiPhien = ai;
            daChuyen = false;
            phienTruoc = phien;
            tachTruoc = tach;
            return Chay(si, gui1, banVe, lam);
        }

        /// Chạy một lượt CLI (Gui, hoặc chạy lại bằng cổng FP khi Pro lỗi đăng nhập / hết hạn mức).
        bool Chay(ProcessStartInfo si, string gui1, string banVe, string lam)
        {
            siLuot = si;
            guiLuot = gui1;
            loiTk = false;
            chiPhiQ = 0;
            loi.Length = 0;
            Process q = new Process();
            q.StartInfo = si;
            q.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                string d = e.Data;
                try { BeginInvoke((Action)delegate { if (d == null) Xong(q); else DocDong(d); }); } catch (System.Exception) { } // palette đã đóng
            };
            q.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) lock (loi) loi.AppendLine(e.Data); };
            try { q.Start(); }
            catch (System.Exception ex) { Ghi(null, (ai == 0 ? CAI_CLAUDE : CAI_AGY) + " (" + ex.Message + ")", GiaoDien.Do); return true; } // lời nhắn đã hiện trong khung
            p = q;
            banVeTruoc = banVe;
            phienCwd = lam;
            nutGui.Glyph = Ve.DUNG;
            nutGui.Nen = GiaoDien.Do;
            goiY.SetToolTip(nutGui, "Dừng (Enter lúc này = xếp hàng, Ctrl+X = gửi gấp vào lượt đang chạy)");
            nutGui.Invalidate();
            goiYNhap.Text = ai == 0 ? "Enter xếp hàng · Ctrl+X gửi gấp…" : "Enter xếp hàng…";
            batDau = DateTime.Now;
            viec = ModelDang().Ten + " đang nghĩ";
            CapNhat();
            dongHo.Start();
            q.BeginOutputReadLine();
            q.BeginErrorReadLine();
            vaoMo = ai == 0 && GhiVao(q, gui1);
            if (!vaoMo) try { q.StandardInput.Close(); } catch (System.Exception) { } // agy nhận lời nhắn qua -p
            return true;
        }

        /// Một dòng stream-json.
        /// claude: system/init (session_id), assistant (chữ, công cụ), result (lỗi). agy: init (conversation_id), step_update (bước tool), result (response, status).
        void DocDong(string dong)
        {
            Dictionary<string, object> j;
            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = int.MaxValue; // kết quả công cụ có thể dài
                j = js.DeserializeObject(dong) as Dictionary<string, object>;
            }
            catch (System.Exception) { j = null; }
            if (j == null) { if (!dong.TrimStart().StartsWith("{")) Ghi(null, dong, GiaoDien.ChuMo); return; }
            object v;
            if (j.TryGetValue("session_id", out v) && v is string)
            {
                if ((string)v != phien) tach = false; // đã tách nhánh: từ giờ nói tiếp nhánh mới
                phien = (string)v;
            }
            if (j.TryGetValue("conversation_id", out v) && v is string) phien = (string)v;
            string loai = j.TryGetValue("type", out v) ? v as string : null;
            string suKien = j.TryGetValue("event", out v) ? v as string : null;
            if (loai == "result") { DongVao(); CongUsage(j); }
            if (loai == "rate_limit_event" && j.TryGetValue("rate_limit_info", out v) && v is Dictionary<string, object>)
            {
                gioiHan = (Dictionary<string, object>)v;
                object t;
                if (gioiHan.TryGetValue("status", out t) && Convert.ToString(t) == "rejected") loiTk = true; // hết hạn mức: Xong chuyển FP nếu được
            }
            if (loai == "assistant" && j.TryGetValue("message", out v))
            {
                Dictionary<string, object> m = v as Dictionary<string, object>;
                object[] nd = m != null && m.TryGetValue("content", out v) ? v as object[] : null;
                foreach (Dictionary<string, object> k in (nd ?? new object[0]).OfType<Dictionary<string, object>>())
                {
                    object t;
                    string kieu = k.TryGetValue("type", out t) ? t as string : null;
                    if (kieu == "text" && k.TryGetValue("text", out t)) { Ghi("Claude", Convert.ToString(t).Trim(), GiaoDien.ChuDam); viec = "Đang viết kết quả"; }
                    else if (kieu == "tool_use" && k.TryGetValue("name", out t)) CongCu(Convert.ToString(t).Replace("mcp__cad__", ""));
                }
            }
            else if (loai == "result" && j.TryGetValue("is_error", out v) && v is bool && (bool)v)
            {
                string r = j.TryGetValue("result", out v) ? Convert.ToString(v) : "";
                if (LoiTk(r)) loiTk = true;
                Ghi(null, "Lỗi: " + r + (CoChuyenFp() ? "" : LoiDangNhap(r)), GiaoDien.Do);
            }
            else if (suKien == "step_update" && j.TryGetValue("step_update", out v))
            {
                Dictionary<string, object> b = v as Dictionary<string, object>;
                object t;
                if (b != null && b.TryGetValue("state", out t) && Convert.ToString(t) == "ACTIVE" && b.TryGetValue("tool_name", out t)) CongCu(Convert.ToString(t));
            }
            else if (suKien == "result" && j.TryGetValue("result", out v))
            {
                Dictionary<string, object> r = v as Dictionary<string, object>;
                object t;
                if (r == null) return;
                if (r.TryGetValue("response", out t) && Convert.ToString(t).Trim() != "") Ghi("Antigravity", Convert.ToString(t).Trim(), GiaoDien.ChuDam);
                if (r.TryGetValue("status", out t) && Convert.ToString(t) != "SUCCESS")
                    Ghi(null, "Lỗi: " + Convert.ToString(t) + (r.TryGetValue("error", out t) ? " " + Convert.ToString(t) : ""), GiaoDien.Do);
            }
        }

        void CongCu(string ten)
        {
            Ghi(null, "· " + ten, GiaoDien.ChuMo);
            viec = "Đang chạy " + ten;
            CapNhat();
        }

        static string LoiDangNhap(string m)
        {
            string t = (m ?? "").ToLowerInvariant();
            return t.Contains("login") || t.Contains("auth") || t.Contains("401") || t.Contains("credential")
                ? "\nChưa đăng nhập / hết hạn: bấm nút model → Tài khoản → Đăng nhập Claude Pro, hoặc chọn Cổng FP (Antigravity: gõ agy trong terminal)." : "";
        }

        void Xong(Process q)
        {
            int ma = -1;
            try { q.WaitForExit(3000); if (q.HasExited) ma = q.ExitCode; } catch (System.Exception) { }
            string e;
            lock (loi) e = loi.ToString().Trim();
            if (ma != 0 && LoiTk(e)) loiTk = true;
            if (loiTk && CoChuyenFp())
            {
                // Người dùng 27/11: Pro lỗi đăng nhập / hết hạn mức thì chạy lại đúng lời nhắn bằng cổng FP, một lần mỗi lượt (lịch sử chung nhờ symlink projects).
                q.Dispose();
                p = null;
                vaoMo = false;
                daChuyen = true;
                phien = phienTruoc;
                tach = tachTruoc;
                DatTk(siLuot, 1);
                Ghi(null, "Đã chuyển sang cổng FP (tính tiền API)", GiaoDien.ChuMo);
                Chay(siLuot, guiLuot, banVeTruoc, phienCwd);
                if (p != null) return; // không chạy được thì dọn giao diện như lượt xong
            }
            if (ma != 0 && e != "") Ghi(null, TEN_AI[Math.Max(0, aiPhien)] + " thoát mã " + ma + ": " + (e.Length > 600 ? e.Substring(0, 600) : e) + LoiDangNhap(e), GiaoDien.Do);
            dongHo.Stop();
            int s = (int)(DateTime.Now - batDau).TotalSeconds;
            trangThai.Text = (ma == 0 ? "Xong" : "Dừng (mã " + ma + ")") + " · " + s / 60 + ":" + (s % 60).ToString("00");
            q.Dispose();
            p = null;
            vaoMo = false;
            goiYNhap.Text = "Thêm yêu cầu…";
            nutGui.Glyph = Ve.GUI;
            nutGui.Nen = GiaoDien.XanhDam;
            goiY.SetToolTip(nutGui, "Gửi (Enter)");
            nutGui.Invalidate();
            if (hangCho.Count > 0) { string cau = hangCho.Dequeue(); BeginInvoke((Action)delegate { GiaoDien.An(delegate { Gui(cau); }); }); }
        }

        // ---------- Tài khoản Claude: Pro (claude.ai, %USERPROFILE%\.claude) hay cổng FP (CLAUDE_CONFIG_DIR + token trong settings.json) ----------
        // Người dùng 27/11: không cần terminal. Bot chạy --setting-sources project,local nên không nạp settings user: env FP phải tự đặt vào tiến trình con.

        string nguonTk = GiaoDien.DocCaiDat("chat_tk", "tu_dong"); // tu_dong | pro | fp
        bool ultra = GiaoDien.DocCaiDat("chat_ultracode", "0") == "1";
        int tkLuot;                 // tài khoản của lượt đang / vừa chạy: 0 Pro, 1 FP
        bool daChuyen, loiTk;       // lượt này đã chuyển FP; lượt này lỗi đăng nhập / hết hạn mức
        string phienTruoc, guiLuot; // để chạy lại đúng lời nhắn khi chuyển FP
        bool tachTruoc;
        ProcessStartInfo siLuot;
        bool dangNhap;              // đang chạy claude auth login

        static readonly string[] MA_TK = { "tu_dong", "pro", "fp" };
        static readonly string[] TEN_TK = { "Tự động (Pro, lỗi / hết hạn mức → FP)", "Claude Pro (đăng nhập claude.ai)", "Cổng FP (tính tiền API)" };

        static string NhaClaude() { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude"); }

        /// Thư mục cấu hình FP: CLAUDE_CONFIG_DIR (tiến trình hoặc User) nếu khác .claude, không thì %USERPROFILE%\.claude-fp.
        static string ThuMucFp()
        {
            string nha = NhaClaude();
            foreach (string d in new[] { Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR"), Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR", EnvironmentVariableTarget.User) })
                if (!string.IsNullOrEmpty(d) && !string.Equals(d.Replace('/', '\\').TrimEnd('\\'), nha, StringComparison.OrdinalIgnoreCase)) return d;
            return nha + "-fp";
        }

        /// {ANTHROPIC_BASE_URL, ANTHROPIC_AUTH_TOKEN} trong settings.json thư mục FP; null = không có token (FP không dùng được). Không ghi token ra đâu cả.
        static string[] FpToken()
        {
            try
            {
                Dictionary<string, object> d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(Path.Combine(ThuMucFp(), "settings.json"))) as Dictionary<string, object>;
                object v;
                Dictionary<string, object> env = d != null && d.TryGetValue("env", out v) ? v as Dictionary<string, object> : null;
                if (env == null) return null;
                string url = env.TryGetValue("ANTHROPIC_BASE_URL", out v) ? v as string : null, tok = env.TryGetValue("ANTHROPIC_AUTH_TOKEN", out v) ? v as string : null;
                return string.IsNullOrEmpty(tok) ? null : new[] { url ?? "", tok };
            }
            catch (System.Exception) { return null; }
        }

        /// claudeAiOauth trong %USERPROFILE%\.claude\.credentials.json; null = chưa đăng nhập Pro.
        static Dictionary<string, object> OauthPro()
        {
            try
            {
                Dictionary<string, object> d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(Path.Combine(NhaClaude(), ".credentials.json"))) as Dictionary<string, object>;
                object v;
                return d != null && d.TryGetValue("claudeAiOauth", out v) ? v as Dictionary<string, object> : null;
            }
            catch (System.Exception) { return null; }
        }

        /// Tài khoản cho lượt kế: 0 Pro, 1 FP.
        int TkChon()
        {
            if (nguonTk == "pro") return 0;
            if (nguonTk == "fp") return 1;
            return OauthPro() != null || FpToken() == null ? 0 : 1;
        }

        /// Đặt env tiến trình con theo tài khoản: Pro bỏ CLAUDE_CONFIG_DIR (dùng .claude); FP đặt thư mục FP + 2 biến cổng (không nạp hook, statusline).
        void DatTk(ProcessStartInfo si, int tk)
        {
            tkLuot = tk;
            si.EnvironmentVariables.Remove("ANTHROPIC_BASE_URL");
            si.EnvironmentVariables.Remove("ANTHROPIC_AUTH_TOKEN");
            if (tk == 0) { si.EnvironmentVariables.Remove("CLAUDE_CONFIG_DIR"); return; }
            si.EnvironmentVariables["CLAUDE_CONFIG_DIR"] = ThuMucFp();
            string[] fp = FpToken();
            if (fp == null) return; // CLI tự báo chưa đăng nhập
            if (fp[0] != "") si.EnvironmentVariables["ANTHROPIC_BASE_URL"] = fp[0];
            si.EnvironmentVariables["ANTHROPIC_AUTH_TOKEN"] = fp[1];
        }

        static bool LoiTk(string m)
        {
            string t = (m ?? "").ToLowerInvariant();
            return LoiDangNhap(m) != "" || t.Contains("usage limit") || t.Contains("limit reached") || t.Contains("rate limit") || t.Contains("rate_limit");
        }

        /// Lượt Claude chạy bằng Pro ở chế độ tự động, chưa chuyển lần nào, FP có token.
        bool CoChuyenFp() { return aiPhien == 0 && tkLuot == 0 && !daChuyen && nguonTk == "tu_dong" && FpToken() != null; }

        static string TenChon(string ma) { int i = Array.IndexOf(MA_TK, ma); return TEN_TK[Math.Max(0, i)]; }

        /// Tên nguồn đang dùng thật ("Claude Pro" / "Cổng FP"): lượt đang chạy, không thì lượt kế.
        public string NguonTkDang() { return (p != null && aiPhien == 0 ? tkLuot : TkChon()) == 0 ? "Claude Pro" : "Cổng FP"; }

        /// ma: tu_dong | pro | fp. Áp từ lượt kế.
        public void DatNguonTk(string ma)
        {
            if (Array.IndexOf(MA_TK, ma) < 0) { trangThai.Text = "Nguồn tài khoản: tu_dong | pro | fp."; return; }
            nguonTk = ma;
            GiaoDien.LuuCaiDat("chat_tk", ma);
            trangThai.Text = "Tài khoản: " + TenChon(ma) + " · lượt kế dùng " + NguonTkDang() + ".";
        }

        /// Nhiều dòng: nguồn chọn, Pro có đăng nhập không + hạn, FP có token không (KHÔNG in token).
        public string TrangThaiTk()
        {
            Dictionary<string, object> o = OauthPro();
            string pro = "chưa đăng nhập (nút model → Tài khoản → Đăng nhập Claude Pro)";
            if (o != null)
            {
                object v;
                pro = "đã đăng nhập" + (o.TryGetValue("subscriptionType", out v) && v != null ? ", gói " + v : "");
                if (o.TryGetValue("refreshTokenExpiresAt", out v) && v != null)
                    try { pro += ", hạn đăng nhập " + GioUnix(Convert.ToDouble(v, CultureInfo.InvariantCulture) / 1000).ToString("dd/MM/yyyy"); } catch (System.Exception) { }
            }
            return "Tài khoản chọn: " + TenChon(nguonTk) + " · đang dùng: " + NguonTkDang()
                + "\nClaude Pro (" + NhaClaude() + "): " + pro
                + "\nCổng FP (" + ThuMucFp() + "): " + (FpToken() != null ? "có token" : "chưa có token trong settings.json (env ANTHROPIC_AUTH_TOKEN)");
        }

        static DateTime GioUnix(double giay) { return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(giay).ToLocalTime(); }

        /// "claude auth login --claudeai" chạy ngầm với thư mục mặc định .claude: CLI tự mở trình duyệt, đăng nhập xong thì thoát. Không chặn giao diện.
        public void DangNhapPro()
        {
            if (dangNhap) { trangThai.Text = "Đang chờ đăng nhập trên trình duyệt…"; return; }
            bool quaCmd;
            string exe = Tim(new[] { "claude.exe", "claude.cmd" }, new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin") }, out quaCmd);
            if (exe == null) { Ghi(null, CAI_CLAUDE, GiaoDien.Do); return; }
            const string DS = "auth login --claudeai";
            ProcessStartInfo si = quaCmd ? new ProcessStartInfo("cmd.exe", "/d /s /c \"\"" + exe + "\" " + DS + "\"") : new ProcessStartInfo(exe, DS);
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.RedirectStandardOutput = si.RedirectStandardError = true;
            si.StandardOutputEncoding = si.StandardErrorEncoding = new UTF8Encoding(false);
            foreach (string k in new[] { "CLAUDE_CONFIG_DIR", "ANTHROPIC_API_KEY", "ANTHROPIC_AUTH_TOKEN", "ANTHROPIC_BASE_URL", "CLAUDECODE" }) si.EnvironmentVariables.Remove(k);
            si.EnvironmentVariables["NO_COLOR"] = "1";
            Process q = new Process();
            q.StartInfo = si;
            StringBuilder ra = new StringBuilder();
            DataReceivedEventHandler doc = delegate(object s, DataReceivedEventArgs e)
            {
                string d = e.Data;
                if (d == null) return;
                lock (ra) ra.AppendLine(d);
                // Trình duyệt không tự mở thì người dùng bấm / chép link này.
                if (d.Contains("http")) try { BeginInvoke((Action)delegate { Ghi(null, d.Trim(), GiaoDien.ChuMo); }); } catch (System.Exception) { }
            };
            q.ErrorDataReceived += doc;
            q.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                if (e.Data != null) { doc(s, e); return; }
                int ma = -1;
                try { if (!q.WaitForExit(5000)) q.Kill(); else ma = q.ExitCode; } catch (System.Exception) { }
                string kq;
                lock (ra) kq = ra.ToString().Trim();
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        dangNhap = false;
                        bool ok = ma == 0 && OauthPro() != null;
                        Ghi(null, ok ? "Đã đăng nhập Claude Pro." : "Đăng nhập Claude Pro chưa xong (mã " + ma + ")" + (kq != "" ? ": " + (kq.Length > 400 ? kq.Substring(kq.Length - 400) : kq) : "."),
                            ok ? GiaoDien.ChuMo : GiaoDien.Do);
                        q.Dispose();
                    });
                }
                catch (System.Exception) { } // palette đã đóng
            };
            try { q.Start(); }
            catch (System.Exception ex) { Ghi(null, "Không chạy được claude auth login: " + ex.Message, GiaoDien.Do); return; }
            dangNhap = true;
            q.BeginOutputReadLine();
            q.BeginErrorReadLine();
            // shortcut: bỏ người dùng không đăng nhập thì 10 phút sau tự dừng; CLI hỏi dán mã (không mở được trình duyệt) thì chưa hỗ trợ.
            System.Threading.ThreadPool.QueueUserWorkItem(delegate { try { if (!q.WaitForExit(600000)) q.Kill(); } catch (System.Exception) { } });
            Ghi(null, "Đang mở trình duyệt để đăng nhập Claude Pro…", GiaoDien.ChuMo);
        }

        /// Nhóm "Tài khoản" trong menu model.
        void MenuTk(ContextMenuStrip m)
        {
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(Nhom("Tài khoản Claude  (đang dùng: " + NguonTkDang() + ")"));
            for (int i = 0; i < MA_TK.Length; i++)
            {
                string ma = MA_TK[i];
                ToolStripMenuItem it = new ToolStripMenuItem(TEN_TK[i], null, delegate { DatNguonTk(ma); });
                it.Checked = nguonTk == ma;
                m.Items.Add(it);
            }
            m.Items.Add("Đăng nhập Claude Pro…", null, delegate { GiaoDien.An(DangNhapPro); });
            m.Items.Add("Xem trạng thái tài khoản", null, delegate { Ghi(null, TrangThaiTk(), GiaoDien.ChuMo); });
        }

        // ---------- Effort, Ultracode (cho lệnh "/") ----------

        /// e: low | medium | high | xhigh (hoặc "extra") | max, theo model đang chọn.
        public void DatEffort(string e)
        {
            e = (e ?? "").Trim().ToLowerInvariant();
            if (e == "extra") e = "xhigh";
            ModelAi m = ModelDang();
            if (m.Effort == null || !m.Effort.Contains(e))
            {
                trangThai.Text = m.Ten + " không có mức " + e + (m.Effort != null ? " (có: " + string.Join(", ", m.Effort.Select(x => Hoa(x))) + ")" : "");
                return;
            }
            ChonModel(m, e);
        }

        public void DatUltracode(bool bat)
        {
            ultra = bat;
            GiaoDien.LuuCaiDat("chat_ultracode", bat ? "1" : "0");
            VeModel();
            trangThai.Text = bat ? "Ultracode bật: Claude điều phối nhiều agent (tốn hạn mức hơn nhiều)." : "Ultracode tắt.";
        }

        public bool Ultracode { get { return ultra; } }

        // ---------- Thống kê cho /usage ----------

        readonly long[] tokDem = new long[4]; // vào, ra, cache đọc, cache ghi (cộng dồn từ hội thoại mới)
        double chiPhi, chiPhiQ;               // chiPhiQ: total_cost_usd lần trước của tiến trình đang chạy (CLI cộng dồn theo tiến trình)
        int soLuot;
        Dictionary<string, object> gioiHan;   // rate_limit_info cuối (claude stream-json, chỉ khi chạy bằng Pro)

        void CongUsage(Dictionary<string, object> j)
        {
            object v, t;
            soLuot++;
            Dictionary<string, object> u = j.TryGetValue("usage", out v) ? v as Dictionary<string, object> : null;
            string[] k = { "input_tokens", "output_tokens", "cache_read_input_tokens", "cache_creation_input_tokens" };
            if (u != null)
                for (int i = 0; i < k.Length; i++)
                    if (u.TryGetValue(k[i], out t) && t != null) try { tokDem[i] += Convert.ToInt64(t); } catch (System.Exception) { }
            if (!j.TryGetValue("total_cost_usd", out v) || v == null) return;
            double c;
            try { c = Convert.ToDouble(v, CultureInfo.InvariantCulture); } catch (System.Exception) { return; }
            chiPhi += c >= chiPhiQ ? c - chiPhiQ : c;
            chiPhiQ = c;
        }

        /// Hội thoại mới: đếm lại token, chi phí (giữ giới hạn: của tài khoản, không của phiên).
        void DatLaiUsage()
        {
            Array.Clear(tokDem, 0, tokDem.Length);
            chiPhi = chiPhiQ = 0;
            soLuot = 0;
        }

        static string So(long n) { return n.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.'); }

        /// Nhiều dòng tiếng Việt: token, chi phí, giới hạn 5 giờ / tuần (nếu CLI gửi), tài khoản.
        public string MoTaUsage()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("Từ hội thoại mới: " + soLuot + " lượt");
            b.AppendLine("Token vào: " + So(tokDem[0] + tokDem[2] + tokDem[3]) + " (mới " + So(tokDem[0]) + ", cache đọc " + So(tokDem[2]) + ", cache ghi " + So(tokDem[3]) + ") · ra: " + So(tokDem[1]));
            if (chiPhi > 0)
                b.AppendLine("Chi phí: $" + chiPhi.ToString("0.0000", CultureInfo.InvariantCulture) + (tkLuot == 0 ? " (Pro: quy đổi theo giá API, không trừ tiền)" : " (cổng FP: tính tiền API)"));
            object v;
            Dictionary<string, object> cua = gioiHan != null && gioiHan.TryGetValue("unifiedWindows", out v) ? v as Dictionary<string, object> : null;
            int co = 0;
            string[] ma = { "five_hour", "seven_day" }, ten = { "Giới hạn 5 giờ", "Giới hạn tuần" };
            for (int i = 0; i < ma.Length; i++)
            {
                Dictionary<string, object> w = cua != null && cua.TryGetValue(ma[i], out v) ? v as Dictionary<string, object> : null;
                if (w == null) continue;
                try
                {
                    object u, r, s;
                    string tt = w.TryGetValue("status", out s) ? Convert.ToString(s) : "";
                    b.AppendLine(ten[i] + ": " + (w.TryGetValue("utilization", out u) ? Math.Round(Convert.ToDouble(u, CultureInfo.InvariantCulture) * 100) + " %" : "?")
                        + (w.TryGetValue("resetsAt", out r) ? " · đặt lại " + GioUnix(Convert.ToDouble(r, CultureInfo.InvariantCulture)).ToString("HH:mm dd/MM") : "")
                        + (tt == "allowed_warning" ? " (sắp chạm)" : tt == "rejected" ? " (HẾT)" : ""));
                    co++;
                }
                catch (System.Exception) { }
            }
            if (co == 0) b.AppendLine("Giới hạn 5 giờ / tuần: chưa có (CLI chỉ gửi khi chạy bằng Claude Pro: gửi một lời nhắn trước).");
            b.Append("Tài khoản: " + NguonTkDang() + " (chọn: " + TenChon(nguonTk) + ")");
            return b.ToString();
        }
    }
}
