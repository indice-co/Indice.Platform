namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>
/// The fixed messages the risk investigation agent says to the user, in the language the user writes in. They are plain
/// strings on purpose: nothing a model could rephrase in a way that changes what a legal disclosure says.
/// </summary>
public sealed class RiskMessages
{
    /// <summary>The languages the agent speaks.</summary>
    public static readonly IReadOnlyList<string> Languages = ["el", "en"];

    private readonly bool _greek;

    private RiskMessages(string language) {
        Language = language;
        _greek = language == "el";
    }

    /// <summary>The language of these messages, <c>el</c> or <c>en</c>.</summary>
    public string Language { get; }

    /// <summary>The messages for a language. Anything but <c>el</c> or <c>en</c> falls back to <paramref name="fallback"/>, then to Greek.</summary>
    public static RiskMessages For(string? language, string? fallback = null) => new(Normalize(language) ?? Normalize(fallback) ?? "el");

    /// <summary>Reduces a language tag such as <c>el-GR</c> or <c>EN</c> to <c>el</c> or <c>en</c>, or <c>null</c> when it is neither.</summary>
    public static string? Normalize(string? language) {
        var code = language?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(code)) {
            return null;
        }
        code = code.Split('-', '_')[0];
        return Languages.Contains(code) ? code : null;
    }

    /// <summary>The name of the language in itself, for a prompt.</summary>
    public string LanguageName => _greek ? "Greek" : "English";

    /// <summary>Asks for both missing details.</summary>
    public string AskTunAndDate => _greek
        ? "Για να συντάξω τον φάκελο χρειάζομαι τον αριθμό συναλλαγής (TUN) και την ημερομηνία της συναλλαγής. Μπορείτε να μου τα δώσετε;"
        : "To prepare the report I need the transaction number (TUN) and the date of the transaction. Could you give me both?";

    /// <summary>Asks for the transaction number.</summary>
    public string AskTun => _greek
        ? "Ποιος είναι ο αριθμός συναλλαγής (TUN) της συναλλαγής;"
        : "What is the transaction number (TUN) of the transaction?";

    /// <summary>Asks for the date.</summary>
    public string AskDate => _greek
        ? "Ποια είναι η ημερομηνία της συναλλαγής (π.χ. 12/11/2026); Μια προσεγγιστική ημερομηνία αρκεί για να περιορίσω την αναζήτηση."
        : "What is the date of the transaction (for example 12/11/2026)? An approximate date is enough to narrow the search.";

    /// <summary>Tells the user a number is not shaped like a TUN, and asks again.</summary>
    public string TunInvalid(string tun) => _greek
        ? $"Ο αριθμός «{tun}» δεν έχει την αναμενόμενη μορφή αριθμού συναλλαγής (TUN). Ελέγξτε τον και στείλτε τον ξανά."
        : $"“{tun}” is not shaped like a transaction number (TUN). Please check it and send it again.";

    /// <summary>The user could not be helped within the attempt limit.</summary>
    public string TooManyAttempts => _greek
        ? "Δεν κατάφερα να συγκεντρώσω τα απαραίτητα στοιχεία. Όταν έχετε τον αριθμό συναλλαγής (TUN) και την ημερομηνία, ξεκινήστε ξανά το αίτημα."
        : "I could not gather the details I need. When you have the transaction number (TUN) and the date, start the request again.";

    /// <summary>The caller is not allowed to use the agent.</summary>
    public string Refused => _greek
        ? "Δεν έχετε πρόσβαση σε αυτή τη λειτουργία."
        : "You do not have access to this function.";

    /// <summary>The request matches a report type that is not available yet.</summary>
    public string Unsupported(string? playbookId) => _greek
        ? $"Προς το παρόν μπορώ να συντάξω μόνο φάκελο τεκμηρίωσης αμφισβητούμενης συναλλαγής (playbook P-02), για δικαστικές ή εισαγγελικές ανάγκες και αμφισβητήσεις πελατών. Το αίτημά σας αντιστοιχεί στο playbook {playbookId ?? "άγνωστο"}, που δεν είναι ακόμη διαθέσιμο."
        : $"For now I can only prepare the disputed-transaction evidence pack (playbook P-02), for court, prosecutor and customer-dispute use. Your request matches playbook {playbookId ?? "unknown"}, which is not available yet.";

    /// <summary>No transaction event was found. <paramref name="coverageNote"/> says what the coverage check showed.</summary>
    public string NotFound(string tun, string date, string coverageNote) => _greek
        ? $"Δεν βρήκα συμβάν συναλλαγής με TUN {tun} γύρω από τις {date}. {coverageNote} Ελέγξτε τον αριθμό και, αν θέλετε, δώστε μου άλλη ημερομηνία."
        : $"I could not find a transaction event with TUN {tun} around {date}. {coverageNote} Check the number and, if you like, give me another date.";

    /// <summary>The coverage probe found no risk events at all around the date.</summary>
    public string CoverageNone => _greek
        ? "Δεν υπάρχουν καθόλου καταγεγραμμένα συμβάντα ρίσκου στο διάστημα αυτό, οπότε το Risk API ενδέχεται να μην κατέγραφε τότε (L-01)."
        : "There are no risk events recorded at all in that period, so the Risk API may not have been collecting then (L-01).";

    /// <summary>The coverage probe found events, but none with the TUN.</summary>
    public string CoverageSome => _greek
        ? "Υπάρχουν καταγεγραμμένα συμβάντα ρίσκου στο διάστημα, αλλά κανένα με αυτό το TUN. Η απουσία συμβάντος δεν αποδεικνύει ότι η συναλλαγή δεν έγινε (L-05)."
        : "Risk events are recorded in that period, but none with this TUN. A missing event does not prove the transaction did not happen (L-05).";

    /// <summary>The coverage probe itself failed.</summary>
    public string CoverageUnknown => _greek
        ? "Δεν μπόρεσα να ελέγξω αν υπάρχουν καταγεγραμμένα συμβάντα στο διάστημα."
        : "I could not check whether any events are recorded in that period.";

    /// <summary>Announces the finished report.</summary>
    public string ReportReady => _greek
        ? "Ο φάκελος είναι έτοιμος. Μπορείτε να κατεβάσετε παρακάτω το αρχείο HTML (έτοιμο για εκτύπωση ή αποθήκευση ως PDF) και το αρχείο Markdown."
        : "The report is ready. You can download the HTML file below (print-ready, or save it as PDF) and the Markdown file.";
}
