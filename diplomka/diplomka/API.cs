using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using RestSharp;
using Npgsql;
using ImageProcessor;
using ImageRecognitionModule;

namespace diplomka
{
    public class ApiClient
    {
        const string BaseUrl = "https://localhost:44311/api";

        readonly IRestClient _client;

        public ApiClient()
        {
            _client = new RestClient(BaseUrl);
        }

        public T Execute<T>(RestRequest request) where T : new()
        {
            var response = _client.Execute<T>(request);
            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response. Check inner details for more info.";
                var clientException = new Exception(message, response.ErrorException);
                throw clientException;
            }
            return response.Data;
        }

        public Response GetCheck(int num)
        {
            var request = new RestRequest("utility/{id}", Method.GET);
            request.RootElement = "Check";
            request.AddParameter("id", num, ParameterType.UrlSegment);
            return Execute<Response>(request);
        }

        public Response ProcessImage(byte[] image)
        {
            string path = string.Format(@"{0}.jpg", DateTime.Now.Ticks);
            // FileStream fs = new(path, FileMode.Open);
            //  if (!System.IO.File.Exists(path)) throw new Exception("Файл не найден");
            MemoryStream stream = new(image);

            var descriptors = PictureUtils.ComputeDescriptors(stream, 512);
            // fs.Close();
            if (descriptors == null) throw new Exception("Не вычислились дескрипторы");
            long id_image, id_descriptor;



            var sql = $"INSERT INTO images(path) VALUES('{GetImagesPath().StringResponse + Path.GetFileName(path)}') RETURNING id";
            using (var connection = new NpgsqlConnection("Server=127.0.0.1;User Id=postgres;Password=12345;Port=5432;Database=image-recognition;"))
            {

                using (var cmd = new NpgsqlCommand(sql, connection))
                {
                    connection.Open();
                    cmd.Prepare();
                    id_image = (long)cmd.ExecuteScalar();
                }
            }

            sql = $"INSERT INTO descriptors(descriptor) VALUES(@descriptor) RETURNING id";
            using (var connection = new NpgsqlConnection("Server=127.0.0.1;User Id=postgres;Password=12345;Port=5432;Database=image-recognition;"))
            {

                using (var cmd = new NpgsqlCommand(sql, connection))
                {
                    NpgsqlParameter param = cmd.CreateParameter();
                    param.ParameterName = "@descriptor";
                    param.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bytea;
                    param.Value = descriptors;
                    cmd.Parameters.Add(param);

                    connection.Open();
                    cmd.Prepare();
                    id_descriptor = (long)cmd.ExecuteScalar();
                }
            }

            sql = $"INSERT INTO image_descriptor(image_id, descriptor_id) VALUES({id_image},{id_descriptor})";
            using (var connection = new NpgsqlConnection("Server=127.0.0.1;User Id=postgres;Password=12345;Port=5432;Database=image-recognition;"))
            {

                using (var cmd = new NpgsqlCommand(sql, connection))
                {
                    connection.Open();
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
            }

            var request = new RestRequest("utility/post", Method.POST);
            request.AddFile("file", stream.ToArray(), path);

            return Execute<Response>(request);
        }

        public Response GetImagesPath()
        {
            var request = new RestRequest("utility/path", Method.GET);
            // request.RootElement = "Check";
            return Execute<Response>(request);
        }
    }
}
