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

        static string Hoa(string e) { return e == null ? "" : e == "xhigh" ? "XHigh" : char.ToUpper(e[0]) + e.Substring(1); }

        void VeModel()
        {
            ModelAi m = ModelDang();
            string e = EffortDang();
            nutModel.Text = m.Ten + (e != null ? "  " + Hoa(e) : "") + "  ▾";
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
                m.Items.Add(new ToolStripSeparator());
                m.Items.Add(Nhom("Effort  (" + dang.Ten + ")"));
                foreach (string e in dang.Effort)
                {
                    string ee = e;
                    ToolStripMenuItem i = new ToolStripMenuItem(Hoa(e), null, delegate { ChonModel(dang, ee); });
                    i.Checked = e == eDang;
                    m.Items.Add(i);
                }
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
                    + " --tools Read Grep Glob Skill Edit Write PowerShell --allowedTools Read Grep Glob Skill Edit Write PowerShell mcp__cad__cad_tt mcp__cad__cad_chay mcp__cad__cad_anh";
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

            Ghi("Bạn", cau + kem, GiaoDien.Xanh, anhCo);
            BoDinhKem();
            loi.Length = 0;
            aiPhien = ai;
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
            if (loai == "result") DongVao();
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
                Ghi(null, "Lỗi: " + (j.TryGetValue("result", out v) ? Convert.ToString(v) : "") + LoiDangNhap(Convert.ToString(v)), GiaoDien.Do);
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
                ? "\nPhiên đăng nhập hết hạn: mở terminal, gõ claude rồi /login (Antigravity: gõ agy)." : "";
        }

        void Xong(Process q)
        {
            int ma = -1;
            try { q.WaitForExit(3000); if (q.HasExited) ma = q.ExitCode; } catch (System.Exception) { }
            string e;
            lock (loi) e = loi.ToString().Trim();
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
    }
}
