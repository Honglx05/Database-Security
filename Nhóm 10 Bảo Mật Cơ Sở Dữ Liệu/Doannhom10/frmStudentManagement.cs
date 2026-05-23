using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.IO; // Thêm thư viện này để check file key

namespace Doannhom10
{
    public partial class frmStudentManagement : Form
    {
        // 1. SỬA: Bỏ 'const', chỉ khai báo biến
        private string ConnectionString;

        private RSACryptoServiceProvider rsa;
        private string publicKeyXml;

        // 2. SỬA: Thêm tham số user, pass vào hàm khởi tạo
        public frmStudentManagement(string user, string pass)
        {
            InitializeComponent();
            CenterToScreen();

            // 3. Tạo chuỗi kết nối động dựa trên user đăng nhập
            // (Nhớ check lại localhost:1521/orcl21 nếu máy bro khác)
            this.ConnectionString = $"Data Source=localhost:1521/orcl21;User Id={user};Password={pass};";
        }

        // --- 1. LOAD DATA ---
        private void btnLoad_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT * FROM SINHVIEN";
                    OracleDataAdapter da = new OracleDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvSinhVien.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        // --- 2. ENCRYPT & SAVE (AES Thường - Cho Địa chỉ & SĐT) ---
        private void btnEncryptSave_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra đầu vào
            if (string.IsNullOrWhiteSpace(txtMaSV.Text))
            {
                MessageBox.Show("Vui lòng nhập Mã sinh viên (Student ID is required).");
                return;
            }

            try
            {
                // 2. Mã hóa dữ liệu nhạy cảm trước (AES)
                string encryptedDiaChi = AesService.EncryptString(txtDiaChi.Text);
                string encryptedSDT = AesService.EncryptString(txtSDT.Text);

                using (OracleConnection conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();

                    // 3. KIỂM TRA: Sinh viên này đã tồn tại chưa?
                    string checkSql = "SELECT COUNT(*) FROM SINHVIEN WHERE MASV = :masv";
                    int count = 0;
                    using (OracleCommand checkCmd = new OracleCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.Add("masv", OracleDbType.Varchar2).Value = txtMaSV.Text;
                        count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    }

                    string sql = "";

                    // 4. LOGIC RẼ NHÁNH
                    if (count > 0)
                    {
                        // TRƯỜNG HỢP A: Đã có -> UPDATE (Cập nhật thông tin)
                        sql = "UPDATE SINHVIEN SET HOTEN = :hoten, NGAYSINH = :ngaysinh, " +
                              "DIACHI = :diachi, SODIENTHOAI = :sdt, DIEM = :diem " +
                              "WHERE MASV = :masv";
                    }
                    else
                    {
                        // TRƯỜNG HỢP B: Chưa có -> INSERT (Thêm mới)
                        sql = "INSERT INTO SINHVIEN (MASV, HOTEN, NGAYSINH, DIACHI, SODIENTHOAI, DIEM) " +
                              "VALUES (:masv, :hoten, :ngaysinh, :diachi, :sdt, :diem)";
                    }

                    // 5. Thực thi lệnh (Dùng chung cho cả Update và Insert)
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true; // Quan trọng để map tham số theo tên

                        // Truyền tham số (Thứ tự add không quan trọng, miễn đúng tên)
                        cmd.Parameters.Add("masv", OracleDbType.Varchar2).Value = txtMaSV.Text;
                        cmd.Parameters.Add("hoten", OracleDbType.Varchar2).Value = txtHoTen.Text;
                        cmd.Parameters.Add("ngaysinh", OracleDbType.Date).Value = dtpNgaySinh.Value;

                        // Lưu chuỗi ĐÃ MÃ HÓA xuống DB
                        cmd.Parameters.Add("diachi", OracleDbType.Varchar2).Value = encryptedDiaChi;
                        cmd.Parameters.Add("sdt", OracleDbType.Varchar2).Value = encryptedSDT;

                        // Xử lý điểm số
                        decimal diem = 0;
                        decimal.TryParse(txtDiem.Text, out diem);
                        cmd.Parameters.Add("diem", OracleDbType.Decimal).Value = diem;

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Lưu thành công! (Dữ liệu đã được mã hóa)", "Success");

                // Load lại bảng ngay lập tức để thấy thay đổi
                LoadData();

                // Xóa trắng ô nhập liệu
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message);
            }
        }

        // --- 3. CLICK GRID ---
        private void dgvSinhVien_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvSinhVien.Rows[e.RowIndex];

                txtMaSV.Text = row.Cells["MASV"].Value.ToString();
                txtHoTen.Text = row.Cells["HOTEN"].Value.ToString();

                if (row.Cells["NGAYSINH"].Value != DBNull.Value)
                    dtpNgaySinh.Value = Convert.ToDateTime(row.Cells["NGAYSINH"].Value);

                if (row.Cells["DIEM"].Value != DBNull.Value)
                    txtDiem.Text = row.Cells["DIEM"].Value.ToString();
                else
                    txtDiem.Text = "";

                // Hiện chuỗi mã hóa AES
                txtDiaChi.Text = row.Cells["DIACHI"].Value != DBNull.Value ? row.Cells["DIACHI"].Value.ToString() : "";
                txtSDT.Text = row.Cells["SODIENTHOAI"].Value != DBNull.Value ? row.Cells["SODIENTHOAI"].Value.ToString() : "";

                // Hiện chuỗi mã hóa HYBRID (Ghi chú)
                txtGhiChu.Text = "";
                if (dgvSinhVien.Columns.Contains("GHICHU_MAT") && row.Cells["GHICHU_MAT"].Value != DBNull.Value)
                {
                    txtGhiChu.Text = row.Cells["GHICHU_MAT"].Value.ToString();
                }
            }
        }

        // --- 4. DECRYPT (Bao gồm cả Hybrid) ---
        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            try
            {
                // A. Giải mã AES thường (Địa chỉ, SĐT)
                string cipherDiaChi = txtDiaChi.Text;
                string cipherSDT = txtSDT.Text;

                string plainDiaChi = AesService.DecryptString(cipherDiaChi);
                string plainSDT = AesService.DecryptString(cipherSDT);

                txtDiaChi.Text = plainDiaChi;
                txtSDT.Text = plainSDT;

                // B. Giải mã Hybrid (Ghi chú)
                if (dgvSinhVien.CurrentRow != null)
                {
                    var cellKhoa = dgvSinhVien.CurrentRow.Cells["KHOA_GIAIMA"].Value;
                    var cellGhiChu = dgvSinhVien.CurrentRow.Cells["GHICHU_MAT"].Value;

                    if (cellKhoa != DBNull.Value && cellGhiChu != DBNull.Value)
                    {
                        string encryptedKeyBase64 = cellKhoa.ToString();
                        string encryptedNoteBase64 = cellGhiChu.ToString();

                        // B1. RSA giải mã lấy Key
                        byte[] encryptedKeyBytes = Convert.FromBase64String(encryptedKeyBase64);
                        byte[] originalKeyBytes = rsa.Decrypt(encryptedKeyBytes, false);
                        string sessionKey = Encoding.UTF8.GetString(originalKeyBytes);

                        // B2. AES giải mã Ghi chú
                        string plainNote = DecryptAES(encryptedNoteBase64, sessionKey);

                        // B3. Hiện kết quả
                        txtGhiChu.Text = plainNote;
                    }
                }

                MessageBox.Show("Đã giải mã xong!", "Thành công");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi giải mã: " + ex.Message);
            }
        }

        private void ClearInputs()
        {
            txtMaSV.Clear();
            txtHoTen.Clear();
            txtDiaChi.Clear();
            txtSDT.Clear();
            txtDiem.Clear();
            txtGhiChu.Clear();
        }

        // --- 5. SIGN (Chữ ký số) ---
        private void btnSign_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaSV.Text) || string.IsNullOrWhiteSpace(txtDiem.Text))
            {
                MessageBox.Show("Select a student and ensure Grade is not empty.");
                return;
            }

            try
            {
                byte[] dataToSign = System.Text.Encoding.UTF8.GetBytes(txtDiem.Text);
                byte[] signatureBytes = rsa.SignData(dataToSign, SHA256.Create());
                string signatureBase64 = Convert.ToBase64String(signatureBytes);

                MessageBox.Show("Generated Signature:\n" + signatureBase64, "Digital Signature Created");

                using (OracleConnection conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = "UPDATE SINHVIEN SET CHUKY = :chuky WHERE MASV = :masv";
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("chuky", OracleDbType.Varchar2).Value = signatureBase64;
                        cmd.Parameters.Add("masv", OracleDbType.Varchar2).Value = txtMaSV.Text;
                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0) MessageBox.Show("Signature saved!", "Success");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error signing data: " + ex.Message);
            }
        }

        // --- 6. LOAD FORM & KEY ---
        private void frmStudentManagement_Load(object sender, EventArgs e)
        {
            // TỰ ĐỘNG LƯU/ĐỌC KHÓA TỪ FILE ĐỂ KHÔNG BỊ LỖI KHI RESTART
            try
            {
                string keyPath = "private_key.xml";
                rsa = new RSACryptoServiceProvider(1024);

                if (File.Exists(keyPath))
                {
                    // Có file -> Đọc lại khóa cũ
                    string xmlKeys = File.ReadAllText(keyPath);
                    rsa.FromXmlString(xmlKeys);
                }
                else
                {
                    // Chưa có -> Tạo mới và Lưu
                    string xmlKeys = rsa.ToXmlString(true);
                    File.WriteAllText(keyPath, xmlKeys);
                }
                publicKeyXml = rsa.ToXmlString(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi Key RSA: " + ex.Message);
            }
        }

        // --- 7. VERIFY ---
        private void btnVerify_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaSV.Text) || string.IsNullOrWhiteSpace(txtDiem.Text)) return;

            try
            {
                string dbSignature = null;
                using (OracleConnection conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT CHUKY FROM SINHVIEN WHERE MASV = :masv";
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("masv", OracleDbType.Varchar2).Value = txtMaSV.Text;
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value) dbSignature = result.ToString();
                    }
                }

                if (string.IsNullOrEmpty(dbSignature))
                {
                    MessageBox.Show("No signature found.", "Warning");
                    return;
                }

                byte[] currentData = System.Text.Encoding.UTF8.GetBytes(txtDiem.Text);
                byte[] signatureBytes = Convert.FromBase64String(dbSignature);
                bool isValid = rsa.VerifyData(currentData, SHA256.Create(), signatureBytes);

                if (isValid)
                    MessageBox.Show("Dữ liệu toàn vẹn (Integrity Verified)!", "Valid", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("CẢNH BÁO: Dữ liệu đã bị sửa đổi!", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // --- 8. LƯU HYBRID (Nút mới) ---
        private void btnLuuHybrid_Click(object sender, EventArgs e)
        {
            try
            {
                string plainText = txtGhiChu.Text;
                string maSV = txtMaSV.Text;

                if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(maSV))
                {
                    MessageBox.Show("Vui lòng nhập Mã SV và Ghi chú!");
                    return;
                }

                // A. Tạo Key ngẫu nhiên
                string sessionKey = GenerateRandomKey(32);

                // B. Mã hóa Ghi chú bằng Key đó (AES)
                string cipherText = EncryptAES(plainText, sessionKey);

                // C. Mã hóa Key đó bằng RSA Public Key
                byte[] keyBytes = Encoding.UTF8.GetBytes(sessionKey);
                byte[] encryptedKeyBytes = rsa.Encrypt(keyBytes, false);
                string encryptedKey = Convert.ToBase64String(encryptedKeyBytes);

                // D. Lưu xuống DB
                using (OracleConnection conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = "UPDATE SINHVIEN SET GHICHU_MAT = :gc, KHOA_GIAIMA = :key WHERE MASV = :msv";
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add("gc", cipherText);
                        cmd.Parameters.Add("key", encryptedKey);
                        cmd.Parameters.Add("msv", maSV);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            MessageBox.Show("Đã lưu Ghi chú theo mô hình Mã hóa Lai!");

                            LoadData();
                        }
                        else
                        {
                            MessageBox.Show("Không tìm thấy sinh viên! Vui lòng dùng 'EncryptSave' để tạo sinh viên trước.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi Hybrid: " + ex.Message);
            }
        }

        // --- CÁC HÀM HỖ TRỢ ---
        private string GenerateRandomKey(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string EncryptAES(string text, string key)
        {
            using (Aes aes = Aes.Create())
            {
                using (MD5 md5 = MD5.Create())
                {
                    aes.Key = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
                    aes.IV = new byte[16];
                }
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs)) { sw.Write(text); }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private string DecryptAES(string cipherText, string key)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    using (MD5 md5 = MD5.Create())
                    {
                        aes.Key = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
                        aes.IV = new byte[16];
                    }
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs)) { return sr.ReadToEnd(); }
                        }
                    }
                }
            }
            catch { return "Lỗi giải mã Hybrid!"; }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                // 1. Đóng Form hiện tại
                this.Hide();

                // 2. Mở lại Form Login mới
                frmLogin login = new frmLogin();
                login.ShowDialog();

                // 3. Đóng hoàn toàn Form quản lý sau khi Form Login đóng (để giải phóng tài nguyên)
                this.Close();
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaSV.Text))
            {
                MessageBox.Show("Vui lòng chọn sinh viên cần xóa!");
                return;
            }

            DialogResult dr = MessageBox.Show("Bạn có chắc chắn muốn xóa sinh viên này?\nHành động này sẽ được ghi lại trong nhật ký hệ thống (Audit).",
                                              "Cảnh báo Xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (dr == DialogResult.Yes)
            {
                try
                {
                    using (OracleConnection conn = new OracleConnection(ConnectionString))
                    {
                        conn.Open();
                        string sql = "DELETE FROM SINHVIEN WHERE MASV = :masv";

                        using (OracleCommand cmd = new OracleCommand(sql, conn))
                        {
                            cmd.BindByName = true;
                            cmd.Parameters.Add("masv", txtMaSV.Text);

                            int rows = cmd.ExecuteNonQuery();
                            if (rows > 0)
                            {
                                MessageBox.Show("Đã xóa thành công!", "Thông báo");
                                LoadData();     // Load lại bảng
                                ClearInputs();  // Xóa trắng ô nhập
                            }
                            else
                            {
                                MessageBox.Show("Không tìm thấy sinh viên để xóa.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xóa: " + ex.Message);
                }
            }
        }
    }
}