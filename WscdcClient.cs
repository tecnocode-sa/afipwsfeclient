using System.ServiceModel;
using System.Threading.Tasks;

namespace AfipWsfeClient
{
    public class WscdcClient
    {
        public bool IsProdEnvironment { get; set; } = false;
        public long Cuit { get; set; }
        public string Token { get; set; }
        public string Sign { get; set; }

        public string WscdcUrlHomologation { get; set; } = "https://wswhomo.afip.gov.ar/WSCDC/service.asmx";
        public string WscdcUrlProd { get; set; } = "https://servicios1.afip.gov.ar/WSCDC/service.asmx";

        public async Task<AfipWscdcServiceReference.ComprobanteConstatarResponse> ComprobanteConstatarAsync(AfipWscdcServiceReference.CmpDatos cmpReq)
        {
            var wscdcService = new AfipWscdcServiceReference.ServiceSoapClient(AfipWscdcServiceReference.ServiceSoapClient.EndpointConfiguration.ServiceSoap);
            wscdcService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? WscdcUrlProd : WscdcUrlHomologation);

            var auth = new AfipWscdcServiceReference.CmpAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };

            var response = await wscdcService.ComprobanteConstatarAsync(auth, cmpReq);

            return response;
        }
    }
}
