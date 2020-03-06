using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Naming",
    "CA1707:Identifiers should not contain underscores",
    Justification = "Test methods should use '_' in names.")]
[assembly: SuppressMessage(
    "Design",
    "CA1063:Implement IDisposable Correctly",
    Justification = "Adds unnecessary verbosity to tests")]
[assembly: SuppressMessage(
    "Globalization",
    "CA1303:Do not pass literals as localized parameters",
    Justification = "Tests do not need to be localized like libraries.")]
[assembly: SuppressMessage(
    "Usage",
    "CA1816:Dispose methods should call SuppressFinalize",
    Justification = "Adds unnecessary verbosity to tests")]
