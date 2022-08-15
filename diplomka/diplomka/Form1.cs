using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using DiplomkaML.Model;
using RestSharp;
using Npgsql;

namespace diplomka
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
            //ModelInput sampleData = new ModelInput()
            //{
            //    ImageSource = @"C:\Users\black\source\repos\diplomka\Datasets\cars\back0.jpg",
            //};
            //var predictionResult = ConsumeModel.Predict(sampleData);
            //textBox1.Text = $"ImageSource: {sampleData.ImageSource}";
            //Console.WriteLine($"ImageSource: {sampleData.ImageSource}");
            
            //Console.WriteLine("=============== End of process, hit any key to finish ===============");
            //Console.ReadKey();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            
            OpenFileDialog ofd = new OpenFileDialog();
            //ofd.InitialDirectory = "c:\\";
            ofd.Filter = "Images Files(*.JPG;*.PNG;*.JPEG)|*.JPG;*.PNG;*.JPEG|All files (*.*)|*.*";
            ofd.Multiselect = true;
            //ofd.RestoreDirectory = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    foreach (var path in ofd.FileNames)
                    {
                        try
                        {
                            ModelInput sampleData = new ModelInput()
                            {
                                ImageSource = path,
                            };

                            var result = ConsumeModel.Predict(sampleData);
                            string newpath = @"C:\Users\black\source\repos\diplomka\diplomka\images\" + Path.GetRandomFileName() + Path.GetExtension(path);
                            File.Copy(path, newpath);
                            int id_image, id_class;
                            var sql = $"INSERT INTO images(path) VALUES('{newpath}') RETURNING id";
                            using (var connection = new NpgsqlConnection("Server=127.0.0.1;User Id=postgres;Password=12345;Port=5432;Database=mlsearch;"))
                            {

                                using (var cmd = new NpgsqlCommand(sql, connection))
                                {
                                    connection.Open();
                                    cmd.Prepare();
                                    id_image = Convert.ToInt32(cmd.ExecuteScalar());
                                }
                            }
                            sql = $"INSERT INTO classes(class) VALUES(\'{result.Prediction}\') RETURNING id";
                            using (var connection = new NpgsqlConnection("Server=127.0.0.1;User Id=postgres;Password=12345;Port=5432;Database=mlsearch;"))
                            {

                                using (var cmd = new NpgsqlCommand(sql, connection))
                                {
                                    connection.Open();
                                   
                                    id_class = Convert.ToInt32(cmd.ExecuteScalar());
                                }
                            }
                            sql = $"INSERT INTO image_class(id_image, id_class) VALUES({id_image},{id_class})";
                            using (var connection = new NpgsqlConnection("Server=127.0.0.1;User Id=postgres;Password=12345;Port=5432;Database=mlsearch;"))
                            {

                                using (var cmd = new NpgsqlCommand(sql, connection))
                                {
                                    connection.Open();
                                    
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }


                }
                catch
                {
                    MessageBox.Show("Невозможно открыть файл", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var FilePath = string.Empty;
            OpenFileDialog ofd = new OpenFileDialog();
            //ofd.InitialDirectory = "c:\\";
            ofd.Filter = "Images Files(*.JPG;*.PNG;*.JPEG)|*.JPG;*.PNG;*.JPEG|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK) 
            {
                FilePath = ofd.FileName;
                ModelInput sampleData = new ModelInput()
                {
                    ImageSource = FilePath,
                };

                var result = ConsumeModel.Predict(sampleData);
                var sql = $"SELECT images.path, classes.class FROM public.image_class INNER JOIN images ON id_image = images.id INNER JOIN classes ON id_class=classes.id where classes.class = \'{result.Prediction}\';";
                using (var connection = new NpgsqlConnection("Server=127.0.0.1;User Id=postgres;Password=12345;Port=5432;Database=mlsearch;"))
                {

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        connection.Open();
                        var reader = cmd.ExecuteReader();
                        if (result.Prediction == "cats")
                        { label2.Text = "Коты"; }
                        else if (result.Prediction == "cars")
                        { label2.Text = "Машины"; }
                        else if (result.Prediction == "dogs")
                        { label2.Text = "Собаки"; }
                        else if (result.Prediction == "flowers")
                        { label2.Text = "Цветы"; }
                        while (reader.Read())
                        {
                            
                            flowLayoutPanel1.Controls.Add(new PictureBox()
                            {
                                Image = new Bitmap(reader["path"].ToString()),SizeMode=PictureBoxSizeMode.Zoom, Size = new System.Drawing.Size(140,140)
                            }) ;
                        }
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = "c:\\";
            ofd.Filter = "Images Files(*.JPG;*.PNG;*.JPEG)|*.JPG;*.PNG;*.JPEG|All files (*.*)|*.*";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                ModelInput sampleData = new ModelInput()
                {
                    ImageSource = ofd.FileName,
                };
                var result = ConsumeModel.Predict(sampleData);
                flowLayoutPanel1.Controls.Add(new PictureBox()
                {
                    Image = new Bitmap(ofd.FileName),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new System.Drawing.Size(950, 400)
                });
                if (result.Prediction == "cats")
                { label2.Text = "Коты"; }
                else if (result.Prediction == "cars")
                { label2.Text = "Машины"; }
                else if (result.Prediction == "dogs")
                { label2.Text = "Собаки"; }
                else if (result.Prediction == "flowers")
                { label2.Text = "Цветы"; }
            }
        }

    }
}
