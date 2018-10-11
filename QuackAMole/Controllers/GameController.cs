using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net;


namespace QuackAMole.Controllers
{
    public class GameController : Controller
    {

        private string _baseUrl = "https://quantumexperience.ng.bluemix.net/api";
        private string _token = "YOURTOKENHERE";
        private HttpClient _client = new HttpClient();
        private int _shots = 1;
        private string _deviceRunType = "simulator";
        public string _codeType = "QASM2";
         
    
        // GET: Game
        public ActionResult Index()
        {
            return View("Index", 1);
        }


        public ActionResult Ent()
        {
            return View("Index", 2);
        }

        [HttpPost]
        public  string GetValues(int typeOfCode)
        {
            string result = "";
            QuackUser currUser = Session["QuackUser"] as QuackUser;

            if (currUser == null)
            {
                Result resObject = LogIn();
                if (resObject.Success)
                {
                    currUser = JsonConvert.DeserializeObject<QuackUser>(resObject.Message);
                    Session["QuackUser"] = currUser;
                }

            }

            if (currUser.id != null && currUser.id.Length > 0)
            {
                Result measurementResult = ExecuteCode(currUser, typeOfCode);

                if (measurementResult.Success)
                {
                    Newtonsoft.Json.Linq.JObject o = Newtonsoft.Json.Linq.JObject.Parse(measurementResult.Message);
                    result = o["result"]["data"]["p"]["labels"].First.ToString();

                    // Delete 
                    string expID = o["code"]["idCode"].ToString();
                    DeleteExperiment(expID, currUser);
                }


            }
          
            Debug.Write("result  " + result);
            return result;
        }

        public void DeleteExperiment(string experimentID, QuackUser currUser)
        {
            //DELETE https://quantumexperience.ng.bluemix.net/api/users/a3e5c196cb90688ba9a50dd7607999a6/codes/553c3398a4039e2b809cc6ec110e971e HTTP/1.1
            //Host: quantumexperience.ng.bluemix.net

            Result result = new Result();
            string baseUrl = string.Format("/users/{0}/codes/{1}",currUser.userid, experimentID);
            System.Diagnostics.Debug.WriteLine("Deleteing to URL: " + baseUrl);
            result = GetAPIData(baseUrl, HttpMethod.Delete, null, currUser);
            if (result.Success)
            {
                Debug.WriteLine("Deleted.");
            }
            else
            {
                Debug.WriteLine("Failed: " + result.Message);
            }
            
        }


        public Result LogIn()
        {
            if (_token == string.Empty)
            {
                return new Result("", false);
            }

            HttpContent content = new StringContent("apiToken=" + _token,
                                    System.Text.Encoding.UTF8,
                                    "application/x-www-form-urlencoded");//CONTENT-TYPE header


            Result result = GetAPIData("/users/loginWithToken", HttpMethod.Post, content, new QuackUser());

            return result;
        }


        public string GetCode(int typeOfCode)
        {

            string result = "include \"qelib1.inc\";qreg q[5];creg c[5];";

            switch (typeOfCode)
            {
                case 1:
                    result += @"
                h q[0];
                h q[1];
                h q[2];
                h q[3];
                h q[4];
                measure q[0] -> c[0];
                measure q[1] -> c[1];
                measure q[2] -> c[2];
                measure q[3] -> c[3];
                measure q[4] -> c[4];";
                    break;

                case 2:
                    result += @"
                        h q[0];
                        cx q[0],q[1];
                        measure q[0] -> c[0];
                        measure q[1] -> c[1];
                        h q[3];
                        cx q[3],q[4];
                        measure q[3] -> c[3];
                        h q[2];
                        measure q[4] -> c[4];
                        measure q[2] -> c[2];";

                    break;

            }
           

            return result;

        }

        public Result ExecuteCode(QuackUser currUser, int typeOfCode)
        {
            Result result = new Result();


            string url = string.Format("/codes/execute?shots={0}&deviceRunType={1}",
                    _shots.ToString(),
                    _deviceRunType
                    );

            var kvp = new List<KeyValuePair<string, string>>();
            kvp.Add(new KeyValuePair<string, string>("qasm", GetCode(typeOfCode)));
            kvp.Add(new KeyValuePair<string, string>("codeType", _codeType));
            kvp.Add(new KeyValuePair<string, string>("name", string.Format("ExperimentID {0} ", System.Guid.NewGuid().ToString())));

            HttpContent content = new FormUrlEncodedContent(kvp);

            result = GetAPIData(url, HttpMethod.Post, content, currUser);

            return result;

        }

        private Result GetAPIData(string urlRelativePath,
                                 HttpMethod httpMethod,
                                 HttpContent contentToSend, QuackUser currUser)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            Result result = new Result();

           
            if (currUser.ttl>0 && httpMethod != HttpMethod.Delete)
            {
                urlRelativePath = urlRelativePath + "&access_token=" + currUser.id;
            }

            string url = _baseUrl + urlRelativePath;


            HttpRequestMessage request = new HttpRequestMessage(httpMethod, url);
            request.Content = contentToSend;
            if (currUser.ttl>0) request.Headers.Add("X-Access-Token", currUser.id);
            using (HttpResponseMessage response = _client.SendAsync(request).Result)
                if (response.IsSuccessStatusCode)
                {
                    using (HttpContent content = response.Content)
                    {
                        result.Message = content.ReadAsStringAsync().Result;
                        result.Success = response.IsSuccessStatusCode;
                    }
                }
                else
                {
                    result.Message = response.ReasonPhrase;
                    result.Success = false;
                }
            return result;
        }
    }
}
