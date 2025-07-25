using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Features.GovGr.Models;

/// <summary>GovGR Docuemnts service response</summary>
public class DoucumentsReponse
{
    /// <summary>Wallet information</summary>
    [JsonPropertyName("data")]
    public DocumentData? Data { get; set; }
    /// <summary>Dynamic metadata</summary>
    [JsonPropertyName("meta")]
    public JsonElement Meta { get; set; }

}

/// <summary>Document Data</summary>
public class DocumentData
{
    /// <summary>Document</summary>
    [JsonPropertyName("document")]
    public Doc? Document { get; set; }
    /// <summary>Attachment Retrieval</summary>
    [JsonPropertyName("attachment_retrieval")]
    public string? AttachmentRetrieval { get; set; }
    /// <summary>Export Document Pdf</summary>
    [JsonPropertyName("export_document_pdf")]
    public bool? ExportDocumentPdf { get; set; }
    /// <summary>Document Pdf</summary>
    [JsonPropertyName("document-pdf")]
    public Attachment? DocumentPdf { get; set; }

    /// <summary>Document</summary>
    public class Doc
    {
        /// <summary>Statements</summary>
        [JsonPropertyName("statements")]
        public Statements? Statements { get; set; }
        /// <summary>Attachments</summary>
        [JsonPropertyName("attachments")]
        public Attachments? Attachments { get; set; }
        /// <summary>Display</summary>
        [JsonPropertyName("display")]
        public Display? Display { get; set; }
        /// <summary>Issuer</summary>
        [JsonPropertyName("issuer")]
        public string? Issuer { get; set; }
        /// <summary>Case Id</summary>
        [JsonPropertyName("case_id")]
        public string? CaseId { get; set; }
        /// <summary>Is Official</summary>
        [JsonPropertyName("is_official")]
        public bool? IsOfficial { get; set; }
        /// <summary>Template</summary>
        [JsonPropertyName("template")]
        public Template? Template { get; set; }
        /// <summary>Timestamp</summary>
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
        /// <summary>Step name of the process. For example otp</summary>
        [JsonPropertyName("step")]
        public string? Step { get; set; }
        /// <summary>Document Id</summary>
        [JsonPropertyName("document-id")]
        public string? DocumentId { get; set; }
        /// <summary>Declaration Id</summary>
        [JsonPropertyName("declaration-id")]
        public string? DeclarationId { get; set; }
        /// <summary>Document Title</summary>
        [JsonPropertyName("document-title")]
        public string? DocumentTitle { get; set; }
        /// <summary>State</summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }
        /// <summary>Digest Sha256</summary>
        [JsonPropertyName("digest-sha256")]
        public string? DigestSha256 { get; set; }
    }

    /// <summary>Template</summary>
    public class Template
    {
        /// <summary>RefName</summary>
        [JsonPropertyName("refname")]
        public string? RefName { get; set; }
        /// <summary>Digest Sha256</summary>
        [JsonPropertyName("digest-sha256")]
        public string? DigestSha256 { get; set; }
    }

    /// <summary>Statements</summary>
    public class Statements
    {
        /// <summary>Id Number</summary>
        [JsonPropertyName("idnumber")]
        public string? IdNumber { get; set; }
        /// <summary>Confirmation Code / otp</summary>
        [JsonPropertyName("confirmation_code")]
        public string? ConfirmationCode { get; set; }
        /// <summary>Name</summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        /// <summary>Name Latin</summary>
        [JsonPropertyName("nameLatin")]
        public string? NameLatin { get; set; }
        /// <summary>Surname</summary>
        [JsonPropertyName("surname")]
        public string? Surname { get; set; }
        /// <summary>Surname Latin</summary>
        [JsonPropertyName("surnameLatin")]
        public string? SurnameLatin { get; set; }
        /// <summary>FatherName</summary>
        [JsonPropertyName("fatherName")]
        public string? FatherName { get; set; }
        /// <summary>FatherName Latin</summary>
        [JsonPropertyName("fatherNameLatin")]
        public string? FatherNameLatin { get; set; }
        /// <summary>Mother Name</summary>
        [JsonPropertyName("motherName")]
        public string? MotherName { get; set; }
        /// <summary>Birth Date</summary>
        [JsonPropertyName("birthDate")]
        public string? BirthDate { get; set; }
        /// <summary>Birth Place</summary>
        [JsonPropertyName("birthPlace")]
        public string? BirthPlace { get; set; }
        /// <summary>Issue Institution Description</summary>
        [JsonPropertyName("issueInstitution_description")]
        public string? IssueInstitutionDescription { get; set; }
        /// <summary>Issue Date</summary>
        [JsonPropertyName("issueDate")]
        public string? IssueDate { get; set; }
    }

    /// <summary>Display</summary>
    public class Display
    {
        /// <summary>QrCode Value</summary>
        [JsonPropertyName("qrcode/value")]
        public string? QrCodeValue { get; set; }
        /// <summary>RefCode Value</summary>
        [JsonPropertyName("refcode/value")]
        public string? RefCodeValue { get; set; }
        /// <summary>Timestamp Value</summary>
        [JsonPropertyName("timestamp/value")]
        public string? TimestampValue { get; set; }
    }

    /// <summary>Attachments</summary>
    public class Attachments
    {
        /// <summary>Id Photo</summary>
        [JsonPropertyName("id_photo")]
        public Attachment? IdPhoto { get; set; }
    }

    /// <summary>Attachment</summary>
    public class Attachment
    {
        /// <summary>Filename</summary>
        [JsonPropertyName("filename")]
        public string? Filename { get; set; }
        /// <summary>Digest Sha256</summary>
        [JsonPropertyName("digest-sha256")]
        public string? DigestSha256 { get; set; }
        /// <summary>Content</summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        /// <summary>Content Encoding</summary>
        [JsonPropertyName("content-encoding")]
        public string? ContentEncoding { get; set; }
    }
}
