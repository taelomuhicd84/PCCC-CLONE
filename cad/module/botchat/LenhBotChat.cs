// Plugin Bot chat (bundle riêng TKPCCC_BotChat.bundle, người dùng 17/11: tách khỏi plugin PCCC). Lệnh TKBOTCHAT mở palette Bot chat
// (neo trái / phải, kéo giãn được). Palette PCCC muốn gửi câu mẫu (vd sơ đồ nguyên lý) thì ghi khoá "bot_mau_cho" rồi gọi TKBOTCHAT.
// Người dùng 18/11 (như VS Code / Claude Code): đối tượng đang chọn trên bản vẽ tự thành thẻ "vùng chọn" gửi kèm lời nhắn kế;
// TKBOTCHON (nút "Quét vùng CAD") hỏi quét chọn rồi gắn vùng đó, giữ cả khi bỏ chọn.
using System;
using System.Drawing;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(TKPCCC.LenhBotChat))]

namespace TKPCCC
{
    public class LenhBotChat
    {
        static PaletteSet ps;
        static BotChat bot;

        [CommandMethod("TKBOTCHAT")]
        public void Mo()
        {
            GiaoDien.An(delegate
            {
                if (ps == null)
                {
                    bot = new BotChat();
                    ps = new PaletteSet("TKPCCC - Bot chat", new Guid("7C2B9E14-3F6A-4D85-B0A1-5E9D3C7F2A68"));
                    ps.MinimumSize = new Size(300, 360);
                    ps.Add("Bot chat", bot);
                    ps.DockEnabled = DockSides.Left | DockSides.Right;
                    ps.Visible = true;
                    ps.Size = new Size(380, 640);
                    ps.Dock = DockSides.Right;
                    foreach (Document d in AcApp.DocumentManager) Theo(d);
                    AcApp.DocumentManager.DocumentCreated += delegate(object s, DocumentCollectionEventArgs e) { Theo(e.Document); };
                }
                ps.Visible = true;
                if (GiaoDien.DocCaiDat("bot_mau_cho", "") == "so_do")
                {
                    GiaoDien.LuuCaiDat("bot_mau_cho", "");
                    bot.DatCau(BotChat.MAU_SO_DO);
                }
            });
        }

        /// Theo dõi đối tượng đang chọn (pickfirst) của một bản vẽ để cập nhật thẻ vùng chọn.
        static void Theo(Document d)
        {
            d.ImpliedSelectionChanged += delegate
            {
                try
                {
                    if (bot == null || bot.IsDisposed || d != AcApp.DocumentManager.MdiActiveDocument) return;
                    PromptSelectionResult r = d.Editor.SelectImplied();
                    bot.ChonDoi(d, r.Status == PromptStatus.OK ? r.Value.GetObjectIds() : new ObjectId[0]);
                }
                catch (System.Exception) { } // sự kiện AutoCAD: lỗi không được thoát ra ngoài
            };
        }

        [CommandMethod("TKBOTCHON", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public void Chon()
        {
            Mo();
            Document d = AcApp.DocumentManager.MdiActiveDocument;
            PromptSelectionOptions o = new PromptSelectionOptions();
            o.MessageForAdding = "\nQuét chọn vùng gửi kèm Bot chat: ";
            PromptSelectionResult r = d.Editor.GetSelection(o);
            if (r.Status != PromptStatus.OK) return;
            ObjectId[] ids = r.Value.GetObjectIds();
            bot.DatVung(d, ids, true);
            d.Editor.SetImpliedSelection(ids);
            d.Editor.WriteMessage("\n[TKPCCC] Bot chat: vùng chọn " + ids.Length + " đối tượng, gõ yêu cầu trong khung chat.");
        }
    }
}
