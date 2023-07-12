using System.Text.Json.Serialization;

namespace Indice.Features.GovGr.Models;

/// <summary></summary>
public class BusinessRegistryRecord
{
    /// <summary>ΕΝΔΕΙΞΗ ΑΠΕΝΕΡΓΟΠΟΙΗΜΕΝΟΣ ΑΦΜ ΠΕΡΙΓΡΑΦΗ): ΕΝΕΡΓΟΣ ΑΦΜ, ΑΠΕΝΕΡΓΟΠΟΙΗΜΕΝΟΣ ΑΦΜ)</summary>
    public string DeactivationFlagDescription { get; set; }
    /// <summary>ΕΝΔΕΙΞΗ ΑΠΕΝΕΡΓΟΠΟΙΗΜΕΝΟΣ ΑΦΜ (1=ΕΝΕΡΓΟΣ ΑΦΜ, 2=ΑΠΕΝΕΡΓΟΠΟΙΗΜΕΝΟΣ ΑΦΜ)</summary>
    public string DeactivationFlag { get; set; }
    /// <summary>ΠΕΡΙΓΡΑΦΗ ΜΟΡΦΗΣ ΜΗ Φ.Π.</summary>
    public string LegalStatusDescription { get; set; }
    /// <summary>ΕΠΩΝΥΜΙΑ</summary>
    public string LegalName { get; set; }
    /// <summary>ΤΙΤΛΟΣ ΕΠΙΧΕΙΡΗΣΗΣ</summary>
    public string CommercialTitle { get; set; }
    /// <summary>ΚΩΔΙΚΟΣ ΔΟΥ</summary>
    public string TaxOfficeCode { get; set; }
    /// <summary>ΠΕΡΙΓΡΑΦΗ ΔΟΥ</summary>
    public string TaxOfficeDescription { get; set; }
    /// <summary>Κωδικός Κύρια δραστηριότητας Taxis</summary>
    public decimal? MainActivityCode { get; set; }
    /// <summary>Περιγραφή κύριας δραστηριότητας Taxis </summary>
    public string MainActivityDescription { get; set; }
    /// <summary>Τύπος Δραστηριότητας (Πάντα 1) </summary>
    public string MainActivityKind { get; set; }
    /// <summary>Διεύθυνση</summary>
    public BusinessRegistryAddress Address { get; set; }
    /// <summary>ΗΜ/ΝΙΑ ΕΝΑΡΞΗΣ</summary>
    public DateTime? RegisterDate { get; set; }
    /// <summary>ΗΜ/ΝΙΑ ΔΙΑΚΟΠΗΣ</summary>
    public DateTime? StopDate { get; set; }
    /// <summary>ΤΙΜΕΣ: (ΕΠΙΤΗΔΕΥΜΑΤΙΑΣ, ΜΗ ΕΠΙΤΗΔΕΥΜΑΤΙΑΣ,ΠΡΩΗΝ ΕΠΙΤΗΔΕΥΜΑΤΙΑΣ)</summary>
    public string FirmFlagDescription { get; set; }
    /// <summary>ΦΥΣΙΚΟ /ΜΗ ΦΥΣΙΚΟ ΠΡΟΣΩΠΟ </summary>
    public string PersonStatusDescription { get; set; }
}
/// <summary>Business Registry Address model</summary>
public class BusinessRegistryAddress {
    /// <summary>πόλη</summary>
    public string City { get; set; }
    /// <summary>κωδικός χώρας</summary>
    public string Country { get; set; }
    /// <summary>αριθμός</summary>
    public string Number { get; set; }
    /// <summary>οδός</summary>
    public string Street { get; set; }
    /// <summary>Τ.Κ.</summary>
    public string PostalCode { get; set; }
}
