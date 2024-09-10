//----------------------
// This file has been created with the help of GSIS official technical documentation
// and the tools described here: https://stackoverflow.com/questions/21611674/how-to-auto-generate-a-c-sharp-class-file-from-a-json-string
//----------------------

using System.Text.Json.Serialization;
using IdentityModel;
using Indice.Features.GovGr.Serialization;

namespace Indice.Features.GovGr.Models;


/// <summary>The encoded response from the EGovKyc resource server (JWS - json web signature?)</summary>
internal class KycHttpResponse
{
    [JsonPropertyName("payload")]
    public string Payload { get; set; }
    [JsonPropertyName("protected")]
    public string Protected { get; set; }
    [JsonPropertyName("signature")]
    public string Signature { get; set; }
}

/// <summary>The decoded protected from EGovKyc resource server's response</summary>
internal class Protected
{
    [JsonPropertyName("x5u")]
    public string X5u { get; set; }
    [JsonPropertyName("alg")]
    public string Alg { get; set; }
}

/// <summary>The decoded payload from EGovKyc resource server's response</summary>
public class KycPayload
{
    /// <summary>Response</summary>
    [JsonPropertyName("response")]
    public KycResponse Response { get; set; }
    /// <summary>Issued at claim (epoch date), <see cref="JwtClaimTypes.IssuedAt"/></summary>
    [JsonPropertyName("iat")]
    public int Iat { get; set; }
    /// <summary>Audience claim, <see cref="JwtClaimTypes.Audience"/></summary>
    [JsonPropertyName("aud")]
    public string Aud { get; set; }
    /// <summary>Jwt ID claim, <see cref="JwtClaimTypes.JwtId"/></summary>
    [JsonPropertyName("jti")]
    public int Jti { get; set; }
    /// <summary>Issuer claim, <see cref="JwtClaimTypes.Issuer"/></summary>
    [JsonPropertyName("iss")]
    public string Iss { get; set; }
    /// <summary>Subject claim, <see cref="JwtClaimTypes.Subject"/></summary>
    [JsonPropertyName("sub")]
    public string Sub { get; set; }
    /// <summary>Response Version</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; }



    /// <summary>Kyc result wrapper</summary>
    public class KycDataWrapper<TData> where TData : class
    {
        /// <summary>Data</summary>
        [JsonPropertyName("data")]
        public TData Data { get; set; }
        /// <summary>Result</summary>
        [JsonPropertyName("result")]
        public KycResult Result { get; set; }
    }

    /// <summary>Encapsulates a message and a <see cref="KycStatusCode"/></summary>
    public class KycResult
    {
        /// <summary>The text message</summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }
        /// <summary>The status code</summary>
        [JsonPropertyName("status")]
        public KycStatusCode Status { get; set; }
    }

    /// <summary>Document information.</summary>
    public class DocumentInfo
    {
        /// <summary>Ημερομηνία λήξης</summary>
        [JsonPropertyName("expireDate")]
        public string ExpireDate { get; set; }
        /// <summary>Μοναδικός αριθμός εγγράφου, ο ΑΔΤ</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }
        /// <summary>Ημερομηνία έκδοσης εγγράφου</summary>
        [JsonPropertyName("issueDate")]
        public string IssueDate { get; set; }
        /// <summary>Κωδικός Αρχής έκδοσης</summary>
        [JsonPropertyName("issuerId")]
        public string IssuerId { get; set; }
        /// <summary>Αρχή έκδοσης</summary>
        [JsonPropertyName("issuerName")]
        public string IssuerName { get; set; }
        /// <summary>Τύπος εγγράφου, π.χ. prado</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
        /// <summary>Είδος εγγράφου</summary>
        [JsonPropertyName("category")]
        public string Category { get; set; }
        /// <summary>Προηγούμενος Μοναδικός αριθμός εγγράφου</summary>
        [JsonPropertyName("previousId")]
        public string PreviousId { get; set; }
    }

    /// <summary>User information</summary>
    public class UserInfo
    {
        /// <summary>Ημερομηνία γέννησης</summary>
        [JsonPropertyName("birthDate")]
        public string BirthDate { get; set; }
        /// <summary>Έτος γέννησης</summary>
        [JsonPropertyName("birthYear")]
        public string BirthYear { get; set; }
        /// <summary>Τοποθεσία γέννησης</summary>
        [JsonPropertyName("birthPlace")]
        public string BirthPlace { get; set; }
        /// <summary>Πατρώνυμο</summary>
        [JsonPropertyName("fatherName")]
        public string FatherName { get; set; }
        /// <summary>Απόδοση με λατινικούς χαρακτήρες του fatherName</summary>
        [JsonPropertyName("fatherNameLatin")]
        public string FatherNameLatin { get; set; }
        /// <summary>Φύλο</summary>
        [JsonPropertyName("gender")]
        public string Gender { get; set; }
        /// <summary>Φωτογραφία</summary>
        [JsonPropertyName("image")]
        public string Image { get; set; }
        /// <summary>τύπος/κωδικοποίηση φωτογραφίας</summary>
        [JsonPropertyName("imageMimeType")]
        public string ImageMimeType { get; set; }
        /// <summary>Μητρώνυμο</summary>
        [JsonPropertyName("motherName")]
        public string MotherName { get; set; }
        /// <summary>Απόδοση με λατινικούς χαρακτήρες του motherName</summary>
        [JsonPropertyName("motherNameLatin")]
        public string MotherNameLatin { get; set; }
        /// <summary>Όνομα</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>Απόδοση με λατινικούς χαρακτήρες του name</summary>
        [JsonPropertyName("nameLatin")]
        public string NameLatin { get; set; }
        /// <summary>Επώνυμο</summary>
        [JsonPropertyName("surname")]
        public string Surname { get; set; }
        /// <summary>Απόδοση με λατινικούς χαρακτήρες του surname</summary>
        [JsonPropertyName("surnameLatin")]
        public string SurnameLatin { get; set; }
    }

    /// <summary>GrcBo Data</summary>
    public class GrcBoData
    {
        /// <summary>Στοιχεία σχετικά με το έγγραφο επιβεβαίωσης</summary>
        [JsonPropertyName("documentInfo")]
        public DocumentInfo DocumentInfo { get; set; }
        /// <summary>Στοιχεία σχετικά με τον χρήστη</summary>
        [JsonPropertyName("userInfo")]
        public UserInfo UserInfo { get; set; }
    }

    /// <summary>Identity Data</summary>
    public class IdentityData
    {
        /// <summary>GrcBo</summary>
        [JsonPropertyName("grcBo")]
        public KycDataWrapper<GrcBoData> GrcBo { get; set; }
    }

    /// <summary>Address</summary>
    public class Address
    {
        /// <summary>πόλη</summary>
        [JsonPropertyName("city")]
        public string City { get; set; }
        /// <summary>κωδικός χώρας</summary>
        [JsonPropertyName("country")]
        public string Country { get; set; }
        /// <summary>πίνακας με τα στοιχεία διεύθυνσης “Οδός και αριθμός”, “Τ.Κ”, “Πόλη”</summary>
        public List<string> lines { get; set; }
        /// <summary>αριθμός</summary>
        [JsonPropertyName("number")]
        public string Number { get; set; }
        /// <summary>οδός</summary>
        [JsonPropertyName("street")]
        public string Street { get; set; }
        /// <summary>Τ.Κ.</summary>
        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; }
    }

    /// <summary>Contact Info Data</summary>
    public class ContactInfoData
    {
        /// <summary>στοιχεία διεύθυνσης διαμονής</summary>
        [JsonPropertyName("address")]
        public Address Address { get; set; }
        /// <summary>στοιχεία διεύθυνσης επικοινωνίας</summary>
        [JsonPropertyName("contactAddress")]
        public Address ContactAddress { get; set; }
        /// <summary>ηλεκτρονική διεύθυνση αλληλογραφίας</summary>
        [JsonPropertyName("email")]
        public string Email { get; set; }
        /// <summary>κινητό τηλέφωνο</summary>
        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }
        /// <summary>σταθερό τηλέφωνο</summary>
        [JsonPropertyName("telephone")]
        public string Telephone { get; set; }
    }

    /// <summary>Income Data</summary>
    public class IncomeData
    {
        /// <summary>Προστιθέμενη διαφορά δαπανών</summary>
        [JsonPropertyName("addedCostDiffs")]
        public string AddedCostDiffs { get; set; }
        /// <summary>Επιχειρηματική δραστηριότητα</summary>
        [JsonPropertyName("businessIncome")]
        public string BusinessIncome { get; set; }
        /// <summary>Υπεραξία μεταβίβασης κεφαλαίου</summary>
        [JsonPropertyName("capitalTransferValue")]
        public string CapitalTransferValue { get; set; }
        /// <summary>Ζημιές από επιχειρηματική δραστηριότητα, ιδίου έτους</summary>
        [JsonPropertyName("damageBusinessCurr")]
        public string DamageBusinessCurr { get; set; }
        /// <summary>Ζημιές από επιχειρηματική δραστηριότητα, προηγούμενων ετών</summary>
        [JsonPropertyName("damageBusinessPrev")]
        public string DamageBusinessPrev { get; set; }
        /// <summary>Ζημιές από αγροτική δραστηριότητα, ιδίου έτους</summary>
        [JsonPropertyName("damageFarmingCurr")]
        public string DamageFarmingCurr { get; set; }
        /// <summary>Ζημιές από αγροτική δραστηριότητα, προηγούμενων ετών</summary>
        [JsonPropertyName("damageFarmingPrev")]
        public string DamageFarmingPrev { get; set; }
        /// <summary>Εισόδημα από αγροτική δραστηριότητα</summary>
        [JsonPropertyName("farmingIncome")]
        public string FarmingIncome { get; set; }
        /// <summary>Ακαθάριστα Έσοδα, από επιχειρηματική δραστηριότητα</summary>
        [JsonPropertyName("grossBusiness")]
        public string GrossBusiness { get; set; }
        /// <summary>Ακαθάριστα Έσοδα, από αγροτική δραστηριότητα</summary>
        [JsonPropertyName("grossFarming")]
        public string GrossFarming { get; set; }
        /// <summary>Εισόδημα από Μερίσματα – Τόκους - Δικαιώματα</summary>
        [JsonPropertyName("investmentIncome")]
        public string InvestmentIncome { get; set; }
        /// <summary>Ναυτικό εισόδημα</summary>
        [JsonPropertyName("maritimeIncome")]
        public string MaritimeIncome { get; set; }
        /// <summary>Έτος αναφοράς</summary>
        [JsonPropertyName("refYear")]
        public string RefYear { get; set; }
        /// <summary>Ημερμηνία έκδοσης πράξης</summary>
        [JsonPropertyName("releaseDate")]
        public string ReleaseDate { get; set; }
        /// <summary>Ακίνητη περιουσία</summary>
        [JsonPropertyName("rentalIncome")]
        public string RentalIncome { get; set; }
        /// <summary>Αυτοτελώς Φορολογούμενα Ποσά</summary>
        [JsonPropertyName("taxableAmounts")]
        public string TaxableAmounts { get; set; }
        /// <summary>Επίδομα ανεργίας</summary>
        [JsonPropertyName("unemploymentBenefits")]
        public string UnemploymentBenefits { get; set; }
        /// <summary>Μισθωτή Εργασία – Συντάξεις</summary>
        [JsonPropertyName("wagesPensionsIncome")]
        public string WagesPensionsIncome { get; set; }
    }

    /// <summary>Department</summary>
    public class Department
    {
        /// <summary>ΚΑΔ, παραρτήματος</summary>
        [JsonPropertyName("activityCode")]
        public string ActivityCode { get; set; }
        /// <summary>Περιγραφή δραστηριότητας</summary>
        [JsonPropertyName("activityDescr")]
        public string ActivityDescr { get; set; }
        /// <summary>Στοιχεία διεύθυνσης παραρτήματος</summary>
        [JsonPropertyName("address")]
        public Address Address { get; set; }
    }

    /// <summary>Main Activity</summary>
    public class MainActivity
    {
        /// <summary>ΚΑΔ, κύριας δραστηριότητας</summary>
        [JsonPropertyName("activityCode")]
        public string ActivityCode { get; set; }
        /// <summary>Περιγραφή κύριας δραστηριότητας</summary>
        [JsonPropertyName("activityDescr")]
        public string ActivityDescr { get; set; }
        /// <summary>Στοιχεία διεύθυνσης Έδρας</summary>
        [JsonPropertyName("address")]
        public Address Address { get; set; }
        /// <summary>Πίνακας δραστηριοτήτων με τα παρακάτω στοιχεία</summary>
        [JsonPropertyName("activities")]
        public List<Activity> Activities { get; set; }
    }

    /// <summary>Firm</summary>
    public class Firm
    {
        /// <summary>Στοιχεία παραρτήματος (optional)</summary>
        [JsonPropertyName("department")]
        public Department Department { get; set; }
        /// <summary>Στοιχεία Έδρας</summary>
        [JsonPropertyName("main")]
        public MainActivity Main { get; set; }
        /// <summary>Επωνυμία</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>ΑΦΜ επιχείρησης</summary>
        [JsonPropertyName("tin")]
        public string Tin { get; set; }
    }

    /// <summary>Private Sector Occupation</summary>
    public class PrivateSectorOccupation
    {
        /// <summary>Κωδικός ειδικότητας. Ακολουθεί το πρότυπο ΣΤΕΠ-92</summary>
        [JsonPropertyName("code")]
        public string Code { get; set; }
        /// <summary>Περιγραφή ειδικότητας. Ακολουθεί το πρότυπο ΣΤΕΠ-92</summary>
        [JsonPropertyName("descr")]
        public string Descr { get; set; }
        /// <summary>Τύπος/Πρότυπο κωδικοποίησης (ΣΤΕΠ-92)</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    /// <summary>Public Sector Occupation</summary>
    public class PublicSectorOccupation
    {
        /// <summary>Κωδικός κατηγορίας προσωπικού. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/EmployeeCategories</summary>
        [JsonPropertyName("categoryCode")]
        public string CategoryCode { get; set; }
        /// <summary>Περιγραφή κατηγορίας προσωπικού. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/EmployeeCategories</summary>
        [JsonPropertyName("categoryDescription")]
        public string CategoryDescription { get; set; }
        /// <summary>Κωδικός Κλάδου. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/ProfessionCategories</summary>
        [JsonPropertyName("sectorCode")]
        public string SectorCode { get; set; }
        /// <summary>Περιγραφή κλάδου. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/ProfessionCategories</summary>
        [JsonPropertyName("sectorDescription")]
        public string SectorDescription { get; set; }
        /// <summary>Κωδικός ειδικότητας. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/Specialities</summary>
        [JsonPropertyName("specialtyCode")]
        public string SpecialtyCode { get; set; }
        /// <summary>Περιγραφή ειδικότητας. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/Specialities</summary>
        [JsonPropertyName("specialtyDescription")]
        public string SpecialtyDescription { get; set; }
    }

    /// <summary>Private Employee Info Data</summary>
    public class PrivateEmployeeInfoData
    {
        /// <summary>καθεστώς εργασίας, 0=Πλήρης, 1=Μερική και 2=Εκ περιτροπής</summary>
        [JsonPropertyName("contractType")]
        public string ContractType { get; set; }
        /// <summary>Επιχείρηση</summary>
        [JsonPropertyName("firm")]
        public Firm Firm { get; set; }
        /// <summary>Ειδικότητα (για ιδιωτικό υπάλληλο)</summary>
        [JsonPropertyName("occupation")]
        public PrivateSectorOccupation Occupation { get; set; }
    }

    /// <summary>Employment Position</summary>
    public class EmploymentPosition
    {
        /// <summary>Επωνυμία φορέα</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>ΑΦΜ φορέα</summary>
        [JsonPropertyName("tin")]
        public string Tin { get; set; }
        /// <summary>Στοιχεία διεύθυνσης φορέα</summary>
        [JsonPropertyName("address")]
        public Address Address { get; set; }
    }

    /// <summary>Work Position</summary>
    public class WorkPosition
    {
        /// <summary>Επωνυμία φορέα</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>ΑΦΜ φορέα</summary>
        [JsonPropertyName("tin")]
        public string Tin { get; set; }
        /// <summary>Στοιχεία διεύθυνσης φορέα</summary>
        [JsonPropertyName("address")]
        public Address Address { get; set; }
    }

    /// <summary>Public Employee Info Data</summary>
    public class PublicEmployeeInfoData
    {
        /// <summary>Ειδικότητα (για δημόσιο υπάλληλο)</summary>
        [JsonPropertyName("occupation")]
        public PublicSectorOccupation Occupation { get; set; }
        /// <summary>Ένδειξη (true/false) αν είναι η κύρια σχέση εργασίας</summary>
        [JsonPropertyName("primary")]
        public string Primary { get; set; }
        /// <summary>Στοιχεία φορέα οργανικής θέσης</summary>
        [JsonPropertyName("employmentPosition")]
        public EmploymentPosition EmploymentPosition { get; set; }
        /// <summary>Στοιχεία φορέα θέσης απασχόλησης (Optional)</summary>
        [JsonPropertyName("workPosition")]
        public WorkPosition WorkPosition { get; set; }
        /// <summary>Εργασιακή σχέση</summary>
        [JsonPropertyName("employmentType")]
        public string EmploymentType { get; set; }
    }

    /// <summary>Activity/Δραστηριότητα</summary>
    public class Activity
    {
        /// <summary>ΚΑΔ</summary>
        [JsonPropertyName("activityCode")]
        public string ActivityCode { get; set; }
        /// <summary>Περιγραφή δραστηριότητας</summary>
        [JsonPropertyName("activityDescr")]
        public string ActivityDescr { get; set; }
        /// <summary>Τύπος δραστηριότητας (1 ή 2)</summary>
        [JsonPropertyName("activityType")]
        public string ActivityType { get; set; }
        /// <summary>Περιγραφή τύπου δραστηριότητας (ΚΥΡΙΑ ή ΔΕΥΤΕΡΕΥΟΥΣΑ)</summary>
        [JsonPropertyName("activityTypeDesc")]
        public string ActivityTypeDesc { get; set; }
    }

    /// <summary>Self Employed Info Data</summary>
    public class SelfEmployedInfoData
    {
        /// <summary>Main activity, Κύρια δραστηριότητα</summary>
        [JsonPropertyName("main")]
        public MainActivity Main { get; set; }
        /// <summary>Επωνυμία</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>ΑΦΜ αυτοαπασχολούμενου (ίδιο με το principal)</summary>
        [JsonPropertyName("tin")]
        public string Tin { get; set; }
    }

    /// <summary>Professional Activity Data</summary>
    public class ProfessionalActivityData
    {
        /// <summary>Private Employed Info</summary>
        [JsonPropertyName("privateEmployeeInfo")]
        public KycDataWrapper<List<PrivateEmployeeInfoData>> PrivateEmployeeInfo { get; set; }
        /// <summary>Public Employed Info</summary>
        [JsonPropertyName("publicEmployeeInfo")]
        public KycDataWrapper<List<PublicEmployeeInfoData>> PublicEmployeeInfo { get; set; }
        /// <summary>Self Employed Info</summary>
        [JsonPropertyName("selfEmployedInfo")]
        public KycDataWrapper<SelfEmployedInfoData> SelfEmployedInfo { get; set; }
    }

    /// <summary> Tax Information Data </summary>
    public class TaxidInfoData
    {
        /// <summary>ΑΦΜ του πολίτη (ίδιο με το principal)</summary>
        [JsonPropertyName("tin")]
        public string Tin { get; set; }

        /// <summary>Κωδικός της ΔΟΥ που υπάγεται ο πολίτης</summary>
        [JsonPropertyName("doy")]
        public string Doy { get; set; }

        /// <summary>Ονομασία της ΔΟΥ που υπάγεται ο πολίτης</summary>
        [JsonPropertyName("doyDescr")]
        public string DoyDescription { get; set; }
    }

    /// <summary> Family Status Data </summary>
    public class FamilyStatusData
    {
        /// <summary>Κωδικός οικογενειακής κατάστασης</summary>
        [JsonPropertyName("maritalStatus")]
        public string MaritalStatus { get; set; }

        /// <summary>Περιγραφή οικογενειακής κατάστασης</summary>
        [JsonPropertyName("maritalStatusDesc")]
        public string MaritalStatusDescription { get; set; }
    }

    /// <summary> Demographics Data </summary>
    public class DemographicsData
    {
        /// <summary>Κωδικός χώρας γέννησης</summary>
        [JsonPropertyName("birthCountryThreeDigitCode")]
        public string BirthCountryThreeDigitCode { get; set; }

        /// <summary>Ονομασία χώρας γέννησης</summary>
        [JsonPropertyName("birthCountryName")]
        public string BirthCountryName { get; set; }

        /// <summary>Κωδικός Υπηκοότητας</summary>
        [JsonPropertyName("mainNationalityId")]
        public string MainNationalityId { get; set; }

        /// <summary>Ονομασία Υπηκοότητας</summary>
        [JsonPropertyName("mainNationalityName")]
        public string MainNationalityName { get; set; }
    }

    /// <summary>Encapsulates the actual KYC data.</summary>
    public class ResponseData
    {
        /// <summary>Στοιχεία ταυτότητας</summary>
        [JsonPropertyName("identity")]
        public KycDataWrapper<IdentityData> Identity { get; set; }
        /// <summary>Στοιχεία επικοινωνίας</summary>
        [JsonPropertyName("contactInfo")]
        public KycDataWrapper<ContactInfoData> ContactInfo { get; set; }
        /// <summary>Στοιχεία εισοδήματος</summary>
        [JsonPropertyName("income")]
        public KycDataWrapper<IncomeData> Income { get; set; }
        /// <summary>Στοιχεία επαγγελματικής δραστηριότητας</summary>
        [JsonPropertyName("professionalActivity")]
        public KycDataWrapper<ProfessionalActivityData> ProfessionalActivity { get; set; }
        /// <summary>Στοιχεία ΔΟΥ</summary>
        [JsonPropertyName("taxidInfo")]
        public KycDataWrapper<TaxidInfoData> TaxidInfo { get; set; }
        /// <summary>Στοιχεία Οικογενειακής Κατάστασης</summary>
        [JsonPropertyName("familyStatus")]
        public KycDataWrapper<FamilyStatusData> FamilyStatus { get; set; }
        /// <summary>Δημογραφικά Στοιχεία</summary>
        [JsonPropertyName("demographics")]
        public KycDataWrapper<DemographicsData> Demographics { get; set; }

    }

    /// <summary>Encapsulates a govgr KYC response.</summary>
    public class KycResponse
    {
        /// <summary>Ο ΑΦΜ του χρήστη</summary>
        [JsonPropertyName("principal")]
        public string Principal { get; set; }
        /// <summary>The response data</summary>
        [JsonPropertyName("data")]
        public ResponseData Data { get; set; }
        /// <summary>Any message regarding the response, fault or other.</summary>
        [JsonPropertyName("result")]
        public KycResult Result { get; set; }
    }
}


/// <summary>GovGr KYC StatusCodes</summary>
[JsonConverter(typeof(StringKycStatusCodeConverter))]
public enum KycStatusCode
{
    /// <summary><see cref="Unknown"/></summary>
    Unknown,
    /// <summary><see cref="SuccessAndData"/></summary>
    SuccessAndData = 1200,
    /// <summary><see cref="SuccessButNoData"/></summary>
    SuccessButNoData = 1201,
    /// <summary><see cref="SuccessButPartialData"/></summary>
    SuccessButPartialData = 1202,
    /// <summary><see cref="CommunicationFailureWithGSIS"/></summary>
    CommunicationFailureWithGSIS = 1400,
    /// <summary><see cref="ConnectionFailureWithGSIS"/></summary>
    ConnectionFailureWithGSIS = 1401,
    /// <summary><see cref="ErroneousResponseFromGSIS"/></summary>
    ErroneousResponseFromGSIS = 1402,
    /// <summary><see cref="FailureInGSISInteroperabilityCenter"/></summary>
    FailureInGSISInteroperabilityCenter = 1410,
    /// <summary><see cref="CommunicationFailureBetweenGSISAndInformationProvider"/></summary>
    CommunicationFailureBetweenGSISAndInformationProvider = 1411,
    /// <summary><see cref="CommunicationFailureBetweenGSISAndInfrastructure"/></summary>
    CommunicationFailureBetweenGSISAndInfrastructure = 1412,
    /// <summary><see cref="ErrorInGSIS"/></summary>
    ErrorInGSIS = 1413,
    /// <summary><see cref="ErrorInGSISInteroperabilityCenter"/></summary>
    ErrorInGSISInteroperabilityCenter = 1510,
    /// <summary><see cref="NoAuthenticationToGSIS"/></summary>
    NoAuthenticationToGSIS = 1511,
    /// <summary><see cref="NoAuthorizationToGSIS"/></summary>
    NoAuthorizationToGSIS = 1512,
    /// <summary><see cref="ErroneousRequestToGSIS"/></summary>
    ErroneousRequestToGSIS = 1513,
    /// <summary><see cref="InvalidInputToGSIS"/></summary>
    InvalidInputToGSIS = 1514,
    /// <summary><see cref="CallLimitExceedanceToGSIS"/></summary>
    CallLimitExceedanceToGSIS = 1515,
    /// <summary><see cref="ErrorInInformationProviderInfrastructure"/></summary>
    ErrorInInformationProviderInfrastructure = 1520,
    /// <summary><see cref="ErrorInInformationProviderData"/></summary>
    ErrorInInformationProviderData = 1521,
    /// <summary><see cref="NoKycPermission"/></summary>
    NoKycPermission = 1530,
    /// <summary><see cref="NoEnoughData"/></summary>
    NoEnoughData = 1539,
}