/* 
 * https://www.scottbrady91.com/C-Sharp/PEM-Loading-in-dotnet-core-and-dotnet
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;

namespace PPK.Client
{
    public class Program
    {
        private static readonly HttpClient _httpClient = new() { BaseAddress = new Uri(AUTHORITY) };
        private static string _accessToken;
        #region Constants
        private const string AUTHORITY = "https://localhost:2000";
        private const string PRIVATE_KEY =
            @"-----BEGIN PRIVATE KEY-----
              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
              vA6PRDilnESC/XzvUJA+jT4BR04VfA==
              -----END PRIVATE KEY-----";
        private const string PUBLIC_KEY =
            @"-----BEGIN CERTIFICATE-----
              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
              2rm8
              -----END CERTIFICATE-----";
        #endregion

        public static void Main(string[] args) {
            DrawMenu();
            var x509SigningCredentials = GetSigningCredentials();
            var message = "This is a sample for .NET 5 private/public cryptography";
            var signature = SignMessage(message, x509SigningCredentials);
            var certificate = new X509Certificate2(Convert.FromBase64String(PUBLIC_KEY.Replace("-----BEGIN CERTIFICATE-----", string.Empty).Replace("-----END CERTIFICATE-----", string.Empty)));
            var securityKey = new X509SecurityKey(certificate);
            var isValidMessage = ValidateMessage(message, signature, securityKey);
            Console.ReadKey();
        }

        private static void MenuItemSelectedEventHandler(object sender, EventArgs eventArgs) {
            var selectedItemIndex = ((MenuItemEventArgs)eventArgs).SelectedItemIndex;
            switch (selectedItemIndex) {
                case 0:
                    StartLoginWithCredentialsFlow();
                    break;
                default:
                    break;
            }
        }

        private static void StartLoginWithCredentialsFlow() {
            Console.WriteLine("Please enter your username:");
            var userName = Console.ReadLine();
            Console.WriteLine("Please enter your password:");
            var password = Console.ReadLine();
            // Get discovery document.
            var discoveryDocument = _httpClient
                .GetDiscoveryDocumentAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            if (discoveryDocument.IsError) {
                Console.WriteLine("Could not obtain discovery document.");
                return;
            }
            // Perform a request to the token endpoint using the 'password' grant.
            var tokenResponse = _httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = "ppk-client",
                ClientSecret = "JUEKX2XugFv5XrX3",
                Scope = "openid phone",
                UserName = userName,
                Password = password
            })
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
            Console.WriteLine();
            if (tokenResponse.IsError) {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Could not obtain access token: {0}", tokenResponse.Error);
                Console.ResetColor();
                Console.WriteLine();
                DrawMenu();
                return;
            }
            _accessToken = tokenResponse.AccessToken;
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Access token was issued: {0}", tokenResponse.AccessToken);
            Console.ResetColor();
            Console.WriteLine();
            DrawMenu();
        }

        private static void DrawMenu() {
            var menu = new ConsoleMenu("Please choose the operation to perform:", new List<string> {
                "1) Log in with username/password",
                "2) Register device",
                "3) Log in with certificate"
            });
            menu.MenuItemSelected += MenuItemSelectedEventHandler;
            menu.Draw();
        }

        private static X509SigningCredentials GetSigningCredentials() {
            var certificate = X509Certificate2.CreateFromPem(PUBLIC_KEY, PRIVATE_KEY);
            var signingCredentials = new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSha256Signature);
            return signingCredentials;
        }

        private static string SignMessage(byte[] message, X509SigningCredentials x509SigningCredentials) {
            using var key = x509SigningCredentials.Certificate.GetRSAPrivateKey();
            return Convert.ToBase64String(key.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        private static string SignMessage(string message, X509SigningCredentials x509SigningCredentials) {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            return SignMessage(messageBytes, x509SigningCredentials);
        }

        private static bool ValidateMessage(string message, string signature, SecurityKey securityKey) {
            var cryptoProviderFactory = securityKey.CryptoProviderFactory;
            var signatureProvider = cryptoProviderFactory.CreateForVerifying(securityKey, "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
            var canVerifyMessage = signatureProvider.Verify(Encoding.UTF8.GetBytes(message), Convert.FromBase64String(signature));
            cryptoProviderFactory.ReleaseSignatureProvider(signatureProvider);
            return canVerifyMessage;
        }
    }
}
