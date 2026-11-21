namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>
/// The fixed wording of the evidence report, in Greek and English. Headings, column names and the sentences that describe a
/// comparison are fixed text, not model output, so a legal reader can rely on them reading the same every time.
/// </summary>
internal sealed class RiskReportLabels
{
    private static readonly Dictionary<string, (string El, string En)> Text = new(StringComparer.Ordinal) {
        // Document
        ["title"] = ("Φάκελος τεκμηρίωσης συναλλαγής", "Transaction evidence report"),
        ["reportId"] = ("Κωδικός αναφοράς", "Report ID"),
        ["generated"] = ("Δημιουργήθηκε", "Generated"),
        ["requestedBy"] = ("Ζητήθηκε από", "Requested by"),
        ["purpose"] = ("Σκοπός", "Purpose"),
        ["tun"] = ("Αριθμός συναλλαγής (TUN)", "Transaction number (TUN)"),
        ["anchorEvent"] = ("Συμβάν αναφοράς", "Anchor event"),
        ["timeNote"] = ("Οι ώρες εμφανίζονται σε UTC και σε ώρα {0}.", "Times are shown in UTC and in {0} time."),
        ["summary"] = ("Σύνοψη ευρημάτων", "Findings summary"),
        ["summaryUnavailable"] = ("Η σύνοψη δεν ήταν διαθέσιμη αυτή τη στιγμή. Οι ενότητες που ακολουθούν είναι πλήρεις και δεν εξαρτώνται από αυτήν.", "The summary was not available at this time. The sections below are complete and do not depend on it."),
        ["footer"] = ("Κωδικός αναφοράς", "Report ID"),
        ["summaryNote"] = ("Αυτόματα παραγόμενη σύνοψη. Επαληθεύστε την με τους πίνακες που ακολουθούν και με τα ερωτήματα που αναφέρονται ως [E-n].", "Automatically generated summary. Verify it against the tables below and the queries cited as [E-n]."),
        ["badCitation"] = ("Προσοχή: η σύνοψη αναφέρει πηγές που δεν υπάρχουν στο παράρτημα: {0}.", "Warning: the summary cites sources that are not in the annex: {0}."),
        // Sections
        ["customer"] = ("Πελάτης", "Customer"),
        ["transaction"] = ("Η συναλλαγή", "The transaction"),
        ["authentication"] = ("Στοιχεία ταυτοποίησης", "Authentication evidence"),
        ["otp"] = ("Αποστολή κωδικών και ειδοποιήσεων", "One-time passwords and notifications"),
        ["network"] = ("Συσκευή και δίκτυο", "Device and network"),
        ["changes"] = ("Προηγούμενες αλλαγές", "Prior changes"),
        ["coverage"] = ("Κάλυψη δεδομένων", "Data coverage"),
        ["limitations"] = ("Περιορισμοί", "Limitations"),
        ["annex"] = ("Παράρτημα Α: ερωτήματα που εκτελέστηκαν", "Annex A: queries executed"),
        // Fixed sentences
        ["source"] = ("Πηγή", "Source"),
        ["incomplete"] = ("Ελλιπές", "Incomplete"),
        ["notRun"] = ("Το ερώτημα δεν εκτελέστηκε.", "The query was not run."),
        ["none"] = ("Δεν βρέθηκαν εγγραφές.", "No records found."),
        ["truncated"] = ("Το αποτέλεσμα περικόπηκε στο όριο γραμμών του διακομιστή· υπάρχουν περισσότερες γραμμές από όσες εμφανίζονται.", "The result was cut at the server's row limit; more rows exist than are shown."),
        ["noUser"] = ("Δεν βρέθηκε εγγραφή χρήστη.", "No user record was found."),
        ["noLogin"] = ("Δεν βρέθηκε σύνδεση για τη συνεδρία στο διάστημα που ελέγχθηκε.", "No login was found for the session in the period checked."),
        ["noSession"] = ("Το συμβάν δεν έχει αναγνωριστικό συνεδρίας.", "The event has no session id."),
        ["deviceRegistered"] = ("Η συσκευή της σύνδεσης {0} αντιστοιχεί σε εγγεγραμμένη συσκευή του πελάτη.", "The device of the login {0} matches a device registered to the customer."),
        ["deviceNotRegistered"] = ("Η σύνδεση {0} δεν αντιστοιχίστηκε σε εγγεγραμμένη συσκευή του πελάτη.", "The login {0} did not match a device registered to the customer."),
        ["otpCount"] = ("Βρέθηκαν {0} ειδοποιήσεις προς τον πελάτη από {1} έως {2}.", "{0} notification(s) were sent to the customer from {1} to {2}."),
        ["ipSeen"] = ("Η διεύθυνση IP της συναλλαγής ({0}) εμφανίζεται στις τελευταίες {1} ημέρες πριν τη συναλλαγή: {2} συμβάντα, πρώτη φορά {3}, τελευταία {4}.", "The transaction's IP address ({0}) appears in the {1} days before the transaction: {2} event(s), first seen {3}, last seen {4}."),
        ["ipNew"] = ("Η διεύθυνση IP της συναλλαγής ({0}) δεν εμφανίζεται σε συμβάντα ρίσκου των τελευταίων {1} ημερών πριν τη συναλλαγή.", "The transaction's IP address ({0}) does not appear in risk events in the {1} days before the transaction."),
        ["countrySeen"] = ("Η χώρα της IP ({0}) εμφανίζεται στο ιστορικό της περιόδου.", "The IP's country ({0}) appears in the history of the period."),
        ["countryNew"] = ("Η χώρα της IP ({0}) δεν εμφανίζεται στο ιστορικό της περιόδου.", "The IP's country ({0}) does not appear in the history of the period."),
        ["changesFound"] = ("Βρέθηκαν {0} αλλαγές διαπιστευτηρίων ή προφίλ στο ρίσκο και {1} εγγραφές δραστηριότητας Identity τις προηγούμενες {2} ημέρες.", "{0} credential or profile change(s) in the risk events and {1} Identity activity record(s) were found in the previous {2} days."),
        ["signInCoverage"] = ("Η σύνδεση της συνεδρίας ({0}) βρίσκεται {1} της κάλυψης των logs συνδέσεων (παλαιότερη εγγραφή: {2}).", "The login of the session ({0}) is {1} the sign-in log coverage (oldest record: {2})."),
        ["within"] = ("εντός", "within"),
        ["before"] = ("πριν από την έναρξη", "before the start of"),
        ["dataNotes"] = ("Σημειώσεις για τα δεδομένα", "Notes on the data"),
        ["limitationText"] = ("Κείμενο της βάσης γνώσης antifraud, αυτούσιο (στα αγγλικά)", "Text of the antifraud knowledge base, verbatim"),
        ["limitationMissing"] = ("Το κείμενο δεν βρέθηκε στη βάση γνώσης.", "The text was not found in the knowledge base."),
        ["reportLimitations"] = ("Περιορισμοί αυτής της αναφοράς", "Limitations of this report"),
        ["masked"] = ("Στοιχεία επικοινωνίας και φορολογικός αριθμός εμφανίζονται μερικώς κρυμμένα· οι πλήρεις τιμές βρίσκονται στις εγγραφές της βάσης που παραπέμπουν τα ερωτήματα.", "Contact details and the tax id are partially masked; the full values are in the database rows the queries cite."),
        ["maskedOtp"] = ("Σε κείμενα μηνυμάτων, ακολουθίες 4 έως 8 ψηφίων κρύβονται ως πιθανοί κωδικοί μίας χρήσης. Κωδικοί, μυστικά και διαπιστευτήρια έχουν αφαιρεθεί από τα δεδομένα των συμβάντων.", "In message texts, runs of 4 to 8 digits are masked as possible one-time passwords. Codes, secrets and credentials were removed from the event data."),
        ["scoreNotVerdict"] = ("Η βαθμολογία ρίσκου είναι αποτέλεσμα κανόνων και όχι κρίση για απάτη (L-06).", "A risk score is the output of rules, not a verdict of fraud (L-06)."),
        ["failedQueries"] = ("{0} ερώτημα/ερωτήματα απέτυχαν· οι αντίστοιχες ενότητες είναι ελλιπείς.", "{0} query(ies) failed; the corresponding sections are incomplete."),
        ["truncatedQueries"] = ("{0} αποτέλεσμα/τα περικόπηκαν· οι πίνακες δεν δείχνουν όλες τις γραμμές.", "{0} result(s) were truncated; the tables do not show every row."),
        ["notVerdict"] = ("Η παρούσα αναφορά παραθέτει δεδομένα καταγραφής και δεν αποτελεί νομική ή πραγματογνωμοσύνη. Το TUN παραπέμπει σε εγγραφές του core banking που δεν περιλαμβάνονται.", "This report presents log data and is not a legal opinion or an expert report. The TUN refers to core banking records that are not included."),
        // Columns
        ["c.time"] = ("Ώρα", "Time"), ["c.event"] = ("Συμβάν", "Event"), ["c.type"] = ("Τύπος", "Type"), ["c.amount"] = ("Ποσό", "Amount"),
        ["c.level"] = ("Επίπεδο", "Level"), ["c.score"] = ("Βαθμός", "Score"), ["c.rule"] = ("Κανόνας", "Rule"), ["c.ip"] = ("IP", "IP"),
        ["c.country"] = ("Χώρα", "Country"), ["c.location"] = ("Τοποθεσία", "Location"), ["c.device"] = ("Συσκευή", "Device"),
        ["c.session"] = ("Συνεδρία", "Session"), ["c.application"] = ("Εφαρμογή", "Application"), ["c.result"] = ("Αποτέλεσμα", "Result"),
        ["c.action"] = ("Ενέργεια", "Action"), ["c.methods"] = ("Μέθοδοι", "Methods"), ["c.registered"] = ("Εγγεγραμμένη συσκευή", "Registered device"),
        ["c.name"] = ("Όνομα", "Name"), ["c.model"] = ("Μοντέλο", "Model"), ["c.os"] = ("Λειτουργικό", "OS"), ["c.created"] = ("Δημιουργία", "Created"),
        ["c.lastSignIn"] = ("Τελευταία σύνδεση", "Last sign-in"), ["c.trusted"] = ("Έμπιστη", "Trusted"), ["c.blocked"] = ("Αποκλεισμένη", "Blocked"),
        ["c.id"] = ("Αναγνωριστικό", "Id"), ["c.usedHere"] = ("Σε χρήση σε αυτή τη συνεδρία", "Used in this session"),
        ["c.channel"] = ("Κανάλι", "Channel"), ["c.recipient"] = ("Παραλήπτης", "Recipient"), ["c.tracking"] = ("Tracking Id", "Tracking Id"),
        ["c.message"] = ("Μήνυμα (κωδικοί κρυμμένοι)", "Message (codes masked)"), ["c.source"] = ("Πηγή", "Source"), ["c.category"] = ("Κατηγορία", "Category"),
        ["c.actor"] = ("Εκτελών", "Actor"), ["c.target"] = ("Στόχος", "Target"), ["c.count"] = ("Πλήθος", "Count"), ["c.first"] = ("Πρώτη φορά", "First seen"),
        ["c.last"] = ("Τελευταία φορά", "Last seen"), ["c.oldest"] = ("Παλαιότερη", "Oldest"), ["c.newest"] = ("Νεότερη", "Newest"), ["c.table"] = ("Πίνακας", "Table"),
        ["c.field"] = ("Πεδίο", "Field"), ["c.value"] = ("Τιμή", "Value"), ["c.claim"] = ("Στοιχείο", "Claim"), ["c.query"] = ("Ερώτημα", "Query"),
        ["c.database"] = ("Βάση", "Database"), ["c.executed"] = ("Εκτελέστηκε (UTC)", "Executed (UTC)"), ["c.parameters"] = ("Παράμετροι", "Parameters"),
        ["c.rows"] = ("Γραμμές", "Rows"), ["c.status"] = ("Κατάσταση", "Status"), ["c.sha"] = ("SHA-256 του SQL", "SQL SHA-256"), ["c.ok"] = ("Επιτυχές", "OK"),
        ["c.failed"] = ("Απέτυχε", "Failed"), ["c.truncatedShort"] = ("Περικόπηκε", "Truncated"), ["c.yes"] = ("Ναι", "Yes"), ["c.no"] = ("Όχι", "No"),
        ["c.success"] = ("Επιτυχία", "Success"), ["c.failure"] = ("Αποτυχία", "Failure"),
        // Customer fields
        ["f.userName"] = ("Όνομα χρήστη", "User name"), ["f.email"] = ("Email", "Email"), ["f.phone"] = ("Τηλέφωνο", "Phone"),
        ["f.emailConfirmed"] = ("Επιβεβαιωμένο email", "Email confirmed"), ["f.phoneConfirmed"] = ("Επιβεβαιωμένο τηλέφωνο", "Phone confirmed"),
        ["f.twoFactor"] = ("Έλεγχος ταυτότητας δύο παραγόντων", "Two-factor enabled"), ["f.blocked"] = ("Αποκλεισμένος χρήστης", "User blocked"),
        ["f.created"] = ("Δημιουργία λογαριασμού", "Account created"), ["f.lastSignIn"] = ("Τελευταία σύνδεση", "Last sign-in"),
        ["f.lastPassword"] = ("Τελευταία αλλαγή κωδικού", "Last password change"), ["f.subject"] = ("Αναγνωριστικό χρήστη (SubjectId)", "User id (SubjectId)"),
        // Anchor fields
        ["f.eventId"] = ("Αναγνωριστικό συμβάντος", "Event id"), ["f.recorded"] = ("Καταγράφηκε", "Recorded"), ["f.name"] = ("Συμβάν", "Event"),
        ["f.type"] = ("Τύπος", "Type"), ["f.amount"] = ("Ποσό", "Amount"), ["f.ip"] = ("Διεύθυνση IP", "IP address"), ["f.country"] = ("Χώρα (GeoIP)", "Country (GeoIP)"),
        ["f.location"] = ("Τοποθεσία (GeoIP)", "Location (GeoIP)"), ["f.source"] = ("Πηγή", "Source"), ["f.session"] = ("Συνεδρία", "Session"),
        ["h.eventData"] = ("Δεδομένα συμβάντος (μετά την αφαίρεση μυστικών)", "Event data (after removal of secrets)"),
        ["h.chain"] = ("Βήματα της συναλλαγής στη συνεδρία", "Steps of the transaction in the session"),
        ["h.scores"] = ("Ανάλυση βαθμολογίας ρίσκου", "Risk score breakdown"),
        ["h.sameDay"] = ("Προσπάθειες της ίδιας ημέρας που δεν ολοκληρώθηκαν (αποκλεισμένες ή εγκαταλειμμένες)", "Attempts the same day that did not complete (blocked or abandoned)"),
        ["h.login"] = ("Σύνδεση της συνεδρίας", "Login of the session"), ["h.devices"] = ("Εγγεγραμμένες συσκευές του πελάτη", "Devices registered to the customer"),
        ["h.notifications"] = ("Ειδοποιήσεις προς τον πελάτη", "Notifications sent to the customer"), ["h.sia"] = ("Επαλήθευση κάρτας (SIA)", "Card verification (SIA)"),
        ["h.usualIps"] = ("IP και χώρες στα συμβάντα ρίσκου του πελάτη", "IPs and countries in the customer's risk events"),
        ["h.usualLogins"] = ("Τοποθεσίες επιτυχημένων συνδέσεων", "Locations of successful logins"),
        ["h.riskChanges"] = ("Αλλαγές στα συμβάντα ρίσκου", "Changes in the risk events"), ["h.activity"] = ("Δραστηριότητα Identity (activity log)", "Identity activity log"),
        ["h.claims"] = ("Στοιχεία πελάτη (claims)", "Customer details (claims)"), ["h.riskCoverage"] = ("Συμβάντα ρίσκου ανά πηγή", "Risk events by source"),
        ["h.signCoverage"] = ("Logs συνδέσεων", "Sign-in logs"), ["h.activityCoverage"] = ("Activity log", "Activity log"),
    };

    private readonly bool _greek;

    private RiskReportLabels(bool greek) {
        _greek = greek;
    }

    /// <summary>The labels for a language, <c>el</c> or <c>en</c>.</summary>
    public static RiskReportLabels For(string? language) => new(RiskMessages.Normalize(language) != "en");

    /// <summary>The label for a key.</summary>
    public string this[string key] => Text.TryGetValue(key, out var pair) ? (_greek ? pair.El : pair.En) : key;

    /// <summary>The label for a key with its placeholders filled in.</summary>
    public string Format(string key, params object?[] values) => string.Format(System.Globalization.CultureInfo.InvariantCulture, this[key], values);
}
