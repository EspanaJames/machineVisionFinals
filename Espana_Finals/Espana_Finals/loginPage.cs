using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Espana_Finals
{
    public partial class loginPage : Form
    {
        private Timer moveTimer = new Timer();
        private Stopwatch stopwatch = new Stopwatch();
        private int startX;
        private int targetX;
        private int duration = 500;
        private bool isMovedRight = false;
        private bool isAnimating = false;
        public loginPage()
        {
            moveTimer.Interval = 15;
            InitializeComponent();
            moveTimer.Tick += MoveTimer_Tick;
        }
        private void MoveTimer_Tick(object sender, EventArgs e)
        {
            double elapsed = stopwatch.Elapsed.TotalMilliseconds;
            double t = elapsed / duration;

            if (t >= 1)
            {
                siticoneAdvancedPanel2.Location = new Point(targetX, siticoneAdvancedPanel2.Location.Y);
                moveTimer.Stop();
                stopwatch.Stop();
                isAnimating = false;
                return;
            }

            double eased = EaseOutBounce(t);
            int newX = startX + (int)((targetX - startX) * eased);
            siticoneAdvancedPanel2.Location = new Point(newX, siticoneAdvancedPanel2.Location.Y);
        }
        private double EaseOutBounce(double t)
        {
            if (t < (1 / 2.75))
            {
                return 7.5625 * t * t;
            }
            else if (t < (2 / 2.75))
            {
                t -= (1.5 / 2.75);
                return 7.5625 * t * t + 0.75;
            }
            else if (t < (2.5 / 2.75))
            {
                t -= (2.25 / 2.75);
                return 7.5625 * t * t + 0.9375;
            }
            else
            {
                t -= (2.625 / 2.75);
                return 7.5625 * t * t + 0.984375;
            }
        }
        private void siticoneButton1_Click(object sender, EventArgs e)
        {
            if (isAnimating)
                return;

            startX = siticoneAdvancedPanel2.Location.X;

            if (!isMovedRight)
            {
                targetX = startX + 760;
                siticoneButton1.Text = "SIGN UP";
            }
            else
            {
                targetX = startX - 760;
                siticoneButton1.Text = "LOGIN";
            }

            isMovedRight = !isMovedRight;
            stopwatch.Restart();
            isAnimating = true;
            moveTimer.Start();
        }

        private async void siticoneButton2_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            lblError.Visible = false;

            string usernameInput = userAdminBox.Text.Trim();
            string passwordInput = passwordAdminBox.Text.Trim();

            if (string.IsNullOrEmpty(usernameInput) || string.IsNullOrEmpty(passwordInput))
            {
                lblError.Text = "Please enter both username and password.";
                lblError.Visible = true;
                lblError.Left = (this.ClientSize.Width - lblError.Width) / 4;
                return;
            }

            DataBase db = new DataBase();

            try
            {
                db.OpenSQLConnection();

                string query = "SELECT password FROM admindb WHERE username = @username LIMIT 1";
                using (var cmd = new MySqlCommand(query, db.mySqlConnection))
                {
                    cmd.Parameters.Add("@username", MySqlDbType.VarChar).Value = usernameInput;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string passwordFromDb = reader["password"].ToString();

                            if (passwordFromDb == passwordInput)
                            {
                                landingPage home = new landingPage();
                                home.Show();
                                this.Hide();
                            }
                            else
                            {
                                lblError.Text = "Incorrect password.";
                                lblError.Visible = true;
                                lblError.Left = (this.ClientSize.Width - lblError.Width) / 4;
                            }
                        }
                        else
                        {
                            lblError.Text = "Username not found.";
                            lblError.Visible = true;
                            lblError.Left = (this.ClientSize.Width - lblError.Width) / 4;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Database error: " + ex.Message;
                lblError.Visible = true;
                lblError.Left = (this.ClientSize.Width - lblError.Width) / 4;
            }
            finally
            {
                db.CloseSQLConnection();
            }
        }

        private void siticoneCheckBox1_Click(object sender, EventArgs e)
        {
            passwordAdminBox.UseSystemPasswordChar = siticoneCheckBox1.Checked;
        }
    }
}
