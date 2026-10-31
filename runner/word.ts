// Xuất báo cáo khảo sát PCCC ra .docx (8 mục theo knowledge/khao-sat-bao-gia.md).
import { AlignmentType, Document, HeadingLevel, Packer, Paragraph, Table, TableCell, TableRow, TextRun, WidthType } from "docx";
import type { KhaoSat } from "./khao_sat";

const p = (text: string, bold = false) => new Paragraph({ children: [new TextRun({ text, bold })] });
const h = (text: string) => new Paragraph({ text, heading: HeadingLevel.HEADING_2 });
const o = (text: string, bold = false) => new TableCell({ children: [p(text, bold)] });
const bang = (dong: string[][]) =>
  new Table({ width: { size: 100, type: WidthType.PERCENTAGE }, rows: dong.map((d, i) => new TableRow({ children: d.map((x) => o(x, i === 0)) })) });
const so = (x: number | null, dv = "") => (x == null ? "" : x.toLocaleString("vi-VN") + dv);

export async function xuatKhaoSat(ks: KhaoSat): Promise<Buffer> {
  const doc = new Document({
    styles: { default: { document: { run: { font: "Times New Roman", size: 26 } } } },
    sections: [{
      children: [
        new Paragraph({ text: "BÁO CÁO KHẢO SÁT PCCC", heading: HeadingLevel.TITLE, alignment: AlignmentType.CENTER }),
        new Paragraph({ alignment: AlignmentType.CENTER, children: [new TextRun({ text: `Dự án: ${ks.du_an} · Ngày lập: ${new Date().toLocaleDateString("vi-VN")}`, italics: true })] }),
        h("1. Hạng mục, công năng"),
        bang([["Nội dung", "Thông tin"], ["Hạng mục", ks.hang_muc], ["Công năng (nhóm F)", ks.cong_nang]]),
        h("2. Diện tích, số tầng"),
        bang([["Nội dung", "Thông tin"], ["Tổng diện tích sàn", so(ks.dien_tich_m2, " m²")], ["Số tầng nổi", so(ks.so_tang)], ["Số tầng hầm", so(ks.so_tang_ham)], ["Chu vi nhà", so(ks.chu_vi_m, " m")]]),
        h("3. Chiều cao PCCC"),
        p(ks.chieu_cao_pccc || "—"),
        p(`Chiều cao công trình: ${so(ks.chieu_cao_m, " m") || "—"}`),
        h("4. Khối tích"),
        p(ks.khoi_tich || "—"),
        h("5. Thuộc diện thẩm định thiết kế PCCC"),
        p(ks.dien_tham_dinh || "—", true),
        p(ks.dien_tham_dinh_can_cu),
        h("6. Lối thoát nạn"),
        bang([["Nội dung", "Thông tin"], ["Số lối thoát nạn", so(ks.so_loi_thoat_nan)], ["Theo tầng", ks.loi_thoat_nan], ["Đánh giá", ks.thoat_nan_danh_gia], ["Ghi chú", ks.thoat_nan_ghi_chu]]),
        h("7. Hệ thống PCCC phải lắp đặt"),
        bang([["Hệ thống", "Trang bị", "Căn cứ", "Ghi chú"], ...ks.he_thong.map((x) => [x.ten, x.co, x.can_cu, x.ghi_chu])]),
        h("8. Bể nước PCCC"),
        p(ks.be_nuoc_pccc || (ks.be_nuoc_m3 ? `${so(ks.be_nuoc_m3)} m³` : "—")),
        new Paragraph({ children: [new TextRun({ text: "Số liệu ước lượng cần kỹ sư PCCC kiểm tra lại trước khi sử dụng.", italics: true, size: 22 })] }),
      ],
    }],
  });
  return Packer.toBuffer(doc);
}
