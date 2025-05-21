using Microsoft.VisualBasic.Devices;
using System.Runtime.InteropServices;

namespace Espana_Finals
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer clock;
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // width of ellipse
            int nHeightEllipse // height of ellipse
        );
        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 30, 30));
            BarLoad();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void siticoneAdvancedPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void BarLoad()
        {
            siticonehProgressBar1.Value = 0;
            siticonehProgressBar1.Maximum = 100;
            clock = new System.Windows.Forms.Timer();
            clock.Interval = 150;
            clock.Tick += openLandingPage;
            clock.Start();
        }
        private void openLandingPage(object sender, EventArgs e)
        {
            if (siticonehProgressBar1.Value < siticonehProgressBar1.Maximum)
            {
                siticonehProgressBar1.Value += 5;
            }
            else
            {
                clock.Stop();
                loginPage call = new loginPage();
                call.Show();
                this.Hide();
            }
        }
        private void siticoneButton1_Click(object sender, EventArgs e)
        {

        }

        private void siticonehProgressBar1_Click(object sender, EventArgs e)
        {

        }

        private void siticoneShimmerLabel1_Click(object sender, EventArgs e)
        {

        }

        private void siticoneLabel1_Click(object sender, EventArgs e)
        {

        }
    }
}
