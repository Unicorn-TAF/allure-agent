using Allure.Commons;

namespace Unicorn.AllureAgent
{
    internal class Labels
    {
        internal static class Names
        {
            internal const string TestCaseId = "AS_ID";
            internal const string Language = "language";
            internal const string Framework = "framework";
        }

        internal static Label LangLabel { get; } = new Label() { name = Names.Language, value = "C#" };

        internal static Label FrameworkLabel { get; } = new Label() { name = Names.Framework, value = "Unicorn.TAF" };
    }
}
