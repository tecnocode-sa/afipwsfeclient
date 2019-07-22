# AFIP WSFE Web Services Client
AFIP WSFE .NET Standard Client. Manages WSAA Ticket caching and access WSFE operations
(C) 2019 Tecnocode S.A. http://tecnocode.net

[![Build Status](https://tecnocodearg.visualstudio.com/Tecnocode/_apis/build/status/tecnocode-sa.afipwsfeclient?branchName=master)](https://tecnocodearg.visualstudio.com/Tecnocode/_build/latest?definitionId=7&branchName=master)

## Installation
- NuGet 
https://www.nuget.org/packages/AfipWsfeClient

- .NET CLI
```
dotnet add package afipwsfeclient
```

- Package Manager Console
```
Install-Package afipwsfeclient
```
## Certificate Generation
You must provide an pkcs12 certificate file to access AFIP services 

- Generate Private Key:
```
openssl genrsa -out afip.key 2048 
```

- Generate Certificate Sign Request:
```
openssl req -new -key afip.key -subj "/C=AR/O=INSERT_ORG_NAME/CN=INSERT_ORG_CN/serialNumber=CUIT INSERT_CUIT" -out afip.csr
```

- Generate PEM with AFIP website

Upload the CSR file to WSASS AFIP Service and save the resulting text in afip.pem file

- Generate X509 Certificate in pkcs12 format
```
openssl pkcs12 -export -inkey afip.key -in afip.pem -out afip.p12 
``` 

## Library usage
```csharp
using System.Collections.Generic;
using AfipWsfeClient;
```

```csharp
//Get Login Ticket
var loginClient = new LoginCmsClient { IsProdEnvironment = false };
var ticket = await loginClient.LoginCmsAsync("wsfe",
                                             "C:\\INSERT_CERTIFICATE_PATH\\afip.p12",
                                             "CERTIFICATE_PASSWORD",
                                             true);

var wsfeClient = new WsfeClient
{
    IsProdEnvironment = false,
    Cuit = INSERT_CUIT,
    Sign = ticket.Sign,
    Token = ticket.Token
};

//Get next WSFE Comp. Number
var compNumber = await wsfeClient.FECompUltimoAutorizadoAsync(1, 6).Body.FECompUltimoAutorizadoResult.CbteNro + 1;

//Build WSFE FECAERequest            
var feCaeReq = new AfipServiceReference.FECAERequest
{
    FeCabReq = new AfipServiceReference.FECAECabRequest
    {
        CantReg = 1,
        CbteTipo = 6,
        PtoVta = 1
    },
    FeDetReq = new List<AfipServiceReference.FECAEDetRequest>
    {
        new AfipServiceReference.FECAEDetRequest
        {
            CbteDesde = compNumber,
            CbteHasta = compNumber,
            CbteFch = "20190717",
            Concepto = 2,
            DocNro = 30111222,
            DocTipo = 96,
            FchVtoPago = "20190718",
            ImpNeto = 10,
            ImpTotal = 10,
            FchServDesde = "20190717",
            FchServHasta = "20190717",
            MonCotiz = 1,
            MonId = "PES",
            Iva = new List<AfipServiceReference.AlicIva>
            {
                new AfipServiceReference.AlicIva
                {
                    BaseImp = 10,
                    Id = 3,
                    Importe = 0
                }
            }
        }
    }
};

//Call WSFE FECAESolicitar
var compResult = await wsfeClient.FECAESolicitarAsync(feCaeReq);
```

## LoginCmsClient Class Parameters
```csharp
public bool IsProdEnvironment { get; set; } = false; //default is Homologation
public string WsaaUrlHomologation { get; set; } = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms"; //default URL
public string WsaaUrlProd { get; set; } = "https://wsaa.afip.gov.ar/ws/services/LoginCms"; //default URL
public string TicketCacheFolderPath { get; set; } = ""; //Path to store ticket chache file, default is runtime current path
```

## WsfeClient Class Parameters
```csharp
public bool IsProdEnvironment { get; set; } = false; //default is Homologation
public long Cuit { get; set; }
public string Token { get; set; } //Your WSAA ticket Token
public string Sign { get; set; } //Your WSAA ticket Sign
public string WsfeUrlHomologation { get; set; } = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx"; //default URL
public string WsfeUrlProd { get; set; } = "https://servicios1.afip.gov.ar/wsfev1/service.asmx"; //default URL
```

## Environments (pre-configured in package)
- URL Testing WSAA: https://wsaahomo.afip.gov.ar/ws/services/LoginCms
- URL Testing WSFE: https://wswhomo.afip.gov.ar/wsfev1/service.asmx

- URL Prod WSAA: https://wsaa.afip.gov.ar/ws/services/LoginCms
- URL Prod WSFE: https://servicios1.afip.gov.ar/wsfev1/service.asmx

## WSDL (used to build the package)
- AFIP LoginCms https://wsaahomo.afip.gov.ar/ws/services/LoginCms?wsdl
- AFIP WSFE https://wswhomo.afip.gov.ar/wsfev1/service.asmx?wsdl
