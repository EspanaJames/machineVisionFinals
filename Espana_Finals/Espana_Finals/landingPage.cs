using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Espana_Finals
{
    public partial class landingPage : Form
    {
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
        public landingPage()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 30, 30));
        }
        

        private void siticoneButton2_Click(object sender, EventArgs e)
        {
          
            siticoneTabControl1.SelectedIndex = 3;
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void siticoneButton1_Click(object sender, EventArgs e)
        {
            siticoneTabControl1.SelectedIndex = 0;
        }

        private void siticoneButton3_Click(object sender, EventArgs e)
        {
            siticoneTabControl1.SelectedIndex = 1;
        }

        private void siticoneButton4_Click(object sender, EventArgs e)
        {
            siticoneTabControl1.SelectedIndex = 2;
        }

        private void siticoneButton5_Click(object sender, EventArgs e)
        {
            string eventId = siticoneTextBox1.Text.Trim();
            string eventName = siticoneTextBox2.Text.Trim();
            string eventDescription = siticoneTextBox4.Text.Trim();
            DateTime timeIn = (DateTime)siticoneDateTimePicker1.Value;
            DateTime timeOut = (DateTime)siticoneDateTimePicker2.Value;

            // Validate input fields
            List<string> missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(eventId)) missingFields.Add("Event ID");
            if (string.IsNullOrWhiteSpace(eventName)) missingFields.Add("Event Name");
            if (string.IsNullOrWhiteSpace(eventDescription)) missingFields.Add("Event Description");

            if (missingFields.Count > 0)
            {
                MessageBox.Show("Please provide the following:\n" + string.Join("\n", missingFields), "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (timeIn >= timeOut)
            {
                MessageBox.Show("Start time must be earlier than end time.", "Invalid Time", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataBase db = new DataBase();

            try
            {
                if (db.OpenSQLConnection())
                {
                    string insertQuery = @"INSERT INTO event (eventId, eventName, eventStart, eventEnd, eventDescription)
                                   VALUES (@eventId, @eventName, @eventStart, @eventEnd, @eventDescription)";
                    Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@eventId", eventId },
                { "@eventName", eventName },
                { "@eventStart", timeIn },
                { "@eventEnd", timeOut },
                { "@eventDescription", eventDescription }
            };

                    if (db.ExecuteQueryWithParameters(insertQuery, parameters))
                    {
                        MessageBox.Show("Event successfully added.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to add event.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Duplicate entry
                {
                    MessageBox.Show("An event with the same ID and name already exists.", "Duplicate Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                db.CloseSQLConnection();
            }
        }

        private void siticoneButton6_Click(object sender, EventArgs e)
        {
            string eventId = siticoneTextBox1.Text.Trim();

            if (string.IsNullOrEmpty(eventId))
            {
                MessageBox.Show("Please enter an Event ID.");
                return;
            }

            DataBase db = new DataBase();

            try
            {
                if (db.OpenSQLConnection())
                {
                    string checkQuery = "SELECT COUNT(*) FROM event WHERE eventId = @eventId";
                    using (MySqlCommand cmd = new MySqlCommand(checkQuery, db.mySqlConnection))
                    {
                        cmd.Parameters.AddWithValue("@eventId", eventId);
                        object result = cmd.ExecuteScalar();
                        int count = Convert.ToInt32(result);
                        if (count == 0)
                        {
                            MessageBox.Show("Event ID not found.");
                            return;
                        }
                    }
                }

                string deleteQuery = "DELETE FROM event WHERE eventId = @eventId";
                Dictionary<string, object> parameters = new Dictionary<string, object>
                {
                    { "@eventId", eventId }
                };

                if (db.ExecuteQueryWithParameters(deleteQuery, parameters))
                {
                    MessageBox.Show("Event successfully deleted.");
                }
                else
                {
                    MessageBox.Show("Failed to delete event.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                db.CloseSQLConnection();
            }
        }

        private void siticoneButton7_Click(object sender, EventArgs e)
        {
            string eventId = siticoneTextBox3.Text.Trim();
            DataBase db = new DataBase();
            try
            {
                using (var reader = db.ExecuteQuery($"SELECT eventName FROM event WHERE eventId = '{eventId}'"))
                {
                    if (reader.Read())
                    {
                        string eventName = reader.GetString("eventName");
                        new populateDatabase(eventName).Show();
                    }
                    else
                    {
                        MessageBox.Show("Event ID not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                db.CloseSQLConnection();
            }
        }

        private void siticoneButton8_Click(object sender, EventArgs e)
        {
            string eventId = siticoneTextBox5.Text.Trim();
            DataBase db = new DataBase();
            try
            {
                using (var reader = db.ExecuteQuery($"SELECT eventName FROM event WHERE eventId = '{eventId}'"))
                {
                    if (reader.Read())
                    {
                        string eventName1 = reader.GetString("eventName");
                        attendancePage call = new attendancePage(eventName1);
                        call.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Event ID not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                db.CloseSQLConnection();
            }
        }
    }
}
