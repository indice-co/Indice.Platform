//----------------------
// This file has been created with the help of GSIS official technical documentation
// and the tools described here: https://stackoverflow.com/questions/21611674/how-to-auto-generate-a-c-sharp-class-file-from-a-json-string
//----------------------

using System.Collections.Generic;
using Indice.Integration.EGov.Kyc.Enums;

namespace Indice.Integration.EGov.Kyc.Models
{
    /// <summary>
    /// The encoded response from the EGovKyc resource server
    /// </summary>
    public class KycResponse
    {
        public string payload { get; set; }
        public string @protected { get; set; }
        public string signature { get; set; }
    }

    public class DocumentInfo
    {
        /// <summary>
        /// Ημερομηνία λήξης
        /// </summary>
        public string expireDate { get; set; }
        /// <summary>
        /// Μοναδικός αριθμός εγγράφου, ο ΑΔΤ
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// Ημερομηνία έκδοσης εγγράφου
        /// </summary>
        public string issueDate { get; set; }
        /// <summary>
        /// Κωδικός Αρχής έκδοσης
        /// </summary>
        public string issuerId { get; set; }
        /// <summary>
        /// Αρχή έκδοσης
        /// </summary>
        public string issuerName { get; set; }
        /// <summary>
        /// Τύπος εγγράφου, π.χ. prado
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// Είδος εγγράφου
        /// </summary>
        public string category { get; set; }
        /// <summary>
        /// Προηγούμενος Μοναδικός αριθμός εγγράφου
        /// </summary>
        public string previousId { get; set; }
    }

    public class UserInfo
    {
        /// <summary>
        /// Ημερομηνία γέννησης
        /// </summary>
        public string birthDate { get; set; }
        /// <summary>
        /// Έτος γέννησης
        /// </summary>
        public string birthYear { get; set; }
        /// <summary>
        /// Τοποθεσία γέννησης
        /// </summary>
        public string birthPlace { get; set; }
        /// <summary>
        /// Πατρώνυμο
        /// </summary>
        public string fatherName { get; set; }
        /// <summary>
        /// Απόδοση με λατινικούς χαρακτήρες του fatherName
        /// </summary>
        public string fatherNameLatin { get; set; }
        /// <summary>
        /// Φύλο
        /// </summary>
        public string gender { get; set; }
        /// <summary>
        /// Φωτογραφία
        /// </summary>
        public string image { get; set; }
        /// <summary>
        /// τύπος/κωδικοποίηση φωτογραφίας
        /// </summary>
        public string imageMimeType { get; set; }
        /// <summary>
        /// Μητρώνυμο
        /// </summary>
        public string motherName { get; set; }
        /// <summary>
        /// Απόδοση με λατινικούς χαρακτήρες του motherName
        /// </summary>
        public string motherNameLatin { get; set; }
        /// <summary>
        /// Όνομα
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Απόδοση με λατινικούς χαρακτήρες του name
        /// </summary>
        public string nameLatin { get; set; }
        /// <summary>
        /// Επώνυμο
        /// </summary>
        public string surname { get; set; }
        /// <summary>
        /// Απόδοση με λατινικούς χαρακτήρες του surname
        /// </summary>
        public string surnameLatin { get; set; }
    }

    public class Result
    {
        public string message { get; set; }
        public KycStatusCode status { get; set; }
    }

    public class GrcBo
    {
        public GrcBoData data { get; set; }
        public Result result { get; set; }
    }

    public class GrcBoData
    {
        /// <summary>
        /// Στοιχεία σχετικά με το έγγραφο επιβεβαίωσης
        /// </summary>
        public DocumentInfo documentInfo { get; set; }
        /// <summary>
        /// Στοιχεία σχετικά με τον χρήστη
        /// </summary>
        public UserInfo userInfo { get; set; }
    }

    public class Identity
    {
        public IdentityData data { get; set; }
        public Result result { get; set; }
    }

    public class IdentityData
    {
        public GrcBo grcBo { get; set; }
    }

    public class EGovKycAddress
    {
        /// <summary>
        /// πόλη
        /// </summary>
        public string city { get; set; }
        /// <summary>
        /// κωδικός χώρας
        /// </summary>
        public string country { get; set; }
        /// <summary>
        /// πίνακας με τα στοιχεία διεύθυνσης “Οδός και αριθμός”, “Τ.Κ”, “Πόλη”
        /// </summary>
        public List<string> lines { get; set; }
        /// <summary>
        /// αριθμός
        /// </summary>
        public string number { get; set; }
        /// <summary>
        /// οδός
        /// </summary>
        public string street { get; set; }
        /// <summary>
        /// Τ.Κ.
        /// </summary>
        public string postalCode { get; set; }
    }

    public class ContactInfo
    {
        public ContactInfoData data { get; set; }
        public Result result { get; set; }
    }

    public class ContactInfoData
    {
        /// <summary>
        /// στοιχεία διεύθυνσης διαμονής
        /// </summary>
        public EGovKycAddress address { get; set; }
        /// <summary>
        /// στοιχεία διεύθυνσης επικοινωνίας
        /// </summary>
        public EGovKycAddress contactAddress { get; set; }
        /// <summary>
        /// ηλεκτρονική διεύθυνση αλληλογραφίας
        /// </summary>
        public string email { get; set; }
        /// <summary>
        /// κινητό τηλέφωνο
        /// </summary>
        public string mobile { get; set; }
        /// <summary>
        /// σταθερό τηλέφωνο
        /// </summary>
        public string telephone { get; set; }
    }

    public class Income
    {
        public IncomeData data { get; set; }
        public Result result { get; set; }
    }

    public class IncomeData
    {
        /// <summary>
        /// Προστιθέμενη διαφορά δαπανών
        /// </summary>
        public string addedCostDiffs { get; set; }
        /// <summary>
        /// Επιχειρηματική δραστηριότητα
        /// </summary>
        public string businessIncome { get; set; }
        /// <summary>
        /// Υπεραξία μεταβίβασης κεφαλαίου
        /// </summary>
        public string capitalTransferValue { get; set; }
        /// <summary>
        /// Ζημιές από επιχειρηματική δραστηριότητα, ιδίου έτους
        /// </summary>
        public string damageBusinessCurr { get; set; }
        /// <summary>
        /// Ζημιές από επιχειρηματική δραστηριότητα, προηγούμενων ετών
        /// </summary>
        public string damageBusinessPrev { get; set; }
        /// <summary>
        /// Ζημιές από αγροτική δραστηριότητα, ιδίου έτους
        /// </summary>
        public string damageFarmingCurr { get; set; }
        /// <summary>
        /// Ζημιές από αγροτική δραστηριότητα, προηγούμενων ετών
        /// </summary>
        public string damageFarmingPrev { get; set; }
        /// <summary>
        /// Εισόδημα από αγροτική δραστηριότητα
        /// </summary>
        public string farmingIncome { get; set; }
        /// <summary>
        /// Ακαθάριστα Έσοδα, από επιχειρηματική δραστηριότητα
        /// </summary>
        public string grossBusiness { get; set; }
        /// <summary>
        /// Ακαθάριστα Έσοδα, από αγροτική δραστηριότητα
        /// </summary>
        public string grossFarming { get; set; }
        /// <summary>
        /// Εισόδημα από Μερίσματα – Τόκους - Δικαιώματα
        /// </summary>
        public string investmentIncome { get; set; }
        /// <summary>
        /// Ναυτικό εισόδημα
        /// </summary>
        public string maritimeIncome { get; set; }
        /// <summary>
        /// Έτος αναφοράς
        /// </summary>
        public string refYear { get; set; }
        /// <summary>
        /// Ημερμηνία έκδοσης πράξης
        /// </summary>
        public string releaseDate { get; set; }
        /// <summary>
        /// Ακίνητη περιουσία
        /// </summary>
        public string rentalIncome { get; set; }
        /// <summary>
        /// Αυτοτελώς Φορολογούμενα Ποσά
        /// </summary>
        public string taxableAmounts { get; set; }
        /// <summary>
        /// Επίδομα ανεργίας
        /// </summary>
        public string unemploymentBenefits { get; set; }
        /// <summary>
        /// Μισθωτή Εργασία – Συντάξεις
        /// </summary>
        public string wagesPensionsIncome { get; set; }
    }

    public class Department
    {
        /// <summary>
        /// ΚΑΔ, παραρτήματος
        /// </summary>
        public string activityCode { get; set; }
        /// <summary>
        /// Περιγραφή δραστηριότητας
        /// </summary>
        public string activityDescr { get; set; }
        /// <summary>
        /// Στοιχεία διεύθυνσης παραρτήματος
        /// </summary>
        public EGovKycAddress address { get; set; }
    }

    public class Main
    {
        /// <summary>
        /// ΚΑΔ, κύριας δραστηριότητας
        /// </summary>
        public string activityCode { get; set; }
        /// <summary>
        /// Περιγραφή κύριας δραστηριότητας
        /// </summary>
        public string activityDescr { get; set; }
        /// <summary>
        /// Στοιχεία διεύθυνσης Έδρας
        /// </summary>
        public EGovKycAddress address { get; set; }
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
        public Department department { get; set; }
        /// <summary>
        /// Στοιχεία Έδρας
        /// </summary>
        public Main main { get; set; }
        /// <summary>
        /// Επωνυμία
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// ΑΦΜ επιχείρησης
        /// </summary>
        public string tin { get; set; }
    }

    public class PrivateSectorOccupation
    {
        /// <summary>
        /// Κωδικός ειδικότητας. Ακολουθεί το πρότυπο ΣΤΕΠ-92
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// Περιγραφή ειδικότητας. Ακολουθεί το πρότυπο ΣΤΕΠ-92
        /// </summary>
        public string descr { get; set; }
        /// <summary>
        /// Τύπος/Πρότυπο κωδικοποίησης (ΣΤΕΠ-92)
        /// </summary>
        public string type { get; set; }
    }

    public class PublicSectorOccupation
    {
        /// <summary>
        /// Κωδικός κατηγορίας προσωπικού. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/EmployeeCategories
        /// </summary>
        public string categoryCode { get; set; }
        /// <summary>
        /// Περιγραφή κατηγορίας προσωπικού. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/EmployeeCategories
        /// </summary>
        public string categoryDescription { get; set; }
        /// <summary>
        /// Κωδικός Κλάδου. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/ProfessionCategories
        /// </summary>
        public string sectorCode { get; set; }
        /// <summary>
        /// Περιγραφή κλάδου. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/ProfessionCategories
        /// </summary>
        public string sectorDescription { get; set; }
        /// <summary>
        /// Κωδικός ειδικότητας. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/Specialities
        /// </summary>
        public string specialtyCode { get; set; }
        /// <summary>
        /// Περιγραφή ειδικότητας. check: https://hr.apografi.gov.gr/api/public/metadata/dictionary/Specialities
        /// </summary>
        public string specialtyDescription { get; set; }
    }

    public class PrivateEmployeeInfo
    {
        public List<PrivateEmployeeInfoData> data { get; set; }
        public Result result { get; set; }
    }

    public class PrivateEmployeeInfoData
    {
        /// <summary>
        /// καθεστώς εργασίας, 0=Πλήρης, 1=Μερική και 2=Εκ περιτροπής
        /// </summary>
        public string contractType { get; set; }
        /// <summary>
        /// Επιχείρηση
        /// </summary>
        public Firm firm { get; set; }
        /// <summary>
        /// Ειδικότητα (για ιδιωτικό υπάλληλο)
        /// </summary>
        public PrivateSectorOccupation occupation { get; set; }
    }

    public class EmploymentPosition
    {
        /// <summary>
        /// Επωνυμία φορέα
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// ΑΦΜ φορέα
        /// </summary>
        public string tin { get; set; }
        /// <summary>
        /// Στοιχεία διεύθυνσης φορέα
        /// </summary>
        public EGovKycAddress address { get; set; }
    }

    public class WorkPosition
    {
        /// <summary>
        /// Επωνυμία φορέα
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// ΑΦΜ φορέα
        /// </summary>
        public string tin { get; set; }
        /// <summary>
        /// Στοιχεία διεύθυνσης φορέα
        /// </summary>
        public EGovKycAddress address { get; set; }
    }

    public class PublicEmployeeInfo
    {
        public List<PublicEmployeeInfoData> data { get; set; }
        public Result result { get; set; }
    }

    public class PublicEmployeeInfoData
    {
        /// <summary>
        /// Ειδικότητα (για δημόσιο υπάλληλο)
        /// </summary>
        public PublicSectorOccupation occupation { get; set; }
        /// <summary>
        /// Ένδειξη (true/false) αν είναι η κύρια σχέση εργασίας
        /// </summary>
        public string primary { get; set; }
        /// <summary>
        /// Στοιχεία φορέα οργανικής θέσης
        /// </summary>
        public EmploymentPosition employmentPosition { get; set; }
        /// <summary>
        /// Στοιχεία φορέα θέσης απασχόλησης (Optional)
        /// </summary>
        public WorkPosition workPosition { get; set; }
        /// <summary>
        /// Εργασιακή σχέση
        /// </summary>
        public string employmentType { get; set; }
    }

    public class Activity
    {
        /// <summary>
        /// ΚΑΔ
        /// </summary>
        public string activityCode { get; set; }
        /// <summary>
        /// Περιγραφή δραστηριότητας
        /// </summary>
        public string activityDescr { get; set; }
        /// <summary>
        /// Τύπος δραστηριότητας (1 ή 2)
        /// </summary>
        public string activityType { get; set; }
        /// <summary>
        /// Περιγραφή τύπου δραστηριότητας (ΚΥΡΙΑ ή ΔΕΥΤΕΡΕΥΟΥΣΑ)
        /// </summary>
        public string activityTypeDesc { get; set; }
    }

    public class SelfEmployedInfo
    {
        public SelfEmployedInfoData data { get; set; }
        public Result result { get; set; }
    }

    public class SelfEmployedInfoData
    {
        public Main main { get; set; }
        /// <summary>
        /// Επωνυμία
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// ΑΦΜ αυτοαπασχολούμενου (ίδιο με το principal)
        /// </summary>
        public string tin { get; set; }
    }

    public class ProfessionalActivity
    {
        public ProfessionalActivityData data { get; set; }
        public Result result { get; set; }
    }

    public class ProfessionalActivityData
    {
        public PrivateEmployeeInfo privateEmployeeInfo { get; set; }
        public PublicEmployeeInfo publicEmployeeInfo { get; set; }
        public SelfEmployedInfo selfEmployedInfo { get; set; }
    }

    public class ResponseData
    {
        /// <summary>
        /// Στοιχεία ταυτότητας
        /// </summary>
        public Identity identity { get; set; }
        /// <summary>
        /// Στοιχεία επικοινωνίας
        /// </summary>
        public ContactInfo contactInfo { get; set; }
        /// <summary>
        /// Στοιχεία εισοδήματος
        /// </summary>
        public Income income { get; set; }
        /// <summary>
        /// Στοιχεία επαγγελματικής δραστηριότητας
        /// </summary>
        public ProfessionalActivity professionalActivity { get; set; }
    }

    public class Response
    {
        /// <summary>
        /// Ο ΑΦΜ του χρήστη
        /// </summary>
        public string principal { get; set; }
        public ResponseData data { get; set; }
        public Result result { get; set; }
    }

    /// <summary>
    /// The decoded payload from EGovKyc resource server's response
    /// </summary>
    public class EGovKycResponsePayload
    {
        public Response response { get; set; }
        public int iat { get; set; }
        public string aud { get; set; }
        public int jti { get; set; }
        public string iss { get; set; }
        public string sub { get; set; }
        public string version { get; set; }
    }
}