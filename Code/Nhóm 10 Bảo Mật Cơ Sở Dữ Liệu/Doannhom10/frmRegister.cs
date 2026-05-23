using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Oracle.ManagedDataAccess.Client; // Thư viện kết nối Oracle

namespace Doannhom10
{
    public partial class frmRegister : Form
    {
        // QUAN TRỌNG: Phải dùng User QLSV (Admin) để có quyền TẠO USER CON
        // Kiểm tra lại connection string của máy bro
        private const string AdminConnectionString = "Data Source=localhost:1521/orcl21;User Id=qlsv;Password=123;";

        public frmRegister()
        {
            InitializeComponent();
            CenterToScreen();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra nhập liệu
            if (string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text) ||
                string.IsNullOrWhiteSpace(txtRetypePass.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Kiểm tra mật khẩu nhập lại
            if (txtPassword.Text != txtRetypePass.Text)
            {
                MessageBox.Show("Mật khẩu nhập lại không khớp.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text.Trim();

            try
            {
                using (OracleConnection conn = new OracleConnection(AdminConnectionString))
                {
                    conn.Open();

                    // 🔥 [FIX LỖI ORA-65096] QUAN TRỌNG NHẤT 🔥
                    // Lệnh này ép Oracle cho phép tạo user thường (không cần C##)
                    using (OracleCommand cmdConfig = new OracleCommand("ALTER SESSION SET \"_ORACLE_SCRIPT\" = TRUE", conn))
                    {
                        cmdConfig.ExecuteNonQuery();
                    }
                    // ---------------------------------------------

                    // 3. TẠO USER ORACLE THẬT
                    string sqlCreateUser = $"CREATE USER {user} IDENTIFIED BY {pass}";

                    using (OracleCommand cmd = new OracleCommand(sqlCreateUser, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 4. CẤP QUYỀN ĐĂNG NHẬP
                    string sqlGrantSession = $"GRANT CREATE SESSION TO {user}";
                    using (OracleCommand cmd = new OracleCommand(sqlGrantSession, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 5. CẤP QUYỀN XEM DỮ LIỆU
                    string sqlGrantSelect = $"GRANT SELECT ON QLSV.SINHVIEN TO {user}";
                    using (OracleCommand cmd = new OracleCommand(sqlGrantSelect, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show($"Đăng ký thành công User: {user}!\nBạn có thể đăng nhập ngay bây giờ.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.Close(); // Đóng form quay về Login
                }
            }
            catch (OracleException ex)
            {
                // Bắt lỗi trùng tên
                if (ex.Number == 1920)
                {
                    MessageBox.Show("Tên tài khoản này đã tồn tại!", "Trùng tên", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Lỗi Oracle: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}