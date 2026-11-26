// Danh mục model Claude / Antigravity. Tách từ GiaoDienChat.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace TKPCCC
{
    class ModelAi
    {
        public int Ai;          // 0 Claude (claude -p), 1 Antigravity (agy -p)
        public string Id, Ten;  // Antigravity có mức effort: Id = gốc, tên agy thật = Id + "-" + mức
        public string[] Effort; // null = model không chọn mức (Antigravity: Orchestrator, Claude qua agy, GPT-OSS)
    }

    static class DanhMucModel
    {
        static readonly string[] E5 = { "low", "medium", "high", "xhigh", "max" }, E4 = { "low", "medium", "high", "max" };

        // Bảng model của Anthropic (skill claude-api, 03/11/2026): 4.6 chưa có xhigh. Phần tử đầu là mặc định.
        public static readonly List<ModelAi> Claude = new List<ModelAi>
        {
            new ModelAi { Ai = 0, Id = "claude-opus-5-5", Ten = "Opus 5.5", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-opus-5", Ten = "Opus 5", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-opus-4-8", Ten = "Opus 4.8", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-opus-4-7", Ten = "Opus 4.7", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-opus-4-6", Ten = "Opus 4.6", Effort = E4 },
            new ModelAi { Ai = 0, Id = "claude-sonnet-5-5", Ten = "Sonnet 5.5", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-sonnet-5", Ten = "Sonnet 5", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-sonnet-4-6", Ten = "Sonnet 4.6", Effort = E4 },
        };

        // `agy models` ngày 18/11/2026 (dùng khi chưa đọc được từ máy).
        const string AGY_MAC_DINH = "gemini-4-hydrogen-high=Gemini 4 Hydrogen (High)|gemini-4-hydrogen-xhigh=Gemini 4 Hydrogen (XHigh)|gemini-4-hydrogen-medium=Gemini 4 Hydrogen (Medium)|"
            + "gemini-4-hydrogen-low=Gemini 4 Hydrogen (Low)|gemini-4-argon-medium=Gemini 4 Argon (Medium)|gemini-4-argon-high=Gemini 4 Argon (High)|gemini-4-argon-low=Gemini 4 Argon (Low)|"
            + "gemini-4-carbon-medium=Gemini 4 Carbon (Medium)|gemini-4-carbon-high=Gemini 4 Carbon (High)|gemini-4-carbon-low=Gemini 4 Carbon (Low)|"
            + "gemini-3.8-flash-high=Gemini 3.8 Flash (High)|gemini-3.8-flash-medium=Gemini 3.8 Flash (Medium)|gemini-3.8-flash-low=Gemini 3.8 Flash (Low)|"
            + "gemini-pro-orchestrator=Orchestrator (Carbon)|gemini-pro-orchestrator-argon=Orchestrator (Argon)|claude-sonnet-4-6=Claude Sonnet 4.6 (Thinking)|"
            + "claude-opus-4-6-thinking=Claude Opus 4.6 (Thinking)|gpt-oss-120b-medium=GPT-OSS 120B (Medium)";

        static readonly string[] MUC = { "low", "medium", "high", "xhigh", "max" };

        /// Model Antigravity: gom "gốc-mức" thành một model nhiều mức (theo thứ tự low → max).
        public static List<ModelAi> Antigravity()
        {
            List<ModelAi> kq = new List<ModelAi>();
            foreach (string cap in GiaoDien.DocCaiDat("agy_models", AGY_MAC_DINH).Split('|'))
            {
                int i = cap.IndexOf('=');
                if (i <= 0) continue;
                string id = cap.Substring(0, i), ten = cap.Substring(i + 1);
                Match m = Regex.Match(id, "^(.+)-(low|medium|high|xhigh|max)$");
                Match mt = Regex.Match(ten, "^(.+?) \\((Low|Medium|High|XHigh|Max)\\)$");
                if (m.Success && mt.Success && !id.StartsWith("gpt-"))
                {
                    ModelAi g = kq.FirstOrDefault(x => x.Id == m.Groups[1].Value);
                    if (g == null) kq.Add(g = new ModelAi { Ai = 1, Id = m.Groups[1].Value, Ten = mt.Groups[1].Value, Effort = new string[0] });
                    g.Effort = g.Effort.Concat(new[] { m.Groups[2].Value }).OrderBy(x => Array.IndexOf(MUC, x)).ToArray();
                }
                else kq.Add(new ModelAi { Ai = 1, Id = id, Ten = ten });
            }
            return kq;
        }

        /// Chạy `agy models` nền, lưu danh sách mới vào "agy_models" (lần mở menu sau dùng). Lỗi / không có agy: giữ danh sách cũ.
        public static void LamMoiAgy(string agy)
        {
            if (agy == null) return;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    ProcessStartInfo si = new ProcessStartInfo(agy, "models");
                    si.UseShellExecute = false;
                    si.CreateNoWindow = true;
                    si.RedirectStandardOutput = true;
                    si.StandardOutputEncoding = new UTF8Encoding(false);
                    using (Process p = Process.Start(si))
                    {
                        string ra = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(30000);
                        List<string> ds = ra.Split('\n').Select(d => d.Trim()).Where(d => Regex.IsMatch(d, "^[a-z0-9.\\-]+\t.+$"))
                            .Select(d => d.Replace('\t', '=').Replace("|", "/")).ToList();
                        if (ds.Count > 0) GiaoDien.LuuCaiDat("agy_models", string.Join("|", ds));
                    }
                }
                catch (Exception) { } // agy lỗi / chưa đăng nhập: giữ danh sách cũ
            });
        }
    }
}
