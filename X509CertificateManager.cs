using System;
using System.Security;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace AfipWsfeClient
{
    /// <summary>
    /// Libreria de utilidades para manejo de certificados
    /// </summary>
    /// <remarks></remarks>
    public class X509CertificateManager
    {
        public static bool VerboseMode = false;

        /// <summary>
        /// Firma mensaje
        /// </summary>
        /// <param name="messageBytes">Bytes del mensaje</param>
        /// <param name="signerCertificate">Certificado usado para firmar</param>
        /// <returns>Bytes del mensaje firmado</returns>
        /// <remarks></remarks>
        public static byte[] SignMessageBytes(byte[] messageBytes, X509Certificate2 signerCertificate)
        {
            const string ID_FNC = "[FirmaBytesMensaje]";
            try
            {
                // Pongo el mensaje en un objeto ContentInfo (requerido para construir el obj SignedCms)
                var contentInfo = new ContentInfo(messageBytes);
                var signedCms = new SignedCms(contentInfo);

                // Creo objeto CmsSigner que tiene las caracteristicas del firmante
                var cmsSigner = new CmsSigner(signerCertificate)
                {
                    IncludeOption = X509IncludeOption.EndCertOnly
                };

                if (VerboseMode) Console.WriteLine(ID_FNC + "***Firmando bytes del mensaje...");

                // Firmo el mensaje PKCS #7
                signedCms.ComputeSignature(cmsSigner);

                if (VerboseMode) Console.WriteLine(ID_FNC + "***OK mensaje firmado");

                // Encodeo el mensaje PKCS #7.
                return signedCms.Encode();
            }
            catch (Exception ex)
            {
                throw new Exception(ID_FNC + "***Error al firmar: " + ex.Message);
            }
        }

        /// <summary>
        /// Lee certificado de disco
        /// </summary>
        /// <param name="file">Ruta del certificado a leer.</param>
        /// <returns>Un objeto certificado X509</returns>
        /// <remarks></remarks>
        public static X509Certificate2 GetCertificateFromFile(string file, SecureString password)
        {
            const string ID_FNC = "[ObtieneCertificadoDesdeArchivo]";
            try
            {
                var objCert = new X509Certificate2(file, password);
                return objCert;
            }
            catch (Exception ex)
            {
                throw new Exception(ID_FNC + "***Error al leer certificado: " + ex.Message);
            }
        }
    }
}
