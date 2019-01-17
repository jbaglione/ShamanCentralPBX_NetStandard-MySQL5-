using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
//using System.Data.OleDb;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace ShamanCentralPBX_NetStandard.Controllers
{
    public class HomeController : Controller
    {
        static string connectionString = ConfigurationManager.AppSettings["MySqlConnetionString"];
        //static string connetionString = Configuration.GetSection("CustomSettings");
        static int minuteVariation = Convert.ToInt32(ConfigurationManager.AppSettings["MinuteVariation"]);

        private readonly Logger _logger;

        public HomeController()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        ////http://localhost:54959/home/index?uniqueId=5&fechaHoraLlamado=10000&numero=20000&agente=30000
        ////http://localhost:54959/?uniqueId=5&fechaHoraLlamado=10000&numero=20000&agente=30000
        //[HttpGet("{uniqueId}")]
        public ActionResult Index(string uniqueId, string fechaHoraLlamado, string numero, string agente)
        {
            _logger.Info("Index page says hello");

            if (uniqueId == "favicon.ico")
            {
                _logger.Error("Index, uniqueId wrong value, uniqueId => 'favicon.ico'");
                return View();
            }
            //app.UseStaticFiles(new StaticFileOptions()
            //{
            //    FileProvider = new PhysicalFileProvider(
            //    Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot", "files")),
            //    RequestPath = new PathString("/MyFyles")
            //});
            //_logger.LogCritical(message);

            string callFileName = "https://www.computerhope.com/jargon/m/example.mp3?" + DateTime.Now.Millisecond;

            callFileName = GetFileByUniqueId(uniqueId);

            //callFileName = "888-20181224-001209-1545621129.564933";

            if (string.IsNullOrEmpty(callFileName))
                callFileName = GetFileByPoperties(fechaHoraLlamado, numero, agente);

            if (!string.IsNullOrEmpty(callFileName))
                callFileName = GetFullPath(callFileName);

                ViewData["callFileName"] = callFileName;
            //Response.Redirect(callFileName, true);
            return View(); //uniqueId + " " + fechaHoraLlamado + " " + numero + " " + agente;
        }

        private string GetFullPath(string callFileName)
        {
            string fullPath = "http://" + Request.Url.Authority;
            fullPath += "/audios/" + callFileName.Replace('-', '/') + ".mp3";
            return fullPath;
        }

        private string GetFileByPoperties(string fechaHoraLlamado, string numero, string agente)
        {
            _logger.Info(string.Format("GetFileByPoperties, fechaHoraLlamado => {0}, numero  => {1}, agente  => {2}", fechaHoraLlamado, numero, agente));

            if (string.IsNullOrEmpty(fechaHoraLlamado) &&
                string.IsNullOrEmpty(numero)
                //&& string.IsNullOrEmpty(agente)
                ) return null;

            try
            {

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        Console.WriteLine("ServerVersion: {0} \nDataSource: {1}",
                            connection.ServerVersion, connection.DataSource);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    // The connection is automatically closed when the
                    // code exits the using block.
                }

                using (MySqlConnection cnn = new MySqlConnection(connectionString))
                {
                    using (MySqlCommand cmd = cnn.CreateCommand())
                    {
                        _logger.Info("GetFileByPoperties, try cnn.Open(), connectionString=> " + connectionString);
                        cnn.Open();
                        _logger.Info("GetFileByPoperties, cnn.Open() succesfull, connectionString=> " + connectionString);

                        cmd.CommandText = "select CALLFILENAME" +
                                            " from cdr" +
                                            " where calldate between '" + Convert.ToDateTime(fechaHoraLlamado).AddMinutes(-minuteVariation).ToString("yyyy-MM-dd HH:mm:ss") + "'" +
                                            " and '" + Convert.ToDateTime(fechaHoraLlamado).AddMinutes(minuteVariation).ToString("yyyy-MM-dd HH:mm:ss") + "'" +
                                            " and src = " + numero +
                                            " and sentido = 'IN' LIMIT 1"
                                            //+ "and agente = " + agente
                                            ;
                        _logger.Info("GetFileByPoperties, cmd.CommandText => " + cmd.CommandText);

                        using (MySqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            _logger.Info("GetFileByPoperties, cmd.ExecuteReader");

                            DataTable dt = new DataTable();
                            dt.Load(rdr);
                            _logger.Info("GetFileByPoperties, dt.Rows.Count => " + dt.Rows.Count);
                            if (dt.Rows.Count > 0)
                            {
                                _logger.Info("GetFileByPoperties, dt.Rows[0][0] => " + dt.Rows[0][0].ToString());
                                return dt.Rows[0][0].ToString();
                            }
                            //else
                            //    addLog(false, "ReadMySqlRings", "No hay llamadas entrantes");
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.Error(ex, "Exception GetFileByPoperties ");
                _logger.Error(("GetFileByPoperties Exepcion (ex GetaAllMessages) => " + Helper.GetaAllMessages(ex)));

            }
            return null;
        }

        private string GetFileByUniqueId(string uniqueId)
        {
            _logger.Info("GetFileByUniqueId, uniqueId => " + uniqueId);

            if (string.IsNullOrEmpty(uniqueId)) return null;

            try
            {
                using (MySqlConnection cnn = new MySqlConnection(connectionString))
                {
                    using (MySqlCommand cmd = cnn.CreateCommand())
                    {
                        _logger.Info("GetFileByUniqueId, try cnn.Open(), connectionString=> " + connectionString);
                        cnn.Open();
                        _logger.Info("GetFileByUniqueId, cnn.Open() succesfull, connectionString=> " + connectionString);

                        cmd.CommandText = "select CALLFILENAME " +
                                            "from cdr where uniqueid = " + uniqueId;

                        using (MySqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            _logger.Info("GetFileByUniqueId, cmd.ExecuteReader");
                            DataTable dt = new DataTable();
                            dt.Load(rdr);

                            _logger.Info("GetFileByUniqueId, dt.Rows.Count => " + dt.Rows.Count);
                            if (dt.Rows.Count > 0)
                            {
                                _logger.Info("GetFileByUniqueId, dt.Rows[0][0] => " + dt.Rows[0][0].ToString());
                                return dt.Rows[0][0].ToString();
                            }
                            //else
                            //    addLog(false, "ReadMySqlRings", "No hay llamadas entrantes");
                        }
                    }
                }
            }

            catch (Exception ex)
            {

                _logger.Error(ex, "Exception GetFileByUniqueId ");
                _logger.Error(("GetFileByUniqueId Exepcion (ex GetaAllMessages) => " + Helper.GetaAllMessages(ex)));
            }
            return null;
        }

        //private string GetFileByPoperties(string fechaHoraLlamado, string numero, string agente)
        //{
        //    _logger.Info(string.Format("GetFileByPoperties, fechaHoraLlamado => {0}, numero  => {1}, agente  => {2}", fechaHoraLlamado, numero, agente));

        //    if (string.IsNullOrEmpty(fechaHoraLlamado) &&
        //        string.IsNullOrEmpty(numero)
        //        //&& string.IsNullOrEmpty(agente)
        //        ) return null;

        //    try
        //    {

        //        using (OleDbConnection connection = new OleDbConnection(connectionString))
        //        {
        //            try
        //            {
        //                connection.Open();
        //                Console.WriteLine("ServerVersion: {0} \nDataSource: {1}",
        //                    connection.ServerVersion, connection.DataSource);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.Message);
        //            }
        //            // The connection is automatically closed when the
        //            // code exits the using block.
        //        }

        //        using (OleDbConnection cnn = new OleDbConnection(connectionString))
        //        {
        //            using (OleDbCommand cmd = cnn.CreateCommand())
        //            {
        //                _logger.Info("GetFileByPoperties, try cnn.Open(), connectionString=> " + connectionString);
        //                cnn.Open();
        //                _logger.Info("GetFileByPoperties, cnn.Open() succesfull, connectionString=> " + connectionString);

        //                cmd.CommandText = "select CALLFILENAME" +
        //                                    "from cdr" +
        //                                    "where calldate <= " + Convert.ToDateTime(fechaHoraLlamado).AddMinutes(-minuteVariation).ToString() +
        //                                    "and calldate >= " + Convert.ToDateTime(fechaHoraLlamado).AddMinutes(minuteVariation).ToString() +
        //                                    "and src = " + numero
        //                                    //+ "and agente = " + agente
        //                                    ;

        //                using (OleDbDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
        //                {
        //                    _logger.Info("GetFileByPoperties, cmd.ExecuteReader");

        //                    DataTable dt = new DataTable();
        //                    dt.Load(rdr);
        //                    _logger.Info("GetFileByPoperties, dt.Rows.Count => " + dt.Rows.Count);
        //                    if (dt.Rows.Count > 0)
        //                    {
        //                        _logger.Info("GetFileByPoperties, dt.Rows[0][0] => " + dt.Rows[0][0].ToString());
        //                        return dt.Rows[0][0].ToString();
        //                    }
        //                    //else
        //                    //    addLog(false, "ReadMySqlRings", "No hay llamadas entrantes");
        //                }
        //            }
        //        }
        //    }

        //    catch (Exception ex)
        //    {
        //        _logger.Error(ex, "Exception GetFileByPoperties ");
        //        _logger.Error(("GetFileByPoperties Exepcion (ex GetaAllMessages) => " + Helper.GetaAllMessages(ex)));

        //    }
        //    return null;
        //}

        //private string GetFileByUniqueId(string uniqueId)
        //{
        //    _logger.Info("GetFileByUniqueId, uniqueId => " + uniqueId);

        //    if (string.IsNullOrEmpty(uniqueId)) return null;

        //    try
        //    {
        //        using (OleDbConnection cnn = new OleDbConnection(connectionString))
        //        {
        //            using (OleDbCommand cmd = cnn.CreateCommand())
        //            {
        //                _logger.Info("GetFileByUniqueId, try cnn.Open(), connectionString=> " + connectionString);
        //                cnn.Open();
        //                _logger.Info("GetFileByUniqueId, cnn.Open() succesfull, connectionString=> " + connectionString);

        //                cmd.CommandText = "select CALLFILENAME" +
        //                                    "from cdr where uniqueid = " + uniqueId;

        //                using (OleDbDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
        //                {
        //                    _logger.Info("GetFileByUniqueId, cmd.ExecuteReader");
        //                    DataTable dt = new DataTable();
        //                    dt.Load(rdr);

        //                    _logger.Info("GetFileByUniqueId, dt.Rows.Count => " + dt.Rows.Count);
        //                    if (dt.Rows.Count > 0)
        //                    {
        //                        _logger.Info("GetFileByUniqueId, dt.Rows[0][0] => " + dt.Rows[0][0].ToString());
        //                        return dt.Rows[0][0].ToString();
        //                    }
        //                    //else
        //                    //    addLog(false, "ReadMySqlRings", "No hay llamadas entrantes");
        //                }
        //            }
        //        }
        //    }

        //    catch (Exception ex)
        //    {

        //        _logger.Error(ex, "Exception GetFileByUniqueId ");
        //        _logger.Error(("GetFileByUniqueId Exepcion (ex GetaAllMessages) => " + Helper.GetaAllMessages(ex)));
        //    }
        //    return null;
        //}


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}