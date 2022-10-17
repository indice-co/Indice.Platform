//----------------------
// This file has been created with the help of GSIS official technical documentation
// and the tools described here: https://stackoverflow.com/questions/21611674/how-to-auto-generate-a-c-sharp-class-file-from-a-json-string
//----------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Indice.Features.Kyc.GovGr.Enums;

namespace Indice.Features.Kyc.GovGr.Models
{
    /// <summary>
    /// The encoded response from the EGovKyc resource server (JWS - json web signature?)
    /// </summary>
    public class KycResponse
    {

        [JsonPropertyName("payload")]
        public string Payload { get; set; }
        [JsonPropertyName("protected")]
        public string Protected { get; set; }
        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }

    /// <summary>
    /// Document information.
    /// </summary>
    public class DocumentInfo
    {
        /// <summary>
        /// Ημερομηνία λήξης
        /// </summary>
        [JsonPropertyName("expireDate")]
        public string ExpireDate { get; set; }
        /// <summary>
        /// Μοναδικός αριθμός εγγράφου, ο ΑΔΤ
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }
        /// <summary>
        /// Ημερομηνία έκδοσης εγγράφου
        /// </summary>
        [JsonPropertyName("issueDate")]
        public string IssueDate { get; set; }
        /// <summary>
        /// Κωδικός Αρχής έκδοσης
        /// </summary>
        [JsonPropertyName("issuerId")]
        public string IssuerId { get; set; }
        /// <summary>
        /// Αρχή έκδοσης
        /// </summary>
        [JsonPropertyName("issuerName")]
        public string IssuerName { get; set; }
        /// <summary>
        /// Τύπος εγγράφου, π.χ. prado
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
        /// <summary>
        /// Είδος εγγράφου
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; }
        /// <summary>
        /// Προηγούμενος Μοναδικός αριθμός εγγράφου
        /// </summary>
        [JsonPropertyName("previousId")]
        public string PreviousId { get; set; }
    }

    /// <summary>
    /// User information
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Ημερομηνία γέννησης
        /// </summary>
        [JsonPropertyName("birthDate")]
        public string BirthDate { get; set; }
        /// <summary>
        /// Έτος γέννησης
        /// </summary>
        [JsonPropertyName("birthYear")]
        public string BirthYear { get; set; }
        /// <summary>
        /// Τοποθεσία γέννησης
        /// </summary>
        [JsonPropertyName("birthPlace")]
        public string BirthPlace { get; set; }
        /// <summary>
        /// Πατρώνυμο
        /// </summary>
        [JsonPropertyName("fatherName")]
        public string FatherName { get; set; }
        /// <summary>
        /// Απόδοση με λατινικούς χαρακτήρες του fatherName
        /// </summary>
        [JsonPropertyName("fatherNameLatin")]
        public string FatherNameLatin { get; set; }
        /// <summary>
        /// Φύλο
        /// </summary>
        [JsonPropertyName("gender")]
        public string Gender { get; set; }
        /// <summary>
        /// Φωτογραφία
        /// </summary>
        [JsonPropertyName("image")]
        public string Image { get; set; }
        /// <summary>
        /// τύπος/κωδικοποίηση φωτογραφίας
        /// </summary>
        [JsonPropertyName("imageMimeType")]
        public string ImageMimeType { get; set; }
        /// <summary>
        /// Μητρώνυμο
        /// </summary>
        [JsonPropertyName("motherName")]
        public string MotherName { get; set; }
        /// <summary>
        /// Απόδοση με λατινικούς χαρακτήρες του motherName
        /// </summary>
        [JsonPropertyName("motherNameLatin")]
        public string MotherNameLatin { get; set; }
        /// <summary>
        /// Όνομα
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// Απόδοση με λατινικούς χαρακτήρες του name
        /// </summary>
        [JsonPropertyName("nameLatin")]
        public string NameLatin { get; set; }
        /// <summary>
        /// Επώνυμο
        /// </summary>
        [JsonPropertyName("surname")]
        public string Surname { get; set; }
        /// <summary>
        /// Απόδοση με λατινικούς χαρακτήρες του surname
        /// </summary>
        [JsonPropertyName("surnameLatin")]
        public string SurnameLatin { get; set; }
    }

    /// <summary>
    /// Encapsulates a message and a <see cref="GovGrKycStatusCode"/>
    /// </summary>
    public class GovGrKycResult
    {
        /// <summary>The text message</summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }
        /// <summary>The status code</summary>
        [JsonPropertyName("status")]
        public GovGrKycStatusCode Status { get; set; }
    }

    public class GrcBo
    {
        [JsonPropertyName("data")]
        public GrcBoData Data { get; set; }
        [JsonPropertyName("result")]
        public GovGrKycResult Result { get; set; }
    }

    public class GrcBoData
    {
        /// <summary>
        /// Στοιχεία σχετικά με το έγγραφο επιβεβαίωσης
        /// </summary>
        [JsonPropertyName("documentInfo")]
        public DocumentInfo DocumentInfo { get; set; }
        /// <summary>
        /// Στοιχεία σχετικά με τον χρήστη
        /// </summary>
        [JsonPropertyName("userInfo")]
        public UserInfo UserInfo { get; set; }
    }

    public class Identity
    {
        [JsonPropertyName("data")]
        public IdentityData Data { get; set; }
        [JsonPropertyName("result")]
        public GovGrKycResult Result { get; set; }
    }

    public class IdentityData
    {
        [JsonPropertyName("grcBo")]
        public GrcBo GrcBo { get; set; }
    }

    public class EGovKycAddress
    {
        /// <summary>
        /// πόλη
        /// </summary>
        [JsonPropertyName("city")]
        public string City { get; set; }
        /// <summary>
        /// κωδικός χώρας
        /// </summary>
        [JsonPropertyName("country")]
        public string Country { get; set; }
        /// <summary>
        /// πίνακας με τα στοιχεία διεύθυνσης “Οδός και αριθμός”, “Τ.Κ”, “Πόλη”
        /// </summary>
        public List<string> lines { get; set; }
        /// <summary>
        /// αριθμός
        /// </summary>
        [JsonPropertyName("number")]
        public string Number { get; set; }
        /// <summary>
        /// οδός
        /// </summary>
        [JsonPropertyName("street")]
        public string Street { get; set; }
        /// <summary>
        /// Τ.Κ.
        /// </summary>
        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; }
    }

    public class ContactInfo
    {
        [JsonPropertyName("data")]
        public ContactInfoData Data { get; set; }
        [JsonPropertyName("result")]
        public GovGrKycResult Result { get; set; }
    }

    public class ContactInfoData
    {
        /// <summary>
        /// στοιχεία διεύθυνσης διαμονής
        /// </summary>
        [JsonPropertyName("address")]
        public EGovKycAddress Address { get; set; }
        /// <summary>
        /// στοιχεία διεύθυνσης επικοινωνίας
        /// </summary>
        [JsonPropertyName("contactAddress")]
        public EGovKycAddress ContactAddress { get; set; }
        /// <summary>
        /// ηλεκτρονική διεύθυνση αλληλογραφίας
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; }
        /// <summary>
        /// κινητό τηλέφωνο
        /// </summary>
        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }
        /// <summary>
        /// σταθερό τηλέφωνο
        /// </summary>
        [JsonPropertyName("telephone")]
        public string Telephone { get; set; }
    }

    public class Income
    {
        [JsonPropertyName("data")]
        public IncomeData Data { get; set; }
        [JsonPropertyName("result")]
        public GovGrKycResult Result { get; set; }
    }

    public class IncomeData
    {
        /// <summary>
        /// Προστιθέμενη διαφορά δαπανών
        /// </summary>
        [JsonPropertyName("addedCostDiffs")]
        public string AddedCostDiffs { get; set; }
        /// <summary>
        /// Επιχειρηματική δραστηριότητα
        /// </summary>
        [JsonPropertyName("businessIncome")]
        public string BusinessIncome { get; set; }
        /// <summary>
        /// Υπεραξία μεταβίβασης κεφαλαίου
        /// </summary>
        [JsonPropertyName("capitalTransferValue")]
        public string CapitalTransferValue { get; set; }
        /// <summary>
        /// Ζημιές από επιχειρηματική δραστηριότητα, ιδίου έτους
        /// </summary>
        [JsonPropertyName("damageBusinessCurr")]
        public string DamageBusinessCurr { get; set; }
        /// <summary>
        /// Ζημιές από επιχειρηματική δραστηριότητα, προηγούμενων ετών
        /// </summary>
        [JsonPropertyName("damageBusinessPrev")]
        public string DamageBusinessPrev { get; set; }
        /// <summary>
        /// Ζημιές από αγροτική δραστηριότητα, ιδίου έτους
        /// </summary>
        [JsonPropertyName("damageFarmingCurr")]
        public string DamageFarmingCurr { get; set; }
        /// <summary>
        /// Ζημιές από αγροτική δραστηριότητα, προηγούμενων ετών
        /// </summary>
        [JsonPropertyName("damageFarmingPrev")]
        public string DamageFarmingPrev { get; set; }
        /// <summary>
        /// Εισόδημα από αγροτική δραστηριότητα
        /// </summary>
        [JsonPropertyName("farmingIncome")]
        public string FarmingIncome { get; set; }
        /// <summary>
        /// Ακαθάριστα Έσοδα, από επιχειρηματική δραστηριότητα
        /// </summary>
        [JsonPropertyName("grossBusiness")]
        public string GrossBusiness { get; set; }
        /// <summary>
        /// Ακαθάριστα Έσοδα, από αγροτική δραστηριότητα
        /// </summary>
        [JsonPropertyName("grossFarming")]
        public string GrossFarming { get; set; }
        /// <summary>
        /// Εισόδημα από Μερίσματα – Τόκους - Δικαιώματα
        /// </summary>
        [JsonPropertyName("investmentIncome")]
        public string InvestmentIncome { get; set; }
        /// <summary>
        /// Ναυτικό εισόδημα
        /// </summary>
        [JsonPropertyName("maritimeIncome")]
        public string MaritimeIncome { get; set; }
        /// <summary>
        /// Έτος αναφοράς
        /// </summary>
        [JsonPropertyName("refYear")]
        public string RefYear { get; set; }
        /// <summary>
        /// Ημερμηνία έκδοσης πράξης
        /// </summary>
        [JsonPropertyName("releaseDate")]
        public string ReleaseDate { get; set; }
        /// <summary>
        /// Ακίνητη περιουσία
        /// </summary>
        [JsonPropertyName("rentalIncome")]
        public string RentalIncome { get; set; }
        /// <summary>
        /// Αυτοτελώς Φορολογούμενα Ποσά
        /// </summary>
        [JsonPropertyName("taxableAmounts")]
        public string TaxableAmounts { get; set; }
        /// <summary>
        /// Επίδομα ανεργίας
        /// </summary>
        [JsonPropertyName("unemploymentBenefits")]
        public string UnemploymentBenefits { get; set; }
        /// <summary>
        /// Μισθωτή Εργασία – Συντάξεις
        /// </summary>
        [JsonPropertyName("wagesPensionsIncome")]
        public string WagesPensionsIncome { get; set; }
    }

    public class Department
    {
        /// <summary>
        /// ΚΑΔ, παραρτήματος
        /// </summary>
        [JsonPropertyName("activityCode")]
        public string ActivityCode { get; set; }
        /// <summary>
        /// Περιγραφή δραστηριότητας
        /// </summary>
        [JsonPropertyName("activityDescr")]
        public string ActivityDescr { get; set; }
        /// <summary>
        /// Στοιχεία διεύθυνσης παραρτήματος
        /// </summary>
        [JsonPropertyName("address")]
        public EGovKycAddress Address { get; set; }
    }

    public class Main
    {
        /// <summary>
        /// ΚΑΔ, κύριας δραστηριότητας
        /// </summary>
        [JsonPropertyName("activityCode")]
        public string ActivityCode { get; set; }
        /// <summary>
        /// Περιγραφή κύριας δραστηριότητας
        /// </summary>
        [JsonPropertyName("activityDescr")]
        public string ActivityDescr { get; set; }
        /// <summary>
        /// Στοιχεία διεύθυνσης Έδρας
        /// </summary>
        [JsonPropertyName("address")]
        public EGovKycAddress Address { get; set; }
        /// <summary>
        /// Πίνακας δραστηριοτήτων με τα παρακάτω στοιχεία
        /// </summary>
        public List<Activity> activities { get; set; }
    }

    public class Firm
    {
        /// <summary>
        /// Στοιχεία παραρτήματος (optional)
        /// </summary>
        [JsonPropertyName("department")]
        public Department Department { get; set; }
        /// <summary>
        /// Στοιχεία Έδρας
        /// </summary>
        [JsonPropertyName("main")]
        public Main Main { get; set; }
        /// <summary>
        /// Επωνυμία
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// ΑΦΜ επιχείρησης
        /// </summary>
        [JsonPropertyName("tin")]
        public string Tin { get; set; }
    }

    public class PrivateSectorOccupation
    {
        /// <summary>
        /// Κωδικός ειδικότητας. Ακολουθεί το πρότυπο ΣΤΕΠ-92
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; }
        /// <summary>
        /// Περιγραφή ειδικότητας. Ακολουθεί το πρότυπο ΣΤΕΠ-92
        /// </summary>
        [JsonPropertyName("descr")]
        public string Descr { get; set; }
        /// <summary>
        /// Τύπος/Πρότυπο κωδικοποίησης (ΣΤΕΠ-92)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class PublicSectorOccupation
    {
        /// <summary>
        /// Κωδικός κατηγορίας προσωπικού. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/EmployeeCategories
        /// </summary>
        [JsonPropertyName("categoryCode")]
        public string CategoryCode { get; set; }
        /// <summary>
        /// Περιγραφή κατηγορίας προσωπικού. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/EmployeeCategories
        /// </summary>
        [JsonPropertyName("categoryDescription")]
        public string CategoryDescription { get; set; }
        /// <summary>
        /// Κωδικός Κλάδου. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/ProfessionCategories
        /// </summary>
        [JsonPropertyName("sectorCode")]
        public string SectorCode { get; set; }
        /// <summary>
        /// Περιγραφή κλάδου. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/ProfessionCategories
        /// </summary>
        [JsonPropertyName("sectorDescription")]
        public string SectorDescription { get; set; }
        /// <summary>
        /// Κωδικός ειδικότητας. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/Specialities
        /// </summary>
        [JsonPropertyName("specialtyCode")]
        public string SpecialtyCode { get; set; }
        /// <summary>
        /// Περιγραφή ειδικότητας. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/Specialities
        /// </summary>
        [JsonPropertyName("specialtyDescription")]
        public string SpecialtyDescription { get; set; }
    }

    public class PrivateEmployeeInfo
    {
        public List<PrivateEmployeeInfoData> data { get; set; }
        [JsonPropertyName("result")]
        public GovGrKycResult Result { get; set; }
    }

    public class PrivateEmployeeInfoData
    {
        /// <summary>
        /// καθεστώς εργασίας, 0=Πλήρης, 1=Μερική και 2=Εκ περιτροπής
        /// </summary>
        [JsonPropertyName("contractType")]
        public string ContractType { get; set; }
        /// <summary>
        /// Επιχείρηση
        /// </summary>
        [JsonPropertyName("firm")]
        public Firm Firm { get; set; }
        /// <summary>
        /// Ειδικότητα (για ιδιωτικό υπάλληλο)
        /// </summary>
        [JsonPropertyName("occupation")]
        public PrivateSectorOccupation Occupation { get; set; }
    }

    public class EmploymentPosition
    {
        /// <summary>
        /// Επωνυμία φορέα
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// ΑΦΜ φορέα
        /// </summary>
        [JsonPropertyName("tin")]
        public string Tin { get; set; }
        /// <summary>
        /// Στοιχεία διεύθυνσης φορέα
        /// </summary>
        [JsonPropertyName("address")]
        public EGovKycAddress Address { get; set; }
    }

    public class WorkPosition
    {
        /// <summary>
        /// Επωνυμία φορέα
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// ΑΦΜ φορέα
        /// </summary>
        [JsonPropertyName("tin")]
        public string Tin { get; set; }
        /// <summary>
        /// Στοιχεία διεύθυνσης φορέα
        /// </summary>
        [JsonPropertyName("address")]
        public EGovKycAddress Address { get; set; }
    }

    public class PublicEmployeeInfo
    {
        public List<PublicEmployeeInfoData> data { get; set; }
        [JsonPropertyName("result")]
        public GovGrKycResult Result { get; set; }
    }

    public class PublicEmployeeInfoData
    {
        /// <summary>
        /// Ειδικότητα (για δημόσιο υπάλληλο)
        /// </summary>
        [JsonPropertyName("occupation")]
        public PublicSectorOccupation Occupation { get; set; }
        /// <summary>
        /// Ένδειξη (true/false) αν είναι η κύρια σχέση εργασίας
        /// </summary>
        [JsonPropertyName("primary")]
        public string Primary { get; set; }
        /// <summary>
        /// Στοιχεία φορέα οργανικής θέσης
        /// </summary>
        [JsonPropertyName("employmentPosition")]
        public EmploymentPosition EmploymentPosition { get; set; }
        /// <summary>
        /// Στοιχεία φορέα θέσης απασχόλησης (Optional)
        /// </summary>
        [JsonPropertyName("workPosition")]
        public WorkPosition WorkPosition { get; set; }
        /// <summary>
        /// Εργασιακή σχέση
        /// </summary>
        [JsonPropertyName("employmentType")]
        public string EmploymentType { get; set; }
    }

    public class Activity
    {
        /// <summary>
        /// ΚΑΔ
        /// </summary>
        [JsonPropertyName("activityCode")]
        public string ActivityCode { get; set; }
        /// <summary>
        /// Περιγραφή δραστηριότητας
        /// </summary>
        [JsonPropertyName("activityDescr")]
        public string ActivityDescr { get; set; }
        /// <summary>
        /// Τύπος δραστηριότητας (1 ή 2)
        /// </summary>
        [JsonPropertyName("activityType")]
        public string ActivityType { get; set; }
        /// <summary>
        /// Περιγραφή τύπου δραστηριότητας (ΚΥΡΙΑ ή ΔΕΥΤΕΡΕΥΟΥΣΑ)
        /// </summary>
        [JsonPropertyName("activityTypeDesc")]
        public string ActivityTypeDesc { get; set; }
    }

    public class SelfEmployedInfo
    {
        [JsonPropertyName("data")]
        public SelfEmployedInfoData Data { get; set; }
        [JsonPropertyName("result")]
        public GovGrKycResult Result { get; set; }
    }

    public class SelfEmployedInfoData
    {
        [JsonPropertyName("main")]
        public Main Main { get; set; }
        /// <summary>
        /// Επωνυμία
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// ΑΦΜ αυτοαπασχολούμενου (ίδιο με το principal)
        /// </summary>
        [JsonPropertyName("tin")]
        public string Tin { get; set; }
    }

    public class ProfessionalActivity
    {
        [JsonPropertyName("data")]
        public ProfessionalActivityData Data { get; set; }
        [JsonPropertyName("result")]
        public GovGrKycResult Result { get; set; }
    }

    public class ProfessionalActivityData
    {
        [JsonPropertyName("privateEmployeeInfo")]
        public PrivateEmployeeInfo PrivateEmployeeInfo { get; set; }
        [JsonPropertyName("publicEmployeeInfo")]
        public PublicEmployeeInfo PublicEmployeeInfo { get; set; }
        [JsonPropertyName("selfEmployedInfo")]
        public SelfEmployedInfo SelfEmployedInfo { get; set; }
    }

    public class ResponseData
    {
        /// <summary>
        /// Στοιχεία ταυτότητας
        /// </summary>
        [JsonPropertyName("identity")]
        public Identity Identity { get; set; }
        /// <summary>
        /// Στοιχεία επικοινωνίας
        /// </summary>
        [JsonPropertyName("contactInfo")]
        public ContactInfo ContactInfo { get; set; }
        /// <summary>
        /// Στοιχεία εισοδήματος
        /// </summary>
        [JsonPropertyName("income")]
        public Income Income { get; set; }
        /// <summary>
        /// Στοιχεία επαγγελματικής δραστηριότητας
        /// </summary>
        [JsonPropertyName("professionalActivity")]
        public ProfessionalActivity ProfessionalActivity { get; set; }
    }

    public class Response
    {
        /// <summary>
        /// Ο ΑΦΜ του χρήστη
        /// </summary>
        [JsonPropertyName("principal")]
        public string Principal { get; set; }
        [JsonPropertyName("data")]
        public ResponseData Data { get; set; }
        [JsonPropertyName("result")]
        public GovGrKycResult Result { get; set; }
    }

    /// <summary>
    /// The decoded payload from EGovKyc resource server's response
    /// </summary>
    public class EGovKycResponsePayload
    {
        [JsonPropertyName("response")]
        public Response Response { get; set; }
        [JsonPropertyName("iat")]
        public int Iat { get; set; }
        [JsonPropertyName("aud")]
        public string Aud { get; set; }
        [JsonPropertyName("jti")]
        public int Jti { get; set; }
        [JsonPropertyName("iss")]
        public string Iss { get; set; }
        [JsonPropertyName("sub")]
        public string Sub { get; set; }
        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
}