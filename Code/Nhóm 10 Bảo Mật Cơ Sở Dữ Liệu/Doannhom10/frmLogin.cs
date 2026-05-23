using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Oracle.ManagedDataAccess.Client; // Thư viện Oracle

namespace Doannhom10
{
    public partial class frmLogin : Form
    {
        public frmLogin()
        {
            InitializeComponent();
            CenterToScreen();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra nhập liệu
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Vui lòng nhập Username và Password.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text.Trim();

            // 2. [QUAN TRỌNG] Tạo chuỗi kết nối động
            // Thay vì dùng user cố định, ta dùng chính user/pass người dùng nhập vào.
            // Nếu kết nối thành công => User/Pass đúng => Oracle tự động xác thực.

            // LƯU Ý: Sửa lại "localhost:1521/orcl21" nếu máy bro cấu hình khác.
            string connectionString = $"Data Source=localhost:1521/orcl21;User Id={user};Password={pass};";

            try
            {
                // Thử mở kết nối
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open(); // Nếu sai Pass, Oracle sẽ ném lỗi ORA-01017 ngay tại dòng này
                }

                // --- NẾU CODE CHẠY ĐẾN ĐÂY NGHĨA LÀ ĐĂNG NHẬP THÀNH CÔNG ---

                MessageBox.Show("Đăng nhập thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Hide();

                // 3. Mở Form Quản lý và TRUYỀN USER/PASS qua
                // (Để Form quản lý dùng đúng user này kết nối và lấy dữ liệu theo phân quyền VPD)
                frmStudentManagement f = new frmStudentManagement(user, pass);
                f.ShowDialog();

                this.Close();
            }
            catch (OracleException ex)
            {
                // Bắt lỗi: ORA-01017: invalid username/password; logon denied
                if (ex.Number == 1017)
                {
                    MessageBox.Show("Sai tên đăng nhập hoặc mật khẩu Oracle!", "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}