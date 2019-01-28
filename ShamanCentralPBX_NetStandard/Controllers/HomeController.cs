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
        public ActionResult Index(string uniqueId, string fechaHoraLlamado, string numero, string agente)
        {
            try
            {
                _logger.Info("Index page says hello");

                if (uniqueId == "favicon.ico")
                {
                    _logger.Error("Index, uniqueId wrong value, uniqueId => 'favicon.ico'");
                    return View();
                }
                string callFileName;// = "https://www.computerhope.com/jargon/m/example.mp3?" + DateTime.Now.Millisecond;

                callFileName = GetFileByUniqueId(uniqueId);

                if (string.IsNullOrEmpty(callFileName))
                    callFileName = GetFileByPoperties(fechaHoraLlamado, numero, agente);

                if (!string.IsNullOrEmpty(callFileName))
                    callFileName = GetFullPath(callFileName, ".wav") ?? GetFullPath(callFileName, ".mp3");

                ViewData["callFileName"] = callFileName;
            }
            catch (Exception)
            {
                throw;
            }

            return View();
        }

        private string GetFullPath(string callFileName, string extension)
        {
            //callFileName = "880-20190122-084138-1548157291.174563";
            string partialPath = "http://" + Request.Url.Authority + "/audios/";
            string physicalfullPath = ConfigurationManager.AppSettings["physicalPath"] + callFileName + extension;
            string fullPath = partialPath + callFileName + extension;

            _logger.Info(string.Format("GetFullPath, physicalfullPath => {0}, fullPath  => {1}", physicalfullPath, fullPath));

            if (!System.IO.File.Exists(physicalfullPath))
            {
                string fechaImplicita = callFileName.Split('-')[1];
                string anio = fechaImplicita.Substring(0, 4);
                string mes = fechaImplicita.Substring(4, 2);
                string periodo = mes + anio;

                physicalfullPath = ConfigurationManager.AppSettings["physicalPath"] + periodo + "\\" + callFileName + extension;
                fullPath = partialPath + periodo + "/" + callFileName + extension;
            }

            if (!System.IO.File.Exists(physicalfullPath))
                return null;
            else
                return fullPath + "?" + DateTime.Now.Millisecond;
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
                                            //" and src like '%" + numero + "'" +
                                            " and RIGHT(src, 8) = RIGHT('" + numero + "', 8)" +
                                            " and CALLFILENAME is not null" +
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
                                            "from cdr where uniqueid = '" + uniqueId + "'";

                        _logger.Info("GetFileByUniqueId, cmd.CommandText => " + cmd.CommandText);

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