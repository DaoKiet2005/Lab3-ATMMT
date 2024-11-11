using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    public partial class Server : Form
    {
        private RSACryptoServiceProvider rsa;
        private TcpListener listener;

        public Server()
        {
            InitializeComponent();
            rsa = new RSACryptoServiceProvider(2048);
            listener = new TcpListener(IPAddress.Any, 5000);
        }

        private void btn_start(object sender, EventArgs e)
        {
            // Lấy khóa công khai từ RSA
            RSAParameters publicKeyParams = rsa.ExportParameters(false);

            // Hiển thị khóa công khai trong TextBox để kiểm tra
            txtPublicKey.Text = Convert.ToBase64String(publicKeyParams.Modulus) + "\n" + Convert.ToBase64String(publicKeyParams.Exponent);

            // Bắt đầu lắng nghe kết nối từ client trong một thread riêng biệt
            listener.Start();
            MessageBox.Show("Server đã sẵn sàng nhận kết nối");

            Thread listenerThread = new Thread(() => ListenForClients(publicKeyParams));
            listenerThread.Start();
        }

        private void ListenForClients(RSAParameters publicKeyParams)
        {
            while (true)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                {
                    // Gửi modulus và exponent đến client
                    stream.Write(publicKeyParams.Modulus, 0, publicKeyParams.Modulus.Length);
                    stream.Write(publicKeyParams.Exponent, 0, publicKeyParams.Exponent.Length);

                    // Nhận dữ liệu mã hóa từ client và giải mã
                    byte[] buffer = new byte[256];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    byte[] encryptedData = new byte[bytesRead];
                    Array.Copy(buffer, encryptedData, bytesRead);

                    string encryptedMessage = Convert.ToBase64String(encryptedData);
                    txtEncrypted.Invoke(new Action(() =>
                    {
                        txtEncrypted.Text = encryptedMessage;
                    }));

                    byte[] decryptedData = rsa.Decrypt(encryptedData, false);
                    string message = Encoding.UTF8.GetString(decryptedData);

                    // Hiển thị thông điệp đã giải mã
                    txtDecryptedMessage.Invoke(new Action(() =>
                    {
                        txtDecryptedMessage.Text = message;
                    }));
                }
            }
        }
    }
}
