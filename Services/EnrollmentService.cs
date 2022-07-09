using WebServicesEnrollment.Models;
using System.Data.SqlClient;
using System.Data;
using System.Text.Json;
using Serilog;

namespace WebServicesEnrollment.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private SqlConnection connection = new SqlConnection("Server=localhost;Database=kalum_test;User Id=sa;Password=K!n@l2022;");
        private AppLog AppLog = new AppLog();

        public EnrollmentService(){
            
        }
        public EnrollmentResponse EnrollmentProcess(EnrollmentRequest request)
        {
            AppLog.ResponseTime = Convert.ToInt16(DateTime.Now.ToString("fff"));
            AppLog.DateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            EnrollmentResponse respuesta = null;
            Aspirante aspirante = BuscarAspirante(request.NoExpediente);

            if (aspirante == null){
                respuesta = new EnrollmentResponse() {Codigo = 204, Respuesta = $"No existen aspirante con el numero de expediente {request.NoExpediente}"};
                ImprimirLog(204, $"No existen aspirante con el numero de expediente {request.NoExpediente}", "Information");
            }else{
                respuesta = EjecutarProcedimiento(request);
            }

            return respuesta;
        }

        private EnrollmentResponse EjecutarProcedimiento(EnrollmentRequest request)
        {
            EnrollmentResponse respuesta = null;

            SqlCommand cmd = new SqlCommand("sp_EnrollmentProcess", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@NoExpediente", request.NoExpediente));
            cmd.Parameters.Add(new SqlParameter("@Ciclo", request.Ciclo));
            cmd.Parameters.Add(new SqlParameter("@MesInicioPago", request.MesInicioPago));
            cmd.Parameters.Add(new SqlParameter("@CarreraId", request.CarreraId));
            SqlDataReader reader = null;
            try
            {
                connection.Open();
                reader = cmd.ExecuteReader();
                while(reader.Read()){
                    respuesta = new EnrollmentResponse(){Respuesta = reader.GetValue(0).ToString(), Carne = reader.GetValue(1).ToString()};
                    if(reader.GetValue(0).ToString().Equals("TRANSACTION SUCCESS")){
                        respuesta.Codigo = 201;
                        ImprimirLog(201, reader.GetValue(0).ToString(), "Information");
                    }else if(reader.GetValue(0).ToString().Equals("TRANSACTION ERROR")){
                        respuesta.Codigo = 503;
                        ImprimirLog(503, reader.GetValue(0).ToString(), "Error");
                    }else{
                        respuesta.Codigo = 503;
                        ImprimirLog(503, "Error al momento de llamar al procedimiento almacenado", "Error");
                    }
                }
                reader.Close();
                connection.Close();
            }catch(Exception e){
                respuesta = new EnrollmentResponse() {Codigo = 503, Respuesta = "Error al momento de llamar al procedimiento almacenado", Carne = "0"};
                ImprimirLog(503, "Error al momento de llamar al procedimiento almacenado", "Error");
            }finally{
                connection.Close();
            }

            return respuesta;
        }

        private void ImprimirLog(int responseCode, string message, string typeLog){
            AppLog.ResponseCode = responseCode;
            AppLog.Message = message;
            AppLog.ResponseTime = Convert.ToInt16(DateTime.Now.ToString("fff")) - AppLog.ResponseTime;
            if(typeLog.Equals("Information")){
                AppLog.Level = 20;
                Log.Information(JsonSerializer.Serialize(AppLog));
            }
            else if(typeLog.Equals("Error")){
                AppLog.Level = 40;
                Log.Error(JsonSerializer.Serialize(AppLog));
            }
            else if(typeLog.Equals("Debug")){
                AppLog.Level = 10;
                Log.Debug(JsonSerializer.Serialize(AppLog));
            }
        }

        private Aspirante BuscarAspirante(string noExpediente){
            Aspirante resultado = null;

            SqlDataAdapter daAspirante = new SqlDataAdapter($"SELECT * FROM Aspirante WHERE NoExpediente = '{noExpediente}'", connection);
            DataSet dsAspirante = new DataSet();
            daAspirante.Fill(dsAspirante, "Aspirante");
            if(dsAspirante.Tables["Aspirante"].Rows.Count > 0){
                resultado = new Aspirante()
                {
                    NoExpediente = dsAspirante.Tables["Aspirante"].Rows[0][0].ToString(),
                    Apellidos = dsAspirante.Tables["Aspirante"].Rows[0][1].ToString(),
                    Nombres = dsAspirante.Tables["Aspirante"].Rows[0][2].ToString(),
                    Direccion = dsAspirante.Tables["Aspirante"].Rows[0][3].ToString(),
                    Telefono = dsAspirante.Tables["Aspirante"].Rows[0][4].ToString(),
                    Email = dsAspirante.Tables["Aspirante"].Rows[0][5].ToString(),
                    Estatus = dsAspirante.Tables["Aspirante"].Rows[0][6].ToString(),
                    CarreraId = dsAspirante.Tables["Aspirante"].Rows[0][7].ToString(),
                    JornadaId = dsAspirante.Tables["Aspirante"].Rows[0][8].ToString()
                };
            }
            return resultado;
        }

        public string Test(string s)
        {
            Console.WriteLine("Test method executed");
            return s;
        }
    }
}