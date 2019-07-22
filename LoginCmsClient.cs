using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AfipWsfeClient
{
    /// <summary>
    /// Clase para crear objetos Login Tickets
    /// </summary>
    /// <remarks>
    /// Ver documentacion: 
    ///    Especificacion Tecnica del Webservice de Autenticacion y Autorizacion
    ///    Version 1.0
    ///    Departamento de Seguridad Informatica - AFIP
    /// </remarks>
    public class LoginCmsClient
    {
        public bool IsProdEnvironment { get; set; } = false;
        public string WsaaUrlHomologation { get; set; } = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
        public string WsaaUrlProd { get; set; } = "https://wsaa.afip.gov.ar/ws/services/LoginCms";

        /// <summary>
        /// The path must end with \\
        /// </summary>
        public string TicketCacheFolderPath { get; set; } = "";

        public uint UniqueId; // Entero de 32 bits sin signo que identifica el requerimiento
        public DateTime GenerationTime; // Momento en que fue generado el requerimiento
        public DateTime ExpirationTime; // Momento en el que expira la solicitud
        public string Service; // Identificacion del WSN para el cual se solicita el TA
        public string Sign; // Firma de seguridad recibida en la respuesta
        public string Token; // Token de seguridad recibido en la respuesta
        public XmlDocument XmlLoginTicketRequest = null;
        public XmlDocument XmlLoginTicketResponse = null;
        public string CertificatePath;
        public string XmlStrLoginTicketRequestTemplate = "<loginTicketRequest><header><uniqueId></uniqueId><generationTime></generationTime><expirationTime></expirationTime></header><service></service></loginTicketRequest>";
        private bool VerboseMode = true;
        private static uint GlobalUniqueID = 0; // OJO! NO ES THREAD-SAFE

        /// <summary>
        /// Construye un Login Ticket obtenido del WSAA
        /// </summary>
        /// <param name="service">Servicio al que se desea acceder</param>
        /// <param name="urlWsaa">URL del WSAA</param>
        /// <param name="x509CertificateFilePath">Ruta del certificado X509 (con clave privada) usado para firmar</param>
        /// <param name="password">Password del certificado X509 (con clave privada) usado para firmar</param>
        /// <param name="verbose">Nivel detallado de descripcion? true/false</param>
        /// <remarks></remarks>
        public async Task<WsaaTicket> LoginCmsAsync(string service,
                                                    string x509CertificateFilePath,
                                                    string password,
                                                    bool verbose)
        {
            var ticketCacheFile = string.IsNullOrEmpty(TicketCacheFolderPath) ?
                                        service + "ticket.json" :
                                        TicketCacheFolderPath + service + "ticket.json";

            if (File.Exists(ticketCacheFile))
            {
                var ticketJson = File.ReadAllText(ticketCacheFile);
                var ticket = JsonConvert.DeserializeObject<WsaaTicket>(ticketJson);
                if (DateTime.UtcNow <= ticket.ExpirationTime)
                    return ticket;
            }

            const string ID_FNC = "[ObtenerLoginTicketResponse]";
            CertificatePath = x509CertificateFilePath;
            VerboseMode = verbose;
            X509CertificateManager.VerboseMode = verbose;

            // PASO 1: Genero el Login Ticket Request
            try
            {
                GlobalUniqueID += 1;

                XmlLoginTicketRequest = new XmlDocument();
                XmlLoginTicketRequest.LoadXml(XmlStrLoginTicketRequestTemplate);

                var xmlNodoUniqueId = XmlLoginTicketRequest.SelectSingleNode("//uniqueId");
                var xmlNodoGenerationTime = XmlLoginTicketRequest.SelectSingleNode("//generationTime");
                var xmlNodoExpirationTime = XmlLoginTicketRequest.SelectSingleNode("//expirationTime");
                var xmlNodoService = XmlLoginTicketRequest.SelectSingleNode("//service");
                xmlNodoGenerationTime.InnerText = DateTime.Now.AddMinutes(-10).ToString("s");
                xmlNodoExpirationTime.InnerText = DateTime.Now.AddMinutes(+10).ToString("s");
                xmlNodoUniqueId.InnerText = Convert.ToString(GlobalUniqueID);
                xmlNodoService.InnerText = service;
                Service = service;

                if (VerboseMode) Console.WriteLine(XmlLoginTicketRequest.OuterXml);
            }
            catch (Exception ex)
            {
                throw new Exception(ID_FNC + "***Error GENERANDO el LoginTicketRequest : " + ex.Message + ex.StackTrace);
            }

            string base64SignedCms;
            // PASO 2: Firmo el Login Ticket Request
            try
            {
                if (VerboseMode) Console.WriteLine(ID_FNC + "***Leyendo certificado: {0}", CertificatePath);

                var securePassword = new NetworkCredential("", password).SecurePassword;
                securePassword.MakeReadOnly();


                var certFirmante = X509CertificateManager.GetCertificateFromFile(CertificatePath, securePassword);

                if (VerboseMode)
                {
                    Console.WriteLine(ID_FNC + "***Firmando: ");
                    Console.WriteLine(XmlLoginTicketRequest.OuterXml);
                }

                // Convierto el Login Ticket Request a bytes, firmo el msg y lo convierto a Base64
                var msgEncoding = Encoding.UTF8;
                var msgBytes = msgEncoding.GetBytes(XmlLoginTicketRequest.OuterXml);
                var encodedSignedCms = X509CertificateManager.SignMessageBytes(msgBytes, certFirmante);
                base64SignedCms = Convert.ToBase64String(encodedSignedCms);
            }
            catch (Exception ex)
            {
                throw new Exception(ID_FNC + "***Error FIRMANDO el LoginTicketRequest : " + ex.Message);
            }

            string loginTicketResponse;
            // PASO 3: Invoco al WSAA para obtener el Login Ticket Response
            try
            {
                if (VerboseMode)
                {
                    Console.WriteLine(ID_FNC + "***Llamando al WSAA en URL: {0}", IsProdEnvironment ? WsaaUrlProd : WsaaUrlHomologation);
                    Console.WriteLine(ID_FNC + "***Argumento en el request:");
                    Console.WriteLine(base64SignedCms);
                }

                var wsaaService = new AfipLoginCmsServiceReference.LoginCMSClient();
                wsaaService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? WsaaUrlProd : WsaaUrlHomologation);

                var response = await wsaaService.loginCmsAsync(base64SignedCms);
                loginTicketResponse = response.loginCmsReturn;

                if (VerboseMode)
                {
                    Console.WriteLine(ID_FNC + "***LoguinTicketResponse: ");
                    Console.WriteLine(loginTicketResponse);
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ID_FNC + "***Error INVOCANDO al servicio WSAA : " + ex.Message);
            }

            // PASO 4: Analizo el Login Ticket Response recibido del WSAA
            try
            {
                XmlLoginTicketResponse = new XmlDocument();
                XmlLoginTicketResponse.LoadXml(loginTicketResponse);

                UniqueId = uint.Parse(XmlLoginTicketResponse.SelectSingleNode("//uniqueId").InnerText);
                GenerationTime = DateTime.Parse(XmlLoginTicketResponse.SelectSingleNode("//generationTime").InnerText);
                ExpirationTime = DateTime.Parse(XmlLoginTicketResponse.SelectSingleNode("//expirationTime").InnerText);
                Sign = XmlLoginTicketResponse.SelectSingleNode("//sign").InnerText;
                Token = XmlLoginTicketResponse.SelectSingleNode("//token").InnerText;
            }
            catch (Exception ex)
            {
                throw new Exception(ID_FNC + "***Error ANALIZANDO el LoginTicketResponse : " + ex.Message);
            }

            var ticketResponse = new WsaaTicket { Sign = Sign, Token = Token, ExpirationTime = ExpirationTime };
            File.WriteAllText(ticketCacheFile, JsonConvert.SerializeObject(ticketResponse));

            return ticketResponse;
        }
    }
}
