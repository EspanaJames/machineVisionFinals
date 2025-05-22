using AForge.Controls;
using AForge.Video;
using AForge.Video.DirectShow;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Linq;

namespace Espana_Finals
{
    public partial class attendancePage : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private CascadeClassifier faceDetector;
        private bool isCameraRunning = false;
        private int frameSkip = 0;
        private DataBase db = new DataBase();
        private Dictionary<string, Mat> studentFaces = new Dictionary<string, Mat>();
        private Dictionary<string, string> studentNames = new Dictionary<string, string>();
        private HashSet<string> recentRecognitions = new HashSet<string>();
        private Dictionary<string, DateTime> lastRecognitionTimes = new Dictionary<string, DateTime>();

        public attendancePage()
        {
            InitializeComponent();
            machineVisionBox.Hide();
            var cascadeBytes = Espana_Finals.Resource1.haarcascade_frontalface_default;
            var cascadePath = Path.GetTempFileName();
            File.WriteAllBytes(cascadePath, cascadeBytes);
            faceDetector = new CascadeClassifier(cascadePath);

            LoadStudentImages();
        }

        private void LoadStudentImages()
        {
            try
            {
                if (db.OpenSQLConnection())
                {
                    string query = "SELECT idCard, firstName, lastName, picture FROM students";
                    MySqlCommand cmd = new MySqlCommand(query, db.mySqlConnection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string idCard = reader.GetString("idCard");
                            string fullName = reader.GetString("firstName") + " " + reader.GetString("lastName");
                            byte[] imgBytes = (byte[])reader["picture"];

                            using (var ms = new MemoryStream(imgBytes))
                            using (var bmp = new Bitmap(ms))
                            {
                                Mat faceMat = BitmapConverter.ToMat(bmp);
                                Cv2.CvtColor(faceMat, faceMat, ColorConversionCodes.BGR2GRAY);
                                Cv2.EqualizeHist(faceMat, faceMat);
                                Cv2.Resize(faceMat, faceMat, new OpenCvSharp.Size(100, 100));
                                studentFaces[idCard] = faceMat;
                                studentNames[idCard] = fullName;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading student images: " + ex.Message);
            }
            finally
            {
                db.CloseSQLConnection();
            }
        }

        private void siticoneButton1_Click(object sender, EventArgs e)
        {
            machineVisionBox.Show();
            userPictureBox.Hide();
            attendanceBox.Hide();

            if (!isCameraRunning)
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                {
                    MessageBox.Show("No camera found.");
                    return;
                }

                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(VideoSource_NewFrame);
                videoSource.Start();
                isCameraRunning = true;
            }
            else
            {
                if (videoSource != null && videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                    videoSource.WaitForStop();
                }

                isCameraRunning = false;
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap rawFrame = (Bitmap)eventArgs.Frame.Clone();
            Mat mat = BitmapConverter.ToMat(rawFrame);
            Mat gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

            Rect[] faces = faceDetector.DetectMultiScale(gray, 1.1, 4);

            foreach (var face in faces)
            {
                Mat faceROI = new Mat(gray, face);
                Cv2.Resize(faceROI, faceROI, new OpenCvSharp.Size(100, 100));

                string matchedId = null;
                double minDistance = double.MaxValue;

                foreach (var kv in studentFaces)
                {
                    double dist = Cv2.Norm(faceROI, kv.Value);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        matchedId = kv.Key;
                    }
                }

                string displayName = "Unknown";
                if (minDistance < 2500 && matchedId != null)
                {
                    displayName = studentNames[matchedId];

                    if (!lastRecognitionTimes.ContainsKey(matchedId) || (DateTime.Now - lastRecognitionTimes[matchedId]).TotalSeconds > 5)
                    {
                        RecordAttendance(matchedId);
                        lastRecognitionTimes[matchedId] = DateTime.Now;
                    }
                }

                Cv2.Rectangle(mat, face, Scalar.Red, 2);
                Cv2.PutText(mat, displayName, new OpenCvSharp.Point(face.X, face.Y - 5), HersheyFonts.HersheySimplex, 0.8, Scalar.Green, 2);
            }

            Bitmap processedFrame = BitmapConverter.ToBitmap(mat);

            if (machineVisionBox.InvokeRequired)
            {
                machineVisionBox.Invoke(new MethodInvoker(() =>
                {
                    if (machineVisionBox.Image != null)
                        machineVisionBox.Image.Dispose();
                    machineVisionBox.Image = processedFrame;
                }));
            }
            else
            {
                if (machineVisionBox.Image != null)
                    machineVisionBox.Image.Dispose();
                machineVisionBox.Image = processedFrame;
            }

            rawFrame.Dispose();
        }

        private void RecordAttendance(string idCard)
        {
            string insertQuery = "INSERT INTO attendance (idCard, timeIn) VALUES (@idCard, @timeIn)";
            var parameters = new Dictionary<string, object>
            {
                {"@idCard", idCard },
                {"@timeIn", DateTime.Now }
            };

            db.ExecuteQueryWithParameters(insertQuery, parameters);
        }

        private void loginPage_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
        }

        private void siticoneButton2_Click(object sender, EventArgs e) { }
        private void siticoneButton3_Click(object sender, EventArgs e) { }
    }
}
