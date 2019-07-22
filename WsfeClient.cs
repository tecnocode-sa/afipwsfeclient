using System.ServiceModel;
using System.Threading.Tasks;

namespace AfipWsfeClient
{
    public class WsfeClient
    {
        public bool IsProdEnvironment { get; set; } = false;
        public long Cuit { get; set; }
        public string Token { get; set; }
        public string Sign { get; set; }

        public string WsfeUrlHomologation { get; set; } = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
        public string WsfeUrlProd { get; set; } = "https://servicios1.afip.gov.ar/wsfev1/service.asmx";

        public async Task<AfipServiceReference.FECompUltimoAutorizadoResponse> FECompUltimoAutorizadoAsync(int ptoVta, int cbteTipo)
        {
            var wsfeService = new AfipServiceReference.ServiceSoapClient(AfipServiceReference.ServiceSoapClient.EndpointConfiguration.ServiceSoap);
            wsfeService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? WsfeUrlProd : WsfeUrlHomologation);

            var auth = new AfipServiceReference.FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var response = await wsfeService.FECompUltimoAutorizadoAsync(auth, ptoVta, cbteTipo);
            return response;
        }

        public async Task<AfipServiceReference.FECAESolicitarResponse> FECAESolicitarAsync(AfipServiceReference.FECAERequest feCaeReq)
        {
            var wsfeService = new AfipServiceReference.ServiceSoapClient(AfipServiceReference.ServiceSoapClient.EndpointConfiguration.ServiceSoap);
            wsfeService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? WsfeUrlProd : WsfeUrlHomologation);

            var auth = new AfipServiceReference.FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };

            var response = await wsfeService.FECAESolicitarAsync(auth, feCaeReq);

            return response;
        }
    }
}
