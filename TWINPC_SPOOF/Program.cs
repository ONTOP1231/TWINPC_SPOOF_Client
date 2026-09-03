#nullable disable
#pragma warning disable CS0414
using System;
using System.Drawing;
using System.Management;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace TWINPC_SPOOF
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!IsAdministrator())
            {
                MessageBox.Show("يرجى تشغيل البرنامج كمسؤول (Run as Administrator).", 
                                "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Application.Run(new MainForm());
        }

        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public class MainForm : Form
    {
        private TabControl tabControl;
        private TabPage tabDashboard, tabSysInfo, tabSettings;

        private TextBox txtKey, txtServerUrl;
        private Button btnActivate;
        private Label lblStatus;
        private RichTextBox txtSysDetails;

        private bool isActivated = false;
        private static readonly HttpClient httpClient = new HttpClient();

        public MainForm()
        {
            this.Text = "TWINPC SPOOF - Client";
            this.Size = new Size(600, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            InitializeUI();
        }

        private void InitializeUI()
        {
            tabControl = new TabControl { Dock = DockStyle.Fill };

            // 1. Dashboard Tab
            tabDashboard = new TabPage("Dashboard");
            Label lblKey = new Label { Text = "أدخل مفتاح الترخيص:", Location = new Point(20, 20), AutoSize = true };
            txtKey = new TextBox { Location = new Point(20, 50), Width = 350 };
            btnActivate = new Button { Text = "تفعيل", Location = new Point(380, 48), Width = 100 };
            btnActivate.Click += BtnActivate_Click;
            lblStatus = new Label { Text = "الحالة: بانتظار التفعيل", Location = new Point(20, 100), AutoSize = true, ForeColor = Color.Red };

            tabDashboard.Controls.Add(lblKey);
            tabDashboard.Controls.Add(txtKey);
            tabDashboard.Controls.Add(btnActivate);
            tabDashboard.Controls.Add(lblStatus);

            // 2. System Info Tab
            tabSysInfo = new TabPage("System Info");
            txtSysDetails = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White };
            tabSysInfo.Controls.Add(txtSysDetails);
            tabSysInfo.Enter += (s, e) => LoadSystemInfo();

            // 3. Settings Tab
            tabSettings = new TabPage("Settings");
            Label lblUrl = new Label { Text = "رابط السيرفر:", Location = new Point(20, 20), AutoSize = true };
            txtServerUrl = new TextBox { Text = "http://localhost:3000", Location = new Point(20, 50), Width = 350 };
            tabSettings.Controls.Add(lblUrl);
            tabSettings.Controls.Add(txtServerUrl);

            // إضافة التبويبات
            tabControl.TabPages.Add(tabDashboard);
            tabControl.TabPages.Add(tabSysInfo);
            tabControl.TabPages.Add(tabSettings);

            this.Controls.Add(tabControl);
        }

        private async void BtnActivate_Click(object sender, EventArgs e)
        {
            string key = txtKey.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("يرجى إدخال المفتاح.");
                return;
            }

            string hwid = GetHWID();
            string serverUrl = txtServerUrl.Text.Trim() + "/api/client/activate";

            try
            {
                var jsonContent = $"{{\"key_code\": \"{key}\", \"hwid\": \"{hwid}\"}}";
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(serverUrl, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    isActivated = true;
                    lblStatus.Text = "الحالة: تم التفعيل بنجاح!";
                    lblStatus.ForeColor = Color.Green;
                    MessageBox.Show("تم التفعيل بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblStatus.Text = "الحالة: فشل التفعيل";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show("فشل التفعيل: " + responseString);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الاتصال: " + ex.Message);
            }
        }

        private void LoadSystemInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== معلومات الجهاز ===");
            sb.AppendLine($"اسم الجهاز: {Environment.MachineName}");
            sb.AppendLine($"المستخدم: {Environment.UserName}");
            sb.AppendLine($"النظام: {Environment.OSVersion}");
            sb.AppendLine($"HWID: {GetHWID()}");
            txtSysDetails.Text = sb.ToString();
        }

        private string GetHWID()
        {
            string cpuId = GetWmiProperty("Win32_Processor", "ProcessorId");
            string motherboardId = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(cpuId + motherboardId));
        }

        private string GetWmiProperty(string wmiClass, string property)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj[property]?.ToString()?.Trim() ?? "N/A";
                    }
                }
            }
            catch { }
            return "N/A";
        }
    }
}