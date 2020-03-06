using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Formatting",
    "RCS1029:Format binary operator on next line.",
    Justification = "Formatting style I disagree with.")]
[assembly: SuppressMessage(
    "Naming",
    "CA1707:Identifiers should not contain underscores",
    Justification = "Test methods should contain underscores.")]
[assembly: SuppressMessage("Design",
    "CA1063:Implement IDisposable Correctly",
    Justification = "Adds unnecessary verbosity to tests.")]
[assembly: SuppressMessage(
    "Globalization",
    "CA1303:Do not pass literals as localized parameters",
    Justification = "Tests do not need to be localized like libraries.")]
[assembly: SuppressMessage(
    "Usage",
    "CA1816:Dispose methods should call SuppressFinalize",
    Justification = "Adds unnecessary verbosity to tests")]
[assembly: SuppressMessage(
    "Style",
    "VSTHRD200:Use \"Async\" suffix for async methods",
    Justification = "Not applicable for tests")]
