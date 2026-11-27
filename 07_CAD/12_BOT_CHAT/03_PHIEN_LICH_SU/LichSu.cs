// Hội thoại mới, lịch sử, link phiên Claude Code / Antigravity. Tách từ BotChat.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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
        string TenNoi() { return noi == "vscode" ? "VS Code" : "Claude Code"; }

        /// Phiên thuộc bên đang nối chung (VS Code: entrypoint claude-vscode; Claude Code: cli, claude-desktop).
        bool KhopNoi(PhienAi x) { return x.Ai == 0 && (noi == "vscode" ? x.Nguon == "VS Code" : noi == "claude" && (x.Nguon == "Claude Code" || x.Nguon == "Desktop")); }

        /// Mở hội thoại Claude đang có ở bên nối chung. VS Code: vscode://anthropic.claude-code/open?session= (extension 2.1.327 tìm phiên theo thư mục
        /// đang mở, nên mở thư mục của phiên trước, đợi cửa sổ lên rồi mới gửi link). Claude Code: claude://resume?session= (app nhập phiên CLI rồi mở).
        void MoBenKia()
        {
            if (phien == null || aiPhien != 0) { trangThai.Text = "Chưa có hội thoại Claude: gửi một lời nhắn hoặc nối một phiên trước."; return; }
            if (p != null) { trangThai.Text = "Đang chạy: đợi xong rồi mở bên kia (hai nơi cùng ghi một phiên sẽ rẽ nhánh)."; return; }
            try
            {
                if (noi == "claude") { Process.Start("claude://resume?session=" + phien); trangThai.Text = "Đã gửi phiên sang app Claude Code."; return; }
                string link = "vscode://anthropic.claude-code/open?session=" + phien;
                bool quaCmd;
                string code = Tim(new[] { "code.cmd", "code.exe" }, new string[0], out quaCmd);
                if (code == null || phienCwd == null || !Directory.Exists(phienCwd)) { Process.Start(link); trangThai.Text = "Đã gửi phiên sang VS Code."; return; }
                ProcessStartInfo si = quaCmd ? new ProcessStartInfo("cmd.exe", "/d /s /c \"\"" + code + "\" " + Ngoac(phienCwd) + "\"") : new ProcessStartInfo(code, Ngoac(phienCwd));
                si.UseShellExecute = false;
                si.CreateNoWindow = true;
                Process.Start(si).Dispose();
                Timer t = new Timer();
                t.Interval = 3000;
                t.Tick += delegate { t.Stop(); t.Dispose(); try { Process.Start(link); } catch (System.Exception) { } };
                t.Start();
                trangThai.Text = "Đang mở trong VS Code: " + phienCwd;
            }
            catch (System.Exception ex) { trangThai.Text = "Không mở được: " + ex.Message; }
        }

        void HoiThoaiMoi()
        {
            if (p != null) return;
            phien = null;
            phienCwd = null;
            aiPhien = -1;
            linkTen = null;
            tach = false;
            noiChung = false;
            DatLaiUsage();
            XoaTin();
            VeChip();
            trangThai.Text = "Hội thoại mới.";
        }

        /// Menu phiên gần đây: Claude Code (VS Code, CLI, Desktop, Bot chat) và Antigravity. Chọn = nối tiếp phiên đó.
        void MoLichSu()
        {
            if (p != null) { trangThai.Text = "Đang chạy: đợi xong (hoặc Dừng) rồi mới đổi phiên."; return; }
            ContextMenuStrip m = new ContextMenuStrip();
            m.Font = GiaoDien.Chu;
            Action<List<PhienAi>> them = delegate(List<PhienAi> ds)
            {
                if (ds.Count == 0) { m.Items.Add("(không có)").Enabled = false; return; }
                foreach (PhienAi x in ds)
                {
                    PhienAi xx = x;
                    ToolStripItem i = m.Items.Add(x.ToString(), null, delegate { GiaoDien.An(delegate { Link(xx); }); });
                    i.ToolTipText = (x.Cwd ?? "") + "\n" + x.Id;
                }
            };
            m.Items.Add(Nhom("Claude Code: VS Code · CLI · Desktop · Bot chat" + (noi != "" ? "  (nối chung: " + TenNoi() + " trước)" : "")));
            them(KhoPhien.Claude(15).OrderBy(x => KhopNoi(x) ? 0 : 1).ToList()); // OrderBy giữ thứ tự mới nhất trong mỗi nhóm
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(Nhom("Antigravity: IDE · agy"));
            them(KhoPhien.Antigravity(10));
            m.Show(Cursor.Position);
        }

        /// Nối phiên x: chọn đúng máy AI, đổi thư mục dự án theo phiên (Claude tìm phiên theo thư mục làm việc), hiện 6 tin cuối.
        void Link(PhienAi x)
        {
            if (p != null) return;
            if (x.Ai != ai)
            {
                ai = x.Ai;
                GiaoDien.LuuCaiDat("chat_ai", ai == 0 ? "claude" : "gemini");
                VeModel();
            }
            if (x.Cwd != null && Directory.Exists(x.Cwd) && !string.Equals(x.Cwd.TrimEnd('\\'), (ThuMucLam() ?? "").TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                DatDuAn(x.Cwd);
            XoaTin();
            phien = x.Id;
            phienCwd = x.Cwd;
            aiPhien = x.Ai;
            // Nối chung = ghi thẳng vào phiên gốc. Phiên vừa ghi trong 2 phút qua có thể đang chạy ở nơi khác: hai nơi cùng ghi sẽ rẽ nhánh, nên lần này vẫn tách.
            bool dangGhi = x.Ai == 0 && File.Exists(x.File) && (DateTime.Now - File.GetLastWriteTime(x.File)).TotalMinutes < 2;
            noiChung = x.Ai == 0 && noi != "" && !dangGhi;
            tach = x.Ai == 0 && !noiChung;
            string ten = x.Ten.Replace("\n", " ");
            linkTen = x.Nguon + ": " + (ten.Length > 28 ? ten.Substring(0, 27) + "…" : ten);
            List<KeyValuePair<bool, string>> ds = KhoPhien.TinCuoi(x, 40);
            tinCu = ds.Take(Math.Max(0, ds.Count - 6)).ToList();
            if (tinCu.Count > 0)
            {
                Label cu = new Label();
                cu.AutoSize = true;
                cu.Text = "Hiện tin cũ hơn (" + tinCu.Count + ")";
                cu.ForeColor = GiaoDien.Xanh;
                cu.Font = GiaoDien.Nho;
                cu.Cursor = Cursors.Hand;
                cu.Click += delegate
                {
                    List<KeyValuePair<bool, string>> them = tinCu;
                    cu.Dispose();
                    List<Control> sau = dsTin.Controls.Cast<Control>().ToList();
                    dsTin.Controls.Clear();
                    foreach (KeyValuePair<bool, string> t in them) dsTin.Controls.Add(new BongBong(t.Key ? 0 : 1, Cat(t.Value), GiaoDien.ChuDam));
                    dsTin.Controls.AddRange(sau.ToArray());
                    tinCu = null;
                    XepTin(false);
                };
                dsTin.Controls.Add(cu);
            }
            foreach (KeyValuePair<bool, string> t in ds.Skip(tinCu.Count)) Ghi(t.Key ? "Bạn" : TEN_AI[x.Ai], Cat(t.Value), GiaoDien.ChuDam);
            Ghi(null, "— Đã nối " + x + (x.Ai != 0 ? ". Lời nhắn kế ghi tiếp vào hội thoại Antigravity này."
                : noiChung ? ". Nối chung: lời nhắn kế ghi thẳng vào phiên này, mở lại ở " + x.Nguon + " (hoặc bấm chip link) là thấy. Đừng gõ cùng lúc ở hai nơi."
                : (dangGhi && noi != "" ? ". Phiên này vừa ghi trong 2 phút qua (có thể đang chạy ở nơi khác) nên lần này nối nhánh riêng." : ".")
                    + " Lời nhắn kế chạy tiếp ngữ cảnh phiên này ở một nhánh mới (phiên gốc giữ nguyên)."), GiaoDien.ChuMo);
            VeChip();
        }

        static string Cat(string s) { return s.Length > 900 ? s.Substring(0, 900) + "…" : s; }

    }
}
