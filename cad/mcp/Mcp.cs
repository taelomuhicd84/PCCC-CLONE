// Máy chủ MCP cho AutoCAD (stdio, JSON-RPC, mỗi dòng một thông điệp). Ghi việc vào hàng đợi file, chờ plugin trả kết quả.
// Chỉ dùng .NET Framework có sẵn trong Windows: máy khác không phải cài Node hay Python. Biên dịch: cad\build.bat.
// AI chỉ gọi được 2 công cụ dưới đây; không có đường chạy LISP hay lệnh tự do.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace TKPCCC
{
    static class Mcp
    {
        static readonly JavaScriptSerializer Js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        static object Lay(Dictionary<string, object> d, string k) { object v; return d.TryGetValue(k, out v) ? v : null; }

        static int Main()
        {
            StreamReader vao = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
            StreamWriter ra = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false));
            ra.NewLine = "\n";
            ra.AutoFlush = true;
            string dong;
            while ((dong = vao.ReadLine()) != null)
            {
                if (dong.Trim().Length == 0) continue;
                Dictionary<string, object> y;
                try { y = Js.Deserialize<Dictionary<string, object>>(dong); }
                catch (Exception) { ra.WriteLine(Js.Serialize(Loi(null, -32700, "Parse error"))); continue; }
                object id;
                bool coId = y.TryGetValue("id", out id);
                string pt = Lay(y, "method") as string;
                if (pt == null || !coId) continue; // thông báo, hoặc câu trả lời của client: không đáp
                Dictionary<string, object> ts = Lay(y, "params") as Dictionary<string, object> ?? new Dictionary<string, object>();
                Dictionary<string, object> tra;
                try { tra = XuLy(id, pt, ts); }
                catch (Exception ex) { tra = Loi(id, -32603, ex.Message); }
                ra.WriteLine(Js.Serialize(tra));
            }
            return 0;
        }

        static Dictionary<string, object> Loi(object id, int ma, string tin)
        {
            return new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", id },
                { "error", new Dictionary<string, object> { { "code", ma }, { "message", tin } } } };
        }

        static Dictionary<string, object> Xong(object id, object kq)
        {
            return new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", id }, { "result", kq } };
        }

        static Dictionary<string, object> XuLy(object id, string pt, Dictionary<string, object> ts)
        {
            switch (pt)
            {
                case "initialize":
                    return Xong(id, new Dictionary<string, object> {
                        { "protocolVersion", Lay(ts, "protocolVersion") as string ?? "2025-06-18" },
                        { "capabilities", new Dictionary<string, object> { { "tools", new Dictionary<string, object>() } } },
                        { "serverInfo", new Dictionary<string, object> { { "name", "tkpccc-cad" }, { "version", "0.1.0" } } },
                        { "instructions", TuVung(true) } });
                case "ping":
                    return Xong(id, new Dictionary<string, object>());
                case "tools/list":
                    return Xong(id, new Dictionary<string, object> { { "tools", CongCu() } });
                case "tools/call":
                    Dictionary<string, object> a = Lay(ts, "arguments") as Dictionary<string, object> ?? new Dictionary<string, object>();
                    bool hong;
                    string chu = Goi(Lay(ts, "name") as string, a, out hong);
                    return Xong(id, new Dictionary<string, object> {
                        { "content", new object[] { new Dictionary<string, object> { { "type", "text" }, { "text", chu } } } },
                        { "isError", hong } });
                default:
                    return Loi(id, -32601, "Method not found: " + pt);
            }
        }

        // Mô tả ngắn có chủ ý: mọi AI phải đọc phần này ở mỗi phiên.
        static object[] CongCu()
        {
            Dictionary<string, object> kieuSo = new Dictionary<string, object> { { "type", "number" } };
            return new object[] {
                new Dictionary<string, object> {
                    { "name", "cad_tt" },
                    { "description", "Trạng thái AutoCAD đang mở: bản vẽ, số thiết bị PCCC theo mã, số đối tượng theo layer. tu_vung=true: trả từ vựng spec đầy đủ thay cho trạng thái." },
                    { "inputSchema", new Dictionary<string, object> { { "type", "object" },
                        { "properties", new Dictionary<string, object> { { "tu_vung", new Dictionary<string, object> { { "type", "boolean" } } } } } } } },
                new Dictionary<string, object> {
                    { "name", "cad_chay" },
                    { "description", "Chạy MỘT gói spec trong AutoCAD (vẽ, xoá, đọc, đếm) = 1 bước Undo. Trả số đối tượng đã vẽ/xoá và lỗi từng thao tác. Gộp mọi thao tác vào một lần gọi. Spec dài hoặc cần chạy lại thì lưu file .json rồi truyền \"file\"." },
                    { "inputSchema", new Dictionary<string, object> { { "type", "object" },
                        { "properties", new Dictionary<string, object> {
                            { "ops", new Dictionary<string, object> { { "type", "array" }, { "items", new Dictionary<string, object> { { "type", "object" } } } } },
                            { "goc", new Dictionary<string, object> { { "type", "array" }, { "items", kieuSo }, { "description", "[x,y] cộng vào mọi toạ độ" } } },
                            { "zoom", new Dictionary<string, object> { { "type", "boolean" } } },
                            { "file", new Dictionary<string, object> { { "type", "string" }, { "description", "đường dẫn tuyệt đối file spec .json, thay cho ops" } } } } } } } } };
        }

        static string Goi(string ten, Dictionary<string, object> a, out bool hong)
        {
            hong = false;
            if (ten == "cad_tt")
            {
                object tv = Lay(a, "tu_vung");
                if (tv is bool && (bool)tv) return TuVung(false);
                return ChayViec("{\"ops\":[{\"op\":\"dem\"},{\"op\":\"doc\",\"gi\":\"layer\"}]}", out hong);
            }
            if (ten == "cad_chay")
            {
                string json;
                string file = Lay(a, "file") as string;
                if (!string.IsNullOrEmpty(file))
                {
                    if (!File.Exists(file)) { hong = true; return "Không thấy file spec: " + file; }
                    json = File.ReadAllText(file, Encoding.UTF8);
                }
                else
                {
                    if (Lay(a, "ops") == null) { hong = true; return "cad_chay cần \"ops\" (mảng thao tác) hoặc \"file\". Từ vựng: gọi cad_tt với tu_vung=true."; }
                    json = Js.Serialize(a);
                }
                return ChayViec(json, out hong);
            }
            hong = true;
            return "Không có công cụ " + ten;
        }

        /// Plugin ghi song.json khoảng 2 giây một lần; quá 6 giây coi như AutoCAD chưa mở hoặc chưa nạp plugin.
        static bool ConSong()
        {
            for (int lan = 0; lan < 2; lan++)
            {
                try
                {
                    Dictionary<string, object> s = Js.Deserialize<Dictionary<string, object>>(File.ReadAllText(HangDoi.Song, Encoding.UTF8));
                    DateTime luc = DateTime.Parse(Lay(s, "luc") as string, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();
                    if ((DateTime.UtcNow - luc).TotalSeconds < 6) return true;
                }
                catch (Exception) { }
                Thread.Sleep(400);
            }
            return false;
        }

        static string ChayViec(string json, out bool hong)
        {
            hong = true;
            if (!ConSong())
                return "AutoCAD chưa mở hoặc chưa nạp plugin TKPCCC. Mở AutoCAD (bundle TKPCCC tự nạp) hoặc gõ NETLOAD chọn TKPCCC.Cad.dll, rồi gọi lại.";
            HangDoi.TaoThuMuc();
            string ten = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".json";
            string fVao = Path.Combine(HangDoi.Vao, ten), fRa = Path.Combine(HangDoi.Ra, ten);
            HangDoi.GhiAnToan(fVao, json);
            int cho = 60;
            int.TryParse(Environment.GetEnvironmentVariable("TKPCCC_CAD_CHO_GIAY") ?? "", out cho);
            if (cho <= 0) cho = 60;
            DateTime han = DateTime.UtcNow.AddSeconds(cho);
            while (true)
            {
                if (File.Exists(fRa))
                {
                    string kq = File.ReadAllText(fRa, Encoding.UTF8);
                    try { File.Delete(fRa); } catch (Exception) { }
                    hong = false;
                    return kq;
                }
                if (DateTime.UtcNow > han)
                {
                    // Chưa ai nhận: rút việc lại để nó không chạy muộn sau khi đã báo lỗi.
                    try
                    {
                        if (File.Exists(fVao))
                        {
                            File.Delete(fVao);
                            return "AutoCAD đang bận (dở lệnh khác hoặc đang mở hộp thoại) nên chưa nhận việc. Kết thúc lệnh trong AutoCAD rồi gọi lại.";
                        }
                    }
                    catch (Exception) { }
                    if (DateTime.UtcNow > han.AddSeconds(30)) return "Quá thời gian chờ AutoCAD trả kết quả; kiểm tra dòng lệnh AutoCAD.";
                }
                Thread.Sleep(100);
            }
        }

        /// TU_VUNG.md nằm cạnh exe (build.bat chép) hoặc ở ..\spec. tomTat = phần trước dòng "---" đầu tiên, gửi cho AI lúc kết nối.
        static string TuVung(bool tomTat)
        {
            string thuMuc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (string f in new string[] { Path.Combine(thuMuc, "TU_VUNG.md"), Path.Combine(thuMuc, "..", "spec", "TU_VUNG.md") })
            {
                if (!File.Exists(f)) continue;
                string t = File.ReadAllText(f, Encoding.UTF8);
                if (!tomTat) return t;
                int i = t.IndexOf("\n---");
                string dau = i > 0 ? t.Substring(0, i) : t;
                return dau.Length > 1900 ? dau.Substring(0, 1900) : dau; // Claude Code cắt phần hướng dẫn của máy chủ MCP quanh 2 KB
            }
            return "Vẽ AutoCAD bằng cad_chay với {\"ops\":[...]}. Thiếu file TU_VUNG.md cạnh TKPCCC.Mcp.exe: chạy lại cad\\build.bat.";
        }
    }
}
