# Báo giá PCCC (tab 8, sau báo cáo khảo sát)

Không phải thẩm định. Từ số liệu báo cáo khảo sát (khối json `khao_sat`: diện tích, số tầng, chiều cao PCCC, lối thoát nạn, hệ thống CÓ / CẦN XÁC NHẬN) lập bảng khối lượng sơ bộ và đơn giá. Đây là bản mặc định để người dùng chỉnh: mọi khối lượng là ƯỚC LƯỢNG, phải ghi cách ước.

## Nguyên tắc
- Chỉ báo giá hệ thống khảo sát ghi CÓ; hệ thống CẦN XÁC NHẬN thì vẫn đưa vào nhưng ghi chú "chờ xác nhận" (người dùng bỏ nếu không cần).
- Khối lượng ước theo diện tích sàn, số tầng, số lối thoát nạn, số phòng (nếu bản vẽ có); làm tròn lên.
- Đơn giá: chỉ lấy từ mục "Bảng đơn giá" dưới đây hoặc người dùng cho trong chat. Không có thì để null; KHÔNG tự đặt giá thị trường.
- Thiếu diện tích từng tầng thì chia đều tổng diện tích cho số tầng.

## Quy tắc ước khối lượng mặc định (người dùng chỉnh lại theo kinh nghiệm)
| Hệ thống | Vật tư chính (ĐVT) | Cách ước mặc định |
|---|---|---|
| Báo cháy tự động | Tủ trung tâm (bộ) | 1 bộ; số kênh ≥ số tầng (mỗi tầng ít nhất 1 kênh) |
| | Đầu báo khói (cái) | diện tích sàn / 60 m², tối thiểu 1 đầu mỗi phòng có người |
| | Đầu báo nhiệt (cái) | bếp, phòng kỹ thuật: 1 cái / phòng (nếu bản vẽ thấy) |
| | Tổ hợp nút ấn + chuông + đèn (bộ) | số lối thoát nạn x số tầng |
| | Dây tín hiệu chống cháy (m) | diện tích sàn x 0,6 m/m² |
| Họng nước vách tường | Tủ họng (bộ: van, lăng, cuộn vòi) | mỗi tầng: diện tích tầng / 500 m², tối thiểu 1 (≥ 2 nếu tiêu chuẩn yêu cầu 2 họng cùng phun) |
| | Ống thép tráng kẽm (m) | chiều cao nhà + 15 m/tầng |
| Chữa cháy sprinkler | Đầu phun (cái) | diện tích được bảo vệ / 10 m² |
| | Ống, van báo động (m, bộ) | ống: diện tích x 0,3 m/m²; 1 van báo động / hệ |
| Cấp nước ngoài nhà | Trụ nước (trụ) | 1 trụ / 150 m chu vi, tối thiểu 1 |
| Trạm bơm | Bộ bơm (bơm điện chính + bơm dự phòng + bơm bù) | 1 trạm |
| Bể nước PCCC | Bể (m³) | lấy từ khảo sát; chưa tính thì khối lượng null, ghi "chờ tính bể" |
| Hút khói, tăng áp | Quạt (bộ), ống gió (m²) | 1 quạt tăng áp / buồng thang không nhiễm khói; hút khói hành lang: theo khảo sát |
| Chiếu sáng sự cố | Đèn sự cố (bộ) | diện tích hành lang + sảnh / 50 m²; không biết thì diện tích sàn / 100 m² |
| Chỉ dẫn thoát nạn | Đèn EXIT (bộ) | số cửa thoát nạn + 1 mỗi chỗ đổi hướng; ước: số lối thoát nạn x số tầng x 2 |
| Bình chữa cháy | Bình bột 4–8 kg + CO2 (bộ 2 bình) | diện tích sàn / 150 m², tối thiểu 1 bộ mỗi tầng; tra lại QCVN 10 phụ lục bình |
| Cấp điện PCCC | Tủ điện ưu tiên + ATS + cáp chống cháy (bộ, m) | 1 tủ; cáp = chiều cao nhà + 20 m |
| Chống sét | Kim thu, dây thoát, cọc tiếp địa (bộ) | 1 hệ |
| Ngăn cháy | Cửa chống cháy (bộ) | số buồng thang x số tầng + phòng kỹ thuật |
| Hồ sơ, thủ tục | Thiết kế, thẩm duyệt, nghiệm thu (gói) | 1 gói mỗi loại |

Ghi chú: nhân công lắp đặt có thể gộp vào đơn giá vật tư hoặc tách dòng "Nhân công lắp đặt (gói)". Thuế VAT mặc định 10% (người dùng đổi trong Excel).

## Bảng đơn giá (người dùng điền; để trống thì đơn giá null)
| Vật tư / hạng mục | ĐVT | Đơn giá (VNĐ) |
|---|---|---|
| (chưa có) | | |

## Định dạng trả lời
Phần chữ ngắn: tóm tắt hạng mục và cách ước từng dòng. Cuối cùng thêm đúng MỘT khối ```json:
```json
{"bao_gia":{"dong":[{"nhom":"Báo cháy tự động","hang_muc":"Đầu báo khói","dvt":"cái","khoi_luong":12,"don_gia":null,"cach_uoc":"720 m² / 60","ghi_chu":""}],"ghi_chu":"giả định chung"}}
```
`khoi_luong` là số (không biết thì null); `don_gia` là số hoặc null.
