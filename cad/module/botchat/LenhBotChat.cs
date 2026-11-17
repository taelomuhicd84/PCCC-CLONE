// Plugin Bot chat (bundle riêng TKPCCC_BotChat.bundle, người dùng 17/11: tách khỏi plugin PCCC). Lệnh TKBOTCHAT mở palette Bot chat
// (neo trái / phải, kéo giãn được). Palette PCCC muốn gửi câu mẫu (vd sơ đồ nguyên lý) thì ghi khoá "bot_mau_cho" rồi gọi TKBOTCHAT.
using System;
using System.Drawing;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;

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
                }
                ps.Visible = true;
                if (GiaoDien.DocCaiDat("bot_mau_cho", "") == "so_do")
                {
                    GiaoDien.LuuCaiDat("bot_mau_cho", "");
                    bot.DatCau(BotChat.MAU_SO_DO);
                }
            });
        }
    }
}
