using AForge.Controls;
using AForge.Video;
using AForge.Video.DirectShow;
using MySql.Data.MySqlClient;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Espana_Finals
{
    public partial class attendancePage : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private CascadeClassifier faceDetector;
        private bool isCameraRunning = false;
        private DataBase db = new DataBase();
        private ConcurrentDictionary<string, (Mat Face, Mat Histogram)> studentData = new ConcurrentDictionary<string, (Mat, Mat)>();
        private Dictionary<string, (string FirstName, string LastName, string EventName)> studentNames = new Dictionary<string, (string, string, string)>();
        private string eventNameHolder;
        private readonly object frameLock = new object();

        public attendancePage(string eventName)
        {
            InitializeComponent();
            var cascadeBytes = Espana_Finals.Resource1.frontalFile;
            var cascadePath = Path.GetTempFileName();
            File.WriteAllBytes(cascadePath, cascadeBytes);
            faceDetector = new CascadeClassifier(cascadePath);
            eventNameHolder = eventName;
            siticoneLabel4.Text = eventNameHolder;
            LoadStudentImagesAsync();
        }

        private async void LoadStudentImagesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (db.OpenSQLConnection())
                    {
                        string query = "SELECT idCard, firstName, lastName, eventName, picture FROM students WHERE eventName = @eventName";
                        using (var cmd = new MySqlCommand(query, db.mySqlConnection))
                        {
                            cmd.Parameters.AddWithValue("@eventName", eventNameHolder);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string idCard = reader.GetString("idCard");
                                    string firstName = reader.GetString("firstName");
                                    string lastName = reader.GetString("lastName");
                                    string eventName = reader.GetString("eventName");
                                    byte[] imgBytes = (byte[])reader["picture"];

                                    using (var ms = new MemoryStream(imgBytes))
                                    using (var bmp = new Bitmap(ms))
                                    {
                                        Mat imgMat = BitmapConverter.ToMat(bmp);
                                        Cv2.CvtColor(imgMat, imgMat, ColorConversionCodes.BGR2GRAY);
                                        Cv2.EqualizeHist(imgMat, imgMat);

                                        var faces = faceDetector.DetectMultiScale(imgMat, 1.1, 5);
                                        if (faces.Length > 0)
                                        {
                                            var largestFace = faces.OrderByDescending(f => f.Width * f.Height).First();
                                            Mat faceMat = new Mat(imgMat, largestFace);
                                            Cv2.Resize(faceMat, faceMat, new OpenCvSharp.Size(100, 100));

                                            Mat hist = GetHistogram(faceMat);
                                            studentData[idCard] = (faceMat.Clone(), hist);
                                            studentNames[idCard] = (firstName, lastName, eventName);
                                        }
                                    }
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
            });
        }

        private void siticoneButton1_Click(object sender, EventArgs e)
        {
            if (!isCameraRunning)
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0)
                {
                    MessageBox.Show("No camera found.");
                    return;
                }

                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += VideoSource_NewFrame;
                videoSource.Start();
                isCameraRunning = true;
            }
            else
            {
                StopCamera();
            }
        }

        private void StopCamera()
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
            isCameraRunning = false;
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (studentData.Count == 0) return;

            try
            {
                using (Bitmap rawFrame = (Bitmap)eventArgs.Frame.Clone())
                {
                    Mat mat = BitmapConverter.ToMat(rawFrame);
                    Mat gray = new Mat();
                    Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                    Cv2.EqualizeHist(gray, gray);

                    Rect[] faces = faceDetector.DetectMultiScale(gray, 1.1, 5);

                    foreach (var face in faces)
                    {
                        Mat faceROI = new Mat(gray, face);
                        Cv2.Resize(faceROI, faceROI, new OpenCvSharp.Size(100, 100));
                        Mat liveHist = GetHistogram(faceROI);

                        string matchedId = null;
                        double bestScore = double.MaxValue;
                        double scoreThreshold = 0.5;

                        foreach (var kv in studentData)
                        {
                            double score = Cv2.CompareHist(liveHist, kv.Value.Histogram, HistCompMethods.Chisqr);
                            if (score < bestScore)
                            {
                                bestScore = score;
                                matchedId = kv.Key;
                            }
                        }

                        if (matchedId != null && bestScore < scoreThreshold)
                        {
                            var (firstName, lastName, eventName) = studentNames[matchedId];

                            string insertQuery = "INSERT INTO attendance (idCard, firstName, lastName, timeIn, eventName) VALUES (@idCard, @firstName, @lastName, @timeIn, @eventName)";
                            var parameters = new Dictionary<string, object>
                            {
                                {"@idCard", matchedId},
                                {"@firstName", firstName},
                                {"@lastName", lastName},
                                {"@timeIn", DateTime.Now},
                                {"@eventName", eventNameHolder}
                            };
                            db.ExecuteQueryWithParameters(insertQuery, parameters);

                            Cv2.PutText(mat, $"{firstName} {lastName} - Timed In", new OpenCvSharp.Point(face.X, face.Y - 10),
                                HersheyFonts.HersheySimplex, 0.7, Scalar.Green, 2);
                        }
                        else
                        {
                            Cv2.PutText(mat, "Unknown", new OpenCvSharp.Point(face.X, face.Y - 10),
                                HersheyFonts.HersheySimplex, 0.7, Scalar.Red, 2);
                        }

                        Cv2.Rectangle(mat, face, Scalar.Red, 2);
                    }

                    Bitmap processedFrame = BitmapConverter.ToBitmap(mat);

                    if (machineVisionBox.InvokeRequired)
                    {
                        machineVisionBox.Invoke(new MethodInvoker(() =>
                        {
                            machineVisionBox.Image?.Dispose();
                            machineVisionBox.Image = processedFrame;
                        }));
                    }
                    else
                    {
                        machineVisionBox.Image?.Dispose();
                        machineVisionBox.Image = processedFrame;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in frame processing: " + ex.Message);
            }
        }

        private Mat GetHistogram(Mat image)
        {
            Mat hist = new Mat();
            Cv2.CalcHist(new[] { image }, new[] { 0 }, null, hist, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
            Cv2.Normalize(hist, hist);
            return hist;
        }

        private void siticoneButton4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Face detection and attendance logging are handled in real-time.");
        }

        private void loginPage_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopCamera();
        }

        private void siticoneButton2_Click(object sender, EventArgs e) { }
        private void siticoneButton3_Click(object sender, EventArgs e) { }
    }
}