using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Client
{
    public partial class Client : Form
    {
        public Client()
        {
            InitializeComponent();
        }

        // Phương thức nhận khóa công khai từ server
        private void ReceivePublicKeyFromServer()
        {
            using (TcpClient client = new TcpClient("127.0.0.1", 5000))
            using (NetworkStream stream = client.GetStream())
            {
                // Đọc modulus từ server
                byte[] modulus = new byte[256]; // Giới hạn kích thước tối đa của modulus
                int bytesRead = stream.Read(modulus, 0, modulus.Length);

                // Đọc exponent từ server
                byte[] exponent = new byte[256]; // Giới hạn kích thước tối đa của exponent
                bytesRead = stream.Read(exponent, 0, exponent.Length);

                // Kiểm tra xem có nhận đủ dữ liệu không
                if (bytesRead == 0)
                {
                    MessageBox.Show("Không nhận được dữ liệu khóa công khai từ server.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Chuyển modulus và exponent sang chuỗi base64 và hiển thị trong txtPublicKey
                txtPublicKey.Invoke(new Action(() =>
                {
                    txtPublicKey.Text = Convert.ToBase64String(modulus, 0, bytesRead) + "\n" + Convert.ToBase64String(exponent, 0, bytesRead);
                }));
            }
        }


        // Phương thức gửi thông điệp mã hóa đến server
        private void btn_send(object sender, EventArgs e)
        {
            string message = txtMessage.Text;

            // Kiểm tra nếu txtPublicKey có ít nhất hai dòng (Modulus và Exponent)
            if (txtPublicKey.Lines.Length < 2)
            {
                MessageBox.Show("Vui lòng nhập đủ modulus và exponent của khóa công khai.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Thiết lập RSA từ khóa công khai nhập từ server
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
                {
                    RSAParameters publicKeyParams = new RSAParameters
                    {
                        Modulus = Convert.FromBase64String(txtPublicKey.Lines[0]),
                        Exponent = Convert.FromBase64String(txtPublicKey.Lines[1])
                    };
                    rsa.ImportParameters(publicKeyParams);

                    // Mã hóa thông điệp
                    byte[] dataToEncrypt = Encoding.UTF8.GetBytes(message);
                    byte[] encryptedData = rsa.Encrypt(dataToEncrypt, false);

                    string encryptedMessage = Convert.ToBase64String(encryptedData);
                    txtEncrypted.Invoke(new Action(() =>
                    {
                        txtEncrypted.Text = encryptedMessage;
                    }));

                    // Gửi dữ liệu mã hóa tới server
                    using (TcpClient client = new TcpClient("127.0.0.1", 5000))
                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.Write(encryptedData, 0, encryptedData.Length);
                        MessageBox.Show("Thông điệp đã được gửi đi.");
                    }
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Định dạng khóa công khai không hợp lệ. Vui lòng kiểm tra lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Đã xảy ra lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Gọi phương thức này để nhận khóa công khai từ server khi form được tải
        private void Form1_Load(object sender, EventArgs e)
        {
            ReceivePublicKeyFromServer();
        }
    }
}
