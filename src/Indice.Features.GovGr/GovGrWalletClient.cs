using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Indice.Features.GovGr.Interfaces;
using Indice.Features.GovGr.Models;
using static Indice.Features.GovGr.Configuration.GovGrOptions;

namespace Indice.Features.GovGr
{
    internal class GovGrWalletClient : IWalletService
    {
        private const string PROD_SERVICE_NAME = "GOV-WALLET-PRESENT-ID";
        private const string SDBX_SERVICE_NAME = "GOV-WALLET-PRESENT-ID-DEMO";

        private readonly GovGrDocumentsClient _documentsClient;
        private readonly WalletOptions _settings;
        
        protected string ServiceName => _settings.Sandbox ? SDBX_SERVICE_NAME : PROD_SERVICE_NAME;

        public GovGrWalletClient(
            HttpClient httpClient,
            WalletOptions settings) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _documentsClient = new GovGrDocumentsClient(httpClient, new DocumentsOptions {
                ServiceName = ServiceName,
                Token = _settings.Token
            });
        }

        public async Task<WalletDocumentReference> RequestIdentificationAsync(string idNumber) {
            if (idNumber is null) {
                throw new ArgumentNullException(nameof(idNumber));
            }

            var response = await _documentsClient.PostAsync(new () {
                Document = new () { 
                    Template = new() {
                        DigestSha256 = string.Empty
                    },
                    Statements = new() {
                        IdNumber = idNumber
                    },
                    Attachments = new()
                }
            });

            return new() {
                DeclarationId = response.Data.Document.DeclarationId,
                Title = response.Data.Document.DocumentTitle,
            };
        }

        public async Task<DocumentData> GetIdentificationAsync(string declarationId, string confirmationCode, bool? exportDocumentPdf = null) {
            if (declarationId is null) {
                throw new ArgumentNullException(nameof(declarationId));
            }
            if (confirmationCode is null) {
                throw new ArgumentNullException(nameof(confirmationCode));
            }
            var response = await _documentsClient.PostAsync(new() {
                Document = new() {
                    Template = new() {
                        DigestSha256 = string.Empty
                    },
                    Statements = new() {
                        ConfirmationCode = confirmationCode
                    },
                    DeclarationId = declarationId,
                    Attachments = new(),
                },
                AttachmentRetrieval = "content",
                ExportDocumentPdf = exportDocumentPdf
            });

            return response.Data;
        }
    }
}
