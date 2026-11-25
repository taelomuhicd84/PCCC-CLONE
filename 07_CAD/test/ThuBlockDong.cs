// Chỉ cho 07_CAD/test/chay_thu.ps1: chèn block động từ bản vẽ mẫu của Autodesk (cài cùng AutoCAD, không đưa vào repo) và đặt trạng thái
// Visibility/Lookup, vì accoreconsole không có setpropertyvalue, BEDIT. Không thuộc plugin, không build vào bundle.
// File danh sách (biến môi trường TKPCCC_THU_DONG), mỗi dòng: bản vẽ mẫu|tên block|tên tham số|giá trị|x|y
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(TKPCCC.Thu.BlockDong))]

namespace TKPCCC.Thu
{
    public class BlockDong
    {
        [CommandMethod("THU_BLOCKDONG")]
        public void Chen()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            int n = 0, sai = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (string dong in File.ReadAllLines(Environment.GetEnvironmentVariable("TKPCCC_THU_DONG"), Encoding.UTF8))
                {
                    string[] c = dong.Split('|');
                    if (c.Length < 6) continue;
                    if (!bt.Has(c[1]))
                        using (Database mau = new Database(false, true))
                        {
                            mau.ReadDwgFile(c[0], FileOpenMode.OpenForReadAndAllShare, true, "");
                            ObjectIdCollection ids = new ObjectIdCollection();
                            using (Transaction t2 = mau.TransactionManager.StartTransaction())
                            {
                                ids.Add(((BlockTable)t2.GetObject(mau.BlockTableId, OpenMode.ForRead))[c[1]]);
                                t2.Commit();
                            }
                            mau.WblockCloneObjects(ids, db.BlockTableId, new IdMapping(), DuplicateRecordCloning.Ignore, false);
                        }
                    BlockReference br = new BlockReference(new Point3d(double.Parse(c[4], CultureInfo.InvariantCulture), double.Parse(c[5], CultureInfo.InvariantCulture), 0), bt[c[1]]);
                    ms.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);
                    bool dat = false;
                    foreach (DynamicBlockReferenceProperty p in br.DynamicBlockReferencePropertyCollection)
                        if (p.PropertyName == c[2]) { p.Value = c[3]; dat = Convert.ToString(p.Value) == c[3]; }
                    if (dat) n++; else sai++;
                }
                tr.Commit();
            }
            doc.Editor.WriteMessage("\n[THU] THU_BLOCKDONG chen=" + n + " sai=" + sai);
        }
    }
}
