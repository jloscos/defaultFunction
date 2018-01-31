using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using WorldCupModel;

namespace DefaultFunction
{
    public static class EWCIAFunction
    {
        [FunctionName("EWCIAFunction")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            string content = await req.Content.ReadAsStringAsync();

            log.Info("Reçu : " + content);
            //MatchState state = JsonConvert.DeserializeObject<MatchState>(content);
            XNode node = JsonConvert.DeserializeXNode(content, "MatchState");
            List<TurnAction> actions = new List<TurnAction>();

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["IADatabase"].ConnectionString;

            using (var con = new SqlConnection(connectionString))
            {
                await con.OpenAsync();
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "EWCIA";
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    var param = new SqlParameter("@MatchState", System.Data.SqlDbType.Xml);
                    param.Value = node.ToString();
                    cmd.Parameters.Add(param);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(reader.GetString(0));
                            return req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeXmlNode(doc));
                        }
                    }
                }
            }

            return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "error");
        }
    }
}
