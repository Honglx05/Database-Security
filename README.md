# Đồ án Bảo mật Cơ sở dữ liệu - Ứng dụng Quản lý Sinh viên 🛡️

Kho lưu trữ này chứa mã nguồn C# (Windows Forms) và kịch bản cơ sở dữ liệu Oracle của dự án môn học Bảo mật Cơ sở dữ liệu tại trường Đại học Công Thương TP.HCM (HUIT).

## 👥 Thành viên Nhóm 10
**Lê Xuân Hồng** (2033230099)

## 📁 Cấu trúc thư mục
- 📂 `Code/`: Chứa toàn bộ mã nguồn của ứng dụng giao diện.
- 📂 `DB/`: Chứa các script khởi tạo cơ sở dữ liệu Oracle (`QLSV.sql`, `sys.sql`).

## 🚀 Hướng dẫn cài đặt và sử dụng
1. Chạy file `sys.sql` bằng tài khoản quản trị (System/sysdba) trên Oracle để tạo các Tablespace (TBS_QLSV), thiết lập Profile và phân quyền cơ bản.
2. Chạy file `QLSV.sql` để khởi tạo cấu trúc các bảng và chèn dữ liệu mẫu.
3. Mở thư mục `Code`, cập nhật lại chuỗi kết nối (Connection String) trong project C# và chạy ứng dụng.

## 🛠️ Các kỹ thuật bảo mật đã áp dụng
Đồ án tích hợp hệ thống bảo mật đa lớp nhằm đảm bảo Tính bí mật, Tính toàn vẹn và Tính sẵn sàng:
* **Mã hóa đối xứng (AES-256):** Mã hóa ở mức ứng dụng để bảo mật các dữ liệu cá nhân nhạy cảm như Địa chỉ và Số điện thoại.
* **Mã hóa bất đối xứng (RSA) & Chữ ký số:** Tạo mã băm SHA-256 và ký số để đảm bảo toàn vẹn dữ liệu điểm số, hệ thống tự động phát hiện và cảnh báo nếu có hành vi sửa điểm trái phép trực tiếp dưới CSDL.
* **Mã hóa lai (Hybrid Encryption):** Kết hợp tốc độ của AES và khả năng phân phối khóa an toàn của RSA để bảo vệ tính năng "Ghi chú mật".
* **Kiểm soát truy cập đa mô hình (DAC, RBAC, MAC):** Áp dụng phân quyền trực tiếp, phân quyền theo vai trò (Role) và đặc biệt là kiểm soát truy cập mức dòng bằng công nghệ VPD (Virtual Private Database), chia 3 cấp độ (Thường, Mật, Tuyệt mật).
* **Kiểm toán (Auditing):** Sử dụng Standard Audit để giám sát các hành vi xóa hồ sơ sinh viên và Fine-Grained Audit (FGA) để lập nhật ký các truy vấn soi mói điểm cao (Điểm > 9).
* **Quản trị Tài khoản:** Thiết lập Profile để chống Brute-force bằng cách khóa tài khoản khi nhập sai mật khẩu 3 lần và giới hạn thời gian sống của mật khẩu trong 30 ngày.
